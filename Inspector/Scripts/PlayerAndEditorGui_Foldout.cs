using System;
using System.Collections.Generic;
using QuizCanners.Migration;
using QuizCanners.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

// ReSharper disable InconsistentNaming
#pragma warning disable IDE0011 // Add braces

namespace QuizCanners.Inspect
{
    public static partial class pegi
    {
        internal static void FoldInNow() => selectedFold = -1;

        public static StateToken IsFoldout(this TextLabel txt, ref bool state)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Foldout(txt, ref state);
#endif

            CheckLine();

            if (ClickUnFocus((state ? "[Hide] {0}..." : ">{0} [Show]").F(txt).PegiLabel()))
                state = !state;


            PegiEditorOnly.isFoldedOutOrEntered = new StateToken(state);

            return PegiEditorOnly.isFoldedOutOrEntered;

        }

        public static StateToken IsFoldout(this TextLabel txt, ref int selected, int current)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Foldout(txt, ref selected, current);
#endif

            CheckLine();

            PegiEditorOnly.IsFoldedOutOrEntered = (selected == current);

            if (ClickUnFocus((PegiEditorOnly.isFoldedOutOrEntered ? "[Hide] {0}..." : ">{0} [Show]").F(txt.label).PegiLabel()).IgnoreChanges(LatestInteractionEvent.Enter))
            {
                if (PegiEditorOnly.isFoldedOutOrEntered)
                    selected = -1;
                else
                    selected = current;
            }

            PegiEditorOnly.IsFoldedOutOrEntered = selected == current;

            return PegiEditorOnly.isFoldedOutOrEntered;

        }

        public static StateToken IsFoldout(this Icon ico, string text, ref bool state) => ico.GetIcon().IsFoldout(text, ref state);

        public static StateToken IsFoldout(this TextLabel txt)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Foldout(txt);
#endif

            IsFoldout(txt, ref selectedFold, _elementIndex);

            _elementIndex++;

            return PegiEditorOnly.isFoldedOutOrEntered;
        }

        internal static StateToken IsFoldout(this Texture2D tex, string text, ref bool state)
        {

            if (state)
            {
                if (Icon.FoldedOut.ClickUnFocus(text, 30).IgnoreChanges(LatestInteractionEvent.Exit))
                    state = false;
            }
            else
            {
                if (tex.Click(text).IgnoreChanges(LatestInteractionEvent.Enter))
                    state = true;
            }
            return new StateToken(state);
        }

      

    }
}