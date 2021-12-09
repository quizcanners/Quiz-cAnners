using QuizCanners.Inspect;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Utils
{
    public static class Singleton
    {
        public static TSingleton Get<TSingleton>() where TSingleton : IQcSingleton => SingletonGeneric<TSingleton>.Instance;

        public static List<TInterface> GetAll<TInterface>() => CollectionSingleton<TInterface>.Instances;

        public static TValue TryGetValue<TService, TValue>(Func<TService,TValue> valueGetter, TValue defaultValue = default(TValue)) where TService : IQcSingleton
        {
            Try<TService>(onFound: s => defaultValue = valueGetter(s));
            return defaultValue;
        }

        public static bool Try<TSingleton>(Action<TSingleton> onFound, bool logOnServiceMissing = true) where TSingleton : IQcSingleton =>
            Try(onFound: onFound, onFailed: null, logOnServiceMissing: logOnServiceMissing);

        public static bool Try<TService>(Action<TService> onFound, Action onFailed, bool logOnServiceMissing = false) where TService : IQcSingleton
        {
            var inst = SingletonGeneric<TService>.Instance;

            if (QcUnity.IsNullOrDestroyed_Obj(inst) == false)
            {
                try
                {
                    onFound.Invoke(inst);
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            } else if (logOnServiceMissing) 
            {
                Debug.LogError("Service {0} is missing".F(typeof(TService).ToPegiStringType()));
            }
         

            if (onFailed != null)
            {
                try
                {
                    onFailed.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            return false;
        }

        private static class SingletonGeneric<T>
        {
            private static T instance;
            private static readonly Gate.Integer _versionGate = new Gate.Integer();
            public static T Instance
            {
                get
                {
                    if (_versionGate.TryChange(Collector.Version))
                    {
                        instance = (T)Collector.Get(typeof(T));
                    }

                    return instance;
                }
                set
                {
                    instance = value;
                    Collector.RegisterService(value, typeof(T));
                }
            }
        }

        private static class CollectionSingleton<T>
        {
            private static List<T> instances;
            private static readonly Gate.Integer _versionGate = new Gate.Integer();
            public static List<T> Instances
            {
                get
                {
                    if (_versionGate.TryChange(Collector.Version) || instances == null)
                    {
                        instances = Collector.GetAll<T>();
                    }

                    return instances;
                }
            }
        }

        public static class Collector
        {
            public static pegi.StateToken InspectionWarningIfMissing<TService>() where TService: BehaniourBase 
            {
                var val = Get<TService>();
                if (QcUnity.IsNullOrDestroyed_Obj(val))
                {
                    "Service {0} is needed".F(typeof(TService).ToPegiStringType()).PegiLabel().writeWarning();
                    pegi.nl();
                    return pegi.StateToken.True;
                }

                return pegi.StateToken.False;
            }

            internal static int Version;

            private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

            internal static List<T> GetAll<T>() 
            {
                var l = new List<T>();
                foreach(var s in _services)
                {
                    if (typeof(T).IsAssignableFrom(s.Key))
                          l.Add((T)s.Value);
                    
                }
                return l;
            }

            public static object Get(Type type) => _services.TryGet(type);

            public static void TryRemove(object obj) => TryRemove(obj, obj.GetType());

            public static void TryRemove(object obj, Type type)
            {
                if (_services.TryGetValue(type, out object s) && s.Equals(obj))
                {
                    _services.Remove(type);
                }
            }

            public static void RegisterService(object service, Type type) 
            {
                _services[type] = service;
                Version++;
            }

            public static void RegisterService<T>(T service)
            {
                _services[typeof(T)] = service;
                Version++;
            }

            private static readonly PlayerPrefValue.Int _prsstInspectedIndex = new PlayerPrefValue.Int("qc_SrvInsp", -1);
            private static readonly PlayerPrefValue.String _prsstInspectedCategory = new PlayerPrefValue.String("qc_SrvCat", "");


            public static void Inspect()
            {
                int inspectedService = _prsstInspectedIndex.GetValue();

                pegi.nl();

                HashSet<Type> processedTypes = new HashSet<Type>();
                HashSet<string> processedCategories = new HashSet<string>();

                if (_prsstInspectedCategory.GetValue().IsNullOrEmpty() == false) 
                {
                    if (icon.Back.Click() | _prsstInspectedCategory.GetValue().PegiLabel().ClickLabel().nl()) 
                        _prsstInspectedCategory.SetValue("");
                }

                if (inspectedService == -1)
                {
                    var enteredCategory = _prsstInspectedCategory.GetValue();

                    for (int i = 0; i < _services.Count; i++)
                    {
                        KeyValuePair<Type, object> el = _services.GetElementAt(i);

                        var srv = el.Value as IQcSingleton;
                       
                        string myCategory = srv == null ? Categories.DEFAULT : srv.InspectedCategory;

                        bool show = false;

                        if (enteredCategory.IsNullOrEmpty()) 
                        {
                            if (myCategory.IsNullOrEmpty())
                                show = true;
                            else
                            {
                                if (processedCategories.Contains(myCategory))
                                    continue;

                                processedCategories.Add(myCategory);
                                if (icon.List.Click() | myCategory.PegiLabel().ClickLabel().nl())
                                    _prsstInspectedCategory.SetValue(myCategory);
                            }
                        } else 
                        {
                            show = enteredCategory.Equals(myCategory);
                        }

                        if (!show)
                            continue;

                        var service = el.Value;
                        if (QcUnity.IsNullOrDestroyed_Obj(service))
                        {
                            pegi.nl();
                            "Service {0} destroyed".F(el.Key).PegiLabel().writeWarning();
                            pegi.nl();
                            continue;
                        }

                        if (processedTypes.Contains(service.GetType())) 
                            continue;
                        
                        processedTypes.Add(service.GetType());

                        if (service.GetNameForInspector().PegiLabel().ClickLabel())
                            inspectedService = i;

                        if (icon.Enter.Click())
                            inspectedService = i;

                        (service as UnityEngine.Object).ClickHighlight();

                        pegi.nl();
                    }
                } else 
                {
                    var s = _services.TryGetByElementIndex(inspectedService);

                    using (pegi.Styles.Background.ExitLabel.SetDisposible())
                    {
                        if (icon.Exit.Click() | s.GetNameForInspector().PegiLabel(style: pegi.Styles.ExitLabel).ClickLabel().nl())
                            inspectedService = -1;
                    }

                    if (s != null)
                        pegi.Nested_Inspect(ref s);
                    else
                        "NULL".PegiLabel().nl();
                }

                Inspect_LoadingProgress();

                _prsstInspectedIndex.SetValue(inspectedService);
            }

            public static void Inspect_LoadingProgress() 
            {
                foreach(var s in _services) 
                {
                    try
                    {
                        if (s.Value != null)
                        {
                            var load = s.Value as ILoadingProgressForInspector;

                            string state = "";
                            float progress01 = 0.5f;

                            if (load != null && load.IsLoading(ref state, ref progress01))
                            {
                               "{0} {1}%  ({2})".F(s.Key, Mathf.FloorToInt(progress01 * 100), state).PegiLabel().drawProgressBar(progress01);
                            }
                            pegi.nl();

                        }
                    } catch (Exception ex) 
                    {
                        Debug.LogException(ex);
                        ex.ToString().PegiLabel().writeWarning();
                        pegi.nl();
                    }
                }
            }
        }

        public class Categories 
        {
            public const string DEFAULT = "Other";
            public const string SCENE_MGMT = "Scene Management";
            public const string RENDERING = "Rendering";
            public const string TEST = "Test";
            public const string GAME_LOGIC = "Game Logic";
            public const string ROOT = "";
        }




        public abstract class BehaniourBase : MonoBehaviour, IQcSingleton, IPEGI, IGotReadOnlyName, IPEGI_ListInspect, INeedAttention
        {
            protected enum SingletonType { DestroyNew, DestroyPrevious, KeepBothAssignNew, KeepBothAssignOld }

            protected virtual SingletonType Singleton => SingletonType.DestroyNew;

            public virtual string InspectedCategory => Categories.DEFAULT;

            protected virtual void AfterEnable(){}

            protected virtual void OnRegisterServiceInterfaces() { }

            private readonly List<Type> _typesToRemove = new List<Type>();

            protected void RegisterServiceAs<T>() 
            {
                _typesToRemove.Add(typeof(T));
                Collector.RegisterService(this, typeof(T));
            }

            private System.Collections.IEnumerator AfterEnableCoro() 
            {
                yield return null;
                AfterEnable();
            }

            protected virtual void OnBeforeOnDisableOrEnterPlayMode() 
            {
                
            }

            private void OnDisable()
            {
                try
                {
                    OnBeforeOnDisableOrEnterPlayMode();
                } catch(Exception ex) 
                {
                    Debug.LogException(ex);
                }
#if UNITY_EDITOR
                UnityEditor.EditorApplication.playModeStateChanged -= StateChangeProcessor;
#endif
            }

            protected virtual void OnDestroy()
            {
                Collector.TryRemove(this);
                foreach (var t in _typesToRemove)
                    Collector.TryRemove(this, t);

            }

            protected void OnEnable()
            {
                var type = GetType();

                if (Application.isPlaying)
                {
                    var previous = Collector.Get(type);
                    if (previous != null)
                    {
                        var previousObj = previous as BehaniourBase;

                        if (previousObj && previousObj != this)
                        {
                            bool destroy = Singleton == SingletonType.DestroyNew || Singleton == SingletonType.DestroyPrevious;
                            bool useNew = Singleton == SingletonType.DestroyPrevious || Singleton == SingletonType.KeepBothAssignNew;

                            var deprecated = useNew ? previousObj : this;
                            var current = useNew ? this : previousObj;

                            if (destroy)
                                Destroy(deprecated.gameObject);

                            if (useNew)
                            {
                                Collector.RegisterService(current, type);
                                StartCoroutine(AfterEnableCoro());
                            }

                            return;
                        }
                    }
                }

                try 
                {
                    OnRegisterServiceInterfaces();
                } catch (Exception ex) 
                {
                    Debug.LogException(ex, this);
                }

                Collector.RegisterService(this, type);
                StartCoroutine(AfterEnableCoro());

#if UNITY_EDITOR
                UnityEditor.EditorApplication.playModeStateChanged += StateChangeProcessor;
#endif
            }

#if UNITY_EDITOR
            private void StateChangeProcessor(UnityEditor.PlayModeStateChange newState)
            {
                if (newState == UnityEditor.PlayModeStateChange.ExitingEditMode)
                    OnBeforeOnDisableOrEnterPlayMode();
            }
#endif

            #region Inspector

            public virtual string GetReadOnlyName() => 
                QcSharp.AddSpacesInsteadOfCapitals(
                GetType().ToPegiStringType().Replace("Service",""),
                keepCatipals: false);

            public virtual void Inspect()
            {
                if (Application.isPlaying == false)
                {
                    string preferedName = GetReadOnlyName();

                    if (preferedName.Equals(gameObject.name) == false && "Set Go Name".PegiLabel(toolTip: preferedName).Click())
                        gameObject.name = preferedName;
                }

                pegi.nl();
            }

            public virtual void InspectInList(ref int edited, int ind)
            {
                if (GetReadOnlyName().PegiLabel().ClickLabel() | this.Click_Enter_Attention())
                    edited = ind;

                this.ClickHighlight();

            }

            public virtual string NeedAttention()
            {
                if (!gameObject)
                    return "Game Object is destroyed";

                if (!enabled || !gameObject.activeInHierarchy)
                    return "Object is Disabled";

                return null;
            }
            #endregion

        }

        public abstract class ClassBase : IQcSingleton
        {
            public virtual string InspectedCategory => Categories.DEFAULT;

            public ClassBase()
            {
                Collector.RegisterService(this, GetType()); 
            }

            public string NameForDisplayPEGI() => QcSharp.AddSpacesInsteadOfCapitals(GetType().ToString().SimplifyTypeName(), keepCatipals: false);
        }

        public interface IQcSingleton 
        {
            string InspectedCategory { get; }
        }

        public interface ILoadingProgressForInspector 
        {
            bool IsLoading(ref string state, ref float progress01);
          
        }
    }
}
