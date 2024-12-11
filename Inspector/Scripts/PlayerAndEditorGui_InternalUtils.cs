using QuizCanners.Utils;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static QuizCanners.Inspect.pegi;

namespace QuizCanners.Inspect
{
    public static class LazyLocalization
    {

        public const int eng = (int)SystemLanguage.English;

        public static LazyTranslation Get(this Msg msg)
        {

            int index = (int)msg;

            if (coreTranslations.Initialized(index))
                return coreTranslations.GetWhenInited(index);

            switch (msg)
            {
                case Msg.New:
                    msg.Translate("New");
                    break;

                case Msg.NameNewBeforeInstancing_1p:
                    msg.Translate("Name for the new {0} you'll instantiate");
                    break;
                case Msg.Texture2D: msg.Translate("Texture"); break;
                case Msg.RenderTexture: msg.Translate("Render Texture"); break;

                case Msg.EditDelayed_HitEnter:
                    msg.Translate("Press Enter to Complete Edit");
                    break;

                case Msg.InspectElement:
                    msg.Translate("Inspect element");
                    break;

                case Msg.HighlightElement:
                    msg.Translate("Highlight this element in the project");
                    break;

                case Msg.RemoveFromCollection:
                    msg.Translate("Remove element from this collection");
                    break;

                case Msg.AddNewCollectionElement:
                    msg.Translate("Add New element to a list");
                    break;

                case Msg.AddEmptyCollectionElement:
                    msg.Translate("Add NULL/default collection element");
                    break;

                case Msg.ReturnToCollection:
                    msg.Translate("Return to collection");
                    break;

                case Msg.MakeElementNull:
                    msg.Translate("Null this element.");
                    break;
                case Msg.ToolTip:
                    msg.Translate("ToolTip", "What is this?");
                    break;
                case Msg.ClickYesToConfirm:
                    msg.Translate("Click yes to confirm operation");
                    break;
                case Msg.No:
                    msg.Translate("NO");
                    break;
                case Msg.Yes:
                    msg.Translate("YES");
                    break;

                case Msg.AreYouSure:
                    msg.Translate("Are you sure?");
                    break;

                case Msg.ClickToInspect:
                    msg.Translate("Click to Inspect");
                    break;

                case Msg.FinishMovingCollectionElements:
                    msg.Translate("Finish moving");
                    break;
                case Msg.MoveCollectionElements:
                    msg.Translate("Organize collection elements");
                    break;

                case Msg.TryCopyReferences:
                    msg.Translate("Try Copy References");
                    break;
                case Msg.TryDuplicateSelected:
                    msg.Translate("Try duplicate selected items");
                    break;

                case Msg.Init:
                    msg.Translate(
                    "Init"); break;
                case Msg.List:
                    msg.Translate(
                    "List"); break;
                case Msg.Collection:
                    msg.Translate(
                    "Collection"); break;
                case Msg.Array:
                    msg.Translate(
                        "Array");
                    break;
                case Msg.Dictionary:
                    msg.Translate(
                        "Dictionary");
                    break;

            }

            return coreTranslations.GetWhenInited(index);
        }

        public class LazyTranslation
        {
            public string text;
            public string details;

            public LazyTranslation(string mText)
            {
                text = mText;
            }

            public LazyTranslation(string mText, string mDetails)
            {
                text = mText;
                details = mDetails;
            }

            public override string ToString() => text;
        }

        #region Inspector

        public static pegi.ChangesToken DocumentationClick(this LazyTranslation trnsl) => pegi.FullWindow.DocumentationClickOpen(trnsl.details, toolTip: trnsl.text);

        public static pegi.ChangesToken WarningDocumentation(this LazyTranslation trnsl) => pegi.FullWindow.DocumentationWarningClickOpen(trnsl.details, trnsl.text);

        #endregion

        #region Translation Class
        public class TranslationsEnum
        {
            public Dictionary<int, Dictionary<int, LazyTranslation>> pntrTexts = new();
            public HashSet<int> textInitialized = new();

            public bool Initialized(int index) => textInitialized.Contains(index);

            public Dictionary<int, LazyTranslation> this[int ind] => pntrTexts.GetOrCreate(ind);

            public LazyTranslation GetWhenInited(int ind, int lang = 0)
            {
                textInitialized.Add(ind);

                var loc = pntrTexts[ind];

                if (loc.TryGetValue(lang, out var val) || lang == eng)
                    return val;

                loc.TryGetValue(eng, out val);

                return val;
            }
        }

