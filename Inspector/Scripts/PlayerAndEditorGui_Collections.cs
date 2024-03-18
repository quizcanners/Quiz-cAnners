using System.Collections;
using System.Collections.Generic;
using QuizCanners.Utils;
using UnityEngine;
using Object = UnityEngine.Object;
using QuizCanners.Migration;

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0011 // Add braces
#pragma warning disable IDE0008 // Use explicit type

namespace QuizCanners.Inspect
{
    public static partial class pegi
    {
        private const string NEW_ELEMENT = "new_element";


        #region Collection MGMT Functions 

        public static int InspectedIndex => collectionInspector.Index;

        internal static readonly CollectionInspector collectionInspector = new();

        internal static ChangesToken InspectValueInArray<T>(ref T[] array, int index, ref int inspected, CollectionInspectorMeta listMeta = null)
        {
            T el = array[index];

            var changed = InspectValueInCollection(ref el, index, ref inspected, listMeta);

            if (changed)
                array[index] = el;

            return changed;
        }

        internal static ChangesToken InspectValueInList<T>(T el, List<T> list, int index, ref int inspected,
            CollectionInspectorMeta listMeta = null)
        {

            var changed = InspectValueInCollection(ref el, index, ref inspected, listMeta);

            if (changed && (typeof(T).IsValueType || typeof(Object).IsAssignableFrom(typeof(T))))
                list[index] = el;

            return changed;

        }

        internal static ChangesToken InspectValueInHashSet<T>(T el, HashSet<T> list, int index, ref int inspected,
            CollectionInspectorMeta listMeta = null)
        {

            var tmp = el;
            var changed = InspectValueInCollection(ref tmp, index, ref inspected, listMeta);

            if (changed) 
            {
                if (typeof(T).IsValueType || typeof(Object).IsAssignableFrom(typeof(T)))
                {
                    list.Remove(el);
                    list.Add(tmp);
                }
            }

            return changed;

        }

        public static ChangesToken InspectValueInCollection<T>(ref T el, int index, ref int inspected, CollectionInspectorMeta listMeta = null)
        {

            var changed = ChangeTrackStart();

            var isPrevious = (listMeta != null && listMeta.previouslyInspectedElement == index);

            if (isPrevious)
                SetBgColor(PreviousInspectedColor);

            bool isShown = false;

            if (el.IsNullOrDestroyed_Obj())
            {
                InspectNullElement(ref el);
            }
            else
            {
                var uo = el as Object;

                if ((listMeta == null || listMeta[CollectionInspectParams.allowDeleting]) && el is Object)
                {
                    isShown = true;

                    if (Edit(ref uo, typeof(T), width: (int)(Screen.width * 0.25f)))
                        el = (T)(object)uo;
                }

                if (!TryUseListInspection(ref el, ref inspected))
                {
                    //var pg = el as IPEGI;

                    var need = el as INeedAttention;
                    var warningText = need?.NeedAttention();

                    if (warningText != null)
                        SetBgColor(AttentionColor);

                    if (!TryInspectNamedElement(ref el))
                    {
                        if (uo)
                        {
                            FallbackUnityObjectInspect(uo, el, ref inspected);
                        }
                        else
                        {
                            FallbackNonUnityElementInspect(ref el, ref inspected);
                        }
                    }

                    if ((warningText == null &&
                            Icon.Enter.ClickUnFocus(Msg.InspectElement)) ||
                        (warningText != null && Icon.Warning.ClickUnFocus(warningText)))
                    {
                        inspected = index;
                        isPrevious = true;
                    }

                  //  if (!clickHighlightHandled && ClickHighlight(uo))
                    //    isPrevious = true;

                    if (listMeta != null && listMeta[CollectionInspectParams.showCopyPasteOptions])
                        CopyPaste.InspectOptionsFor(ref el);
                }
            }
            

            RestoreBGColor();

            if (listMeta != null)
            {
                if (listMeta.InspectedElement != -1)
                    listMeta.previouslyInspectedElement = listMeta.InspectedElement;
                else if (isPrevious)
                    listMeta.previouslyInspectedElement = index;

            }
            else if (isPrevious)
                collectionInspector.previouslyEntered = el;


            bool TryUseListInspection(ref T el, ref int inspected)
            {
                var pl = el as IPEGI_ListInspect;

                if (pl == null)
                    return false;

                try
                {
                    pl.InspectInList(ref inspected, index);
                }
                catch (System.Exception ex)
                {
                    Write(ex);
                }

                if (changed && (typeof(T).IsValueType))
                    el = (T)pl;

                if (changed || inspected == index)
                    isPrevious = true;

                return true;
            }

            bool TryInspectNamedElement(ref T el)
            {
                var named = el as IGotName;

                if (named == null)
                    return false;

                var n = named.NameForInspector;

                if (el is Object)
                {
                    if (Edit_Delayed(ref n))
                    {
                        named.NameForInspector = n;
                        isPrevious = true;
                    }
                }
                else
                {
                    if (Edit_Delayed(ref n))
                    {
                        named.NameForInspector = n;
                        if (typeof(T).IsValueType)
                            el = (T)named;

                        isPrevious = true;
                    }
                }

                var sb = new System.Text.StringBuilder();

                var iind = el as IGotIndex;
                if (iind != null)
                    sb.Append(iind.IndexForInspector.ToString() + ": ");

                var count = el as IGotCount;
                if (count != null)
                    sb.Append("[x{0}] ".F(count.GetCount()));

                var label = sb.ToString();

                if (label.Length > 0)
                    label.PegiLabel(70).Write();

                return true;
            }

            void InspectNullElement(ref T el)
            {
                var ed = listMeta?[index];
                if (ed == null)
                {
                    if (typeof(Object).IsAssignableFrom(typeof(T)))
                    {
                        var tmp = el as Object;
                        if (Edit(ref tmp, typeof(T), 200))//edit(ref tmp))
                            el = (T)(object)tmp;
                    }
                    else
                    {
                        "{0}: NULL {1}".F(index, typeof(T).ToPegiStringType()).PegiLabel(150).Write();
                    }
                }
                else
                {
                    object obj = el;

                    if (ed.PEGI_inList<T>(ref obj))
                    {
                        el = (T)obj;
                        isPrevious = true;
                    }
                }
            }

            void FallbackNonUnityElementInspect(ref T el, ref int inspected)
            {
                if (typeof(T).IsEnum) 
                {
                    Edit_Enum(ref el);

                    if (Icon.Enter.Click()) 
                    {
                        inspected = index;
                        isPrevious = true;
                    }

                    return;
                }

                if (!isShown && el.GetNameForInspector().PegiLabel(toolTip: Msg.InspectElement.GetText(), RemainingLength(otherElements: defaultButtonSize * 2 + 10)).ClickLabel())
                {
                    inspected = index;
                    isPrevious = true;
                }
            }

            void FallbackUnityObjectInspect(Object uo, T el, ref int inspected)
            {
                if (uo)
                {
                    Texture tex = uo as Texture;

                    if (tex)
                    {
                        if (ClickHighlight(uo, tex))
                            isPrevious = true;

                        return;
                    }
                }

                if (el.GetNameForInspector().PegiLabel("Inspect", RemainingLength(defaultButtonSize * 2 + 10)).ClickLabel())
                {
                    inspected = index;
                    isPrevious = true;
                }

                ClickHighlight(uo);
            }


            return changed;
        }

