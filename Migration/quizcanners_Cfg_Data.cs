using System;
using System.Collections.Generic;
using System.Globalization;
using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QuizCanners.Migration
{

    #region Interfaces

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0018 // Inline variable declaration

    public interface ICfgDecode
    {
        void DecodeTag(string key, CfgData data);
    }
    
    public interface ICfg : ICfgDecode
    {
        CfgEncoder Encode();
    }

    public interface ICfgCustom : ICfg
    {
        void DecodeInternal(CfgData data);
    }
    
    public interface ICanBeDefaultCfg: ICfg {
        bool IsDefault { get; }
    }

    public interface ITaggedCfg : ICfg
    {
        string TagForConfig { get; }
    }

    #endregion

    #region Config

    [Serializable]
    public struct CfgData : IPEGI
    {
        [HideInInspector] [SerializeField] private string _value;

        public override readonly string ToString() => _value;

        public readonly bool IsEmpty => _value.IsNullOrEmpty();

        public void Clear() => _value = null;

        public CfgData(string val)
        {
            _value = val;
        }
        
        void IPEGI.Inspect()
        {
            pegi.CopyPaste.InspectOptionsFor(ref this);

            if (_value != null)
                "{0} characters".PL().Write();
        }

        private readonly int ToIntInternal(string text)
        {
            int variable;
            int.TryParse(text, out variable);
            return variable;
        }

        private readonly int ToIntFromTextSafe(string text, int defaultReturn)
        {
            int res;
            return int.TryParse(text, out res) ? res : defaultReturn;
        }

        #region Decoding Base Values

        public readonly BoneWeight ToBoneWeight()
        {
            var cody = new CfgDecoder(_value);
            var b = new BoneWeight();

            foreach (var t in cody)
            {
                var d = cody.GetData();
                switch (t)
                {
                    case "i0": b.boneIndex0 = d.ToInt(); break;
                    case "w0": b.weight0 = d.ToFloat(); break;

                    case "i1": b.boneIndex1 = d.ToInt(); break;
                    case "w1": b.weight1 = d.ToFloat(); break;

                    case "i2": b.boneIndex2 = d.ToInt(); break;
                    case "w2": b.weight2 = d.ToFloat(); break;

                    case "i3": b.boneIndex3 = d.ToInt(); break;
                    case "w3": b.weight3 = d.ToFloat(); break;
                }
            }
            return b;
        }

        public readonly Matrix4x4 ToMatrix4X4()
        {
            var cody = new CfgDecoder(_value);
            var m = new Matrix4x4();

            foreach (var t in cody)
            {
                var d = cody.GetData();
                switch (t)
                {

                    case "00": m.m00 = d.ToFloat(); break;
                    case "01": m.m01 = d.ToFloat(); break;
                    case "02": m.m02 = d.ToFloat(); break;
                    case "03": m.m03 = d.ToFloat(); break;

                    case "10": m.m10 = d.ToFloat(); break;
                    case "11": m.m11 = d.ToFloat(); break;
                    case "12": m.m12 = d.ToFloat(); break;
                    case "13": m.m13 = d.ToFloat(); break;

                    case "20": m.m20 = d.ToFloat(); break;
                    case "21": m.m21 = d.ToFloat(); break;
                    case "22": m.m22 = d.ToFloat(); break;
                    case "23": m.m23 = d.ToFloat(); break;

                    case "30": m.m30 = d.ToFloat(); break;
                    case "31": m.m31 = d.ToFloat(); break;
                    case "32": m.m32 = d.ToFloat(); break;
                    case "33": m.m33 = d.ToFloat(); break;

                    default: Debug.Log("Unknown component: " + t); break;
                }
            }
            return m;
        }

        public readonly Quaternion ToQuaternion()
        {

            var cody = new CfgDecoder(_value);

            var q = new Quaternion();

            foreach (var t in cody)
            {
                var d = cody.GetData();
                switch (t)
                {
                    case "x": q.x = d.ToFloat(); break;
                    case "y": q.y = d.ToFloat(); break;
                    case "z": q.z = d.ToFloat(); break;
                    case "w": q.w = d.ToFloat(); break;
                    default: Debug.Log("Unknown component: " + cody.GetType()); break;
                }
            }
            return q;
        }

        public readonly Vector4 ToVector4()
        {

            var cody = new CfgDecoder(_value);

            var v4 = new Vector4();

            foreach (var t in cody)
            {
                var d = cody.GetData();
                switch (t)
                {
                    case "x": v4.x = d.ToFloat(); break;
                    case "y": v4.y = d.ToFloat(); break;
                    case "z": v4.z = d.ToFloat(); break;
                    case "w": v4.w = d.ToFloat(); break;
                    default: Debug.Log("Unknown component: " + t); break;
                }
            }
            return v4;
        }

        public readonly Vector3 ToVector3()
        {

            var cody = new CfgDecoder(_value);

            var v3 = new Vector3();

            foreach (var t in cody)
            {
                var d = cody.GetData();
                switch (t)
                {
                    case "x": v3.x = d.ToFloat(); break;
                    case "y": v3.y = d.ToFloat(); break;
                    case "z": v3.z = d.ToFloat(); break;
                }
            }
            return v3;
        }

        public readonly Vector2 ToVector2()
        {

            var cody = new CfgDecoder(_value);

            var v2 = new Vector3();

            foreach (var t in cody)
            {
                var d = cody.GetData();
                switch (t)
                {
                    case "x": v2.x = d.ToFloat(); break;
                    case "y": v2.y = d.ToFloat(); break;
                }
            }
            return v2;
        }

        public readonly Rect ToRect()
        {
            var cody = new CfgDecoder(_value);

            var rect = new Rect();

            foreach (var t in cody)
            {
                var d = cody.GetData();
                switch (t)
                {
                    case "pos": rect.position = d.ToVector2(); break;
                    case "size": rect.size = d.ToVector2(); break;
                }
            }
            return rect;
        }
        
        public readonly bool ToBool() => _value == CfgEncoder.IsTrueTag;

        public readonly bool ToBool(string yesTag) => _value == yesTag;
        
        public readonly void ToInt(ref int value)
        {
            int variable;
            if (int.TryParse(_value, out variable))
                value = variable;
        }

        public readonly int ToInt(int defaultValue = 0)
        {
            int variable;
            return int.TryParse(_value, out variable) ? variable : defaultValue;
        }

        public readonly uint ToUInt()
        {
            uint value;
            uint.TryParse(_value, out value);
            return value;
        }

        public readonly float ToFloat()
        {
            float val;
            float.TryParse(_value, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out val);
            return val;
        }
        
        public readonly Color ToColor()
        {
            var cody = new CfgDecoder(_value);
            var c = new Color();
            foreach (var t in cody)
            {
                var d = cody.GetData();
                switch (t)
                {
                    case "r": c.r = d.ToFloat(); break;
                    case "g": c.g = d.ToFloat(); break;
                    case "b": c.b = d.ToFloat(); break;
                    case "a": c.a = d.ToFloat(); break;
                    default:
                        cody.GetData(); break;
                }
            }

            return c;
        }

        #endregion

        public readonly T ToEnum<T>(T defaultValue = default(T), bool ignoreCase = true) where T : struct
        {
            T tmp;
            if (Enum.TryParse(_value, ignoreCase: ignoreCase, out tmp))
            {
                return tmp;
            } else
            {
                tmp = defaultValue;
            }

            return tmp;
        }

        public readonly Matrix4x4[] ToArray(out Matrix4x4[] l)
        {

            var cody = new CfgDecoder(this);

            l = null;

            var tmpList = new List<Matrix4x4>();

            var ind = 0;

            foreach (var tag in cody)
            {
                var d = cody.GetData();

                if (tag == "len")
                    l = new Matrix4x4[d.ToInt()];
                else
                {
                    var obj = d.ToMatrix4X4();

                    if (l != null)
                        l[ind] = obj;
                    else
                        tmpList.Add(obj);

                    ind++;
                }
            }

            return l ?? tmpList.ToArray();
        }

        #region Tagged Types Internal
        private readonly T Decode<T>(string tagAsTypeIndex, TaggedTypes.DerrivedList tps) where T : ICfg
        {
            if (tagAsTypeIndex == CfgEncoder.NullTag) return default;

            var type = tps.TaggedTypes.GetValueOrDefault(tagAsTypeIndex);

            if (type != null)
                return Decode<T>(type);

            return default;
        }

        private readonly T Decode<T>(string tagAsTypeIndex, List<Type> tps) where T : ICfg
        {
            if (tagAsTypeIndex == CfgEncoder.NullTag) return default;

            var type = tps.TryGet(ToIntFromTextSafe(tagAsTypeIndex , - 1));

            if (type != null)
                return Decode<T>(type);

            return tagAsTypeIndex == CfgDecoder.ListElementTag ? Decode<T>(tps[0]) : default;
        }
        #endregion

        #region Decodey To Type

        public readonly void Decode<T>(out T val, TaggedTypes.DerrivedList typeList) where T : IGotClassTag, ICfg
        {
            val = default;

            var cody = new CfgDecoder(_value);

            var type = typeList.TaggedTypes.GetValueOrDefault(cody.GetNextTag());

            if (type != null)
                val = cody.GetData().Decode<T>(type);
        }

        public readonly T Decode<T>() where T : ICfg, new()
        {
            var val = new T();
            DecodeOverride(ref val);
            return val;
        }

        public readonly void Decode<T>(out T val) where T : ICfg, new()
        {
            val = new T();
            DecodeOverride(ref val);
        }

        public readonly void DecodeOverride<T>(ref T obj) where T : ICfg
        {
            var cstm = obj as ICfgCustom;

            if (cstm != null)
            {
                cstm.DecodeInternal(this);
                obj = (T)((object)cstm);
            }
            else
                new CfgDecoder(this).DecodeTagsFor(ref obj);
        }

        private readonly T Decode<T>(Type childType) where T : ICfg
        {
            var val = (T)Activator.CreateInstance(childType);
             DecodeOverride(ref val);
            return val;
        }

        #endregion
        
        public readonly void ToDelegate(CfgDecoder.DecodeDelegate dec) => new CfgDecoder(this).DecodeTagsFor(dec);

        #region List

        private const string ListTag = "_lst";
        private const string ListMetaTag = "_lstMeta";

        private readonly void ToListInternal<T>(List<T> list, CfgDecoder overCody, TaggedTypes.DerrivedList tps) where T : ICfg
        {
            var dta = overCody.GetData();
            var tag = overCody.CurrentTag;

            if (tag == ListMetaTag)
                return;

            if (tag == ListTag)
            {
                var cody = new CfgDecoder(dta);

                foreach (var _ in cody)
                    ToListInternal(list, cody, tps);
            }
            else
                list.Add(dta.Decode<T>(tag, tps));
        }

        private readonly void ToListInternal<T>(List<T> list, CfgDecoder overCody, List<Type> tps) where T : ICfg
        {
            var dta = overCody.GetData();
            var tag = overCody.CurrentTag;

            if (tag == ListMetaTag)
                return;

            if (tag == ListTag)
            {
                var cody = new CfgDecoder(dta);

                foreach (var _ in cody)
                    ToListInternal(list, cody, tps);
            } else 
                list.Add(dta.Decode<T>(tag, tps));
        }

        private readonly void ToListInternal<T>(List<T> list, CfgDecoder overCody) where T : ICfg, new()
        {
            var dta = overCody.GetData();
            var tag = overCody.CurrentTag;

            if (tag == ListMetaTag)
                return;

            if (tag == ListTag)
            {
                var cody = new CfgDecoder(dta);

                foreach (var _ in cody)
                    ToListInternal(list, cody);
            }
            else
                list.Add(dta.Decode<T>());
        }

        public readonly void Decode_ListOfList<T>(out List<List<T>> l) where T : ICfg, new()
        {
            l = new List<List<T>>();

            var cody = new CfgDecoder(this);

            while (cody.GotData)
            {
                cody.GetNextTag();
                List<T> el;
                cody.GetData().ToList(out el);
                l.Add(el);
            }
        }

        public readonly void ToList_Derrived<T>(out List<T> list) where T : ICfg
        {
            list = new List<T>();

            var cody = new CfgDecoder(this);

            var tps = ICfgExtensions.TryGetDerivedClasses(typeof(T));

            if (tps != null)
                foreach (string _ in cody)
                    ToListInternal(list, cody, tps);
            else
                Debug.LogError("{0} doesn't have Derrived classes".F(typeof(T).ToPegiStringType()));

        }

        public readonly void TryToListElements<T>(List<T> list) where T : class, ICfg
        {
            var cody = new CfgDecoder(this);

            int index = 0;

            foreach (var _ in cody)
            {
                var dta = cody.GetData();
                var tag = cody.CurrentTag;

                if (tag != ListMetaTag)
                {
                    if (list.Count > index)
                    {
                        var el = list[index];
                        el.Decode(dta);
                    }
                }
                index++;
            }
        } 

        public readonly void ToList<T>(out List<T> list) where T : ICfg, new()
        {
            list = new List<T>();

            var cody = new CfgDecoder(this);

            foreach (var _ in cody)
                ToListInternal(list, cody);
        }

        public readonly void ToList<T>(out List<T> l, TaggedTypes.DerrivedList tps) where T : ICfg
        {
            var cody = new CfgDecoder(_value);

            l = new List<T>();

            foreach (var _ in cody)
               ToListInternal(l, cody, tps); //l.Add(cody.GetData().Decode<T>(tag, tps)); 
        }
        
        public readonly void ToList(out List<string> l)
        {
            l = new List<string>();

            var cody = new CfgDecoder(_value);

            foreach (var _ in cody)
                l.Add(cody.GetData().ToString());
        }

        public readonly void ToList(out List<int> l)
        {
            l = new List<int>();

            var cody = new CfgDecoder(_value);

            foreach (var _ in cody)
                l.Add(cody.GetData().ToInt());
        }

        public readonly void ToList(out List<uint> l)
        {
            l = new List<uint>();

            var cody = new CfgDecoder(_value);

            foreach (var _ in cody)
                l.Add(cody.GetData().ToUInt());
        }

        public readonly void ToList(out List<Color> l)
        {
            l = new List<Color>();

            var cody = new CfgDecoder(_value);

            foreach (var _ in cody)
                l.Add(cody.GetData().ToColor());
        }

        #endregion

        #region Dictionary

        public readonly void ToDictionary(out Dictionary<int, string> dic)
        {
            var cody = new CfgDecoder(_value);

            dic = new Dictionary<int, string>();

            while (cody.GotData)
                dic.Add(ToIntInternal(cody.GetNextTag()), cody.GetData().ToString());
        }

        public readonly void ToDictionary(out Dictionary<string, string> dic)
        {
            var cody = new CfgDecoder(_value);

            dic = new Dictionary<string, string>();

            while (cody.GotData)
                dic.Add(cody.GetNextTag(), cody.GetData().ToString());
        }

        public readonly void ToDictionary<T>(out T dic) where T: Dictionary<string, CfgData>, new()
        {
            var cody = new CfgDecoder(_value);

            dic = new T();

            while (cody.GotData)
                dic.Add(cody.GetNextTag(), cody.GetData());
        }

        public readonly void ToDictionary(out Dictionary<string, int> dic)
        {
            var cody = new CfgDecoder(_value);

            dic = new Dictionary<string, int>();

            while (cody.GotData)
                dic.Add(cody.GetNextTag(), cody.GetData().ToInt());
        }

        public readonly void ToDictionary<T>(out Dictionary<string, T> dic) where T : class, ICfg, new()
        {
            var cody = new CfgDecoder(_value);

            dic = new Dictionary<string, T>();

            while (cody.GotData)
            {
                var val = new T();
                var tag = cody.GetNextTag();
                val.Decode(cody.GetData());
                dic.Add(tag, val);
            }

        }
        
        #endregion
    }

#endregion

    #region Extensions
    public static class ICfgExtensions {


        public static void TryCopyIcfg(object from, object into)
        {
            if (into == null || into == from) return;
            
            var intoStd = into as ICfg;
            
            if (intoStd != null)
            {
                var fromStd = from as ICfg;

                if (fromStd != null)
                {
                    intoStd.Decode(fromStd.Encode().CfgData);
                }
            }
        }

        public static List<Type> TryGetDerivedClasses(Type t)
        {
            var tps = t.TryGetClassAttribute<DerivedListAttribute>()?.derivedTypes;
            if (tps == null || tps.Count == 0)
                return null;
            return tps;
        }
        public static string copyBufferValue;
        public static string copyBufferTag;

        public static bool DropStringObject(out string txt) {

            txt = null;

            Object myType = null;
            if (pegi.Edit(ref myType)) {
                txt = QcFile.Load.TryLoadAsTextAsset(myType, asBytes: true);
                pegi.GameView.ShowNotification("Loaded " + myType.name);

                return true;
            }
            return false;
        }

        public static bool LoadCfgOnDrop<T>(this T obj) where T: ICfg
        {
            string txt;
            if (DropStringObject(out txt)) {
               new CfgData(txt).DecodeOverride(ref obj);
                return true;
            }

            return false;
        }

        public static ICfg SaveToAssets(this ICfg s, string path, string filename)
        {
            QcFile.Save.ToAssets(path, filename, s.Encode().ToString(), asBytes: true);
            return s;
        }

        public static T LoadFromResources<T>(this T s, string subFolder, string file)where T:ICfg, new() {
			s ??= new T ();
			new CfgData(QcFile.Load.FromResources(subFolder, file, asBytes: true)).DecodeOverride(ref s);
			return s;
		}

    }
#endregion
}