        public static Dictionary<int, LazyTranslation> From(this Dictionary<int, LazyTranslation> sntnc, int lang, string text)
        {
            sntnc[lang] = new LazyTranslation(text);
            return sntnc;
        }

        public static Dictionary<int, LazyTranslation> From(this Dictionary<int, LazyTranslation> sntnc, int lang, string text, string details)
        {
            sntnc[lang] = new LazyTranslation(text, details);
            return sntnc;
        }
        #endregion

        #region Implementation of Extensions

        private static readonly TranslationsEnum coreTranslations = new();

        private static Dictionary<int, LazyTranslation> Translate(this Msg smg, string english)
        {
            var org = coreTranslations[(int)smg];
            org[eng] = new LazyTranslation(english);
            return org;
        }

        private static Dictionary<int, LazyTranslation> Translate(this Msg smg, string english, string englishDetails)
        {
            var org = coreTranslations[(int)smg];
            org[eng] = new LazyTranslation(english, englishDetails);
            return org;
        }

        public static string GetText(this Msg s)
        {
            var lt = s.Get();
            return lt != null ? lt.ToString() : s.ToString();
        }

        public static string GetDescription(this Msg msg)
        {
            var lt = msg.Get();
            return lt != null ? lt.details : msg.ToString();
        }


        public static string F(this Msg msg, Msg other) =>
            msg.GetText() + " " + other.GetText();
        public static void DocumentationClick(this Msg msg) => msg.Get().DocumentationClick();
        public static void Nl(this Msg m) { m.GetText().PL().Nl(); }
        public static void Nl(this Msg m, int width) { m.GetText().PL(width: width).Nl(); }
        public static void Nl(this Msg m, string tip, int width) { m.GetText().PL(tip, width).Nl(); }
        public static void Write(this Msg m) { m.GetText().PL().Write(); }
        public static void Write(this Msg m, int width) { m.GetText().PL(width).Write(); }
        public static void Write(this Msg m, string tip, int width) { m.GetText().PL(tip, width).Write(); }
        public static pegi.ChangesToken Click(this Icon icon, Msg text) => icon.ClickUnFocus(text.GetText());
        //  public static bool Click(this icon icon, Msg text, ref bool changed) => icon.ClickUnFocus(text.GetText()).changes(ref changed);
        public static pegi.ChangesToken ClickUnFocus(this Icon icon, Msg text, int size = pegi.DEFAULT_BUTTON_SIZE) => pegi.Click(icon.GetIcon(), text.GetText(), size).UnfocusOnChange();
        public static pegi.ChangesToken ClickUnFocus(this Icon icon, Msg text, int width, int height) => pegi.Click(icon.GetIcon(), text.GetText(), width, height).UnfocusOnChange();

        #endregion

    }


    public static partial class pegi
    {
        public enum Msg
        {
            Texture2D, RenderTexture, EditDelayed_HitEnter, InspectElement,
            HighlightElement, RemoveFromCollection, AddNewCollectionElement, AddEmptyCollectionElement,
            ReturnToCollection, MakeElementNull, NameNewBeforeInstancing_1p, New,
            ToolTip, ClickYesToConfirm, Yes, No, Exit, AreYouSure, ClickToInspect,
            FinishMovingCollectionElements, MoveCollectionElements, TryDuplicateSelected, TryCopyReferences,
            Init, List, Collection, Array, Dictionary
        }

  
        private static class Utils 
        {
            public static GUILayoutOption GuiMaxWidthOptionFrom(GUIContent cnt, pegi.Styles.PegiGuiStyle style) => GUILayout.MaxWidth(Mathf.Min(PLAYTIME_GUI_WIDTH, ApproximateLength(cnt.text, style.Current.fontSize)));
            public static GUILayoutOption GuiMaxWidthOptionFrom(string txt, GUIStyle style) => GUILayout.MaxWidth(Mathf.Min(PLAYTIME_GUI_WIDTH, ApproximateLength(txt, style.fontSize)));

            public static int GuiMaxWidthFrom(string text) => Mathf.Min(PLAYTIME_GUI_WIDTH, ApproximateLength(text));

            public static GUILayoutOption GuiMaxWidthOption => GUILayout.MaxWidth(PLAYTIME_GUI_WIDTH);
            public static GUILayoutOption GuiMaxWidthOptionFrom(string text) =>
                GUILayout.MaxWidth(GuiMaxWidthFrom(text));

