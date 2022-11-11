﻿using QuizCanners.Utils;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable IDE0011 // Add braces
#pragma warning disable IDE0008 // Use explicit type

namespace QuizCanners.Inspect
{
    public static partial class pegi
    {
        public const int defaultButtonSize = 26;

        public static class EditorViewPegi
        {
            public static void RefocusIfLocked(Object current, Object target)
            {
#if UNITY_EDITOR
                var tracker = UnityEditor.ActiveEditorTracker.sharedTracker;
                if (current != target && target && tracker.isLocked)
                {
                    tracker.isLocked = false;
                    using (QcSharp.DisposableAction(onDispose: () => tracker.isLocked = true))
                    {
                        QcUnity.FocusOn(target);
                    }
                }
#endif
            }

            public static void Lock_UnlockClick(Object obj)
            {
#if UNITY_EDITOR
                if (!PaintingGameViewUI)
                {
                    var tracker = UnityEditor.ActiveEditorTracker.sharedTracker;
                    if (!tracker.isLocked)
                        Icon.Unlock.ClickUnFocus("Lock Inspector Window").OnChanged(() =>
                        {
                            QcUnity.FocusOn(PegiEditorOnly.SerObj.targetObject);
                            tracker.isLocked = true;
                        });
                    else
                        Icon.Lock.ClickUnFocus("Unlock Inspector Window").OnChanged(() =>
                        {
                            tracker.isLocked = false;
                            QcUnity.FocusOn(obj);
                        });
                }
#endif
            }
        }

        public static ChangesToken ClickLink(this TextLabel label, string link)
        {
            label.FallbackHint = ()=> "Go To: {0}".F(link);
            return label.ClickText(12).OnChanged(() => Application.OpenURL(link));
        }

        public static class ConfirmationDialogue
        {
            private static string _tag;
            private static object _targetObject;
            private static string _details;

            public static string ConfirmationText => _details.IsNullOrEmpty() ? Msg.AreYouSure.GetText() : _details;

            public static void Request(string tag, object forObject = null, string details = "")
            {
                _tag = tag;
                _targetObject = forObject;
                _details = details;
            }

            public static void Close()
            {
                _tag = null;
                _targetObject = null;
            }

            public static bool IsRequestedFor(string tag) => tag.Equals(_tag);//(!_confirmTag.IsNullOrEmpty() && _confirmTag.Equals(tag));

            public static bool IsRequestedFor(string confirmationTag, object obj) =>
                confirmationTag.Equals(_tag) && ((_targetObject != null && _targetObject.Equals(obj)) ||
                                                        (obj == null && _targetObject == null));
        }
        
        private static ChangesToken ConfirmClick()
        {
            Nl();

            System.IDisposable disp = null;

            if (BgColorReplaced)
                disp = SetBgColorDisposable(_previousBgColors[0]);

            using (disp)
            {
                if (Icon.Close.Click(Msg.No.GetText(), 30))
                    ConfirmationDialogue.Close();

                ConfirmationDialogue.ConfirmationText.PegiLabel().WriteHint(false);

                if (Icon.Done.Click(Msg.Yes.GetText(), 30))
                {
                    ConfirmationDialogue.Close();
                    return ChangesToken.True;
                }
            }

            Nl();

            return ChangesToken.False;
        }

        public static ChangesToken ClickConfirm(this TextLabel label, Action action)
        {
            string confirmationTag = "{0}()".F(action.Method.Name);

            if (ConfirmationDialogue.IsRequestedFor(confirmationTag))
            {
                if (ConfirmClick())
                {
                    action.Invoke();
                    return ChangesToken.True;
                }

                return ChangesToken.False;
            }

            if (label.ClickUnFocus())
                ConfirmationDialogue.Request(tag: confirmationTag, details: label.toolTip);

            return ChangesToken.False;
        }

        public static ChangesToken ClickConfirm(Action action)
        {
            string confirmationTag = "{0}()".F(action.Method.Name);

            if (ConfirmationDialogue.IsRequestedFor(confirmationTag))
            {
                if (ConfirmClick()) 
                {
                    action.Invoke();
                    return ChangesToken.True;
                }

                return ChangesToken.False;
            }

            if (confirmationTag.PegiLabel().ClickUnFocus())
                ConfirmationDialogue.Request(tag: confirmationTag, "Execute {0}".F(confirmationTag));

            return ChangesToken.False;
        }

