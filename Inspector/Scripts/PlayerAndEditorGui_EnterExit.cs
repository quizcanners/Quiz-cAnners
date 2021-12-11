using System.Collections.Generic;
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
        public static StateToken Toggle_Enter (this TextLabel exitLabel, ref bool toggle) 
        {
            exitLabel.ToggleIcon(ref toggle, hideTextWhenTrue: true);
            if (toggle)
                return exitLabel.IsEntered();

            return StateToken.False;
        }

#       region Enter & Exit

        public static StateToken IsEntered(this TextLabel label, bool showLabelIfTrue = true, Styles.PegiGuiStyle enterLabelStyle = null)
        {
            using (Context.IncrementDisposible(out bool canSkip))
            {
                if (canSkip)
                    return StateToken.False;

                if (Context.IsEnteredCurrent)
                {
                    using (Styles.Background.ExitLabel.SetDisposible())
                    {
                        Icon.Exit.ClickUnFocus("{0} {1}".F(Icon.Exit.GetText(), label)).IgnoreChanges(LatestInteractionEvent.Exit).OnChanged(() => Context.IsEnteredCurrent = StateToken.False);
                    }

                    if (showLabelIfTrue)
                    {
                        label.style = Styles.ExitLabel;
                        if (label.ClickLabel().IgnoreChanges(LatestInteractionEvent.Exit))
                            Context.IsEnteredCurrent = StateToken.False;
                    }
                }
                else
                {
                    label.style = enterLabelStyle ?? Styles.EnterLabel;
                    (Icon.Enter.ClickUnFocus(label.label).IgnoreChanges(LatestInteractionEvent.Enter) |
                    label.ClickLabel().IgnoreChanges(LatestInteractionEvent.Enter)).OnChanged(() => Context.IsEnteredCurrent = StateToken.True);
                }
            }

            return Context.IsEnteredCurrent;
        }
     
        public static StateToken IsEntered(this TextLabel txt, ref int entered, int thisOne, bool showLabelIfTrue = true, Styles.PegiGuiStyle enterLabelStyle = null)
        {
            if (!EnterInternal.OptionsDrawn(ref entered, thisOne))
                return StateToken.False;

            var outside = entered == -1;

            var IsCurrent = entered == thisOne;
 
            if (IsCurrent)
            {
                using (Styles.Background.ExitLabel.SetDisposible())
                {
                    if (Icon.Exit.ClickUnFocus("{0} {1}".F(Icon.Exit.GetText(), txt)).IgnoreChanges(LatestInteractionEvent.Exit))
                        entered = -1;
                }
            }
            else if (outside && Icon.Enter.ClickUnFocus(txt.label).IgnoreChanges(LatestInteractionEvent.Enter))
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

        public static StateToken IsConditionally_Entered(this TextLabel exitLabel, bool canEnter, bool showLabelIfTrue = true)
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

        public static StateToken IsConditionally_Entered(this TextLabel label, bool canEnter, ref bool entered, bool showLabelIfTrue = true)
        {

            if (!canEnter && entered)
            {
                if (Icon.Back.Click() | "All Done here".PegiLabel().ClickText(14))
                    entered = false;
            }
            else
            {
                if (canEnter)
                    EnterInternal.IsEntered(label, ref entered, showLabelIfTrue);
                else
                    PegiEditorOnly.IsFoldedOutOrEntered = false;
            }

            return PegiEditorOnly.isFoldedOutOrEntered;
        }

        public static ChangesToken Enter_Inspect(this TextLabel label, IPEGI val)
        {
            using (Context.IncrementDisposible(out bool canSkip))
            {
                if (canSkip)
                    return ChangesToken.False;

                if (val == null)
                    label.label = "NULL ({0})".F(label.label);
  
                var change = ChangeTrackStart();

                Context.Internal_isEntered(label);

                if (Context.IsEnteredCurrent)//label.isEntered())
                    val.Nested_Inspect();

                return change;
            }
        }

        public static ChangesToken Enter_Inspect(this TextLabel label, IPEGI val, ref int entered, int thisOne)
        {
            if (!EnterInternal.OptionsDrawn(ref entered, thisOne))
                return ChangesToken.False;

            if (val == null) 
                label.label += " (NULL)";

            if (label.IsEntered(ref entered, thisOne))
                return val.Nested_Inspect();

            return ChangesToken.False;
        }

        public static ChangesToken Enter_Inspect<T>(this T var) where T : class, IPEGI
        {
            using (Context.IncrementDisposible(out bool canSkip))
            {
                if (canSkip)
                    return ChangesToken.False;

                var change = ChangeTrackStart();

                var lst = var as IPEGI_ListInspect;

                if (lst != null)
                    Context.Internal_Enter_Inspect_AsList(lst);
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

        public static ChangesToken Try_enter_Inspect(this TextLabel label, object target, ref int entered, int thisOne) 
        {
            if (!EnterInternal.OptionsDrawn(ref entered, thisOne))
                return ChangesToken.False;

            var lst = target as IPEGI_ListInspect;

            if (lst != null)
            {
                return lst.Enter_Inspect_AsList(ref entered, thisOne);
            }

            var IPEGI = target as IPEGI;

            if (IPEGI == null) 
            {
                if (entered == thisOne && Icon.Back.Click().IgnoreChanges(LatestInteractionEvent.Exit))
                    entered = -1;

                "{0} : {1}".F(label, (target == null) ? "NULL" : "No IPEGI").PegiLabel().Write();

                return ChangesToken.False;
            }

            return target.GetNameForInspector().PegiLabel().Enter_Inspect(IPEGI, ref entered, thisOne);
        }

        public static ChangesToken Enter_Inspect_AsList(this IPEGI_ListInspect var, string exitLabel = null)
        {
            using (Context.IncrementDisposible(out bool canSkip))
            {
                if (canSkip)
                    return ChangesToken.False;

                var changed = ChangeTrackStart();

                Context.Internal_Enter_Inspect_AsList(var, exitLabel);

                return changed;
            }
        }

        public static ChangesToken Enter_Inspect_AsList(this IPEGI_ListInspect var, ref int entered, int thisOne, string exitLabel = null)
        {
            if (!EnterInternal.OptionsDrawn(ref entered, thisOne))
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
                        var label =  new TextLabel(exitLabel.IsNullOrEmpty() ? var.GetNameForInspector() : exitLabel, tooltip: Icon.Exit.GetDescription(), style: Styles.ExitLabel);

                        if (Icon.Exit.ClickUnFocus("{0} L {1}".F(Icon.Exit.GetText(), var)).IgnoreChanges(LatestInteractionEvent.Exit)
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

#       region Internal

        private static class EnterInternal 
        {
            public static bool OptionsDrawn(ref int entered, int thisOne) => entered == -1 || entered == thisOne;

            public static StateToken IsConditionally_Entered(TextLabel label, bool canEnter, ref int entered, int thisOne, bool showLabelIfEntered = true, Styles.PegiGuiStyle enterLabelStyle = null)
            {
                if (!OptionsDrawn(ref entered, thisOne))
                    return StateToken.False;

                if (!canEnter && entered == thisOne)
                {
                    if (Icon.Back.Click() | "All Done here".PegiLabel().ClickText(14))
                        entered = -1;
                }
                else
                {
                    if (canEnter)
                        label.IsEntered(ref entered, thisOne, showLabelIfEntered, enterLabelStyle);
                    else
                        PegiEditorOnly.IsFoldedOutOrEntered = false;
                }

                return PegiEditorOnly.isFoldedOutOrEntered;
            }


            public static StateToken IsEntered(Icon ico, TextLabel txt, ref bool state, bool showLabelIfTrue = true)
            {

                if (state)
                {
                    if (Icon.Exit.ClickUnFocus("{0} {1}".F(Icon.Exit.GetText(), txt)).IgnoreChanges(LatestInteractionEvent.Exit))
                        state = false;
                }
                else if (ico.ClickUnFocus("{0} {1}".F(Icon.Enter.GetText(), txt)).IgnoreChanges(LatestInteractionEvent.Exit))
                    state = true;

                txt.style = state ? Styles.ExitLabel : Styles.EnterLabel;

                if ((showLabelIfTrue || !state) &&
                    txt.ClickLabel().IgnoreChanges(LatestInteractionEvent.Exit))
                    state = !state;

                PegiEditorOnly.isFoldedOutOrEntered = new StateToken(state);

                return PegiEditorOnly.isFoldedOutOrEntered;
            }
            public static StateToken IsEntered(TextLabel txt, ref bool state, bool showLabelIfTrue = true) => IsEntered(Icon.Enter, txt, ref state, showLabelIfTrue);

            public static ChangesToken ClickEnter_DirectlyToElement_Internal<K, V>(Dictionary<K, V> dic, ref int inspected)
            {

                if ((inspected == -1 && dic.Count > 1) || dic.Count == 0) return ChangesToken.False;

                int suggestedIndex = Mathf.Max(inspected, 0);

                if (suggestedIndex >= dic.Count)
                    suggestedIndex = 0;

                Icon ico;
                string msg;

                if (NeedsAttention(dic, out msg))
                {
                    if (inspected == -1)
                        suggestedIndex = LastNeedAttentionIndex;

                    ico = Icon.Warning;
                }
                else
                {
                    ico = Icon.Next;
                    msg = "->";
                }

                var el = dic.TryGetElementByIndex(suggestedIndex);// as IPEGI;

                if (ico.Click(msg + el.GetNameForInspector()).IgnoreChanges(LatestInteractionEvent.Enter))
                {
                    inspected = suggestedIndex;
                    PegiEditorOnly.isFoldedOutOrEntered = StateToken.True;
                    return ChangesToken.True;
                }
                return ChangesToken.False;
            }

            public static ChangesToken ClickEnter_DirectlyToElement_Internal<T>(List<T> list, ref int inspected)
            {

                if ((inspected == -1 && list.Count > 1) || list.Count == 0) return ChangesToken.False;

                int suggestedIndex = Mathf.Max(inspected, 0);

                if (suggestedIndex >= list.Count)
                    suggestedIndex = 0;

                Icon ico;
                string msg;

                if (NeedsAttention(list, out msg))
                {
                    if (inspected == -1)
                        suggestedIndex = LastNeedAttentionIndex;

                    ico = Icon.Warning;
                }
                else
                {
                    ico = Icon.Next;
                    msg = "->";
                }

                var el = list.TryGet(suggestedIndex);

                if (ico.Click(msg + el.GetNameForInspector()).IgnoreChanges(LatestInteractionEvent.Enter))
                {
                    inspected = suggestedIndex;
                    PegiEditorOnly.isFoldedOutOrEntered = StateToken.True;
                    return ChangesToken.True;
                }
                return ChangesToken.False;
            }

            public static ChangesToken ClickEnter_DirectlyToElement_Internal<T>(T[] list, ref int inspected)
            {

                if ((inspected == -1 && list.Length > 1) || list.Length == 0)
                    return ChangesToken.False;

                int suggestedIndex = Mathf.Max(inspected, 0);

                if (suggestedIndex >= list.Length)
                    suggestedIndex = 0;

                Icon ico;
                string msg;

                if (NeedsAttention(list, out msg))
                {
                    if (inspected == -1)
                        suggestedIndex = LastNeedAttentionIndex;

                    ico = Icon.Warning;
                }
                else
                {
                    ico = Icon.Next;
                    msg = "->";
                }

                var el = list.TryGet(suggestedIndex);

                if (ico.Click(msg + el.GetNameForInspector()).IgnoreChanges(LatestInteractionEvent.Enter))
                {
                    inspected = suggestedIndex;
                    PegiEditorOnly.isFoldedOutOrEntered = StateToken.True;
                    return ChangesToken.True;
                }
                return ChangesToken.False;
            }


            public static StateToken IsEntered_DirectlyToElement<T>(List<T> list, ref int inspected)
            {

                if (!Context.IsEnteredCurrent && ClickEnter_DirectlyToElement_Internal(list, ref inspected))
                    Context.IsEnteredCurrent = StateToken.True;

                return Context.IsEnteredCurrent;
            }

            public static StateToken IsEntered_DirectlyToElement<T>(T[] array, ref int inspected)
            {

                if (!Context.IsEnteredCurrent && ClickEnter_DirectlyToElement_Internal(array,ref inspected))
                    Context.IsEnteredCurrent = StateToken.True;

                return Context.IsEnteredCurrent;
            }

            public static StateToken isEntered_HeaderPart<T, V>(CollectionInspectorMeta meta, Dictionary<T, V> dic, bool showLabelIfTrue = false)
            {
                using (Context.IncrementDisposible(out bool canSkip))
                {
                    if (canSkip)
                        return StateToken.False;

                    var before = Context.IsEnteredCurrent;

                    meta.Label.PegiLabel().AddCount(dic, Context.IsEnteredCurrent).IsEntered(showLabelIfTrue, dic.Count == 0 ? Styles.ClippingText : null);

                    if (Context.IsEnteredCurrent && !before)
                        meta.InspectedElement = -1;

                    if (Context.IsEnteredCurrent == false)
                        ClickEnter_DirectlyToElement_Internal(dic, ref meta.inspectedElement_Internal).OnChanged(() => Context.IsEnteredCurrent = StateToken.True);

                    return Context.IsEnteredCurrent;
                }
            }

            public static StateToken isEntered_HeaderPart<T>(CollectionInspectorMeta meta, List<T> list, bool showLabelIfTrue = false)
            {
                var before = Context.IsEnteredCurrent;

                Context.Internal_isEntered(meta.Label.AddCount(list, Context.IsEnteredCurrent).PegiLabel(), showLabelIfTrue);

                if (!Context.IsEnteredCurrent && before)
                    meta.InspectedElement = -1;

                EnterInternal.IsEntered_DirectlyToElement(list, ref meta.inspectedElement_Internal);

                return Context.IsEnteredCurrent;
            }

            public static StateToken isEntered_HeaderPart<T>(CollectionInspectorMeta meta, T[] array, bool showLabelIfTrue = false)
            {
                var before = Context.IsEnteredCurrent;

                Context.Internal_isEntered(meta.Label.AddCount(array, Context.IsEnteredCurrent).PegiLabel(), showLabelIfTrue);

                if (!Context.IsEnteredCurrent && before)
                    meta.InspectedElement = -1;

                EnterInternal.IsEntered_DirectlyToElement(array, ref meta.inspectedElement_Internal);

                return Context.IsEnteredCurrent;
            }


        }




        private static StateToken IsEntered_ListIcon<T>(this TextLabel txt, List<T> list)
        {
            if (collectionInspector.CollectionIsNull(list))
            {
                if (Context.IsEnteredCurrent)
                    Context.IsEnteredCurrent = StateToken.False;
                return StateToken.False;
            }

            txt.label = txt.label.AddCount(list, entered: Context.IsEnteredCurrent);

            bool ent = Context.IsEnteredCurrent;

            Context.IsEnteredCurrent = EnterInternal.IsEntered(Icon.List, txt, ref ent, showLabelIfTrue: false);

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


        #endregion

        #region List 

        public static ChangesToken Enter_List<T>(this CollectionInspectorMeta meta, List<T> list)
        {
            using (Context.IncrementDisposible(out bool canSkip))
            {
                if (canSkip)
                    return ChangesToken.False;

                if (EnterInternal.isEntered_HeaderPart(meta, list))
                    return meta.Edit_List(list).Nl();

                return ChangesToken.False;
            }
        }

        public static ChangesToken Enter_List_UObj<T>(this TextLabel label, List<T> list) where T : Object
        {
            using (Context.IncrementDisposible(out var canSKip))
            {
                if (canSKip)
                    return ChangesToken.False;

                if (IsEntered_ListIcon(label, list))
                    return label.Edit_List_UObj(list).Nl();

                return ChangesToken.False;
            }
        }

        public static ChangesToken Enter_List<T>(this TextLabel label, List<T> list)
        {
            int _inspected = -1;
            return label.Enter_List(list, ref _inspected, out _);
        }

        public static ChangesToken Enter_List<T>(this TextLabel label, List<T> list, ref int inspected) => label.Enter_List(list, ref inspected, out _);
        
        public static ChangesToken Enter_List<T>(this TextLabel label, List<T> list, ref int inspectedElement, out T added)
        {
            added = default(T);

            using (Context.IncrementDisposible(out bool canSkip))
            {
                if (canSkip)
                    return ChangesToken.False;

                var changes = ChangeTrackStart();

                Context.Internal_isEntered_ListIcon(label, list, ref inspectedElement);
                if (Context.IsEnteredCurrent)
                    label.Edit_List(list, ref inspectedElement, out added);

                return changes;
            }
        }

        #endregion

        #region Array

        public static ChangesToken Enter_Array<T>(this CollectionInspectorMeta meta, ref T[] array)
        {
            using (Context.IncrementDisposible(out bool canSkip))
            {
                if (canSkip)
                    return ChangesToken.False;

                if (EnterInternal.isEntered_HeaderPart(meta, array))
                    return meta.Edit_Array(ref array).Nl();

                return ChangesToken.False;
            }
        }


        #endregion

        #region Dictionary
        public static ChangesToken Enter_Dictionary<TKey, TValue>(this CollectionInspectorMeta meta, Dictionary<TKey, TValue> list)
        {
            if (EnterInternal.isEntered_HeaderPart(meta, list))
                return meta.Edit_Dictionary(list).Nl();
            return ChangesToken.False;
        }

        public static ChangesToken Enter_Dictionary<TKey, TValue>(this TextLabel label, Dictionary<TKey, TValue> list, bool showKey = true)
        {
            int _inspected = -1;
            return label.Enter_Dictionary(list, ref _inspected, showKey: showKey);
        }

        public static ChangesToken Enter_Dictionary<TKey, TValue>(this TextLabel label, Dictionary<TKey, TValue> list, ref int inspectedElement, bool showKey = true)
        {
            using (Context.IncrementDisposible(out bool canSkip))
            {
                if (canSkip)
                    return ChangesToken.False;

                var changes = ChangeTrackStart();

                Context.Internal_isEntered_ListIcon(label, list, ref inspectedElement);
                if (Context.IsEnteredCurrent)
                    label.Edit_Dictionary(list, ref inspectedElement, showKey: showKey);

                return changes;
            }
        }
#       endregion

#       endregion

#       region Line

        public static void Line() => Line(PaintingGameViewUI ? Color.white : Color.black);

        public static void Line(Color col)
        {
            Nl();

            var c = GUI.color;
            GUI.color = col;
            if (PaintingGameViewUI)
                GUILayout.Box(GUIContent.none, Styles.HorizontalLine.Current, Utils.GuiMaxWidthOption);
            else
                GUILayout.Box(GUIContent.none, Styles.HorizontalLine.Current);

            GUI.color = c;
        }

#       endregion
    }
}