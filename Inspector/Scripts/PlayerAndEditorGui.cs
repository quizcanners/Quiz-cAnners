using System;
using System.Collections.Generic;
using QuizCanners.Utils;
using UnityEngine;


// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0011 // Add braces
#pragma warning disable IDE0008 // Use explicit type

namespace QuizCanners.Inspect
{

    #region interfaces & Attributes

    public interface IPEGI { void Inspect(); }

    public interface IPEGI_ListInspect { void InspectInList(ref int edited, int index); }

    public interface IPEGI_SceneDraw { void DrawHandles(); }

    public interface IGotReadOnlyName { string GetReadOnlyName(); }

    public interface IGotName { string NameForInspector { get; set; } }

    public interface IGotIndex { int IndexForInspector { get; set; } }

    public interface IGotCount { int GetCount(); }

    public interface ISearchable { IEnumerator<object> SearchKeywordsEnumerator(); }

    public interface INeedAttention { string NeedAttention(); }

    public interface IInspectorDropdown { bool ShowInInspectorDropdown(); }

    #endregion

    public static partial class pegi
    {
        private const int PLAYTIME_GUI_WIDTH = 400;

        private static int _elementIndex;
        private static int selectedFold = -1;
        internal static bool _horizontalStarted;
        private static readonly Color AttentionColor = new Color(1f, 0.7f, 0.7f, 1);
        private static readonly Color PreviousInspectedColor = new Color(0.3f, 0.7f, 0.3f, 1);
        private static readonly List<Color> _previousBgColors = new List<Color>();

        public static bool IsFoldedOut => PegiEditorOnly.isFoldedOutOrEntered;
        public static string EnvironmentNl => Environment.NewLine;

     
        #region GUI Modes & Fitting

        public static bool PaintingGameViewUI
        {
            get { return currentMode == PegiPaintingMode.PlayAreaGui; }
            private set { currentMode = value ? PegiPaintingMode.PlayAreaGui : PegiPaintingMode.EditorInspector; }
        }

        private enum PegiPaintingMode
        {
            EditorInspector,
            PlayAreaGui
        }

        private static PegiPaintingMode currentMode = PegiPaintingMode.EditorInspector;

        private static int letterSizeInPixels => PaintingGameViewUI ? 10 : 9;


        private static int RemainingLength(int otherElements) => PaintingGameViewUI ? PLAYTIME_GUI_WIDTH - otherElements : Screen.width - otherElements;

        #endregion

        #region Inspection Variables

        #region GUI Colors
        private static icon GUIColor(this icon icn, Color col)
        {
            SetGUIColor(col);
            return icn;
        }
        private static void SetGUIColor(this Color col)
        {
            GUI.color = col;
        }
        #endregion

        #region BG Color

        private static bool BgColorReplaced => !_previousBgColors.IsNullOrEmpty();

        public static void SetPreviousBgColor()
        {
            if (BgColorReplaced)
            {
                GUI.backgroundColor = _previousBgColors.RemoveLast();
            }
        }

        public static void SetBgColor(Color col)
        {
            _previousBgColors.Add(GUI.backgroundColor);
            GUI.backgroundColor = col;
        }

        public static IDisposable SetBgColorDisposable(Color col)
        {
            SetBgColor(col);
            return QcSharp.DisposableAction(SetPreviousBgColor);
        }

        public static void RestoreBGColor()
        {
            if (BgColorReplaced)
                GUI.backgroundColor = _previousBgColors[0];

            _previousBgColors.Clear();
        }

#endregion

        private static void checkLine()
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                PegiEditorOnly.checkLine_Editor();
            else
#endif
            if (!_horizontalStarted)
            {
                GUILayout.BeginHorizontal();
                _horizontalStarted = true;
            }
        }

        private static int LastNeedAttentionIndex;

        private static bool NeedsAttention(object el, out string msg)
        {
            msg = null;

            var need = el as INeedAttention;

            if (need == null) 
                return false;

            msg = need.NeedAttention();

            return msg != null;
        }

        public static bool NeedsAttention(System.Collections.IList list, out string message, string listName = "list", bool canBeNull = false)
        {
            message = NeedsAttention(list, listName, canBeNull);
            return message != null;
        }

        public static string NeedsAttention(System.Collections.IList list, string listName = "list", bool canBeNull = false)
        {
            string msg = null;
            if (list == null)
                msg = canBeNull ? null : "{0} is Null".F(listName);
            else
            {
                
                int i= 0;
                
                foreach (var el in list)
                {
                    if (!el.IsNullOrDestroyed_Obj())
                    {

                        if (NeedsAttention(el, out msg))
                        {
                            msg = " {0} on {1}:{2}".F(msg, i, el.GetNameForInspector());
                            LastNeedAttentionIndex = i;
                            return msg;
                        }
                    } else if (!canBeNull)
                    {
                        msg = "{0} element in {1} is NULL".F(i, listName);
                        LastNeedAttentionIndex = i;

                        return msg;
                    }
                    
                    i++;
                }
            }

            return msg;
        }

