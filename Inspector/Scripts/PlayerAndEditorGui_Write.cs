using QuizCanners.Utils;
using UnityEngine;
#if UNITY_EDITOR
using  UnityEditor;
#endif

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable IDE0011 // Add braces
#pragma warning disable IDE0008 // Use explicit type

namespace QuizCanners.Inspect
{
    public static partial class pegi
    {
        private static readonly TextToken TEXT_TOK = new TextToken();

        public interface IPegiText 
        {
            TextToken TryWrite();
            bool TryGetLabel(out string label);
        }

        public static TextLabel PegiLabel(this string label, int width) => new TextLabel(label, null, width, null);
        public static TextLabel PegiLabel(this string label, Styles.PegiGuiStyle style) => new TextLabel(label, null, -1, style);
        public static TextLabel PegiLabel(this string label, string toolTip = null, int width = -1, Styles.PegiGuiStyle style = null) => new TextLabel(label, toolTip, width, style);

        public static TextToken write(this IPegiText label) => label == null ? TEXT_TOK : label.TryWrite();

        public struct TextLabel : IPegiText
        {
            internal string label;
           
            internal int width;
            internal string toolTip;
            internal Styles.PegiGuiStyle style;

            internal string TooltipOrLabel => GotToolTip ? toolTip : label;

            internal System.Func<string> FallbackHint 
            { 
                set 
                {
                    if (!GotToolTip)
                        toolTip = value.Invoke();
                } 
            }

            public TextLabel ApproxWidth() 
            {
                if (GotWidth == false)
                    width = Utils.GuiMaxWidthFrom(label);
                return this;
            }

            internal GUILayoutOption WidthOrApprox => GotWidth ? GUILayout.MaxWidth(width) : Utils.GuiMaxWidthOptionFrom(label);

            internal bool IsInitialized => label.Length > 0;
            internal bool GotWidth => width > 0;
            internal bool GotToolTip => !toolTip.IsNullOrEmpty();
            internal bool GotStyle => style != null;

            public override string ToString() => label;

            public bool TryGetLabel(out string label)
            {
                label = this.label;
                return true;
            }

            public TextToken TryWrite() 
            {
                if (IsInitialized == false)
                    return TEXT_TOK;

                if (GotWidth)
                {
                    if (GotToolTip)
                    {
                        if (GotStyle)
                            write(label, toolTip, width, style);
                        else
                            write(label, toolTip, width);
                    }
                    else
                    {
                        if (GotStyle)
                            write(label, width, style);
                        else
                            write(label, width);
                    }

                }
                else
                {
                    if (GotToolTip)
                    {
                        if (GotStyle)
                            write(label, toolTip, style: style);
                        else
                            write(label, toolTip);
                    }
                    else
                    {
                        if (GotStyle)
                            write(label, style);
                        else
                            write(label);
                    }
                }

                return TEXT_TOK;
            }

            internal TextLabel(string label, string tooltip = null, int width = -1, Styles.PegiGuiStyle style = null) 
            {
                this.label = label;
                toolTip = tooltip;
                this.width = width;
                this.style = style;
            }
        }

        public struct TextToken : IPegiText
        {
            public TextToken TryWrite() => this;

            public override string ToString() => "";

            public bool TryGetLabel(out string label)
            {
                label = null;
                return false;
            }
        }

        #region GUI Contents
        private static readonly GUIContent imageAndTip = new GUIContent();

        private static GUIContent ImageAndTip(Texture tex, string toolTip)
        {
            imageAndTip.image = tex;
            imageAndTip.tooltip = toolTip;
            return imageAndTip;
        }

      /*  private static GUIContent ImageAndTip(Texture tex)
        {
            imageAndTip.image = tex;
            imageAndTip.tooltip = tex ? tex.name : "Null Image";
            return imageAndTip;
        }*/

        private static readonly GUIContent textAndTip = new GUIContent();

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

       // private static GUIContent tipOnlyContent = new GUIContent();

      /*  private static GUIContent TipOnlyContent(string text)
        {
            tipOnlyContent.tooltip = text;
            return tipOnlyContent;
        }*/

        #endregion

        #region Unity Object

        public static void write<T>(T field) where T : Object
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                PegiEditorOnly.write(field);
#endif
        }

        public static void write<T>(this TextLabel label, T field) where T : Object
        {
            write(label);
            write(field);

        }

        public static void draw(this Sprite sprite, int width = defaultButtonSize, bool alphaBlend = false) =>
            draw(sprite, Color.white, width: width, alphaBlend: alphaBlend);
        
