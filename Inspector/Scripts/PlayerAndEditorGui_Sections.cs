using System;
using System.Collections.Generic;
using QuizCanners.Migration;
using QuizCanners.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0011 // Add braces
#pragma warning disable IDE0008 // Use explicit type

namespace QuizCanners.Inspect
{
    public static partial class pegi
    {

        #region Foldout    

        public static StateToken isFoldout(this TextLabel txt, ref bool state)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.foldout(txt, ref state);
#endif

            checkLine();

            if (ClickUnFocus((state ? "[Hide] {0}..." : ">{0} [Show]").F(txt).PegiLabel()))
                state = !state;


            PegiEditorOnly.isFoldedOutOrEntered = new StateToken(state);

            return PegiEditorOnly.isFoldedOutOrEntered;

        }

        public static StateToken isFoldout(this TextLabel txt, ref int selected, int current)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.foldout(txt, ref selected, current);
#endif

            checkLine();

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

        public static StateToken isFoldout(this icon ico, string text, ref bool state) => ico.GetIcon().isFoldout(text, ref state);

        public static StateToken isFoldout(this TextLabel txt)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.foldout(txt);
#endif

            isFoldout(txt, ref selectedFold, _elementIndex);

            _elementIndex++;

            return PegiEditorOnly.isFoldedOutOrEntered;
        }

        internal static StateToken isFoldout(this Texture2D tex, string text, ref bool state)
        {

            if (state)
            {
                if (icon.FoldedOut.ClickUnFocus(text, 30).IgnoreChanges(LatestInteractionEvent.Exit))
                    state = false;
            }
            else
            {
                if (tex.Click(text).IgnoreChanges(LatestInteractionEvent.Enter))
                    state = true;
            }
            return new StateToken(state);
        }

        internal static void FoldInNow() => selectedFold = -1;
        #endregion

        public static StateToken toggle_Enter (this TextLabel exitLabel, ref bool toggle) 
        {
            exitLabel.toggleIcon(ref toggle, hideTextWhenTrue: true);
            if (toggle)
                return exitLabel.isEntered();

            return StateToken.False;
        }

        #region Enter & Exit

        public static StateToken isEntered(this TextLabel txt, bool showLabelIfTrue = true, Styles.PegiGuiStyle enterLabelStyle = null)
        {
            using (Context.IncrementDisposible(out bool canSkip))
            {
                if (canSkip)
                    return StateToken.False;

                if (Context.IsEnteredCurrent)
                {
                    using (Styles.Background.ExitLabel.SetDisposible())
                    {
                        icon.Exit.ClickUnFocus("{0} {1}".F(icon.Exit.GetText(), txt)).IgnoreChanges(LatestInteractionEvent.Exit).OnChanged(() => Context.IsEnteredCurrent = StateToken.False);
                    }

                    if (showLabelIfTrue)
                    {
                        txt.style = Styles.ExitLabel;
                        if (txt.ClickLabel().IgnoreChanges(LatestInteractionEvent.Exit))
                            Context.IsEnteredCurrent = StateToken.False;
                    }
                }
                else
                {
                    txt.style = enterLabelStyle ?? Styles.EnterLabel;
                    (icon.Enter.ClickUnFocus(txt.label).IgnoreChanges(LatestInteractionEvent.Enter) |
                    txt.ClickLabel().IgnoreChanges(LatestInteractionEvent.Enter)).OnChanged(() => Context.IsEnteredCurrent = StateToken.True);
                }
            }

            return Context.IsEnteredCurrent;
        }
     
        public static StateToken isEntered(this TextLabel txt, ref int entered, int thisOne, bool showLabelIfTrue = true, Styles.PegiGuiStyle enterLabelStyle = null)
        {
            if (!EnterOptionsDrawn_Internal(ref entered, thisOne))
                return StateToken.False;

            var outside = entered == -1;

            var IsCurrent = entered == thisOne;
 
            if (IsCurrent)
            {
                using (Styles.Background.ExitLabel.SetDisposible())
                {
                    if (icon.Exit.ClickUnFocus("{0} {1}".F(icon.Exit.GetText(), txt)).IgnoreChanges(LatestInteractionEvent.Exit))
                        entered = -1;
                }
            }
            else if (outside && icon.Enter.ClickUnFocus(txt.label).IgnoreChanges(LatestInteractionEvent.Enter))
                entered = thisOne;


            if ((showLabelIfTrue && IsCurrent) || outside) 
            {
                txt.style = outside ? enterLabelStyle ?? Styles.EnterLabel : Styles.ExitLabel;
                if (txt.ClickLabel().IgnoreChanges(outside ? LatestInteractionEvent.Enter : LatestInteractionEvent.Exit))
                    entered = outside ? thisOne : -1;
            }


            PegiEditorOnly.isFoldedOutOrEntered = new StateToken(entered == thisOne);

            return PegiEditorOnly.isFoldedOutOrEntered;
        }

        public static StateToken isConditionally_Entered(this TextLabel exitLabel, bool canEnter, bool showLabelIfTrue = true)
        {
            using (Context.IncrementDisposible(out bool canSkip))
            {
                if (canSkip)
                    return StateToken.False;

                if (!canEnter && Context.IsEnteredCurrent)
                    Context.IsEnteredCurrent = StateToken.False;

                Context.Internal_isConditionally_Entered(exitLabel, canEnter: canEnter, showLabelIfTrue: showLabelIfTrue);

                return Context.IsEnteredCurrent;
            }
        }

        public static StateToken isConditionally_Entered(this TextLabel label, bool canEnter, ref bool entered, bool showLabelIfTrue = true)
        {

            if (!canEnter && entered)
            {
                if (icon.Back.Click() | "All Done here".PegiLabel().ClickText(14))
                    entered = false;
            }
            else
            {
                if (canEnter)
                    label.isEntered(ref entered, showLabelIfTrue);
                else
                    PegiEditorOnly.IsFoldedOutOrEntered = false;
            }

            return PegiEditorOnly.isFoldedOutOrEntered;
        }

        public static ChangesToken enter_Inspect(this TextLabel label, IPEGI val)
        {
            using (Context.IncrementDisposible(out bool canSkip))
            {
                if (canSkip)
                    return ChangesToken.False;

                if (val == null)
                    label.label += " (NULL)";

                var change = ChangeTrackStart();

                Context.Internal_isEntered(label);

                if (Context.IsEnteredCurrent)//label.isEntered())
                    val.Nested_Inspect();

                return change;
            }
        }

        public static ChangesToken enter_Inspect(this TextLabel label, IPEGI val, ref int entered, int thisOne)
        {
            if (!EnterOptionsDrawn_Internal(ref entered, thisOne))
                return ChangesToken.False;

            if (val == null) 
                label.label += " (NULL)";

            if (label.isEntered(ref entered, thisOne))
                return val.Nested_Inspect();

            return ChangesToken.False;
        }

        public static ChangesToken enter_Inspect(this IPEGI var)
        {
            using (Context.IncrementDisposible(out bool canSkip))
            {
                if (canSkip)
                    return ChangesToken.False;

                var change = ChangeTrackStart();

                var lst = var as IPEGI_ListInspect;

                if (lst != null)
                    Context.Internal_enter_Inspect_AsList(lst);
                else
                {
                    var label = var.GetNameForInspector().PegiLabel();

                    Context.Internal_isEntered(label);

                    if (Context.IsEnteredCurrent)
                        var.Nested_Inspect();
                }

                return change;
            }
        }

        public static ChangesToken try_enter_Inspect(this TextLabel label, object target, ref int entered, int thisOne) 
        {
            if (!EnterOptionsDrawn_Internal(ref entered, thisOne))
                return ChangesToken.False;

            var lst = target as IPEGI_ListInspect;

            if (lst != null)
            {
                return lst.enter_Inspect_AsList(ref entered, thisOne);
            }

            var IPEGI = target as IPEGI;

            if (IPEGI == null) 
            {
                if (entered == thisOne && icon.Back.Click().IgnoreChanges(LatestInteractionEvent.Exit))
                    entered = -1;

                "{0} : {1}".F(label, (target == null) ? "NULL" : "No IPEGI").PegiLabel().write();

                return ChangesToken.False;
            }

            return target.GetNameForInspector().PegiLabel().enter_Inspect(IPEGI, ref entered, thisOne);
        }

        public static ChangesToken enter_Inspect_AsList(this IPEGI_ListInspect var, string exitLabel = null)
        {
            using (Context.IncrementDisposible(out bool canSkip))
            {
                if (canSkip)
                    return ChangesToken.False;

                var changed = ChangeTrackStart();

                Context.Internal_enter_Inspect_AsList(var, exitLabel);

                return changed;
            }
        }

        public static ChangesToken enter_Inspect_AsList(this IPEGI_ListInspect var, ref int entered, int thisOne, string exitLabel = null)
        {
            if (!EnterOptionsDrawn_Internal(ref entered, thisOne))
                return ChangesToken.False;

            var changed = ChangeTrackStart();

            var outside = entered == -1;

            if (!var.IsNullOrDestroyed_Obj())
            {
                if (outside)
                {
                    var.InspectInList(ref entered, thisOne);
                    new ChangesToken(entered == thisOne).IgnoreChanges(LatestInteractionEvent.Enter);
                }
                else if (entered == thisOne)
                {
                    using (Styles.Background.ExitLabel.SetDisposible())
                    {
                        var label =  new TextLabel(exitLabel.IsNullOrEmpty() ? var.GetNameForInspector() : exitLabel, tooltip: icon.Exit.GetDescription(), style: Styles.ExitLabel);

                        if (icon.Exit.ClickUnFocus("{0} L {1}".F(icon.Exit.GetText(), var)).IgnoreChanges(LatestInteractionEvent.Exit)
                            | label.ClickLabel().IgnoreChanges(LatestInteractionEvent.Exit))
                            entered = -1;
                    }

                    Try_Nested_Inspect(var);
                }
            }
            else if (entered == thisOne)
                entered = -1;

            PegiEditorOnly.IsFoldedOutOrEntered = entered == thisOne;

            return changed;
        }

        #region Internal

        private static bool EnterOptionsDrawn_Internal(ref int entered, int thisOne) => entered == -1 || entered == thisOne;

        private static StateToken isEntered(this icon ico, TextLabel txt, ref bool state, bool showLabelIfTrue = true)
        {

            if (state)
            {
                if (icon.Exit.ClickUnFocus("{0} {1}".F(icon.Exit.GetText(), txt)).IgnoreChanges(LatestInteractionEvent.Exit))
                    state = false;
            }
            else if (ico.ClickUnFocus("{0} {1}".F(icon.Enter.GetText(), txt)).IgnoreChanges(LatestInteractionEvent.Exit))
                state = true;

            txt.style = state ? Styles.ExitLabel : Styles.EnterLabel;

            if ((showLabelIfTrue || !state) &&
                txt.ClickLabel().IgnoreChanges(LatestInteractionEvent.Exit))
                state = !state;

            PegiEditorOnly.isFoldedOutOrEntered = new StateToken(state);

            return PegiEditorOnly.isFoldedOutOrEntered;
        }
        private static StateToken isEntered(this TextLabel txt, ref bool state, bool showLabelIfTrue = true) => icon.Enter.isEntered(txt, ref state, showLabelIfTrue);

        private static StateToken isConditionally_Entered(this TextLabel label, bool canEnter, ref int entered, int thisOne, bool showLabelIfEntered = true, Styles.PegiGuiStyle enterLabelStyle = null)
        {
            if (!EnterOptionsDrawn_Internal(ref entered, thisOne))
                return StateToken.False;

            if (!canEnter && entered == thisOne)
            {
                if (icon.Back.Click() | "All Done here".PegiLabel().ClickText(14))
                    entered = -1;
            }
            else
            {
                if (canEnter)
                    label.isEntered(ref entered, thisOne, showLabelIfEntered, enterLabelStyle);
                else
                    PegiEditorOnly.IsFoldedOutOrEntered = false;
            }

            return PegiEditorOnly.isFoldedOutOrEntered;
        }

        private static StateToken isEntered_ListIcon<T>(this TextLabel txt, List<T> list)
        {
            if (collectionInspector.CollectionIsNull(list))
            {
                if (Context.IsEnteredCurrent)
                    Context.IsEnteredCurrent = StateToken.False;
                return StateToken.False;
            }

            txt.label = txt.label.AddCount(list, entered: Context.IsEnteredCurrent);

            bool ent = Context.IsEnteredCurrent;

            Context.IsEnteredCurrent = icon.List.isEntered(txt, ref ent, showLabelIfTrue: false);

            return Context.IsEnteredCurrent;
        }

        private static TextLabel AddCount<T>(this TextLabel label, ICollection<T> lst, bool entered = false) 
        {
            label.label = label.label.AddCount(lst, entered);

            return label;
        }

        private static string AddCount<T>(this string txt, ICollection<T> lst, bool entered = false)
        {
            if (lst == null)
                return "{0} is NULL".F(txt);

            if (lst.Count > 1)
                return "{0} [{1}]".F(txt, lst.Count);

            if (lst.Count == 0)
                return "NO {0}".F(txt);

            if (!entered)
            {

                var el = lst.GetElementAt(0);

                if (!el.IsNullOrDestroyed_Obj())
                {

                    var nm = el as IGotReadOnlyName;

                    if (nm != null)
                        return "{0}: {1}".F(txt, nm.GetReadOnlyName());

                    var n = el as IGotName;

                    if (n != null)
                        return "{0}: {1}".F(txt, n.NameForInspector);

                    return "{0}: {1}".F(txt, el.GetNameForInspector());

                }

                return "{0} one Null Element".F(txt);
            }

            return "{0} [1]".F(txt);
        }

        private static ChangesToken clickEnter_DirectlyToElement<K,V>(this Dictionary<K,V> dic, ref int inspected)
        {

            if ((inspected == -1 && dic.Count > 1) || dic.Count == 0) return ChangesToken.False;

            int suggestedIndex = Mathf.Max(inspected, 0);

            if (suggestedIndex >= dic.Count)
                suggestedIndex = 0;

            icon ico;
            string msg;

            if (NeedsAttention(dic, out msg))
            {
                if (inspected == -1)
                    suggestedIndex = LastNeedAttentionIndex;

                ico = icon.Warning;
            }
            else
            {
                ico = icon.Next;
                msg = "->";
            }

            var el = dic.TryGetByElementIndex(suggestedIndex);// as IPEGI;

            if (ico.Click(msg + el.GetNameForInspector()).IgnoreChanges(LatestInteractionEvent.Enter))
            {
                inspected = suggestedIndex;
                PegiEditorOnly.isFoldedOutOrEntered = StateToken.True;
                return ChangesToken.True;
            }
            return ChangesToken.False;
        }

        private static ChangesToken clickEnter_DirectlyToElement<T>(this List<T> list, ref int inspected)
        {

            if ((inspected == -1 && list.Count > 1) || list.Count == 0) return ChangesToken.False;

            int suggestedIndex = Mathf.Max(inspected, 0);

            if (suggestedIndex >= list.Count)
                suggestedIndex = 0;

            icon ico;
            string msg;

            if (NeedsAttention(list, out msg))
            {
                if (inspected == -1)
                    suggestedIndex = LastNeedAttentionIndex;

                ico = icon.Warning;
            }
            else
            {
                ico = icon.Next;
                msg = "->";
            }

            var el = list.TryGet(suggestedIndex);// as IPEGI;

            if (ico.Click(msg + el.GetNameForInspector()).IgnoreChanges(LatestInteractionEvent.Enter))
            {
                inspected = suggestedIndex;
                PegiEditorOnly.isFoldedOutOrEntered = StateToken.True;
                return ChangesToken.True;
            }
            return ChangesToken.False;
        }

        private static StateToken isEntered_DirectlyToElement<T>(this List<T> list, ref int inspected)
        {

            if (!Context.IsEnteredCurrent && list.clickEnter_DirectlyToElement(ref inspected))
                Context.IsEnteredCurrent = StateToken.True;

            return Context.IsEnteredCurrent;
        }

        private static StateToken isEntered_HeaderPart<T, V>(this CollectionInspectorMeta meta, Dictionary<T, V> dic, bool showLabelIfTrue = false)
        {
            using (Context.IncrementDisposible(out bool canSkip))
            {
                if (canSkip)
                    return StateToken.False;

                var before = Context.IsEnteredCurrent;

                meta.Label.PegiLabel().AddCount(dic, Context.IsEnteredCurrent).isEntered(showLabelIfTrue, dic.Count == 0 ? Styles.ClippingText : null);

                if (Context.IsEnteredCurrent && !before)
                    meta.inspectedElement = -1;

                if (Context.IsEnteredCurrent == false)
                    dic.clickEnter_DirectlyToElement(ref meta.inspectedElement).OnChanged(() => Context.IsEnteredCurrent = StateToken.True);

                return Context.IsEnteredCurrent;
            }
        }

        private static StateToken isEntered_HeaderPart<T>(this CollectionInspectorMeta meta, List<T> list, bool showLabelIfTrue = false)
        {
            var before = Context.IsEnteredCurrent;

            Context.Internal_isEntered(meta.Label.AddCount(list, Context.IsEnteredCurrent).PegiLabel(), showLabelIfTrue);

            if (!Context.IsEnteredCurrent && before)
                meta.inspectedElement = -1;

            list.isEntered_DirectlyToElement(ref meta.inspectedElement);

            return Context.IsEnteredCurrent;
        }

        #endregion

        #region List 

        public static ChangesToken enter_List<T>(this CollectionInspectorMeta meta, List<T> list)
        {
            using (Context.IncrementDisposible(out bool canSkip))
            {
                if (canSkip)
                    return ChangesToken.False;

                if (meta.isEntered_HeaderPart(list))
                    return meta.edit_List(list).nl();

                return ChangesToken.False;
            }
        }

        public static ChangesToken enter_List_UObj<T>(this TextLabel label, List<T> list) where T : Object
        {
            using (Context.IncrementDisposible(out var canSKip))
            {
                if (canSKip)
                    return ChangesToken.False;

                if (isEntered_ListIcon(label, list))
                    return label.edit_List_UObj(list).nl();

                return ChangesToken.False;
            }
        }

        public static ChangesToken enter_List<T>(this TextLabel label, List<T> list)
        {
            int _inspected = -1;
            return label.enter_List(list, ref _inspected, out _);
        }

        public static ChangesToken enter_List<T>(this TextLabel label, List<T> list, ref int inspected) => label.enter_List(list, ref inspected, out _);
        
        public static ChangesToken enter_List<T>(this TextLabel label, List<T> list, ref int inspectedElement, out T added)
        {
            added = default(T);

            using (Context.IncrementDisposible(out bool canSkip))
            {
                if (canSkip)
                    return ChangesToken.False;

                var changes = ChangeTrackStart();

                Context.Internal_isEntered_ListIcon(label, list, ref inspectedElement);
                if (Context.IsEnteredCurrent)
                    label.edit_List(list, ref inspectedElement, out added);

                return changes;
            }
        }

        #endregion

        public static ChangesToken enter_Dictionary<TKey, TValue>(this CollectionInspectorMeta meta, Dictionary<TKey, TValue> list)
        {
            if (meta.isEntered_HeaderPart(list))
                return meta.edit_Dictionary(list).nl();
            return ChangesToken.False;
        }

        #endregion

        #region Line

        public static void line() => line(PaintingGameViewUI ? Color.white : Color.black);

        public static void line(Color col)
        {
            nl();

            var c = GUI.color;
            GUI.color = col;
            if (PaintingGameViewUI)
                GUILayout.Box(GUIContent.none, Styles.HorizontalLine.Current, Utils.GuiMaxWidthOption);
            else
                GUILayout.Box(GUIContent.none, Styles.HorizontalLine.Current);

            GUI.color = c;
        }

        #endregion
    }
}