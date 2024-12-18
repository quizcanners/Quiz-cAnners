using System;
using System.Collections.Generic;
using QuizCanners.Migration;
using QuizCanners.Utils;
using UnityEngine;

namespace QuizCanners.Inspect
{
    public static partial class pegi
    {
        public class EnterExitContext : ICfgCustom, IPEGI
        {
            internal static bool TryGetCurrent(out EnterExitContext cnt) => s_contexts.TryGetLast(out cnt);
            internal static List<EnterExitContext> s_contexts = new();

            internal static void Clear() 
            {
                if (s_contexts.Count > 0)
                {
                    Debug.LogError("Some Contexts were not cleared: " + s_contexts.Count);
                    s_contexts.Clear();
                }
            }


            [NonSerialized] internal int _currentIndex = -1;
            [HideInInspector] [SerializeField] private int _currentlyEntered = -1;

            [NonSerialized] internal bool contextUsed;
            private readonly PlayerPrefValue.Int _playerPref = null;
            private readonly LogicWrappers.Request checkPlayerPrefs = new();
            private bool insideUsinglock;
           
            private void CheckUsingBlock(string function) 
            {
                if (!insideUsinglock)
                {
                    QcLog.ChillLogger.LogErrorOnce("Calling {0} outside of the Context Using() block".F(function), key: "CtxBlck"+ function);
                }
            }

            private int GetCurrentlyEnteredInternal() 
            {
                if (checkPlayerPrefs.TryUseRequest())
                {
                    _currentlyEntered = _playerPref.GetValue();
                }
                return _currentlyEntered;
            }

            internal int CurrentlyEntered
            {
                get
                {
                    CheckUsingBlock(nameof(CurrentlyEntered));
                    return GetCurrentlyEnteredInternal();
                }
                set
                {
                    _currentlyEntered = value;
                    OnChanged();
                }
            }

            public EnterExitContext(string playerPrefId = null)
            {
                if (!playerPrefId.IsNullOrEmpty())
                {
                    _playerPref = new PlayerPrefValue.Int("pegi/EntExit/" + playerPrefId, defaultValue: -1);
                    checkPlayerPrefs.CreateRequest();
                }
            }

            internal void OnChanged()
            {
                _playerPref?.SetValue(_currentlyEntered);
            }

            internal void Increment() => _currentIndex++;
            
            internal bool CanSkipCurrent => IsAnyEntered && _currentlyEntered != _currentIndex;

            public StateToken IsAnyEntered => new(GetCurrentlyEnteredInternal() != -1);

            public StateToken IsCurrentEntered
            {
                get
                {
                    CheckUsingBlock(nameof(IsCurrentEntered));
                    return new StateToken(_currentIndex == CurrentlyEntered);
                }
                set => CurrentlyEntered = value ? _currentIndex : -1;

            }

            public IDisposable StartContext()
            {
                _currentIndex = -1;
                s_contexts.Add(this);
                insideUsinglock = true;
                return QcSharp.DisposableAction(() =>
                {
                    s_contexts.Remove(this);

                    if (CurrentlyEntered > _currentIndex)
                    {
                        Debug.LogWarning("Entered is outside the range, exiting");
                        CurrentlyEntered = -1;
                    }
                    insideUsinglock = false;
                });
            }

            #region Inspector
            void IPEGI.Inspect()
            {
                "Currently Entered".PL().Edit(ref _currentlyEntered).Nl();
                "Current Index".PL().Edit(ref _currentIndex).Nl();
            }
            #endregion

            #region Save & Load

            public CfgEncoder Encode() => new CfgEncoder().Add_IfNotNegative("i", GetCurrentlyEnteredInternal());

            public void DecodeTag(string key, CfgData data)
            {
                switch (key)
                {
                    case "i": CurrentlyEntered = data.ToInt(); break;
                }
            }

            public void DecodeInternal(CfgData data)
            {
                CurrentlyEntered = -1;
                this.DecodeTagsFrom(data);
            }

            #endregion
        }

        internal static class Context
        {
            internal static void OnExitClick() 
            {
                if (TryGet(out var context))
                    context.IsCurrentEntered = StateToken.False;

                if (currentMode == PegiPaintingMode.UI_Toolkit) 
                    Toolkit.CurrentState.SetDirty();
            }

            internal static void OnEnterClick()
            {
                if (TryGet(out var context))
                    context.IsCurrentEntered = StateToken.True;

                if (currentMode == PegiPaintingMode.UI_Toolkit)
                    Toolkit.CurrentState.SetDirty();
            }

            internal static StateToken IsEnteredCurrent
            {
                get => TryGet(out var context) ? context.IsCurrentEntered : StateToken.False;
                /*set
                {
                    if (TryGet(out var context))
                        context.IsCurrentEntered = value;
                }*/
            }

            internal static IDisposable IncrementDisposible(out bool canSkip)
            {
            
                if (!TryGet(out var context)) 
                {
                    canSkip = true;
                    Nl();
                    Icon.Copy.Click(toolTip: "Log").OnChanged(()=> Debug.LogError("Check out this Stack Trace!")); ;
                    "You have forgotten to use Context".PL().WriteWarning().Nl();
                    return null;
                }

                if (!context.contextUsed)
                {
                    context.contextUsed = true;
                    context.Increment();
                    canSkip = context.CanSkipCurrent;

                    return QcSharp.DisposableAction(() =>
                    {
                        context.contextUsed = false;
                        IsFoldedOutOrEntered = IsEnteredCurrent;
                    });
                }

                canSkip = context.CanSkipCurrent;
                return null;
            }

