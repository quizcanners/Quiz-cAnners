using QuizCanners.Utils;
using UnityEngine;
#if UNITY_EDITOR
using  UnityEditor;
#endif

// ReSharper disable InconsistentNaming
#pragma warning disable IDE0011 // Add braces
#pragma warning disable IDE0008 // Use explicit type

namespace QuizCanners.Inspect
{
    public static partial class pegi
    {
        private static readonly TextToken TEXT_TOK = new();

        private static readonly GUIContent _textAndToolTip = new();
        internal static GUIContent ToGUIContext(this TextLabel text)
        {
            _textAndToolTip.text = text.label;
            _textAndToolTip.tooltip = text.toolTip;
            return _textAndToolTip;
        }

        public static TextLabel ConstLabel(this string label, Styles.PegiGuiStyle style)
        {
            var pgi = new TextLabel(label, style: style);

            pgi.width = 10 + Mathf.CeilToInt(GUI.skin.label.CalcSize(pgi.ToGUIContext()).x);

            return pgi;
        }
        public static TextLabel ConstLabel(this string label, string toolTip = null, Styles.PegiGuiStyle style = null) 
        {
            var pgi = new TextLabel(label, toolTip: toolTip, style: style);

            pgi.width = 10 + Mathf.CeilToInt(GUI.skin.label.CalcSize(pgi.ToGUIContext()).x);

            return pgi;
        }

        public static TextLabel PegiLabel(this string label, float widthFraction) => new(label, null, Mathf.FloorToInt(widthFraction * Screen.width), null);
        public static TextLabel PegiLabel(this string label, int width) => new(label, null, width, null);
        public static TextLabel PegiLabel(this string label, Styles.PegiGuiStyle style) => new(label, null, -1, style);
        public static TextLabel PegiLabel(this string label, int width, Styles.PegiGuiStyle style) => new(label, null, width, style);

        public static TextLabel PegiLabel(this string label, string toolTip, float widthFraction, Styles.PegiGuiStyle style = null) => new(label, toolTip, Mathf.FloorToInt(widthFraction * Screen.width), style);
        public static TextLabel PegiLabel(this string label, string toolTip = null, int width = -1, Styles.PegiGuiStyle style = null) => new(label, toolTip, width, style);

        public static TextToken Write(this TextLabel label) => label.TryWrite();

        internal static TextToken Write(TextLabel label, float defaultWidthFraction)
        {
            label.FallbackWidthFraction = defaultWidthFraction;
            return label.TryWrite();
        }

        public struct TextLabel
        {
            internal string label;
           
            internal int width;
            internal string toolTip;
            internal Styles.PegiGuiStyle style;

            internal readonly string TooltipOrLabel => GotToolTip ? toolTip : label;

            internal System.Func<string> FallbackHint 
            { 
                set 
                {
                    if (!GotToolTip)
                        toolTip = value.Invoke();
                } 
            }

            internal int FallbackWidth
            {
                set
                {
                    if (!GotWidth)
                        width = value;
                }
            }

            internal float FallbackWidthFraction
            {
                set 
                {
                    if (!GotWidth)
                        width = Mathf.FloorToInt(value * Screen.width);
                }
            }

            public TextLabel ApproxWidth() 
            {
                if (GotWidth == false)
                    width = Utils.GuiMaxWidthFrom(label);
                return this;
            }

            internal readonly bool IsInitialized => !label.IsNullOrEmpty();
            internal readonly bool GotWidth => width > 0;
            internal readonly bool GotToolTip => !toolTip.IsNullOrEmpty();
            internal readonly bool GotStyle => style != null;

            public override readonly string ToString() => label;

            public readonly bool TryGetLabel(out string label)
            {
                label = this.label;
                return true;
            }

            public readonly TextToken TryWrite() 
            {
                if (IsInitialized == false)
                    return TEXT_TOK;

                if (GotWidth)
                {
                    if (GotToolTip)
                    {
                        if (GotStyle)
                            Write(label, toolTip, width, style);
                        else
                            Write(label, toolTip, width);
                    }
                    else
                    {
                        if (GotStyle)
                            Write(label, label, width, style);
                        else
                            Write(label, label, width);
                    }

                }
                else
                {
                    if (GotToolTip)
                    {
                        if (GotStyle)
                            Write(label, toolTip, style: style);
                        else
                            Write(label, toolTip);
                    }
                    else
                    {
                        if (GotStyle)
                            Write(label, label, style);
                        else
                            Write(label, label);
                    }
                }

                return TEXT_TOK;
            }

            internal TextLabel(string label, string toolTip = null, int width = -1, Styles.PegiGuiStyle style = null) 
            {
                this.label = label;
                this.toolTip = toolTip;
                this.width = width;
                this.style = style;
            }

