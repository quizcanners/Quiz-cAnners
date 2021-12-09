using QuizCanners.Migration;
using QuizCanners.Utils;
using System.Collections.Generic;
using UnityEngine;


namespace QuizCanners.Inspect
{
#pragma warning disable IDE1006 // Naming Styles
    public static partial class pegi
    {
  
        private static T filterEditorDropdown<T>(this T obj)
        {
            var edd = obj as IInspectorDropdown;
            return (edd == null || edd.ShowInInspectorDropdown()) ? obj : default;
        }

        private static string CompileSelectionName<T>(int index, T obj, bool showIndex, bool stripSlashesAndDots = false, bool dotsToSlashes = true)
        {
            var st = obj.GetNameForInspector();

            if (stripSlashesAndDots) 
                st = st.SimplifyDirectory();

            if (dotsToSlashes)
                st = st.Replace('.', '/');

            return (showIndex || st.Length == 0) ? "{0}: {1}".F(index, st) : st;
        }

        private static ChangesToken selectFinal_Internal<T>(T val, ref int index, List<string> namesList)
        {
            var count = namesList.Count;

            if (index == -1 && !val.IsNullOrDestroyed_Obj())
            {
                index = namesList.Count;
                namesList.Add("[{0}]".F(val.GetNameForInspector()));
            }

            var tmp = index;

            if (select(ref tmp, namesList.ToArray()) && tmp < count)
            {
                index = tmp;
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }

        #region Select From Int List

        public static ChangesToken selectPow2(this TextLabel label, ref int current, int min, int max)
        {
            label.ApproxWidth().write();
            return selectPow2(ref current, min, max);
        }

        public static ChangesToken selectPow2(ref int current, int min, int max)
        {
            List<int> tmp = new List<int>(4);
            min = Mathf.NextPowerOfTwo(min);

            while (min <= max)
            {
                tmp.Add(min);
                min = Mathf.NextPowerOfTwo(min + 1);
            }

            return select(ref current, tmp);
        }

        internal static ChangesToken select(ref int value, List<int> list) => select(ref value, list.ToArray());

        public static ChangesToken select(this TextLabel text, ref int value, int minInclusive, int maxInclusive)
        {
            write(text);
            return select(ref value, minInclusive, maxInclusive);
        }

        public static ChangesToken select(ref int value, int minInclusive, int maxInclusive)
        {
            var cnt = maxInclusive - minInclusive + 1;

            var tmp = value;
            var array = new int[cnt];
            for (var i = 0; i < cnt; i++)
                array[i] = minInclusive + i;

            if (select(ref tmp, array))
            {
                value = tmp;
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }

        public static ChangesToken select(ref int val, int[] arr, bool showIndex = false, bool stripSlashes = false, bool dotsToSlashes = true)
        {
            checkLine();

            var listNames = new List<string>(arr.Length + 1);

            int tmp = -1;

            for (var i = 0; i < arr.Length; i++)
            {
                var el = arr[i];
                if (el == val)
                    tmp = i;
                listNames.Add(CompileSelectionName(i, el, showIndex, stripSlashes, dotsToSlashes));
            }

            if (selectFinal_Internal(val, ref tmp, listNames))
            {
                val = arr[tmp];
                return ChangesToken.True;
            }

            return ChangesToken.False;

        }

        #endregion

        #region From Strings

        public static ChangesToken select(ref int no, List<string> from)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return from.IsNullOrEmpty() ? "Selecting from null:".PegiLabel(90).write().edit(ref no) : PegiEditorOnly.select(ref no, from.ToArray());
#endif

            if (from.IsNullOrEmpty()) return ChangesToken.False;

            isFoldout(QcSharp.TryGet(from, no, "...").PegiLabel());

            if (PegiEditorOnly.isFoldedOutOrEntered)
            {
                if (from.Count > 1)
                    nl();
                for (var i = 0; i < from.Count; i++)
                    if (i != no && "{0}: {1}".F(i, from[i]).PegiLabel().ClickUnFocus().nl())
                    {
                        no = i;
                        FoldInNow();
                        return ChangesToken.True;
                    }
            }

            GUILayout.Space(10);

            return ChangesToken.False;

        }

        public static ChangesToken select(this TextLabel text, ref int value, string[] array)
        {
            write(text);
            return select(ref value, array);
        }

        public static ChangesToken selectFlags(ref int no, string[] from, int width = -1)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return width > 0 ? PegiEditorOnly.selectFlags(ref no, from, width) : PegiEditorOnly.selectFlags(ref no, from);
#endif

            "Flags Only in Editor for now".PegiLabel().write();

            return ChangesToken.False;
        }

