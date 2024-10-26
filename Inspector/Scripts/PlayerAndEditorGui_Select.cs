using QuizCanners.Migration;
using QuizCanners.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace QuizCanners.Inspect
{
#pragma warning disable IDE1006 // Naming Styles
    public static partial class pegi
    {
  
       /* private static T filterEditorDropdown<T>(this T obj)
        {
            var edd = obj as IInspectorDropdown;
            return (edd == null || edd.ShowInInspectorDropdown()) ? obj : default;
        }*/

        private static string CompileSelectionName<T>(int index, T obj, bool showIndex, bool stripSlashesAndDots = false, bool dotsToSlashes = true)
        {
            var st = obj.GetNameForInspector();

            if (stripSlashesAndDots) 
                st = st.SimplifyDirectory();

            if (dotsToSlashes)
                st = st.Replace('.', '/');

            return (showIndex || st.Length == 0) ? "{0}: {1}".F(index, st) : st;
        }

        /*
        private static ChangesToken selectFinal_Internal<T>(T val, ref int index)
        {
            var count = namesList.Count;

            if (index == -1 && !val.IsNullOrDestroyed_Obj())
            {
                index = namesList.Count;
                namesList.Add("[{0}]".F(val.GetNameForInspector()));
            }

            var tmp = index;

            if (Select(ref tmp, namesList.ToArray()) && tmp < count)
            {
                index = tmp;
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }*/

        private static ChangesToken selectFinal_InternalDeprecated<T>(T val, ref int index,  List<string> namesList)
        {
            var count = namesList.Count;

            if (index == -1 && !val.IsNullOrDestroyed_Obj())
            {
                index = namesList.Count;
                namesList.Add("[{0}]".F(val.GetNameForInspector()));
            }

            var tmp = index;

            if (Select_Deprecated(ref tmp, namesList.ToArray()) && tmp < count)
            {
                index = tmp;
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }

        #region Select From Int List

        public static ChangesToken SelectPow2(this TextLabel label, ref int current, int min, int max)
        {
            label.ApproxWidth().Write();
            return SelectPow2(ref current, min, max);
        }

        public static ChangesToken SelectPow2(ref int current, int min, int max)
        {
            List<int> tmp = new(4);
            min = Mathf.NextPowerOfTwo(min);

            while (min <= max)
            {
                tmp.Add(min);
                min = Mathf.NextPowerOfTwo(min + 1);
            }

            return Select(ref current, tmp);
        }

        internal static ChangesToken Select(ref int value, List<int> list) => Select(ref value, list.ToArray());

        public static ChangesToken Select(this TextLabel text, ref int value, int minInclusive, int maxInclusive)
        {
            Write(text);
            return Select(ref value, minInclusive, maxInclusive);
        }

        public static ChangesToken Select(ref int value, int minInclusive, int maxInclusive)
        {
            var cnt = maxInclusive - minInclusive + 1;

            var tmp = value;
            var array = new int[cnt];
            for (var i = 0; i < cnt; i++)
                array[i] = minInclusive + i;

            if (Select(ref tmp, array))
            {
                value = tmp;
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }

        public static ChangesToken Select(ref int val, int[] arr, bool showIndex = false, bool stripSlashes = false, bool dotsToSlashes = true)
        {
            CheckLine();

            var listNames = new List<string>(arr.Length + 1);

            int tmp = -1;

            for (var i = 0; i < arr.Length; i++)
            {
                var el = arr[i];
                if (el == val)
                    tmp = i;
                listNames.Add(CompileSelectionName(i, el, showIndex, stripSlashes, dotsToSlashes));
            }

            if (selectFinal_InternalDeprecated(val, ref tmp, listNames))
            {
                val = arr[tmp];
                return ChangesToken.True;
            }

            return ChangesToken.False;

        }

        #endregion

        #region From Strings

        public static ChangesToken Select(ref int no, List<string> from)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return from.IsNullOrEmpty() ? "Selecting from null:".ConstLabel().Edit(ref no) : PegiEditorOnly.Select(ref no, from.ToArray());
#endif

            if (from.IsNullOrEmpty()) return ChangesToken.False;

            IsFoldout(QcSharp.TryGet(from, no, "...").PegiLabel());

            if (FoldoutManager.isFoldedOutOrEntered)
            {
                if (from.Count > 1)
                    Nl();
                for (var i = 0; i < from.Count; i++)
                    if (i != no && "{0}: {1}".F(i, from[i]).PegiLabel().ClickUnFocus().Nl())
                    {
                        no = i;
                        FoldoutManager.FoldInNow();
                        return ChangesToken.True;
                    }
            }

            GUILayout.Space(10);

            return ChangesToken.False;

        }

        public static ChangesToken Select(this TextLabel text, ref int value, string[] array)
        {
            Write(text);
            return Select_Deprecated(ref value, array);
        }

        public static ChangesToken SelectFlags(ref int no, string[] from, int width = -1)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return width > 0 ? PegiEditorOnly.SelectFlags(ref no, from, width) : PegiEditorOnly.SelectFlags(ref no, from);
#endif

            "Flags Only in Editor for now".PegiLabel().Write();

            return ChangesToken.False;
        }

        private static string tmpSelectSearch;

        private static int SEARCH_SELECTIONTHOLD => PaintingGameViewUI ? 8 : 16;

        private delegate void GetArrayAndCurrent(out string[] elements, ref int index);

        private static ChangesToken Select(ref int current, string display, int length, GetArrayAndCurrent getter, int width = -1)
        {
            var needSearch = length > SEARCH_SELECTIONTHOLD;

#if UNITY_EDITOR
            if (!PaintingGameViewUI && !needSearch)
            {
                getter.Invoke(out var fromEd, ref current);

                return width > 0 ?
                    PegiEditorOnly.Select(ref current, fromEd, width) :
                    PegiEditorOnly.Select(ref current, fromEd);
            }
#endif

            if (length == 0)
                return ChangesToken.False;

        
            if (!PaintingGameViewUI)
                " ".PegiLabel(10).Write();

            using (FoldoutManager.StartFoldoutDisposable(out var isFoldedOut))
            {
                if (!isFoldedOut)
                {
                    if (display.ConstLabel().ClickLabel() | Icon.Search.Click())
                        FoldoutManager.FoldOutNow();
                }
                else
                    Icon.FoldedOut.Click(FoldoutManager.FoldInNow);
            }


            if (!FoldoutManager.isFoldedOutOrEntered) 
            {
                GUILayout.Space(10);
                return ChangesToken.False;
            }
            
            getter.Invoke(out var from, ref current);

            if (needSearch)
            {

                Edit(ref tmpSelectSearch);
                Icon.Search.Draw();
                Nl();
            }

            if (from.Length > 1)
                Nl();

            bool searching = needSearch && !tmpSelectSearch.IsNullOrEmpty();

            int maxCount = (from.Length > SEARCH_SELECTIONTHOLD * 2)
                ? (int)(SEARCH_SELECTIONTHOLD * 1.5)
                : from.Length;

            int shownCount = 0;
            bool currentShown = false;
            int tmpCurrent = current;

            if (current >= 0 && current < from.Length && current >= maxCount) 
                ShowCurrent();
            
            for (var i = 0; i < from.Length; i++)
            {
                if (i == current)
                {
                    if (!currentShown)
                        ShowCurrent();
                    continue;
                }

                if (!searching || tmpSelectSearch.IsSubstringOf(from[i]))
                {
                    shownCount++;

                    if (from[i].PegiLabel().ClickUnFocus().Nl())
                    {
                        current = i;
                        return ChangesToken.True;
                    }

                    if (shownCount >= maxCount)
                        break;
                }
            }

            if (shownCount < from.Length)
                "...{0} more items".F(from.Length- shownCount).PegiLabel(Styles.HeaderText).Nl();

            GUILayout.Space(10);
            return ChangesToken.False;

            void ShowCurrent()
            {
                using (SetGuiColorDisposable(Color.yellow))
                {
                    "[{0}]".F(from[tmpCurrent]).PegiLabel().ClickUnFocus().Nl(FoldoutManager.FoldInNow);
                }
                shownCount++;
                currentShown = true;
            }
        }


        public static ChangesToken Select_Deprecated(ref int no, string[] from, int width = -1)
        {
            var needSearch = from.Length > SEARCH_SELECTIONTHOLD;

#if UNITY_EDITOR
            if (!PaintingGameViewUI && !needSearch)
                return width > 0 ?
                    PegiEditorOnly.Select(ref no, from, width) :
                    PegiEditorOnly.Select(ref no, from);
#endif

            if (from.IsNullOrEmpty())
                return ChangesToken.False;

            string hint = FoldoutManager.IsNextFoldedOut ? "{0} ... " : "{0} ... (foldout to select)";

            if (!PaintingGameViewUI)
                " ".PegiLabel(10).Write();

            from.TryGet(no, hint.F(no)).ConstLabel().IsFoldout();

            if (FoldoutManager.isFoldedOutOrEntered)
            {
                if (from.Length > 1)
                    Nl();

                if (needSearch)
                    "Search".PegiLabel(70).Edit(ref tmpSelectSearch).Nl();

                bool searching = needSearch && !tmpSelectSearch.IsNullOrEmpty();

                int maxCount = Mathf.Min(SEARCH_SELECTIONTHOLD + 4, from.Length);

                int shownIndex = 0;
                for (var i = 0; i < from.Length; i++)
                {
                    if (i == no)
                    {
                        "[{0}]".F(from[i]).PegiLabel().ClickUnFocus().Nl();
                        continue;
                    }

                    if (!searching || tmpSelectSearch.IsSubstringOf(from[i]))
                    {
                        shownIndex++;

                        if (from[i].PegiLabel().ClickUnFocus().Nl())
                        {
                            no = i;
                            return ChangesToken.True;
                        }

                        if (shownIndex >= maxCount)
                            break;
                    }
                }
            }

            GUILayout.Space(10);

            return ChangesToken.False;

        }

        public static ChangesToken Select(this TextLabel text, ref string value, HashSet<string> hashSet)
        {
            Write(text);
            return Select(ref value, hashSet);
        }

        public static ChangesToken Select(ref string value, HashSet<string> lst) => Select(ref value, lst == null ? new List<string>() : new List<string>(lst));

        public static ChangesToken Select(ref string val, List<string> lst)
        {
            if (lst.IsNullOrEmpty())
            {
                "{0} selecting from Empty list".F(val).PegiLabel().Write();
                return ChangesToken.False;
            }

            var ind = -1;

            bool anyValue = false;

            for (var i = 0; i < lst.Count; i++)
            {
                if (!lst[i].IsNullOrEmpty())
                {
                    anyValue = true;
                    if (lst[i].SameAs(val))
                    {
                        ind = i;
                        break;
                    }
                }
            }

            if (!anyValue)
            {
                "Can't select {0}. All values are empty.".F(val).PegiLabel().Write();
                return ChangesToken.False;
            }

            if (Select(ref ind, lst))
            {
                if (ind >= 0)
                {
                    val = lst[ind];
                    return ChangesToken.True;
                }
            }

            return ChangesToken.False;
        }

        #endregion

        #region UnityObject

        private static readonly Dictionary<System.Type, List<Object>> s_assets = new();

        public static ChangesToken SelectInAssets<T>(ref T obj) where T : Object
        {
            if (!s_assets.TryGetValue(typeof(T), out var objects)) 
            {
                objects = new List<Object>(QcUnity.FindAssetsByType<T>());
                s_assets[typeof(T)] = objects;
            }

            Object o = obj;

            var changed = ChangeTrackStart();

            if (Select(ref o, objects))
                obj = o as T;

            if (Icon.Refresh.Click("Refresh List"))
                s_assets.Remove(typeof(T));


            ClickHighlight(o);

            return changed;
        }

        public static ChangesToken Select(ref SortingLayer sortingLayer)
        {
            var indexes = new List<int>(SortingLayer.layers.Length + 1);
            var values = new List<string>(SortingLayer.layers.Length + 1);

            int selected = -1;

            foreach (var layer in SortingLayer.layers)
            {
                if (layer.Equals(sortingLayer))
                    selected = indexes.Count;

                indexes.Add(layer.id);
                values.Add("{0} [{1}]".F(layer.name, layer.value));
            }

            if (selectFinal_InternalDeprecated(sortingLayer, ref selected, values))
            {
                sortingLayer = SortingLayer.layers[selected];
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }

        private static readonly Dictionary<System.Type, List<Object>> s_objectsInScene = new();

        public static ChangesToken SelectInScene<T>(this TextLabel label, ref T obj) where T : Object
        {
            if (!s_objectsInScene.TryGetValue(typeof(T), out List<Object> objects))
            {
                objects = new List<Object>(Object.FindObjectsByType<T>(FindObjectsSortMode.None));
                s_objectsInScene[typeof(T)] = objects;
            }

            Object o = obj;

            var changed = ChangeTrackStart();

            if (label.ApproxWidth().Select(ref o, objects))
                obj = o as T;

            ClickHighlight(o);

            if (Icon.Refresh.Click("Refresh List"))
                s_objectsInScene.Remove(typeof(T)); 

            return changed;
        }

        public static ChangesToken SelectOrAdd<T>(this TextLabel label, ref int selected, ref List<T> objs) where T : Object
        {
            label.Write();
            return SelectOrAdd(ref selected, ref objs);
        }

        public static ChangesToken SelectOrAdd<T>(ref int selected, ref List<T> objcts) where T : Object
        {
            var changed = ChangeTrackStart();

            Select_Index(ref selected, objcts);

            var tex = objcts.TryGet(selected);

            if (Edit(ref tex, 100))
            {
                if (!tex)
                    selected = -1;
                else
                {
                    var ind = objcts.IndexOf(tex);
                    if (ind >= 0)
                        selected = ind;
                    else
                    {
                        selected = objcts.Count;
                        objcts.Add(tex);
                    }
                }
            }

            return changed;
        }

        #endregion

        #region Select Generic

        public static ChangesToken Select<T>(this TextLabel text, ref T value, List<T> list, bool showIndex = false, bool stripSlashes = false, bool allowInsert = true)
        {
            text.FallbackWidthFraction = 0.25f;
             Write(text);
            return Select(ref value, list, showIndex, stripSlashes, allowInsert);
        }

        public static ChangesToken Select<T>(ref T val, List<T> lst, bool showIndex = false, bool stripSlashes = false, bool allowInsert = true, bool dotsToSlashes = false)
        {
            var changed = ChangeTrackStart();

            CheckLine();

            lst ??= new List<T>();

            var names = new List<string>(lst.Count + 1);
            var indexes = new List<int>(lst.Count + 1);

            var currentIndex = -1;

            bool notInTheList = true;

            var currentIsNull = val.IsDefaultOrNull();

            if (lst != null)
            {
                for (var i = 0; i < lst.Count; i++)
                {
                    var tmp = lst[i];
                    if (tmp.IsDefaultOrNull()) 
                        continue;

                    if (!currentIsNull && tmp.Equals(val))
                    {
                        currentIndex = names.Count;
                        notInTheList = false;
                    }

                    var name = CompileSelectionName(i, tmp, showIndex: showIndex, stripSlashes, dotsToSlashes: dotsToSlashes);

                    names.Add(name);
                    indexes.Add(i);
                }

                if (selectFinal_InternalDeprecated(val, ref currentIndex, names))
                {
                    val = lst[indexes[currentIndex]];
                }
                else if (allowInsert && notInTheList && !currentIsNull && Icon.Insert.Click("Insert into list"))
                    lst.Add(val);
            }
            else
                val.GetNameForInspector().PegiLabel().Write();

            return changed;

        }

        public static ChangesToken Select_SameClass<T, G>(ref T val, List<G> lst, bool showIndex = false, bool allowInsert = true) where T : class where G : class
        {
            var changed = ChangeTrackStart();
            var same = typeof(T) == typeof(G);

            CheckLine();

            var namesList = new List<string>(lst.Count + 1);
            var indexList = new List<int>(lst.Count + 1);

            var current = -1;

            var notInTheList = true;

            var currentIsNull = val.IsNullOrDestroyed_Obj();

            for (var j = 0; j < lst.Count; j++)
            {
                var tmp = lst[j];
                if (tmp.IsDefaultOrNull() ||
                    (!same && !typeof(T).IsAssignableFrom(tmp.GetType()))) continue;

                if (!currentIsNull && tmp.Equals(val))
                {
                    current = namesList.Count;
                    notInTheList = false;
                }

                namesList.Add(CompileSelectionName(j, tmp, showIndex));
                indexList.Add(j);
            }

            if (selectFinal_InternalDeprecated(val, ref current, namesList))
                val = lst[indexList[current]] as T;
            else if (allowInsert && notInTheList && !currentIsNull && Icon.Insert.Click("Insert into list"))
                lst.TryAdd(val);

            return changed;

        }

        #endregion

        #region Select Index
        public static ChangesToken Select_Index<T>(this TextLabel text, ref int ind, List<T> lst, bool showIndex = false)
        {
            Write(text);
            return Select_Index(ref ind, lst, showIndex);
        }

        public static ChangesToken Select_Index<T>(this TextLabel text, ref int ind, T[] lst)
        {
            Write(text);
            return Select_Index(ref ind, lst);
        }

        public static ChangesToken Select_Index<T>(this TextLabel text, ref int ind, T[] lst, bool showIndex = false)
        {
            Write(text);
            return Select_Index(ref ind, lst, showIndex);
        }

        public static ChangesToken Select_Index<T>(this TextLabel text,  ref int ind, List<T> lst)
        {
            Write(text);
            return Select_Index(ref ind, lst);
        }

        public static ChangesToken Select_Index<T>(ref int ind, List<T> lst, int width) =>
#if UNITY_EDITOR
            (!PaintingGameViewUI) ?
                PegiEditorOnly.Select(ref ind, lst, width) :
#endif
                Select_Index(ref ind, lst);

        public static ChangesToken Select_Index<T>(ref int ind, List<T> lst, bool showIndex = false, bool stripSlashes = false, bool dotsToSlashes = true)
        {

            CheckLine();

            var namesList = new List<string>(lst.Count + 1);
            var indexes = new List<int>(lst.Count + 1);

            var current = -1;

            for (var j = 0; j < lst.Count; j++)
                if (!lst[j].IsNullOrDestroyed_Obj())
                {
                    if (ind == j)
                        current = indexes.Count;
                    namesList.Add(CompileSelectionName(j, lst[j], showIndex, stripSlashes, dotsToSlashes));
                    indexes.Add(j);
                }

            if (selectFinal_InternalDeprecated(ind, ref current, namesList))
            {
                ind = indexes[current];
                return ChangesToken.True;
            }

            return ChangesToken.False;

        }

        public static ChangesToken Select_Index<T>(ref int ind, T[] arr, bool showIndex = false, bool stripSlashes = false, bool dotsToSlashes = true)
        {

            CheckLine();

            var lnms = new List<string>(arr.Length + 1);

            if (arr.ClampIndexToCount(ref ind))
            {
                for (var i = 0; i < arr.Length; i++)
                    lnms.Add(CompileSelectionName(i, arr[i], showIndex, stripSlashes, dotsToSlashes));
            }

            return selectFinal_InternalDeprecated(ind, ref ind, lnms);

        }

        #endregion

        #region With Lambda
        public static ChangesToken Select<T>(this TextLabel label, ref int val, List<T> list, System.Func<T, bool> lambda, bool showIndex = false)
        {
            Write(label);
            return Select(ref val, list, lambda, showIndex);
        }

        public static ChangesToken Select<T>(this TextLabel text, ref T val, List<T> list, System.Func<T, bool> lambda, bool showIndex = false)
        {
            Write(text);
            return Select(ref val, list, lambda, showIndex);
        }

        public static ChangesToken Select<T>(ref int val, List<T> lst, System.Func<T, bool> lambda, bool showIndex = false, bool stripSlashes = false, bool dotsToSlashes = true)
        {

            CheckLine();

            var names = new List<string>(lst.Count + 1);
            var indexes = new List<int>(lst.Count + 1);

            var current = -1;

            for (var j = 0; j < lst.Count; j++)
            {
                var tmp = lst[j];

                if (tmp.IsDefaultOrNull() || !lambda(tmp)) continue;

                if (val == j)
                    current = names.Count;
                names.Add(CompileSelectionName(j, tmp, showIndex, stripSlashes, dotsToSlashes));
                indexes.Add(j);
            }


            if (selectFinal_InternalDeprecated(val, ref current, names))
            {
                val = indexes[current];
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }

        public static ChangesToken Select<T>(ref T val, List<T> lst, System.Func<T, bool> lambda, bool showIndex = false, bool stripSlashes = false, bool dotsToSlashes = true)
        {
            var changed = ChangeTrackStart(); 

            CheckLine();

            var namesList = new List<string>(lst.Count + 1);
            var indexList = new List<int>(lst.Count + 1);

            var current = -1;

            for (var j = 0; j < lst.Count; j++)
            {
                var tmp = lst[j];
                if (tmp.IsDefaultOrNull() || !lambda(tmp)) continue;

                if (current == -1 && tmp.Equals(val))
                    current = namesList.Count;

                namesList.Add(CompileSelectionName(j, tmp, showIndex, stripSlashes, dotsToSlashes));
                indexList.Add(j);
            }

            if (selectFinal_InternalDeprecated(val, ref current, namesList))
                val = lst[indexList[current]];

            return changed;

        }

        #endregion

        #region Select Type

        private static ChangesToken Select(ref System.Type val, List<System.Type> lst, string textForCurrent, bool showIndex = false, bool stripSlashes = false, bool dotsToSlashes = true)
        {
            CheckLine();

            var count = lst.Count;
            var names = new List<string>(count + 1);
            var indexes = new List<int>(count + 1);

            var current = -1;

            for (var j = 0; j < count; j++)
            {
                var tmp = lst[j];
                if (tmp.IsDefaultOrNull()) continue;

                if ((!val.IsDefaultOrNull()) && tmp == val)
                    current = names.Count;
                names.Add(CompileSelectionName(j, tmp, showIndex, stripSlashes, dotsToSlashes));
                indexes.Add(j);

            }

            if (current == -1 && val != null)
                names.Add(textForCurrent);

            if (Select_Deprecated(ref current, names.ToArray()) && (current < indexes.Count))
            {
                val = lst[indexes[current]];
                return ChangesToken.True;
            }

            return ChangesToken.False;

        }

        public static ChangesToken SelectType<T>(this TextLabel text, ref T el) where T : class, IGotClassTag
        {
            text.Write();

            object obj = el;

            var cfg = TaggedTypes<T>.DerrivedList;

            if (SelectType_Obj<T>(ref obj, cfg))
            {
                el = obj as T;
                return ChangesToken.True;
            }
            return ChangesToken.False;
        }

        private static ChangesToken SelectType_Obj<T>(ref object obj, TaggedTypes.DerrivedList cfg) where T : IGotClassTag
        {
            if (cfg == null)
            {
                "No Types Holder".PegiLabel().WriteWarning();
                return ChangesToken.False;
            }

            var type = obj?.GetType();

            if (cfg.Inspect_Select(ref type).Nl())
            {
                TaggedTypesExtensions.ChangeType(ref obj, type);
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }



        #endregion

        #region Dictionary

        public static ChangesToken Select(ref int current, Dictionary<int, string> from)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Select(ref current, from);

#endif

            var options = new string[from.Count];

            int ind = current;

            for (int i = 0; i < from.Count; i++)
            {
                var e = from.GetElementAt(i);
                options[i] = e.Value;
                if (current == e.Key)
                    ind = i;
            }

            if (Select_Deprecated(ref ind, options))
            {
                current = from.GetElementAt(ind).Key;
                return ChangesToken.True;
            }
            return ChangesToken.False;

        }

        public static ChangesToken Select(ref int current, Dictionary<int, string> from, int width)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Select(ref current, from, width);
#endif

            var options = new string[from.Count];

            var ind = current;

            for (var i = 0; i < from.Count; i++)
            {
                var e = from.GetElementAt(i);
                options[i] = e.Value;
                if (current == e.Key)
                    ind = i;
            }

            if (Select_Deprecated(ref ind, options, width))
            {
                current = from.GetElementAt(ind).Key;
                return ChangesToken.True;
            }
            return ChangesToken.False;

        }

        public static ChangesToken Select<TKey, TValue>(this TextLabel text, ref TKey key, Dictionary<TKey, TValue> from, bool showIndex = false)
        {
            Write(text);
            return Select(ref key, from, showIndex: showIndex);
        }

        public static ChangesToken Select<TKey, TValue>(this TextLabel text, ref TValue value, Dictionary<TKey, TValue> from, bool showIndex = false)
        {
            Write(text);
            return Select(ref value, from, showIndex: showIndex);
        }

        private static class SelectCaching 
        {
            public static ICollection collectionFiltered;
            public static ICollection keys;
            public static string[] names;
            public static int elementIndex;

            public static Gate.UnityTimeUnScaled SearchGate = new(Gate.InitialValue.StartArmed);

            public static void Clear() 
            {
                collectionFiltered = null;
                keys = null;
                names = null;
            }
        }

        public static ChangesToken Select<TKey, TValue>(ref TValue currentValue, Dictionary<TKey, TValue> from, bool showIndex = false)
        {
            CheckLine();

            if (from == null)
            {
                "Dictionary of {0} for {1} is null ".F(typeof(TValue).ToPegiStringType(), typeof(TKey).ToPegiStringType()).PegiLabel().Write();
                return ChangesToken.False;
            }

            TValue tmpCurValue = currentValue;
            List<TKey> keysList = null;
            int current = -1;

            string display;

            if (tmpCurValue == null)
                display = "";
            else
            {
                if (showIndex) 
                {
                    KeyValuePair<TKey, TValue> pair = from.FirstOrDefault(pair => pair.Value.Equals(tmpCurValue));

                    if (!pair.Equals(default(KeyValuePair<TKey, TValue>))) 
                    {
                        display = "{0} : {1}".F(pair.Key, pair.Value);
                    }
                    else
                        display = tmpCurValue.ToString();

                } else 
                display = tmpCurValue.ToString();
            }
            if (Select(ref current, display: display, from.Count, GetArrayAndInd))
            {
                SelectCaching.Clear();
                currentValue = from[keysList[current]];
                return ChangesToken.True;
            }

            return ChangesToken.False;

            void GetArrayAndInd(out string[] arr, ref int elementIndex)
            {
                if (SelectCaching.collectionFiltered == from && !SelectCaching.SearchGate.TryUpdateIfTimePassed(10))
                {
                    arr = SelectCaching.names;
                    keysList = (List<TKey>)SelectCaching.keys;
                    elementIndex = SelectCaching.elementIndex;
                    return;
                }

                var namesList = new List<string>(from.Count + 1);
                keysList = new List<TKey>(from.Count);

                elementIndex = -1;

                if (tmpCurValue == null)
                {
                    for (var i = 0; i < from.Count; i++)
                    {
                        var pair = from.GetElementAt(i);
                        CreateEntry(pair);
                    }
                }
                else
                {

                    for (var i = 0; i < from.Count; i++)
                    {
                        KeyValuePair<TKey, TValue> pair = from.GetElementAt(i);

                        if (tmpCurValue.Equals(pair.Value))
                            elementIndex = keysList.Count;

                        CreateEntry(pair);
                    }
                }

                arr = namesList.ToArray();

                SelectCaching.collectionFiltered = from;
                SelectCaching.keys = keysList;
                SelectCaching.names = arr;
                SelectCaching.elementIndex = elementIndex;

                return;

                void CreateEntry(KeyValuePair<TKey, TValue> pair)
                {
                    keysList.Add(pair.Key);

                    var valueName = pair.Value.GetNameForInspector();

                    if (!showIndex)
                    {
                        namesList.Add(valueName);
                        return;
                    }

                    string entryKey = pair.Key.ToString();

                    if (valueName.IndexOf(entryKey, System.StringComparison.CurrentCultureIgnoreCase) >= 0) //Contains(entryKey, System.StringComparison.CurrentCultureIgnoreCase))
                        namesList.Add(valueName);
                    else
                        namesList.Add("{0}: {1}".F(entryKey, valueName));
                }
            }

        }

        public static ChangesToken Select<TKey, TValue>(ref TKey currentKey, Dictionary<TKey, TValue> from, bool showIndex = false, 
            List<TKey> exclude = null, System.Func<KeyValuePair<TKey,TValue>, bool> optionValidator = null)
        {
            CheckLine();

            if (from == null)
            {
                "Dictionary of {0} for {1} is null ".F(typeof(TValue).ToPegiStringType(), typeof(TKey).ToPegiStringType()).PegiLabel().Write();
                return ChangesToken.False;
            }

            TKey tmpCurKey = currentKey;
            List<TKey> keysList = null;
            int current = -1;

            string display;

            if (tmpCurKey == null)
                display = "";
            else if (from.TryGetValue(tmpCurKey, out var val))
                display = showIndex ? "{0} : {1}".F(tmpCurKey.ToString(), val.ToString()) : val.ToString();
            else
                display = tmpCurKey.ToString();

            if (Select(ref current, display: display, from.Count, GetArrayAndInd))
            {
                SelectCaching.Clear();
                currentKey = keysList[current];
                return ChangesToken.True;
            }

            return ChangesToken.False;

            void GetArrayAndInd(out string[] arr, ref int elementIndex)
            {
                if (SelectCaching.collectionFiltered == from && !SelectCaching.SearchGate.TryUpdateIfTimePassed(10)) 
                {
                    arr = SelectCaching.names;
                    keysList = (List<TKey>)SelectCaching.keys;
                    elementIndex = SelectCaching.elementIndex;
                    return;
                }

                var namesList = new List<string>(from.Count + 1);
                keysList = new List<TKey>(from.Count);

                elementIndex = -1;

                if (tmpCurKey == null)
                {
                    for (var i = 0; i < from.Count; i++)
                    {
                        var pair = from.GetElementAt(i);

                        if (exclude != null && exclude.Contains(pair.Key))
                            continue;

                        if (optionValidator != null && !optionValidator(pair))
                            continue;

                        CreateEntry(pair);
                    }
                }
                else
                {

                    for (var i = 0; i < from.Count; i++)
                    {
                        KeyValuePair<TKey, TValue> pair = from.GetElementAt(i);

                        if (tmpCurKey.Equals(pair.Key))
                            elementIndex = keysList.Count;
                        else
                        {
                            if (optionValidator != null && !optionValidator(pair))
                                continue;

                            if (exclude != null && exclude.Contains(pair.Key))
                                continue;
                        }

                        CreateEntry(pair);
                    }
                }

                arr = namesList.ToArray();

                SelectCaching.collectionFiltered = from;
                SelectCaching.keys = keysList;
                SelectCaching.names = arr;
                SelectCaching.elementIndex = elementIndex;

                return;

                void CreateEntry(KeyValuePair<TKey, TValue> pair)
                {
                    keysList.Add(pair.Key);

                    var valueName = pair.Value.GetNameForInspector();

                    if (!showIndex)
                    {
                        namesList.Add(valueName);
                        return;
                    }

                    string entryKey = pair.Key.ToString();

                    if (valueName.IndexOf(entryKey, System.StringComparison.CurrentCultureIgnoreCase) >= 0) //Contains(entryKey, System.StringComparison.CurrentCultureIgnoreCase))
                        namesList.Add(valueName);
                    else
                        namesList.Add("{0}: {1}".F(entryKey, valueName));
                }
            }

        }

        #endregion

        #region Select Or Edit
        public static ChangesToken Select_or_edit_ColorPropertyName(this TextLabel name, ref string property, Material material)
        {
            name.Write();
            return Select_or_edit_TexturePropertyName(ref property, material);
        }

        public static ChangesToken Select_or_edit_ColorProperty(ref string property, Material material)
        {
            var lst = material.GetColorProperties();
            return lst.Count == 0 ? Edit(ref property) : Select(ref property, lst);
        }

        public static ChangesToken Select_or_edit_TexturePropertyName(this TextLabel name, ref string property, Material material)
        {
            name.Write();
            return Select_or_edit_TexturePropertyName(ref property, material);
        }

        public static ChangesToken Select_or_edit_TexturePropertyName(ref string property, Material material)
        {
            var lst = material.MyGetTexturePropertiesNames();
            return lst.Count == 0 ? Edit(ref property) : Select(ref property, lst);
        }

        public static ChangesToken Select_or_edit_TextureProperty(ref ShaderProperty.TextureValue property, Material material)
        {
            var lst = material.MyGetTextureProperties_Editor();
            return Select(ref property, lst, allowInsert: false);

        }

        public static ChangesToken Select_or_edit<T>(this TextLabel text, ref T obj, List<T> list, bool showIndex = false, bool stripSlahes = false, bool allowInsert = true) where T : Object
        {
            if (list.IsNullOrEmpty())
            {
                Write(text);
                return Edit(ref obj);
            }

            var changed = ChangeTrackStart();
            if (obj && Icon.Delete.ClickUnFocus())
                obj = null;

            Write(text);

            Select(ref obj, list, showIndex, stripSlahes, allowInsert);

            ClickHighlight(obj);

            return changed;
        }
        public static ChangesToken Select_or_edit<T>(this TextLabel name, ref int val, List<T> list, bool showIndex = false) =>
       list.IsNullOrEmpty() ? name.Edit(ref val) : name.Select_Index(ref val, list, showIndex);

        public static ChangesToken Select_or_edit<T>(ref T obj, List<T> list, bool showIndex = false) where T : Object
            => Select_or_edit(new TextLabel(), ref obj, list, showIndex);

  
        public static ChangesToken Select_or_edit(ref string val, List<string> list, bool showIndex = false, bool stripSlashes = true, bool allowInsert = true)
        {
            var changed = ChangeTrackStart();

            var gotList = !list.IsNullOrEmpty();

            var gotValue = !val.IsNullOrEmpty();

            if (gotList && gotValue && Icon.Delete.ClickUnFocus())
                val = "";

            if (!gotValue || !gotList)
                Edit(ref val);

            if (gotList)
                Select(ref val, list, showIndex, stripSlashes, allowInsert);

            return changed;
        }

        public static ChangesToken Select_or_edit(this TextLabel name, ref string val, List<string> list, bool showIndex = false)
        {
            var changed = ChangeTrackStart();

            var gotList = !list.IsNullOrEmpty();

            var gotValue = !val.IsNullOrEmpty();

            if (gotList && gotValue && Icon.Delete.ClickUnFocus())
                val = "";

            if (!gotValue || !gotList)
                name.Edit(ref val);

            if (gotList)
                name.Select(ref val, list, showIndex);

            return changed;
        }

        public static ChangesToken Select_SameClass_or_edit<T, G>(this TextLabel text, ref T obj, List<G> list) where T : Object where G : class
        {
            if (list.IsNullOrEmpty())
                return Edit(ref obj);

            var changed = ChangeTrackStart();

            if (obj && Icon.Delete.ClickUnFocus())
                obj = null;

            Write(text);

            Select_SameClass(ref obj, list);

            return changed;

        }

        #endregion

        #region Select IGotIndex
        public static ChangesToken Select_iGotIndex<T>(this TextLabel label, ref int ind, List<T> lst, bool showIndex = false) where T : IGotIndex
        {
            Write(label);
            return Select_iGotIndex(ref ind, lst, showIndex);
        }

        public static ChangesToken Select_iGotIndex<T>(ref int ind, List<T> lst, bool showIndex = false) where T : IGotIndex
        {

            if (lst.IsNullOrEmpty())
            {
                return Edit(ref ind);
            }

            var names = new List<string>(lst.Count + 1);
            var indexes = new List<int>(lst.Count + 1);

            var current = -1;

            foreach (var el in lst)
                if (!el.IsNullOrDestroyed_Obj())
                {
                    var index = el.IndexForInspector;

                    if (ind == index)
                        current = indexes.Count;
                    names.Add((showIndex ? index + ": " : "") + el.GetNameForInspector());
                    indexes.Add(index);

                }

            if (selectFinal_InternalDeprecated(ind, ref current, names))
            {
                ind = indexes[current];
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }

        #endregion

        #region Select IGotName

        public static ChangesToken Select_iGotDisplayName<T>(this TextLabel label, ref string name, List<T> lst) 
        {
            Write(label);
            return Select_iGotDisplayName(ref name, lst);
        }

        public static ChangesToken Select_iGotName<T>(this TextLabel label, ref string name, List<T> lst) where T : IGotName
        {
           
            Write(label);
            if (lst == null)
                return ChangesToken.False;
            return Select_iGotName(ref name, lst);
        }

        public static ChangesToken Select_iGotName<T>(ref string val, List<T> lst) where T : IGotName
        {

            if (lst == null)
                return ChangesToken.False;

            var namesList = new List<string>(lst.Count + 1);

            var current = -1;

            foreach (var el in lst)
                if (!el.IsNullOrDestroyed_Obj())
                {
                    var name = el.NameForInspector;

                    if (name == null) continue;

                    if (val != null && val.SameAs(name))
                        current = namesList.Count;
                    namesList.Add(name);

                }

            if (selectFinal_InternalDeprecated(val, ref current, namesList))
            {
                val = namesList[current];
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }

        public static ChangesToken Select_iGotDisplayName<T>(ref string val, List<T> lst)
        {

            if (lst == null)
                return ChangesToken.False;

            var namesList = new List<string>(lst.Count + 1);

            var current = -1;

            foreach (var el in lst)
                if (!el.IsNullOrDestroyed_Obj())
                {
                    var name = el.ToString();

                    if (name == null) continue;

                    if (val != null && val.SameAs(name))
                        current = namesList.Count;
                    namesList.Add(name);

                }

            if (selectFinal_InternalDeprecated(val, ref current, namesList))
            {
                val = namesList[current];
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }


        #endregion
    }
}