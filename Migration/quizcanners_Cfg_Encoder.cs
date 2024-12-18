﻿using QuizCanners.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Migration
{
    public static class EncodeExtensions 
    {
        public static System.Text.StringBuilder AppendSplit(this System.Text.StringBuilder builder, string value) => builder.Append(value).Append(CfgEncoder.Splitter);
        
        public static CfgEncoder Encode (this Transform tf, bool local) {

            var cody = new CfgEncoder();

            cody.Add_Bool("loc", local);

            if (local) {
                cody.Add("pos", tf.localPosition)
                .Add("size", tf.localScale)
                .Add("rot", tf.localRotation);
            } else {
                cody.Add("pos", tf.position)
                .Add("size", tf.localScale)
                .Add("rot", tf.rotation);
            }

            return cody;
        }

        public static CfgEncoder Encode(this Rect rect) => new CfgEncoder()
            .Add("pos",rect.position)
            .Add("size",rect.size);
            
        public static CfgEncoder Encode(this RectTransform tf, bool local)
        {
            return new CfgEncoder()
            .Add("tfBase", tf.transform.Encode(local))
            .Add("aPos", tf.anchoredPosition)
            .Add("aPos3D", tf.anchoredPosition3D)
            .Add("aMax", tf.anchorMax)
            .Add("aMin", tf.anchorMin)
            .Add("ofMax", tf.offsetMax)
            .Add("ofMin", tf.offsetMin)
            .Add("pvt", tf.pivot)
            .Add("deSize", tf.sizeDelta);
        }
    
        public static CfgEncoder Encode<T>(this T[] arr) where T : ICfg, new() {
            var cody = new CfgEncoder();

            if (arr.IsNullOrEmpty()) return cody; 

            cody.Add("len", arr.Length);

          /*  var types = ICfgExtensions.TryGetDerivedClasses(typeof(T));

            if (types != null && types.Count > 0) {
                foreach (var v in arr)
                    cody.Add(v, types);
            }
            else*/
            foreach (var v in arr) {
                if (!QcUnity.IsNullOrDestroyed_Obj(v))
                    cody.Add(CfgDecoder.ListElementTag, v.Encode().CfgData);
                else
                    cody.Add_String(CfgEncoder.NullTag, "");
            }

            return cody;
        }

        public static CfgEncoder Encode<T>(this Dictionary<string, T> dic) where T: ICfg
        {
            var sub = new CfgEncoder();

            if (dic == null) return sub;

            foreach (var e in dic)
                sub.Add(e.Key, e.Value.Encode());

            return sub;
        }
        
        public static CfgEncoder Encode(this Dictionary<string, string> dic)
        {
            var sub = new CfgEncoder();

            if (dic == null) return sub;
            
            foreach (var e in dic)
                sub.Add_String(e.Key, e.Value);

            return sub;
        }

        public static CfgEncoder Encode(this Dictionary<string, int> dic)
        {
            var sub = new CfgEncoder();

            if (dic == null) return sub;

            foreach (var e in dic)
                sub.Add(e.Key, e.Value);

            return sub;
        }

        public static CfgEncoder Encode(this Dictionary<int, string> dic)
        {
            var sub = new CfgEncoder();

            if (dic == null) return sub; 
            
            foreach (var e in dic)
                sub.Add_String(e.Key.ToString(), e.Value);

            return sub;
        }

        public static CfgEncoder Encode(this Dictionary<string, CfgData> dic)
        {
            var sub = new CfgEncoder();

            if (dic == null) return sub;

            foreach (var e in dic)
                sub.Add(e.Key, e.Value);

            return sub;
        }


        #region ValueTypes
        public static CfgEncoder Encode(this Vector3 v3, int precision) => new CfgEncoder()
            .Add_IfNotEpsilon("x", QcSharp.RoundTo(v3.x, precision))
            .Add_IfNotEpsilon("y", QcSharp.RoundTo(v3.y, precision))
            .Add_IfNotEpsilon("z", QcSharp.RoundTo(v3.z, precision));
            
        public static CfgEncoder Encode(this Vector2 v2, int precision) => new CfgEncoder()
            .Add_IfNotEpsilon("x", QcSharp.RoundTo(v2.x, precision))
            .Add_IfNotEpsilon("y", QcSharp.RoundTo(v2.y, precision));
        
        public static CfgEncoder Encode(this Quaternion q) => new CfgEncoder()
            .Add_IfNotEpsilon("x", q.x.RoundTo6Dec())
            .Add_IfNotEpsilon("y", q.y.RoundTo6Dec())
            .Add_IfNotEpsilon("z", q.z.RoundTo6Dec())
            .Add_IfNotEpsilon("w", q.w.RoundTo6Dec());
            
        public static CfgEncoder Encode(this BoneWeight bw) => new CfgEncoder()
            .Add_IfNotZero("i0", bw.boneIndex0)
            .Add_IfNotEpsilon("w0", bw.weight0)

            .Add_IfNotZero("i1", bw.boneIndex1)
            .Add_IfNotEpsilon("w1", bw.weight1)

            .Add_IfNotZero("i2", bw.boneIndex2)
            .Add_IfNotEpsilon("w2", bw.weight2)

            .Add_IfNotZero("i3", bw.boneIndex3)
            .Add_IfNotEpsilon("w3", bw.weight3);
            
        public static CfgEncoder Encode (this Matrix4x4 m) => new CfgEncoder()

                .Add_IfNotEpsilon("00", m.m00)
                .Add_IfNotEpsilon("01", m.m01)
                .Add_IfNotEpsilon("02", m.m02)
                .Add_IfNotEpsilon("03", m.m03)

                .Add_IfNotEpsilon("10", m.m10)
                .Add_IfNotEpsilon("11", m.m11)
                .Add_IfNotEpsilon("12", m.m12)
                .Add_IfNotEpsilon("13", m.m13)

                .Add_IfNotEpsilon("20", m.m20)
                .Add_IfNotEpsilon("21", m.m21)
                .Add_IfNotEpsilon("22", m.m22)
                .Add_IfNotEpsilon("23", m.m23)

                .Add_IfNotEpsilon("30", m.m30)
                .Add_IfNotEpsilon("31", m.m31)
                .Add_IfNotEpsilon("32", m.m32)
                .Add_IfNotEpsilon("33", m.m33);
        
        public static CfgEncoder Encode(this Vector4 v4) => new CfgEncoder()
            .Add_IfNotEpsilon("x", v4.x.RoundTo6Dec())
            .Add_IfNotEpsilon("y", v4.y.RoundTo6Dec())
            .Add_IfNotEpsilon("z", v4.z.RoundTo6Dec())
            .Add_IfNotEpsilon("w", v4.w.RoundTo6Dec());

        public static CfgEncoder Encode(this Vector3 v3) => new CfgEncoder()
            .Add_IfNotEpsilon("x", v3.x.RoundTo6Dec())
            .Add_IfNotEpsilon("y", v3.y.RoundTo6Dec())
            .Add_IfNotEpsilon("z", v3.z.RoundTo6Dec());

        public static CfgEncoder Encode(this Vector2 v2) => new CfgEncoder()
            .Add_IfNotEpsilon("x", v2.x.RoundTo6Dec())
            .Add_IfNotEpsilon("y", v2.y.RoundTo6Dec());
        
        public static CfgEncoder Encode(this Color col) => new CfgEncoder()
            .Add_IfNotEpsilon("r", QcSharp.RoundTo(col.r, 3))
            .Add_IfNotEpsilon("g", QcSharp.RoundTo(col.g, 3))
            .Add_IfNotEpsilon("b", QcSharp.RoundTo(col.b, 3))
            .Add_IfNotEpsilon("a", QcSharp.RoundTo(col.a, 3));
        #endregion

        private static float RoundTo6Dec(this float val) => Mathf.Round(val * 1000000f) * 0.000001f;

    }

    public class CfgEncoder
    {
        #region Constants
        public const char Splitter = '|';
        public const string NullTag = "null";
        public const string ListElementTag = "e";
        public const string UnrecognizedTag = "_urec";
        public const string IsTrueTag = "y";
        public const string IsFalseTag = "n";
        #endregion

        private readonly System.Text.StringBuilder _builder = new();

        public CfgData CfgData => new(_builder.ToString());

        public override string ToString() 
        {
            return _builder.ToString();
        }

        public delegate CfgEncoder EncodeDelegate();
        public CfgEncoder Add(string tag, EncodeDelegate cody) => cody == null ? this : Add(tag, cody());

        public CfgEncoder Add(string tag, CfgEncoder cody) => cody == null ? this : Add_String(tag, cody.ToString());

        public CfgEncoder Add(string tag, CfgData data)
        {
            if (data.IsEmpty)
                data = new CfgData("");

            return Add_String(tag, data.ToString());
        }

        public CfgEncoder Add_String(string tag, string data)
        {
            data ??= "";

            _builder.AppendSplit(tag)
            .AppendSplit(data.Length.ToString())
            .AppendSplit(data);
            return this;
        }

        public CfgEncoder Add_Bool(string tag, bool val) => Add_String(tag, val ? IsTrueTag : IsFalseTag);
        
        #region ValueTypes
        public CfgEncoder Add(string tag, float val) =>
        Add_String(tag, val.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat));
        public CfgEncoder Add(string tag, float val, int precision) =>
            Add_String(tag, QcSharp.RoundTo(val, precision).ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat));
        public CfgEncoder Add(string tag, int val) => Add_String(tag, val.ToString());
        public CfgEncoder Add(string tag, uint val) => Add_String(tag, val.ToString());

        public CfgEncoder Add(string tag, Transform tf) => Add(tag, tf.Encode(true));
        public CfgEncoder Add(string tag, Transform tf, bool local) => Add(tag, tf.Encode(local));
        public CfgEncoder Add(string tag, Rect tf) => Add(tag, tf.Encode());
        public CfgEncoder Add(string tag, Matrix4x4 m) => Add(tag, m.Encode());
        public CfgEncoder Add(string tag, BoneWeight bw) => Add(tag, bw.Encode());
        public CfgEncoder Add(string tag, Quaternion q) => Add(tag, q.Encode());
        public CfgEncoder Add(string tag, Vector4 v4) => Add(tag, v4.Encode());
        public CfgEncoder Add(string tag, Vector3 v3) => Add(tag, v3.Encode());
        public CfgEncoder Add(string tag, Vector2 v2) => Add(tag, v2.Encode());
        public CfgEncoder Add(string tag, Vector3 v3, int precision) => Add(tag, v3.Encode(precision));
        public CfgEncoder Add(string tag, Vector2 v2, int precision) => Add(tag, v2.Encode(precision));
        public CfgEncoder Add(string tag, Color col) => Add(tag, col.Encode());
        #endregion

        #region Internal Add Unrecognized Data

        public CfgEncoder Add<T>(T v, List<Type> types) where T : ICfg
        {
            if (QcUnity.IsNullOrDestroyed_Obj(v))  return Add_String(NullTag, "");
            
            var typeIndex = types.IndexOf(v.GetType());
            return Add(typeIndex != -1 ? typeIndex.ToString() : UnrecognizedTag, v.Encode());
           
        }

        #endregion

        #region Abstracts

        public CfgEncoder Add<T>(string tag, List<T> val, TaggedTypes.DerrivedList _) where T : IGotClassTag, ICfg => Add_Abstract(tag, val);

        public CfgEncoder Add_Abstract<T>(string tag, List<T> lst) where T : IGotClassTag, ICfg
        {

            if (lst.IsNullOrEmpty()) return this;
            
            var cody = new CfgEncoder();

            foreach (var v in lst)
                if (v!= null)
                    cody.Add(v.ClassTag, v);
                else
                    cody.Add_String(NullTag, "");
            

            return Add(tag, cody);
        }