        private static string tmpSelectSearch;

        private static int SEARCH_SELECTIONTHOLD => PaintingGameViewUI ? 8 : 16;

        public static ChangesToken select(ref int no, string[] from, int width = -1)
        {
            var needSearch = from.Length > SEARCH_SELECTIONTHOLD;

#if UNITY_EDITOR
            if (!PaintingGameViewUI && !needSearch)
                return width > 0 ?
                    PegiEditorOnly.select(ref no, from, width) :
                    PegiEditorOnly.select(ref no, from);
#endif

            if (from.IsNullOrEmpty())
                return ChangesToken.False;

            string hint = PegiEditorOnly.IsNextFoldedOut ? "{0} ... " : "{0} ... (foldout to select)";

            if (!PaintingGameViewUI)
                " ".PegiLabel(10).write();

            from.TryGet(no, hint.F(no)).PegiLabel().isFoldout();

            if (PegiEditorOnly.isFoldedOutOrEntered)
            {
                if (from.Length > 1)
                    nl();

                if (needSearch)
                    "Search".PegiLabel(70).edit(ref tmpSelectSearch).nl();

                bool searching = needSearch && !tmpSelectSearch.IsNullOrEmpty();

                for (var i = 0; i < from.Length; i++)
                {
                    if (i != no)
                    {
                        if ((!searching || tmpSelectSearch.IsSubstringOf(from[i])) && from[i].PegiLabel().ClickUnFocus().nl())
                        {
                            no = i;
                            return ChangesToken.True;
                        }
                    } else 
                        "[{0}]".F(from[i]).PegiLabel().ClickUnFocus().nl();
                }
            }

            GUILayout.Space(10);

            return ChangesToken.False;

        }

        public static ChangesToken select(ref string val, List<string> lst)
        {
            var ind = -1;

            for (var i = 0; i < lst.Count; i++)
                if (lst[i] != null && lst[i].SameAs(val))
                {
                    ind = i;
                    break;
                }

            if (select(ref ind, lst))
            {
                val = lst[ind];
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }

        #endregion

        #region UnityObject

        public static ChangesToken select(ref SortingLayer sortingLayer)
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

            if (selectFinal_Internal(sortingLayer, ref selected, values))
            {
                sortingLayer = SortingLayer.layers[selected];
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }

        private static readonly Dictionary<System.Type, List<Object>> objectsInScene = new Dictionary<System.Type, List<Object>>();

        public static ChangesToken selectInScene<T>(this TextLabel label, ref T obj) where T : Object
        {
            if (!objectsInScene.TryGetValue(typeof(T), out List<Object> objects))
            {
                objects = new List<Object>(Object.FindObjectsOfType<T>());
                objectsInScene[typeof(T)] = objects;
            }

            Object o = obj;

            var changed = ChangeTrackStart();

            if (label.ApproxWidth().select(ref o, objects))
                obj = o as T;

            o.ClickHighlight();

            if (icon.Refresh.Click("Refresh List"))
                objectsInScene.Remove(typeof(T)); 

            return changed;
        }

        public static ChangesToken selectOrAdd<T>(this TextLabel label, ref int selected, ref List<T> objs) where T : Object
        {
            label.write();
            return selectOrAdd(ref selected, ref objs);
        }

        public static ChangesToken selectOrAdd<T>(ref int selected, ref List<T> objcts) where T : Object
        {
            var changed = ChangeTrackStart();

            select_Index(ref selected, objcts);

            var tex = objcts.TryGet(selected);

            if (edit(ref tex, 100))
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

        public static ChangesToken select<T>(this TextLabel text, ref T value, List<T> list, bool showIndex = false, bool stripSlashes = false, bool allowInsert = true)
        {

             write(text);
            return select(ref value, list, showIndex, stripSlashes, allowInsert);
        }

        public static ChangesToken select<T>(ref T val, List<T> lst, bool showIndex = false, bool stripSlashes = false, bool allowInsert = true, bool dotsToSlashes = false)
        {
            var changed = pegi.ChangeTrackStart();

            checkLine();

            if (lst == null)
                lst = new List<T>();

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
                    if (tmp.filterEditorDropdown().IsDefaultOrNull()) continue;

                    if (!currentIsNull && tmp.Equals(val))
                    {
                        currentIndex = names.Count;
                        notInTheList = false;
                    }

                    names.Add(CompileSelectionName(i, tmp, showIndex, stripSlashes, dotsToSlashes: dotsToSlashes));
                    indexes.Add(i);
                }

                if (selectFinal_Internal(val, ref currentIndex, names))
                {
                    val = lst[indexes[currentIndex]];
                }
                else if (allowInsert && notInTheList && !currentIsNull && icon.Insert.Click("Insert into list"))
                    lst.Add(val);
            }
            else
                val.GetNameForInspector().PegiLabel().write();

            return changed;

        }

