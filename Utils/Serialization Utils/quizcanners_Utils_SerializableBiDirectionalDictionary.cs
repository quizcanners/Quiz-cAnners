using QuizCanners.Inspect;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QuizCanners.Utils
{
    [System.Serializable]
    public class SerializableBiDirectionalDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IPEGI
    {
        [SerializeField] private SerializableDictionary<TKey, TValue> _dictionary = new();
        [SerializeField] private SerializableDictionary<TValue, TKey> _inverseDictionary = new();

        public ICollection<TKey> Keys => _dictionary.Keys;

        public ICollection<TValue> Values => _dictionary.Values;

        public int Count => _dictionary.Count;

        public bool IsReadOnly => false;

        public TValue this[TKey key] 
        {
            get => _dictionary[key];
            set => _dictionary[key] = value;
        } 

        public bool TryGetValue(TKey key, out TValue value) => _dictionary.TryGetValue(key, out value);
        
        public bool TryGetKey(TValue value, out TKey key) => _inverseDictionary.TryGetValue(value, out key);
        
        #region Inspector
        public override string ToString() => _dictionary.ToString();

        public void Inspect()
        {
            _dictionary.Nested_Inspect().Nl();
        }

        #endregion

        public bool Remove(TKey key)
        {
            if (!_dictionary.TryGetValue(key, out var value))
                return false;

            _dictionary.Remove(key);
            _inverseDictionary.Remove(value);
            return true;
        }

        public bool RemoveByValue(TValue value)
        {
            if (_inverseDictionary.TryGetValue(value, out var key))
            {
                _inverseDictionary.Remove(value);
                _dictionary.Remove(key);
                return true;
            }
            return false;
        }

        public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

        public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

        public void Add(TKey key, TValue value)
        {
            if (_dictionary.ContainsKey(key)) 
            {
                Debug.LogError("Duplicate of "+key);
                return;
            }

            if (_inverseDictionary.ContainsKey(value))
            {
                Debug.LogError("Duplicate of " + value);
                return;
            }

            _dictionary[key] = value;
            _inverseDictionary[value] = key;
        }

        public void Clear()
        {
            _dictionary.Clear();
            _inverseDictionary.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item) => _dictionary.Contains(item);

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new System.NotImplementedException();
        }

        public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dictionary.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _dictionary.GetEnumerator();
    }
}