#pragma warning disable IDE0060 // Remove unused parameter
        public CfgEncoder Add<T>(string tag, T typeTag, TaggedTypes.DerrivedList cfg) where T: IGotClassTag, ICfg
            => typeTag == null ? this :
            Add(tag, new CfgEncoder().Add(typeTag.ClassTag, typeTag.Encode()));
#pragma warning restore IDE0060 // Remove unused parameter

        public CfgEncoder Add_Abstract<T>(string tag, T typeTag) where T : IGotClassTag, ICfg
            =>  typeTag == null ? this :
             Add(tag, new CfgEncoder().Add(typeTag.ClassTag, typeTag.Encode()));
        
        #endregion

        public CfgEncoder Add(string tag, ICfg other)
        {
            if (QcUnity.IsNullOrDestroyed_Obj(other)) return this;
            
            return Add(tag, other.Encode());
        }

        public CfgEncoder Add(string tag, List<int> val) {

            var cody = new CfgEncoder();
            
            foreach (var i in val)
                cody.Add(CfgDecoder.ListElementTag, i);

            return Add(tag, cody);
        }

        public CfgEncoder Add(string tag, List<string> lst)
        {
            if (lst == null) return this;
            
            var cody = new CfgEncoder();
            
            foreach (var s in lst)
                cody.Add_String(CfgDecoder.ListElementTag, s);

            return Add(tag, cody);
        }

        public CfgEncoder Add(string tag, List<uint> val)
        {
            var cody = new CfgEncoder();
            foreach (var i in val)
                cody.Add(CfgDecoder.ListElementTag, i);
            
            return Add(tag, cody);
        }

        public CfgEncoder Add(string tag, List<Color> val)  
        {
            var cody = new CfgEncoder();
            
            foreach (var i in val)
                cody.Add(CfgDecoder.ListElementTag, i);
            
            return Add(tag, cody);
        }

        public CfgEncoder Add(string tag, List<Matrix4x4> val)  
        {
            var cody = new CfgEncoder();
            
            foreach (var i in val)
                cody.Add(CfgDecoder.ListElementTag, i);
            
            return Add(tag, cody);
        }

        public CfgEncoder Add(string tag, Matrix4x4[] arr) 
        {
            if (arr == null) 
                return this;
            
            var cody = new CfgEncoder()
            .Add("len", arr.Length);

            foreach (var v in arr) 
                cody.Add(CfgDecoder.ListElementTag, v.Encode());
            
            return Add(tag, cody);
        }

        public CfgEncoder Add_Derrived<T>(string tag, List<T> lst) where T : ICfg
        {
            var cody = new CfgEncoder();

            if (lst == null) return this;

            var indTypes = ICfgExtensions.TryGetDerivedClasses(typeof(T));

            if (indTypes != null)
            {
                foreach (var v in lst)
                    cody.Add(v, indTypes);

                return Add(tag, cody);
            }
            else
            {
                Debug.LogError("{0} doesn't have Derrived Classes".F(typeof(T).ToPegiStringType()));
                return this;
            }
        }


        public CfgEncoder Add<T>(string tag, List<T> lst) where T : ICfg, new()
        {
            var cody = new CfgEncoder();

            if (lst == null) 
                return this;

            foreach (var v in lst)
                if (v != null)
                    cody.Add(CfgDecoder.ListElementTag, v.Encode());
                else
                    cody.Add_String(NullTag, "");
            
            return Add(tag, cody);
        }
        
        public CfgEncoder Add(string tag, Dictionary<int, string> dic)
        {
            var sub = new CfgEncoder();

            if (dic == null) return this;
                
            foreach (var e in dic)
                sub.Add_String(e.Key.ToString(), e.Value);

            return Add(tag, sub);
        }

        public CfgEncoder Add(string tag, Dictionary<string, string> dic) => Add(tag, dic.Encode());

        public CfgEncoder Add(string tag, Dictionary<string, int> dic) => Add(tag, dic.Encode());

        public CfgEncoder Add(string tag, Dictionary<string, CfgData> dic) => Add(tag, dic.Encode());
        
        public CfgEncoder Add<T>(string tag, Dictionary<string, T> dic) where T: ICfg => Add(tag, dic.Encode());
        
        public CfgEncoder Add<T>(string tag, T[] val) where T : ICfg, new() => Add(tag, val.Encode());

        #region NonDefault Encodes

        public CfgEncoder TryAdd<T>(string tag, T obj) {

            var objStd = QcUnity.TryGetInterfaceFrom<ICfg>(obj); 
            return (objStd != null) ? Add(tag, objStd) : this;
        }

        public CfgEncoder Add_IfNotNegative(string tag, int val) => (val >= 0) ? Add_String(tag, val.ToString()) : this;
        
        public CfgEncoder Add_IfTrue(string tag, bool val) => val ? Add_Bool(tag, true) : this;
  
        public CfgEncoder Add_IfFalse(string tag, bool val) => (!val) ? Add_Bool(tag, false) :  this;
        
        public CfgEncoder Add_IfNotDefault(string tag, ICanBeDefaultCfg cfg) => (!QcUnity.IsNullOrDestroyed_Obj(cfg) && !cfg.IsDefault) ? Add(tag, cfg): this;

        public CfgEncoder Add_IfNotDefault(string tag, ICfg cfg)
        {
            if (QcUnity.IsNullOrDestroyed_Obj(cfg)) return this;

            return (cfg is not ICanBeDefaultCfg def || !def.IsDefault) ? Add(tag, cfg) : this;
        }

        public CfgEncoder Add_IfNotEmpty(string tag, string val) => val.IsNullOrEmpty() ? this : Add_String(tag, val);

        public CfgEncoder Add_IfNotEmpty_Derrived<T>(string tag, List<T> lst) where T : ICfg =>
                lst.IsNullOrEmpty() ? this : Add_Derrived(tag, lst);

        public CfgEncoder Add_IfNotEmpty<T>(string tag, List<T> lst) where T : ICfg, new() => 
            lst.IsNullOrEmpty() ? this : Add(tag, lst);
        
        public CfgEncoder Add_IfNotEmpty(string tag, List<string> val) => val.IsNullOrEmpty() ? this : Add(tag, val);
        
        public CfgEncoder Add_IfNotEmpty(string tag, List<int> val) => val.IsNullOrEmpty() ? this : Add(tag, val);

        public CfgEncoder Add_IfNotEmpty(string tag, List<uint> val) => val.IsNullOrEmpty() ? this : Add(tag, val);

        public CfgEncoder Add_IfNotEmpty<T>(string tag, List<List<T>> lst) where T : ICfg, new()
        {

            if (lst.IsNullOrEmpty()) return this;

            var sub = new CfgEncoder();

            foreach (var l in lst)
                sub.Add_IfNotEmpty(CfgDecoder.ListElementTag, l);

            return Add_String(tag, sub.ToString());
            
        }

