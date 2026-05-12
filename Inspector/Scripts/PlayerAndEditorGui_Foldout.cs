using NUnit.Framework.Constraints;
using QuizCanners.Utils;
using System;
using UnityEngine;

namespace QuizCanners.Inspect
{
    public static partial class pegi
    {
        internal static class Focus

        {
            internal static int _elementIndex;
            internal static int editedElementIndex = -1;
            internal static string _latestActualFocused;
            internal static string _previouslyFocused = "";
            private static readonly Gate.String currentFocused = new();
            private static readonly Gate.Frame _framesToClearPReviousFocus = new();

            public enum EditState { Unfocused, Started, IsFocused, PressedEnter, Ended }

            internal static void OnContextChange()
            {
                editedElementIndex = 0;
                _latestActualFocused = "";
                _previouslyFocused = "";
                currentFocused.ValueIsDefined = false;
                FoldoutManager.OnContextChange();
            }


            static string _editingControl;
            static bool _wasKeyboardVisible;

            internal static EditState IsCurrentlyFocusedElement(string context)
            {
                _elementIndex++;

                var controlName = context + _elementIndex;
                GUI.SetNextControlName(controlName);

                if (Event.current.type != EventType.Repaint)
                    _latestActualFocused = GUI.GetNameOfFocusedControl();

                bool isFocused = _latestActualFocused == controlName;

#if UNITY_IOS || UNITY_ANDROID
                bool keyboardClosed =
                    _editingControl == controlName &&
                    _wasKeyboardVisible &&
                    !TouchScreenKeyboard.visible;

                _wasKeyboardVisible = TouchScreenKeyboard.visible;

                if (keyboardClosed)
                {
                    GUI.FocusControl(null);
                    currentFocused.ValueIsDefined = false;
                    _editingControl = null;
                    return EditState.Ended;
                }
#endif

                if (!isFocused)
                {
                    if (currentFocused.CurrentValue == controlName ||
                        _previouslyFocused == controlName)
                    {
                        currentFocused.ValueIsDefined = false;
                        _editingControl = null;
                        return EditState.Ended;
                    }

                    return EditState.Unfocused;
                }

                if (currentFocused.IsDirty(controlName))
                {
                    _previouslyFocused = currentFocused.CurrentValue;
                    currentFocused.TryChange(controlName);
                    _editingControl = controlName;

#if UNITY_IOS || UNITY_ANDROID
                    _wasKeyboardVisible = TouchScreenKeyboard.visible;
#endif

                    return EditState.Started;
                }

                if (Event.current.type == EventType.KeyDown &&
                    Event.current.keyCode == KeyCode.Return)
                {
                    Event.current.Use();
                    GUI.FocusControl(null);
                    currentFocused.ValueIsDefined = false;
                    _editingControl = null;
                    return EditState.PressedEnter;
                }

                return EditState.IsFocused;
            }
        }




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

        public static StateToken IsFoldout(this Icon ico, string text, ref bool state) => ico.GetIcon().texture.IsFoldout(text, ref state);

        public static StateToken IsFoldout(this TextLabel txt)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Foldout(txt);
#endif

            using (FoldoutManager.StartFoldoutDisposable(out _))
            {
                IsFoldout(txt, ref FoldoutManager.selectedFold, Focus._elementIndex);
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



        //  private static int editedFloatIndex = -1;

        internal static class FoldoutManager
        {

            internal static StateToken isFoldedOutOrEntered;


            internal static int selectedFold = -1;

            internal static void FoldInNow() => selectedFold = -1;

            internal static void FoldOutNow() => selectedFold = Focus._elementIndex;

            internal static bool IsNextFoldedOut => selectedFold == Focus._elementIndex - 1;


            internal static void OnContextChange()
            {
                selectedFold = -1;
            }
        

            internal static IDisposable StartFoldoutDisposable(out bool isFoldedOut)
            {
                Focus._elementIndex++;
                IsFoldedOutOrEntered = selectedFold == Focus._elementIndex;
                isFoldedOut = IsFoldedOutOrEntered;

                return QcSharp.DisposableAction(() =>
                {
                    IsFoldedOutOrEntered = (selectedFold == Focus._elementIndex);
                });

            }


        }
    }
}