using QuizCanners.Migration;
using QuizCanners.Inspect;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Utils
{
    public abstract class SerializableDictionaryForTaggedTypesEnum<T> : Dictionary<string, T>, ISerializationCallbackReceiver, IPEGI where T : IGotClassTag
    {
        protected enum SerializationMode { Json = 0, ICfg = 1 }

        [HideInInspector] [SerializeField] protected List<string> keys = new List<string>();
        [HideInInspector] [SerializeField] protected List<string> values = new List<string>();
        [HideInInspector] [SerializeField] protected List<SerializationMode> modes = new List<SerializationMode>();

        protected TaggedTypes.DerrivedList Cfg => TaggedTypes<T>.DerrivedList;

        public G GetOrCreate<G>() where G: T , new()
        {
            if (TryGet(out G tmp))
                return tmp;

            G tmpG = new G();

            this[Cfg.GetTag(typeof(G))] = tmpG;

            return tmpG;
        }

        public bool TryGet<G>(out G value) where G : T, new()
        {
            var tag = Cfg.GetTag(typeof(G));

            var result = TryGetValue(tag, out T tmp);

            value = (G)tmp;

            return result;
        }

        #region Serialization
        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();
            modes.Clear();

            foreach (var pair in this)
            {
                string serializedValue;
                var instance = pair.Value;
                var icfg = instance as ICfg;

                var mode = icfg != null ? SerializationMode.ICfg : SerializationMode.Json;

                try
                {
                    switch (mode)
                    {
                        case SerializationMode.ICfg: serializedValue = icfg.Encode().ToString(); break;
                        case SerializationMode.Json:
                        default: serializedValue = JsonUtility.ToJson(instance); break;
                    }
                } catch (Exception ex) 
                {
                    Debug.LogException(ex);
                    continue;
                }

                keys.Add(instance.ClassTag);
                modes.Add(mode);
                values.Add(serializedValue);
            }
        }

        public void OnAfterDeserialize()
        {
            Clear();

            if (keys.Count != values.Count)
            {
                Debug.LogError(
                    "there are {0} keys and {1} values after deserialization. Make sure that both key and value types are serializable."
                    .F(keys.Count, values.Count));
            }
            else
            {
                var types = Cfg.TaggedTypes;

                if (types.Count < 1)
                {
                    Debug.LogError("Found no Tagged Types derrived from {0} ".F(typeof(T).ToPegiStringType()));
                }
                else
                {
                    for (int i = 0; i < keys.Count; i++)
                    {
                        string key = keys[i];
                        SerializationMode mode = modes.TryGet(i, defaultValue: SerializationMode.Json);
                        Type type = types.TryGet(keys[i]);

                        if (type == null)
                        {
                            type = types.GetElementAt(0).Value;

                            Debug.LogError("Could not find a class derived from {0} for Tag {1}. Using Default ({2})".F(typeof(T).ToString(), key, type.ToPegiStringType()));
                        }

                        try 
                        {
                            T tmp;
                            switch (mode) 
                            {
                                case SerializationMode.ICfg: 
                                    tmp = (T)Activator.CreateInstance(type);

                                    if (tmp is ICfg icfg)
                                    {
                                        icfg.Decode(new CfgData(values[i]));
                                        tmp = (T)icfg;
                                    }
                                    else Debug.LogError("{0} is not ICfg".F(type));
                                    break;
                                case SerializationMode.Json:
                                default: tmp = (T)JsonUtility.FromJson(values[i], type);  break;
                            }

                            Add(keys[i], tmp);
                        } catch (Exception ex) 
                        {
                            Debug.LogException(ex);
                        }


                       
                    }
                }
            }

            keys.Clear();
            modes.Clear();
            values.Clear();
        }

        #endregion

        #region Inspector

        private int _inspected = -1;

        private string _selectedTag = "_";
        void IPEGI.Inspect()
        {
            ToString().PegiLabel().Edit_Dictionary(this, ref _inspected).Nl();

            if (_inspected == -1) 
            {
                "Tag".PegiLabel().Select(ref _selectedTag, Cfg.DisplayNames).Nl();

                var type = Cfg.TaggedTypes.TryGet(_selectedTag);

                if (type != null)
                {
                    if (ContainsKey(_selectedTag))
                        "Type {0} is already in the Dictionary".F(type).PegiLabel().Write_Hint();
                    else if ("Create {0}".F(type.ToPegiStringType()).PegiLabel().Click())
                        this[_selectedTag] = (T)Activator.CreateInstance(type);
                }

                pegi.Nl();
            }
        }

        public override string ToString()
        {
            var tmp = typeof(T).ToString();

            var parts = tmp.Split('.', '+');

            if (parts.Length > 1)
                return parts[parts.Length-2];

            return tmp;
        }

        #endregion
    }
}