        public static void draw(this Sprite sprite, Color color, int width = defaultButtonSize, bool alphaBlend = false)
        {
            if (!sprite)
            {
                icon.Empty.draw(width);
            }
            else
            {

                checkLine();

                Rect c = sprite.textureRect;

                float max = Mathf.Max(c.width, c.height);

                float scale = defaultButtonSize / max;

                float spriteW = c.width * scale;
                float spriteH = c.height * scale;
                Rect rect = GUILayoutUtility.GetRect(spriteW, spriteH,
                    GUILayout.ExpandWidth(false));

                if (Event.current.type == EventType.Repaint)
                {
                    if (sprite.packed)
                    {
                        var tex = sprite.texture;
                        c.xMin /= tex.width;
                        c.xMax /= tex.width;
                        c.yMin /= tex.height;
                        c.yMax /= tex.height;
                        GUI.DrawTextureWithTexCoords(rect, tex, c, alphaBlend);
                    }

                    else
                    {
                        GUI.DrawTexture(rect, sprite.texture, ScaleMode.ScaleToFit, alphaBlend, 1, color,
                            Vector4.zero, Vector4.zero);
                    }
                }
            }

        }

        public static TextToken draw(this Texture img, int width = defaultButtonSize, bool alphaBlend = true)
        {
            if (!img)
                return TEXT_TOK;

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                PegiEditorOnly.draw(img, width, alphaBlend: alphaBlend);

            else
#endif
            {
                SetBgColor(Color.clear);

                img.Click(width);

                RestoreBGColor();
            }

            return TEXT_TOK;
        }

        public static TextToken draw(this Texture img, string toolTip, int width = defaultButtonSize)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                PegiEditorOnly.draw(img, toolTip, width, width);
            else
#endif
            {

                SetBgColor(Color.clear);
                img.Click(toolTip, width, width);
                RestoreBGColor();
            }
            return TEXT_TOK;
        }

        public static TextToken draw(this Texture img, string toolTip, int width, int height)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                PegiEditorOnly.draw(img, toolTip, width, height);
            else
#endif
            {

                SetBgColor(Color.clear);

                img.Click(toolTip, width, height);

                RestoreBGColor();

            }
            return TEXT_TOK;
        }

        #endregion

        #region Icon

        public static TextToken draw(this icon icon, int size = defaultButtonSize) => draw(icon.GetIcon(), size);

        public static TextToken draw(this icon icon, string toolTip, int size = defaultButtonSize) => draw(icon.GetIcon(), toolTip, size);

        public static TextToken write(this icon icon, string toolTip, int width, int height) => draw(icon.GetIcon(), toolTip, width, height);

        #endregion

        #region String

        private static TextToken write(string text)
        {
            var cnt = TextAndTip(text);

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                PegiEditorOnly.write(cnt);
            else
#endif
            {
                checkLine();
                GUILayout.Label(cnt, Utils.GuiMaxWidthOption);
            }

            return TEXT_TOK;
        }

        private static TextToken write(string text, Styles.PegiGuiStyle style)
        {
            var cnt = TextAndTip(text);

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                PegiEditorOnly.write(cnt, style.Current);
            else
#endif
            {
                checkLine();
                GUILayout.Label(cnt, style.Current, Utils.GuiMaxWidthOption);
            }

            return TEXT_TOK;
        }

        private static TextToken write(string text, int width, Styles.PegiGuiStyle style)
        {
            textAndTip.text = text;
            textAndTip.tooltip = text;

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                PegiEditorOnly.write(textAndTip, width, style.Current);
                return TEXT_TOK;
            }
#endif

            checkLine();
            GUILayout.Label(textAndTip, style.Current, GUILayout.MaxWidth(width));
            return TEXT_TOK;

        }

        private static TextToken write(string text, string toolTip, int width, Styles.PegiGuiStyle style)
        {

            textAndTip.text = text;
            textAndTip.tooltip = toolTip;

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                PegiEditorOnly.write(textAndTip, width, style.Current);
                return TEXT_TOK;
            }
#endif

            checkLine();
            GUILayout.Label(textAndTip, style.Current, GUILayout.MaxWidth(width));
            return TEXT_TOK;

        }

        private static TextToken write(string text, string toolTip, Styles.PegiGuiStyle style)
        {

            textAndTip.text = text;
            textAndTip.tooltip = toolTip;

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                PegiEditorOnly.write(textAndTip, style.Current);
                return TEXT_TOK;
            }
#endif

            checkLine();
            GUILayout.Label(textAndTip, style.Current);
            return TEXT_TOK;

        }

        private static TextToken write(string text, int width) => write(text, text, width);

        private static TextToken write(string text, string toolTip)
        {

            textAndTip.text = text;
            textAndTip.tooltip = toolTip;

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                PegiEditorOnly.write(textAndTip);
                return TEXT_TOK;
            }
#endif

            checkLine();
            GUILayout.Label(textAndTip, Utils.GuiMaxWidthOption);
            return TEXT_TOK;
        }

        private static TextToken write(string text, string toolTip, int width)
        {

            textAndTip.text = text;
            textAndTip.tooltip = toolTip;

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                PegiEditorOnly.write(textAndTip, width);
                return TEXT_TOK;
            }