            /*
            
            private static TextToken Write_Internal(string text)
            {
                var cnt = TextAndTip(text);

#if UNITY_EDITOR
                if (!PaintingGameViewUI)
                    PegiEditorOnly.Write(cnt);
                else
#endif
                {
                    CheckLine();
                    GUILayout.Label(cnt, Utils.GuiMaxWidthOption);
                }

                return TEXT_TOK;
            }

            private static TextToken Write(string text, Styles.PegiGuiStyle style)
            {
                var cnt = TextAndTip(text);

#if UNITY_EDITOR
                if (!PaintingGameViewUI)
                    PegiEditorOnly.Write(cnt, style.Current);
                else
#endif
                {
                    CheckLine();
                    GUILayout.Label(cnt, style.Current, Utils.GuiMaxWidthOption);
                }

                return TEXT_TOK;
            }

            private static TextToken Write(string text, int width, Styles.PegiGuiStyle style)
            {
                textAndTip.text = text;
                textAndTip.tooltip = text;

#if UNITY_EDITOR
                if (!PaintingGameViewUI)
                {
                    PegiEditorOnly.Write(textAndTip, width, style.Current);
                    return TEXT_TOK;
                }
#endif

                CheckLine();
                GUILayout.Label(textAndTip, style.Current, GUILayout.MaxWidth(width));
                return TEXT_TOK;

            }

            */

            private static TextToken Write(string text, string toolTip, int width, Styles.PegiGuiStyle style)
            {

                textAndTip.text = text;
                textAndTip.tooltip = toolTip;

#if UNITY_EDITOR
                if (!PaintingGameViewUI)
                {
                    PegiEditorOnly.Write(textAndTip, width, style.Current);
                    return TEXT_TOK;
                }
#endif

                CheckLine();
                GUILayout.Label(textAndTip, style.Current, GUILayout.MaxWidth(width));
                return TEXT_TOK;

            }

            private static TextToken Write(string text, string toolTip, Styles.PegiGuiStyle style)
            {

                textAndTip.text = text;
                textAndTip.tooltip = toolTip;

#if UNITY_EDITOR
                if (!PaintingGameViewUI)
                {
                    PegiEditorOnly.Write(textAndTip, style.Current);
                    return TEXT_TOK;
                }
#endif

                CheckLine();
                GUILayout.Label(textAndTip, style.Current);
                return TEXT_TOK;

            }

            //private static TextToken Write(string text, int width) => Write(text, text, width);

            private static TextToken Write(string text, string toolTip)
            {

                textAndTip.text = text;
                textAndTip.tooltip = toolTip;

#if UNITY_EDITOR
                if (!PaintingGameViewUI)
                {
                    PegiEditorOnly.Write(textAndTip);
                    return TEXT_TOK;
                }
#endif

                CheckLine();
                GUILayout.Label(textAndTip, Utils.GuiMaxWidthOption);
                return TEXT_TOK;
            }

            private static TextToken Write(string text, string toolTip, int width)
            {

                textAndTip.text = text;
                textAndTip.tooltip = toolTip;

#if UNITY_EDITOR
                if (!PaintingGameViewUI)
                {
                    PegiEditorOnly.Write(textAndTip, width);
                    return TEXT_TOK;
                }
#endif

                CheckLine();
                GUILayout.Label(textAndTip, GUILayout.MaxWidth(width));
                return TEXT_TOK;
            }


        }

        public class TextToken 
        {
            internal TextToken() { }
        }

        #region GUI Contents
        private static readonly GUIContent imageAndTip = new();

        private static GUIContent ImageAndTip(Texture tex, string toolTip)
        {
            imageAndTip.image = tex;
            imageAndTip.tooltip = toolTip;
            return imageAndTip;
        }

        private static readonly GUIContent textAndTip = new();

        private static GUIContent TextAndTip(string text)
        {
            textAndTip.text = text;
            textAndTip.tooltip = text;
            return textAndTip;
        }

        private static GUIContent TextAndTip(TextLabel text)
        {
            textAndTip.text = text.label;
            textAndTip.tooltip = text.GotToolTip ? text.toolTip : text.label;
            return textAndTip;
        }

        #endregion

        #region Unity Object

        public static TextToken Write<T>(T field) where T : Object
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                PegiEditorOnly.Write(field);
#endif

            return  TEXT_TOK;
        }

        public static TextToken Write<T>(this TextLabel label, T field) where T : Object
        {
            Write(label);
            return Write(field);

        }

        public static TextToken Draw(Sprite sprite, int width = defaultButtonSize, bool alphaBlend = true) =>
            Draw(sprite, Color.white, width: width, alphaBlend: alphaBlend);
        
