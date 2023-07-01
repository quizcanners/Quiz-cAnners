using QuizCanners.Inspect;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Utils
{
    public static class Singleton
    {
        public static TSingleton GetOrCreate<TSingleton>() where TSingleton : ClassBase, new()
        {
            var inst = Get<TSingleton>();

            if (inst == null) 
            {
                inst = new TSingleton();
                Debug.Log("Creating "+nameof(TSingleton));
            }

            return inst;
        }

        public static TSingleton Get<TSingleton>() where TSingleton : IQcSingleton => SingletonGeneric<TSingleton>.Instance;

        public static List<TInterface> GetAll<TInterface>() => CollectionSingleton<TInterface>.Instances;

        public static TValue GetValue<TService, TValue>(Func<TService,TValue> valueGetter, TValue defaultValue = default, bool logOnServiceMissing = true) where TService : IQcSingleton
        {
            Try<TService>(onFound: s => defaultValue = valueGetter(s), logOnServiceMissing: logOnServiceMissing);
            return defaultValue;
        }

        public static bool Try<TSingleton>(Action<TSingleton> onFound, bool logOnServiceMissing = true) where TSingleton : IQcSingleton =>
            Try(onFound: onFound, onFailed: null, logOnServiceMissing: logOnServiceMissing);

        public static bool Try<TServiceA, TServiceB>(Action<TServiceA, TServiceB> onFound, bool logOnServiceMissing = true) where TServiceA : IQcSingleton where TServiceB : IQcSingleton =>
            Try(onFound: onFound, onFailed: null, logOnServiceMissing: logOnServiceMissing);

        public static bool Try<TService>(Action<TService> onFound, Action onFailed, bool logOnServiceMissing = false) where TService : IQcSingleton
        {
            var inst = SingletonGeneric<TService>.Instance;

            if (IsValid(inst, logOnServiceMissing)) //!QcUnity.IsNullOrDestroyed_Obj(inst) && inst.IsSingletonActive)
            {
                try
                {
                    onFound.Invoke(inst);
                    return true;
                }
                catch (Exception ex)
                {
                    if (pegi.IsExitGUIException(ex))
                        throw ex;
                    else
                        Debug.LogException(ex);
                }
            } else if (logOnServiceMissing) 
            {
                QcLog.ChillLogger.LogWarningOnce("Service {0} is missing".F(typeof(TService).ToPegiStringType()), "SngMsng" + typeof(TService).ToString());
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

        public static bool Try<TServiceA, TServiceB>(Action<TServiceA, TServiceB> onFound, Action onFailed, bool logOnServiceMissing = false) where TServiceA : IQcSingleton where TServiceB : IQcSingleton
        {
            var instA = SingletonGeneric<TServiceA>.Instance;
            var instB = SingletonGeneric<TServiceB>.Instance;

            if (IsValid(instA, logOnServiceMissing) && IsValid(instB, logOnServiceMissing))
            {
                try
                {
                    onFound.Invoke(instA, instB);
                    return true;
                }
                catch (Exception ex)
                {
                    if (pegi.IsExitGUIException(ex))
                        throw ex;
                    else
                        Debug.LogException(ex);
                }
            }
            else if (logOnServiceMissing)
            {
                QcLog.ChillLogger.LogWarningOnce("Service {0} is missing".F(typeof(TServiceA).ToPegiStringType()), "SngMsng" + typeof(TServiceA).ToString());
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

        private static bool IsValid<T>(T srv, bool logWarning) where T : IQcSingleton
        {
            if (QcUnity.IsNullOrDestroyed_Obj(srv))
            {
                if (logWarning)
                {
                    var srvNm = typeof(T).ToPegiStringType();
                    QcLog.ChillLogger.LogWarningOnce("Service {0} is missing".F(srvNm), "SngMsng" + srvNm);
                }

                return false;
            }

            if (!srv.IsSingletonActive)
            {
                if (logWarning)
                {
                    var srvNm = typeof(T).ToPegiStringType();
                    QcLog.ChillLogger.LogWarningOnce("Service {0} is deactivated".F(srvNm), "SngInAct" + srvNm);
                }

                return false;
            }

            return true;
        }

        private static class SingletonGeneric<T>
        {
            private static T instance;
            private static readonly Gate.Integer _versionGate = new();
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
                    Collector.RegisterSingleton(value, typeof(T));
                }
            }
        }

        private static class CollectionSingleton<T>
        {
            private static List<T> instances;
            private static readonly Gate.Integer _versionGate = new();
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
                    "Service {0} is needed".F(typeof(TService).ToPegiStringType()).PegiLabel().WriteWarning();
                    pegi.Nl();
                    return pegi.StateToken.True;
                }

                return pegi.StateToken.False;
            }

            internal static int Version;

            private static readonly Dictionary<Type, object> _services = new();

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

            public static void RegisterSingleton(object service, Type type) 
            {
#if UNITY_EDITOR

                if (_services.TryGetValue(type, out var existing) && service.GetType() != existing.GetType())
                    Debug.LogError("Singleton Collection already Contains {0}. Trying to replace with {1}".F(existing.GetType().ToPegiStringType(), service.GetType().ToPegiStringType()));

#endif

                _services[type] = service;
                Version++;
            }

            public static void RegisterSingleton<T>(T service)
            {
                _services[typeof(T)] = service;
                Version++;
            }

            private static readonly PlayerPrefValue.Int _prsstInspectedIndex = new("qc_SrvInsp", -1);
            private static readonly PlayerPrefValue.String _prsstInspectedCategory = new("qc_SrvCat", "");

            private static readonly LoopLock _singletonLoop = new();

            private static string _searchText = "";

            public static void Inspect()
            {
                if (!_singletonLoop.Unlocked) 
                {
                    pegi.Nl();
                    "Recursion detected.".PegiLabel().Write().Nl();
                    return;

                }

                using (_singletonLoop.Lock())
                {

                    int inspectedService = _prsstInspectedIndex.GetValue();

                    pegi.Nl();

                    HashSet<Type> processedTypes = new();
                    HashSet<string> processedCategories = new();

                    if (_prsstInspectedCategory.GetValue().IsNullOrEmpty() == false)
                    {
                        if (Icon.Exit.Click() | _prsstInspectedCategory.GetValue().PegiLabel().ClickLabel().Nl())
                            _prsstInspectedCategory.SetValue("");
                    }

                    if (inspectedService == -1)
                    {
                        var enteredCategory = _prsstInspectedCategory.GetValue();

                        bool enteredAnyCategory = enteredCategory.IsNullOrEmpty() == false;

                        bool inspectingSearch = false;

                        if (!enteredAnyCategory)
                        {
                            "Search".PegiLabel(50).Edit(ref _searchText);
                            Icon.Clear.Click(() => _searchText = "");
                            pegi.Nl();

                            inspectingSearch = _searchText.IsNullOrEmpty() == false;

                            if (inspectingSearch)
                            {
                                for (int i = 0; i < _services.Count; i++)
                                {
                                    var s = _services.GetElementAt(i).Value;

                                    if (pegi.Try_SearchMatch_Obj(s, _searchText))
                                    {
                                        InspectServiceInList(s, i);
                                        pegi.Nl();
                                    }

                                }
                            }
                        }

                        if (!inspectingSearch)
                        {
                            for (int i = 0; i < _services.Count; i++)
                            {
                                KeyValuePair<Type, object> el = _services.GetElementAt(i);


                                string myCategory = el.Value is not IQcSingleton srv ? Categories.DEFAULT : srv.InspectedCategory;

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
                                        if (Icon.List.Click() | myCategory.PegiLabel().ClickLabel().Nl())
                                            _prsstInspectedCategory.SetValue(myCategory);
                                    }
                                }
                                else
                                {
                                    show = enteredCategory.Equals(myCategory);
                                }

                                if (!show)
                                    continue;

                                var service = el.Value;
                                if (QcUnity.IsNullOrDestroyed_Obj(service))
                                {
                                    pegi.Nl();
                                    "Service {0} destroyed".F(el.Key).PegiLabel().WriteWarning();
                                    pegi.Nl();
                                    continue;
                                }

                                if (processedTypes.Contains(service.GetType()))
                                    continue;

                                processedTypes.Add(service.GetType());

                                InspectServiceInList(service, i);

                                pegi.Nl();
                            }
                        }
                    }
                    else
                    {
                        var s = _services.TryGetElementByIndex(inspectedService);

                        using (pegi.Styles.Background.ExitLabel.SetDisposible())
                        {
                            if (Icon.Exit.Click() | s.GetNameForInspector().PegiLabel(style: pegi.Styles.ExitLabel).ClickLabel().Nl())
                                inspectedService = -1;
                        }

                        if (s != null)
                            pegi.Nested_Inspect(ref s);
                        else
                            "NULL".PegiLabel().Nl();
                    }


                    void InspectServiceInList(object service, int index)
                    {
                        if (service is IPEGI_ListInspect lst)
                        {
                            int entered = -1;
                            if (lst.InspectInList_Nested(ref entered, index))
                            {
                                if (entered == index)
                                    inspectedService = index;
                            }
                        }
                        else
                        {
                            if (service.GetNameForInspector().PegiLabel().ClickLabel())
                                inspectedService = index;

                            if (Icon.Enter.Click())
                                inspectedService = index;

                            pegi.ClickHighlight((service as UnityEngine.Object));
                        }
                    }

                    Inspect_LoadingProgress();

                    _prsstInspectedIndex.SetValue(inspectedService);
                }
            }

            public static void Inspect_LoadingProgress() 
            {
                foreach(var s in _services) 
                {
                    try
                    {
                        if (s.Value != null)
                        {
                            string state = "";
                            float progress01 = 0.5f;

                            if (s.Value is ILoadingProgressForInspector load && load.IsLoading(ref state, ref progress01))
                            {
                               "{0} {1}%  ({2})".F(s.Key, Mathf.FloorToInt(progress01 * 100), state).PegiLabel().DrawProgressBar(progress01);
                            }
                            pegi.Nl();

                        }
                    } catch (Exception ex) 
                    {
                        Debug.LogException(ex);
                        ex.ToString().PegiLabel().WriteWarning();
                        pegi.Nl();
                    }
                }
            }
        }

        public class Categories 
        {
            public const string DEFAULT = "Other";
            public const string SCENE_MGMT = "Unity Systems";
            public const string AUDIO = "Audio";
            public const string RENDERING = "Rendering";
            public const string TEST = "Test";
            public const string GAME_LOGIC = "Game Logic";
            public const string ROOT = "";
            public const string POOL = "Pools";
        }

        public abstract class BehaniourBase : MonoBehaviour, IQcSingleton, IPEGI, IPEGI_ListInspect, INeedAttention
        {
            protected enum SingletonCollisionSolutionEnum { DestroyNew, DestroyPrevious, KeepBothAssignNew, KeepBothAssignOld }

            protected virtual SingletonCollisionSolutionEnum SingletonCollisionSolution => SingletonCollisionSolutionEnum.DestroyNew;

            public virtual string InspectedCategory => Categories.DEFAULT;

            public virtual bool IsSingletonActive { get => gameObject.activeInHierarchy; set { gameObject.SetActive(value); } } 

            protected virtual void OnRegisterServiceInterfaces() { }

            private readonly List<Type> _typesToRemove = new();

            protected void RegisterServiceAs<T>() 
            {
                _typesToRemove.Add(typeof(T));
                Collector.RegisterSingleton(this, typeof(T));
            }

            //[SerializeField] 
            private bool _afterEnableCalled;

            protected virtual void OnAfterEnable() 
            { 

            }

            private System.Collections.IEnumerator AfterEnableCoro() 
            {
                yield return null;
                _afterEnableCalled = true;
                OnAfterEnable();
            }

            protected virtual void OnBeforeOnDisableOrEnterPlayMode(bool afterEnableCalled) 
            {
                
            }

            protected void OnDisable()
            {
                try
                {
                   
                    OnBeforeOnDisableOrEnterPlayMode(_afterEnableCalled);
                   
                } catch(Exception ex) 
                {
                    Debug.LogException(ex);
                }
                finally 
                {
                    _afterEnableCalled = false;
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

            protected void RegisterSevice() 
            {
                
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
                            bool destroy = SingletonCollisionSolution == SingletonCollisionSolutionEnum.DestroyNew || SingletonCollisionSolution == SingletonCollisionSolutionEnum.DestroyPrevious;
                            bool useNew = SingletonCollisionSolution == SingletonCollisionSolutionEnum.DestroyPrevious || SingletonCollisionSolution == SingletonCollisionSolutionEnum.KeepBothAssignNew;

                            var deprecated = useNew ? previousObj : this;
                            var current = useNew ? this : previousObj;

                            if (destroy && deprecated)
                            {
                                Debug.Log("{0}: {1} ({2})".F(SingletonCollisionSolution.ToString().SimplifyTypeName(), deprecated.gameObject.name, GetType().ToPegiStringType()), gameObject);
                                Destroy(deprecated.gameObject);
                            }

                            if (!useNew)
                            {
                                return;
                            }
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

                Collector.RegisterSingleton(this, type);
                StartCoroutine(AfterEnableCoro());

#if UNITY_EDITOR
                UnityEditor.EditorApplication.playModeStateChanged += StateChangeProcessor;
#endif
            }

#if UNITY_EDITOR
            private void StateChangeProcessor(UnityEditor.PlayModeStateChange newState)
            {
                if (newState == UnityEditor.PlayModeStateChange.ExitingEditMode)
                {
                    OnBeforeOnDisableOrEnterPlayMode(_afterEnableCalled);
                    _afterEnableCalled = false;
                }
            }
#endif

            #region Inspector

            public override string ToString() => 
                QcSharp.AddSpacesToSentence(
                GetType().ToPegiStringType().Replace("Singleton_","").Replace("Pool_", ""),
                preserveAcronyms: true);

            public virtual void Inspect()
            {
                if (Application.isPlaying == false)
                {
                    string preferedName = "MGMT-"+ ToString();

                    if (preferedName.Equals(gameObject.name) == false && "Set Go Name".PegiLabel(toolTip: preferedName).Click())
                        gameObject.name = preferedName;
                }

                pegi.Nl();
            }

            public virtual void InspectInList(ref int edited, int ind)
            {
                if (gameObject.activeSelf && !gameObject.activeInHierarchy)
                    Icon.Warning.Draw("Object is disabled in hierarchy");
                else if ((IsSingletonActive ? Icon.Active : Icon.InActive).Click(toolTip: gameObject.activeSelf ? "Make Inactive" : "Make Active"))
                    IsSingletonActive = !IsSingletonActive; 
                
                if (ToString().PegiLabel().ClickLabel() | this.Click_Enter_Attention())
                    edited = ind;

                pegi.ClickHighlight(this);

            }

            public virtual string NeedAttention()
            {
                if (!gameObject)
                    return "Game Object is destroyed";

                if (gameObject.activeInHierarchy && !enabled)
                    return "Game object is active but component is disabled.";

                return null;
            }
            #endregion

        }

        public abstract class ClassBase : IQcSingleton
        {
            public virtual string InspectedCategory => Categories.DEFAULT;

            public virtual bool IsSingletonActive { get => true; set { } }

            public ClassBase()
            {
                Collector.RegisterSingleton(this, GetType()); 
            }

            public string NameForDisplayPEGI() => QcSharp.AddSpacesInsteadOfCapitals(GetType().ToString().SimplifyTypeName(), keepCatipals: false);
        }

        public interface IQcSingleton
        {
            string InspectedCategory { get; }

            bool IsSingletonActive { get; set; }
        }

        public interface ILoadingProgressForInspector 
        {
            bool IsLoading(ref string state, ref float progress01);
          
        }
    }
}
