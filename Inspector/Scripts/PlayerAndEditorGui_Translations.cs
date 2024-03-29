﻿using QuizCanners.Utils;
using UnityEngine;

namespace QuizCanners.Inspect {

    public enum Msg  
    {
        Texture2D, RenderTexture,  EditDelayed_HitEnter, InspectElement, 
        HighlightElement, RemoveFromCollection, AddNewCollectionElement, AddEmptyCollectionElement,
        ReturnToCollection, MakeElementNull, NameNewBeforeInstancing_1p, New,
        ToolTip, ClickYesToConfirm, Yes, No, Exit, AreYouSure, ClickToInspect,
        FinishMovingCollectionElements, MoveCollectionElements, TryDuplicateSelected, TryCopyReferences,
        Init, List, Collection, Array, Dictionary
    }

    public static class LazyLocalization {

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

                case Msg.Init: msg.Translate(
                    "Init"); break;
                case Msg.List: msg.Translate(
                    "List"); break;
                case Msg.Collection: msg.Translate(
                    "Collection"); break;
                case Msg.Array: msg.Translate(
                        "Array");
                    break;
                case Msg.Dictionary: msg.Translate(
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
            public UnNullable<Countless<LazyTranslation>> pntrTexts = new UnNullable<Countless<LazyTranslation>>();
            public CountlessBool textInitialized = new CountlessBool();

            public bool Initialized(int index) => textInitialized[index];

            public Countless<LazyTranslation> this[int ind] => pntrTexts[ind];

            public LazyTranslation GetWhenInited(int ind, int lang = 0)
            {

                textInitialized[ind] = true;

                var val = pntrTexts[ind][lang];

                if (val != null)
                    return val;

                val = pntrTexts[ind][eng];

                return val;
            }
        }

        public static Countless<LazyTranslation> From(this Countless<LazyTranslation> sntnc, int lang, string text)
        {
            sntnc[lang] = new LazyTranslation(text);
            return sntnc;
        }

        public static Countless<LazyTranslation> From(this Countless<LazyTranslation> sntnc, int lang, string text, string details)
        {
            sntnc[lang] = new LazyTranslation(text, details);
            return sntnc;
        }
        #endregion
        
        #region Implementation of Extensions

        private static readonly TranslationsEnum coreTranslations = new TranslationsEnum();

        private static Countless<LazyTranslation> Translate(this Msg smg, string english)
        {
            var org = coreTranslations[(int)smg];
            org[eng] = new LazyTranslation(english);
            return org;
        }

        private static Countless<LazyTranslation> Translate(this Msg smg, string english, string englishDetails)
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
        public static void Nl(this Msg m) { m.GetText().PegiLabel().Nl(); }
        public static void Nl(this Msg m, int width) { m.GetText().PegiLabel(width: width).Nl(); }
        public static void Nl(this Msg m, string tip, int width) { m.GetText().PegiLabel(tip, width).Nl(); }
        public static void Write(this Msg m) { m.GetText().PegiLabel().Write(); }
        public static void Write(this Msg m, int width) { m.GetText().PegiLabel(width).Write(); }
        public static void Write(this Msg m, string tip, int width) { m.GetText().PegiLabel(tip, width).Write(); }
        public static pegi.ChangesToken Click(this Icon icon, Msg text) => icon.ClickUnFocus(text.GetText());
      //  public static bool Click(this icon icon, Msg text, ref bool changed) => icon.ClickUnFocus(text.GetText()).changes(ref changed);
        public static pegi.ChangesToken ClickUnFocus(this Icon icon, Msg text, int size = pegi.defaultButtonSize) => pegi.Click(icon.GetIcon(), text.GetText(), size).UnfocusOnChange();
        public static pegi.ChangesToken ClickUnFocus(this Icon icon, Msg text, int width, int height) => pegi.Click(icon.GetIcon(), text.GetText(), width, height).UnfocusOnChange();

        #endregion

    }
}