            internal static bool IsMonoType<T>(System.Collections.Generic.IList<T> list, int i)
            {
                if (!(typeof(MonoBehaviour)).IsAssignableFrom(typeof(T))) return false;

                GameObject mb = null;
                if (Edit(ref mb))
                {
#pragma warning disable UNT0014 // Invalid type for call to GetComponent
                    list[i] = mb.GetComponent<T>();
#pragma warning restore UNT0014 // Invalid type for call to GetComponent
                    if (list[i] == null) GameView.ShowNotification(typeof(T) + " Component not found");
                }
                return true;
            }

            public static int ApproximateLength(string label, int fontSize = -1)
            {
                if (label == null || label.Length == 0)
                    return 1;

                if (fontSize <= 1)
                    fontSize = LetterSizeInPixels;

                int length = fontSize * label.Length;

                if (PaintingGameViewUI && length > PLAYTIME_GUI_WIDTH)
                    return PLAYTIME_GUI_WIDTH;

                int count = 0;
                for (int i = 0; i < label.Length; i++)
                {
                    if (char.IsUpper(label[i])) count++;
                }

                length += (int)(count * fontSize * 0.5f);

                return Mathf.Max(30, length);
            }
        }

        public static bool SearchMatch_ObjectList(IEnumerable list, string searchText) => list.Cast<object>().Any(e => Try_SearchMatch_Obj(e, searchText));

        internal static IEnumerable<Type> GetBaseClassesAndInterfaces(Type type, bool includeSelf = false)
        {
            List<Type> allTypes = new();

            if (includeSelf) allTypes.Add(type);

            allTypes.AddRange(
                (type.BaseType == typeof(object)) ?
                    type.GetInterfaces() :
                     Enumerable
                    .Repeat(type.BaseType, 1)
                    .Concat(type.GetInterfaces())
                    .Concat(GetBaseClassesAndInterfaces(type.BaseType))
                    .Distinct()

                    );


            return allTypes;
        }


        public static IDisposable StartDisabledGroup(bool disabled)
        {
#if UNITY_EDITOR
            EditorGUI.BeginDisabledGroup(disabled);
            return QcSharp.DisposableAction(() => EditorGUI.EndDisabledGroup());
#else
            return QcSharp.DisposableAction(() => { });
#endif
        }

        public class CopyPaste
        {
            public class Buffer
            {
                public string CopyPasteJson;
                public string CopyPasteJsonSourceName;
            }

            private static readonly Dictionary<System.Type, Buffer> _copyPasteBuffs = new();

            private static Buffer GetOrCreate(System.Type type)
            {
                if (_copyPasteBuffs.TryGetValue(type, out Buffer buff) == false)
                {
                    buff = new Buffer();
                    _copyPasteBuffs[type] = buff;
                }

                return buff;
            }

            public static ChangesToken InspectOptionsFor<T>(ref T el)
            {
                var type = typeof(T);

                var changed = ChangeTrackStart();

                if (type.IsSerializable)
                {
                    if (_copyPasteBuffs.TryGetValue(type, out var buffer))
                    {
                        if (!buffer.CopyPasteJson.IsNullOrEmpty() && Icon.Paste.Click("Paste " + buffer.CopyPasteJsonSourceName))
                            JsonUtility.FromJsonOverwrite(buffer.CopyPasteJson, el);
                    }

                    if (Icon.Copy.Click().IgnoreChanges(LatestInteractionEvent.Click))
                    {
                        buffer ??= GetOrCreate(type);
                        buffer.CopyPasteJson = JsonUtility.ToJson(el);
                        buffer.CopyPasteJsonSourceName = el.GetNameForInspector();
                    }
                }
                return changed;
            }

            public static bool InspectOptions<T>(CollectionInspectorMeta meta = null)
            {
                if (meta != null && meta[CollectionInspectParams.showCopyPasteOptions] == false)
                    return false;

                var type = typeof(T);

                if (_copyPasteBuffs.TryGetValue(type, out Buffer buff))
                {
                    Nl();

                    "Copy Paste: {0}".F(buff.CopyPasteJsonSourceName).PL().Write();
                    if (Icon.Clear.Click())
                        _copyPasteBuffs.Remove(type);

                    Nl();
                }

                return false;
            }
        }
    }
}