﻿using QuizCanners.Utils;
using System;
using UnityEngine;

namespace QuizCanners.Inspect
{

    public static partial class pegi
    {
        public static class Styles
        {
            public static bool InList;

            private static bool InGameView => PaintingGameViewUI
#if UNITY_EDITOR
            || UnityEditor.EditorGUIUtility.isProSkin
#endif
        ;

            public static Color listReadabilityRed = new(1, 0.85f, 0.85f, 1);
            public static Color listReadabilityBlue = new(0.9f, 0.9f, 1f, 1);

            public delegate GUIStyle CreateGUI();

            public class PegiGuiStyle : IPEGI
            {
                private GUIStyle editorGui;
                private GUIStyle playtime;
                private GUIStyle editorGuiInList;
                private GUIStyle playtimeInList;

                private readonly CreateGUI generator;

                public GUIStyle Current
                {
                    get
                    {
                        if (InGameView)
                        {
                            if (InList)
                            {
                                playtimeInList ??= generator();
                                return playtimeInList;
                            }

                            playtime ??= generator();

                            return playtime;
                        }

                        if (InList)
                        {
                            editorGuiInList ??= generator();

                            return editorGuiInList;
                        }

                        editorGui ??= generator();

                        return editorGui;
                    }
                }

                public PegiGuiStyle(CreateGUI generator)
                {
                    this.generator = generator;
                }

                #region Inspector

                private readonly EnterExitContext _inspectedProperty = new();

                void IPEGI.Inspect()
                {
                    using (_inspectedProperty.StartContext())
                    {

                        var cur = Current;

                        var al = cur.alignment;

                        if ("Allignment".ConstLabel().Edit_Enum(ref al).Nl())
                            cur.alignment = al;

                        var fs = cur.fontSize;
                        if ("Font Size".ConstLabel().Edit(ref fs).Nl())
                            cur.fontSize = fs;

                        if ("Padding".PegiLabel().IsFoldout().Nl())
                        {
                            RectOffset pad = cur.padding;

                            if (Edit(ref pad, -15, 15).Nl())
                                cur.padding = pad;
                        }

                        if ("Margins".PegiLabel().IsFoldout().Nl())
                        {
                            RectOffset mar = cur.margin;

                            if (Edit(ref mar, -15, 15).Nl())
                                cur.margin = mar;
                        }
                    }
                }

                #endregion
            }

            #region Button

            public static PegiGuiStyle ImageButton = new(() => new GUIStyle(GUI.skin.button)
            {
                overflow = new RectOffset(-3, -3, 0, 0),
                margin = new RectOffset(1, -3, 1, 1)
            });

            public static PegiGuiStyle ClickableText = new(() => new GUIStyle(GUI.skin.label)
            {
                wordWrap = false,
                fontStyle = FontStyle.Bold,
                contentOffset = new Vector2(0, 4),
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = InGameView ? new Color32(220, 220, 255, 255) : new Color32(40, 40, 40, 255) }
            });

            public static PegiGuiStyle ScalableBlueText(int fontSize)
            {
                _scalableBlueText.Current.fontSize = fontSize;
                return _scalableBlueText;
            }

            private static readonly PegiGuiStyle _scalableBlueText = new(() => new GUIStyle(GUI.skin.label)
            {
                wordWrap = false,
                fontStyle = FontStyle.Bold,
                normal = { textColor = InGameView ? new Color32(120, 120, 255, 255) : new Color32(40, 40, 255, 255) },
                margin = new RectOffset(0, 0, 0, -15),
                fontSize = 14
            });

            #endregion

            #region Toggle

            public static PegiGuiStyle ToggleButton = new(() => new GUIStyle(GUI.skin.button)
            {
                overflow = new RectOffset(-3, -3, 0, 0),
                margin = new RectOffset(-13, -13, -10, -10),
                contentOffset = new Vector2(0, 6)
            });

            private static readonly PegiGuiStyle ToggleLabel_Off = new(() => new GUIStyle(GUI.skin.label)
            {
                contentOffset = new Vector2(0, 2),
                wordWrap = true,
                normal = { textColor = InGameView ? new Color32(255, 255, 255, 255) : new Color32(40, 40, 40, 255) }
            });

            private static readonly PegiGuiStyle ToggleLabel_On = new(() => new GUIStyle(GUI.skin.label)
            {
                contentOffset = new Vector2(0, 2),
                wordWrap = true
            });

            public static PegiGuiStyle ToggleLabel(bool isOn) => isOn ? ToggleLabel_On : ToggleLabel_Off;

            #endregion

            #region List

            public static PegiGuiStyle ListLabel = new(() => new GUIStyle(GUI.skin.label)
            {
                margin = new RectOffset(9, 1, 6, 1),
                fontSize = 12,
                clipping = TextClipping.Clip,
                richText = true,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal =
            {
                textColor = InGameView ? new Color32(255, 255, 255, 255) : new Color32(43, 30, 11, 255)
            }
            });

            #endregion

            #region Fold / Enter / Exit

            public static PegiGuiStyle EnterLabel = new(() => new GUIStyle
            {
                padding = InGameView ? new RectOffset(0, 0, 4, 7) : new RectOffset(10, 10, 10, 0),
                margin = InGameView ? new RectOffset(9, 0, 3, 3) : new RectOffset(9, 0, 0, 0),
                fontSize = InGameView ? 14 : 12,
                richText = true,
                wordWrap = false,
                clipping = TextClipping.Overflow,
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Bold,
                contentOffset = InGameView ? new Vector2(0, 0) : new Vector2(0, -6),
                normal = { textColor = InGameView ? new Color32(255, 255, 220, 255) : new Color32(43, 30, 77, 255) }
            });