        #endregion

        #region LISTS

        #region List of Unity Objects

        public static ChangesToken Edit_List_UObj<T>(this CollectionInspectorMeta listMeta, List<T> list) where T : Object
        {
            var changed = ChangeTrackStart();
            using (collectionInspector.Write_Search_ListLabel(listMeta, list))
            {
                if (listMeta == null)
                {
                    "List MEta is Null".PegiLabel().WriteWarning(); Nl();
                }
                else
                    Edit_or_select_List_UObj(list, ref listMeta.inspectedElement_Internal, listMeta).OnChanged(listMeta.OnChanged);
            }

            return changed;
        }


        public static ChangesToken Edit_List_UObj<T>(this TextLabel label, List<T> list, ref int inspected) where T : Object
        {
            using (collectionInspector.Write_Search_ListLabel(label, ref inspected, list))
            {
                return Edit_or_select_List_UObj(list, ref inspected);
            }
        }

        public static ChangesToken Edit_List_UObj<T>(this TextLabel label, List<T> list) where T : Object
        {
            using (collectionInspector.Write_Search_ListLabel(label, list))
            {
                return list.Edit_List_UObj();
            }
        }

        public static ChangesToken Edit_List_UObj<T>(this List<T> list) where T : Object
        {
            var edited = -1;
            return Edit_or_select_List_UObj(list, ref edited);
        }

        public static ChangesToken Edit_List_UObj<T>(List<T> list, System.Func<T, T> lambda) where T : Object
        {
            var changed = ChangeTrackStart();

            if (collectionInspector.CollectionIsNull(list))
                return changed;

            collectionInspector.Edit_List_Order(list);

            if (list != collectionInspector.reordering)
            {
                collectionInspector.ListAddEmptyClick(list);

                foreach (var el in collectionInspector.InspectionIndexes(list))
                {
                    int i = collectionInspector.Index;
                    var ch = GUI.changed;

                    var tmpEl = lambda(el);
                    Nl();
                    if (ch || !GUI.changed) 
                        continue;

                    changed.Feed(isChanged: true);
                    list[i] = tmpEl;
                }
            }
            Nl();
            return changed;
        }

        public static ChangesToken Edit_or_select_List_UObj<T>(List<T> list, ref int inspected, pegi.CollectionInspectorMeta listMeta = null) where T : Object
        {
            var changed = ChangeTrackStart();

            if (collectionInspector.CollectionIsNull(list))
                return ChangesToken.False;

            var before = inspected;
            if (list.ClampIndexToCount(ref inspected, -1))
                changed.Feed(inspected != before);

            if (inspected == -1)
            {

                collectionInspector.Edit_List_Order(list, listMeta);

                if (list != collectionInspector.reordering)
                {
                    collectionInspector.ListAddEmptyClick(list, listMeta);

                    foreach (var el in collectionInspector.InspectionIndexes(list, listMeta))
                    {
                        var i = collectionInspector.Index;

                        if (!el)
                        {
                            var elTmp = el;

                            if (Edit(ref elTmp))
                                list[i] = elTmp;
                        }
                        else
                            collectionInspector.InspectClassInList(list, i, ref inspected, listMeta);

                        Nl();
                    }

                    CopyPaste.InspectOptions<T>(listMeta);

                }
                else
                    collectionInspector.List_DragAndDropOptions(list, listMeta);

            }
            else collectionInspector.ExitOrDrawPEGI(list, ref inspected);

            Nl();
            return changed;

        }

        #endregion

        #region List

        public static ChangesToken Edit_List_Enum<T>(this TextLabel label, List<T> list, T defaultValue = default) 
        {
            label.style = Styles.ListLabel;
            label.Nl();
            return Edit_List_Enum(list, defaultValue: defaultValue);
        }

        public static ChangesToken Edit_List_Enum<T>(List<T> list, T defaultValue = default)
        {
            var changed = ChangeTrackStart();
            int toDelete = -1;
            for (int i = 0; i < list.Count; i++)
            {
                var d = list[i];

                Icon.Delete.Click(() => toDelete = i);

                if (Edit_Enum(ref d))
                    list[i] = d;

                Nl();
            }

            if (toDelete != -1)
                list.RemoveAt(toDelete);

            if ("Add {0}".F(typeof(T).ToPegiStringType()).PegiLabel().Click())
            {
                list.Add(list.Count == 0 ? defaultValue : list[^1]);
            }

            return changed;
        }

        public static ChangesToken Edit_List<T>(this TextLabel label, List<T> list, ref int inspected)
        {
            using (collectionInspector.Write_Search_ListLabel(label, ref inspected, list))
            {
                return Edit_List(list, ref inspected);
            }
        }

        public static ChangesToken Edit_List<T>(List<T> list, ref int inspected) => Edit_List(list, ref inspected, out _);
        
        public static ChangesToken Edit_List<T>(this TextLabel label, List<T> list)
        {
            using (collectionInspector.Write_Search_ListLabel(label, list))
            {
                return Edit_List(list);
            }
        }

        public static ChangesToken Edit_List<T>(List<T> list)
        {
            var edited = -1;
            return Edit_List(list, ref edited);
        }

