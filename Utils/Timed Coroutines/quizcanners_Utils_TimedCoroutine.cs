﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using QuizCanners.Inspect;

using Debug = UnityEngine.Debug;

namespace QuizCanners.Utils
{
    public class TimedCoroutine : IPEGI_ListInspect, IPEGI, IGotName
    {

        public static CallAgainRequest CallAgain => new();

        public class CallAgainRequest
        {
            public string message;

            public Task task;

            public CallAgainRequest()
            {
            }

            public CallAgainRequest(string message)
            {
                this.message = message;
            }

            public CallAgainRequest(Task task)
            {
                this.task = task;
            }

            public CallAgainRequest(Task task, string message)
            {
                this.task = task;
                this.message = message;
            }
        }

        #region FrameTiming
        private const float maxMilisecondsPerFrame = 1000f / 60f;
        private static float TotalTimeUsedThisFrame;
        private static int FrameIndex = -1;
        #endregion
            
        public bool DoneFully { get; private set; }
        public bool Exited { get; private set; }
        public int EnumeratorVersion { get; private set; }

        private List<Action> _onExit = new();
        private List<Action> _onDoneFully = new();

        private readonly List<IEnumerator> _enumeratorStack = new();
        private Task _task;
        private int _runningVersion;
        private CallAgainRequest _currentCallAgainRequest;
        private object _current;
        private bool _currentIsUncheck;
        protected bool _stopAndCancel;
        private readonly System.Diagnostics.Stopwatch timer = new();

        public void Stop() => EnumeratorVersion +=1;
            
        private void ResetInternal(IEnumerator enumerator) 
        {
            EnumeratorVersion += 1; // To stop any active coroutines
            DoneFully = false;
            Exited = false;
            _onExit.Clear();
            _onDoneFully.Clear();

            _task = null;
            _stopAndCancel = false;
            _enumeratorStack.Clear();
            _enumeratorStack.Add(enumerator);
            _currentIsUncheck = true;
        }

        protected virtual void OnDone()
        {
            Exited = true;

            // In case On Exit Call restarts enumerator, we are disconnecting the following values:

            var onExit = _onExit;
            var onFulleDone = _onDoneFully;
            var stopAndCancel = _stopAndCancel;

            if (!stopAndCancel)
                DoneFully = true;

            _onExit = new List<Action>();
            _onDoneFully = new List<Action>();

            if (onExit.Count > 0)
            {
                foreach (var act in onExit)
                {
                    try
                    {
                        act?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        _state = "Eception in OnExit of TimedEnumerator: " + _state + ex;
                        Debug.LogError(_state);
                        Debug.LogException(ex);
                    }
                }
            }

            if (stopAndCancel)
            {
                _state = "Stopped and cancelled after " + _state;
            }
            else
            {
                if (onFulleDone.Count>0)
                {
                    foreach (var act in onFulleDone)
                    {
                        try
                        {
                            act.Invoke();
                        }
                        catch (Exception ex)
                        {
                            _state = "Exception in OnFully Done " + ex;
                            Debug.LogError(_state);
                            Debug.LogException(ex);
                        }
                    }
                }
            }
        }

        private void ProcessCurrent(IEnumerator en) 
        {
            bool wasEnumerator = _currentIsUncheck;

            _currentIsUncheck = false;
            _yields++;
            _current = en.Current;

            if (_current == null)
                return;

            if (_current is IEnumerator enm)
            {
                if (wasEnumerator)
                {
                    // Debug.LogError("Upon change enumerator returned enumerator {0} => {1}".F(en.ToString(), enm.ToString()));
                    if (enm == en)
                    {
                        return;
                    }
                }

                _enumeratorStack.Add(enm);
                _current = null;
                _currentIsUncheck = true;
                return;
            }

            if (_current is string)
            {
                _state = _current as string;
                return;
            }

            _currentCallAgainRequest = _current as CallAgainRequest;

            if (_currentCallAgainRequest != null)
            {
                if (_currentCallAgainRequest.message != null)
                {
                    _state = _currentCallAgainRequest.message;
                }

                if (_currentCallAgainRequest.task != null)
                {
                    _task = _currentCallAgainRequest.task;
                }

                _current = null;

                return;
            }

            if (_current is Task tsk)
            {
                _task = tsk;
                _current = null;
                return;
            }
        }

