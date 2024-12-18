﻿using System;
using System.Collections.Generic;
using QuizCanners.Utils;
using UnityEngine;

namespace QuizCanners.Inspect
{

    #region interfaces & Attributes

    public interface IPEGI { void Inspect(); }

    public interface IPEGI_ListInspect { void InspectInList(ref int edited, int index); }

    public interface IPEGI_Context { void InspectContext(pegi.EnterExitContext context); }

    public interface IGotStringId { string StringId { get; set; } }

    public interface IGotIndex { int IndexForInspector { get; set; } }

    public interface IGotCount { int GetCount(); }

    public interface IPEGI_Reference { object GetReferencedObject(); }

    public interface ISearchable { System.Collections.IEnumerator SearchKeywordsEnumerator(); }

    public interface INeedAttention { string NeedAttention(); }

    public interface IInspectorDropdown { bool ShowInInspectorDropdown(); }

    public interface IPEGI_Preview 
    {
        Texture GetPreview();
    }

    #endregion

    public static partial class pegi
    {
        private const int PLAYTIME_GUI_WIDTH = 400;

        private static bool _globChanged = false;

        internal static bool GlobChanged 
        {
            get => _globChanged;
            set 
            {
                // Debug Inspector
                
              //  if (value) // && !_globChanged)
                 //   Debug.Log("Glob Changed to true. Was changed: {0}".F(_globChanged));

                _globChanged = value;
            }
        }
        
        // Some times user can change temporary fields, like delayed Edits

        internal static bool _horizontalStarted;
        private static readonly Color AttentionColor = new(1f, 0.7f, 0.7f, 1);
        private static readonly Color PreviousInspectedColor = new(0.3f, 0.7f, 0.3f, 1);
        private static readonly List<Color> _previousBgColors = new();
        private static readonly List<Color> _previousGuiColors = new();

        public static UnityEngine.Object InspectedUnityObject =>
        #if UNITY_EDITOR
            inspectedUnityObject;
        #else
            null;
        #endif
        public static bool IsFoldedOut => FoldoutManager.isFoldedOutOrEntered;
        public static string EnvironmentNl => Environment.NewLine;

        internal static UnityEngine.Object inspectedUnityObject;
        internal static object inspectedTarget;
        internal static bool InspectorStarted;

        internal static IDisposable StartInspector(object obj, PegiPaintingMode mode)
        {
            if (InspectorStarted)
                Debug.LogError("Inspector was aleady started");

            currentMode = mode;

            InspectorStarted = true;

            _elementIndex = 0;
            _horizontalStarted = false;
            GlobChanged = false;

            inspectedTarget = obj;
            inspectedUnityObject = obj as UnityEngine.Object;
            ResetInspectedChain();

            return QcSharp.DisposableAction(() => EndInspector());
        }

        internal static List<Styles.Background.BackgroundStyle> nextBgStyle = new();
        
        private static void EndInspector()
        {
            InspectorStarted = false;

            if (GlobChanged)
            {
#if UNITY_EDITOR
                PegiEditorOnly.OnEndInspector();
#endif
            }
            inspectedTarget = null;
            inspectedUnityObject = null;
            nextBgStyle.Clear();
            Nl();
        }


        #region GUI Modes & Fitting

#if UNITY_EDITOR
        private static ChangesToken EditorOnly_EndChangeCheck()
        {
            var changed = UnityEditor.EditorGUI.EndChangeCheck();
            if (changed)
                GlobChanged = true;

            return new ChangesToken(changed);
        }
#endif


        public static bool PaintingGameViewUI
        {
            get { return currentMode == PegiPaintingMode.GameViewGUI; }
        }

        internal enum PegiPaintingMode
        {
            Nothing,
            EditorInspector,
            GameViewGUI,
            UI_Toolkit
        }

        internal static PegiPaintingMode currentMode = PegiPaintingMode.EditorInspector;

        private static int LetterSizeInPixels => PaintingGameViewUI ? 10 : 9;


        private static int RemainingLength(int otherElements) => PaintingGameViewUI ? PLAYTIME_GUI_WIDTH - otherElements : Screen.width - otherElements;

#endregion

#region Inspection Variables



#region BG Color

        private static bool BgColorReplaced => !_previousBgColors.IsNullOrEmpty();

        public static void SetPreviousBgColor()
        {
            if (BgColorReplaced)
            {
                GUI.backgroundColor = _previousBgColors.RemoveLast();
            }
        }

        public static void SetPreviousGuiColor()
        {
            if (_previousGuiColors.Count>0)
            {
                GUI.color = _previousGuiColors.RemoveLast();
            }
        }

        public static void SetBgColor(Color col)
        {
           _previousBgColors.Add(GUI.backgroundColor);
            GUI.backgroundColor = col;
        }

        public static IDisposable SetGuiColorDisposable(Color col) 
        {
            _previousGuiColors.Add(GUI.color);
            GUI.color = col;
            return QcSharp.DisposableAction(SetPreviousGuiColor);
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

        private static void CheckLine()
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                PegiEditorOnly.CheckLine_Editor();
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


            if (el is not INeedAttention need)
                return false;

            msg = need.NeedAttention();

            return msg != null;
        }

