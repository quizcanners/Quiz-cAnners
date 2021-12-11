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

        protected int lastInstancePrefab;

        protected virtual int MAX_INSTANCES => 50;

        public virtual float VacancyPortion => (MAX_INSTANCES - (float)instances.Count) / MAX_INSTANCES;

        protected static List<T> pool = new List<T>();
        protected static List<T> instances = new List<T>();

        public static int InstancesCount => instances.Count;

        public static int TotalCount => instances.Count + pool.Count;

        public bool CanSpawn() 
        {
            if (!gameObject.activeInHierarchy)
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

        public static void ReturnToPool(T effect)
        {
            instances.Remove(effect);
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
                inst.gameObject.SetActive(true);
                inst.transform.position = worldPosition;
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
            foreach (var e in pool)
                if (e)
                    e.gameObject.DestroyWhatever();
            
            pool.Clear();

            foreach (var e in instances)
                if (e)
                    e.gameObject.DestroyWhatever();

            instances.Clear();
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

        private pegi.TabContext _tab = new pegi.TabContext();
        public override void Inspect()
        {
            "Pool of {0}".F(typeof(T).Name).PegiLabel(pegi.Styles.ListLabel).Write();

            if (Icon.Play.Click())
                TrySpawn(Vector3.zero, out _);

            if (pool.Count > 0 || instances.Count > 0)
                Icon.Delete.Click().OnChanged(ClearAll);

            pegi.Nl();

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
    }
}