        public static ChangesToken ClickConfirm(this TextLabel label, string confirmationTag)
        {
            if (ConfirmationDialogue.IsRequestedFor(confirmationTag))
                return ConfirmClick();

            if (label.ClickUnFocus())
                ConfirmationDialogue.Request(tag: confirmationTag, details: label.toolTip);

            return ChangesToken.False;
        }

        public static ChangesToken ClickConfirm(this Icon icon, string confirmationTag, string toolTip = "", int width = defaultButtonSize)
        {
            if (ConfirmationDialogue.IsRequestedFor(confirmationTag))
                return ConfirmClick();

            if (icon.ClickUnFocus(toolTip, width))
                ConfirmationDialogue.Request(confirmationTag, details: toolTip.IsNullOrEmpty() ? icon.GetTranslations().text : toolTip);

            return ChangesToken.False;
        }

        public static ChangesToken ClickConfirm(this Icon icon, string confirmationTag, object obj, string toolTip = "", int width = defaultButtonSize)
        {
            if (ConfirmationDialogue.IsRequestedFor(confirmationTag, obj))
                return ConfirmClick();

            if (icon.ClickUnFocus(toolTip, width))
                ConfirmationDialogue.Request(confirmationTag, obj, toolTip.IsNullOrEmpty() ? icon.GetTranslations().text : toolTip);

            return ChangesToken.False;
        }

        public static ChangesToken UnfocusOnChange(this ChangesToken token) 
        {
            if (token)
                UnFocus();
            return token;

        }

        public static ChangesToken UnfocusOnChange(this ChangesToken token, LatestInteractionEvent evnt)
        {
            if (token)
                UnFocus();
            return FeedChanges_Internal(token, evnt);
        }

        public static ChangesToken ClickUnFocus(this TextLabel text)
        {
            var cntnt = TextAndTip(text);

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Click(cntnt).UnfocusOnChange(LatestInteractionEvent.Click);
#endif
            CheckLine();
            return new ChangesToken(GUILayout.Button(cntnt, Utils.GuiMaxWidthOptionFrom(text.label))).UnfocusOnChange(LatestInteractionEvent.Click);
        }

        public static ChangesToken ClickText(this TextLabel label, int fontSize)
        {
            var textAndTip = TextAndTip(label);

            var style = Styles.ScalableBlueText(fontSize);

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Click(textAndTip, style.Current);
#endif
            CheckLine();
            return  GUILayout.Button(textAndTip, style.Current, Utils.GuiMaxWidthOptionFrom(textAndTip, style: style)).FeedChanges_Internal(LatestInteractionEvent.Click);
        }

        public static ChangesToken ClickLabelConfirm(this TextLabel label, string confirmationTag)
        {
            if (ConfirmationDialogue.IsRequestedFor(confirmationTag))
                return ConfirmClick();

            label.FallbackHint = ()=> "Click " + label;


            if (label.ClickLabel())
                ConfirmationDialogue.Request(confirmationTag);

            return ChangesToken.False;
        }
        
        public static ChangesToken ClickLabel(this TextLabel label)
        {
            if (label.GotToolTip == false)
                label.toolTip = "Click " + label;

            using (SetBgColorDisposable(Color.clear))
            {
                GUIStyle st = label.GotStyle ? label.style.Current : Styles.ClickableText.Current;

                var textAndTip = TextAndTip(label);

#if UNITY_EDITOR
                if (!PaintingGameViewUI)
                {
                    var changes = (label.GotWidth ? PegiEditorOnly.Click(textAndTip, label.width, st) : PegiEditorOnly.Click(textAndTip, st)).UnfocusOnChange();
                    RestoreBGColor();
                    return changes;
                }
#endif

                CheckLine();

                return new ChangesToken(label.GotWidth ?
                    GUILayout.Button(textAndTip, st, GUILayout.MaxWidth(label.width)) :
                    GUILayout.Button(textAndTip, st, Utils.GuiMaxWidthOptionFrom(label.label, st))
                    ).UnfocusOnChange(LatestInteractionEvent.Click);
            }
        }