        public static bool NeedsAttention(System.Collections.IList list, out string message, string listName = "list", bool canBeNull = false)
        {
            message = NeedsAttention(list, listName, canBeNull);
            return message != null;
        }

        public static bool NeedsAttention_UObj<T>(IList<T> list, out string message, string listName = "list", bool canBeNull = false) where T : UnityEngine.Object
        {
            message = NeedsAttention_UObj(list, listName, canBeNull);
            return message != null;
        }

        public static string NeedsAttention_UObj<T>(IList<T> list, string listName = "list", bool canBeNull = false) where T : UnityEngine.Object
        {
            string msg = null;
            if (list == null)
                msg = canBeNull ? null : "{0} is Null".F(listName);
            else
            {

                int i = 0;

                foreach (var el in list)
                {
                    if (el)
                    {
                        if (NeedsAttention(el, out msg))
                        {
                            msg = " {0} on {1}:{2}".F(msg, i, el.GetNameForInspector());
                            LastNeedAttentionIndex = i;
                            return msg;
                        }
                    }
                    else if (!canBeNull)
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

        public static void Space()
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                PegiEditorOnly.Space();
            else
#endif

            {
                CheckLine();
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
            public bool Changed => !_wasAlreadyChanged && GlobChanged;
            
            public void Feed(bool isChanged) 
            {
                if (isChanged)
                {
                    _wasAlreadyChanged = false;
                    GlobChanged = true;
                }
            }

            public static implicit operator bool(ChangesTracker me) => me.Changed;

            public static implicit operator ChangesToken(ChangesTracker me) => new(me.Changed);

            internal ChangesTracker()
            {
                _wasAlreadyChanged = GlobChanged;
            }
        }

        public struct StateToken
        {
            internal bool IsEntered;

            public static implicit operator bool(StateToken d) => d.IsEntered;

            internal static StateToken True => new() { IsEntered = true };
            internal static StateToken False => new() { IsEntered = false };

            public StateToken(bool value)
            {
                IsEntered = value;
            }
        }

        public struct ChangesToken 
        {
            private bool IsChanged;
            public UnityEngine.UIElements.CallbackEventHandler Handler;

            public static implicit operator bool(ChangesToken d) => d.IsChanged;

            public static ChangesToken False => new() { IsChanged = false };
            public static ChangesToken True => new() { IsChanged = true };

            public static ChangesToken operator |(ChangesToken a, ChangesToken b) 
            {
                a.IsChanged |= b.IsChanged;
                return a;
            }

            public ChangesToken(bool value) 
            {
                IsChanged = value;
                Handler = null;
            }

            public ChangesToken(UnityEngine.UIElements.CallbackEventHandler handler) 
            {
                IsChanged = false;
                this.Handler = handler;
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
                return new Unindenter(width);
            }
            else
#endif
            {

                List<IDisposable> disposables = new()
                {
                    new GUILayout.HorizontalScope()
                };

                GUILayout.Space(width);
                disposables.Add(new GUILayout.VerticalScope());

                return QcSharp.DisposableAction(() =>
                {
                    foreach (var d in disposables)
                        d.Dispose();
                });
            }
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

        public static void Nl()
        {
            switch (currentMode) 
            {
                case PegiPaintingMode.Nothing: Debug.LogError("Current mode is none"); return;

#if UNITY_EDITOR
                case PegiPaintingMode.EditorInspector:
                    PegiEditorOnly.NewLine_Editor();
                    break;
#endif
                case PegiPaintingMode.GameViewGUI:
                    if (_horizontalStarted)
                    {
                        _horizontalStarted = false;
                        GUILayout.EndHorizontal();
                    }
                    break;
                case PegiPaintingMode.UI_Toolkit:
                    Toolkit.NewLine();
                    break;
                default:
                    Debug.LogError(QcLog.CaseNotImplemented(currentMode));
                    break;
            }
        }

        public static void Nl_ifEntered()
        {
            if (FoldoutManager.isFoldedOutOrEntered)
                Nl();
        }

        public static void Nl_ifNotEntered()
        {
            if (FoldoutManager.isFoldedOutOrEntered == false)
                Nl();
        }

        public static StateToken Nl_ifNotEntered(this StateToken value)
        {
            Nl_ifNotEntered();
            return value;
        }

        public static StateToken Nl_ifEntered(this StateToken value)
        {
            Nl_ifEntered();
            return value;
        }

        public static StateToken If_Entered (this StateToken value, Action action) 
        {
            if (value)
                action?.Invoke();

            return value;
        }

        public static StateToken Nl(this StateToken value)
        {
            Nl();
            return value;
        }

        public static ChangesToken Nl(this ChangesToken value)
        {
            Nl();
            return value;
        }

        public static ChangesToken Nl(this ChangesToken value, Action onChanged)
        {
            if (value)
                onChanged?.Invoke();

            Nl();
            return value;
        }

        public static void Nl(this TextLabel value)
        {
            Write(value);
            Nl();
        }

        public static void Nl(this TextToken value) => Nl();
        
        public static void Nl(this Icon icon, int size = DEFAULT_BUTTON_SIZE)
        {
            icon.Draw(size);
            Nl();
        }

        public static void Nl(this Icon icon, string hint, int size = DEFAULT_BUTTON_SIZE)
        {
            icon.Draw(hint, size);
            Nl();
        }

#endregion

    }
}
