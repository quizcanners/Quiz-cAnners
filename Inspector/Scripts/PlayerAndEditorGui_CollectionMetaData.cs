using QuizCanners.Migration;
using QuizCanners.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Inspect
{

#pragma warning disable IDE1006 // Naming Styles
    public static partial class pegi
#pragma warning restore IDE1006 // Naming Styles
    {
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
            [NonSerialized] internal UnNullable<ElementData> elementDatas = new();
            [NonSerialized] internal bool inspectListMeta = false;
            [NonSerialized] private CollectionInspectParams _config;

            private readonly PlayerPrefValue.Int _playerPref = null;

            internal int InspectedElement 
            {
                get => inspectedElement_Internal;
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
                    elementDatas.TryGet(i, out ElementData dta);
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
                    if (e.selected) sel.Add(elementDatas.currentEnumerationIndex);
                return sel;
            }

            internal bool GetIsSelected(int ind)
            {
                var el = elementDatas.GetIfExists(ind);
                return el != null && el.selected;
            }

            internal void SetIsSelected(int ind, bool value)
            {
                var el = value ? elementDatas[ind] : elementDatas.GetIfExists(ind);
                if (el != null)
                    el.selected = value;
            }

            #region Inspector

            [NonSerialized] internal readonly SearchData searchData = new();
            [NonSerialized] private readonly EnterExitContext _context = new();

            void IPEGI.Inspect()
            {
                using (_context.StartContext())
                {
                    Nl();
                    if (!_context.IsAnyEntered)
                    {
                        "List Label".PegiLabel(70).Edit(ref Label).Nl();
                        "Config".PegiLabel().Edit_EnumFlags(ref _config).Nl();
                    }

                    if ("Elements".PegiLabel().IsEntered().Nl())
                        elementDatas.Inspect();
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
                    inspectedElement_Internal = _playerPref.GetValue();
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
    }
}
