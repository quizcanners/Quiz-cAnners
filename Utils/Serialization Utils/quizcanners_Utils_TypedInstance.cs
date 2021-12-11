using QuizCanners.Inspect;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using UnityEngine;

namespace QuizCanners.Utils
{
    public class TypedInstance
    {
        public abstract class JsonSerializable<T> : ISerializationCallbackReceiver, IPEGI, IPEGI_ListInspect where T : class
        {
            [SerializeField] protected string Type;
            [SerializeField] protected string Data;
            [SerializeField] private bool _debugJson;

            [NonSerialized] private T _decoded;
            [NonSerialized] private bool _isDecoded;

            public T Decoded
            {
                get
                {
                    if (!_isDecoded)
                        FromJson();

                    return _decoded;
                }
                set
                {
                    InstanceType = value.GetType();
                    _decoded = value;
                    _isDecoded = true;
                }
            }

            public bool TryGetDecoded(out T value)
            {
                value = Decoded;
                return value != null;
            }

            protected Type InstanceType
            {
                get
                {
                    if (!_isDecoded || _decoded == null)
                    {
                        return CachedTypes.Get(Type);
                    }
                    else
                        return _decoded.GetType();
                }
                set => Type = value.ToPegiStringType();
            }

            protected void FromJson()
            {
                try
                {
                    var type = InstanceType;

                    if (type == null)
                    {
                        Debug.LogError("Target Type {0} not found".F(this.Type));
                        return;
                    }

                    _isDecoded = true;

                    _decoded = Activator.CreateInstance(type) as T;

                    if (_decoded == null)
                    {
                        Debug.LogError("Failed to Create Instance of {0}".F(type));
                        return;
                    }

                    JsonUtility.FromJsonOverwrite(Data, _decoded);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            protected void ToJson()
            {
                if (_isDecoded && _decoded != null)
                {
                    try
                    {
                        Data = JsonUtility.ToJson(_decoded);
                        InstanceType = _decoded.GetType();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }
            }

            #region Inspector

           

            public void Inspect()
            {
                Type current = InstanceType;

                if (Icon.Debug.Click())
                    _debugJson = !_debugJson;

                if (_debugJson)
                {
                    Icon.Save.Click().OnChanged(() =>
                    {
                        ToJson();
                        _isDecoded = false;
                    }).Nl();

                    "Type".PegiLabel(40).Select(ref current, PossibleTypes).OnChanged(() =>
                    {
                        ToJson();
                        InstanceType = current;
                        _isDecoded = false;
                        FromJson();
                    }).Nl();

                    "Json Data".PegiLabel().EditBig(ref Data).Nl().OnChanged(FromJson);
                }

                if (TryGetDecoded(out var value)) //_isDecoded && _decoded != null)
                    pegi.Try_Nested_Inspect(value);
                else
                    "Couldn't load".PegiLabel().WriteWarning().Nl();
            }

            public void InspectInList(ref int edited, int index)
            {
                bool enterHandled = false;

                if (TryGetDecoded(out var value))
                {
                    var asIp = _decoded as IPEGI_ListInspect;
                    if (asIp != null)
                    {
                        asIp.InspectInList(ref edited, index);
                        enterHandled = true;
                    }
                    else
                    if (_decoded.GetNameForInspector().PegiLabel().ClickLabel())
                        edited = index;
                }
                else
                {
                    bool matched = CachedTypes.TryGet(Type, out var current);

                    if (Type.IsNullOrEmpty() || !matched)
                    {
                        (matched ? "Type".PegiLabel(40) : Type.PegiLabel(90)).Select(ref current, PossibleTypes).OnChanged(() => InstanceType = current);
                    }
                    else
                    {
                        Icon.Load.Click().OnChanged(FromJson);
                        if (Type.PegiLabel().ClickLabel())
                            edited = index;
                    }
                }

                if (!enterHandled && Icon.Enter.Click())
                    edited = index;
            }

            #endregion

            public void OnAfterDeserialize()
            {
                _isDecoded = false;
            }

            public void OnBeforeSerialize()
            {
                ToJson();
            }

            protected List<Type> PossibleTypes => QcSharp.GetTypesAssignableFrom<T>();

            internal static class CachedTypes
            {
                public static Dictionary<string, Type> byKey;

                public static Type Get(string key)
                {
                    TryGet(key, out var type);
                    return type;
                }

                public static bool TryGet(string key, out Type type)
                {
                    if (key.IsNullOrEmpty())
                    {
                        type = null;
                        return false;
                    }

                    if (byKey == null)
                    {
                        byKey = new Dictionary<string, Type>();
                        var allTypes = QcSharp.GetTypesAssignableFrom<T>();
                        foreach (var t in allTypes)
                            byKey[t.ToPegiStringType()] = t;
                    }

                    return byKey.TryGetValue(key, out type);
                }

            }
        }

        public abstract class Simple<T> : IPEGI_ListInspect, IPEGI where T : class
        {
            protected bool isCreated;
            protected T instance;

            public void Clear() 
            {
                isCreated = false;
                instance = null;
            }

            protected virtual T Instanciate(string type)
            {
                var instanceType = QcSharp.GetTypesAssignableFrom<T>().FirstOrDefault(t => t.Name.Equals(type));
                if (instanceType != null)
                {
                    return Activator.CreateInstance(instanceType) as T;
                }
                return null;
            }

            public virtual T GetInstance(string type)
            {
                if (!isCreated)
                {
                    isCreated = true;
                    InstanciateNew();


                } else if (instance != null && !instance.GetType().Name.Equals(type)) 
                {
                    InstanciateNew();
                }

                void InstanciateNew() 
                {
                    try
                    {
                        instance = Instanciate(type);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }

                return instance;
            }

            public void SelectType(ref string typeName) 
            {
                var all = QcSharp.GetTypesAssignableFrom<T>();

                var unrefType = typeName;

                var type = all.FirstOrDefault(t => t.Name.Equals(unrefType));

                if (pegi.Select(ref type, all))
                {
                    typeName = type.Name;
                    Clear();
                    GetInstance(typeName);
                }
            }

            public void InspectInList(ref int edited, int index)
            {
                var pgiList = instance as IPEGI_ListInspect;

                if (pgiList != null)
                    pgiList.Enter_Inspect_AsList(ref edited, index);
                else
                {
                    (instance == null ? "NULL" : instance.GetNameForInspector()).PegiLabel().Write();

                    if (Icon.Enter.Click())
                        edited = index;
                }
            }

            public void Inspect()
            {
                var pgi = instance as IPEGI;

                if (pgi != null)
                    pgi.Nested_Inspect();
            }
        }
    }
}