        public static ChangesToken select_SameClass<T, G>(ref T val, List<G> lst, bool showIndex = false, bool allowInsert = true) where T : class where G : class
        {
            var changed = ChangeTrackStart();
            var same = typeof(T) == typeof(G);

            checkLine();

            var namesList = new List<string>(lst.Count + 1);
            var indexList = new List<int>(lst.Count + 1);

            var current = -1;

            var notInTheList = true;

            var currentIsNull = val.IsNullOrDestroyed_Obj();

            for (var j = 0; j < lst.Count; j++)
            {
                var tmp = lst[j];
                if (tmp.filterEditorDropdown().IsDefaultOrNull() ||
                    (!same && !typeof(T).IsAssignableFrom(tmp.GetType()))) continue;

                if (!currentIsNull && tmp.Equals(val))
                {
                    current = namesList.Count;
                    notInTheList = false;
                }

                namesList.Add(CompileSelectionName(j, tmp, showIndex));
                indexList.Add(j);
            }

            if (selectFinal_Internal(val, ref current, namesList))
                val = lst[indexList[current]] as T;
            else if (allowInsert && notInTheList && !currentIsNull && icon.Insert.Click("Insert into list"))
                lst.TryAdd(val);

            return changed;

        }

        #endregion

        #region Select Index
        public static ChangesToken select_Index<T>(this TextLabel text, ref int ind, List<T> lst, bool showIndex = false)
        {
            write(text);
            return select_Index(ref ind, lst, showIndex);
        }

        public static ChangesToken select_Index<T>(this TextLabel text, ref int ind, T[] lst)
        {
            write(text);
            return select_Index(ref ind, lst);
        }

        public static ChangesToken select_Index<T>(this TextLabel text, ref int ind, T[] lst, bool showIndex = false)
        {
            write(text);
            return select_Index(ref ind, lst, showIndex);
        }

        public static ChangesToken select_Index<T>(this TextLabel text,  ref int ind, List<T> lst)
        {
            write(text);
            return select_Index(ref ind, lst);
        }

        public static ChangesToken select_Index<T>(ref int ind, List<T> lst, int width) =>
#if UNITY_EDITOR
            (!PaintingGameViewUI) ?
                PegiEditorOnly.select(ref ind, lst, width) :
#endif
                select_Index(ref ind, lst);

        public static ChangesToken select_Index<T>(ref int ind, List<T> lst, bool showIndex = false, bool stripSlashes = false, bool dotsToSlashes = true)
        {

            checkLine();

            var namesList = new List<string>(lst.Count + 1);
            var indexes = new List<int>(lst.Count + 1);

            var current = -1;

            for (var j = 0; j < lst.Count; j++)
                if (!lst[j].filterEditorDropdown().IsNullOrDestroyed_Obj())
                {
                    if (ind == j)
                        current = indexes.Count;
                    namesList.Add(CompileSelectionName(j, lst[j], showIndex, stripSlashes, dotsToSlashes));
                    indexes.Add(j);
                }

            if (selectFinal_Internal(ind, ref current, namesList))
            {
                ind = indexes[current];
                return ChangesToken.True;
            }

            return ChangesToken.False;

        }

