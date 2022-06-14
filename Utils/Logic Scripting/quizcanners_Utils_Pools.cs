using QuizCanners.Inspect;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Utils
{
    public static partial class Pool
    {
        public abstract class Base : IGotCount
        {
            public int GetCount() => Count;

            public abstract int Count { get; }
        }
        
        public abstract class Generic<T> : Base, IPEGI, IEnumerable<T> where T : Component
        {
            protected List<T> pool = new List<T>();
            [SerializeField] protected List<T> instances = new List<T>();

            protected abstract T CreateInternal(Transform transform);

            public T this[int index] => instances.TryGet(index);

            public T GetOrCreate(int index, Transform parent)
            {
                if (instances.Count <= index)
                {
                    SetInstancesTotal(index + 1, parent);
                }

                return this[index];
            }

            public void SetInstancesTotal(int targetCount, Transform parent)
            {
                while (instances.Count > targetCount)
                {
                    ReturnToPool(instances[instances.Count - 1]);
                }

                while (instances.Count < targetCount)
                {
                    Spawn(parent);
                }
            }

            public T Spawn(Transform parent)
            {
                T inst = null;

                while (!inst && pool.Count > 0)
                {
                    inst = pool.TryTake(0);
                    if (!inst)
                        pool.RemoveAt(0);
                }

                if (inst)
                {
                    instances.Add(inst);
                    inst.gameObject.SetActive(true);
                }
                else
                {
                    inst = CreateInternal(parent);
                    instances.Add(inst);
                    inst.name += " ({0})".F(instances.Count + pool.Count);
                }

                OnInstanciated(inst);

                return inst;
            }

            public void ReturnToPool(T effect)
            {
                instances.Remove(effect);
                pool.Add(effect);
                effect.gameObject.SetActive(false);
            }

            public void ClearAll()
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

            public override int Count => instances.Count;

            protected virtual void OnInstanciated(T inst) { }


            #region Inspector

            protected int TotalCount => pool.Count + instances.Count;

            public virtual void InspectInList(ref int edited, int ind)
            {
                if (GetCount() > 0)
                {
                    Icon.Clear.Click().OnChanged(ClearAll);
                    "{0}".F(GetCount()).PegiLabel(60).Write();
                }
            }

            public virtual void Inspect()
            {
                "Active {0}".F(ToString()).PegiLabel().Edit_List(instances).Nl();

                if (Application.isPlaying)
                {
                    if (TotalCount > 0)
                    {
                        "Capacity: {0}".F(TotalCount).PegiLabel().Write();
                        Icon.Delete.Click().OnChanged(ClearAll);
                        pegi.Nl();
                    }
                }
            }


            public override string ToString() => "Pool of {0}".F(typeof(T).Name);

            #endregion

            #region Enumeration
            public IEnumerator<T> GetEnumerator() => instances.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => instances.GetEnumerator();
            #endregion
        }

        public abstract class GenericWithLimit<T> : Generic<T>, IEnumerable<T> where T : Component
        {
            public virtual int MaxInstances { get; } = 300;

            public virtual float VacancyPortion => (MaxInstances - (float)GetCount()) / MaxInstances;

            public virtual bool TrySpawnIfVisible(Vector3 worldPosition, out T inst, Transform transform)
            {
                if (Camera.main)
                {
                    var pos = Camera.main.WorldToViewportPoint(worldPosition);
                    if (pos.x < 0 || pos.x > 1 || pos.y < 0 || pos.y > 1)
                    {
                        inst = null;
                        return false;
                    }
                }

                return TrySpawn(worldPosition, out inst, transform);
            }

            public bool TrySpawn(Vector3 worldPosition, out T inst, Transform transform)
            {
                inst = null;

                while (!inst && pool.Count > 0)
                {
                    inst = pool.TryTake(0);
                    if (!inst)
                        pool.RemoveAt(0);
                }

                if (inst)
                {
                    instances.Add(inst);
                    inst.gameObject.SetActive(true);
                    inst.transform.position = worldPosition;
                }
                else
                {
                    int total = instances.Count + pool.Count;
                    if (total >= MaxInstances)
                    {
                        inst = null;
                        return false;
                    }

                    inst = CreateInternal(transform);//UnityEngine.Object.Instantiate(prefab, worldPosition, Quaternion.identity, transform);
                    inst.transform.position = worldPosition;
                    instances.Add(inst);

                    inst.name += " ({0})".F(total);
                }

                OnInstanciated(inst);

                return true;
            }
        }

        public abstract class WithPrefabAndRoot<T> : Generic<T> where T : Component
        {
            [SerializeField] protected Transform parent;
            [SerializeField] protected T prefab;
          

            public T Spawn() => Spawn(parent);

            public void SetInstancesTotal(int targetCount) => SetInstancesTotal(targetCount, parent);

            public T GetOrCreate(int index)
            {
                if (instances.Count <= index)
                {
                    SetInstancesTotal(index + 1, parent);
                }

                return this[index];
            }

            public bool TrySpawnIfVisible(Vector3 worldPosition, out T inst)
            {
                if (!prefab)
                {
                    QcLog.ChillLogger.LogErrorOnce("No Prefab", key: "noSmokePrefab", parent);
                    inst = null;
                    return false;
                }

                if (Camera.main)
                {
                    var pos = Camera.main.WorldToViewportPoint(worldPosition);
                    if (pos.x < 0 || pos.x > 1 || pos.y < 0 || pos.y > 1)
                    {
                        inst = null;
                        return false;
                    }
                }

                return TrySpawn(worldPosition, out inst);
            }

            public bool TrySpawn(Vector3 worldPosition, out T inst)
            {
                inst = null;

                while (!inst && pool.Count > 0)
                {
                    inst = pool.TryTake(0);
                    if (!inst)
                        pool.RemoveAt(0);
                }

                if (inst)
                {
                    instances.Add(inst);
                    inst.gameObject.SetActive(true);
                    inst.transform.position = worldPosition;
                }
                else
                {
                    int total = instances.Count + pool.Count;
                    inst = CreateInternal(parent);
                    inst.transform.position = worldPosition;
                    instances.Add(inst);

                    inst.name += " ({0})".F(total);
                }

                OnInstanciated(inst);

                return true;
            }


            protected override T CreateInternal(Transform transform)
            {
                using (QcDebug.TimeProfiler.Instance["Pool Instancers"].Sum(GetType().ToString()).Start())
                {
                    return UnityEngine.Object.Instantiate(prefab, transform);
                }
            }

            #region Inspector

            public override void InspectInList(ref int edited, int ind)
            {
                if (!Application.isPlaying)
                {
                    if (!parent)
                        "Parent".PegiLabel(90).Edit(ref parent);
                    else
                        "Prefab".PegiLabel(50).Edit(ref prefab);
                }

                base.InspectInList(ref edited, ind);
            }

            public override void Inspect()
            {
                base.Inspect();

                if (!Application.isPlaying)
                {
                    "Parent".PegiLabel(60).Edit(ref parent).Nl();
                    "Prefab".PegiLabel(60).Edit(ref prefab).Nl();
                }
            }

            #endregion
        }

        public class Precedural<T> : Generic<T> where T : Component
        {
            protected override T CreateInternal(Transform transform)
            {
                var go = new GameObject(typeof(T).ToPegiStringType());
                go.transform.parent = transform;
                var cmp = go.AddComponent<T>();
                return cmp;
            }
        }

        public class PreceduralWithLimits<T> : GenericWithLimit<T> where T : Component
        {
            protected override T CreateInternal(Transform transform)
            {
                var go = new GameObject(typeof(T).ToPegiStringType());
                go.transform.parent = transform;
                var cmp = go.AddComponent<T>();
                return cmp;
            }
        }
    }
}