        public static ChangesToken Edit_List<T>(this TextLabel label, List<T> list, out T added)
        {
            using (collectionInspector.Write_Search_ListLabel(label, list))
            {
                return Edit_List(list, out added);
            }
        }

        public static ChangesToken Edit_List<T>(List<T> list, out T added)
        {
            var edited = -1;
            return Edit_List(list, ref edited, out added);
        }

        public static ChangesToken Edit_List<T>(this TextLabel label, List<T> list, ref int inspected, out T added)
        {
            using (collectionInspector.Write_Search_ListLabel(label, ref inspected, list))
            {
                return Edit_List(list, ref inspected, out added);
            }
        }

        public static ChangesToken Edit_List<T>(this CollectionInspectorMeta listMeta, List<T> list)
        {
            var changed = ChangeTrackStart();
            using (collectionInspector.Write_Search_ListLabel(listMeta, list)) 
            {
                if (listMeta == null)
                {
                    "List MEta is Null".PegiLabel().WriteWarning(); Nl();
                }
                else
                    Edit_List(list, ref listMeta.inspectedElement_Internal, out _, listMeta).OnChanged(listMeta.OnChanged);
            }

            return changed;
        }

        public static ChangesToken Edit_List<T>(this CollectionInspectorMeta listMeta, List<T> list, out T added)
        {
            using (collectionInspector.Write_Search_ListLabel(listMeta, list))
            {
                return Edit_List(list, ref listMeta.inspectedElement_Internal, out added, listMeta);
            }
        }

        public static ChangesToken Edit_List<T>(List<T> list, ref int inspected, out T added, CollectionInspectorMeta listMeta = null)
        {
            var changes = ChangeTrackStart();

            added = default;

            if (list == null)
            {
                "List of {0} is null".F(typeof(T).ToPegiStringType()).PegiLabel().Write();

                return changes;
            }

            if (inspected >= list.Count)
            {
                inspected = -1;
                changes.Feed(isChanged: true);
            }

            if (inspected == -1)
            {

                collectionInspector.Edit_List_Order(list, listMeta);

                if (list != collectionInspector.reordering)
                {
                    collectionInspector.TryShowListAddNewOption(list, ref added, listMeta);

                    if (list.Count == 0)
                    {
                        Nl();
                        "Empty List of {0}".F(typeof(T).ToPegiStringType()).PegiLabel(Styles.HeaderText).Nl();
                    }
                    else
                    {
                        foreach (var el in collectionInspector.InspectionIndexes(list, listMeta))
                        {
                            int i = collectionInspector.Index;

                            if (el.IsNullOrDestroyed_Obj())
                            {
                                var us = el as Object;

                                if (Edit(ref us, typeof(T)))
                                    list[i] = (T)((object)us);

                                /*
                                if (!Utils.IsMonoType(list, i))
                                {
                                    (typeof(T).IsSubclassOf(typeof(Object))
                                        ? "use edit_List_UObj"
                                        : "is NUll").PegiLabel().Write();
                                }*/
                            }
                            else
                            {
                                InspectValueInList(el, list, i, ref inspected, listMeta);
                            }

                            Nl();
                        }
                    }

                    collectionInspector.TryShowListCreateNewOptions(list, ref added, listMeta);

                    CopyPaste.InspectOptions<T>(listMeta);
                }
            }
            else collectionInspector.ExitOrDrawPEGI(list, ref inspected);

            Nl();

            if (changes && listMeta != null) 
            {
                listMeta.OnChanged();
            }

            return changes;
        }

        #region Tagged Types

        public static ChangesToken Edit_List<T>(this CollectionInspectorMeta listMeta, List<T> list, TaggedTypes.DerrivedList types, out T added)
        {
            using (collectionInspector.Write_Search_ListLabel(listMeta, list))
            {
                return Edit_List(list, ref listMeta.inspectedElement_Internal, types, out added, listMeta);
            }
        }

        public static ChangesToken Edit_List<T>(this CollectionInspectorMeta listMeta, List<T> list, TaggedTypes.DerrivedList types)
        {
            using (collectionInspector.Write_Search_ListLabel(listMeta, list))
            {
                return Edit_List(list, ref listMeta.inspectedElement_Internal, types, out _, listMeta);
            }
        }

        public static ChangesToken Edit_List<T>(this TextLabel label, List<T> list, ref int inspected, TaggedTypes.DerrivedList types, out T added)
        {
            using (collectionInspector.Write_Search_ListLabel(label, ref inspected, list))
            {
                return Edit_List(list, ref inspected, types, out added);
            }
        }

        private static ChangesToken Edit_List<T>(List<T> list, ref int inspected, TaggedTypes.DerrivedList types, out T added, CollectionInspectorMeta listMeta = null)
        {
            var changes = ChangeTrackStart();

            added = default;

            if (list == null)
            {
                "List of {0} is null".F(typeof(T)).PegiLabel().Write();

                 return changes;
            }

            if (inspected >= list.Count)
            {
                inspected = -1;
                changes.Feed(isChanged: true);
            }

            if (inspected == -1)
            {
                collectionInspector.Edit_List_Order(list, listMeta);

                if (list != collectionInspector.reordering)
                {
                    foreach (var el in collectionInspector.InspectionIndexes(list, listMeta))
                    {
                        int i = collectionInspector.Index;

                        if (el == null)
                        {
                            if (!Utils.IsMonoType(list, i))
                            {
                                (typeof(T).IsSubclassOf(typeof(Object))
                                    ? "use edit_List_UObj"
                                    : "is NUll").PegiLabel().Write();
                            }
                        }
                        else
                        {
                            InspectValueInList(el, list, i, ref inspected, listMeta);
                        }
                        Nl();
                    }

                    collectionInspector.TryShowListCreateNewOptions(list, ref added, types, listMeta).Nl();

                    CopyPaste.InspectOptions<T>(listMeta);
                }
            }
            else collectionInspector.ExitOrDrawPEGI(list, ref inspected);

            Nl();
            return changes;
        }

        #endregion

        #endregion

        #region List by Lambda 

        #region SpecialLambdas

        private static IList listElementsRoles;

        private static Color lambda_Color(Color val)
        {
            Edit(ref val);
            return val;
        }