        public static ChangesToken select_Index<T>(ref int ind, T[] arr, bool showIndex = false, bool stripSlashes = false, bool dotsToSlashes = true)
        {

            checkLine();

            var lnms = new List<string>(arr.Length + 1);

            if (arr.ClampIndexToCount(ref ind))
            {
                for (var i = 0; i < arr.Length; i++)
                    lnms.Add(CompileSelectionName(i, arr[i], showIndex, stripSlashes, dotsToSlashes));
            }

            return selectFinal_Internal(ind, ref ind, lnms);

        }

        #endregion

        #region With Lambda
        public static ChangesToken select<T>(this TextLabel label, ref int val, List<T> list, System.Func<T, bool> lambda, bool showIndex = false)
        {
            write(label);
            return select(ref val, list, lambda, showIndex);
        }

        public static ChangesToken select<T>(this TextLabel text, ref T val, List<T> list, System.Func<T, bool> lambda, bool showIndex = false)
        {
            write(text);
            return select(ref val, list, lambda, showIndex);
        }

        public static ChangesToken select<T>(ref int val, List<T> lst, System.Func<T, bool> lambda, bool showIndex = false, bool stripSlashes = false, bool dotsToSlashes = true)
        {

            checkLine();

            var names = new List<string>(lst.Count + 1);
            var indexes = new List<int>(lst.Count + 1);

            var current = -1;

            for (var j = 0; j < lst.Count; j++)
            {
                var tmp = lst[j];

                if (tmp.filterEditorDropdown().IsDefaultOrNull() || !lambda(tmp)) continue;

                if (val == j)
                    current = names.Count;
                names.Add(CompileSelectionName(j, tmp, showIndex, stripSlashes, dotsToSlashes));
                indexes.Add(j);
            }


            if (selectFinal_Internal(val, ref current, names))
            {
                val = indexes[current];
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }

        public static ChangesToken select<T>(ref T val, List<T> lst, System.Func<T, bool> lambda, bool showIndex = false, bool stripSlashes = false, bool dotsToSlashes = true)
        {
            var changed = ChangeTrackStart(); 

            checkLine();

            var namesList = new List<string>(lst.Count + 1);
            var indexList = new List<int>(lst.Count + 1);

            var current = -1;

            for (var j = 0; j < lst.Count; j++)
            {
                var tmp = lst[j];
                if (tmp.filterEditorDropdown().IsDefaultOrNull() || !lambda(tmp)) continue;

                if (current == -1 && tmp.Equals(val))
                    current = namesList.Count;

                namesList.Add(CompileSelectionName(j, tmp, showIndex, stripSlashes, dotsToSlashes));
                indexList.Add(j);
            }

            if (selectFinal_Internal(val, ref current, namesList))
                val = lst[indexList[current]];

            return changed;

        }

        #endregion

        #region Select Type

        private static ChangesToken select(ref System.Type val, List<System.Type> lst, string textForCurrent, bool showIndex = false, bool stripSlashes = false, bool dotsToSlashes = true)
        {
            checkLine();

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

            if (select(ref current, names.ToArray()) && (current < indexes.Count))
            {
                val = lst[indexes[current]];
                return ChangesToken.True;
            }

            return ChangesToken.False;

        }

        public static ChangesToken selectType<T>(this TextLabel text, ref T el) where T : class, IGotClassTag
        {
            text.write();

            object obj = el;

            var cfg = TaggedTypes<T>.DerrivedList;

            if (selectType_Obj<T>(ref obj, cfg))
            {
                el = obj as T;
                return ChangesToken.True;
            }
            return ChangesToken.False;
        }

        private static ChangesToken selectType_Obj<T>(ref object obj, TaggedTypes.DerrivedList cfg) where T : IGotClassTag
        {
            if (cfg == null)
            {
                "No Types Holder".PegiLabel().writeWarning();
                return ChangesToken.False;
            }

            var type = obj?.GetType();

            if (cfg.Inspect_Select(ref type).nl())
            {
                TaggedTypesExtensions.ChangeType(ref obj, type);
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }



        #endregion

        #region Dictionary
        public static ChangesToken select<TKey, TValue>(ref TValue val, Dictionary<TKey, TValue> dic, bool showIndex = false, bool stripSlashes = false, bool allowInsert = true)
            => select(ref val, new List<TValue>(dic.Values), showIndex, stripSlashes, allowInsert);

        public static ChangesToken select(ref int current, Dictionary<int, string> from)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.select(ref current, from);

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

            if (select(ref ind, options))
            {
                current = from.GetElementAt(ind).Key;
                return ChangesToken.True;
            }
            return ChangesToken.False;

        }

        public static ChangesToken select(ref int current, Dictionary<int, string> from, int width)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.select(ref current, from, width);
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

            if (select(ref ind, options, width))
            {
                current = from.GetElementAt(ind).Key;
                return ChangesToken.True;
            }
            return ChangesToken.False;

        }

