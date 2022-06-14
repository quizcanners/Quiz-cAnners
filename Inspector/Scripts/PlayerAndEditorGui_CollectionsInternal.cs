using QuizCanners.Migration;
using QuizCanners.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

using Enm = System.Linq.Enumerable;

namespace QuizCanners.Inspect
{
    public static partial class pegi
    {
        internal class CollectionInspector : System.IDisposable
        {
            private const int SCROLL_ARROWS_WIDTH = 190;
            private const int SCROLL_ARROWS_HEIGHT = 20;
            private int DEFAULT_MAX_ELEMENTS_ON_SCREEN => PaintingGameViewUI ? 10 : 20;

            public int Index { get; set; } = -1;
            public IList reordering;
            public TextLabel currentListLabel = new TextLabel();
            public System.Array _editingArrayOrder;
            public readonly CountlessBool selectedEls = new CountlessBool();
            public object previouslyEntered;

            private readonly Dictionary<IEnumerable, int> Indexes = new Dictionary<IEnumerable, int>();
            private bool _searching;
            private List<int> filteredList;
            private int _sectionSizeOptimal;
            private int _count;
            private List<int> _copiedElements = new List<int>();
            private bool cutPaste;
            private readonly CountlessInt SectionOptimal = new CountlessInt();
            private static IList addingNewOptionsInspected;
            private string addingNewNameHolder = "Name";
            private bool exitOptionHandled;
            private static IList listCopyBuffer;
            private int _lastElementToShow;
            private int _sectionStartIndex;
            private SearchData searchData; // IN META
            private bool _scrollDownRequested;
            private bool allowDuplicants; // IN META
            private readonly List<System.IDisposable> _toDispose = new List<System.IDisposable>();

            public void Dispose() => End();
            public void End()
            {
                currentListLabel = new TextLabel();
                foreach (var toDisp in _toDispose)
                    toDisp.Dispose();

                _toDispose.Clear();
            }
            public IEnumerable<T> InspectionIndexes<T>(ICollection<T> collectionReference, CollectionInspectorMeta listMeta = null, iCollectionInspector<T> listElementInspector = null)
            {

                _toDispose.Add(Styles.Background.List.SetDisposible());

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

                if (_count >= _sectionSizeOptimal * 2 || _sectionStartIndex > 0)
                {
                    if (_count > _sectionSizeOptimal)
                    {

                        while ((_sectionStartIndex > 0 && _sectionStartIndex >= _count))
                        {
                            _sectionStartIndex = Mathf.Max(0, _sectionStartIndex - _sectionSizeOptimal);
                        }
                        Nl();
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

                            ".. {0}; ".F(_sectionStartIndex - 1).PegiLabel().Write();

                        }
                        else
                            Icon.UpLast.Write("Is the first section of the list.", SCROLL_ARROWS_WIDTH, SCROLL_ARROWS_HEIGHT);
                        Nl();

                    }
                }

                Nl();

                #endregion

                Styles.InList = true;

                if (!_searching)
                {
                    _lastElementToShow = Mathf.Min(_count, _sectionStartIndex + _sectionSizeOptimal);

                    Index = _sectionStartIndex;

                    var list = collectionReference as IList<T>;

                    if (list != null)
                    {
                        for (; Index < collectionReference.Count; Index++)
                        {

                            var lel = list[Index];

                            SetListElementReadabilityBackground(Index);
                            yield return lel;
                            RestoreBGColor();

                            if (Index >= _lastElementToShow)
                                break;
                        }
                    }
                    else
                    {
                        foreach (var el in Enm.Skip(collectionReference, _sectionStartIndex))
                        {
                            SetListElementReadabilityBackground(Index);
                            yield return el;
                            RestoreBGColor();

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

                            "+ {0}".F(_count - _lastElementToShow).PegiLabel().Write();

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
                            SetListElementReadabilityBackground(sectionIndex);

                            yield return collectionReference.GetElementAt(Index);

                            RestoreBGColor();

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
                                "+ {0}".F(filteredList.Count - _lastElementToShow).PegiLabel().Write();

                        }
                        else if (_sectionStartIndex > 0)
                            Icon.DownLast.Write("Is the last section of the list. ", SCROLL_ARROWS_WIDTH, SCROLL_ARROWS_HEIGHT);

                    }

                }

                Styles.InList = false;