        private static Color32 lambda_Color(Color32 val)
        {
            Edit(ref val);
            return val;
        }

        private static int lambda_int(int val)
        {
            Edit(ref val);
            return val;
        }

        private static string lambda_string_role(string val)
        {
            var role = listElementsRoles.TryGetObj(collectionInspector.Index);
            if (role != null)
                role.GetNameForInspector().PegiLabel(90).Edit(ref val);
            else Edit(ref val);

            return val;
        }

        public static string lambda_string(string val)
        {
            Edit(ref val);
            return val;
        }

        public static ChangesToken Edit_List(this TextLabel label, List<int> list) =>
            label.Edit_List(list, lambda_int);

        public static ChangesToken Edit_List(this TextLabel label, List<Color> list) =>
            label.Edit_List(list, lambda_Color);

        public static ChangesToken Edit_List(this TextLabel label, List<Color32> list) =>
            label.Edit_List(list, lambda_Color);

        public static ChangesToken Edit_List(this TextLabel label, List<string> list) =>
            label.Edit_List(list, lambda_string);
        #endregion

        public static ChangesToken Edit_List<T>(this TextLabel label, List<T> list, System.Func<T, T> lambda) 
        {
            using (collectionInspector.Write_Search_ListLabel(label, list))
            {
                return Edit_List(list, lambda, out _);
            }
        }

        public static ChangesToken Edit_List<T>(this TextLabel label, List<T> list, System.Func<T, T> lambda, out T added) 
        {
            using (collectionInspector.Write_Search_ListLabel(label, list))
            {
                return Edit_List(list, lambda, out added);
            }
        }

        public static ChangesToken Edit_List<T>(List<T> list, System.Func<T, T> lambda, out T added)
        {
            var changed = ChangeTrackStart();

            added = default;

            if (collectionInspector.CollectionIsNull(list))
                return changed;

            collectionInspector.Edit_List_Order(list);

            if (list != collectionInspector.reordering)
            {
                collectionInspector.TryShowListAddNewOption(list, ref added);

                foreach (var el in collectionInspector.InspectionIndexes(list))
                {
                    int i = collectionInspector.Index;

                    var ch = GUI.changed;
                    var tmpEl = lambda(el);
                    if (!ch && GUI.changed)
                    {
                        list[i] = tmpEl;
                    }
                    Nl();
                }
            }
            Nl();
            return changed;
        }

        public static ChangesToken Edit_List(this TextLabel name, List<string> list, System.Func<string, string> lambda)
        {
            using (collectionInspector.Write_Search_ListLabel(name, list))
            {
                return Edit_List(list, lambda);
            }
        }

        public static ChangesToken Edit_List(List<string> list, System.Func<string, string> lambda)
        {
            var changed = ChangeTrackStart();

            if (collectionInspector.CollectionIsNull(list))
                return ChangesToken.False;

            collectionInspector.Edit_List_Order(list);

            if (list != collectionInspector.reordering)
            {
                if (Icon.Add.ClickUnFocus())
                {
                    list.Add("");
                    collectionInspector.SkrollToBottom();
                }

                foreach (var el in collectionInspector.InspectionIndexes(list))
                {
                    int i = collectionInspector.Index;

                    var ch = GUI.changed;
                    var tmpEl = lambda(el);
                    Nl();
                    if (ch || !GUI.changed) continue;

                    changed.Feed(isChanged: true);
                    list[i] = tmpEl;
                }

            }

            Nl();
            return changed;
        }

        #endregion

        #endregion

        #region Hash Set

        public static ChangesToken Edit_HashSet<T>(HashSet<T> list, ref int inspected, out T added, CollectionInspectorMeta listMeta = null)
        {
            var changes = ChangeTrackStart();

            added = default;

            if (list == null)
            {
                "List of {0} is null".F(typeof(T).ToPegiStringType()).PegiLabel().Write();

                return changes;
            }

            if (inspected >= list.Count)
            {
                inspected = -1;
                changes.Feed(isChanged: true);
            }

            if (inspected == -1)
            {

                //collectionInspector.Edit_List_Order(list, listMeta);

               //if (list != collectionInspector.reordering)
                //{

                   // collectionInspector.TryShowListAddNewOption(list, ref added, listMeta);

                    foreach (var el in collectionInspector.InspectionIndexes(list, listMeta))
                    {
                        int i = collectionInspector.Index;

                        if (el.IsNullOrDestroyed_Obj())
                        {
                            
                            //if (!collectionInspector.IsMonoType(list, i))
                            //{
                                (typeof(T).IsSubclassOf(typeof(Object))
                                    ? "need to create edit_HashSetFor_UObj"
                                    : "is NUll").PegiLabel().Write();
                            //}
                        }
                        else
                        {
                            InspectValueInHashSet(el, list, i, ref inspected, listMeta);
                        }

                        Nl();
                    }

                    //collectionInspector.TryShowListCreateNewOptions(list, ref added, listMeta);

                    CopyPaste.InspectOptions<T>(listMeta);
              //  }
            }
           // else collectionInspector.ExitOrDrawPEGI(list, ref inspected);

            Nl();
            return changes;
        }


        #endregion

        #region Dictionary Generic
        internal interface iCollectionInspector<T>
        {
            void Set(T val);
        }

        internal class KeyValuePairInspector<T,G> : iCollectionInspector<KeyValuePair<T,G>>, ISearchable, INeedAttention
        {
            private KeyValuePair<T, G> _pair;

            public void Set(KeyValuePair<T, G> pair)
            {
                _pair = pair;
            }

            public override string ToString() =>
                 _pair.Value == null ? _pair.Key.GetNameForInspector() : _pair.Value.GetNameForInspector();
            

            public string NeedAttention()
            {

                string msg;// = null;

                if (NeedsAttention(_pair.Value, out msg))
                    "{0} at {1}".F(msg, _pair.Key.GetNameForInspector());

                return msg;
               
            }

            public IEnumerator SearchKeywordsEnumerator()
            {
                yield return _pair.Value;
                yield return _pair.Key;
            }
        }

        private static void WriteNullDictionary_Internal<T>() => "NULL {0} Dictionary".PegiLabel().Write();
        