            public static PegiGuiStyle ExitLabel = new(() => new GUIStyle
            {
                padding = InGameView ? new RectOffset(0, 0, 4, 7) : new RectOffset(10, 10, 10, 0),
                margin = InGameView ? new RectOffset(9, 0, 3, 3) : new RectOffset(9, 0, 0, 0),
                fontSize = 13,
                richText = true,
                wordWrap = false,
                clipping = TextClipping.Clip,
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Italic,
                contentOffset = InGameView ? new Vector2(0, 0) : new Vector2(0, -6),
                normal = { textColor = InGameView ? new Color32(160, 160, 160, 255) : new Color32(77, 77, 77, 255) }
            });

            public static PegiGuiStyle FoldedOutLabel = new(() => new GUIStyle
            {
                margin = new RectOffset(40, 10, 10, 10),
                fontSize = 12,
                richText = true,
                wordWrap = false,
                clipping = TextClipping.Clip,
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Bold,
                imagePosition = ImagePosition.ImageLeft,
                normal = { textColor = InGameView ? new Color32(200, 220, 220, 255) : new Color32(43, 77, 33, 255) }
            });

            #endregion

            #region Text

            public static PegiGuiStyle HeaderText = new(() => new GUIStyle(GUI.skin.label)
            {
                margin = new RectOffset(9, 1, 6, 1),
                fontSize = 16,
                clipping = TextClipping.Clip,
                richText = true,
                wordWrap = false,
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Bold,
                normal =
            {
                textColor = InGameView ? new Color32(255, 128, 128, 255) : new Color32(255, 0, 0, 255)
            }
            });

            public static PegiGuiStyle BaldText = new(() =>
               InList
                   ? ToGrayBg(new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold })
                   : new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });

            public static PegiGuiStyle ClippingText = new(() =>
                InList
                    ? ToGrayBg(new GUIStyle(GUI.skin.label) { clipping = TextClipping.Clip })
                    : new GUIStyle(GUI.skin.label) { clipping = TextClipping.Clip });


            public static PegiGuiStyle OverflowText = new(() => new GUIStyle(GUI.skin.label)
            {
                clipping = TextClipping.Overflow,
                wordWrap = true,
                fontSize = 12
            });

            public static PegiGuiStyle HintText = new(() => new GUIStyle(GUI.skin.label)
            {
                clipping = TextClipping.Overflow,
                wordWrap = true,
                fontSize = 10,
                fontStyle = FontStyle.Italic,
                normal =
            {
                textColor = InGameView ? new Color32(192, 192, 100, 255) : new Color32(64, 64, 11, 255)
            }
            });

            public static PegiGuiStyle WarningText = new(() => new GUIStyle(GUI.skin.label)
            {
                clipping = TextClipping.Overflow,
                wordWrap = true,
                fontSize = 13,
                fontStyle = FontStyle.BoldAndItalic,
                normal =
            {
                textColor = InGameView ? new Color32(255, 20, 20, 255) : new Color32(255, 64, 64, 255)
            }
            });

            #endregion

            #region Line

            public static PegiGuiStyle HorizontalLine = new(() => new GUIStyle
            {
#if UNITY_EDITOR
                normal = { background = UnityEditor.EditorGUIUtility.whiteTexture },
#endif
                margin = new RectOffset(0, 0, 4, 4),
                fixedHeight = 1
            });

            #endregion

            public static class Background
            {
                public static BackgroundStyle ExitLabel = new(new Color(0.4f, 0.4f, 0.4f, 0.3f));
                public static BackgroundStyle List = new(new Color(0.1f, 0.1f, 0.3f, 0.1f));


                public class BackgroundStyle
                {
                    private Color _color;
                    private readonly GUIStyle style = new();
                    private Texture2D texture; // = new Texture2D(1, 1);


                    internal GUIStyle Get()
                    {
                        if (!texture)
                        {
                            texture = new Texture2D(1, 1);
                            texture.SetPixel(0, 0, _color);
                            texture.Apply();
                            style.normal.background = texture;
                        }

                        return style;
                    }

                    public IDisposable SetDisposible() =>
                        QcSharp.SetTemporaryValueDisposable(this, bg => PegiEditorOnly.nextBgStyle = bg, () => PegiEditorOnly.nextBgStyle);


                    public BackgroundStyle(Color color)
                    {
                        _color = color;
                    }
                }


            }


            // Todo: Only give texture with BG for Lists
            private static GUIStyle ToGrayBg(GUIStyle style)
            {
#if UNITY_2020_1_OR_NEWER
                if (InList && !InGameView)
                    style.normal.background = Texture2D.linearGrayTexture;
#endif
                return style;
            }


            private static int _inspectedFont = -1;
            private static int _iteratiedFont;

            private static void InspectInteranl(string StyleName, PegiGuiStyle style)
            {
                if (StyleName.PegiLabel().IsEntered(ref _inspectedFont, _iteratiedFont).Nl())
                {
                    "Example text in {0} style ".F(StyleName).PegiLabel(style: style).Nl();
                    style.Nested_Inspect().Nl();
                }

                _iteratiedFont++;
            }

            public static pegi.ChangesToken Inspect()
            {
                var changed = pegi.ChangeTrackStart();

                _iteratiedFont = 0;

                InspectInteranl("Clipping Text", ClippingText);

                InspectInteranl("Overfloaw text", OverflowText);

                InspectInteranl("Text Button", ClickableText);

                InspectInteranl("Enter Label", EnterLabel);

                InspectInteranl("Exit Label", ExitLabel);

                InspectInteranl("Hint Text", HintText);

                InspectInteranl("Warning Text", WarningText);

                return changed;
            }
        }
    }
}