            private static bool TryGet(out EnterExitContext context)
            {
                if (!EnterExitContext.TryGetCurrent(out context)) //context == null)
                {
                    "Indexer unset. wrap section in  using(EnterExitIndexes.StartContext()) {  }".PL().WriteWarning();
                    return false;
                }
                return true;
            }

            internal static bool Internal_isConditionally_Entered(TextLabel label, bool canEnter, bool showLabelIfTrue = true)
            {
                if (canEnter)
                    Internal_isEntered(label, showLabelIfTrue: showLabelIfTrue);
                else
                if (IsEnteredCurrent)
                    OnExitClick();//IsEnteredCurrent = StateToken.False;

                return IsEnteredCurrent;
            }

            internal static void Internal_isEntered_ListIcon<Tkey, TValue>(TextLabel txt, Dictionary<Tkey,TValue> list, ref int inspected)
            {
                if (collectionInspector.CollectionIsNull(list))
                {
                    if (IsEnteredCurrent)
                        OnExitClick();
                    return;
                }

                var before = IsEnteredCurrent;

                var label = txt.AddCount(list, IsEnteredCurrent);

                Internal_isEntered(label, showLabelIfTrue: false);

                if (IsEnteredCurrent)
                {
                    if (!before && IsEnteredCurrent)
                        inspected = -1;
                }
                else
                    EnterInternal.ClickEnter_DirectlyToElement_Internal(list, ref inspected).OnChanged(() => OnEnterClick());
            }

            internal static void Internal_isEntered_ListIcon<T>(TextLabel txt, List<T> list, ref int inspected)
            {
                if (collectionInspector.CollectionIsNull(list))
                {
                    if (IsEnteredCurrent)
                        OnExitClick();
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
                    EnterInternal.ClickEnter_DirectlyToElement_Internal(list, ref inspected).OnChanged(() => OnEnterClick());
            }

            internal static void Internal_exitOptionOnly(TextLabel txt, bool showLabelIfTrue = true)
            {
                if (IsEnteredCurrent)
                    ExitClick(txt, showLabelIfTrue: showLabelIfTrue);
            }

            internal static bool Internal_isEntered(TextLabel txt, bool showLabelIfTrue = true)
            {
                if (IsEnteredCurrent)
                    ExitClick(txt, showLabelIfTrue: showLabelIfTrue);
                else
                {
                    txt.style = Styles.Text.EnterLabel;
                    (Icon.Enter.ClickUnFocus(txt.label).IgnoreChanges(LatestInteractionEvent.Enter) |
                    txt.ClickLabel().IgnoreChanges(LatestInteractionEvent.Enter)).OnChanged(() => OnEnterClick());
                }

                return IsEnteredCurrent;
            }

            internal static ChangesToken Internal_Enter_Inspect_AsList<T>(T var, string exitLabel = null) where T : class, IPEGI_ListInspect
            {
                var changed = ChangeTrackStart();
                if (var.IsNullOrDestroyed_Obj())
                {
                    if (IsEnteredCurrent)
                        OnExitClick();
                    return changed;
                }

                if (!EnterExitContext.TryGetCurrent(out var cnt))
                {
                    "No Context".PL().WriteWarning().Nl();
                    return changed;
                }

                if (!cnt.IsCurrentEntered) //!IsEnteredCurrent)
                {
                    int current = cnt._currentIndex;
                    int entered = cnt.CurrentlyEntered;

                    if (Nested_Inspect(() => var.InspectInList(ref entered, current), var as UnityEngine.Object))
                    {
                        cnt.CurrentlyEntered = entered;
                        new ChangesToken(IsEnteredCurrent).IgnoreChanges(LatestInteractionEvent.Enter);
                    }

                    return changed;
                }
               
                var label = new TextLabel(exitLabel.IsNullOrEmpty() ? var.GetNameForInspector() : exitLabel, style: Styles.Text.ExitLabel);
                ExitClick(label, showLabelIfTrue: true);
                Try_Nested_Inspect(var);

                return changed;
            }

            internal static ChangesToken Internal_Enter_Inspect_AsList<T>(ref T var, string exitLabel = null) where T : IPEGI_ListInspect
            {
                var changed = ChangeTrackStart();

                if (!EnterExitContext.TryGetCurrent(out var cnt))
                {
                    "No Context".PL().WriteWarning().Nl();
                    return changed;
                }

                if (!cnt.IsCurrentEntered)
                {
                    int current = cnt._currentIndex;
                    int entered = cnt.CurrentlyEntered;

                    var.InspectInList(ref entered, current);

                    if (changed)
                    {
                        cnt.CurrentlyEntered = entered;
                        new ChangesToken(IsEnteredCurrent).IgnoreChanges(LatestInteractionEvent.Enter);
                    }

                    return changed;
                }

                var label = new TextLabel(exitLabel.IsNullOrEmpty() ? var.GetNameForInspector() : exitLabel, style: Styles.Text.ExitLabel);
                ExitClick(label, showLabelIfTrue: true);

                NestedOrReflection_Inspect(ref var);
                //Nested_Inspect_Value(ref var);
                //Nested_Inspect_Value(ref var);

                return changed;
            }

            private static void ExitClick(TextLabel text, bool showLabelIfTrue) 
            {
                using (Styles.Background.ExitLabel.SetDisposible())
                {
                    text.FallbackHint = ()=> Icon.Exit.GetDescription();
                    text.style = Styles.Text.ExitLabel;
                    (Icon.Exit.ClickUnFocus("{0} L {1}".F(Icon.Exit.GetText(), text)) |
                        (showLabelIfTrue ? text.ClickLabel().IgnoreChanges(LatestInteractionEvent.Exit) : ChangesToken.False)
                        ).OnChanged(() => OnExitClick()).IgnoreChanges();
                }
            }

        }
    }
}