        private bool MoveNext_Internal()
        {
            if (_stopAndCancel) 
            {
                return false;
            }

            _currentCallAgainRequest = null;

            if (_task != null)
            {
                if (!_task.IsCompleted)
                {
                    return true;
                }
                _task = null;
            }

            try
            {
                IEnumerator en = _enumeratorStack[_enumeratorStack.Count - 1];

                if (en.MoveNext())
                {
                    ProcessCurrent(en);
                    return true;
                }
                else
                {
                    if (_enumeratorStack.Count > 1)
                    {
                        _enumeratorStack.RemoveAt(_enumeratorStack.Count - 1);
                        _currentIsUncheck = true;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _state = "Error after {0}: {1}".F(_state, ex.ToString());

                Debug.LogError("Managed Exception in Timed Enumerator: " + _state);
                Debug.LogException(ex);

                _task = null;
                _stopAndCancel = true;
            }

            return false;
        }

        private bool NeedToStopInternalYielding()
        {
            if (_currentIsUncheck)
            {
                return false;
            }
                
            if (_currentCallAgainRequest == null || ((TotalTimeUsedThisFrame + timer.ElapsedMilliseconds) > maxMilisecondsPerFrame))
            {
                var el = timer.ElapsedMilliseconds;

                TotalTimeUsedThisFrame += el;

                if (_logUnoptimizedSections && UnityEngine.Application.isEditor && el > maxMilisecondsPerFrame * 2)
                {
                    Debug.Log("{0} Needs x{1} segmentation".F(_state, el / maxMilisecondsPerFrame));
                }

                _frames++;
                return true;
            }
                
            return false;
        }

        private void ResetTimer()
        {
            timer.Restart();

            if (FrameIndex != UnityEngine.Time.frameCount)
            {
                TotalTimeUsedThisFrame = 0;
                FrameIndex = UnityEngine.Time.frameCount;
            }
        }

        // For Managed Yielding:
        public bool MoveNext()
        {
            ResetTimer();

            while (MoveNext_Internal())
            {
                if (NeedToStopInternalYielding())
                {
                    return true;
                }
            }

            OnDone();

            return false;
        }

        public async Task StartTask(Action onExitAction = null, Action onDoneFullyAction = null)
        {
                
            if (!CanStart_Internal(out var thisVersion))
                return;

            TryAdd(onExitAction: onExitAction, onDoneFullyAction: onDoneFullyAction);

            while (MoveNext_Internal())
            {
                if (NeedToStopInternalYielding())
                {
                    await Task.Yield();
                    ResetTimer();
                }

                if (EnumeratorVersion != thisVersion)
                {
                    return;
                }
            }
            OnDone();
        }
            
        //To be used inside a Coroutine:
        public IEnumerator GetCoroutine(Action onExitAction = null, Action onDoneFullyAction = null)
        {
            if (!CanStart_Internal(out var thisVersion))
                yield break;

            TryAdd(onExitAction: onExitAction, onDoneFullyAction: onDoneFullyAction);

            while (MoveNext_Internal())
            {
                if (NeedToStopInternalYielding())
                {
                    yield return _current;
                    ResetTimer();
                }

                if (EnumeratorVersion != thisVersion)
                {
                    yield break;
                }
            }
            OnDone();
        }

        private void TryAdd(Action onExitAction, Action onDoneFullyAction) 
        {
            if (onDoneFullyAction != null)
                _onDoneFully.Add(onDoneFullyAction);

            if (onExitAction != null)
                _onExit.Add(onExitAction);
        }

        private bool CanStart_Internal(out int thisVersion)
        {
            thisVersion = EnumeratorVersion;

            if (_enumeratorStack.IsNullOrEmpty())
            {
                _state = "No enumerator";
                Debug.LogError(_state);
                return false;
            }

            if (thisVersion == _runningVersion)
            {
                _state = "This enumerator is already running";
                Debug.LogError(_state);
                return false;
            }

            _runningVersion = thisVersion;

            ResetTimer();

            _state = "Starting";

            return true;
        }
        
        public TimedCoroutine Reset(IEnumerator enumerator, Action onExitAction = null, Action onDoneFullyAction = null, string nameForInspector = "")
        {
            ResetInternal(enumerator);
            _state = "Resetting: " + enumerator;
            TryAdd(onExitAction: onExitAction, onDoneFullyAction: onDoneFullyAction);
            NameForInspector = nameForInspector.IsNullOrEmpty() ? enumerator.ToString() : nameForInspector;

            return this;
        }

        public TimedCoroutine(bool logUnoptimizedSections = false, string nameForInspector = "")
        {
            _logUnoptimizedSections = logUnoptimizedSections;
            NameForInspector = nameForInspector;
        }

        public TimedCoroutine(IEnumerator enumerator, bool logUnoptimizedSections = false, string nameForInspector = "")
        {
            _logUnoptimizedSections = logUnoptimizedSections;
            Reset(enumerator, nameForInspector: nameForInspector);
        }

        #region Inspector
            
        protected bool _logUnoptimizedSections;
        private string _state = "";
        private int _yields;
        private int _frames;

        public string NameForInspector { get; set; }

        public void InspectInList(ref int edited, int ind)
        {

            if (Icon.Enter.Click())
                edited = ind;

            if (Exited)
                (DoneFully ? Icon.Done : Icon.Empty).Draw();

            "{4}: {5} {2} {3} [{0}y {1}f]".F(
                _yields, // 0
                _frames, // 1
                EnumeratorVersion > 1 ? ("v: " + EnumeratorVersion) : "", //2
                _task == null ? "[CORO]" : "[TASK]", //3
                NameForInspector, // 4
                _state // 5
                ) // 4
                .PegiLabel(_state).Write();
                
        }

        void IPEGI.Inspect()
        {
            if (!Exited && !_stopAndCancel && "Stop & Cancel".PegiLabel().Click().Nl())
                _stopAndCancel = true;

            if (!Exited && "Yield".PegiLabel().Click().Nl())
                MoveNext();

            _state.PegiLabel().WriteBig();

        }
        #endregion
    }

    
}