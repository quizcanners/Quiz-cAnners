using QuizCanners.Inspect;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Utils
{
    [System.Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver, IPEGI, IGotReadOnlyName
    {
        [HideInInspector] [SerializeField] protected List<TKey> keys = new List<TKey>();
        [HideInInspector] [SerializeField] protected List<TValue> values = new List<TValue>();

        protected virtual bool CanAdd => true;
        protected virtual string ElementName => "New "+this.GetNameForInspector();

        public virtual void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();
            foreach (var pair in this)
            {
                keys.Add(pair.Key);
                values.Add(pair.Value);
            }
        }
        public virtual void OnAfterDeserialize()
        {
            Clear();

            if (keys.Count != values.Count)
            {
                Debug.LogError(
                    "there are {0} keys ({3}) and {1} values ({4}) after deserialization in {2}. Make sure that both key and value types are serializable."
                    .F(keys.Count, values.Count, GetType().ToPegiStringType(), typeof(TKey).ToPegiStringType(), typeof(TValue).ToPegiStringType()));
            }
            else
            {
                for (int i = 0; i < keys.Count; i++)
                    Add(keys[i], values[i]);
            }

            keys.Clear();
            values.Clear();
        }

        #region Inspector
        [System.NonSerialized] protected pegi.CollectionInspectorMeta _collectionMeta;
        protected virtual pegi.CollectionInspectorMeta CollectionMeta 
        {
            get 
            {
                if (_collectionMeta == null)
                {
                    _collectionMeta = new pegi.CollectionInspectorMeta(labelName: this.GetNameForInspector().Replace("Dictionary", ""), showAddButton: CanAdd)
                    {
                        ElementName = ElementName
                    };
                }
                return _collectionMeta;
            }
        }

        public virtual void Inspect()
        {
            CollectionMeta.edit_Dictionary(this).nl();
        }

        public virtual string GetReadOnlyName() => QcSharp.AddSpacesToSentence(GetType().ToPegiStringType());
        #endregion
    }


    [System.Serializable]
    public abstract class SerializableDictionary_ForEnum<TKey, TValue> : SerializableDictionary<TKey, TValue> where TValue : new()
    {
        public virtual void Create(TKey key)
        {
            this[key] = new TValue();
        }

        protected virtual void InspectElementInList(TKey key, int index) 
        {
            string name = key.ToString().SimplifyTypeName();

            var value = this.TryGet(key);

            if (value == null)
            {
                if ("Create {0}".F(name).PegiLabel().Click())
                    Create(key);
            }
            else
            {
                if (value is IPEGI_ListInspect pgi)
                {
                    if (name.PegiLabel("Click to Copy to Clipboard", width: 90).ClickLabel())
                        pegi.SetCopyPasteBuffer(name);
                    var change = pegi.ChangeTrackStart();

                    pgi.InspectInList(ref CollectionMeta.inspectedElement, index);
                    if (change)
                    {
                        this[key] = (TValue)pgi;
                    }
                }
                else
                {
                    name.PegiLabel().try_enter_Inspect(value, ref CollectionMeta.inspectedElement, index);
                }
            }
        }

        protected virtual void InspectElement(TKey key)
        {
            TValue element = this.TryGet(key);

            if (element == null)
            {
                "NULL".PegiLabel().write();
                return;
            }

            if (element is IPEGI pgi)
                pgi.Nested_Inspect();
            else
                pegi.TryDefaultInspect(element as UnityEngine.Object);
        }

        public override void Inspect()
        {
            var type = typeof(TKey);

            type.ToString().PegiLabel(style: pegi.Styles.ListLabel).nl();

            TKey[] Keys = (TKey[])System.Enum.GetValues(typeof(TKey));

            if (CollectionMeta.IsInspectingElement)
            {
                var key = Keys[CollectionMeta.inspectedElement];
                if (key.ToString().SimplifyTypeName().PegiLabel().isEntered(ref CollectionMeta.inspectedElement, CollectionMeta.inspectedElement).nl())
                {
                    InspectElement(key);
                    pegi.nl();
                }
            }
            else
            {
                for (int i = 0; i < Keys.Length; i++)
                {
                    InspectElementInList(Keys[i], i);
                    pegi.nl();
                }
            }

        }
    }
}