        public static TextToken Draw(Sprite sprite, Color color, int width = defaultButtonSize, bool alphaBlend = true)
        {
            if (!sprite)
            {
                Icon.Empty.Draw(width);
                return TEXT_TOK;
            }

            CheckLine();

            Rect c = sprite.textureRect;

            float max = Mathf.Max(c.width, c.height);

            float scale = width / max;
            float spriteW = c.width * scale;
            float spriteH = c.height * scale;
            Rect rect = GUILayoutUtility.GetRect(spriteW, spriteH, GUILayout.ExpandWidth(false));

            if (Event.current.type != EventType.Repaint)
                return TEXT_TOK;

            if (sprite.packed)
            {
                var tex = sprite.texture;
                c.xMin /= tex.width;
                c.xMax /= tex.width;
                c.yMin /= tex.height;
                c.yMax /= tex.height;

                using (SetGuiColorDisposable(color))
                {
                    GUI.DrawTextureWithTexCoords(rect, tex, c, alphaBlend);
                }
                return TEXT_TOK;
            }

            GUI.DrawTexture(rect, sprite.texture, ScaleMode.ScaleToFit, alphaBlend, 1, color,
                Vector4.zero, Vector4.zero);

            return TEXT_TOK;
        }


        public static TextToken Draw(this TextLabel text, Texture image, bool alphaBlend = false) 
        {
            text.Write().Nl();
            var ret = Draw(image, width: Screen.width, alphaBlend: alphaBlend);
            Nl();
            return ret;
        }

        public static TextToken Draw(Texture img, int width = defaultButtonSize, bool alphaBlend = false)
        {
            if (!img)
                return TEXT_TOK;

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                PegiEditorOnly.Draw(img, width, alphaBlend: alphaBlend);
                return TEXT_TOK;
            }
#endif
            
            SetBgColor(Color.clear);
            Click(img, width);
            RestoreBGColor();
            
            return TEXT_TOK;
        }

        public static TextToken Draw(Texture img, string toolTip, int width = defaultButtonSize)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                PegiEditorOnly.Draw(img, toolTip, width, width);
                return TEXT_TOK;
            }
#endif

            SetBgColor(Color.clear);
            Click(img, toolTip, width, width);
            RestoreBGColor();
            
            return TEXT_TOK;
        }

        public static TextToken Draw(Texture img, string toolTip, int width, int height)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                PegiEditorOnly.Draw(img, toolTip, width, height);
                return TEXT_TOK;
            }
#endif

            SetBgColor(Color.clear);
            Click(img, toolTip, width, height);
            RestoreBGColor();
            return TEXT_TOK;
        }

        #endregion

        #region Icon

        public static TextToken Draw(this Icon icon, int size = defaultButtonSize) => Draw(icon.GetIcon(), size, alphaBlend: true);

        public static TextToken Draw(this Icon icon, string toolTip, int size = defaultButtonSize) => Draw(icon.GetIcon(), toolTip, size);

        public static TextToken Write(this Icon icon, string toolTip, int width, int height) => Draw(icon.GetIcon(), toolTip, width, height);

        #endregion

        #region String

        public static TextToken WriteBig(this TextLabel text, string contents)
        {
            text.Nl();
            contents.PegiLabel().WriteBig();
            Nl();
            return TEXT_TOK;
        }

        public static TextToken WriteBig(this TextLabel text, TextLabel contents)
        {
            text.Nl();
            contents.WriteBig();
            Nl();
            return TEXT_TOK;
        }

        public static TextToken WriteBig(this TextLabel text)
        {
            text.style = Styles.OverflowText;
            text.Write();
            Nl();
            return TEXT_TOK;
        }

        public static ChangesToken Write_ForCopy(this TextLabel text, bool showCopyButton = false, bool writeAsEditField = false)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI && !writeAsEditField)
                PegiEditorOnly.Write_ForCopy(text);
            else
#endif
            {
                var tmp = text.label;
                Edit(ref tmp);
            }

            if (showCopyButton && Icon.Copy.Click("Copy text to clipboard"))
                SetCopyPasteBuffer(text.label);

            return ChangesToken.False;
        }

        public static ChangesToken Write_ForCopy(this TextLabel text, int width, bool showCopyButton = false)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                PegiEditorOnly.Write_ForCopy(text);
            else
