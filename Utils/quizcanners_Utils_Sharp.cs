﻿using QuizCanners.Inspect;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace QuizCanners.Utils
{


#pragma warning disable IDE0018 // Inline variable declaration

    public static partial class QcSharp
    {
        #region Time

        public static string SecondsToReadableString(int seconds) => TicksToReadableString(seconds * TimeSpan.TicksPerSecond);

        public static string SecondsToReadableString(double seconds) => TicksToReadableString(seconds * TimeSpan.TicksPerSecond);

        public static string SecondsToReadableString(float seconds) => TicksToReadableString(seconds * TimeSpan.TicksPerSecond);

        public static string SecondsToReadableString(long seconds) => TicksToReadableString(seconds * TimeSpan.TicksPerSecond);

        public static string TicksToReadableString(double totalTicks)
        {
            double absElapsed = Math.Abs(totalTicks);

            if (absElapsed < TimeSpan.TicksPerMillisecond)  return "{1} ms ({0} ticks)".F(totalTicks.ToString("0.00"), ForScale(TimeSpan.TicksPerMillisecond));
            if (absElapsed < TimeSpan.TicksPerSecond)       return "{1} s ({0} miliseconds)".F(ForScale(TimeSpan.TicksPerMillisecond), ForScale(TimeSpan.TicksPerSecond));
            if (absElapsed < TimeSpan.TicksPerMinute)       return "{1} min ({0} seconds)".F(ForScale(TimeSpan.TicksPerSecond), ForScale(TimeSpan.TicksPerMinute));
            if (absElapsed < TimeSpan.TicksPerHour)         return "{1} hours ({0} minutes)".F(ForScale(TimeSpan.TicksPerMinute), ForScale(TimeSpan.TicksPerHour));
                                                            return "{1} days ({0} hours)".F(ForScale(TimeSpan.TicksPerHour), ForScale(TimeSpan.TicksPerDay));

            string ForScale(long scale)
            {
                var val = totalTicks / scale;
                val = Math.Max(val, 0.01);
                return val.ToString("0.00");
            }

        }

        public static string ToShortDisplayString(this TimeSpan span)
        {
            if (span == TimeSpan.MaxValue)
                return "infinite";

            if (span == TimeSpan.Zero)
                return "zero";

            var sb = new StringBuilder(16);

            float daysInYear = 365.25f;

            if (span.TotalDays > daysInYear)
            {
                sb.AppendIfNonZero(value: span.Days / daysInYear, span.TotalDays / daysInYear, suffix: "y", last: false)
                  .AppendIfNonZero(value: span.Days, span.TotalDays, suffix: "d", last: true);
            }
            if (span.TotalDays >= 1)
            {
                sb.AppendIfNonZero(value: span.Days, span.TotalDays, suffix: "d", last: false)
                  .AppendIfNonZero(value: span.Hours, span.TotalHours, suffix: "h", last: true);
            }
            else if (span.TotalHours >=1 )
            {
                sb.AppendIfNonZero(value: span.Hours, span.TotalHours, suffix: "h", last: false)
                  .AppendIfNonZero(value: span.Minutes, span.TotalMinutes, suffix: "m", last: true);
            }
            else if (span.TotalMinutes >=1 )
            {
                sb.AppendIfNonZero(value: span.Minutes, span.TotalMinutes, suffix: "m", last: false)
                  .AppendIfNonZero(value: span.Seconds, span.TotalSeconds, suffix: "s", last: true);
            }
            else if (span.TotalSeconds >= 1)
            {
                sb.AppendIfNonZero(value: span.Seconds, span.TotalSeconds, suffix: "s", last: false)
                  .AppendIfNonZero(value: span.Milliseconds, span.TotalMilliseconds, suffix: "ms", last: true);
            } else //if (span.TotalMilliseconds >= 1) 
            {
                sb.AppendIfNonZero(value: span.Milliseconds, span.TotalMilliseconds, suffix: "ms", last: false)
                .AppendIfNonZero(value: 0, span.Ticks, suffix: "ticks", last: true);
            }

            return sb.ToString();
        }

        private static StringBuilder AppendIfNonZero(this StringBuilder sb, double value, double totalValue, string suffix, bool last)
        {
            if (sb.Length == 0)
            {
                value = Math.Floor(totalValue); // Use Full value if no previous
            }

            if (last && value == 0 && sb.Length == 0)
            {
                value = 1; // Not to return empty string
            }

            if (value > 0)
            {
                if (sb.Length > 0)
                {
                    sb.Append(", ");
                }
                sb.Append(value.ToString());
                sb.Append(suffix);
            }

            return sb;
        }

        #endregion

        #region Html Tags (For Text Mesh Pro)

        public static string HtmlTag(string tag, string value) => "<{0}={1}>".F(tag, value);

        public static string HtmlTag(string tag) => "<{0}>".F(tag);

        public static string HtmlTagWrap(string tag, string content) => content.IsNullOrEmpty() ? "" : "<{0}>{1}</{0}>".F(tag, content);

        public static string HtmlTagWrap(string tag, string tagValue, string content) => content.IsNullOrEmpty() ? "" : "<{0}={1}>{2}</{0}>".F(tag, tagValue, content);

        public static void HtmlTagWrap(string tag, string tagValue, string content, StringBuilder sb)
        {
            sb.Append('<')
                .Append(tag)
                .Append('=')
                .Append(tagValue)
                .Append('>')
                .Append(content)
                .Append("</")
                .Append(tag)
                .Append('>');
           // content.IsNullOrEmpty() ? "" : "<{0}={1}>{2}</{0}>".F(tag, tagValue, content);
        }


        public static string HtmlTagWrap(string content, Color color) => content.IsNullOrEmpty() ? "" : "<{0}={1}>{2}</{0}>".F("color", "#" + ColorUtility.ToHtmlStringRGBA(color), content);

        public static string HtmlTagAlpha(float alpha01) => HtmlTag("alpha", "#" + Mathf.FloorToInt(Mathf.Clamp01(alpha01) * 255).ToString("X2"));

        public static string HtmlTagWrapAlpha(string content, float alpha01) => HtmlTagAlpha(alpha01) + content + HtmlTagAlpha(1f);

        public static StringBuilder AppendHtmlTag(this StringBuilder bld, string tag) => bld.Append(HtmlTag(tag));

        public static StringBuilder AppendHtmlTag(this StringBuilder bld, string tag, string value) => bld.Append(HtmlTag(tag, value));

        public static StringBuilder AppendHtmlText(this StringBuilder bld, string tag, string value, string content) => bld.Append(HtmlTagWrap(tag, value, content));

        public static StringBuilder AppendHtmlText(this StringBuilder bld, string tag, string content) => bld.Append(HtmlTagWrap(tag, content));

        public static StringBuilder AppendHtmlAlpha(this StringBuilder bld, string content, float alpha) => bld.Append(HtmlTagWrapAlpha(content, alpha));

        public static StringBuilder AppendHtmlBold(this StringBuilder bld, string content) => bld.Append(HtmlTagWrap("b", content));

        public static StringBuilder AppendHtmlItalics(this StringBuilder bld, string content) => bld.Append(HtmlTagWrap("i", content));

        public static StringBuilder AppendHtml(this StringBuilder bld, string content, Color col) => bld.Append(HtmlTagWrap(content, col)); //content.IsNullOrEmpty() ? bld : bld.AppendHtmlText("color", "#"+ColorUtility.ToHtmlStringRGBA(col), content);

        public static StringBuilder AppendHtmlLink(this StringBuilder bld, string content) => content.IsNullOrEmpty() ? bld : bld.AppendHtmlText("link", "dummy", content);

        public static StringBuilder AppendHtmlLink(this StringBuilder bld, string content, Color col) => content.IsNullOrEmpty() ? bld :
            bld.AppendHtmlText("link", "dummy", HtmlTagWrap(content, col));


        #endregion

        #region Enum

        public static T ToEnum<T>(int intValue) => (T)Enum.ToObject(typeof(T), intValue);

        #endregion

        #region List Management

        internal static bool CanAdd<T>(List<T> list, ref object obj, out T conv, bool onlyIfNew = true)
        {
            conv = default;

            if (obj == null || list == null)
                return false;

            if (obj is not T t)
            {

                GameObject go = null;

                if (typeof(T).IsSubclassOf(typeof(MonoBehaviour)))
                {
                    var mb = (obj as MonoBehaviour);
                    if (mb)
                    {
                        go = mb.gameObject;
                    }
                }
                else go = obj as GameObject;

                if (go)
#pragma warning disable UNT0014 // Invalid type for call to GetComponent
                    conv = go.GetComponent<T>();
#pragma warning restore UNT0014 // Invalid type for call to GetComponent
            }
            else conv = t;

            if (conv == null || conv.Equals(default(T))) return false;

            var objType = obj.GetType();

            var dl = Migration.ICfgExtensions.TryGetDerivedClasses(typeof(T));
            if (dl != null)
            {
                if (!dl.Contains(objType))
                    return false;
            }
            else
            {

                var tc = Migration.TaggedTypes<T>.DerrivedList;

                if (tc != null && !tc.Types.Contains(objType))
                    return false;
            }

            return !onlyIfNew || !list.Contains(conv);
        }

        private static void AssignUniqueIndex<T>(IList<T> list, T el)
        {
            if (el is not IGotIndex ind) 
                return;
            
            var maxIndex = ind.IndexForInspector;
            foreach (var o in list)
                if (!el.Equals(o))
                {
                    if (o is IGotIndex oInd)
                        maxIndex = Mathf.Max(maxIndex, oInd.IndexForInspector + 1);
                }
            ind.IndexForInspector = maxIndex;

        }

        public static T AddWithUniqueNameAndIndex<T>(IList<T> list) => AddWithUniqueNameAndIndex(list, "New " + typeof(T).ToPegiStringType());

        internal static T AddWithUniqueNameAndIndex<T>(IList<T> list, string name) =>
            AddWithUniqueNameAndIndex(list, (T)Activator.CreateInstance(typeof(T)), name);

        internal static T AddWithUniqueNameAndIndex<T>(IList<T> list, T e, string name)
        {
            AssignUniqueIndex(list, e);
            list.Add(e);

            if (e is IGotName named)
            {
                bool duplicate = false;

                var oldName = named.NameForInspector;

                if (oldName.IsNullOrEmpty())
                {
                    duplicate = true;
                }
                else
                {
                    for (int i = 0; i < list.Count - 1; i++)
                    {
                        var el = list[i];
                        if (el != null)
                        {
                            if (el is IGotName existingName)
                            {
                                if (oldName.Equals(existingName.NameForInspector))
                                {
                                    duplicate = true;
                                    break;
                                }

                            }
                        }
                    }
                }

                if (duplicate)
                {
                    named.NameForInspector = name;
                    AssignUniqueNameIn(e, list);
                }
            }
            return e;
        }

        private static void AssignUniqueNameIn<T>(T el, IList<T> list)
        {
            if (el is not IGotName namedNewElement) 
                return;

            var newName = namedNewElement.NameForInspector;
            var duplicate = true;
            var counter = 0;

            while (duplicate)
            {
                duplicate = false;

                foreach (var e in list)
                {
                    if (e is not IGotName currentName)
                        continue;

                    var otherName = currentName.NameForInspector;

                    otherName ??= "";

                    if (e.Equals(el) || !newName.Equals(otherName))
                        continue;

                    duplicate = true;
                    counter++;
                    newName = namedNewElement.NameForInspector + counter;
                    break;
                }
            }

            namedNewElement.NameForInspector = newName;
        }

        public static List<T> SelectByNames<T>(this IEnumerable<T> collection, List<string> names) where T : class, IGotName
            => collection.Join(names, el => el.NameForInspector, id => id, (e, i) => e).ToList();

        public static T GetElementAt<T>(this IEnumerable<T> source, int index) => source.ElementAt(index);
        
        public static T GetRandom<T>(this List<T> list, ref int previous)
        {
            if (list.IsNullOrEmpty())
                return default;

            if (list.Count == 1)
                return list[0];

            var rnd = Random.Range(0, list.Count);

            if (rnd == previous)
            {
                rnd = (rnd + 1) % list.Count;
            }

            previous = rnd;
            return list[previous];
        }
        public static T GetRandom<T>(this List<T> list)
        {
            if (list.IsNullOrEmpty())
                return default;
            
            if (list.Count == 1)
                return list[0];

            return list[Random.Range(0, list.Count)];
        }
        public static TValue GetRandom<TKey, TValue>(this Dictionary<TKey, TValue> dic)
        {
            if (dic.IsNullOrEmpty())
                return default;

            if (dic.Count == 1)
                return dic.GetElementAt(0).Value;

            return dic.GetElementAt(Random.Range(0, dic.Count)).Value;
        }

        public static T GetRandomByWeight<T>(this List<T> sequence, Func<T, float> weightSelector)
        {
            if (sequence.IsNullOrEmpty())
            {
                var typeName = typeof(T).Name;
                QcLog.ChillLogger.LogErrorOnce("{0}: List<{1}> is {2}".F(nameof(GetRandomByWeight), typeName, sequence == null ? "NULL" : "Empty"), key: "{0} grnd".F(typeName));
                return default;
            }

            if (sequence.Count == 1)
                return sequence[0];

            float TotalWeight = 0;

            foreach (var el in sequence)
            {
                TotalWeight += weightSelector(el);
            }

            float itemWeightIndex = Random.Range(0f, TotalWeight);

            float currentWeightIndex = 0;

            foreach (var item in sequence)
            {
                currentWeightIndex += weightSelector(item);

                if (currentWeightIndex >= itemWeightIndex)
                {
                    return item;
                }
            }

            return sequence.TryGet(0);
        }

        /// <summary>Returns True if any change to <c>List</c> were made to satisfy the target state.</summary>
        public static bool SetContains<T>(this List<T> list, T value, bool targetState)
        {
            if (list.Contains(value) != targetState)
            {
                if (targetState)
                    list.Add(value);
                else
                    list.Remove(value);

                return true;
            }

            return false;
        }
        public static void ForceSetCount<T>(this List<T> list, int count) where T : new()
        {
            if (count == list.Count)
                return;

            var diff = list.Count - count;

            if (diff > 0)
                list.RemoveRange(count, diff);
            else
            {
                while (list.Count < count)
                    list.Add(new T());
            }
        }

        public static T GetOrCreate<T>(this List<T> list, int index) where T : new()
        {
            T val;

            if (list.Count> index)
                return list[index];

            list.ForceSetCount(index + 1);

            val = new T();
            list[index] = val;

            return val;
        }

        public static T GetOrSet<T>(this List<T> list, int index, T defaultValue)
        {
            T val;

            if (list.Count > index)
                return list[index];

            while (list.Count < (index + 1))
                list.Add(default);

            val = defaultValue;
            list[index] = val;

            return val;
        }

        public static List<T> TryAdd<T>(this List<T> list, object ass, bool onlyIfNew = true)
        {

            T toAdd;

            if (CanAdd(list, ref ass, out toAdd, onlyIfNew))
                list.Add(toAdd);

            return list;

        }

        public static T TryTake<T>(this List<T> list, int index, T defaultValue = default)
        {

            if (list.IsNullOrEmpty() || list.Count <= index)
                return defaultValue;

            var ret = list[index];

            list.RemoveAt(index);

            return ret;
        }

        public static T TryTakeLast<T>(this List<T> list) => list.TryTake(list.Count - 1);

        public static void ForceSet<T, G>(this List<T> list, int index, G val) where G : T
        {
            if (list == null || index < 0) return;

            while (list.Count <= index)
                list.Add(default);

            list[index] = val;
        }

        public static bool AddIfNew<T>(this List<T> list, T val)
        {
            if (list.Contains(val))
                return false;

            list.Add(val);
            return true;
        }

        public static T TryGetLast<T>(this IList<T> list)
        {

            if (list == null || list.Count == 0)
                return default;

            return list[list.Count - 1];

        }

        public static T TryGet<T>(this List<T> list, int index, T defaultValue = default)
        {
            if (list == null || index < 0 || index >= list.Count)
                return defaultValue;

            return list[index];
        }


        public static bool IsNew(this Type t) => t.IsValueType || (!t.IsUnityObject() && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null);

        public static void Move<T>(this List<T> list, int oldIndex, int newIndex)
        {
            if (oldIndex == newIndex) return;

            var item = list[oldIndex];
            list.RemoveAt(oldIndex);
            list.Insert(newIndex, item);
        }

        public static List<T> RemoveLast<T>(this List<T> list, int count)
        {
            var len = list.Count;

            count = Mathf.Min(count, len);

            var from = len - count;

            var range = list.GetRange(from, count);

            list.RemoveRange(from, count);

            return range;
        }

        public static T RemoveLast<T>(this List<T> list)
        {
            var index = list.Count - 1;

            var last = list[index];

            list.RemoveAt(index);

            return last;
        }

        public static void Swap<T>(this List<T> list, int indexOfFirst)
        {
            (list[indexOfFirst + 1], list[indexOfFirst]) = (list[indexOfFirst], list[indexOfFirst + 1]);
        }

        public static void Swap<T>(this IList<T> list, int indexA, int indexB)
        {
            (list[indexB], list[indexA]) = (list[indexA], list[indexB]);
        }

        public static bool IsNullOrEmpty(this ICollection list) => list == null || list.Count == 0;

        public static bool IsNullOrEmpty<T>(this HashSet<T> hashSet) => hashSet == null || hashSet.Count == 0;

        #endregion

        #region Array Management
        public static T TryGetLast<T>(this T[] array)
        {

            if (array.IsNullOrEmpty())
                return default;

            return array[^1];

        }

        public static bool TryGetValue<T>(this T[] array, int index, out T value)
        {
            if (array == null || array.Length <= index || index < 0)
            {
                value = default;
                return false;
            }

            value = array[index];
            return true;
        }

        public static T TryGet<T>(this T[] array, int index, T defaultValue = default)
        {
            if (array == null || array.Length <= index || index < 0)
                return defaultValue;

            return array[index];
        }

        public static T[] GetCopy<T>(this T[] args)
        {
            var temp = new T[args.Length];
            args.CopyTo(temp, 0);
            return temp;
        }

        public static void Swap<T>(ref T[] array, int a, int b)
        {
            if (array == null || a >= array.Length || b >= array.Length || a == b) return;

            (array[b], array[a]) = (array[a], array[b]);
        }

        public static void Resize<T>(ref T[] args, int to)
        {
            var temp = new T[to];
            if (args != null)
                Array.Copy(args, 0, temp, 0, Mathf.Min(to, args.Length));

            args = temp;
        }

        public static void ExpandBy<T>(ref T[] array, int add)
        {
            T[] tempArray;
            if (array != null)
            {
                tempArray = new T[array.Length + add];
                array.CopyTo(tempArray, 0);
            }
            else tempArray = new T[add];
            array = tempArray;
        }

        public static void Remove<T>(ref T[] args, int ind)
        {
            var temp = new T[args.Length - 1];
            Array.Copy(args, 0, temp, 0, ind);
            var count = args.Length - ind - 1;
            Array.Copy(args, ind + 1, temp, ind, count);
            args = temp;
        }

        public static void AddAndInit<T>(ref T[] args, int add)
        {
            T[] temp;
            if (args != null)
            {
                temp = new T[args.Length + add];
                args.CopyTo(temp, 0);
            }
            else temp = new T[add];
            args = temp;
            for (var i = args.Length - add; i < args.Length; i++)
                args[i] = Activator.CreateInstance<T>();
        }

        #endregion

        #region Dictionaries

        public static TValue GetOrSet<TKey, TValue>(this IDictionary<TKey, TValue> dic, TKey key, TValue defaultVal) 
        {
            if (dic == null || key == null)
            {
                Debug.LogError("Dictionary of {0}{1} {2} for key {3}".F(nameof(TKey), nameof(TValue), (dic == null ? "IS NULL" : ""), key.GetNameForInspector()));
                return default;
            }

            if (dic.TryGetValue(key, out var val))
                return val;

            dic.Add(key, defaultVal);

            return defaultVal;
        }

        public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dic, TKey key) where TValue : new()
        {
            if (dic == null)
            {
                Debug.LogError("Dictionary of {0}-{1} is null".F(typeof(TKey).ToPegiStringType(), typeof(TValue).ToPegiStringType()));
                return default;
            }

            if (key == null)
            {
                Debug.LogError("Key is NULL for Dictionary of {0}-{1}".F(typeof(TKey).ToPegiStringType(), typeof(TValue).ToPegiStringType()));
                return default;
            }

            TValue val;

            if (dic.TryGetValue(key, out val)) 
                return val;

            val = new TValue();
            dic.Add(key, val);

            return val;
        }

        public static TValue TryGet<TKey, TValue>(this Dictionary<TKey, TValue> dic, TKey key)
        {
            if (dic == null || key == null) 
            {
                //Debug.LogError("Dictionary of {0}{1} {2} for key {3}".F(nameof(TKey), nameof(TValue), (dic == null ? "IS NULL" : ""), key.GetNameForInspector()));
                return default;
            }
            TValue value;
            dic.TryGetValue(key, out value);
            return value;
        }

        public static TValue TryGet<TKey, TValue>(this Dictionary<TKey, TValue> dic, TKey key, TValue defaultValue)
        {
            if (dic == null || key == null)
            {
                //Debug.LogError("Dictionary of {0}{1} {2} for key {3}".F(nameof(TKey), nameof(TValue), (dic == null ? "IS NULL" : ""), key.GetNameForInspector()));
                return default;
            }

            TValue value;
            if (dic.TryGetValue(key, out value))
                return value;

            return defaultValue;
        }

        #endregion

        #region String Editing

        #region TextOperations

        private const string BadFormat = "!Bad format: ";

        public const string NonBreakableString = "\u00A0";

        public static string F(this string format, Type type)
        {
            try
            {
                return string.Format(format, type.ToPegiStringType());
            }
            catch
            {
                return BadFormat + format + " " + (type == null ? "null type" : type.ToString());
            }
        }

        public static string F(this string format, Func<string> func)
        {
            string result;

            try
            {
                result = func();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                result = "ERR";
            }

            return format.F(result);
        }

        public static string F(this string format, string obj)
        {
            try
            {
                return string.Format(format, obj);
            }
            catch
            {
                return BadFormat + format + " " + obj;
            }
        }
        public static string F(this string format, object obj1)
        {
            try
            {
                return string.Format(format, obj1.GetNameForInspector());
            }
            catch
            {
                return BadFormat + format + " " + obj1.GetNameForInspector();
            }
        }
        public static string F(this string format, string obj1, string obj2)
        {
            try
            {
                return string.Format(format, obj1, obj2);
            }
            catch
            {
                return BadFormat + format + " " + obj1 + " " + obj2;
            }
        }
        public static string F(this string format, object obj1, object obj2)
        {
            try
            {
                return string.Format(format, obj1.GetNameForInspector(), obj2.GetNameForInspector());
            }
            catch
            {
                return BadFormat + format;
            }
        }
        public static string F(this string format, string obj1, string obj2, string obj3)
        {
            try
            {
                return string.Format(format, obj1, obj2, obj3);
            }
            catch
            {
                return BadFormat + format;
            }
        }
        public static string F(this string format, object obj1, object obj2, object obj3)
        {
            try
            {
                return string.Format(format, obj1.GetNameForInspector(), obj2.GetNameForInspector(), obj3.GetNameForInspector());
            }
            catch
            {
                return BadFormat + format;
            }
        }
        public static string F(this string format, params object[] objs)
        {
            string[] converted = new string[objs.Length];

            for (int i =0; i<objs.Length; i++) 
            {
                var o = objs[i];
                string res;
                try
                {
                    res = o.GetNameForInspector();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    res = "ERR";
                }

                converted[i] = res;
            }

            return format.F(converted);
        }

        public static string F(this string format, params string[] objs)
        {
            try
            {
                return string.Format(format, objs);
            }
            catch
            {
                return BadFormat + format;
            }
        }

        #endregion

        public static string GetFileNameFromPath(string text) 
        {
            if (text.IsNullOrEmpty())
                return "";

            int lastBack = text.LastIndexOf('/');
            int lastFront = text.LastIndexOf('\\');

            var maxSlash = Math.Max(lastBack, lastFront);

            if (maxSlash >= 0) 
            {
                text = text[(maxSlash + 1)..];
            }

            var dot = text.IndexOf('.');
            if (dot >= 0) 
            {
                text = text[..dot];
            }

            return text;
        }

        public static string FixDecimalSeparator(string text)
        {
            var separator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

            return separator switch
            {
                "." => text.Replace(",", "."),
                _ => text.Replace(".", ","),
            };
        }

        public static string ReplaceFirst(string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text[..pos] + replace + text[(pos + search.Length)..];
        }

        public static string AddSpacesToSentence(string text, bool preserveAcronyms = true)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;
            StringBuilder newText = new(text.Length * 2);
            newText.Append(text[0]);
            char previousCharacter = text[0];
            bool wasUnderscore = false;
                 
            for (int i = 1; i < text.Length; i++)
            {
                var currentCharacter = text[i];
                if (char.IsUpper(currentCharacter))
                {
                    if ((previousCharacter != ' ' && !char.IsUpper(previousCharacter)) ||
                        (preserveAcronyms && char.IsUpper(previousCharacter) &&
                         i < text.Length - 1 && !char.IsUpper(text[i + 1])))
                        newText.Append(' ');

                    if (!wasUnderscore)
                    {
                        currentCharacter = char.ToLower(currentCharacter);
                    }
                }
                else
                {
                    if (currentCharacter == '_')
                    {
                        newText.Append(' ');
                        previousCharacter = ' ';
                        wasUnderscore = true;
                        continue;
                    }
                }

                wasUnderscore = false;

                previousCharacter = currentCharacter;

                newText.Append(currentCharacter);
            }
            return newText.ToString();
        }

        public static string AddSpacesInsteadOfCapitals(string text, bool keepCatipals = false)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";
            StringBuilder newText = new(text.Length * 2);
            newText.Append(text[0]);

            if (keepCatipals)
            {
                for (int i = 1; i < text.Length; i++)
                {
                    if (char.IsUpper(text[i]) && text[i - 1] != ' ')
                        newText.Append(' ');

                    newText.Append(text[i]);
                }
            }
            else
            {
                for (int i = 1; i < text.Length; i++)
                {
                    if (char.IsUpper(text[i]) && text[i - 1] != ' ')
                    {
                        newText.Append(' ');
                        newText.Append(char.ToLower(text[i]));

                    }
                    else newText.Append(text[i]);
                }
            }

            return newText.ToString();
        }

        public static string FirstLine(this string str) => new StringReader(str).ReadLine();

        public static string ToPegiStringType(this Type type)
        {
            if (type == null)
                return "NULL Type";

            if (type.IsGenericType)
            {
                var ind = type.Name.IndexOf("`");

                if (ind <= 0)
                    return type.Name;


                string genericArguments = type.GetGenericArguments()
                                    .Select(x => x.Name)
                                    .Aggregate((x1, x2) => $"{x1}, {x2}");
                return $"{type.Name[..ind]}"
                     + $"<{genericArguments}>";
            }

            return type.Name;
        }
        public static string SimplifyTypeName(this string name)
        {
            if (name == null)
                return "TYPE IS A NULL STRING";
            if (name.Length == 0)
                return "TYPE IS EMPTY STRING";

            var ind = Mathf.Max(name.LastIndexOf(".", StringComparison.Ordinal), name.LastIndexOf("+", StringComparison.Ordinal));
            return (ind == -1 || ind == name.Length -1) ? name : name[(ind + 1)..];
        }

        public static string SimplifyDirectory(this string name)
        {
            var ind = name.LastIndexOf("/", StringComparison.Ordinal);
            return (ind == -1 || ind > name.Length - 2) ? name : name[(ind + 1)..];
        }

        public static string KeyToReadablaString(string label) 
        {
            if (label.IsNullOrEmpty())
                return label;

            StringBuilder sb = new(label.Length + 5);

            for (int i=0; i<label.Length; i++)
            {
                var c = label[i];

                switch (c)
                {
                    case '_': if (i > 0) sb.Append(' '); break;
                    case '.': sb.Append(' '); break;
                    default:  sb.Append(c); break;

                }
            }

            return sb.ToString();
        }

        public static string ToElipsisString(string text, int maxLength)
        {

            if (text == null)
                return "null";

            int index = text.IndexOf(Environment.NewLine, StringComparison.Ordinal);

            if (index > 10)
                text = text[..index];

            if (text.Length < (maxLength + 3))
                return text;

            return text[..maxLength] + "…";

        }

        public static bool SameAs(this string s, string other) => s?.Equals(other) ?? other == null;

        public static bool IsSubstringOf(this string text, string biggerText,
            RegexOptions opt = RegexOptions.IgnoreCase)
        {
            try
            {
                if (Regex.IsMatch(biggerText, text, opt))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                QcLog.ChillLogger.LogErrorOnce(() => "Is Substring of({0} -> {1}) Error {2}".F(text, biggerText, ex.ToString()), key: text);
            }

            return false;
        }

        public static int FindMostSimilarFrom(string s, string[] t)
        {
            var mostSimilar = -1;
            var distance = 999;
            for (var i = 0; i < t.Length; i++)
            {
                var newDistance = s.LevenshteinDistance(t[i]);
                if (newDistance >= distance) continue;
                mostSimilar = i;
                distance = newDistance;
            }
            return mostSimilar;
        }

        private static int LevenshteinDistance(this string s, string t)
        {

            if (s == null || t == null)
            {
                Debug.Log("Compared string is null: " + (s == null) + " " + (t == null));
                return 999;
            }

            if (s.Equals(t))
                return 0;

            var n = s.Length;
            var m = t.Length;
            var d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
                return m;

            if (m == 0)
                return n;

            // Step 2
            for (var i = 0; i <= n; d[i, 0] = i++) { }

            for (var j = 0; j <= m; d[0, j] = j++) { }

            // Step 3
            for (var i = 1; i <= n; i++)
                for (var j = 1; j <= m; j++)
                {
                    var cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }

            return d[n, m];
        }

        public static bool IsNullOrEmpty(this string s) => string.IsNullOrEmpty(s);

        #endregion

        public static bool IsDefaultOrNull<T>(T obj) => (obj == null) || EqualityComparer<T>.Default.Equals(obj, default);

        public static float RoundTo(this float val, int digits) => (float)Math.Round(val, digits);

        public static void SetMaximumLength<T>(List<T> list, int length)
        {
            while (list.Count > length)
                list.RemoveAt(0);
        }

        public static T MoveFirstToLast<T>(List<T> list)
        {
            var item = list[0];
            list.RemoveAt(0);
            list.Add(item);
            return item;
        }

        public static int CharToInt(this char c) => c - '0';

        public static IDisposable SetTemporaryValueDisposable<T> (T valueToSet, Action<T> contextSetter, Func<T> previousValue)
            => new TemporaryContextSetterGeneric<T>(valueToSet, contextSetter, previousValue);

        public static IDisposable DisposableAction(Action onDispose) => new DisposableActionToken(onDispose);

        public static T GetEnumFromStringByDistance<T>(string name)
        {
            int index = FindMostSimilarFrom(name, Enum.GetNames(typeof(T)));
            return (T)Enum.GetValues(typeof(T)).GetValue(index);
        }

        private class DisposableActionToken : IDisposable
        {
            readonly Action onDispose;
            public void Dispose()
            {
                try
                {
                    onDispose?.Invoke();
                } catch (Exception ex) 
                {
                    Debug.LogException(ex);
                }
            }

            public DisposableActionToken(Action action) 
            {
                onDispose = action;
            }
        }

        private class TemporaryContextSetterGeneric<T> : IDisposable
        {
            private readonly T _previousValue;
            private readonly Action<T> _setter;
            public void Dispose()
            {
                _setter.Invoke(_previousValue);
            }

            public TemporaryContextSetterGeneric(T valueToSet, Action<T> contextSetter, Func<T> previousValue)
            {
                _previousValue = previousValue.Invoke();
                _setter = contextSetter;
                _setter.Invoke(valueToSet);
            }
        }
    }
}

