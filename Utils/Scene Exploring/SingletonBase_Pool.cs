using QuizCanners.Inspect;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Utils
{
    public abstract class PoolSingletonBase<T> : Singleton.BehaniourBase, IEnumerable<T>, IGotCount where T: Component
    {
        [SerializeField] protected List<T> prefabs = new List<T>();
        [SerializeField] private bool _disablePooling;

        public bool DisablePooling 
        {
            get => _disablePooling;
            set 
            {
                _disablePooling = value;
                if (value)
                {
                    ClearPool();
                }    
            }
        }

        protected int lastInstancePrefab;

        protected virtual int MAX_INSTANCES => 50;

        public virtual float VacancyPortion => (MAX_INSTANCES - (float)instances.Count) / MAX_INSTANCES;

        protected List<T> pool = new List<T>();
        protected List<T> instances = new List<T>();

        public int InstancesCount => instances.Count;

        public int TotalCount => instances.Count + pool.Count;

        public bool CanSpawn() 
        {
            if (!IsSingletonActive()) //!gameObject.activeInHierarchy)
                return false;

            if (prefabs.Count == 0)
            {
                QcLog.ChillLogger.LogErrorOnce("No Prefabs", key: "noSmokePrefab", this);
                return false;
            }

            if (instances.Count >= MAX_INSTANCES)
                return false;

            return true;
        }

        public bool CanSpawnIfVisible(Vector3 pos)
        {
            if (!Camera.main.IsInCameraViewArea(pos))
                return false;

            return CanSpawn();
        }

        protected Vector3 GetScaleBasedOnDistance(Vector3 pos) => Vector3.one * GetScaleFactorFromDistance(pos);

        protected float GetScaleFactorFromDistance(Vector3 pos) => (0.25f + QcMath.SmoothStep(0, 5, GetDistanceToCamera(pos)) * 0.75f);

        protected float GetDistanceToCamera(Vector3 pos)
        {
            var cam = Singleton.Get<Singleton_CameraOperatorGodMode>();

            if (cam) 
            {
                Vector3.Distance(cam.transform.position, pos);
            }

            return Vector3.Distance(Camera.main.transform.position, pos);
        }
        
        protected virtual void OnReturn(T element) { }

        public void ReturnToPool(T effect)
        {
            instances.Remove(effect);

            try 
            {
                OnReturn(effect);
            } catch (Exception ex) 
            {
                Debug.LogException(ex);
            }

            if (DisablePooling)
            {
                effect.gameObject.DestroyWhatever();
                return;
            }

            pool.Add(effect);
            effect.gameObject.SetActive(false);
            
        }

        public bool TrySpawnIfVisible(Vector3 worldPosition, Action<T> onInstanciate = null)
        {
            if (!CanSpawnIfVisible(worldPosition))
                return false;

            if (Spawn_Internal(worldPosition, out var inst))
            {
                try
                {
                    onInstanciate?.Invoke(inst);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex, this);
                }

                return true;
            }

            return false;
        }

        public bool TrySpawnIfVisible(Vector3 worldPosition, out T inst)
        {
            inst = null;

            if (!CanSpawnIfVisible(worldPosition))
                return false;

            return Spawn_Internal(worldPosition, out inst);
        }

        public bool TrySpawn(Vector3 worldPosition = default(Vector3), Action<T> onInstanciate = null)
        {
            if (!CanSpawn())
                return false;

            if (Spawn_Internal(worldPosition, out var inst))
            {
                try
                {
                    onInstanciate?.Invoke(inst);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex, this);
                }

                return true;
            }

            return false;
        }

        public bool TrySpawn(Vector3 worldPosition, out T inst)
        {
            if (!CanSpawn())
            {
                inst = null;
                return false;
            }

            return Spawn_Internal(worldPosition, out inst);
        }

        private bool Spawn_Internal(Vector3 worldPosition, out T inst) 
        {
            if (pool.Count > 0)
            {
                inst = pool.TryTake(0);
                instances.Add(inst);
                inst.transform.position = worldPosition;
                inst.gameObject.SetActive(true);
            }
            else
            {
                using (QcDebug.TimeProfiler.Instance["Pool Instancers"].Sum(typeof(T).ToPegiStringType()).Start())
                {
                    lastInstancePrefab = (lastInstancePrefab + 1) % prefabs.Count;
                    inst = Instantiate(prefabs[lastInstancePrefab], worldPosition, Quaternion.identity, transform);
                }

                instances.Add(inst);

                inst.name += "({0})".F(TotalCount);
            }

            inst.transform.SetSiblingIndex(0);

            OnInstanciated(inst);

          

            return true;
        }

        protected virtual void OnInstanciated(T inst) {}

        protected override void OnBeforeOnDisableOrEnterPlayMode(bool _afterEnableCalled)
        {
            base.OnBeforeOnDisableOrEnterPlayMode(_afterEnableCalled);
            ClearAll();
        }

        protected void ClearAll()
        {
            ClearPool();

            foreach (var e in instances)
                if (e)
                    e.gameObject.DestroyWhatever();

            instances.Clear();
        }

        private void ClearPool() 
        {
            foreach (var e in pool)
                if (e)
                    e.gameObject.DestroyWhatever();

            pool.Clear();
        }

        #region Inspector

        private readonly pegi.CollectionInspectorMeta _active = new pegi.CollectionInspectorMeta(labelName: "Active Instances", showAddButton: false, showCopyPasteOptions: false, showEditListButton: false);

        public override void InspectInList(ref int edited, int ind)
        {
            if (InstancesCount > 0)
            {
                Icon.Clear.Click().OnChanged(ClearAll);

                "{0}/{1}".F(InstancesCount, MAX_INSTANCES).PegiLabel(60).Write();
            }
            

            base.InspectInList(ref edited, ind);
        }

        public override string InspectedCategory => Singleton.Categories.POOL;
        public int GetCount() => instances.Count;

        private readonly pegi.TabContext _tab = new pegi.TabContext();
        public override void Inspect()
        {
            "Pool of {0}".F(typeof(T).Name).PegiLabel(pegi.Styles.ListLabel).Write();

            if (Application.isPlaying && Icon.Play.Click())
                TrySpawn(Vector3.zero, out _);

            if (pool.Count > 0 || instances.Count > 0)
                Icon.Delete.Click().OnChanged(ClearAll);

            pegi.Nl();

            bool isPooling = !DisablePooling;
            "Pooling".PegiLabel().ToggleIcon(ref isPooling).OnChanged(()=> DisablePooling = !isPooling).Nl();

            using (_tab.StartContext()) 
            {
                pegi.AddTab("Prefabs", () => 
                {
                    "Prefabs".PegiLabel(60).Edit_List_UObj(prefabs).Nl();
                    "Capacity: {0}/{1}".F(pool.Count + instances.Count, MAX_INSTANCES).PegiLabel().Nl();
                });

                pegi.AddTab("Instance", () => 
                {
                    _active.Edit_List(instances).Nl();
                });
            }
        }

        #endregion

        #region Enumeration
        public IEnumerator<T> GetEnumerator()
        {
            for (int i = instances.Count - 1; i >= 0; i--)
            {
                yield return instances[i];
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();//instances.GetEnumerator();
        #endregion

        protected override void OnRegisterServiceInterfaces()
        {
            base.OnRegisterServiceInterfaces();
            RegisterServiceAs<PoolSingletonBase<T>>();
        }
    }

    public static partial class Pool
    {
        public static bool TrySpawn<T>(Vector3 position) where T : Component => Singleton.Try<PoolSingletonBase<T>>(s => s.TrySpawn(position, out var result));
        
        public static bool TrySpawn<T>(Vector3 position, out T instance) where T : Component
        {
            T result = null;

            Singleton.Try<PoolSingletonBase<T>>(s => s.TrySpawn(position, out result));

            instance = result;

            return instance;
        }

        public static bool TrySpawn<T>(Vector3 position, Action<T> onInstanciated) where T : Component 
            => Singleton.Try<PoolSingletonBase<T>>(s => s.TrySpawn(position, onInstanciated));

        public static bool TrySpawnIfVisible<T>(Vector3 position) where T : Component => Singleton.Try<PoolSingletonBase<T>>(s => s.TrySpawnIfVisible(position, out var result));

        public static bool TrySpawnIfVisible<T>(Vector3 position, out T instance) where T : Component
        {
            T result = null;

            Singleton.Try<PoolSingletonBase<T>>(s => s.TrySpawnIfVisible(position, out result));

            instance = result;

            return instance;
        }

        public static bool TrySpawnIfVisible<T>(Vector3 position, Action<T> onInstanciated) where T : Component 
            => Singleton.Try<PoolSingletonBase<T>>(s => s.TrySpawnIfVisible(position, onInstanciated));

        public static float VacancyFraction<T>(float defaultValue = 1f) where T : Component => Singleton.TryGetValue<PoolSingletonBase<T>, float>(s => s.VacancyPortion, defaultValue: defaultValue);

        public static void Return<T>(T instance) where T : Component => Singleton.Try<PoolSingletonBase<T>>(onFound: s => s.ReturnToPool(instance), logOnServiceMissing: false); 

        public static void TrySpawnIfVisible<T>(Vector3 position, int preferedCount, Action<T> onInstanciate) where T : Component
        {
            Singleton.Try<PoolSingletonBase<T>>(pool =>
            {
                if (Camera.main.IsInCameraViewArea(position))
                {
                    int count = (int)Math.Max(1, preferedCount * pool.VacancyPortion);

                    for (int i = 0; i < count; i++)
                    {
                        if (!pool.TrySpawn(worldPosition: position, out var instance))
                            break;

                        onInstanciate.Invoke(instance);
                    }
                };
            });
        }
    }

}