#endif

            checkLine();
            GUILayout.Label(textAndTip, GUILayout.MaxWidth(width));
            return TEXT_TOK;
        }
        
        public static TextToken writeBig(this TextLabel text, TextLabel contents)
        {
            text.nl();
            contents.writeBig();
            nl();
            return TEXT_TOK;
        }

        public static TextToken writeBig(this TextLabel text)
        {
            text.style = Styles.OverflowText;
            text.write();
            nl();
            return TEXT_TOK;
        }

        public static ChangesToken write_ForCopy(this TextLabel text, bool showCopyButton = false)
        {

            var ret = false;

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                PegiEditorOnly.write_ForCopy(text);
            else
#endif
            {
                var tmp = text.label;
                ret = edit(ref tmp);
            }

            if (showCopyButton && icon.Copy.Click("Copy text to clipboard"))
                SetCopyPasteBuffer(text.label);

            return new ChangesToken(ret);
        }

        public static ChangesToken write_ForCopy(this TextLabel text, int width, bool showCopyButton = false)
        {
            var ret = false;

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                PegiEditorOnly.write_ForCopy(text);
            else
#endif
            {
                var tmp = text.label;
                ret = edit(ref tmp);
            }

            if (showCopyButton && icon.Copy.Click("Copy text to clipboard"))
                SetCopyPasteBuffer(text.label);

            return new ChangesToken(ret);

        }

        public static ChangesToken write_ForCopy(this TextLabel label, string value, bool showCopyButton = false)
        {
            if (!label.TryGetLabel(out var hint))
                hint = "Unlabeled buffer";

            write(label);
            var ret = edit(ref value);

            if (showCopyButton && icon.Copy.Click("Copy {0} to clipboard".F(label)))
                SetCopyPasteBuffer(value, hint);

            return ret;

        }

        public static ChangesToken write_ForCopy_Big(this TextLabel value, bool showCopyButton = false)
        {

            var text = value.label;
            if (showCopyButton && "Copy text to clipboard".PegiLabel().Click().nl())
                SetCopyPasteBuffer(text);

            if (PaintingGameViewUI && !text.IsNullOrEmpty() && ContainsAtLeast(value.label, '\n', 5)) // Due to MGUI BUG
                ".....   Big Text Has Many Lines: {0}".F(text.FirstLine()).PegiLabel().write();
            else
            {
                return editBig(ref text);

            }
            return ChangesToken.False;
        }

        public static ChangesToken write_ForCopy_Big(this TextLabel label, string value, bool showCopyButton = false)
        {
            if (!label.TryGetLabel(out var hint))
                hint = "Unlabeled buffer";

            label.write();

            if (showCopyButton && icon.Copy.Click("Copy text to clipboard"))
                SetCopyPasteBuffer(value, hint);

            nl();

            if (PaintingGameViewUI && !value.IsNullOrEmpty() && ContainsAtLeast(value, '\n', 5)) // Due to MGUI BUG
                ".....   Big Text Has Many Lines: {0}".F(value.FirstLine()).PegiLabel().write();
            else
                return editBig(ref value);

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
        public static TextToken write(System.Exception ex)
        {
            icon.Warning.draw();
            var txt = ex.ToString();
            write_ForCopy(txt.PegiLabel(), showCopyButton: true);
            if ("Log".PegiLabel().Click())
                Debug.LogException(ex);
            nl();
            return TEXT_TOK;
        }

        public static TextToken writeWarning(this TextLabel text)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                PegiEditorOnly.writeHint(text, MessageType.Warning);
                nl();
                //PegiEditorOnly.newLine();
                return TEXT_TOK;
            }
#endif

            checkLine();
            GUILayout.Label(text.label, Styles.WarningText.Current, Utils.GuiMaxWidthOption);
            nl();
            return TEXT_TOK;

        }

        public static TextToken writeHint(this TextLabel text, bool startNewLineAfter = true)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                PegiEditorOnly.writeHint(text, MessageType.Info);
                if (startNewLineAfter)
                    nl();
                return TEXT_TOK;
            }
#endif

            checkLine();
            GUILayout.Label(text.label, Styles.HintText.Current, Utils.GuiMaxWidthOption);
            if (startNewLineAfter)
                nl();

            return TEXT_TOK;
        }

        public static void resetOneTimeHint(string key) => PlayerPrefs.SetInt(key, 0);

        public static void hideOneTimeHint(string key) => PlayerPrefs.SetInt(key, 1);

        public static StateToken writeOneTimeHint(this TextLabel text, string key)
        {

            if (PlayerPrefs.GetInt(key) != 0) return StateToken.False;

            nl();

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                PegiEditorOnly.writeHint(text, MessageType.Info);
            }
            else
#endif
            {
                checkLine();
                GUILayout.Label(text.label, Styles.HintText.Current, Utils.GuiMaxWidthOption);
            }

            if (icon.Done.ClickUnFocus("Got it").nl()) 
                PlayerPrefs.SetInt(key, 1);

            return StateToken.True;
        }

        #endregion

        #region Progress Bar


        public static TextToken drawProgressBar(this TextLabel text, float value)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                PegiEditorOnly.ProgressBar(text, value);
            else
#endif
            {
                checkLine();
                text.label = "{0}: {1}%".F(text, Mathf.FloorToInt(value * 100));
                text.write();
                //GUILayout.Label(cnt, GuiMaxWidthOption);
            }

            return TEXT_TOK;
        }

        #endregion
    }
}
