using System;
using System.Collections;
using System.Collections.Generic;
using QuizCanners.Inspect;
using QuizCanners.Utils;

namespace QuizCanners.Migration 
{

    #pragma warning disable IDE0019 // Use pattern matching
    #pragma warning disable IDE0018 // Inline variable declaration

    public interface IGotClassTag {
        string ClassTag { get; }
    }

    public class TaggedTypes 
    {
        internal static int Version;
        internal static Dictionary<Type, DerrivedList> _configs = new();

        private static void RegisterType(DerrivedList cfg, Type type)
        {
            _configs[type] = cfg;
            Version++;
        }

        public static DerrivedList TryGetOrCreate(Type type)
        {
            if (!typeof(IGotClassTag).IsAssignableFrom(type))
            {
                return null;
            }

            DerrivedList cfg;

            if (_configs.TryGetValue(type, out cfg))
                return cfg;

            cfg = new DerrivedList(type);

            RegisterType(cfg, type);

            return cfg;
        }

        public class DerrivedList
        {
            public Type CoreType { get; private set; }

            public DerrivedList(Type type)
            {
                CoreType = type;
                RegisterType(this, type);
            }

            private List<string> _keys;

            //public CountlessBool _disallowMultiplePerList = new CountlessBool();
            private readonly HashSet<int> _disallowMultiplePerList = new();

            public bool CanAdd(int typeIndex, IList toList)
            {
                RefreshNodeTypesList();

                if (!_disallowMultiplePerList.Contains(typeIndex))
                    return true;

                var t = _types[typeIndex];

                foreach (var el in toList)
                    if (el != null && (el.GetType().Equals(t)))
                        return false;
                
                return true;
            }

            private List<Type> _types;

            private List<string> _displayNames;

            private Dictionary<string, Type> _dictionary;

            private DerrivedList RefreshNodeTypesList()
            {
                if (_keys != null) return this;

                _dictionary = new Dictionary<string, Type>();

                _keys = new List<string>();

                List<Type> allTypes;

                if (_types == null)
                {
                    _types = new List<Type>();
                    allTypes = CoreType.FindAllChildTypes();
                }
                else
                {
                    allTypes = _types;
                    _types = new List<Type>();
                }

                _displayNames = new List<string>();

                int cnt = 0;

                foreach (var t in allTypes)
                {
                    var att = t.TryGetClassAttribute<Tag>();

                    if (att == null)
                        continue;

                    if (_dictionary.ContainsKey(att.tag))
                        UnityEngine.Debug.LogError("Class {0} and class {1} both share the same tag {2}".F(att.displayName,
                            _dictionary[att.tag].ToString(), att.tag));
                    else
                    {
                        _dictionary.Add(att.tag, t);
                        _displayNames.Add(att.displayName);
                        _keys.Add(att.tag);
                        _types.Add(t);
                        if (!att.allowDuplicates)
                            _disallowMultiplePerList.Add(cnt);
                        cnt++;
                    }
                }

                return this;
            }

            public Dictionary<string, Type> TaggedTypes =>
                RefreshNodeTypesList()._dictionary;

            public List<string> Keys =>
                RefreshNodeTypesList()._keys;

            public List<Type> Types
            {
                get { return RefreshNodeTypesList()._types; }
                set { _types = value; }
            }

            public IEnumerator<Type> GetEnumerator()
            {
                foreach (var t in Types)
                    yield return t;
            }

            public List<string> DisplayNames => RefreshNodeTypesList()._displayNames;

            public string GetTag(Type type)
            {

                int ind = Types.IndexOf(type);
                if (ind >= 0)
                    return _keys[ind];

                return null;
            }

            #region Inspector
            public pegi.ChangesToken Inspect_Select(ref Type type)
            {
                var changed = pegi.ChangeTrackStart();

                var ind = type != null ? Types.IndexOf(type) : -1;
                if (pegi.Select(ref ind, DisplayNames))
                    type = _types[ind];

                return changed;
            }
            #endregion
        }


