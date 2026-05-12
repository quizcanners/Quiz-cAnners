using QuizCanners.Inspect;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
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


        #region Html Tags (For Text Mesh Pro)

        public static class HtmlWrap 
        {
            public static string TagAndValue(string tag, string value) => "<{0}={1}>".F(tag, value);

            public static string Tag(string tag) => "<{0}>".F(tag);

            public static string TagWrapContent(string tag, string content) => content.IsNullOrEmpty() ? "" : "<{0}>{1}</{0}>".F(tag, content);

            public static string Bold( string content) => HtmlWrap.TagWrapContent("b", content);

            public static string Italics( string content) => HtmlWrap.TagWrapContent("i", content);

            public static string TagAndValueWrapContent(string tag, string tagValue, string content) => content.IsNullOrEmpty() ? "" : "<{0}={1}>{2}</{0}>".F(tag, tagValue, content);

            public static void TagAndValueWrapContent(string tag, string tagValue, string content, StringBuilder sb)
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


            public static string Color(string content, Color color) => content.IsNullOrEmpty() ? "" : "<{0}={1}>{2}</{0}>".F("color", "#" + ColorUtility.ToHtmlStringRGBA(color), content);

            public static string Alpha(float alpha01) => TagAndValue("alpha", "#" + Mathf.FloorToInt(Mathf.Clamp01(alpha01) * 255).ToString("X2"));

            public static string WrapAlpha(string content, float alpha01) => Alpha(alpha01) + content + Alpha(1f);
        }

     
        public static StringBuilder Append_Tab(this StringBuilder bld) => bld.Append('\t');

        public static StringBuilder Append_HtmlTag(this StringBuilder bld, string tag) => bld.Append(HtmlWrap.Tag(tag));

        public static StringBuilder Append_HtmlTag(this StringBuilder bld, string tag, string value) => bld.Append(HtmlWrap.TagAndValue(tag, value));

        public static StringBuilder Append_HtmlText(this StringBuilder bld, string tag, string value, string content) => bld.Append(HtmlWrap.TagAndValueWrapContent(tag, value, content));

        public static StringBuilder Append_HtmlText(this StringBuilder bld, string tag, string content) => bld.Append(HtmlWrap.TagWrapContent(tag, content));

        public static StringBuilder Append_HtmlColor(this StringBuilder bld, string content, Color col) => bld.Append(HtmlWrap.Color(content, col));

        public static StringBuilder Append_HtmlAlpha(this StringBuilder bld, string content, float alpha) => bld.Append(HtmlWrap.WrapAlpha(content, alpha));

        public static StringBuilder Append_HtmlBold(this StringBuilder bld, string content) => bld.Append(HtmlWrap.TagWrapContent("b", content));

        public static StringBuilder Append_HtmlItalics(this StringBuilder bld, string content) => bld.Append(HtmlWrap.TagWrapContent("i", content));

        public static StringBuilder AppendHtml(this StringBuilder bld, string content, Color col) => bld.Append(HtmlWrap.Color(content, col)); //content.IsNullOrEmpty() ? bld : bld.AppendHtmlText("color", "#"+ColorUtility.ToHtmlStringRGBA(col), content);

        public static StringBuilder Append_HtmlLink(this StringBuilder bld, string content) => content.IsNullOrEmpty() ? bld : bld.Append_HtmlText("link", "dummy", content);

        public static StringBuilder AppendHtmlLink(this StringBuilder bld, string content, Color col) => content.IsNullOrEmpty() ? bld :
            bld.Append_HtmlText("link", "dummy", HtmlWrap.Color(content, col));


        #endregion

        #region Enum

        public static T ToEnum<T>(int intValue) => (T)Enum.ToObject(typeof(T), intValue);

        #endregion

        #region Collection Management

        public static void EnqueueClearIfFull<T>(this System.Collections.Concurrent.ConcurrentQueue<T> queue, int maxCount, T value)
        {
            if (queue.Count >= maxCount)
            {
                if (queue.Count == maxCount)
                    Debug.LogError("The ConcurrentQueue of {0} is {1} elements. Clearing".F(typeof(T).ToPegiStringType(), maxCount));

                queue.Clear();
            }

            queue.Enqueue(value);
        }

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
//#pragma warning disable UNT0014 // Invalid type for call to GetComponent
                    conv = go.GetComponent<T>();