        private static ChangesToken ClickImage(this GUIContent content, int width, GUIStyle style)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.ClickImage(content, width, style);
#endif
            CheckLine();

            return GUILayout.Button(content, GUILayout.MaxWidth(width + 5), GUILayout.MaxHeight(width)).FeedChanges_Internal(LatestInteractionEvent.Click);
        }

        public static ChangesToken Click(System.Action action)
        {
            string name = "{0}()".F(action.Method.Name);

            return name.PegiLabel().Click(action);
        }

        public static ChangesToken Click(this TextLabel text, System.Action onClick)
        {
            text.FallbackHint = () => onClick.Method.Name;

            return text.Click().OnChanged(onClick); 
        }

        public static ChangesToken Click(this TextLabel text)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Click(text);
#endif
            CheckLine();
            return GUILayout.Button(text.label, Utils.GuiMaxWidthOptionFrom(text.label)).FeedChanges_Internal(LatestInteractionEvent.Click);
        }

        private static Texture GetTexture_orEmpty(this Sprite sp) => sp ? sp.texture : Icon.Empty.GetIcon();

        internal static ChangesToken Click(Sprite img, string toolTip, int size = defaultButtonSize)
            => Click(img.GetTexture_orEmpty(), toolTip, size);

        public static ChangesToken Click(Texture img, int size = defaultButtonSize)
        {

            if (!img) img = Icon.Empty.GetIcon();

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Click(img, size);
#endif

            CheckLine();
            return GUILayout.Button(img, GUILayout.MaxWidth(size + 5), GUILayout.MaxHeight(size)).FeedChanges_Internal(LatestInteractionEvent.Click);

        }

        public static ChangesToken Click(Texture img, string toolTip, int size = defaultButtonSize)
        {

            if (!img)
                img = Icon.Empty.GetIcon();
            
            var cnt = ImageAndTip(img, toolTip);

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.ClickImage(cnt, size);
#endif

            CheckLine();
            return GUILayout.Button(cnt, GUILayout.MaxWidth(size + 5), GUILayout.MaxHeight(size)).FeedChanges_Internal(LatestInteractionEvent.Click);
        }

        public static ChangesToken Click(Texture img, string toolTip, int width, int height)
        {
            if (!img) img = Icon.Empty.GetIcon();

            var cnt = ImageAndTip(img, toolTip);

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.ClickImage(cnt, width, height);
#endif
            CheckLine();
            return GUILayout.Button(cnt, GUILayout.MaxWidth(width), GUILayout.MaxHeight(height)).FeedChanges_Internal(LatestInteractionEvent.Click);
        }

        public static ChangesToken Click(this Icon icon, string toolTip, int width, int height)
        {
            if (!icon.TryGetTexture(out var tex))
                return icon.GetText().ClickUnFocus();

            return Click(tex, toolTip, width, height);
        }

        public static ChangesToken Click(this Icon icon, System.Action onClick, string toolTip) => icon.Click(toolTip: toolTip).OnChanged(onClick);
        
        public static ChangesToken Click(this Icon icon, System.Action onClick)
        {
            var hint = onClick.Method.Name;
            if (hint.Contains("Inspect"))
                hint = icon.GetText().label;

            return icon.Click(toolTip: hint).OnChanged(onClick);
        }

        public static ChangesToken Click(this Icon icon)
        {
            if (!icon.TryGetTexture(out var tex))
                return icon.GetText().Click();
            
            return Click(tex, icon.GetText().label);
        }

        public static ChangesToken ClickUnFocus(this Icon icon)
        {
            if (!icon.TryGetTexture(out var tex))
                return icon.GetText().ClickUnFocus();

            return Click(tex, icon.GetText().label).UnfocusOnChange();
        }

        public static ChangesToken ClickUnFocus(this Icon icon, string toolTip, int size = defaultButtonSize)
        {
            if (toolTip.IsNullOrEmpty())
                toolTip = icon.GetText().label;

            if (!icon.TryGetTexture(out var tex)) 
            {
                var label = icon.GetText();
                label.toolTip = toolTip;
                return label.ClickUnFocus();
            }

            return Click(tex, toolTip, size).UnfocusOnChange();
        }

        public static ChangesToken Click(this Icon icon, int size)
        {
            if (!icon.TryGetTexture(out var tex))
                return icon.GetText().Click();

            return Click(tex, size);
        }

        public static ChangesToken Click(this Icon icon, string toolTip, int size = defaultButtonSize)
        {
            if (!icon.TryGetTexture(out var tex))
                return icon.GetText(toolTip).Click();

            return Click(tex, toolTip, size);
        }

        public static ChangesToken Click(Color col)
        {
            using (SetBgColorDisposable(Color.clear))
            {
                using (SetGuiColorDisposable(col))
                {
                    return Icon.Empty.Click();
                }
            }
        }

        public static ChangesToken ClickHighlight(Sprite sp, int width = defaultButtonSize)
        {
#if UNITY_EDITOR
            if (sp && Click(sp, Msg.HighlightElement.GetText(), width).IgnoreChanges(LatestInteractionEvent.Click))
            {
                UnityEditor.EditorGUIUtility.PingObject(sp);
                return ChangesToken.True;
            }
#endif
            return ChangesToken.False;
        }

        public static ChangesToken ClickHighlight(Texture tex, int width = defaultButtonSize)
        {
#if UNITY_EDITOR
            if (tex && Click(tex, Msg.HighlightElement.GetText(), width).IgnoreChanges())
            {
                UnityEditor.EditorGUIUtility.PingObject(tex);
                return ChangesToken.True;
            }
#endif

            return ChangesToken.False;
        }

        public static ChangesToken ClickHighlight(Object obj, int width = defaultButtonSize) =>
           ClickHighlight(obj, Icon.Ping.GetIcon(), width).IgnoreChanges();

        public static ChangesToken ClickHighlight(Object obj, Texture tex, int width = defaultButtonSize)
        {
#if UNITY_EDITOR
            if (obj && pegi.Click(tex, Msg.HighlightElement.GetText()).IgnoreChanges())
            {
                UnityEditor.EditorGUIUtility.PingObject(obj);
                return ChangesToken.True;
            }
#endif

            return ChangesToken.False;
        }

        public static ChangesToken ClickHighlight(Object obj, string hint, int width = defaultButtonSize)
        {
#if UNITY_EDITOR
            if (obj && Icon.Ping.Click(hint).IgnoreChanges(LatestInteractionEvent.Click))
            {
                UnityEditor.EditorGUIUtility.PingObject(obj);
                return ChangesToken.True;
            }
#endif

            return ChangesToken.False;
        }

        public static StateToken isAttentionWrite (this INeedAttention attention, bool canBeNull = false) 
        {
            string warningMsg;

            if (attention.IsNullOrDestroyed_Obj())
            {
                if (canBeNull)
                    return StateToken.False;

                warningMsg = "Object is null";
            }
            else
                warningMsg = attention.NeedAttention();

            if (warningMsg != null)
            {
                if (Icon.Warning.Click("Copy to Clipboard"))
                    SetCopyPasteBuffer(warningMsg);

                warningMsg.PegiLabel(Styles.WarningText).Write();
                return StateToken.True;
            }

            return StateToken.False;
        }

        public static ChangesToken Click_Enter_Attention(this INeedAttention attention, Icon icon = Icon.Enter, string hint = "", bool canBeNull = true)
        {
            if (attention.IsNullOrDestroyed_Obj())
            {
                if (!canBeNull)
                    return Icon.Warning.ClickUnFocus("Object is null; {0}".F(hint));
            }
            else
            {
                var msg = attention.NeedAttention();
                if (msg != null)
                {
                    Icon.Warning.Draw(msg);
                    return Icon.Enter.ClickUnFocus(hint);
                }
            }

            if (hint.IsNullOrEmpty())
                hint = icon.GetText().label;

            return icon.ClickUnFocus(hint);
        }
    }
}