#pragma warning disable IDE0060 // Remove unused parameter

        public CfgEncoder Add_IfNotEmpty<T>(string tag, List<T> val, TaggedTypes.DerrivedList tts) where T : IGotClassTag, ICfg  =>
            val.IsNullOrEmpty() ? this : Add_Abstract(tag, val);

#pragma warning restore IDE0060 // Remove unused parameter

        public CfgEncoder Add_IfNotEmpty(string tag, Dictionary<int, string> dic) => dic.IsNullOrEmpty() ? this : Add(tag, dic);
   
        public CfgEncoder Add_IfNotEmpty(string tag, Dictionary<string, string> dic) => dic.IsNullOrEmpty() ? this :  Add(tag, dic);

        public CfgEncoder Add_IfNotEmpty(string tag, Dictionary<string, CfgData> dic) => dic.IsNullOrEmpty() ? this : Add(tag, dic);
        
        public CfgEncoder Add_IfNotEpsilon(string tag, float val) => (Mathf.Abs(val) > float.Epsilon * 100) ? Add(tag, RoundTo6Dec(val)) : this;
       
        public CfgEncoder Add_IfNotOne(string tag, Vector4 v4) => v4.Equals(Vector4.one) ? this : Add(tag, v4.Encode());

        public CfgEncoder Add_IfNotOne(string tag, Vector3 v3) => v3.Equals(Vector3.one) ? this : Add(tag, v3.Encode());

        public CfgEncoder Add_IfNotOne(string tag, Vector2 v2) => v2.Equals(Vector2.one) ? this : Add(tag, v2.Encode());
        
        public CfgEncoder Add_IfNotZero(string tag, int val) => val == 0 ? this : Add_String(tag, val.ToString());
            
        public CfgEncoder Add_IfNotZero(string tag, float val, float precision) => Mathf.Abs(val) > precision ?  Add(tag, val): this;
            
        public CfgEncoder Add_IfNotZero(string tag, Vector4 v4)  => v4.magnitude> Mathf.Epsilon ? Add(tag, v4.Encode()) : this;
        
        public CfgEncoder Add_IfNotZero(string tag, Vector3 v3) => v3.magnitude> Mathf.Epsilon ? Add(tag, v3.Encode()) : this;

        public CfgEncoder Add_IfNotZero(string tag, Vector2 v2) => v2.magnitude > Mathf.Epsilon ? Add(tag, v2.Encode()) : this;

        public CfgEncoder Add_IfNotBlack(string tag, Color col) => col == Color.black ? this : Add(tag, col);

        public CfgEncoder Add_IfNotWhite(string tag, Color col) => col == Color.white ? this : Add(tag, col);

        #endregion

        private static float RoundTo6Dec(float val) => Mathf.Round(val * 1000000f) * 0.000001f;

    }
}