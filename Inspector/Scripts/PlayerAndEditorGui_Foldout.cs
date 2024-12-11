using QuizCanners.Utils;
using System;
using UnityEngine;

namespace QuizCanners.Inspect
{
    public static partial class pegi
    {
        public static StateToken IsFoldout(this TextLabel txt, ref bool state)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Foldout(txt, ref state);
#endif

            CheckLine();

            if (ClickUnFocus((state ? "[Hide] {0}..." : ">{0} [Show]").F(txt).PL()))
                state = !state;


            FoldoutManager.isFoldedOutOrEntered = new StateToken(state);

            return FoldoutManager.isFoldedOutOrEntered;

        }

        public static StateToken IsFoldout(this TextLabel txt, ref int selected, int current)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Foldout(txt, ref selected, current);
#endif

            CheckLine();

            IsFoldedOutOrEntered = (selected == current);

            if (ClickUnFocus((FoldoutManager.isFoldedOutOrEntered ? "[Hide] {0}..." : ">{0} [Show]").F(txt.label).PL()).IgnoreChanges(LatestInteractionEvent.Enter))
            {
                if (FoldoutManager.isFoldedOutOrEntered)
                    selected = -1;
                else
                    selected = current;
            }

            IsFoldedOutOrEntered = selected == current;

            return FoldoutManager.isFoldedOutOrEntered;

        }

        public static StateToken IsFoldout(this Icon ico, string text)
        {
            using (FoldoutManager.StartFoldoutDisposable(out var isFoldedOut))
            {
                if (isFoldedOut)
                {
                    if (Icon.FoldedOut.ClickUnFocus(text, 30).IgnoreChanges(LatestInteractionEvent.Exit))
                        FoldoutManager.FoldInNow();
                }
                else
                {
                    if (ico.Click(text).IgnoreChanges(LatestInteractionEvent.Enter))
                        FoldoutManager.FoldOutNow();
                }
            }

            return FoldoutManager.isFoldedOutOrEntered;
        }

        public static StateToken IsFoldout(this Icon ico, string text, ref bool state) => ico.GetIcon().IsFoldout(text, ref state);

        public static StateToken IsFoldout(this TextLabel txt)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Foldout(txt);
#endif

            using (FoldoutManager.StartFoldoutDisposable(out _))
            {
                IsFoldout(txt, ref FoldoutManager.selectedFold, _elementIndex);
            }

            return FoldoutManager.isFoldedOutOrEntered;
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
                if (pegi.Click(tex, text).IgnoreChanges(LatestInteractionEvent.Enter))
                    state = true;
            }
            return new StateToken(state);
        }

        internal static bool IsFoldedOutOrEntered
        {
            get => FoldoutManager.isFoldedOutOrEntered;
            set => FoldoutManager.isFoldedOutOrEntered = new StateToken(value);
        }

        internal static int _elementIndex;

        internal static class FoldoutManager 
        {
            internal static StateToken isFoldedOutOrEntered;

           
            internal static int selectedFold = -1;

            internal static void FoldInNow() => selectedFold = -1;

            internal static void FoldOutNow() => selectedFold = _elementIndex;

            internal static bool IsNextFoldedOut => selectedFold == _elementIndex - 1;


            internal static IDisposable StartFoldoutDisposable(out bool isFoldedOut) 
            {
                _elementIndex++;
                IsFoldedOutOrEntered = selectedFold == _elementIndex;
                isFoldedOut = IsFoldedOutOrEntered;

                return QcSharp.DisposableAction(() => 
                {
                    IsFoldedOutOrEntered = (selectedFold == _elementIndex);
                });

            }
        }
    }
}