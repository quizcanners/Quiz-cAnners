using QuizCanners.Inspect;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Utils
{

    public abstract class PoolBehaviourCore<T> : Singleton.BehaniourBase, IEnumerable<T>, IGotCount where T : Component
    {
        [SerializeField] protected List<T> prefabs = new();
        protected int lastInstancePrefab;

        protected virtual int MAX_INSTANCES => 50;
        protected int RECOMMENDED_INSTANCES => 1 + (int)((MAX_INSTANCES * Math.Clamp(8 - Pool.GetFrameMiliseconds(), 0, 1)) * Pool.MaxCount.GetCoefficientFromFramerate());
        public virtual float VacancyPortion => Mathf.Clamp01((RECOMMENDED_INSTANCES - (float)GetCount()) / RECOMMENDED_INSTANCES);

        protected readonly LoopLock _clearAllLock = new();

        public abstract int InstancesCount { get; }
      
        public abstract IEnumerator<T> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public virtual bool CanSpawn_ByPerformance()
        {
            if (!IsSingletonActive) //!gameObject.activeInHierarchy)
                return false;

            if (GetCount() >= RECOMMENDED_INSTANCES)
                return false;

            return true;
        }

        //public override bool IsSingletonActive { get => base.IsSingletonActive && CanSpawn_ByPerformance(); set => base.IsSingletonActive = value; }

        protected virtual bool CanSpawn_AtPosition(Vector3 position) => Camera.main.IsInCameraViewArea(position);

        public bool CanSpawn(Vector3 pos)
        {
            if (!CanSpawn_AtPosition(pos))
                return false;

            return CanSpawn_ByPerformance();
        }


        protected abstract void Spawn_Internal(Vector3 worldPosition, out T inst);


        public bool TrySpawnIfVisible(Vector3 worldPosition, Action<T> onInstanciate = null)
        {
            if (!CanSpawn(worldPosition))
                return false;

            Spawn_Internal(worldPosition, out var inst);

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

        public bool TrySpawnIfVisible(Vector3 worldPosition, out T inst)
        {
            inst = null;

            if (!CanSpawn(worldPosition))
                return false;

            Spawn_Internal(worldPosition, out inst);

            return true;
        }

        public bool TrySpawn(Vector3 worldPosition = default, Action<T> onInstanciate = null)
        {
            if (!CanSpawn_ByPerformance())
                return false;

            Spawn_Internal(worldPosition, out var inst);

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

        public bool TrySpawn(Vector3 worldPosition, out T inst)
        {
            if (!CanSpawn_ByPerformance())
            {
                inst = null;
                return false;
            }

            Spawn_Internal(worldPosition, out inst);

            return true;
        }

        public T SpawnNow(Vector3 worldPosition) 
        {
            Spawn_Internal(worldPosition, out var inst);
            return inst;
        }

        protected void ClearAll() 
        {
            if (!_clearAllLock.Unlocked)
                return;

            using (_clearAllLock.Lock())
            {
                ClearAll_Internal();
            }
        }

        protected abstract void ClearAll_Internal();

        protected abstract bool ReturnToPool_Internal(T effect);

        public bool ReturnToPool(T effect)
        {
            if (!_clearAllLock.Unlocked)
            {
                effect.gameObject.DestroyWhatever();
                return true;
            }

            effect.gameObject.SetActive(false);

            return ReturnToPool_Internal(effect);
        }


        protected override void OnBeforeOnDisableOrEnterPlayMode(bool _afterEnableCalled)
        {
            base.OnBeforeOnDisableOrEnterPlayMode(_afterEnableCalled);
            ClearAll();
        }

        #region Inspector

        public virtual int GetCount() => InstancesCount;

        public override string InspectedCategory => Singleton.Categories.POOL;

        public override void InspectInList(ref int edited, int ind)
        {
            base.InspectInList(ref edited, ind);

            if (InstancesCount > 0)
            {
                Icon.Clear.Click().OnChanged(ClearAll);

                "{0}/{1}".F(InstancesCount, MAX_INSTANCES).PegiLabel(60).Write();
            }
        }

        #endregion

        protected override void OnRegisterServiceInterfaces()
        {
            base.OnRegisterServiceInterfaces();
            RegisterServiceAs<PoolBehaviourCore<T>>();
        }
    }


    public abstract class PoolSingleton_Sorted<T> : PoolBehaviourCore<T> where T : Component
    {
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

        protected Stack<T> pool = new();
        protected List<T> instances = new();

        public override int InstancesCount => instances.Count;

        public int TotalCount => instances.Count + pool.Count;

        public override bool CanSpawn_ByPerformance()
        {
            if (!base.CanSpawn_ByPerformance())
                return false;

            if (prefabs.Count == 0)
            {
                QcLog.ChillLogger.LogErrorOnce("No Prefabs", key: "noSmokePrefab", this);
                return false;
            }

            return true;
        }

        public bool TryIterate_Randomly(ref int index, out T current)
        {
            int cnt = instances.Count;

            if (cnt == 0)
            {
                current = null;
                return false;
            }

            index = (index + Mathf.FloorToInt(cnt / 10) + 1) % cnt;
            current = instances[index];
            return true;
        }

        public bool TryGetNearest(Vector3 myPosition, out T data)
        {
            float nearest = float.MaxValue;
            data = null;

            foreach (var m in this)
            {
                var dist = Vector3.Distance(myPosition, m.transform.position);
                if (dist < nearest)
                {
                    nearest = dist;
                    data = m;
                }
            }

            return data;
        }

        protected override bool ReturnToPool_Internal(T effect) 
        {
            if (!instances.Remove(effect))
                return false;

            if (DisablePooling)
            {
                effect.gameObject.DestroyWhatever();
                return true;
            }

            pool.Push(effect);

            return true;
        }

        protected override void Spawn_Internal(Vector3 worldPosition, out T inst)
        {
            if (pool.Count > 0)
            {
                inst = pool.Pop();
                instances.Add(inst);
                inst.transform.position = worldPosition;
                inst.gameObject.SetActive(true);
            }
            else
            {
                if (prefabs.Count == 0) 
                {
                    QcLog.ChillLogger.LogErrorOnce("No prefabs assigned", "oPfbs", this);
                    inst = null;
                    return;
                }

                using (QcDebug.TimeProfiler.Instance["Pool Instancers"].Sum(typeof(T).ToPegiStringType()).Start())
                {
                    lastInstancePrefab = (lastInstancePrefab + 1) % prefabs.Count;
                    inst = Instantiate(prefabs[lastInstancePrefab], worldPosition, Quaternion.identity, transform);
                }

                instances.Add(inst);

                inst.name += "({0})".F(TotalCount);
            }

            OnInstanciated(inst);
        }

        protected virtual void OnInstanciated(T inst) { }

        protected override void ClearAll_Internal()
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

        private readonly pegi.CollectionInspectorMeta _active = new(labelName: "Active Instances", showAddButton: false, showCopyPasteOptions: false, showEditListButton: false);

        private readonly pegi.TabContext _tab = new();
        public override void Inspect()
        {
            "Pool of {0}".F(typeof(T).Name).PegiLabel(pegi.Styles.ListLabel).Write();

            if (Application.isPlaying && Icon.Play.Click())
                TrySpawn(Vector3.zero, out _);

            if (pool.Count > 0 || instances.Count > 0)
                Icon.Delete.Click().OnChanged(ClearAll);

            pegi.Nl();

            bool isPooling = !DisablePooling;
            "Pooling".PegiLabel().ToggleIcon(ref isPooling).OnChanged(() => DisablePooling = !isPooling).Nl();

            using (_tab.StartContext())
            {
                pegi.AddTab("Prefabs", ()=>
                {
                    "Prefabs".PegiLabel(60).Edit_List_UObj(prefabs).Nl();
                    "Capacity: {0}/{1}".F(pool.Count + instances.Count, MAX_INSTANCES).PegiLabel().Nl();
                });

                pegi.AddTab("Instance", ()=> _active.Edit_List(instances).Nl());
            }
        }

        #endregion

        public override IEnumerator<T> GetEnumerator()
        {
            for (int i = instances.Count - 1; i >= 0; i--)
            {
                yield return instances[i];
            }
        }
    }


    public abstract class PoolSingleton_Indexed<T> : PoolBehaviourCore<T> where T : PoolableElement
    {
        private readonly List<T> allInstances = new();

        private int _activeInstancesCount;
        private int _firstInactiveIndex;
        private int _maxActiveIndex = -1;

        public override int InstancesCount => _activeInstancesCount;

        #region Iteration

        public override IEnumerator<T> GetEnumerator() => new PoolIterator(this);

        private class PoolIterator : IEnumerator<T>
        {
            private readonly PoolSingleton_Indexed<T> _pool;
            private int index = -1;

            public T Current => _pool.allInstances[index];

            object IEnumerator.Current => _pool.allInstances[index];

            public void Dispose()
            {
                index = -1;
            }

            public bool MoveNext()
            {
                index++;

                try
                {
                    while (index <= _pool._maxActiveIndex && (!_pool.allInstances[index] || !_pool.allInstances[index].IsActive))
                    {
                        index++;
                    }
                } catch 
                {
                    Debug.LogError("Getting {0}, maxActive - {1}, Count: {2}".F(index, _pool._maxActiveIndex, _pool.allInstances.Count));
                }

                return index <= _pool._maxActiveIndex;//_pool.allInstances.Count;

            }

            public void Reset()
            {
                index = -1;
            }

            public PoolIterator(PoolSingleton_Indexed<T> pool)
            {
                this._pool = pool;
                index = -1;
            }
        }

        public bool TryIterate(ref int index, out T current)
        {
            int cnt = _activeInstancesCount;

            if (cnt == 0)
            {
                current = null;
                return false;
            }

            //int toSkip = Mathf.FloorToInt(cnt / 10) + 1;

            int maxSteps = 5;

            //index = (index + ) % cnt;
            do
            {
                maxSteps--;
                index++;
            } while (index < _maxActiveIndex && !allInstances[index].IsActive && maxSteps > 0);

            if (index>= _maxActiveIndex)
            {
                index = -1;
                current = null;
                return false;
            }

            current = allInstances[index];
            return current && current.IsActive;
        }

        #endregion

        protected override void ClearAll_Internal()
        {
            allInstances.DestroyAndClear();
            _activeInstancesCount = 0;
            _maxActiveIndex = -1;
        }

        protected override bool ReturnToPool_Internal(T element)
        {
            if (!element || !element.IsActive)
                return false;

            if (_maxActiveIndex == element.PoolIndex) 
            {
                _maxActiveIndex--;
                while (_maxActiveIndex >= 0 && !allInstances[_maxActiveIndex].IsActive)
                    _maxActiveIndex--;
            }

            _activeInstancesCount--;
            _firstInactiveIndex = Mathf.Min(_firstInactiveIndex, element.PoolIndex);
            element.PoolIndex = -1;
            element.gameObject.SetActive(false);

            return true;
        }

        protected override void Spawn_Internal(Vector3 worldPosition, out T inst)
        {
            while (_firstInactiveIndex < allInstances.Count && allInstances[_firstInactiveIndex].IsActive)
                _firstInactiveIndex++;
            
            if (_firstInactiveIndex < allInstances.Count)
            {
                inst = allInstances[_firstInactiveIndex];
                if (!inst)
                    InstanciateNew(out inst);

                inst.transform.position = worldPosition;
                ProcessInstance(inst, index: _firstInactiveIndex);
                _firstInactiveIndex++;
                return;
            }

            InstanciateNew(out inst);

         
            allInstances.Add(inst);

            ProcessInstance(inst, index: allInstances.Count-1);

            return;

            void InstanciateNew(out T inst)
            {
#if UNITY_EDITOR
                using (QcDebug.TimeProfiler.Instance["Pool Instancers"].Sum(typeof(T).ToPegiStringType()).Start())
                {
#endif
                    lastInstancePrefab = (lastInstancePrefab + 1) % prefabs.Count;
                    inst = Instantiate(prefabs[lastInstancePrefab], worldPosition, Quaternion.identity, transform);
#if UNITY_EDITOR
                }
#endif
            }

            void ProcessInstance(T newInst, int index) 
            {
                newInst.PoolIndex = index;
                _activeInstancesCount++;
                newInst.gameObject.SetActive(true);
                try
                {
                    OnInstanciated(newInst);
                } catch (Exception ex) 
                {
                    Debug.LogException(ex);
                }

                _maxActiveIndex = Mathf.Max(_maxActiveIndex, index);
            }
        }

        protected virtual void OnInstanciated(T inst) { }

        #region Inspector

        private readonly pegi.CollectionInspectorMeta _prefabsMeta = new("Prefabs");
        private readonly pegi.CollectionInspectorMeta _instancesMeta = new("Instances");

        private readonly pegi.TabContext _tab = new();

        public override void Inspect()
        {
            base.Inspect();

            if (InstancesCount > 0)
                Icon.Delete.Click().OnChanged(ClearAll);

            "Pool of {0}".F(typeof(T).Name).PegiLabel(pegi.Styles.ListLabel).Write();

            pegi.Nl();

            using (_tab.StartContext())
            {
                pegi.AddTab("Prefabs", ()=>
                {
                    _prefabsMeta.Edit_List(prefabs).Nl();
                    if (Application.isPlaying)
                        "Capacity: {0}/{1} ({2})".F(InstancesCount, RECOMMENDED_INSTANCES, MAX_INSTANCES).PegiLabel().Nl();
                });

                pegi.AddTab("Instance", ()=>
                {
                    _instancesMeta.Edit_List(allInstances).Nl();
                });

                pegi.AddTab("Debug", ()=>
                {
                    if (Application.isPlaying && Icon.Play.Click())
                        TrySpawn(Vector3.zero, out _);

                    "First Inactive:{0}".F(_firstInactiveIndex).PegiLabel().Nl();
                    "Last Active: {0}".F(_maxActiveIndex).PegiLabel().Nl();
                });
            }
        }

        #endregion

    }

    public abstract class PoolableElement : MonoBehaviour
    {
        public int PoolIndex = -1;
        public bool IsActive => PoolIndex >= 0;

        public override string ToString() => IsActive ? "Active [{0}]".F(PoolIndex) : "Inactive";
    }
}