                if (changed)
                    SaveSectionIndex(collectionReference, listMeta);
            }
            public void ListInstantiateNewName<T>()
            {
                Msg.New.GetText().PegiLabel(Msg.NameNewBeforeInstancing_1p.GetText().F(typeof(T).ToPegiStringType()), 30, Styles.ExitLabel).Write();
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

                var hasName = typeof(T).IsSubclassOf(typeof(Object)) || typeof(IGotName).IsAssignableFrom(typeof(T));

                if (hasName)
                    ListInstantiateNewName<T>();
                   
                if (!hasName || addingNewNameHolder.Length > 1)
                {

                    var selectingDerrived = lst == addingNewOptionsInspected;

                    if (selectingDerrived)
                        Line(); Nl();

                    (derrivedTypesExplicit == null ? "Create new {0}".F(typeof(T).ToPegiStringType()) : "Create new {0}".F(typeof(T).ToPegiStringType())).PegiLabel(Styles.ClickableText).Write();

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
                                "Search".PegiLabel(width: 60, style: Styles.FoldedOutLabel).Edit(ref _listTypeSearch).Nl();
                                searchString = _listTypeSearch;
                            }

                            foreach (var t in derrivedTypesExplicit)
                            {
                                string typeName = t.ToPegiStringType();

                                if (searchString == null || typeName.Contains(searchString))
                                {
                                    typeName.PegiLabel().Write();
                                    if (Icon.Create.ClickUnFocus().Nl())
                                    {
                                        added = (T)System.Activator.CreateInstance(t);
                                        QcSharp.AddWithUniqueNameAndIndex(lst, added, addingNewNameHolder);
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

                                    tagTypes.DisplayNames[i].PegiLabel().Write();
                                    if (Icon.Create.ClickUnFocus().Nl())
                                    {
                                        added = (T)System.Activator.CreateInstance(tagTypes.TaggedTypes.TryGet(k[i]));
                                        QcSharp.AddWithUniqueNameAndIndex(lst, added, addingNewNameHolder);
                                        SkrollToBottom();
                                    }
                                }

                            }

                            if (availableOptions == 0)
                                (k.Count == 0 ? "There no types derrived from {0}".F(typeof(T).ToString()) :
                                    "Existing types are restricted to one instance per list").PegiLabel().WriteHint();

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

                var hasName = typeof(T).IsSubclassOf(typeof(Object)) || typeof(IGotName).IsAssignableFrom(typeof(T));

                if (hasName)
                    ListInstantiateNewName<T>();
                else
                    "Create new {0}".F(typeof(T).ToPegiStringType()).PegiLabel().Write();

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

                            types.DisplayNames[i].PegiLabel().Write();
                            if (Icon.Create.ClickUnFocus().Nl())
                            {
                                changed = true;
                                added = (T)System.Activator.CreateInstance(types.TaggedTypes.TryGet(k[i]));
                                QcSharp.AddWithUniqueNameAndIndex(lst, added, addingNewNameHolder);
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

                _sectionSizeOptimal = SectionOptimal[count];

                if (_sectionSizeOptimal != 0)
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
            private int GetNextFiltered<T>(ICollection<T> collectionReference, string[] searchby, iCollectionInspector<T> inspector = null)
            {
                foreach (var reff in Enm.Skip(collectionReference, searchData.UncheckedElement))
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
            private void SetListElementReadabilityBackground(int index)
            {
                switch (index % 4)
                {
                    case 1: SetBgColor(Styles.listReadabilityBlue); break;
                    case 3: SetBgColor(Styles.listReadabilityRed); break;
                }
            }
            internal TextLabel GetCurrentListLabel<T>(CollectionInspectorMeta ld = null) =>
                ld != null
                    ? ld.Label.PegiLabel() :
                        (currentListLabel.IsInitialized ? currentListLabel : typeof(T).ToPegiStringType().PegiLabel());

            internal CollectionInspector Write_Search_DictionaryLabel<K, V>(CollectionInspectorMeta collectionMeta, Dictionary<K, V> dic) =>
                Write_Search_DictionaryLabel<K, V>(collectionMeta.Label.PegiLabel(), ref collectionMeta.inspectedElement_Internal, dic, collectionMeta.searchData);
            internal CollectionInspector Write_Search_DictionaryLabel<K, V>(TextLabel label, ref int inspected, Dictionary<K, V> dic, SearchData sd = null)
            {
                currentListLabel = label;

                bool inspecting = inspected != -1;

                if (sd == null)
                    sd = defaultSearchData;

                if (!inspecting)
                    sd.ToggleSearch(dic, label);
                else
                {
                    exitOptionHandled = true;
                    if (Icon.List.ClickUnFocus("{0} [1]".F(Msg.ReturnToCollection.GetText(), dic.Count)))
                        inspected = -1;
                }

                if (dic != null && inspected >= 0 && dic.Count > inspected)
                {
                    var el = dic.GetElementAt(inspected);

                    var keyName = el.Key.GetNameForInspector();
                    var valName = el.Value.GetNameForInspector();

                    bool isSubset = false;
                    string nameToShow = "";

                    if (valName.Contains(keyName)) 
                    {
                        isSubset = true;
                        nameToShow = valName;
                    } else if (keyName.Contains(valName)) 
                    {
                        isSubset = true;
                        nameToShow = keyName;
                    }

                    label = (isSubset ? "{0}->{1}".F(label, nameToShow) : "{0}->{1}:{2}".F(label, keyName, valName)).PegiLabel();
                }
                else label = (dic == null || dic.Count < 6) ? label : label.AddCount(dic, true);

                label.width = RemainingLength(defaultButtonSize * 2 + 10);
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
                    defaultSearchData.ToggleSearch(lst, label);
                else
                {
                    exitOptionHandled = true;
                    if (Icon.List.ClickUnFocus("{0} [1]".F(Msg.ReturnToCollection.GetText(), lst.Count)))
                        inspected = -1;
                }

                if (lst != null && inspected >= 0 && lst.Count > inspected)
                    label = "{0}->{1}".F(label, lst.GetElementAt(inspected).GetNameForInspector()).PegiLabel();
                else label = (lst == null || lst.Count < 6) ? label : label.AddCount(lst, true);

                label.width = RemainingLength(defaultButtonSize * 2 + 10);
                label.style = Styles.ListLabel;

                if (label.ClickLabel() && inspected != -1)
                    inspected = -1;

                return this;
            }
            internal CollectionInspector Write_Search_ListLabel<T>(CollectionInspectorMeta ld, ICollection<T> lst)
            {
                if (ld == null) 
                {
                    "Meta is Null. Could be due to ScriptableObject serializing private fields.".PegiLabel().WriteWarning();
                    return this;
                }

                currentListLabel = ld.Label.PegiLabel();

                if (!ld.IsAnyEntered && ld[CollectionInspectParams.showSearchButton])
                    ld.searchData.ToggleSearch(lst, ld.Label);

                if (lst != null && ld.InspectedElement >= 0 && lst.Count > ld.InspectedElement)
                {
                    var el = lst.GetElementAt(ld.InspectedElement);
                    string nameToShow = el.GetNameForInspector();
                    currentListLabel = "{0}->{1}".F(ld.Label, nameToShow).PegiLabel();
                }
                else currentListLabel = ((lst == null || lst.Count < 6) ? ld.Label : ld.Label.AddCount(lst, true)).PegiLabel();


                if (ld.IsAnyEntered && lst != null)
                {
                    exitOptionHandled = true;
                    if (Icon.List.ClickUnFocus("{0} {1} [2]".F(Msg.ReturnToCollection.GetText(), currentListLabel, lst.Count)))
                        ld.IsAnyEntered = false;
                }

                currentListLabel.width = RemainingLength(defaultButtonSize * 2 + 10);
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
                    "Dictionary of {0} is null".F(typeof(T).ToPegiStringType()).PegiLabel().Write();

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
                    "List of {0} is null".F(typeof(T).ToPegiStringType()).PegiLabel().Write();

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
                        selectedEls[i] = val;
                }
                else for (var i = 0; i < list.Count; i++)
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
                        el.GetNameForInspector().PegiLabel().Write();
                    else
                        "{0} {1}".F(Icon.Empty.GetText(), typeof(T).ToPegiStringType()).PegiLabel().Write();

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
                            el.GetNameForInspector().PegiLabel().Write();
                        else
                            "{0} {1}".F(Icon.Empty.GetText(), typeof(T).ToPegiStringType()).PegiLabel().Write();

                        Nl();
                    }

                }

                #endregion

                #region Select

                var selectedCount = 0;

                if (listMeta == null)
                {
                    for (var i = 0; i < list.Count; i++)
                        if (selectedEls[i])
                            selectedCount++;
                }
                else
                    for (var i = 0; i < list.Count; i++)
                        if (listMeta.GetIsSelected(i))
                            selectedCount++;

                if (selectedCount > 0 && Icon.DeSelectAll.Click().IgnoreChanges(LatestInteractionEvent.Click))
                    SetSelected(listMeta, list, false);

                if (selectedCount == 0 && list.Count>0 && Icon.SelectAll.Click().IgnoreChanges(LatestInteractionEvent.Click))
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

                            var igc1 = emp1 as IGotIndex;
                            var igc2 = emp2 as IGotIndex;

                            if (igc1 == null || igc2 == null)
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
                        "DUPLICATE:".PegiLabel("Selected elements are from this list", 60).Write();

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

                                    var istd = el as ICfgCustom;

                                    if (istd != null)
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
                        _copiedElements = listMeta != null ? listMeta.GetSelectedElements() : selectedEls.GetItAll();
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
                                    if (selectedEls[i])
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
                    "Allow Duplicants".PegiLabel("Will add elements to the list even if they are already there", 120).Toggle(ref duplicants).IgnoreChanges(LatestInteractionEvent.Click);

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

                var pl = el as IPEGI_ListInspect;
                var isPrevious = (listMeta != null && listMeta.previouslyInspectedElement == index)
                                 || (listMeta == null && collectionInspector.previouslyEntered != null && el == collectionInspector.previouslyEntered);

                if (isPrevious)
                    SetBgColor(PreviousInspectedColor);

                if (pl != null)
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
                            "{0}: NULL {1}".F(index, typeof(T).ToPegiStringType()).PegiLabel().Write();
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

                        iind?.IndexForInspector.ToString().PegiLabel(20).Write();

                        var named = el as IGotName;
                        if (named != null)
                        {
                            var so = uo as ScriptableObject;
                            var n = named.NameForInspector;

                            if (so)
                            {
                                if (Edit_Delayed(ref n))
                                {
                                    QcUnity.RenameAsset(so, n);
                                    named.NameForInspector = n;
                                    isPrevious = true;
                                }
                            }
                            else if (Edit(ref n))
                            {
                                named.NameForInspector = n;
                                isPrevious = true;
                            }
                        }
                        else
                        {
                            if (!uo && pg == null && listMeta == null)
                            {
                                var label = el.GetNameForInspector().PegiLabel(toolTip: Msg.InspectElement.GetText(), width: RemainingLength(defaultButtonSize * 2 + 10));

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
                                    else if (Try_NameInspect(uo))
                                        isPrevious = true;
                                }
                                else if (el.GetNameForInspector().PegiLabel(toolTip: "Inspect", width: RemainingLength(defaultButtonSize * 2 + 50)).ClickLabel())
                                {
                                    inspected = index;
                                    isPrevious = true;
                                }
                            }
                        }

                        if ((warningText == null &&
                             Icon.Enter.ClickUnFocus(Msg.InspectElement)) ||
                            (warningText != null && Icon.Warning.ClickUnFocus(warningText)))
                        {
                            inspected = index;
                            isPrevious = true;
                        }

                        if (!clickHighlightHandled && pegi.ClickHighlight(uo))
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

            internal StateToken TryShowListAddNewOption<T>(List<T> list, ref T added, CollectionInspectorMeta ld = null)
            {

                if (ld != null && !ld[CollectionInspectParams.showAddButton])
                    return StateToken.True;

                var type = typeof(T);

                if (type.IsInterface || (!type.IsUnityObject() && type.IsAbstract))
                    return StateToken.False;

                if (!type.IsNew())
                {
                    collectionInspector.ListAddEmptyClick(list, ld);
                    return StateToken.True;
                }

                if (type.TryGetClassAttribute<DerivedListAttribute>() != null || typeof(IGotClassTag).IsAssignableFrom(type))
                    return StateToken.False;

                string name = null;

                var sd = ld == null ? defaultSearchData : ld.searchData;

                if (ReferenceEquals(sd.FilteredList, list))
                    name = sd.SearchedText;

                if (Icon.Add.ClickUnFocus(Msg.AddNewCollectionElement.GetText() + (name.IsNullOrEmpty() ? "" : " Named {0}".F(name))))
                {
                    if (typeof(T).IsSubclassOf(typeof(Object)))
                        list.Add(default);
                    else
                        added = name.IsNullOrEmpty() ? QcSharp.AddWithUniqueNameAndIndex(list) : QcSharp.AddWithUniqueNameAndIndex(list, name);

                    SkrollToBottom();
                }

                return StateToken.True;
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
    }
}