        private static int _tmpKeyInt;
        private static readonly Dictionary<System.Type, string> dictionaryNamesForAddNewElement = new();
        public static ChangesToken AddDictionaryPairOptions<TValue>(Dictionary<int, TValue> dic) 
        {
            var changed = ChangeTrackStart();
            if (dic == null)
            {
                WriteNullDictionary_Internal<TValue>();
                return changed;
            }

            "Key".PegiLabel(60).Edit(ref _tmpKeyInt);

            if (dic.ContainsKey(_tmpKeyInt))
            {
                if (Icon.Refresh.Click("Find Free index"))
                {
                    while (dic.ContainsKey(_tmpKeyInt))
                        _tmpKeyInt++;
                }
                "Key {0} already exists".F(_tmpKeyInt).PegiLabel().WriteWarning();
            }
            else
            {
                if (Icon.Add.Click("Add new Value"))
                {
                    dic.Add(_tmpKeyInt, System.Activator.CreateInstance<TValue>());
                    while (dic.ContainsKey(_tmpKeyInt))
                        _tmpKeyInt++;
                }
            }

            Nl();

            return changed;

        }

        public static ChangesToken AddDictionaryPairOptions<TValue>(Dictionary<string, TValue> dic, string defaultElementName) 
        {
            string newElementName;
            if (!dictionaryNamesForAddNewElement.TryGetValue(typeof(TValue), out newElementName))
                newElementName = defaultElementName;

            var changed = ChangeTrackStart();
            if (dic == null)
            {
                WriteNullDictionary_Internal<TValue>();
                return changed;
            }

            if ("Key".PegiLabel(60).Edit(ref newElementName).IgnoreChanges())
                dictionaryNamesForAddNewElement[typeof(TValue)] = newElementName;

            var suggestedName = "{0} {1}".F(defaultElementName.Length > 0 ? defaultElementName : NEW_ELEMENT, dic.Count);

            if (!suggestedName.Equals(newElementName) && Icon.Refresh.Click())
                newElementName = suggestedName;

            if (dic.ContainsKey(newElementName))
            {
                Nl();
                "Key {0} already exists".F(newElementName).PegiLabel().WriteWarning();
            }
            else
            {
                if (Icon.Add.Click("Add new Value"))
                {
                    TValue value = default;

                    var t = typeof(TValue);

                    if (t.IsValueType || (typeof(Object).IsAssignableFrom(t) == false && t.GetConstructor(System.Type.EmptyTypes) != null))
                        value = System.Activator.CreateInstance<TValue>();
                    
                    dic.Add(newElementName, value);
                    var name = value as IGotName;
                    if (name != null)
                        name.NameForInspector = newElementName;

                    newElementName = defaultElementName + " " + dic.Count;
                }
            }

            Nl();

            if (changed)
                dictionaryNamesForAddNewElement[typeof(TValue)] = newElementName;

            return changed;
        }

        public static ChangesToken Edit_Dictionary<TKey, TValue>(Dictionary<TKey, TValue> dic, bool showKey = true)
        {
            int inspected = -1;
            using (collectionInspector.Write_Search_DictionaryLabel(dic.ToString().PegiLabel(), ref inspected, dic))
            {
                return Edit_Dictionary_Internal(dic, ref inspected, showKey: showKey);
            }
        }

        public static ChangesToken Edit_Dictionary<TKey, TValue>(Dictionary<TKey, TValue> dic, ref int inspected, bool showKey = true)
        {
            using (collectionInspector.Write_Search_DictionaryLabel(QcSharp.AddSpacesInsteadOfCapitals(dic.ToString().SimplifyTypeName()).PegiLabel(), ref inspected, dic))
            {
                return Edit_Dictionary_Internal(dic, ref inspected, showKey: showKey);
            }
        }

        public static ChangesToken Edit_Dictionary<TKey, TValue>(this TextLabel label, Dictionary<TKey, TValue> dic, bool showKey = true)
        {
            int inspected = -1;
            using (collectionInspector.Write_Search_DictionaryLabel(label, ref inspected, dic))
            {
                return Edit_Dictionary_Internal(dic, ref inspected, showKey: showKey);
            }
        }

        public static ChangesToken Edit_Dictionary<TKey, TValue>(this TextLabel label, Dictionary<TKey, TValue> dic, ref int inspected, bool showKey = true)
        {
            using (collectionInspector.Write_Search_DictionaryLabel(label, ref inspected, dic))
            {
                return Edit_Dictionary_Internal(dic, ref inspected, showKey: showKey);
            }
        }

        public static ChangesToken Edit_Dictionary<TKey, TValue>(this TextLabel label, Dictionary<TKey, TValue> dic, System.Func<TValue, TValue> lambda, bool showKey = false)
        {
            int inspected = -1;
            using (collectionInspector.Write_Search_DictionaryLabel(label, ref inspected, dic))
            {
                return Edit_Dictionary_Internal(dic, lambda, showKey: showKey);
            }
        }

        public static ChangesToken Edit_Dictionary<G, T>(this CollectionInspectorMeta listMeta, Dictionary<G, T> dic)
        {
            using (collectionInspector.Write_Search_DictionaryLabel(listMeta, dic))
            {
                return Edit_Dictionary_Internal(dic, ref listMeta.inspectedElement_Internal, showKey: listMeta[CollectionInspectParams.showDictionaryKey], listMeta: listMeta);
            }
        }

        public static ChangesToken Edit_Dictionary<TKey, TValue>(this CollectionInspectorMeta listMeta, Dictionary<TKey, TValue> dic, System.Func<TValue, TValue> lambda)
        {
            using (collectionInspector.Write_Search_DictionaryLabel(listMeta, dic))
            {
                return Edit_Dictionary_Internal(dic, lambda, listMeta: listMeta);
            }
        }

        public static ChangesToken Edit_Dictionary(this CollectionInspectorMeta listMeta, Dictionary<string, string> dic)
        {
            using (collectionInspector.Write_Search_DictionaryLabel(listMeta, dic))
            {
                return Edit_Dictionary_Internal(dic, lambda_string, listMeta: listMeta);
            }
        }
        
        public static ChangesToken Edit_Dictionary(this TextLabel label, Dictionary<string, string> dic)
        {
            int inspected = -1;
            using (collectionInspector.Write_Search_DictionaryLabel(label, ref inspected, dic))
            {
                return Edit_Dictionary_Internal(dic, lambda_string);
            }
        }

