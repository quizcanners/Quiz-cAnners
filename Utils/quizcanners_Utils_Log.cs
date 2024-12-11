using QuizCanners.Inspect;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QuizCanners.Utils
{
    public static class QcLog
    {
        public static bool IfNull<T>(T val, string context) where T : class
        {
            if (val == null)
            {
                Debug.LogError(IsNull(val, context));
                return true;
            }

            return false;
        }

        public static string IsNull<T>(T _, string context) =>
            "{1}: {0} is not found".F(typeof(T).ToPegiStringType(), context);

        public static string CaseNotImplemented(object unimplementedValue)
            => "Case [{0}] for [{1}] is not implemented".F(
                unimplementedValue.ToString().SimplifyTypeName(),
                unimplementedValue.GetType().ToPegiStringType());

        public static string CaseNotImplemented<T>(T unimplementedValue, string context)
        {
          //  string valueName;

            var type = typeof(T);

            /*
            if (type.IsEnum)
            {
                valueName = Enum.GetName(type, unimplementedValue);
            }
            else
                valueName = unimplementedValue.ToString().SimplifyTypeName();*/

            return "Case [{0}] for [{1}] is not implemented for {2}".F(
               unimplementedValue.ToString().SimplifyTypeName(),
               type.ToPegiStringType(),
               context
               );
        }

        /*
        public static string CaseNotImplemented(object unimplementedValue, string context)
        {
            return "Case [{0}] for [{1}] is not implemented for {2}".F(
               unimplementedValue.ToString().SimplifyTypeName(),
               unimplementedValue.GetType().ToPegiStringType(),
               context
               );
        }*/

        public static InspectableLogging LogHandler = new InspectableLogging();

        public class InspectableLogging : IPEGI
        {
            private bool _subscribedToLogs;
            private bool _subscribedToQuit;
            private readonly int _maxLogs;

            public bool SavingLogs
            {
                get => _subscribedToLogs;
                set
                {
                    if (_subscribedToLogs == value)
                        return;

                    _subscribedToLogs = value;

                    if (_subscribedToLogs)
                        Application.logMessageReceived += HandleLog;
                    else
                        Application.logMessageReceived -= HandleLog;

                    if (!_subscribedToQuit) 
                    {
                        _subscribedToQuit = true;
                        Application.quitting += () => SavingLogs = false;
                    }
                }
            }

            private void HandleLog(string logString, string stackTrace, LogType type)
            {
                if (logs.Count > _maxLogs)
                {
                    int toClear = Mathf.FloorToInt(_maxLogs * 0.5f);
                    logs.RemoveRange(toClear, toClear);
                }

                logs.Add(new LogData { Log = logString, Stack = stackTrace, type = type });
            }

            public InspectableLogging (int maxRecods = 300) 
            {
                _maxLogs = maxRecods;
            }

            #region Inspector

            private readonly List<LogData> logs = new();
            private readonly pegi.CollectionInspectorMeta _logMeta = new(labelName: "Logs", showAddButton: false, showEditListButton: false, showCopyPasteOptions: true); // _inspectedLog = -1;
            void IPEGI.Inspect()
            {
                var sub = SavingLogs;
                if ("Save Logs".PL().ToggleIcon(ref sub).Nl())
                    SavingLogs = sub;

                if (!_logMeta.IsAnyEntered)
                {
                    if (logs.Count > 10)
                    {
                        if ("Clear All But 5".PL().ClickConfirm(confirmationTag: "Del Logs").Nl())
                            logs.RemoveRange(0, logs.Count - 5);
                    }
                    else
                    {
                        if (SavingLogs && "Create Test Logs".PL().Click().Nl())
                        {
                            Debug.Log("Debug Log");
                            Debug.LogWarning("Log Warning");
                            Debug.LogError("Log Error");
                            try
                            {
                                int x = 0;
                                int y = 10 / x;
                            }
                            catch (Exception ex)
                            {
                                Debug.LogException(ex);
                            }
                        }
                    }
                }

                if (logs.Count == 0)
                    "NO LOGS YET".PL().Nl();
                else
                    _logMeta.Edit_List(logs);
            }

            #endregion
        }

        private class LogData : IPEGI, INeedAttention, ISearchable, IPEGI_ListInspect
        {
            public string Log;
            public string Stack;
            public LogType type;

            public override string ToString() => Log;

            public void InspectInList(ref int edited, int ind)
            {
                if (this.Click_Enter_Attention() | ToString().PL().ClickLabel())
                    edited = ind;
            }

            void IPEGI.Inspect()
            {
                "Log:".ConstL().Write_ForCopy(Log, showCopyButton: true).Nl();
                "Stack:".PL().Write_ForCopy_Big(Stack, showCopyButton: true).Nl();
            }

            public string NeedAttention()
            {
                switch (type)
                {
                    case LogType.Log:
                    case LogType.Warning:
                        return null;
                    default:
                        return type.ToString().SimplifyTypeName();
                }
            }

            public IEnumerator SearchKeywordsEnumerator()
            {
                yield return Log;
                yield return type.ToString();
            }
        }

        public class ChillLogger 
        {
            private bool _logged;
            private readonly bool _logInBuild;
            private double _lastLogged;
            private int _calls;
            private readonly string _name = "error";

            public override string ToString() => _name + (Disabled ? " Disabled" : " Enabled");

            protected bool Disabled => QcDebug.IsRelease && !_logInBuild;
            private static readonly Dictionary<string, int> loggedErrors = new();
            private static readonly Dictionary<string, int> loggedWarnings = new();

            public ChillLogger(string name, bool logInBuild = false)
            {
                _name = name;
#if !UNITY_EDITOR
                _logInBuild = logInBuild;
#else
                _logInBuild = logInBuild;
#endif
            }

            public ChillLogger()
            {

            }

            public void Log_Now(Exception ex, Object obj = null)
            {
                if (Disabled)
                    return;

                if (obj)
                    Debug.LogException(ex, obj);
                else
                    Debug.LogException(ex);

                _lastLogged = QcUnity.TimeSinceStartup();
                _calls = 0;
                _logged = true;
            }

            public void Log_Now(string msg, bool asError, Object obj = null)
            {
                if (Disabled)
                    return;

                if (!_name.IsNullOrEmpty())
                    msg = "{0}: {1}".F(_name, msg); 

                if (_calls > 0)
                    msg += " [+ {0} calls]".F(_calls);

                if (_lastLogged > 0)
                    msg += " [{0} s. later]".F(QcUnity.TimeSinceStartup() - _lastLogged);
                else
                    msg += " [at {0}]".F(QcUnity.TimeSinceStartup());

                if (asError)
                    Debug.LogError(msg, obj);
                else
                    Debug.Log(msg, obj);

                _lastLogged = QcUnity.TimeSinceStartup();
                _calls = 0;
                _logged = true;
            }

            public void Log(string msg = null, float seconds = 5, bool asError = true, Object target = null)
            {
                if (Disabled)
                    return;

                if (!_logged || (QcUnity.TimeSinceStartup() - _lastLogged > seconds))
                    Log_Now(msg, asError, target);
                else
                    _calls++;
            }

            public void Log(Exception err = null, float seconds = 5, Object obj = null)
            {
                if (Disabled)
                    return;

                if (!_logged || (QcUnity.TimeSinceStartup() - _lastLogged > seconds))
                    Log_Now(err, obj);
                else
                    _calls++;
            }

            public void Log_Every(int callCount, string msg = null, bool asError = true, Object obj = null)
            {
                if (Disabled)
                    return;

                if (!_logged || (_calls > callCount))
                    Log_Now(msg, asError, obj);
                else
                    _calls++;
            }

            public static void LogErrorOnce(string msg, string key, Object target = null)
            {
                if (key.IsNullOrEmpty()) 
                {
                    Debug.LogError("Chill Key is Null: " + msg);
                    return;
                }

                int count = loggedErrors.GetOrCreate(key);
                loggedErrors[key]++;

                if (count>0)
                    return;

                if (target)
                    Debug.LogError(msg, target);
                else
                    Debug.LogError(msg);
            }

            public static void LogErrorOnce(Func<string> action, string key, Object target = null)
            {
                if (key.IsNullOrEmpty())
                {
                    Debug.LogError("Chill Key is Null: " + action?.Invoke());
                    return;
                }

                int count = loggedErrors.GetOrCreate(key);
                loggedErrors[key]++;

                if (count > 0)
                    return;

                if (target)
                    Debug.LogError(action(), target);
                else
                    Debug.LogError(action());
            }

            public static void LogWarningOnce(Func<string> action, string key, Object target = null)
            {
                if (key.IsNullOrEmpty())
                {
                    Debug.LogError("Chill Key is Null: " + action());
                    return;
                }

                int count = loggedWarnings.GetOrCreate(key);
                loggedWarnings[key]++;

                if (count > 0)
                    return;

                if (target)
                    Debug.LogWarning(action(), target);
                else
                    Debug.LogWarning(action());
            }

            public static void LogWarningOnce(string msg, string key, Object target = null)
            {
                if (key.IsNullOrEmpty())
                {
                    Debug.LogError("Chill Key is Null: " + msg);
                    return;
                }

                int count = loggedWarnings.GetOrCreate(key);
                loggedWarnings[key]++;

                if (count > 0)
                    return;

                if (target)
                    Debug.LogWarning(msg, target);
                else
                    Debug.LogWarning(msg);
            }

            public static void LogErrosExpOnly(Func<string> action, string key, Object target = null)
            {
                if (key.IsNullOrEmpty())
                {
                    Debug.LogError("Chill Key is Null: " + action?.Invoke());
                    return;
                }

                int count = loggedErrors.GetOrCreate(key);
                loggedErrors[key]++;

                if (count > 4 && !Mathf.IsPowerOfTwo(count))
                    return;

                string logText = (count > 0 ? "{0} times: ".F(count) : "") + action(); 

                if (target)
                    Debug.LogError(logText, target);
                else
                    Debug.LogError(logText);
            }

            public static void LogExceptionExpOnly(Exception ex, string key, Object target = null)
            {
                if (key.IsNullOrEmpty())
                {
                    Debug.LogException(ex);
                    return;
                }

                int count = loggedErrors.GetOrCreate(key);
                loggedErrors[key]++;

                if (count > 4 && !Mathf.IsPowerOfTwo(count))
                    return;

 
                if (target)
                    Debug.LogException(ex, target);
                else
                    Debug.LogException(ex);
            }
        }
    }
}