//#pragma warning restore UNT0014 // Invalid type for call to GetComponent
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

        public static T AddWithUniqueStringIdAndIndex<T>(IList<T> list) => AddWithUniqueStringIdAndIndex(list, "New " + typeof(T).ToPegiStringType());

        internal static T AddWithUniqueStringIdAndIndex<T>(IList<T> list, string name) =>
            AddWithUniqueStringIdAndIndex(list, (T)Activator.CreateInstance(typeof(T)), name);

        internal static T AddWithUniqueStringIdAndIndex<T>(IList<T> list, T e, string name)
        {
            AssignUniqueIndex(list, e);
            list.Add(e);

            if (e is IGotStringId named)
            {
                bool duplicate = false;

                var oldName = named.StringId;

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
                            if (el is IGotStringId existingName)
                            {
                                if (oldName.Equals(existingName.StringId))
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
                    named.StringId = name;
                    AssignUniqueStringId(e, list);
                }
            }
            return e;
        }

        private static void AssignUniqueStringId<T>(T el, IList<T> list)
        {
            if (el is not IGotStringId namedNewElement) 
                return;

            var newName = namedNewElement.StringId;
            var duplicate = true;
            var counter = 0;

            while (duplicate)
            {
                duplicate = false;

                foreach (var e in list)
                {
                    if (e is not IGotStringId currentName)
                        continue;

                    var otherName = currentName.StringId;

                    otherName ??= "";

                    if (e.Equals(el) || !newName.Equals(otherName))
                        continue;

                    duplicate = true;
                    counter++;
                    newName = namedNewElement.StringId + counter;
                    break;
                }
            }

            namedNewElement.StringId = newName;
        }

        public static List<T> SelectByNames<T>(this IEnumerable<T> collection, List<string> names) where T : class, IGotStringId
            => collection.Join(names, el => el.StringId, id => id, (e, i) => e).ToList();

        public static T GetElementAt<T>(this IEnumerable<T> source, int index) => source.ElementAt(index);


        public static T GetRandom<T>(this T[] arr, ref int previous)
        {
            if (arr.IsNullOrEmpty())
                return default;

            if (arr.Length == 1)
                return arr[0];

            var rnd = Random.Range(0, arr.Length);

            if (rnd == previous)
            {
                rnd = (rnd + 1) % arr.Length;
            }

            previous = rnd;
            return arr[previous];
        }

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

        public static bool ToggleContains<T>(this List<T> list, T value)
        {
            if (!list.Remove(value))
            {
                list.Add(value);
                return true;
            }

            return false;
        }

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

        public static bool TryGetLast<T>(this IList<T> list, out T el)
        {
            if (list == null || list.Count == 0)
            {
                el = default;
                return false;
            }

            el = list[list.Count - 1];
            return true;
        }

        public static bool TryGetValue<T>(this List<T> list, int index, out T result)
        {
            if (list == null || index < 0 || index >= list.Count)
            {
                result = default;
                return false;
            }

            result = list[index];
            return true;
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

        public static bool IsNullOrEmpty<T>(this ICollection<T> collection) => collection == null || collection.Count == 0;

        public static bool IsNullOrEmptyNonGeneric(this ICollection collection) => collection == null || collection.Count == 0;

        // public static bool IsNullOrEmpty(this ICollection collection)
        //  => collection == null || collection.Count == 0;

        /*
        public static bool IsNullOrEmpty(this ICollection list) => list == null || list.Count == 0;

        public static bool IsNullOrEmpty<T>(this HashSet<T> hashSet) => hashSet == null || hashSet.Count == 0;
        */
        public static void SetContains(this HashSet<int> vals, int index, bool contains)
        {
            if (contains)
                vals.Add(index);
            else
                vals.Remove(index);
        }

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

        public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dic, TKey key, out bool wasCreated) where TValue : new()
        {
            wasCreated = false;

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
            wasCreated = true;

            return val;
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

        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dic, TKey key)
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

        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dic, TKey key, TValue defaultValue)
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
#if UNITY_EDITOR
                return BadFormat + format + " " + (type == null ? "null type" : type.ToString());
