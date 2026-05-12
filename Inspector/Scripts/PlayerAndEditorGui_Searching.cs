using QuizCanners.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Inspect
{
    public static partial class pegi
    {
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
                }
                else
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
                                }
                                else
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

                                }
                                else if (el is ISearchable)
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

            public void ToggleSearch(IEnumerable collection, TextLabel label, bool showSearchByWarning = false)
            {
                ToggleSearch(collection, label.label, showSearchByWarning: showSearchByWarning);
            }

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
                    FilteredList = null;
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
                    NL();
                    Icon.Search.Draw();
                    NameNextForFocus(SEARCH_FIELD_FOCUS_NAME);

                    if (Edit(ref SearchedText) | Icon.Refresh.Click("Search again", 20).NL())
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

            public void Refresh() => OnCountChange();
        }
    }
}