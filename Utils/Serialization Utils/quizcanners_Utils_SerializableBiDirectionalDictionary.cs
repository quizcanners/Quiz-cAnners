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

        private static readonly EqualityComparer<TKey> KeyComparer = EqualityComparer<TKey>.Default;
        private static readonly EqualityComparer<TValue> ValueComparer = EqualityComparer<TValue>.Default;

        public ICollection<TKey> Keys => _dictionary.Keys;

        public ICollection<TValue> Values => _dictionary.Values;

        public int Count => _dictionary.Count;

        public bool IsReadOnly => false;

        public TValue this[TKey key] 
        {
            get => _dictionary[key];
            set
            {
                Set(key, value);
            }
        } 

        public bool TryGetValue(TKey key, out TValue value) => _dictionary.TryGetValue(key, out value);
        
        public bool TryGetKey(TValue value, out TKey key)
        {
           // EnsureInverseDictionaryIsValid();
            return _inverseDictionary.TryGetValue(value, out key);
        }
        
        #region Inspector
        public override string ToString() => _dictionary.ToString();

        public void Inspect()
        {
            _dictionary.Nested_Inspect().NL();

            if ("Check validity".PL().Click() && !IsInverseDictionaryValid())
            {
                Debug.LogError("Dictionaries out of sync!"); //.PL(pegi.Styles.Text.Warning);
              //  if ("Repair".PL().Click())
                    RebuildInverseDictionary();
            }

            return;

            bool IsInverseDictionaryValid()
            {
                if (_dictionary.Count != _inverseDictionary.Count)
                    return false;

                foreach (var pair in _dictionary)
                {
                    if (!_inverseDictionary.TryGetValue(pair.Value, out var key) || !KeyComparer.Equals(key, pair.Key))
                        return false;
                }

                return true;
            }

            void RebuildInverseDictionary()
            {
                _inverseDictionary.Clear();

                foreach (var pair in _dictionary)
                {
                    if (_inverseDictionary.TryGetValue(pair.Value, out var existingKey))
                    {
                        Debug.LogError("Duplicate value {0} found for keys {1} and {2}".F(pair.Value, existingKey, pair.Key));
                        continue;
                    }

                    _inverseDictionary[pair.Value] = pair.Key;
                }
            }

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
           // EnsureInverseDictionaryIsValid();

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
                Debug.LogError("Duplicate of " + key);
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

        public bool Contains(KeyValuePair<TKey, TValue> item) =>
            _dictionary.TryGetValue(item.Key, out var value) && ValueComparer.Equals(value, item.Value);

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (array == null)
                throw new System.ArgumentNullException(nameof(array));

            if (arrayIndex < 0 || arrayIndex > array.Length)
                throw new System.ArgumentOutOfRangeException(nameof(arrayIndex));

            if (array.Length - arrayIndex < Count)
                throw new System.ArgumentException("The number of elements in the source collection is greater than the available space from arrayIndex to the end of the destination array.");

            foreach (var kvp in _dictionary)
            {
                array[arrayIndex++] = kvp;
            }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (!Contains(item))
                return false;

            return Remove(item.Key);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dictionary.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _dictionary.GetEnumerator();

        private void Set(TKey key, TValue value)
        {
            if (_dictionary.TryGetValue(key, out var previousValue))
            {
                if (ValueComparer.Equals(previousValue, value))
                {
                    _inverseDictionary[value] = key;
                    return;
                }

                _inverseDictionary.Remove(previousValue);
            }

            if (_inverseDictionary.TryGetValue(value, out var previousKey) && !KeyComparer.Equals(previousKey, key))
                _dictionary.Remove(previousKey);

            _dictionary[key] = value;
            _inverseDictionary[value] = key;
        }


    }
}