#else
                if (QcLog.LogHandler.SavingLogs)
                    return BadFormat + format + " " + (type == null ? "null type" : type.ToString());
                return format;
#endif
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
#if UNITY_EDITOR
                Debug.LogException(ex);
#else
                if (QcLog.LogHandler.SavingLogs)
                    Debug.LogException(ex);
#endif
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
#if UNITY_EDITOR
                return BadFormat + format + " " + obj;
#else
                if (QcLog.LogHandler.SavingLogs)
                    return BadFormat + format + " " + obj;
                return format;
#endif
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
#if UNITY_EDITOR
                return BadFormat + format + " " + obj1.GetNameForInspector();
#else
                if (QcLog.LogHandler.SavingLogs)
                    return BadFormat + format + " " + obj1.GetNameForInspector();
                return format;
#endif
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
#if UNITY_EDITOR
                return BadFormat + format + " " + obj1 + " " + obj2;
#else
                if (QcLog.LogHandler.SavingLogs)
                    return BadFormat + format + " " + obj1 + " " + obj2;
                return format;
#endif
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
#if UNITY_EDITOR
                return BadFormat + format;
#else
                if (QcLog.LogHandler.SavingLogs)
                    return BadFormat + format;
                return format;
#endif
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
#if UNITY_EDITOR
                return BadFormat + format;
#else
                if (QcLog.LogHandler.SavingLogs)
                    return BadFormat + format;
                return format;
#endif
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
#if UNITY_EDITOR
                return BadFormat + format;
#else
                if (QcLog.LogHandler.SavingLogs)
                    return BadFormat + format;
                return format;
#endif
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
#if UNITY_EDITOR
                return BadFormat + format;
#else
                if (QcLog.LogHandler.SavingLogs)
                    return BadFormat + format;
                return format;