        public static ChangesToken Edit_Dictionary(this TextLabel label, Dictionary<int, string> dic, List<string> roles)
        {
            int inspected = -1;
            using (collectionInspector.Write_Search_DictionaryLabel(label, ref inspected, dic))
            {
                listElementsRoles = roles;
                var changes = Edit_Dictionary_Internal(dic, lambda_string_role, false);
                listElementsRoles = null;
                return changes;
            }
        }

        internal static ChangesToken Edit_Dictionary_Internal<TKey, TValue>(Dictionary<TKey, TValue> dic, System.Func<TValue, TValue> lambda, bool showKey = true, CollectionInspectorMeta listMeta = null)
        {

            if (dic == null)
            {
                WriteNullDictionary_Internal<TValue>();
                return ChangesToken.False;
            }

            Nl();

            if (listMeta != null)
                showKey = listMeta[CollectionInspectParams.showDictionaryKey];

            var changed = ChangeTrackStart();

            if (listMeta != null && listMeta.IsAnyEntered)
            {

                if (Icon.Exit.Click("Exit " + listMeta.Label))
                    listMeta.IsAnyEntered = false;

                if (listMeta.IsAnyEntered && (dic.Count > listMeta.InspectedElement))
                {
                    var el = dic.GetElementAt(listMeta.InspectedElement);

                    var val = el.Value;

                    var ch = GUI.changed;

                    Try_Nested_Inspect(val);

                    if (new ChangesToken(!ch && GUI.changed))
                        dic[el.Key] = val;
                }
            }
            else
            {
                foreach (var item in collectionInspector.InspectionIndexes(dic, listMeta, new KeyValuePairInspector<TKey, TValue>()))
                {
                    var itemKey = item.Key;
                    
                    if ((listMeta != null && listMeta[CollectionInspectParams.allowDeleting]) && Icon.Delete.Click(25).UnfocusOnChange())
                        dic.Remove(itemKey);
                    else
                    {
                        if (showKey)
                            itemKey.GetNameForInspector().PegiLabel(50).Write_ForCopy();

                        var el = item.Value;
                        var ch = GUI.changed;
                        el = lambda(el);

                        if (new ChangesToken(!ch && GUI.changed))
                        {
                            dic[itemKey] = el;
                            break;
                        }

                        if (listMeta != null && Icon.Enter.Click("Enter " + el))
                            listMeta.InspectedElement = collectionInspector.Index;
                    }
                    Nl();
                }
            }
            return changed;
        }

        internal static ChangesToken Edit_Dictionary_Internal<TKey, TValue>(Dictionary<TKey, TValue> dic, ref int inspected, bool showKey, CollectionInspectorMeta listMeta = null)
        {
            var changed = ChangeTrackStart();

            Nl();

            if (dic == null)
            {
                WriteNullDictionary_Internal<TValue>();
                return ChangesToken.False;
            }

            inspected = Mathf.Clamp(inspected, -1, dic.Count - 1);

            if (inspected == -1)
            {
                string keyToReplace = null;
                string keyToReplaceWith = null;
                bool nameIsKey = true;

                if (listMeta != null)
                {
                    showKey = listMeta[CollectionInspectParams.showDictionaryKey];
                    nameIsKey = listMeta[CollectionInspectParams.nameIsDictionaryKey];
                }

                KeyValuePair<TKey, TValue> modifiedElement = new();
                bool modified = false;

                foreach (var item in collectionInspector.InspectionIndexes(dic, listMeta, new KeyValuePairInspector<TKey, TValue>()))
                {
                    var itemKey = item.Key;

                    if ((listMeta == null || listMeta[CollectionInspectParams.allowDeleting]) 
                        && Icon.Delete.ClickConfirm(confirmationTag: "DelDicEl"+collectionInspector.Index))
                    {
                        dic.Remove(itemKey);
                        return ChangesToken.True;
                    }
                    else
                    {
                        if (showKey)
                        {
                            if (!TryEditStringKey())
                            {
                                if (itemKey is Object)
                                {
                                    var asUobj = itemKey as Object;

                                    Write(asUobj);
                                    ClickHighlight(asUobj);
                                }
                                else
                                {
                                    itemKey.GetNameForInspector().PegiLabel(0.3f).Write_ForCopy();
                                }
                            }

                            bool TryEditStringKey()
                            {
                                bool keyHandled = false;

                                var strKey = itemKey as string;

                                if (strKey != null)
                                {
                                    IGotName iGotName = item.IsNullOrDestroyed_Obj() ? null : item.Value as IGotName;

                                    try
                                    {
                                        if (nameIsKey && iGotName != null)
                                        {
                                            keyHandled = true;

                                            var theName = iGotName.NameForInspector;

                                            if (theName.IsNullOrEmpty())
                                            {
                                                "NULL NAME".PegiLabel(60).Write();
                                            }
                                            else if (!theName.Equals(strKey))
                                            {
                                                var strDic = dic as Dictionary<string, TValue>;

                                                if (strDic.ContainsKey(theName))
                                                    "Name exists as Key".PegiLabel(90).Write();
                                                else
                                                {
                                                    if ("Key<-".PegiLabel("Override Key with Name").ClickUnFocus())
                                                    {
                                                        keyToReplace = strKey;
                                                        keyToReplaceWith = theName;
                                                    }

                                                    if ("->Name".PegiLabel("Override Name with Key").ClickUnFocus())
                                                        iGotName.NameForInspector = strKey;
                                                }
                                            }
                                        }
                                    }
                                    catch (System.Exception ex)
                                    {
                                        if (Icon.Warning.Click(toolTip: "Log Exception"))
                                            Debug.LogException(ex);

                                    }
                                    string tmp = strKey;

                                    if (!keyHandled && Edit_Delayed(ref tmp))
                                    {
                                        keyToReplace = strKey;
                                        keyToReplaceWith = tmp;
                                    }

                                    keyHandled = true;
                                }

                                return keyHandled;
                            }

                        }

                        var el = item.Value;

                        if (InspectValueInCollection(ref el, collectionInspector.Index, ref inspected, listMeta))// && typeof(TValue).IsValueType)
                        {
                            modifiedElement = new KeyValuePair<TKey, TValue>(item.Key, el);
                            modified = true;
                        }
                    }
                    Nl();
                }

                if (modified) 
                    dic[modifiedElement.Key] = modifiedElement.Value;
                
                if (keyToReplace != null)
                {
                    var strDic = dic as Dictionary<string, TValue>;
                    var tmpVal = strDic[keyToReplace];
                    strDic.Remove(keyToReplace);
                    strDic.Add(keyToReplaceWith, tmpVal);
                }

                if ((listMeta == null || listMeta[CollectionInspectParams.showAddButton]) && typeof(TKey).Equals(typeof(string)))
                {
                    Nl();
                    var stringDick = dic as Dictionary<string, TValue>;

                    string defaultName = (listMeta == null ? ("New " + QcSharp.AddSpacesToSentence(typeof(TValue).ToPegiStringType())) : listMeta.ElementName);

                    AddDictionaryPairOptions(stringDick, defaultElementName: defaultName);
                }

            }
            else
                collectionInspector.ExitOrDrawPEGI(dic, ref inspected);

            Nl();
            return changed;
        }

