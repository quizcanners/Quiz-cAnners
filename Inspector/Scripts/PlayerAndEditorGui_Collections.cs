using System.Collections;
using System.Collections.Generic;
using QuizCanners.Utils;
using UnityEngine;
using Object = UnityEngine.Object;
using QuizCanners.Migration;
using System;
using System.Linq;

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

            if (changed && ((el is string) || typeof(T).IsValueType || typeof(Object).IsAssignableFrom(typeof(T))))
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

                    if (uo)
                    {
                        FallbackUnityObjectInspect(uo, el, ref inspected);
                    }
                    else
                    {
                        FallbackNonUnityElementInspect(ref el, ref inspected);
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
                if (el is not IPEGI_ListInspect pl)
                    return false;

                try
                {
                    pl.InspectInList(ref inspected, index);
                }
                catch (System.Exception ex)
                {
                    pegi.Nl();
                    Write(ex);
                }

                if (changed && (typeof(T).IsValueType))
                    el = (T)pl;

                if (changed || inspected == index)
                    isPrevious = true;

                return true;
            }

            void InspectNullElement(ref T el)
            {
                ElementData ed = listMeta?[index];
            
                if (typeof(Object).IsAssignableFrom(typeof(T)))
                {
                    if (ed != null)
                    {
                        object obj = el;

                        if (ed.PEGI_inList<T>(ref obj))
                        {
                            el = (T)obj;
                            isPrevious = true;
                        }
                    }
                    else
                    {

                        var tmp = el as Object;
                        if (Edit(ref tmp, typeof(T), 200))//edit(ref tmp))
                            el = (T)(object)tmp;
                    }
                    return;
                }

                if (typeof(string).IsAssignableFrom(typeof(T))) 
                {
                    if ("Instanciate string".PL().Click())
                        el = (T)((object)"");
                    return;
                }
               
                "{0}: NULL {1}".F(index, typeof(T).ToPegiStringType()).PL(150).Write();

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

                if (el is string tmp) 
                {
                    if (Edit(ref tmp))
                    {
                        el = (T)(object)tmp;
                        isPrevious = true;
                    }
                    return;
                }

                if (!isShown && el.GetNameForInspector().PL(toolTip: Msg.InspectElement.GetText(), RemainingLength(otherElements: DEFAULT_BUTTON_SIZE * 2 + 10)).ClickLabel())
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

                if (el.GetNameForInspector().PL("Inspect", RemainingLength(DEFAULT_BUTTON_SIZE * 2 + 10)).ClickLabel())
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
                    "List MEta is Null".PL().WriteWarning(); Nl();
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

            if ("Add {0}".F(typeof(T).ToPegiStringType()).PL().Click())
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
                    "List Meta is Null".PL().WriteWarning(); Nl();
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
                Icon.List.Draw();
                Nl();
                "List of {0} is null".F(listMeta == null ? typeof(T).ToPegiStringType() : listMeta.Label).PL().Write_Hint();

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
                        "Empty List of {0}".F(listMeta == null ? typeof(T).ToPegiStringType() : listMeta.Label).PL(Styles.Text.Header).Nl();
                    }
                    else
                    {
                        foreach (var el in collectionInspector.InspectionIndexes(list, listMeta))
                        {
                            int i = collectionInspector.Index;

                            /*
                            if (el.IsNullOrDestroyed_Obj())
                            {
                                if (typeof(Object).IsAssignableFrom(typeof(T)))
                                {
                                    var us = el as Object;

                                    if (Edit(ref us, typeof(T), allowSceneObjects: listMeta==null ? true : listMeta.allowScreenObject))
                                        list[i] = (T)((object)us);
                                }
                            }
                            else
                            {*/
                                InspectValueInList(el, list, i, ref inspected, listMeta);
                           // }

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

        public static ChangesToken Search_List<T>(this CollectionInspectorMeta listMeta, List<T> list)
        {
            var changed = ChangeTrackStart();



            /*
            using (collectionInspector.Write_Search_ListLabel(listMeta, list))
            {
                if (listMeta == null)
                {
                    "List Meta is Null".PegiLabel().WriteWarning(); Nl();
                }
                else
                {
                    
                    Edit_List(list, ref listMeta.inspectedElement_Internal, out _, listMeta).OnChanged(listMeta.OnChanged);
                }
            }*/

            return changed;
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
                "List of {0} is null".F(typeof(T)).PL().Write();

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
                                    : "is NUll").PL().Write();
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

        private static Color Lambda_Color(Color val)
        {
            Edit(ref val);
            return val;
        }

        private static Color32 Lambda_Color(Color32 val)
        {
            Edit(ref val);
            return val;
        }

        private static int Lambda_int(int val)
        {
            Edit(ref val);
            return val;
        }

        private static string Lambda_string_role(string val)
        {
            var role = listElementsRoles.TryGetObj(collectionInspector.Index);
            if (role != null)
                role.GetNameForInspector().PL(90).Edit(ref val);
            else Edit(ref val);

            return val;
        }

        public static string Lambda_string(string val)
        {
            Edit(ref val);
            return val;
        }

        public static ChangesToken Edit_List(this TextLabel label, List<int> list) =>
            label.Edit_List(list, Lambda_int);

        public static ChangesToken Edit_List(this TextLabel label, List<Color> list) =>
            label.Edit_List(list, Lambda_Color);

        public static ChangesToken Edit_List(this TextLabel label, List<Color32> list) =>
            label.Edit_List(list, Lambda_Color);

        public static ChangesToken Edit_List(this TextLabel label, List<string> list) =>
            label.Edit_List(list, Lambda_string);
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
                "List of {0} is null".F(typeof(T).ToPegiStringType()).PL().Write();

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
                                    : "is NUll").PL().Write();
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
        internal interface ICollectionInspector<T>
        {
            void Set(T val);
        }

        internal class KeyValuePairInspector<T,G> : ICollectionInspector<KeyValuePair<T,G>>, ISearchable, INeedAttention
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
                if (NeedsAttention(_pair.Value, out string msg))
                    "{0} at {1}".F(msg, _pair.Key.GetNameForInspector());
                return msg;
            }

            public IEnumerator SearchKeywordsEnumerator()
            {
                yield return _pair.Value;
                yield return _pair.Key;
            }
        }

        private static void WriteNullDictionary_Internal<T>() => "NULL {0} Dictionary".PL().Write();
        

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

            "Key".PL(60).Edit(ref _tmpKeyInt);

            if (dic.ContainsKey(_tmpKeyInt))
            {
                if (Icon.Refresh.Click("Find Free index"))
                {
                    while (dic.ContainsKey(_tmpKeyInt))
                        _tmpKeyInt++;
                }
                "Key {0} already exists".F(_tmpKeyInt).PL().WriteWarning();
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
            if (!dictionaryNamesForAddNewElement.TryGetValue(typeof(TValue), out string newElementName))
                newElementName = defaultElementName;

            var changed = ChangeTrackStart();
            if (dic == null)
            {
                WriteNullDictionary_Internal<TValue>();
                return changed;
            }

            if ("Key".PL(60).Edit(ref newElementName).IgnoreChanges())
                dictionaryNamesForAddNewElement[typeof(TValue)] = newElementName;

            var suggestedName = "{0} {1}".F(defaultElementName.Length > 0 ? defaultElementName : NEW_ELEMENT, dic.Count);

            if (!suggestedName.Equals(newElementName) && Icon.Refresh.Click())
                newElementName = suggestedName;

            if (dic.ContainsKey(newElementName))
            {
                Nl();
                "Key {0} already exists".F(newElementName).PL().WriteWarning();
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
                    if (value is IGotStringId name)
                        name.StringId = newElementName;

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
            using (collectionInspector.Write_Search_DictionaryLabel(dic.ToString().PL(), ref inspected, dic))
            {
                return Edit_Dictionary_Internal(dic, ref inspected, showKey: showKey);
            }
        }

        public static ChangesToken Edit_Dictionary<TKey, TValue>(Dictionary<TKey, TValue> dic, ref int inspected, bool showKey = true)
        {
            using (collectionInspector.Write_Search_DictionaryLabel(QcSharp.AddSpacesInsteadOfCapitals(dic.ToString().SimplifyTypeName()).PL(), ref inspected, dic))
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
                return Edit_Dictionary_Internal(dic, Lambda_string, listMeta: listMeta);
            }
        }
        
        public static ChangesToken Edit_Dictionary(this TextLabel label, Dictionary<string, string> dic)
        {
            int inspected = -1;
            using (collectionInspector.Write_Search_DictionaryLabel(label, ref inspected, dic))
            {
                return Edit_Dictionary_Internal(dic, Lambda_string);
            }
        }

        public static ChangesToken Edit_Dictionary(this TextLabel label, Dictionary<int, string> dic, List<string> roles)
        {
            int inspected = -1;
            using (collectionInspector.Write_Search_DictionaryLabel(label, ref inspected, dic))
            {
                listElementsRoles = roles;
                var changes = Edit_Dictionary_Internal(dic, Lambda_string_role, false);
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
                            itemKey.GetNameForInspector().PL(50).Write_ForCopy();

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
                        && Icon.Delete.ClickConfirm(confirmationTag: "DelDicEl"+collectionInspector.Index, toolTip: "Delete {0} (ID: {1})".F(item.Value.ToString(), itemKey.ToString())))
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
                                    itemKey.GetNameForInspector().PL(0.3f).Write_ForCopy();
                                }
                            }

                            bool TryEditStringKey()
                            {
                                bool keyHandled = false;

                                if (itemKey is string strKey)
                                {
                                    IGotStringId iGotName = item.IsNullOrDestroyed_Obj() ? null : item.Value as IGotStringId;

                                    try
                                    {
                                        if (nameIsKey && iGotName != null)
                                        {
                                            keyHandled = true;

                                            var theName = iGotName.StringId;

                                            if (theName.IsNullOrEmpty())
                                            {
                                                "NULL NAME".PL(60).Write();
                                            }
                                            else if (!theName.Equals(strKey))
                                            {
                                                var strDic = dic as Dictionary<string, TValue>;

                                                if (strDic.ContainsKey(theName))
                                                    "Name exists as Key".PL(90).Write();
                                                else
                                                {
                                                    if ("Key<-".PL("Override Key with Name").ClickUnFocus())
                                                    {
                                                        keyToReplace = strKey;
                                                        keyToReplaceWith = theName;
                                                    }

                                                    if ("->Name".PL("Override Name with Key").ClickUnFocus())
                                                        iGotName.StringId = strKey;
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
                    "Array Meta is Null".PL().WriteWarning(); Nl();
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
                if (Msg.Init.F(Msg.Array).PL().ClickUnFocus().Nl())
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
                    Internal_String(go.name, text, ref matched) ||
                    Internal_ByISearchable(go.GetComponent<ISearchable>(), text, ref matched) ||
                    Internal_ByAttention(go.GetComponent<INeedAttention>(), text, ref matched);
                }
              
                return false;
            }

            private static int _reffRecursionBlock;

            public static bool MatchObjectToStrings(object obj, string[] text, ref bool[] matched)
            {
                if (_reffRecursionBlock > 16) 
                {
                    Debug.LogError("Reference recursion for " + obj);
                    return false;
                }

                if (Internal_ByISearchable(QcUnity.TryGetInterfaceFrom<ISearchable>(obj), text, ref matched))
                    return true;

                if (Internal_ByAttention(QcUnity.TryGetInterfaceFrom<INeedAttention>(obj), text, ref matched))
                    return true;

                if (Internal_String(obj.ToString(), text, ref matched))
                    return true;

                if (Internal_ICollection(obj as ICollection, text, ref matched))
                    return true;
                
                if (obj is IPEGI_Reference reff)
                {
                    _reffRecursionBlock++;
                    using (QcSharp.DisposableAction(() => _reffRecursionBlock--))
                    {
                        var refObj = reff.GetReferencedObject();

                        if (refObj != null)
                        {
                            return MatchObjectToStrings(refObj, text, ref matched);
                        }
                    }
                }
               
                return false;
            }

            private static bool Internal_ByAttention(INeedAttention needAttention, string[] text, ref bool[] matched)
                => Internal_String(needAttention?.NeedAttention(), text, ref matched);

            public static bool Internal_ICollection(ICollection collection, string[] text, ref bool[] matched)
            {
                if (collection == null)
                    return false;

                foreach (var el in collection) 
                {
                    if (Match(el, text, ref matched))
                        return true;
                }
            
                return false;
            }

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

            private static bool _searchableLoopLogged = false;

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

                            if (enumerators.Count > 32) 
                            {
                                if (!_searchableLoopLogged)
                                {
                                    Debug.LogError("Enumerator recursion overflow. Logging all");
                                    foreach (var e in enumerators) 
                                    {
                                        Debug.LogError(e.ToString());
                                    }
                                    _searchableLoopLogged = true;
                                }

                                return false;
                            }

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

                                if (el is string str) 
                                {
                                  //  var str = el as string;
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

            public void ToggleSearch(IEnumerable collection, TextLabel label, bool showSearchByWarning = false) => ToggleSearch(collection, label.label, showSearchByWarning: showSearchByWarning);

            public void ToggleSearch(IEnumerable collection, string label = "", bool showSearchByWarning = false)
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

                if (active && showSearchByWarning)
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

        #region Meta
        [System.Flags]
        internal enum CollectionInspectParams
        {
            None = 0,
            allowDeleting = 1,
            allowReordering = 2,
            showAddButton = 4,
            showEditListButton = 8,
            showSearchButton = 16,
            showDictionaryKey = 32,
            allowDuplicates = 64,
            showCopyPasteOptions = 128,
            nameIsDictionaryKey = 256,
        }

        [Serializable]
        public class CollectionInspectorMeta : IPEGI, ICfg
        {
            [NonSerialized] public string Label = "list";
            [NonSerialized] private string _elementName;
            public string ElementName { get => _elementName.IsNullOrEmpty() ? Label : _elementName; set => _elementName = value; }

            [SerializeField] internal int inspectedElement_Internal = -1;
            [NonSerialized] internal int previouslyInspectedElement = -1;
            [SerializeField] internal int listSectionStartIndex;
            [NonSerialized] internal bool useOptimalShowRange = true;
            [NonSerialized] internal int itemsToShow = 10;
            [NonSerialized] internal Dictionary<int, ElementData> elementDatas = new();
            [NonSerialized] internal bool inspectListMeta = false;
            [NonSerialized] private CollectionInspectParams _config;
            [NonSerialized] internal readonly SearchData searchData = new();
            [NonSerialized] internal bool allowScreenObject = true;

            private readonly PlayerPrefValue.Int _playerPref = null;
            private readonly Gate.Bool _playerPrefsChecked = new();

            public int InspectedElement
            {
                get
                {
                    if (_playerPrefsChecked.TryChange(true)) 
                    {
                        if (_playerPref!= null)
                            inspectedElement_Internal = _playerPref.GetValue();
                    }

                    return inspectedElement_Internal;
                }
                set
                {
                    inspectedElement_Internal = value;
                    _playerPref?.SetValue(value);
                }
            }

            internal bool this[CollectionInspectParams param]
            {
                get => (_config & param) == param;
                set
                {
                    if (value)
                    {
                        _config |= param;
                    }
                    else
                    {
                        _config &= ~param;
                    }
                }
            }
            public ElementData this[int i]
            {
                get
                {
                    elementDatas.TryGetValue(i, out ElementData dta);
                    return dta;
                }
            }

            public bool IsAnyEntered
            {
                get => InspectedElement != -1;
                set
                {
                    if (!value)
                        InspectedElement = -1;
                }
            }

            internal void OnChanged()
            {
                InspectedElement = inspectedElement_Internal;
            }

            internal List<int> GetSelectedElements()
            {
                var sel = new List<int>();
                foreach (var e in elementDatas)
                    if (e.Value.selected)
                        sel.Add(e.Key);
                return sel;
            }

            internal bool GetIsSelected(int ind)
            {
                if (elementDatas.TryGetValue(ind, out var el))
                    return el.selected;
                return el != null && el.selected;
            }

            internal void SetIsSelected(int ind, bool value)
            {
                if (elementDatas.TryGetValue(ind, out var el))
                    el.selected = value;
                else if (value)
                {
                    el = new ElementData
                    {
                        selected = true
                    };
                    elementDatas[ind] = el;
                }
            }

            #region Inspector


            [NonSerialized] private readonly EnterExitContext _context = new();

            void IPEGI.Inspect()
            {
                using (_context.StartContext())
                {
                    Nl();
                    if (!_context.IsAnyEntered)
                    {
                        "List Label".PL(70).Edit(ref Label).Nl();
                        "Config".PL().Edit_EnumFlags(ref _config).Nl();
                    }

                    "Elements".PL().Edit_Dictionary(elementDatas);
                }
            }

            public CfgEncoder Encode() => new CfgEncoder().Add_IfNotNegative("ind", InspectedElement);

            public void DecodeTag(string key, CfgData data)
            {
                switch (key)
                {
                    case "ind": InspectedElement = data.ToInt(); break;
                }
            }
            #endregion

            public CollectionInspectorMeta()
            {
                this[CollectionInspectParams.showAddButton] = true;
                this[CollectionInspectParams.allowDeleting] = true;
                this[CollectionInspectParams.allowReordering] = true;
            }

            public CollectionInspectorMeta(string labelName,
                bool allowDeleting = true,
                bool allowReordering = true,
                bool showAddButton = true,
                bool showEditListButton = true,
                bool showSearchButton = true,
                bool showDictionaryKey = true,
                bool showCopyPasteOptions = false,
                bool nameIsDictionaryKey = true,
                string playerPrefsIndex = null)
            {

                Label = labelName;

                this[CollectionInspectParams.showAddButton] = showAddButton;
                this[CollectionInspectParams.allowDeleting] = allowDeleting;
                this[CollectionInspectParams.allowReordering] = allowReordering;
                this[CollectionInspectParams.showEditListButton] = showEditListButton;
                this[CollectionInspectParams.showSearchButton] = showSearchButton;
                this[CollectionInspectParams.showDictionaryKey] = showDictionaryKey;
                this[CollectionInspectParams.showCopyPasteOptions] = showCopyPasteOptions;
                this[CollectionInspectParams.nameIsDictionaryKey] = nameIsDictionaryKey;

                if (!playerPrefsIndex.IsNullOrEmpty())
                {
                    _playerPref = new PlayerPrefValue.Int("pegi/Col/" + playerPrefsIndex, defaultValue: -1);
                }
            }
        }

        public class ElementData
        {
            public bool selected;

            internal bool PEGI_inList<T>(ref object obj)
            {
                var changed = ChangeTrackStart();

                if (typeof(T).IsUnityObject())
                {
                    var uo = obj as UnityEngine.Object;
                    if (Edit(ref uo))
                        obj = uo;
                }

                return changed;
            }
        }
        #endregion

        #region Internal
        internal class CollectionInspector : System.IDisposable
        {
            private const int SCROLL_ARROWS_WIDTH = 190;
            private const int SCROLL_ARROWS_HEIGHT = 20;
            private int DEFAULT_MAX_ELEMENTS_ON_SCREEN => PaintingGameViewUI ? 10 : 20;

            public int Index { get; set; } = -1;
            public IList reordering;
            public TextLabel currentListLabel = new();
            public System.Array _editingArrayOrder;
            public readonly HashSet<int> selectedEls = new();
            public object previouslyEntered;

            private readonly Dictionary<IEnumerable, int> Indexes = new();
            private bool _searching;
            private List<int> filteredList;
            private int _sectionSizeOptimal;
            private int _count;
            private List<int> _copiedElements = new();
            private bool cutPaste;
            private readonly Dictionary<int, int> SectionOptimal = new();
            private static IList addingNewOptionsInspected;
            private string addingNewNameHolder = "Name";
            private bool exitOptionHandled;
            private static IList listCopyBuffer;
            private int _lastElementToShow;
            private int _sectionStartIndex;
            private SearchData searchData; // IN META
            private bool _scrollDownRequested;
            private bool allowDuplicants; // IN META
            private readonly List<System.IDisposable> _toDispose = new();

            public void Dispose() => End();
            public void End()
            {
                currentListLabel = new TextLabel();
                foreach (var toDisp in _toDispose)
                    toDisp.Dispose();

                _toDispose.Clear();

                Space();
            }
            public IEnumerable<T> InspectionIndexes<T>(ICollection<T> collectionReference, CollectionInspectorMeta listMeta = null, ICollectionInspector<T> listElementInspector = null)
            {
                searchData = listMeta == null ? defaultSearchData : listMeta.searchData;

                #region Inspect Start

                var changed = ChangeTrackStart();

                if (_scrollDownRequested)
                    searchData.CloseSearch();

                searchData.SearchString(collectionReference, out _searching, out string[] searchby);

                _sectionStartIndex = 0;

                if (_searching)
                    _sectionStartIndex = searchData.InspectionIndexStart;
                else if (listMeta != null)
                    _sectionStartIndex = listMeta.listSectionStartIndex;
                else if (!Indexes.TryGetValue(collectionReference, out _sectionStartIndex))
                {
                    if (Indexes.Count > 100)
                    {
                        Debug.LogError("Inspector Indexes > 100. Clearing");
                        Indexes.Clear();
                    }
                    Indexes.Add(collectionReference, 0);
                }

                _count = collectionReference.Count;

                _lastElementToShow = _count;

                _sectionSizeOptimal = listMeta == null ? DEFAULT_MAX_ELEMENTS_ON_SCREEN : (listMeta.useOptimalShowRange ? GetOptimalSectionFor(_count) : listMeta.itemsToShow);

                if (_scrollDownRequested)
                {
                    SkrollToBottomInternal();
                }

                /*
                if (!_searching)
                {
                    while ((_sectionStartIndex > 0 && _sectionStartIndex >= _count))
                    {
                        _sectionStartIndex = Mathf.Max(0, _sectionStartIndex - _sectionSizeOptimal);
                    }
                }*/

                if (_sectionStartIndex >= _count)
                    _sectionStartIndex = Mathf.Max(0, _sectionStartIndex - _sectionSizeOptimal);

                Nl();

                bool needScrollArrows = _sectionStartIndex > 0 || _sectionSizeOptimal < _count;

                if (needScrollArrows)
                {
                    if (_sectionStartIndex > 0)
                    {

                        if (_sectionStartIndex > _sectionSizeOptimal && Icon.UpLast.ClickUnFocus("To First element"))
                            _sectionStartIndex = 0;

                        if (Icon.Up.Click("To previous elements of the list. ", SCROLL_ARROWS_WIDTH, SCROLL_ARROWS_HEIGHT).UnfocusOnChange())
                        {
                            _sectionStartIndex = Mathf.Max(0, _sectionStartIndex - _sectionSizeOptimal + 1);
                            if (_sectionStartIndex == 1)
                                _sectionStartIndex = 0;
                        }

                        ".. {0}; ".F(_sectionStartIndex - 1).PL().Write();

                    }
                    else
                        Icon.UpLast.Write("Is the first section of the list.", SCROLL_ARROWS_WIDTH, SCROLL_ARROWS_HEIGHT);
               
                    Nl();
                }

                #endregion

                Styles.InList = true;

                using (QcSharp.DisposableAction(() => Styles.InList = false))
                {
                    _toDispose.Add(Styles.Background.List.SetDisposible());

                    if (!_searching)
                    {
                        _lastElementToShow = Mathf.Min(_count, _sectionStartIndex + _sectionSizeOptimal);
                        Index = _sectionStartIndex;

                        if (collectionReference is IList<T> list)
                        {
                            for (; Index < collectionReference.Count; Index++)
                            {
                                var lel = list[Index];
                                using (SetListElementReadabilityBackground(Index))
                                {
                                    yield return lel;
                                }

                                if (Index >= _lastElementToShow)
                                    break;
                            }
                        }
                        else
                        {
                            foreach (var el in System.Linq.Enumerable.Skip(collectionReference, _sectionStartIndex))
                            {
                                using (SetListElementReadabilityBackground(Index))
                                {
                                    yield return el;
                                }

                                if (Index >= _lastElementToShow)
                                    break;

                                Index++;
                            }
                        }

                        if ((_sectionStartIndex > 0) || (_count > _lastElementToShow))
                        {
                            Nl();
                            if (_count > _lastElementToShow)
                            {
                                if (Icon.Down.Click("To next elements of the list. ", SCROLL_ARROWS_WIDTH, SCROLL_ARROWS_HEIGHT).UnfocusOnChange())
                                    _sectionStartIndex += _sectionSizeOptimal - 1;

                                if (Icon.DownLast.ClickUnFocus("To Last element"))
                                    SkrollToBottomInternal();

                                " {0}{1}".F(X_SYMBOL, _count - _lastElementToShow).PL().Write();
                            }
                            else if (_sectionStartIndex > 0)
                                Icon.DownLast.Write("Is the last section of the list. ", SCROLL_ARROWS_WIDTH, SCROLL_ARROWS_HEIGHT);
                        }
                    }
                    else
                    {

                        var sectionIndex = _sectionStartIndex;
                        filteredList = searchData.GetFilteredList(_count);
                        _lastElementToShow = Mathf.Min(_count, _sectionStartIndex + _sectionSizeOptimal);

                        while (sectionIndex < _lastElementToShow)
                        {
                            Index = -1;

                            if (filteredList.Count > sectionIndex)
                                Index = filteredList[sectionIndex];
                            else
                                Index = GetNextFiltered(collectionReference, searchby, listElementInspector);

                            if (Index != -1)
                            {
                                using (SetListElementReadabilityBackground(sectionIndex))
                                {
                                    yield return collectionReference.GetElementAt(Index);
                                }
                                sectionIndex++;
                            }
                            else break;
                        }

                        bool gotUnchecked = (searchData.UncheckedElement < _count - 1);
                        bool gotToShow = (filteredList.Count > _lastElementToShow) || gotUnchecked;

                        if (_sectionStartIndex > 0 || gotToShow)
                        {
                            Nl();
                            if (gotToShow)
                            {
                                if (Icon.Down.Click("To next elements of the list. ", SCROLL_ARROWS_WIDTH, SCROLL_ARROWS_HEIGHT).UnfocusOnChange())
                                    _sectionStartIndex += _sectionSizeOptimal - 1;

                                if (Icon.DownLast.ClickUnFocus("To Last element"))
                                {
                                    if (_searching)
                                        while (GetNextFiltered(collectionReference, searchby, listElementInspector) != -1) { }

                                    SkrollToBottomInternal();
                                }

                                if (!gotUnchecked)
                                    "{0}{1}".F(X_SYMBOL, filteredList.Count - _lastElementToShow).PL().Write();

                            }
                            else if (_sectionStartIndex > 0)
                                Icon.DownLast.Write("Is the last section of the list. ", SCROLL_ARROWS_WIDTH, SCROLL_ARROWS_HEIGHT);
                        }
                    }

                    if (changed)
                        SaveSectionIndex(collectionReference, listMeta);
                }
            }
            public void ListInstantiateNewName<T>()
            {
                Msg.New.GetText().PL(Msg.NameNewBeforeInstancing_1p.GetText().F(typeof(T).ToPegiStringType()), 30, Styles.Text.ExitLabel).Write();
                Edit(ref addingNewNameHolder);
            }

            private static string _listTypeSearch = "";

            public bool TryShowListCreateNewOptions<T>(List<T> lst, ref T added, CollectionInspectorMeta ld)
            {
                if (ld != null && !ld[CollectionInspectParams.showAddButton])
                    return false;

                if (reordering != null && reordering == lst)
                    return false;

                var type = typeof(T);

                var derrivedTypesExplicit = ICfgExtensions.TryGetDerivedClasses(type);

                var tagTypes = TaggedTypes<T>.DerrivedList;

                if (derrivedTypesExplicit == null && tagTypes == null)
                {
                    if (type.IsAbstract || type.IsInterface)
                    {
                        derrivedTypesExplicit = QcSharp.GetTypesAssignableFrom<T>();
                    }
                    else
                    {
                        return false;
                    }
                }

                var hasName = typeof(T).IsSubclassOf(typeof(Object)) || typeof(IGotStringId).IsAssignableFrom(typeof(T));

                if (hasName)
                    ListInstantiateNewName<T>();

                if (!hasName || addingNewNameHolder.Length > 1)
                {

                    var selectingDerrived = lst == addingNewOptionsInspected;

                    if (selectingDerrived)
                        Line(); Nl();

                    (derrivedTypesExplicit == null ? "Create new {0}".F(typeof(T).ToPegiStringType()) : "Create new {0}".F(typeof(T).ToPegiStringType())).PL(Styles.Text.ClickableText).Write();

                    Icon.Add.IsFoldout("Instantiate Class Options", ref selectingDerrived).Nl();

                    if (selectingDerrived)
                        addingNewOptionsInspected = lst;
                    else if (addingNewOptionsInspected == lst)
                        addingNewOptionsInspected = null;

                    if (selectingDerrived)
                    {
                        if (derrivedTypesExplicit != null)
                        {
                            string searchString = "";

                            if (derrivedTypesExplicit.Count > 5)
                            {
                                "Search".PL(width: 60, style: Styles.Text.FoldedOut).Edit(ref _listTypeSearch).Nl();
                                searchString = _listTypeSearch;
                            }

                            foreach (var t in derrivedTypesExplicit)
                            {
                                string typeName = t.ToPegiStringType();

                                if (searchString == null || typeName.Contains(searchString))
                                {
                                    typeName.PL().Write();
                                    if (Icon.Create.ClickUnFocus().Nl())
                                    {
                                        added = (T)System.Activator.CreateInstance(t);
                                        QcSharp.AddWithUniqueStringIdAndIndex(lst, added, addingNewNameHolder);
                                        SkrollToBottom();
                                    }
                                }
                            }
                        }

                        if (tagTypes != null)
                        {
                            var k = tagTypes.Keys;

                            int availableOptions = 0;

                            for (var i = 0; i < k.Count; i++)
                            {
                                if (tagTypes.CanAdd(i, lst))
                                {
                                    availableOptions++;

                                    tagTypes.DisplayNames[i].PL().Write();
                                    if (Icon.Create.ClickUnFocus().Nl())
                                    {
                                        added = (T)System.Activator.CreateInstance(tagTypes.TaggedTypes.GetValueOrDefault(k[i]));
                                        QcSharp.AddWithUniqueStringIdAndIndex(lst, added, addingNewNameHolder);
                                        SkrollToBottom();
                                    }
                                }

                            }

                            if (availableOptions == 0)
                                (k.Count == 0 ? "There no types derrived from {0}".F(typeof(T).ToString()) :
                                    "Existing types are restricted to one instance per list").PL().Write_Hint();

                        }

                    }
                }
                else
                    Icon.Add.GetText("Input a name for a new element", 40).Write();
                Nl();

                return true;
            }

            public ChangesToken TryShowListCreateNewOptions<T>(List<T> lst, ref T added, TaggedTypes.DerrivedList types, CollectionInspectorMeta ld)
            {
                if (ld != null && !ld[CollectionInspectParams.showAddButton])
                    return ChangesToken.False;

                if (reordering != null && reordering == lst)
                    return ChangesToken.False;

                var changed = false;

                var hasName = typeof(T).IsSubclassOf(typeof(Object)) || typeof(IGotStringId).IsAssignableFrom(typeof(T));

                if (hasName)
                    ListInstantiateNewName<T>();
                else
                    "Create new {0}".F(typeof(T).ToPegiStringType()).PL().Write();

                if (!hasName || addingNewNameHolder.Length > 1)
                {
                    var selectingDerrived = lst == addingNewOptionsInspected;

                    Icon.Add.IsFoldout("Instantiate Class Options", ref selectingDerrived).Nl();

                    if (selectingDerrived)
                        addingNewOptionsInspected = lst;
                    else if (addingNewOptionsInspected == lst)
                        addingNewOptionsInspected = null;

                    if (selectingDerrived)
                    {
                        var k = types.Keys;
                        for (var i = 0; i < k.Count; i++)
                        {
                            types.DisplayNames[i].PL().Write();
                            if (Icon.Create.ClickUnFocus().Nl())
                            {
                                changed = true;
                                added = (T)System.Activator.CreateInstance(types.TaggedTypes.GetValueOrDefault(k[i]));
                                QcSharp.AddWithUniqueStringIdAndIndex(lst, added, addingNewNameHolder);
                                SkrollToBottom();
                            }
                        }
                    }
                }
                else
                    Icon.Add.GetText("Input a name for a new element", 40).Write();
                Nl();

                return new ChangesToken(changed);
            }
            public void SkrollToBottom()
            {
                _scrollDownRequested = true;
            }

            private int GetOptimalSectionFor(int count)
            {
                int listShowMax = DEFAULT_MAX_ELEMENTS_ON_SCREEN;

                if (count < listShowMax)
                    return listShowMax;

                if (count > listShowMax * 3)
                    return listShowMax;

                if (SectionOptimal.TryGetValue(count, out _sectionSizeOptimal))
                    return _sectionSizeOptimal;

                var minDiff = 999;

                for (var i = listShowMax - 2; i < listShowMax + 2; i++)
                {
                    var difference = i - (count % i);

                    if (difference >= minDiff) continue;
                    _sectionSizeOptimal = i;
                    minDiff = difference;
                    if (difference == 0)
                        break;
                }

                SectionOptimal[count] = _sectionSizeOptimal;
                return _sectionSizeOptimal;
            }
            private int GetNextFiltered<T>(ICollection<T> collectionReference, string[] searchby, ICollectionInspector<T> inspector = null)
            {
                foreach (var reff in System.Linq.Enumerable.Skip(collectionReference, searchData.UncheckedElement))
                {
                    if (searchData.UncheckedElement >= _count)
                        return -1;

                    int index = searchData.UncheckedElement;

                    searchData.UncheckedElement++;

                    object target;

                    if (inspector != null)
                    {
                        inspector.Set(reff);
                        target = inspector;
                    }
                    else
                        target = reff;

                    var na = target as INeedAttention;

                    var msg = na?.NeedAttention();

                    if (!searchData.FilterByNeedAttention || !msg.IsNullOrEmpty())
                    {
                        if (searchby.IsNullOrEmpty() || target.SearchMatch_Obj_Internal(searchby))
                        {
                            filteredList.Add(index);
                            return index;
                        }
                    }
                }

                return -1;
            }
            private void SaveSectionIndex<T>(ICollection<T> list, CollectionInspectorMeta listMeta)
            {
                if (_searching)
                    searchData.InspectionIndexStart = _sectionStartIndex;
                else if (listMeta != null)
                    listMeta.listSectionStartIndex = _sectionStartIndex;
                else
                {
                    if (Indexes.Count > 100)
                    {
                        Debug.LogError("Collection Inspector Indexes > 100. Clearing...");
                        Indexes.Clear();
                    }
                    Indexes[list] = _sectionStartIndex;
                }
            }

            private void SkrollToBottomInternal()
            {

                if (!_searching)
                    _sectionStartIndex = _count - _sectionSizeOptimal;
                else
                    _sectionStartIndex = filteredList.Count - _sectionSizeOptimal;

                _sectionStartIndex = Mathf.Max(0, _sectionStartIndex);

                _scrollDownRequested = false;
            }
            private IDisposable SetListElementReadabilityBackground(int index)
            {
                return (index % 4) switch
                {
                    1 => SetBgColorDisposable(Styles.listReadabilityBlue),
                    3 => SetBgColorDisposable(Styles.listReadabilityRed),
                    _ => null,
                };
            }
            internal TextLabel GetCurrentListLabel<T>(CollectionInspectorMeta ld = null) =>
                ld != null
                    ? ld.Label.PL() :
                        (currentListLabel.IsInitialized ? currentListLabel : typeof(T).ToPegiStringType().PL());

            internal CollectionInspector Write_Search_DictionaryLabel<K, V>(CollectionInspectorMeta collectionMeta, Dictionary<K, V> dic) =>
                Write_Search_DictionaryLabel(collectionMeta.Label.PL(), ref collectionMeta.inspectedElement_Internal, dic, collectionMeta);
            internal CollectionInspector Write_Search_DictionaryLabel<K, V>(TextLabel label, ref int inspected, Dictionary<K, V> dic, CollectionInspectorMeta meta = null)
            {
                SearchData sd = meta == null ? defaultSearchData : meta.searchData;

                currentListLabel = label;

                bool inspecting = inspected != -1;

                if (!inspecting)
                    sd.ToggleSearch(dic, label, showSearchByWarning: typeof(INeedAttention).IsAssignableFrom(typeof(V)));
                else
                {
                    exitOptionHandled = true;
                    if (Icon.List.ClickUnFocus("{0} {1}{2}".F(Msg.ReturnToCollection.GetText(), X_SYMBOL, dic.Count)))
                        inspected = -1;
                }

                if (dic != null && inspected >= 0 && dic.Count > inspected)
                {
                    var el = dic.GetElementAt(inspected);

                    var keyName = el.Key.GetNameForInspector();
                    var valName = el.Value.GetNameForInspector();

                    bool isSubset = false;
                    string nameToShow = "";

                    if (valName != null)
                    {
                        if (valName.Contains(keyName))
                        {
                            isSubset = true;
                            nameToShow = valName;
                        }
                        else if (keyName.Contains(valName))
                        {
                            isSubset = true;
                            nameToShow = keyName;
                        }
                    }

                    if (meta != null && !meta[CollectionInspectParams.showDictionaryKey])
                        label = valName.PL();
                    else
                        label = (isSubset ? nameToShow : "{0}:{1}".F(keyName, valName)).PL();
                }
                else label = (dic == null || dic.Count < 6) ? label : label.AddCount(dic, true);

                label.width = RemainingLength(DEFAULT_BUTTON_SIZE * 2 + 10);
                label.style = Styles.ListLabel;

                if (label.ClickLabel() && inspected != -1)
                    inspected = -1;


                return this;
            }
            internal CollectionInspector Write_Search_ListLabel<T>(TextLabel label, ICollection<T> lst = null)
            {
                var notInsp = -1;
                return collectionInspector.Write_Search_ListLabel(label, ref notInsp, lst);
            }

            internal CollectionInspector Write_Search_ListLabel<T>(TextLabel label, ref int inspected, ICollection<T> lst)
            {
                currentListLabel = label;

                bool inspecting = inspected != -1;

                if (!inspecting)
                    defaultSearchData.ToggleSearch(lst, label, showSearchByWarning: typeof(INeedAttention).IsAssignableFrom(typeof(T)));
                else
                {
                    exitOptionHandled = true;
                    if (Icon.List.ClickUnFocus("{0} {1}{2}".F(Msg.ReturnToCollection.GetText(), X_SYMBOL, lst.Count)))
                        inspected = -1;
                }

                if (lst != null && inspected >= 0 && lst.Count > inspected)
                    label = lst.GetElementAt(inspected).GetNameForInspector().PL();
                else label = (lst == null || lst.Count < 6) ? label : label.AddCount(lst, true);

                label.width = RemainingLength(DEFAULT_BUTTON_SIZE * 2 + 10);
                label.style = Styles.ListLabel;

                if (label.ClickLabel() && inspected != -1)
                    inspected = -1;

                return this;
            }
            internal CollectionInspector Write_Search_ListLabel<T>(CollectionInspectorMeta ld, ICollection<T> lst)
            {
                if (ld == null)
                {
                    "Meta is Null. Could be due to ScriptableObject serializing private fields.".PL().WriteWarning();
                    return this;
                }

               // _toDispose.Add(Styles.Background.ListLabel.SetDisposible());

                currentListLabel = ld.Label.PL();

                if (!ld.IsAnyEntered && ld[CollectionInspectParams.showSearchButton])
                    ld.searchData.ToggleSearch(lst, ld.Label, showSearchByWarning: typeof(INeedAttention).IsAssignableFrom(typeof(T)));

                if (lst != null && ld.InspectedElement >= 0 && lst.Count > ld.InspectedElement)
                {
                    var el = lst.GetElementAt(ld.InspectedElement);
                    string nameToShow = el.GetNameForInspector();
                    currentListLabel = "{0}->{1}".F(ld.Label, nameToShow).PL();
                }
                else currentListLabel = ((lst == null || lst.Count < 6) ? ld.Label : ld.Label.AddCount(lst, true)).PL();


                if (ld.IsAnyEntered && lst != null)
                {
                    exitOptionHandled = true;
                    if (Icon.List.ClickUnFocus("{0} {1} {2}{3}".F(Msg.ReturnToCollection.GetText(), currentListLabel, X_SYMBOL, lst.Count)))
                        ld.IsAnyEntered = false;
                }

                currentListLabel.width = RemainingLength(DEFAULT_BUTTON_SIZE * 2 + 10);
                currentListLabel.style = Styles.ListLabel;

                if (currentListLabel.ClickLabel() && ld.InspectedElement != -1)
                    ld.InspectedElement = -1;

                return this;
            }
            internal ChangesToken ExitOrDrawPEGI<T>(T[] array, ref int index, CollectionInspectorMeta ld = null)
            {
                var changed = ChangeTrackStart();

                if (index >= 0)
                {
                    if (!exitOptionHandled && (array == null || index >= array.Length || Icon.List.ClickUnFocus("Return to {0} array".F(GetCurrentListLabel<T>(ld))).Nl()))
                        index = -1;
                    else
                    {
                        Nl();

                        object obj = array[index];
                        if (Nested_Inspect(ref obj))
                            array[index] = (T)obj;
                    }
                }

                exitOptionHandled = false;

                return changed;
            }
            internal ChangesToken ExitOrDrawPEGI<K, T>(Dictionary<K, T> dic, ref int index, CollectionInspectorMeta ld = null)
            {
                var changed = ChangeTrackStart();

                if (!exitOptionHandled && Icon.List.ClickUnFocus("{0}[{1}] of {2}".F(Msg.ReturnToCollection.GetText(), dic.Count, GetCurrentListLabel<T>(ld))).Nl())
                    index = -1;
                else
                {
                    Nl();

                    var item = dic.GetElementAt(index);
                    var key = item.Key;

                    object obj = dic[key];
                    if (Nested_Inspect(ref obj))
                        dic[key] = (T)obj;
                }

                exitOptionHandled = false;

                return changed;
            }
            internal ChangesToken ExitOrDrawPEGI<T>(List<T> list, ref int index, CollectionInspectorMeta ld = null)
            {
                var changed = ChangeTrackStart();

                if (!exitOptionHandled && Icon.List.ClickUnFocus("{0}[{1}] of {2}".F(Msg.ReturnToCollection.GetText(), list.Count, GetCurrentListLabel<T>(ld))).Nl())
                    index = -1;
                else
                {
                    Nl();

                    object obj = list[index];
                    if (Nested_Inspect(ref obj))
                        list[index] = (T)obj;
                }

                exitOptionHandled = false;

                return changed;
            }
            internal bool CollectionIsNull<T, V>(Dictionary<T, V> list)
            {
                if (list == null)
                {
                    "Dictionary of {0} is null".F(typeof(T).ToPegiStringType()).PL().Write();

                    /* if ("Initialize list".ClickUnFocus().nl())
                         list = new List<T>();
                     else*/
                    return true;
                }

                return false;
            }
            internal bool CollectionIsNull<T>(List<T> list)
            {
                if (list == null)
                {
                    "List of {0} is null".F(typeof(T).ToPegiStringType()).PL().Write();

                    /* if ("Initialize list".ClickUnFocus().nl())
                         list = new List<T>();
                     else*/
                    return true;
                }

                return false;
            }
            internal ChangesToken List_DragAndDropOptions<T>(List<T> list, CollectionInspectorMeta meta = null) where T : Object
            {
                var changed = false;
#if UNITY_EDITOR

                var tracker = UnityEditor.ActiveEditorTracker.sharedTracker;

                if (tracker.isLocked == false && Icon.Unlock.ClickUnFocus("Lock Inspector Window"))
                    tracker.isLocked = true;

                if (tracker.isLocked && Icon.Lock.ClickUnFocus("Unlock Inspector Window"))
                {
                    tracker.isLocked = false;

                    var mb = PegiEditorOnly.SerObj.targetObject as MonoBehaviour;

                    QcUnity.FocusOn(mb ? mb.gameObject : PegiEditorOnly.SerObj.targetObject);

                }

                var dpl = meta?[CollectionInspectParams.allowDuplicates] ?? allowDuplicants;

                foreach (var ret in PegiEditorOnly.DropAreaGUI<T>())
                {
                    if (dpl || !list.Contains(ret))
                    {
                        list.Add(ret);
                        changed = true;
                    }
                }



#endif
                return new ChangesToken(changed);
            }

            private void SetSelected<T>(CollectionInspectorMeta meta, List<T> list, bool val)
            {
                if (meta == null)
                {
                    for (var i = 0; i < list.Count; i++)
                    {
                        if (val)
                            selectedEls.Add(i);
                        else
                            selectedEls.Remove(i);
                    }
                    return;
                }

                for (var i = 0; i < list.Count; i++)
                    meta.SetIsSelected(i, val);
            }
            private void TryMoveCopiedElement<T>(List<T> list, bool isAllowDuplicants)
            {
                bool errorShown = false;

                for (var i = _copiedElements.Count - 1; i >= 0; i--)
                {
                    var srcInd = _copiedElements[i];
                    var e = listCopyBuffer.TryGetObj(srcInd);

                    if (QcSharp.CanAdd(list, ref e, out T conv, !isAllowDuplicants))
                    {
                        list.Add(conv);
                        listCopyBuffer.RemoveAt(srcInd);
                    }
                    else if (!errorShown)
                    {
                        errorShown = true;
                        Debug.LogError("Couldn't add some of the elements");
                    }
                }

                if (!errorShown)
                    listCopyBuffer = null;
            }
            internal ChangesToken Edit_Array_Order<T>(ref T[] array, CollectionInspectorMeta listMeta = null)
            {
                var changed = ChangeTrackStart();

                if (array != _editingArrayOrder)
                {
                    if ((listMeta == null || listMeta[CollectionInspectParams.showEditListButton]) && Icon.Edit.ClickUnFocus(Msg.MoveCollectionElements, 28))
                        _editingArrayOrder = array;
                }

                else if (Icon.Done.ClickUnFocus(Msg.FinishMovingCollectionElements.GetText(), 28).Nl())
                {
                    _editingArrayOrder = null;
                }

                if (array != _editingArrayOrder) return changed;

                var derivedClasses = ICfgExtensions.TryGetDerivedClasses(typeof(T));

                for (var i = 0; i < array.Length; i++)
                {
                    if (listMeta == null || listMeta[CollectionInspectParams.allowReordering])
                    {
                        if (i > 0)
                        {
                            if (Icon.Up.ClickUnFocus("Move up"))
                                QcSharp.Swap(ref array, i, i - 1);
                        }
                        else
                            Icon.UpLast.Draw("Last");

                        if (i < array.Length - 1)
                        {
                            if (Icon.Down.ClickUnFocus("Move down"))
                                QcSharp.Swap(ref array, i, i + 1);
                        }
                        else Icon.DownLast.Draw();
                    }

                    var el = array[i];

                    var isNull = el.IsNullOrDestroyed_Obj();

                    if (listMeta == null || listMeta[CollectionInspectParams.allowDeleting])
                    {
                        if (!isNull && typeof(T).IsUnityObject())
                        {
                            if (Icon.Delete.ClickUnFocus(Msg.MakeElementNull))
                                array[i] = default;
                        }
                        else
                        {
                            if (Icon.Close.ClickUnFocus(Msg.RemoveFromCollection))
                            {
                                QcSharp.Remove(ref array, i);
                                i--;
                            }
                        }
                    }

                    if (!isNull && derivedClasses != null)
                    {
                        var ty = el.GetType();
                        if (Select(ref ty, derivedClasses, el.GetNameForInspector()))
                            array[i] = (el as ICfgCustom).TryDecodeInto<T>(ty);
                    }

                    if (!isNull)
                        el.GetNameForInspector().PL().Write();
                    else
                        "{0} {1}".F(Icon.Empty.GetText(), typeof(T).ToPegiStringType()).PL().Write();

                    Nl();
                }

                return changed;
            }
            internal bool Edit_List_Order<T>(List<T> list, CollectionInspectorMeta listMeta = null)
            {
                var changed = ChangeTrackStart();

                var sd = listMeta == null ? defaultSearchData : listMeta.searchData;

                if (list != collectionInspector.reordering)
                {
                    if (!ReferenceEquals(sd.FilteredList, list) && (listMeta == null || listMeta[CollectionInspectParams.showEditListButton]) &&
                        Icon.Edit.ClickUnFocus(Msg.MoveCollectionElements, 28).IgnoreChanges(LatestInteractionEvent.Click))
                        reordering = list;
                }
                else if (Icon.Done.ClickUnFocus(Msg.FinishMovingCollectionElements, 28))
                    reordering = null;

                if (list != collectionInspector.reordering)
                    return changed;

                if (!PaintingGameViewUI)
                {
                    Nl();
#if UNITY_EDITOR
                    PegiEditorOnly.Reorder_List(list, listMeta);
                    Nl();
#endif
                }
                else

                #region Playtime UI reordering

                {
                    var derivedClasses = ICfgExtensions.TryGetDerivedClasses(typeof(T));

                    foreach (var el in collectionInspector.InspectionIndexes(list, listMeta))
                    {
                        int i = collectionInspector.Index;

                        if (listMeta == null || listMeta[CollectionInspectParams.allowReordering])
                        {

                            if (i > 0)
                            {
                                if (Icon.Up.ClickUnFocus("Move up"))
                                    list.Swap(i - 1);

                            }
                            else
                                Icon.UpLast.Draw("Last");

                            if (i < list.Count - 1)
                            {
                                if (Icon.Down.ClickUnFocus("Move down"))
                                    list.Swap(i);
                            }
                            else Icon.DownLast.Draw();
                        }

                        var isNull = el.IsNullOrDestroyed_Obj();

                        if (listMeta == null || listMeta[CollectionInspectParams.allowDeleting])
                        {

                            if (!isNull && typeof(T).IsUnityObject())
                            {
                                if (Icon.Delete.ClickUnFocus(Msg.MakeElementNull))
                                    list[i] = default;
                            }
                            else
                            {
                                if (Icon.Close.ClickUnFocus(Msg.RemoveFromCollection))
                                {
                                    list.RemoveAt(Index);
                                    Index--;
                                    _lastElementToShow--;
                                }
                            }
                        }


                        if (!isNull && derivedClasses != null)
                        {
                            var ty = el.GetType();
                            if (Select(ref ty, derivedClasses, el.GetNameForInspector()))
                                list[i] = (el as ICfgCustom).TryDecodeInto<T>(ty);
                        }

                        if (!isNull)
                            el.GetNameForInspector().PL().Write();
                        else
                            "{0} {1}".F(Icon.Empty.GetText(), typeof(T).ToPegiStringType()).PL().Write();

                        Nl();
                    }

                }

                #endregion

                #region Select

                var selectedCount = 0;

                if (listMeta == null)
                {
                    for (var i = 0; i < list.Count; i++)
                        if (selectedEls.Contains(i))
                            selectedCount++;
                }
                else
                    for (var i = 0; i < list.Count; i++)
                        if (listMeta.GetIsSelected(i))
                            selectedCount++;

                if (selectedCount > 0 && Icon.UnSelected.Click().IgnoreChanges(LatestInteractionEvent.Click))
                    SetSelected(listMeta, list, false);

                if (selectedCount == 0 && list.Count > 0 && Icon.Selected.Click().IgnoreChanges(LatestInteractionEvent.Click))
                    SetSelected(listMeta, list, true);


                #endregion

                #region Copy, Cut, Paste, Move 

                var duplicants = listMeta != null ? listMeta[CollectionInspectParams.allowDuplicates] : allowDuplicants;

                if (list.Count > 1 && typeof(IGotIndex).IsAssignableFrom(typeof(T)))
                {
                    bool down = Icon.Down.Click("Sort Ascending");

                    if (down || Icon.Up.Click("Sort Descending"))
                    {
                        list.Sort((emp1, emp2) =>
                        {
                            if (emp1 is not IGotIndex igc1 || emp2 is not IGotIndex igc2)
                                return 0;

                            return (down ? 1 : -1) * (igc1.IndexForInspector - igc2.IndexForInspector);

                        });
                    }
                }

                if (listCopyBuffer != null)
                {

                    if (Icon.Close.ClickUnFocus("Clean buffer").IgnoreChanges(LatestInteractionEvent.Exit))
                        listCopyBuffer = null;

                    bool same = listCopyBuffer == list;

                    if (same && !cutPaste)
                        "DUPLICATE:".PL("Selected elements are from this list", 60).Write();

                    if (typeof(T).IsUnityObject())
                    {
                        if (!cutPaste && Icon.Paste.ClickUnFocus(same
                                ? Msg.TryDuplicateSelected.GetText()
                                : "{0} Of {1} to here".F(Msg.TryDuplicateSelected.GetText(),
                                    listCopyBuffer.GetNameForInspector())))
                        {
                            foreach (var e in _copiedElements)
                                list.TryAdd(listCopyBuffer.TryGetObj(e), !duplicants);
                        }

                        if (!same && cutPaste && Icon.Move.ClickUnFocus("Try Move References Of {0}".F(listCopyBuffer)))
                            collectionInspector.TryMoveCopiedElement(list, duplicants);
                    }
                    else
                    {

                        if (!cutPaste && Icon.Paste.ClickUnFocus(same
                                ? "Try to duplicate selected references"
                                : "Try Add Deep Copy {0}".F(listCopyBuffer.GetNameForInspector())))
                        {
                            foreach (var e in _copiedElements)
                            {
                                var el = listCopyBuffer.TryGetObj(e);

                                if (el != null)
                                {
                                    if (el is ICfgCustom istd)
                                    {
                                        var ret = (T)System.Activator.CreateInstance(el.GetType());

                                        (ret as ICfgCustom).Decode(istd.Encode().CfgData);

                                        list.TryAdd(ret);
                                    }

                                    //list.TryAdd(istd.CloneCfg());
                                    else
                                        list.TryAdd(JsonUtility.FromJson<T>(JsonUtility.ToJson(el)));
                                }
                            }
                        }

                        if (!same && cutPaste && Icon.Move.ClickUnFocus("Try Move {0}".F(listCopyBuffer)))
                            collectionInspector.TryMoveCopiedElement(list, duplicants);
                    }

                }
                else if (selectedCount > 0)
                {
                    var copyOrMove = false;

                    if (Icon.Copy.ClickUnFocus("Copy selected elements").IgnoreChanges(LatestInteractionEvent.Click))
                    {
                        cutPaste = false;
                        copyOrMove = true;
                    }

                    if (Icon.Cut.ClickUnFocus("Cut selected elements").IgnoreChanges(LatestInteractionEvent.Click))
                    {
                        cutPaste = true;
                        copyOrMove = true;
                    }

                    if (copyOrMove)
                    {
                        listCopyBuffer = list;
                        _copiedElements = listMeta != null ? listMeta.GetSelectedElements() : selectedEls.ToList();
                    }
                }

                #endregion

                #region Clean & Delete

                if (list != listCopyBuffer)
                {
                    if ((listMeta == null || listMeta[CollectionInspectParams.allowDeleting]) && list.Count > 0)
                    {
                        var nullOrDestroyedCount = 0;

                        for (var i = 0; i < list.Count; i++)
                            if (list[i].IsNullOrDestroyed_Obj())
                                nullOrDestroyedCount++;

                        if (nullOrDestroyedCount > 0 && Icon.Refresh.ClickUnFocus("Remove all null elements"))
                        {
                            for (var i = list.Count - 1; i >= 0; i--)
                                if (list[i].IsNullOrDestroyed_Obj())
                                    list.RemoveAt(i);

                            SetSelected(listMeta, list, false);
                        }
                    }

                    if ((listMeta == null || listMeta[CollectionInspectParams.allowDeleting]) && list.Count > 0)
                    {
                        if (selectedCount > 0 &&
                            Icon.Delete.ClickConfirm("delLstPegi", list, "Delete {0} Selected".F(selectedCount)))
                        {
                            if (listMeta == null)
                            {
                                for (var i = list.Count - 1; i >= 0; i--)
                                    if (selectedEls.Contains(i))
                                        list.RemoveAt(i);
                            }
                            else
                                for (var i = list.Count - 1; i >= 0; i--)
                                    if (listMeta.GetIsSelected(i))
                                        list.RemoveAt(i);

                            SetSelected(listMeta, list, false);
                        }
                    }
                }

                #endregion

                if (listMeta != null)
                {
                    if (listMeta.inspectListMeta)
                    {
                        if (Icon.Exit.ClickUnFocus().IgnoreChanges(LatestInteractionEvent.Exit))
                            listMeta.inspectListMeta = false;
                    }
                    else if (Icon.Config.ClickUnFocus().IgnoreChanges(LatestInteractionEvent.Enter))
                        listMeta.inspectListMeta = true;
                }


                if (listMeta != null && listMeta.inspectListMeta)
                    listMeta.Nested_Inspect();
                else if (typeof(Object).IsAssignableFrom(typeof(T)) || !listCopyBuffer.IsNullOrEmpty())
                {
                    "Allow Duplicants".PL("Will add elements to the list even if they are already there", 120).Toggle(ref duplicants).IgnoreChanges(LatestInteractionEvent.Click);

                    if (listMeta != null)
                        listMeta[CollectionInspectParams.allowDuplicates] = duplicants;
                    else allowDuplicants = duplicants;
                }

                return changed;
            }
            internal ChangesToken InspectClassInList<T>(List<T> list, int index, ref int inspected, CollectionInspectorMeta listMeta = null) where T : class
            {
                var el = list[index];
                var changed = ChangeTrackStart();

                var isPrevious = (listMeta != null && listMeta.previouslyInspectedElement == index)
                                 || (listMeta == null && collectionInspector.previouslyEntered != null && el == collectionInspector.previouslyEntered);

                if (isPrevious)
                    SetBgColor(PreviousInspectedColor);

                if (el is IPEGI_ListInspect pl)
                {
                    var chBefore = GUI.changed;

                    pl.InspectInList(ref inspected, index);

                    if (!chBefore && GUI.changed)
                        pl.SetToDirty_Obj();

                    if (changed || inspected == index)
                        isPrevious = true;

                }
                else
                {

                    if (el.IsNullOrDestroyed_Obj())
                    {
                        var ed = listMeta?[index];
                        if (ed == null)
                            "{0}: NULL {1}".F(index, typeof(T).ToPegiStringType()).PL().Write();
                        else
                        {
                            var elObj = (object)el;
                            if (ed.PEGI_inList<T>(ref elObj))
                            {
                                isPrevious = true;
                                list[index] = elObj as T;
                            }

                        }
                    }
                    else
                    {
                        var uo = el as Object;

                        var pg = el as IPEGI;

                        var need = el as INeedAttention;
                        var warningText = need?.NeedAttention();

                        if (warningText != null)
                            SetBgColor(AttentionColor);

                        var clickHighlightHandled = false;

                        var iind = el as IGotIndex;

                        iind?.IndexForInspector.ToString().PL(20).Write();

                        if (!uo && pg == null && listMeta == null)
                        {
                            var label = el.GetNameForInspector().PL(toolTip: Msg.InspectElement.GetText(), width: RemainingLength(DEFAULT_BUTTON_SIZE * 2 + 10));

                            if (label.ClickLabel())
                            {
                                inspected = index;
                                isPrevious = true;
                            }
                        }
                        else
                        {
                            if (uo)
                            {
                                if (Edit(ref uo))
                                    list[index] = uo as T;

                                Texture tex = uo as Texture;

                                if (tex)
                                {
                                    if (ClickHighlight(uo, tex))
                                        isPrevious = true;

                                    clickHighlightHandled = true;
                                }
                                //else if (Try_NameInspect(uo))
                                //  isPrevious = true;
                            }
                            else if (el.GetNameForInspector().PL(toolTip: "Inspect", width: RemainingLength(DEFAULT_BUTTON_SIZE * 2 + 50)).ClickLabel())
                            {
                                inspected = index;
                                isPrevious = true;
                            }
                        }
                        
                        if ((warningText == null &&
                             Icon.Enter.ClickUnFocus(Msg.InspectElement)) ||
                            (warningText != null && Icon.Warning.ClickUnFocus(warningText)))
                        {
                            inspected = index;
                            isPrevious = true;
                        }

                        if (!clickHighlightHandled && ClickHighlight(uo))
                            isPrevious = true;
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

                return changed;
            }

            internal void TryShowListAddNewOption<T>(List<T> list, ref T added, CollectionInspectorMeta ld = null)
            {

                if (ld != null && !ld[CollectionInspectParams.showAddButton])
                    return;

                var type = typeof(T);

                if (type.IsInterface || (!type.IsUnityObject() && type.IsAbstract))
                    return;

                if (!type.IsNew())
                {
                    collectionInspector.ListAddEmptyClick(list, ld);
                    return;
                }

                if (type.TryGetClassAttribute<DerivedListAttribute>() != null || typeof(IGotClassTag).IsAssignableFrom(type))
                    return;

                string name = null;

                var sd = ld == null ? defaultSearchData : ld.searchData;

                if (ReferenceEquals(sd.FilteredList, list))
                    name = sd.SearchedText;

                if (Icon.Add.ClickUnFocus(Msg.AddNewCollectionElement.GetText() + (name.IsNullOrEmpty() ? "" : " Named {0}".F(name))))
                {
                    if (typeof(T).IsSubclassOf(typeof(Object)))
                        list.Add(default);
                    else
                        added = name.IsNullOrEmpty() ? QcSharp.AddWithUniqueStringIdAndIndex(list) : QcSharp.AddWithUniqueStringIdAndIndex(list, name);

                    SkrollToBottom();
                }

                return;
            }
            internal StateToken ListAddEmptyClick<T>(IList<T> list, CollectionInspectorMeta ld = null)
            {

                if (ld != null && !ld[CollectionInspectParams.showAddButton])
                    return StateToken.False;

                var type = typeof(T);

                if (!type.IsUnityObject() && (type.TryGetClassAttribute<DerivedListAttribute>() != null || typeof(IGotClassTag).IsAssignableFrom(type)))
                    return StateToken.False;

                if (Icon.Add.ClickUnFocus(Msg.AddNewCollectionElement.GetText()))
                {
                    list.Add(default);
                    collectionInspector.SkrollToBottom();
                    return StateToken.True;
                }
                return StateToken.False;
            }
        }

        public static ChangesToken DragAndDrop_Area<T>(out List<T> droppedElements) where T : Object
        {
            droppedElements = new List<T>();

#if UNITY_EDITOR
            foreach (var ret in PegiEditorOnly.DropAreaGUI<T>())
            {
                droppedElements.Add(ret);
            }
#endif
            return droppedElements.Count > 0 ? ChangesToken.True : ChangesToken.False; // ChangesToken.;

        }
    }
    #endregion

}