        [AttributeUsage(AttributeTargets.Class)]
        public class Tag : UnityEngine.Scripting.PreserveAttribute
        {
            public string tag;

            public string displayName;

            public bool allowDuplicates;

            public Tag(string tag, string displayName = null, bool allowMultiplePerList = true)
            {
                this.tag = tag;
                this.displayName = displayName ?? tag;
                this.allowDuplicates = allowMultiplePerList;
            }
        }

    }

    public static class TaggedTypes<T>
    {
        private static TaggedTypes.DerrivedList _instance;
        private static readonly Gate.Integer _versionGate = new();
        public static TaggedTypes.DerrivedList DerrivedList
        {
            get
            {
                if (_versionGate.TryChange(TaggedTypes.Version))
                {
                    _instance = TaggedTypes.TryGetOrCreate(typeof(T));
                }

                return _instance;
            }
        }
    }

    internal static class TaggedTypesExtensions 
    {
        public static void ChangeType(ref object target, Type type) 
        {
            var previousInstance = target;

            CfgData previousData = new();

            if (previousInstance is ICfg previousAsCfg)
                previousData = previousAsCfg.Encode().CfgData;

            var newInstance = Activator.CreateInstance(type);

            if (newInstance is ICfg newAsCfg)
            {
                newAsCfg.Decode(previousData);
            }

            ICfgExtensions.TryCopyIcfg(previousInstance, newInstance);

            target = newInstance;

        }

        public static void TryChangeObjectType(IList list, int index, Type type)
        {
            object previous = null;

            if (list != null && index >= 0 && index < list.Count)
                previous = list[index];
            
            var obj = previous;

            var std = (obj as ICfgCustom);

            var iTag = obj as IGotClassTag;

            if (iTag != null)
                ChangeType(ref obj, type);
            else
            {
                obj = std.TryDecodeInto<object>(type);
                ICfgExtensions.TryCopyIcfg(previous, obj);
            }

            list[index] = obj;
        }
    }
    
    public class TaggedModulesList<T> : ICfg, IPEGI, IEnumerable<T> where T : class, IGotClassTag, ICfg {
        
        protected List<T> modules = new();
        
        protected virtual List<T> Modules {
            get {

                if (initialized)
                    return modules;
                
                initialized = true;

                for (var i = modules.Count - 1; i >= 0; i--)
                    if (modules[i] == null)
                        modules.RemoveAt(i);

                if (modules.Count < all.Types.Count)
                    foreach (var t in all)
                        if (!modules.ContainsInstanceOfType(t))
                            modules.Add((T)Activator.CreateInstance(t));
                
                OnAfterInitialize();

                return modules;
            }
        }
        
        public IEnumerator<T> GetEnumerator() => Modules.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Modules.GetEnumerator();

        private bool initialized;

        public G GetModule<G>() where G : class, T {

            G returnPlug = null;

            var targetType = typeof(G);
            
            foreach (var i in Modules)
                if (i.GetType() == targetType)
                {
                    returnPlug = (G)i;
                    break;
                }

            return returnPlug;
        }
        
        #region Encode & Decode
        public static readonly TaggedTypes.DerrivedList all = TaggedTypes<T>.DerrivedList;
        
        public CfgEncoder Encode()  
            => new CfgEncoder()
            .Add("pgns", Modules, all);
        
        public void DecodeTag(string key, CfgData data) {
            switch (key) {
                case "pgns": 
                    data.ToList(out modules, all);
                    OnAfterInitialize();
                    break;
            }
        }
        #endregion

        public virtual void OnAfterInitialize() { }

        #region Inspector
        
        private readonly pegi.CollectionInspectorMeta modulesMeta = new("Modules", allowDeleting: false, showAddButton:false, allowReordering: false, showEditListButton:false);

        void IPEGI.Inspect()
        {
            modulesMeta.Edit_List(modules).Nl();

        }

        #endregion
    }
}