        public static void space()
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                PegiEditorOnly.Space();
            else
#endif

            {
                checkLine();
                GUILayout.Space(10);
            }
        }
        
        #endregion

        #region Focus MGMT

        private static void RepaintEditor()
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                PegiEditorOnly.RepaintEditor();
#endif
        }

        public static void UnFocus()
        {
            #if UNITY_EDITOR
            if (!PaintingGameViewUI)
                UnityEditor.EditorGUI.FocusTextInControl("_");
            else
            #endif
                GUI.FocusControl("_");
        }

        public static void NameNextForFocus(string name) => GUI.SetNextControlName(name);

        public static string FocusedName
        {
            get { return GUI.GetNameOfFocusedControl(); }
            set { GUI.FocusControl(value); }
        }

        public static string FocusedText 
        {
            set 
            {
#if UNITY_EDITOR
                UnityEditor.EditorGUI.FocusTextInControl(value);
#endif
            }
        }

        #endregion




        public class ChangesTracker
        {
            private bool _wasAlreadyChanged;
            public bool Changed
            {
                get => !_wasAlreadyChanged && PegiEditorOnly.globChanged;
                set
                {
                    if (value)
                    {
                        _wasAlreadyChanged = false;
                        PegiEditorOnly.globChanged = true;
                    }

                    if (!value)
                    {
                        _wasAlreadyChanged = true;
                    }
                }
            }

            public static implicit operator bool(ChangesTracker me) => me.Changed;

            public static implicit operator ChangesToken(ChangesTracker me) => new ChangesToken(me.Changed);

            internal ChangesTracker()
            {
                _wasAlreadyChanged = PegiEditorOnly.globChanged;
            }
        }

        public struct StateToken
        {
            internal bool IsEntered;

            public static implicit operator bool(StateToken d) => d.IsEntered;

            internal static StateToken True => new StateToken() { IsEntered = true };
            internal static StateToken False => new StateToken() { IsEntered = false };

            public StateToken(bool value)
            {
                IsEntered = value;
            }
        }

        public struct ChangesToken 
        {
            internal bool IsChanged;

            public static implicit operator bool(ChangesToken d) => d.IsChanged;

            internal static ChangesToken False => new ChangesToken() { IsChanged = false };
            internal static ChangesToken True => new ChangesToken() { IsChanged = true };

            public static ChangesToken operator |(ChangesToken a, ChangesToken b) 
            {
                a.IsChanged |= b.IsChanged;
                return a;
            }

            public ChangesToken(bool value) 
            {
                IsChanged = value;
            }
        }

        #region New Line

        private static int IndentLevel
        {
            get
            {
#if UNITY_EDITOR
                if (!PaintingGameViewUI)
                    return UnityEditor.EditorGUI.indentLevel;
#endif

                return 0;
            }

            set
            {
#if UNITY_EDITOR
                if (!PaintingGameViewUI)
                    UnityEditor.EditorGUI.indentLevel = Mathf.Max(0, value);
#endif
            }
        }

        public static void UnIndent(int width = 1)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                PegiEditorOnly.UnIndent(width);
            }
#endif

        }

        public static IDisposable Indent(int width = 1)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                PegiEditorOnly.Indent(width);
            }
#endif

            return new Unindenter(width);
        }

        private class Unindenter : IDisposable
        {
            public int _indent;

            public void Dispose() =>  UnIndent(_indent);
            
            public Unindenter (int width) 
            {
                _indent = width;
            }
        }

        public static void nl()
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                PegiEditorOnly.newLine_Editor();
                return;
            }
#endif

            if (_horizontalStarted)
            {
                _horizontalStarted = false;
                GUILayout.EndHorizontal();
            }
        }

        public static void nl_ifEntered()
        {
            if (PegiEditorOnly.isFoldedOutOrEntered)
                nl();
        }

        public static void nl_ifNotEntered()
        {
            if (PegiEditorOnly.isFoldedOutOrEntered == false)
                nl();
        }

        public static StateToken nl_ifNotEntered(this StateToken value)
        {
            nl_ifNotEntered();
            return value;
        }

        public static StateToken nl_ifEntered(this StateToken value)
        {
            nl_ifEntered();
            return value;
        }

        public static StateToken If_Entered (this StateToken value, Action action) 
        {
            if (value)
                action?.Invoke();

            return value;
        }

        public static StateToken nl(this StateToken value)
        {
            nl();
            return value;
        }

        public static ChangesToken nl(this ChangesToken value)
        {
            nl();
            return value;
        }

        public static void nl(this IPegiText value)
        {
            write(value);
            nl();
        }

        public static void nl(this icon icon, int size = defaultButtonSize)
        {
            icon.draw(size);
            nl();
        }

        public static void nl(this icon icon, string hint, int size = defaultButtonSize)
        {
            icon.draw(hint, size);
            nl();
        }

#endregion

    }
}