        public static ChangesToken select<TKey, TValue>(this TextLabel text, ref TKey key, Dictionary<TKey, TValue> from)
        {
            write(text);
            return select(ref key, from);
        }

        public static ChangesToken select<TKey, TValue>(ref TKey key, Dictionary<TKey, TValue> from)
        {
            checkLine();

            if (from == null)
            {
                "Dictionary of {0} for {1} is null ".F(typeof(TValue).ToPegiStringType(), typeof(TKey).ToPegiStringType()).PegiLabel().write();
                return ChangesToken.False;
            }

            var namesList = new List<string>(from.Count + 1);

            int elementIndex = -1;

            TValue val = default;

            if (key == null)
            {
                for (var i = 0; i < from.Count; i++)
                {
                    var pair = from.GetElementAt(i);

                    var pKey = pair.Key.ToString();
                    var pVal = pair.Value.GetNameForInspector();

                    namesList.Add(pVal.Contains(pKey) ? pVal : "{0}: {1}".F(pKey, pVal));
                }
            }
            else
            {
                for (var i = 0; i < from.Count; i++)
                {
                    var pair = from.GetElementAt(i);

                    if (key.Equals(pair.Key)) 
                        elementIndex = i;

                    var keyName = pair.Key.ToString();
                    var valueName = pair.Value.GetNameForInspector();

                    if (valueName.Contains(keyName))
                        namesList.Add(valueName);
                    else
                        namesList.Add("{0}: {1}".F(keyName, valueName));
                }
            }

            if (selectFinal_Internal(val, ref elementIndex, namesList))
            {
                key = from.GetElementAt(elementIndex).Key;
                return ChangesToken.True;
            }

            return ChangesToken.False;

        }

        #endregion

        #region Select Or Edit
        public static ChangesToken select_or_edit_ColorPropertyName(this TextLabel name, ref string property, Material material)
        {
            name.write();
            return select_or_edit_TexturePropertyName(ref property, material);
        }

        public static ChangesToken select_or_edit_ColorProperty(ref string property, Material material)
        {
            var lst = material.GetColorProperties();
            return lst.Count == 0 ? edit(ref property) : select(ref property, lst);
        }

        public static ChangesToken select_or_edit_TexturePropertyName(this TextLabel name, ref string property, Material material)
        {
            name.write();
            return select_or_edit_TexturePropertyName(ref property, material);
        }

        public static ChangesToken select_or_edit_TexturePropertyName(ref string property, Material material)
        {
            var lst = material.MyGetTexturePropertiesNames();
            return lst.Count == 0 ? edit(ref property) : select(ref property, lst);
        }

        public static ChangesToken select_or_edit_TextureProperty(ref ShaderProperty.TextureValue property, Material material)
        {
            var lst = material.MyGetTextureProperties_Editor();
            return select(ref property, lst, allowInsert: false);

        }

        public static ChangesToken select_or_edit<T>(this TextLabel text, ref T obj, List<T> list, bool showIndex = false, bool stripSlahes = false, bool allowInsert = true) where T : Object
        {
            if (list.IsNullOrEmpty())
            {
                write(text);
                return edit(ref obj);
            }

            var changed = ChangeTrackStart();
            if (obj && icon.Delete.ClickUnFocus())
                obj = null;

            write(text);

            select(ref obj, list, showIndex, stripSlahes, allowInsert);

            obj.ClickHighlight();

            return changed;
        }
        public static ChangesToken select_or_edit<T>(this TextLabel name, ref int val, List<T> list, bool showIndex = false) =>
       list.IsNullOrEmpty() ? name.edit(ref val) : name.select_Index(ref val, list, showIndex);