#endif
            }
        }

        #endregion

        public static string AddNewLinesIntoText(string text, int maxLineLength)
        {
            var formattedName = new System.Text.StringBuilder(text.Length + (text.Length / maxLineLength) * 2);

            int lineStart = 0;
            int addedStart = 0;

            for (int i = 0; i < text.Length; i++)
            {
                int indexOfWord = text.IndexOf(' ', i);

                if (indexOfWord < 0)
                {
                    formattedName.Append(text[addedStart..]);
                    break;
                }

                var length = indexOfWord - addedStart;

                if ((i - lineStart + length) > maxLineLength - 8)
                {
                    lineStart = i;
                    formattedName.Append('\n');
                }

                formattedName.Append(text[addedStart..indexOfWord]);
                addedStart = indexOfWord;

                i = indexOfWord;

                /*
                if ((i - lineStart) > maxLineLength - 16) //i > 0 && i % maxLineLength == 0)
                {
                    lineStart = i;
                    formattedName.Append('\n');
                }*/
            }

            return formattedName.ToString();
        }


        private static int GetLastSlashIndex(string text) 
        {
            int lastBack = text.LastIndexOf('/');
            int lastFront = text.LastIndexOf('\\');

            return Math.Max(lastBack, lastFront);
        }

        public static string GetPathToFile(string fullPath)
        {
            if (fullPath.IsNullOrEmpty())
                return "";

            var maxSlash = GetLastSlashIndex(fullPath);

            if (maxSlash >= 0)
                fullPath = fullPath[..maxSlash]; //[(maxSlash + 1)..];
            
            return fullPath;
        }

        public static string GetFileNameFromPath(string fullPath) 
        {
            if (fullPath.IsNullOrEmpty())
                return "";

            var maxSlash = GetLastSlashIndex(fullPath);

            if (maxSlash >= 0) 
            {
                fullPath = fullPath[(maxSlash + 1)..];
            }

            var dot = fullPath.IndexOf('.');
            if (dot >= 0) 
            {
                fullPath = fullPath[..dot];
            }

            return fullPath;
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

            var frst = text[0];

            if (frst != '_')
                newText.Append(frst);
            char previousCharacter = frst;
            bool wasUnderscore = false;
                 
            for (int i = 1; i < text.Length; i++)
            {
                char currentCharacter = text[i];
               
                bool TryGetNext(out char symbol) 
                {
                    if (i >= text.Length-1) 
                    {
                        symbol = ' ';
                        return false;
                    }

                    symbol = text[i+1];
                    return true;
                }

                bool ShouldPreserve(char symbol) => char.IsUpper(symbol) || char.IsNumber(symbol);

                if (char.IsUpper(currentCharacter))
                {
                    bool preserveWithPrevious = ShouldPreserve(previousCharacter);

                    if (preserveAcronyms 
                        && (preserveWithPrevious || (TryGetNext(out var nextCharacter) && ShouldPreserve(nextCharacter))))
                    {
                        if (!preserveWithPrevious)
                            newText.Append(' ');
                    }
                    else
                    {

                        if (previousCharacter != ' ' && !char.IsUpper(previousCharacter))
                            newText.Append(' ');

                        if (!wasUnderscore)
                        {
                            currentCharacter = char.ToLower(currentCharacter);
                        }
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

        public static List<int> SortByLevenshteinDistance<T>(this Dictionary<int, T> dic, string toCompareAgainst, Func<T, string> toStringFunc) 
        {
            if (dic == null || dic.Count == 0)
                return new List<int>();

            var scored = new List<(int key, int distance)>(dic.Count);

            foreach (var kvp in dic)
            {
                int distance = toStringFunc(kvp.Value).LevenshteinDistance(toCompareAgainst);
                scored.Add((kvp.Key, distance));
            }

            scored.Sort((a, b) => a.distance.CompareTo(b.distance));

            var result = new List<int>(scored.Count);

            for (int i = 0; i < scored.Count; i++)
            {
                result.Add(scored[i].key);
            }

            return result;
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

        public static float RoundTo(float val, int digits) => (float)Math.Round(val, digits);

        public static double RoundTo(double val, int digits) => Math.Round(val, digits);

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

        internal static string GetPropertyName<T>(Expression<Func<T>> memberExpression)
        {
            System.Reflection.MemberInfo member = ((MemberExpression)memberExpression.Body).Member;

            string name;

            switch (member.MemberType)
            {
                case System.Reflection.MemberTypes.Field: name = member.Name; break;
                case System.Reflection.MemberTypes.Property: name = "m_{0}{1}".F(char.ToUpper(member.Name[0]), member.Name[1..]); break;
                default: "Not Impl {0}".F(member.MemberType.ToString().SimplifyTypeName()).PL(90).Write(); return null;
            }

            return name;
        }

        public static string TRIM_TO_UPPER_KEY(this string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            int start = 0;
            int end = name.Length - 1;

            while (start <= end && !IsNameChar(name[start]))
                start++;

            while (end >= start && !IsNameChar(name[end]))
                end--;

            if (start > end)
                return string.Empty;

            int length = end - start + 1;

            return string.Create(length, (name, start), static (span, state) =>
            {
                string source = state.name;
                int src = state.start;

                for (int i = 0; i < span.Length; i++)
                {
                    char c = source[src + i];

                    // Fast ASCII uppercase
                    if ((uint)(c - 'a') <= ('z' - 'a'))
                        c = (char)(c - 32);
                    else
                        c = char.ToUpperInvariant(c);

                    span[i] = c;
                }
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsNameChar(char c)
        {
            // Fast ASCII path first
            if ((uint)(c - 'A') <= ('Z' - 'A')) return true;
            if ((uint)(c - 'a') <= ('z' - 'a')) return true;
            if ((uint)(c - '0') <= ('9' - '0')) return true;
            if (c == '_') return true; // keep underscore if you want

            // Fallback for Unicode letters/digits
            return char.IsLetterOrDigit(c);
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

