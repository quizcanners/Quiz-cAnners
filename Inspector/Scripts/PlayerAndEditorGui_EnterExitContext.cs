using System;
using System.Collections.Generic;
using QuizCanners.Migration;
using QuizCanners.Utils;
using UnityEngine;
using Object = UnityEngine.Object;


namespace QuizCanners.Inspect
{
    public static partial class pegi
    {
        [Serializable]
        public class EnterExitContext : ICfgCustom
        {
            [NonSerialized] internal int _currentIndex = -1;
            [SerializeField] internal int _currentlyEntered = -1;
            private EnterExitContext _previous;
            [NonSerialized] internal bool contextUsed;

            internal static EnterExitContext CurrentIndexer;

            internal void Increment()
            {
                _currentIndex++;
            }

            internal bool CanSkipCurrent => _currentlyEntered != -1 && _currentlyEntered != _currentIndex;

            public StateToken IsAnyEntered => new StateToken(_currentlyEntered != -1);

            public StateToken IsCurrentEntered
            {
                get => new StateToken(_currentIndex == _currentlyEntered);
                set
                {
                    _currentlyEntered = value ? _currentIndex : -1;
                }
            }

            public IDisposable StartContext()
            {
                _currentIndex = -1;
                _previous = CurrentIndexer;
                CurrentIndexer = this;
                return QcSharp.DisposableAction(()=> 
                {
                    CurrentIndexer = _previous;
                    if (_currentlyEntered > _currentIndex)
                    {
                        Debug.LogWarning("Entered is outside the range, exiting");
                        _currentlyEntered = -1;
                    }
                });
            }

            public CfgEncoder Encode() => new CfgEncoder().Add_IfNotNegative("i", _currentlyEntered);
            
            public void DecodeTag(string key, CfgData data)
            {
                switch (key) 
                {
                    case "i": _currentlyEntered = data.ToInt(); break;
                }
            }

            public void DecodeInternal(CfgData data)
            {
                _currentlyEntered = -1;
                this.DecodeTagsFrom(data);
            }
        }

        internal static class Context
        {
            internal static StateToken IsEnteredCurrent
            {
                get => TryGet(out var context) ? context.IsCurrentEntered : StateToken.False;
                set
                {

                    if (TryGet(out var context))
                        context.IsCurrentEntered = value;
                }
            }

            internal static IDisposable IncrementDisposible(out bool canSkip)
            {
                canSkip = !TryGet(out var context);

                if (context.contextUsed == false)
                {
                    context.contextUsed = true;
                    context.Increment();
                    canSkip |= context.CanSkipCurrent;

                    return QcSharp.DisposableAction(() =>
                    {
                        context.contextUsed = false;
                        PegiEditorOnly.IsFoldedOutOrEntered = IsEnteredCurrent;
                    });
                } else 
                {
                    if (context != null)
                    {
                        canSkip |= context.CanSkipCurrent;
                    }

                    return null;
                }

            }

            private static bool TryGet(out EnterExitContext context)
            {
                context = EnterExitContext.CurrentIndexer;
                if (context == null)
                {
                    "Indexer unset. wrap section in  using(EnterExitIndexes.StartContext()) {  }".PegiLabel().writeWarning();
                    return false;
                }
                return true;
            }

            public static bool Internal_isConditionally_Entered(TextLabel label, bool canEnter, bool showLabelIfTrue = true)
            {
                if (canEnter)
                    Internal_isEntered(label, showLabelIfTrue: showLabelIfTrue);
                else
                if (IsEnteredCurrent)
                    IsEnteredCurrent = StateToken.False;

                return IsEnteredCurrent;
            }

            public static void Internal_isEntered_ListIcon<T>(TextLabel txt, List<T> list, ref int inspected)
            {
                if (collectionInspector.CollectionIsNull(list))
                {
                    if (IsEnteredCurrent)
                        IsEnteredCurrent = StateToken.False;
                    return;
                }

                var before = IsEnteredCurrent;

                var label = txt.AddCount(list, IsEnteredCurrent);

                Internal_isEntered(label, showLabelIfTrue: false);

                if (IsEnteredCurrent) 
                { 
                    if (!before && IsEnteredCurrent)
                        inspected = -1;
                } else 
                    list.clickEnter_DirectlyToElement(ref inspected).OnChanged(() => IsEnteredCurrent = StateToken.True);
            }

            public static void Internal_isEntered(TextLabel txt, bool showLabelIfTrue = true)
            {
                if (IsEnteredCurrent)
                    ExitClick(txt, showLabelIfTrue: showLabelIfTrue);
                else
                {
                    txt.style = Styles.EnterLabel;
                    (icon.Enter.ClickUnFocus(txt.label).IgnoreChanges(LatestInteractionEvent.Enter) |
                    txt.ClickLabel().IgnoreChanges(LatestInteractionEvent.Enter)).OnChanged(() => IsEnteredCurrent = StateToken.True);
                }
            }

            public static void Internal_enter_Inspect_AsList(IPEGI_ListInspect var, string exitLabel = null)
            {
                if (!var.IsNullOrDestroyed_Obj())
                {
                    if (!IsEnteredCurrent)
                    {
                        int current = EnterExitContext.CurrentIndexer._currentIndex;
                        int entered = EnterExitContext.CurrentIndexer._currentlyEntered;

                        if (Nested_Inspect(() => var.InspectInList(ref entered, current)))
                        {
                            EnterExitContext.CurrentIndexer._currentlyEntered = entered;
                            new ChangesToken(IsEnteredCurrent).IgnoreChanges(LatestInteractionEvent.Enter);
                        }
                    }
                    else
                    {
                        var label = new TextLabel(exitLabel.IsNullOrEmpty() ? var.GetNameForInspector() : exitLabel, style: Styles.ExitLabel);
                        ExitClick(label, showLabelIfTrue: true);
                        Try_Nested_Inspect(var);
                    }
                }
                else if (IsEnteredCurrent)
                    IsEnteredCurrent = StateToken.False;
            }

            private static void ExitClick(TextLabel text, bool showLabelIfTrue) 
            {
                using (Styles.Background.ExitLabel.SetDisposible())
                {
                    text.FallbackHint = ()=> icon.Exit.GetDescription();
                    text.style = Styles.ExitLabel;
                    (icon.Exit.ClickUnFocus("{0} L {1}".F(icon.Exit.GetText(), text)) |
                        (showLabelIfTrue ? text.ClickLabel().IgnoreChanges(LatestInteractionEvent.Exit) : ChangesToken.False)
                        ).OnChanged(() => IsEnteredCurrent = StateToken.False);
                }
            }

        }

    }
}