        public static ChangesToken select_or_edit<T>(ref T obj, List<T> list, bool showIndex = false) where T : Object
            => select_or_edit(new TextLabel(), ref obj, list, showIndex);

  
        public static ChangesToken select_or_edit(ref string val, List<string> list, bool showIndex = false, bool stripSlashes = true, bool allowInsert = true)
        {
            var changed = ChangeTrackStart();

            var gotList = !list.IsNullOrEmpty();

            var gotValue = !val.IsNullOrEmpty();

            if (gotList && gotValue && icon.Delete.ClickUnFocus())
                val = "";

            if (!gotValue || !gotList)
                edit(ref val);

            if (gotList)
                select(ref val, list, showIndex, stripSlashes, allowInsert);

            return changed;
        }

        public static ChangesToken select_or_edit(this TextLabel name, ref string val, List<string> list, bool showIndex = false)
        {
            var changed = ChangeTrackStart();

            var gotList = !list.IsNullOrEmpty();

            var gotValue = !val.IsNullOrEmpty();

            if (gotList && gotValue && icon.Delete.ClickUnFocus())
                val = "";

            if (!gotValue || !gotList)
                name.edit(ref val);

            if (gotList)
                name.select(ref val, list, showIndex);

            return changed;
        }

        public static ChangesToken select_SameClass_or_edit<T, G>(this TextLabel text, ref T obj, List<G> list) where T : Object where G : class
        {
            if (list.IsNullOrEmpty())
                return edit(ref obj);

            var changed = ChangeTrackStart();

            if (obj && icon.Delete.ClickUnFocus())
                obj = null;

            write(text);

            select_SameClass(ref obj, list);

            return changed;

        }

        #endregion

        #region Select IGotIndex
        public static ChangesToken select_iGotIndex<T>(this TextLabel label, ref int ind, List<T> lst, bool showIndex = false) where T : IGotIndex
        {
            write(label);
            return select_iGotIndex(ref ind, lst, showIndex);
        }

        public static ChangesToken select_iGotIndex<T>(ref int ind, List<T> lst, bool showIndex = false) where T : IGotIndex
        {

            if (lst.IsNullOrEmpty())
            {
                return edit(ref ind);
            }

            var names = new List<string>(lst.Count + 1);
            var indexes = new List<int>(lst.Count + 1);

            var current = -1;

            foreach (var el in lst)
                if (!el.filterEditorDropdown().IsNullOrDestroyed_Obj())
                {
                    var index = el.IndexForInspector;

                    if (ind == index)
                        current = indexes.Count;
                    names.Add((showIndex ? index + ": " : "") + el.GetNameForInspector());
                    indexes.Add(index);

                }

            if (selectFinal_Internal(ind, ref current, names))
            {
                ind = indexes[current];
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }

        #endregion

        #region Select IGotName

        public static ChangesToken select_iGotDisplayName<T>(this TextLabel label, ref string name, List<T> lst) where T : IGotReadOnlyName
        {
            write(label);
            return select_iGotDisplayName(ref name, lst);
        }

        public static ChangesToken select_iGotName<T>(this TextLabel label, ref string name, List<T> lst) where T : IGotName
        {
           
            write(label);
            if (lst == null)
                return ChangesToken.False;
            return select_iGotName(ref name, lst);
        }

        public static ChangesToken select_iGotName<T>(ref string val, List<T> lst) where T : IGotName
        {

            if (lst == null)
                return ChangesToken.False;

            var namesList = new List<string>(lst.Count + 1);

            var current = -1;

            foreach (var el in lst)
                if (!el.filterEditorDropdown().IsNullOrDestroyed_Obj())
                {
                    var name = el.NameForInspector;

                    if (name == null) continue;

                    if (val != null && val.SameAs(name))
                        current = namesList.Count;
                    namesList.Add(name);

                }

            if (selectFinal_Internal(val, ref current, namesList))
            {
                val = namesList[current];
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }

        public static ChangesToken select_iGotDisplayName<T>(ref string val, List<T> lst) where T : IGotReadOnlyName
        {

            if (lst == null)
                return ChangesToken.False;

            var namesList = new List<string>(lst.Count + 1);

            var current = -1;

            foreach (var el in lst)
                if (!el.filterEditorDropdown().IsNullOrDestroyed_Obj())
                {
                    var name = el.GetReadOnlyName();

                    if (name == null) continue;

                    if (val != null && val.SameAs(name))
                        current = namesList.Count;
                    namesList.Add(name);

                }

            if (selectFinal_Internal(val, ref current, namesList))
            {
                val = namesList[current];
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }


        #endregion
    }
}