        #endregion

        #region Arrays

        public static ChangesToken Edit_Array<T>(this CollectionInspectorMeta listMeta, ref T[] array)
        {
            var changed = ChangeTrackStart();
            using (collectionInspector.Write_Search_ListLabel(listMeta, array))
            {
                if (listMeta == null)
                {
                    "Array Meta is Null".PegiLabel().WriteWarning(); Nl();
                }
                else
                    Edit_Array(ref array, ref listMeta.inspectedElement_Internal, out _, listMeta).OnChanged(listMeta.OnChanged);
            }
            collectionInspector.End();
            return changed;
        }

        public static ChangesToken Edit_Array<T>(this TextLabel label, ref T[] array)
        {
            int inspected = -1;
            using (collectionInspector.Write_Search_ListLabel(label, array))
            {
                return Edit_Array(ref array, ref inspected);
            }
        }

        public static ChangesToken Edit_Array<T>(ref T[] array, ref int inspected) => Edit_Array(ref array, ref inspected, out _);
        
        private static ChangesToken Edit_Array<T>(ref T[] array, ref int inspected, out T added, CollectionInspectorMeta metaDatas = null)
        {
           // Nl();

            var changed = ChangeTrackStart();

            added = default;

            if (array == null)
            {
                if (Msg.Init.F(Msg.Array).PegiLabel().ClickUnFocus().Nl())
                    array = new T[0];
            }
            else
            {
                collectionInspector.ExitOrDrawPEGI(array, ref inspected);

                if (inspected != -1) 
                    return changed;

                if (!typeof(T).IsNew())
                {
                    if (Icon.Add.ClickUnFocus(Msg.AddEmptyCollectionElement))
                    {
                        QcSharp.ExpandBy(ref array, 1);
                        collectionInspector.SkrollToBottom();
                    }
                }
                else if (Icon.Create.ClickUnFocus(Msg.AddNewCollectionElement))
                    QcSharp.AddAndInit(ref array, 1);

                collectionInspector.Edit_Array_Order(ref array, metaDatas).Nl();

                if (array == collectionInspector._editingArrayOrder) 
                    return changed;

                for (var i = 0; i < array.Length; i++)
                {
                    InspectValueInArray(ref array, i, ref inspected, metaDatas).Nl();
                }
            }

            collectionInspector.End();

            return changed;
        }

        #endregion
        
        #region Searching

    
        public static bool Try_SearchMatch_Obj(object obj, string searchText) => SearchMatch_Obj_Internal(obj, new[] { searchText });

        private static bool SearchMatch_Obj_Internal(this object obj, string[] text, int[] indexes = null)
        {
            if (obj.IsNullOrDestroyed_Obj())
                return false;

            var matched = new bool[text.Length];

            var go = QcUnity.TryGetGameObjectFromObj(obj);

            if (go)
            {
                return 
                SearchMatching_Internal.MatchGameObjectToStrings(go, text, ref matched) ||
                (!indexes.IsNullOrEmpty() && SearchMatching_Internal.Internal_GotIndex(go.GetComponent<IGotIndex>(), indexes));
            }
            else
            {
                if (SearchMatching_Internal.MatchObjectToStrings(obj, text, ref matched))
                    return true;

                if (!indexes.IsNullOrEmpty() && SearchMatching_Internal.Internal_GotIndex(go.GetComponent<IGotIndex>(), indexes))
                    return true;
            }

            return false;
        }

        private static class SearchMatching_Internal 
        {
            public static bool Match(object obj, string[] text, ref bool[] matched)
            {
                var go = QcUnity.TryGetGameObjectFromObj(obj);
                if (go)
                {
                    return MatchGameObjectToStrings(go, text, ref matched);
                } else 
                {
                    return MatchObjectToStrings(obj, text, ref matched);
                }
            }

            public static bool MatchGameObjectToStrings(GameObject go, string[] text, ref bool[] matched)
            {
                if (go)
                {
                    return
                    Internal_ByGotName(go.GetComponent<IGotName>(), text, ref matched) ||
                    Internal_String(go.name, text, ref matched) ||
                    Internal_ByISearchable(go.GetComponent<ISearchable>(), text, ref matched) ||
                    Internal_ByAttention(go.GetComponent<INeedAttention>(), text, ref matched);
                }
              
                return false;
            }

            public static bool MatchObjectToStrings(object obj, string[] text, ref bool[] matched)
            {
               
                if (Internal_ByGotName(QcUnity.TryGetInterfaceFrom<IGotName>(obj), text, ref matched))
                    return true;

                if (Internal_ByISearchable(QcUnity.TryGetInterfaceFrom<ISearchable>(obj), text, ref matched))
                    return true;

                if (Internal_ByAttention(QcUnity.TryGetInterfaceFrom<INeedAttention>(obj), text, ref matched))
                    return true;

                if (Internal_String(obj.ToString(), text, ref matched))
                    return true;
                
                return false;
            }

            private static bool Internal_ByAttention(INeedAttention needAttention, string[] text, ref bool[] matched)
                => Internal_String(needAttention?.NeedAttention(), text, ref matched);

            private static bool Internal_ByGotName(IGotName gotName, string[] text, ref bool[] matched)
                => Internal_String(gotName?.NameForInspector, text, ref matched);

