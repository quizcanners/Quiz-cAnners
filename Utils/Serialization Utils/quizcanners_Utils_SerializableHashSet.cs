using QuizCanners.Inspect;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Utils
{
    [System.Serializable]
    public abstract class SerializableHashSet<T> : ISerializationCallbackReceiver, IPEGI, ICollection<T>, IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>, ISet<T>
    {
        [SerializeField] private List<T> _values = new List<T>();
        [NonSerialized] private readonly HashSet<T> _set = new HashSet<T>();

        public void OnBeforeSerialize()
        {
            _values.Clear();
            foreach (var val in _set)
                _values.Add(val);
        }

        public void OnAfterDeserialize()
        {
            _set.Clear();
            foreach (var val in _values)
            {
                _set.Add(val);
            }
        }

        protected int inspectedElement = -1;

        public int Count => _set.Count;

        public bool IsReadOnly => false;

        public virtual void Inspect()
        {
            pegi.edit_HashSet(_set, ref inspectedElement, out _);
        }

        public void Add(T item) => _set.Add(item);

        public void Clear() => _set.Clear();

        public bool Contains(T item) => _set.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => _set.CopyTo(array, arrayIndex);

        public bool Remove(T item) => _set.Remove(item);

        public IEnumerator<T> GetEnumerator() => _set.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _set.GetEnumerator();

        bool ISet<T>.Add(T item) => _set.Add(item);

        public void ExceptWith(IEnumerable<T> other) => _set.ExceptWith(other);

        public void IntersectWith(IEnumerable<T> other) => _set.IntersectWith(other);

        public bool IsProperSubsetOf(IEnumerable<T> other) => _set.IsProperSubsetOf(other);

        public bool IsProperSupersetOf(IEnumerable<T> other) => _set.IsProperSupersetOf(other);

        public bool IsSubsetOf(IEnumerable<T> other) => _set.IsSubsetOf(other);

        public bool IsSupersetOf(IEnumerable<T> other) => _set.IsSupersetOf(other);

        public bool Overlaps(IEnumerable<T> other) => _set.Overlaps(other);

        public bool SetEquals(IEnumerable<T> other) => _set.SetEquals(other);

        public void SymmetricExceptWith(IEnumerable<T> other) => _set.SymmetricExceptWith(other);

        public void UnionWith(IEnumerable<T> other) => _set.UnionWith(other);
    }

    [Serializable]
    public abstract class SerializableHashSetForEnum<T> : SerializableHashSet<T>
    {

        private string search = "";
        public override void Inspect()
        {
            var vals = (T[])System.Enum.GetValues(typeof(T));

            "Search".PegiLabel(70).edit(ref search).nl();

            foreach (var val in vals)
            {
                string name = val.GetNameForInspector();

                if (search.Length > 0 && !name.Contains(search))
                    continue;

                var isOn = Contains(val);
                if (pegi.toggle(ref isOn))
                {
                    if (isOn)
                        Add(val);
                    else
                        Remove(val);
                }

                name.GetNameForInspector().PegiLabel().nl();
            }
        }
    }
}