#endif
            {
                var tmp = text.label;
                Edit(ref tmp);
            }

            if (showCopyButton && Icon.Copy.Click("Copy text to clipboard"))
                SetCopyPasteBuffer(text.label);

            return ChangesToken.False;

        }

        public static ChangesToken Write_ForCopy(this TextLabel label, string value, bool showCopyButton = false)
        {
            if (!label.TryGetLabel(out var hint))
                hint = "Unlabeled buffer";

            Write(label, 0.33f);
            var ret = Edit(ref value);

            if (showCopyButton && Icon.Copy.Click("Copy {0} to clipboard".F(label)))
                SetCopyPasteBuffer(value, hint);

            return ret;

        }

        private const int TEXT_LINE_HEIGHT = 17;

        public static ChangesToken Write_ForCopy_Big(this TextLabel value, bool showCopyButton = false, int lines = 5)
        {

            var text = value.label;
            if (showCopyButton && "Copy text to clipboard".PegiLabel().Click().Nl())
                SetCopyPasteBuffer(text);

            if (PaintingGameViewUI && !text.IsNullOrEmpty() && ContainsAtLeast(value.label, '\n', 5)) // Due to MGUI BUG
                ".....   Big Text Has Many Lines: {0}".F(text.FirstLine()).PegiLabel().Write();
            else
            {
                return Edit_Big(ref text, height: lines * TEXT_LINE_HEIGHT);

            }
            return ChangesToken.False;
        }

        public static ChangesToken Write_ForCopy_Big(this TextLabel label, string value, bool showCopyButton = false, int lines = 5)
        {
            if (!label.TryGetLabel(out var hint))
                hint = "Unlabeled buffer";

            Write(label, 0.33f);

            if (showCopyButton && Icon.Copy.Click("Copy text to clipboard"))
                SetCopyPasteBuffer(value, hint);

            Nl();

            if (PaintingGameViewUI && !value.IsNullOrEmpty() && ContainsAtLeast(value, '\n', 5)) // Due to MGUI BUG
                ".....   Big Text Has Many Lines: {0}".F(value.FirstLine()).PegiLabel().Write();
            else
                return Edit_Big(ref value, lines * TEXT_LINE_HEIGHT);

            return ChangesToken.False;
        }

        public static string CopyPasteBuffer 
        {
            get => GUIUtility.systemCopyBuffer;
            set => GUIUtility.systemCopyBuffer = value;
        }

        public static void SetCopyPasteBuffer(string value, string hint = "", bool sendNotificationIn3Dview = true)
        {
            GUIUtility.systemCopyBuffer = value;

            if (sendNotificationIn3Dview)
                GameView.ShowNotification("{0} Copied to clipboard".F(hint));
        }

        private static bool ContainsAtLeast(string text, char symbols = '\n', int occurances = 1)
        {

            if (text.IsNullOrEmpty())
                return false;

            foreach (var c in text)
            {
                if (c == symbols)
                {
                    occurances--;
                    if (occurances <= 0)
                        return true;
                }
            }

            return false;
        }


        #endregion

        #region Warning & Hints
        public static TextToken Write(System.Exception ex)
        {
            Icon.Warning.Draw();
            var txt = ex.ToString();
            Write_ForCopy(txt.PegiLabel(), showCopyButton: true);
            if ("Log".PegiLabel().Click())
                Debug.LogException(ex);
            Nl();
            return TEXT_TOK;
        }

        public static TextToken WriteWarning(this TextLabel text)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                PegiEditorOnly.WriteHint(text, MessageType.Warning);
                Nl();
                //PegiEditorOnly.newLine();
                return TEXT_TOK;
            }
#endif

            CheckLine();
            GUILayout.Label(text.label, Styles.WarningText.Current, Utils.GuiMaxWidthOption);
            Nl();
            return TEXT_TOK;

        }

        public static TextToken Write_Hint(this TextLabel text, bool startNewLineAfter = true)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                PegiEditorOnly.WriteHint(text, MessageType.Info);
                if (startNewLineAfter)
                    Nl();
                return TEXT_TOK;
            }
#endif

            CheckLine();
            GUILayout.Label(text.label, Styles.HintText.Current, Utils.GuiMaxWidthOption);
            if (startNewLineAfter)
                Nl();

            return TEXT_TOK;
        }
        
        public static void ResetOneTimeHint(string key) => PlayerPrefs.SetInt(key, 0);

        public static void HideOneTimeHint(string key) => PlayerPrefs.SetInt(key, 1);

        public static StateToken WriteOneTimeHint(this TextLabel text, string key)
        {

            if (PlayerPrefs.GetInt(key) != 0) return StateToken.False;

            Nl();

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                PegiEditorOnly.WriteHint(text, MessageType.Info);
            }
            else
#endif
            {
                CheckLine();
                GUILayout.Label(text.label, Styles.HintText.Current, Utils.GuiMaxWidthOption);
            }

            if (Icon.Done.ClickUnFocus("Got it").Nl()) 
                PlayerPrefs.SetInt(key, 1);

            return StateToken.True;
        }

        #endregion

        #region Progress Bar


        public static TextToken DrawProgressBar(this TextLabel text, float value)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                PegiEditorOnly.ProgressBar(text, value);
            else
#endif
            {
                CheckLine();
                text.label = "{0}: {1}%".F(text, Mathf.FloorToInt(value * 100));
                text.Write();
                //GUILayout.Label(cnt, GuiMaxWidthOption);
            }

            return TEXT_TOK;
        }

        #endregion
    }
}