            public static bool Internal_GotIndex(IGotIndex gotIndex, int[] indexes)
            {
                if (gotIndex == null)
                    return false;

                var target = gotIndex.IndexForInspector;

                foreach (var i in indexes) 
                {
                    if (i == target)
                        return true;
                }

                return false;
            }
            public static bool Internal_String(string label, string[] text, ref bool[] matched)
            {

                if (label.IsNullOrEmpty())
                    return false;

                var fullMatch = true;

                for (var i = 0; i < text.Length; i++)
                    if (!matched[i])
                    {
                        if (!text[i].IsSubstringOf(label))
                            fullMatch = false;
                        else
                            matched[i] = true;
                    }

                return fullMatch;

            }

            public static bool Internal_ByISearchable(ISearchable searchable, string[] text, ref bool[] matched)
            {
                if (searchable == null) return false;

                var fullMatch = true;

                List<string> tmpStrings = new();

                List<IEnumerator> enumerators = new()
                {
                    searchable.SearchKeywordsEnumerator()
                };

                for (var i = 0; i < text.Length; i++)
                    if (!matched[i])
                    {
                        var val = text[i];

                        foreach (var s in tmpStrings)
                        {
                            if (val.IsSubstringOf(s)) 
                            {
                                matched[i] = true;
                                break;
                            }
                        }

                        while (enumerators.Count > 0 && !matched[i]) 
                        {
                            var cur = enumerators[^1];
                            bool loopingEnumerator = true;

                            while (loopingEnumerator && !matched[i])
                            {
                                object el;

                                if (!cur.MoveNext())
                                {
                                    enumerators.RemoveAt(enumerators.Count - 1);
                                    loopingEnumerator = false;
                                    continue;
                                } else 
                                {
                                    el = cur.Current;
                                }

                                if (el == null)
                                    continue;

                                if (el is string) 
                                {
                                    var str = el as string;
                                    tmpStrings.Add(str);

                                    if (val.IsSubstringOf(str))
                                    {
                                        matched[i] = true;
                                        break;
                                    }

                                } else if (el is ISearchable)
                                {
                                    enumerators.Add((el as ISearchable).SearchKeywordsEnumerator());
                                    loopingEnumerator = false;
                                }
                                else if (el is IEnumerator)
                                {
                                    enumerators.Add(el as IEnumerator);
                                    loopingEnumerator = false;
                                }
                                else if (el is IEnumerable)
                                {
                                    enumerators.Add((el as IEnumerable).GetEnumerator());
                                    loopingEnumerator = false;
                                }

                                Match(el, text, ref matched);
                            }
                        }

                        if (!matched[i])
                            fullMatch = false;
                    }

                return fullMatch;
            }

        }


        private static readonly SearchData defaultSearchData = new();

        private static readonly char[] splitCharacters = { ' ', '.' };

        internal class SearchData 
        {
            private const string SEARCH_FIELD_FOCUS_NAME = "_pegiSearchField";


            public static bool UnityFocusNameWillWork; // Focus name bug on first focus
            public IEnumerable FilteredList;
            public string SearchedText;
            public int UncheckedElement;
            public int InspectionIndexStart;
            public bool FilterByNeedAttention;

            private string[] _searchBys;
            private readonly List<int> _filteredListElements = new();
            private int _fileredForCount = -1;
            private int _focusOnSearchBarIn;
       
            public List<int> GetFilteredList(int count)
            {
                if (_fileredForCount != count)
                {
                    OnCountChange(count);
                }

                return _filteredListElements;
            }

            public void CloseSearch()
            {
                FilteredList = null;
                UnFocus();
            }

            public void ToggleSearch(IEnumerable collection, TextLabel label) => ToggleSearch(collection, label.label);

            public void ToggleSearch(IEnumerable collection, string label = "")
            {

                if (collection == null)
                    return;

                var active = ReferenceEquals(collection, FilteredList);

                var changed = ChangeTrackStart();

                if (active && Icon.FoldedOut.ClickUnFocus("{0} {1} {2}".F(Icon.Hide.GetText(), Icon.Search.GetText(), collection), 27) || KeyCode.UpArrow.IsDown())
                    active = false;

                if (!active && !ReferenceEquals(collection, collectionInspector.reordering) &&
                    (Icon.Search
                        .Click("{0} {1}".F(Icon.Search.GetText(), label.IsNullOrEmpty() ? collection.ToString() : label), 27)))
                {
                    active = true;
                    _focusOnSearchBarIn = 2;
                    FocusedName = SEARCH_FIELD_FOCUS_NAME;
                }

                if (active)
                {
                    Icon.Warning.Draw(toolTip: "Filter by warnings");
                    if (ToggleIcon(ref FilterByNeedAttention))
                        Refresh();
                }

                if (changed)
                {
                    FilteredList = active ? collection : null;
                }

            }

            public void SearchString(IEnumerable list, out bool searching, out string[] searchBy)
            {
                searching = false;

                if (ReferenceEquals(list, FilteredList))
                {
                    Nl();
                    Icon.Search.Draw();
                    NameNextForFocus(SEARCH_FIELD_FOCUS_NAME);

                    if (Edit(ref SearchedText) | Icon.Refresh.Click("Search again", 20).Nl())
                    {
                        UnityFocusNameWillWork = true;
                        Refresh();
                        if (SearchedText == null)
                            _searchBys = new string[0];
                        else
                            _searchBys = SearchedText.Split(splitCharacters, System.StringSplitOptions.RemoveEmptyEntries);
                    }

                    if (_focusOnSearchBarIn > 0)
                    {
                        _focusOnSearchBarIn--;
                        if (_focusOnSearchBarIn == 0)
                        {
                            FocusedName = SEARCH_FIELD_FOCUS_NAME;
                            RepaintEditor();
                        }
                    }
                    searching = FilterByNeedAttention || !_searchBys.IsNullOrEmpty();
                }
                searchBy = _searchBys;
            }

            private void OnCountChange(int newCount = -1)
            {
                _fileredForCount = newCount;
                _filteredListElements.Clear();
                UncheckedElement = 0;
                InspectionIndexStart = Mathf.Max(0, Mathf.Min(InspectionIndexStart, newCount - 1));
            }
        
            public void Refresh()=> OnCountChange();
        }

        #endregion
        
    }
}
