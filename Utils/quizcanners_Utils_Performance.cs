using QuizCanners.Inspect;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Utils
{
    public  static partial class QcDebug 
    {
        public class EnumeratedUpdateLoopTracker<T> : IPEGI
        {
            private T _currentStage;
            QcDebug.TimeProfiler.DisposableTimer disp = null;
            private readonly Dictionary<T, QcDebug.TimeProfiler.TimerCollectionElements.AvgValue> updateErrorTrackers = new();

            private readonly string _key;
            public QcDebug.TimeProfiler.DictionaryOfParallelTimers Timer; // => 

            public bool Enabled;

            public EnumeratedUpdateLoopTracker(string key, bool enabled)
            {
                _key = key;
                Enabled = enabled;
            }

            public double ElapsedMiliseconds => disp == null ? 0 : disp.ElapsedMiliseconds;// Display 

            public void End()
            {
                disp?.Dispose();
            }

            public IDisposable StartContext()
            {
                return QcSharp.DisposableAction(() => End());
            }

            public IDisposable MeasureStage(T stage)
            {
                if (!Enabled)
                    return null;

                Timer ??= QcDebug.TimeProfiler.Instance[_key];

                if (disp != null && disp.ElapsedMiliseconds > 100)
                {
                    Debug.LogError("{0}ms in {1}.{2}".F(disp.ElapsedMiliseconds, _key, _currentStage));
                }

                disp?.Dispose();
                _currentStage = stage;

                if (!updateErrorTrackers.TryGetValue(stage, out var timer))
                {
                    timer = Timer.Avg(stage.ToString());
                    updateErrorTrackers[stage] = timer;
                }

                disp = timer.Start();
                return disp;
            }

            public override string ToString() => _key;

            public void Inspect()
            {
                "Enabled".PL().ToggleIcon(ref Enabled).NL();

                if (Timer == null)
                {
                    "Timer  never started".PL().NL();
                    return;
                }
                else
                    Timer.Nested_Inspect().NL();
            }
        }

        public static class PerformanceTurnTable 
        {
            private static float longestWaitLastFrame = 0;
            private static float longestWaitThisFrame = 0;
            internal static bool _permissionGrantedThisFrame;
            private static readonly Gate.Frame _newFrameGate = new();

            public static bool TryGetMyTurn(Gate.TimeGeneric<float> ticket, float minDelay = 0.01f) 
            {
               // if (_newFrameGate.DoneThisFrame)
                 //   return false;

                //float delta = (float)ticket.GetDeltaWithoutUpdate();

               if (!ticket.TryConsume_IfElapsedOrFirst(minDelay)) //delta < minDelay)
                    return false;

                if (_newFrameGate.TryConsume()) 
                    ResetFrame();
            
                if (!_permissionGrantedThisFrame) 
                {
                    if (ticket.TryConsume_IfElapsedOrFirst(longestWaitLastFrame))
                    {
                        _permissionGrantedThisFrame = true;
                        if (Application.isEditor && longestWaitLastFrame > 2) 
                        {
                            Debug.LogError("Performane token waited for "+longestWaitLastFrame);
                        }
                        return true;
                    }
                } else if (ticket.TryConsume_IfElapsedOrFirst(longestWaitLastFrame * 3))
                {
                   // Debug.Log("Irregular token");
                    return true;
                }

                float delay = (float)ticket.Peek_OrFirstStart();

                longestWaitThisFrame = Mathf.Max(longestWaitThisFrame, delay);

                return false;

                static void ResetFrame()
                {
                    _permissionGrantedThisFrame = false;
                    longestWaitLastFrame = longestWaitThisFrame;
                    longestWaitThisFrame = 0;
                }

            }

            public class CollectionIndexToken 
            {
                private readonly Gate.Frame _frameGate = new();
                int max_Index;
                int max_Count_Previous;
                int next_Index;

                private bool loopInProgress;

                public bool TryGetMyTurn(int index, Gate.TimeBase sinceLastLoopGate, float delay)
                {
                    if (index < max_Index && _frameGate.TryConsume())    
                        OnNextLoop();

                    max_Index = Mathf.Max(max_Index, index);

                    return loopInProgress && index == next_Index;

                    void OnNextLoop()
                    {
                        max_Count_Previous = max_Index + 1;
                        max_Index = 0;

                        next_Index++;

                        if (!loopInProgress || next_Index >= max_Count_Previous)
                        {
                            TryRestartLoop();

                            void TryRestartLoop()
                            {
                                loopInProgress = sinceLastLoopGate.TryConsume_IfElapsedOrFirst(delay);
                                if (loopInProgress)
                                    next_Index = 0;
                            }

                            return;
                        }
                  
                        loopInProgress = true;
                        sinceLastLoopGate.Start();
                    }
                }

                public bool TryGetMyTurn(int index) 
                {
                    if (index < max_Index && _frameGate.TryConsume())
                        OnNextFrame();

                    max_Index = Mathf.Max(max_Index, index);

                    return index == next_Index;

                    void OnNextFrame()
                    {
                        max_Count_Previous = max_Index + 1;
                        max_Index = 0;
                        if (max_Count_Previous > 0)
                            next_Index = (next_Index + 1) % max_Count_Previous;
                        else
                            next_Index = 0;
                    }
                }
            }

            public class Token 
            {
                private readonly float _delay;
                private readonly Gate.UnityTimeUnScaled _checkVisibilityDelta;
                private readonly string _name;
                private readonly bool _gotName;
                private int permissionGrantedFrame = -1;

                public void Reset() => _checkVisibilityDelta.ValueIsDefined = false;

                public void OnFrameResourcesNotUsed() 
                {
                    if (permissionGrantedFrame == Time.frameCount)
                        _permissionGrantedThisFrame = false;
                }

                public bool TryGetTurn(float delay)
                {
                    if (!TryGetMyTurn(_checkVisibilityDelta, delay))
                        return false;

                    permissionGrantedFrame = Time.frameCount;

                    if (_gotName)
                    {
                        _requests.TryGetValue(_name, out var val);
                        val++;
                        _requests[_name] = val;
                    }

                    return true;
                }

                public bool Update(bool needsFrameResources) 
                {
                    if (!TryGetTurn())
                        return false;

                    if (!needsFrameResources)
                        OnFrameResourcesNotUsed();

                    return needsFrameResources;
                }

                public bool TryGetTurn(bool shouldBlockOthersPermission) 
                {
                    if (!TryGetTurn())
                        return false;

                    if (!shouldBlockOthersPermission)
                        OnFrameResourcesNotUsed();

                    return true;
                }

                public bool TryGetTurn() 
                {
                    if (!TryGetMyTurn(_checkVisibilityDelta, _delay))
                        return false;

                    permissionGrantedFrame = Time.frameCount;

                    if (_gotName) 
                    {
                        _requests.TryGetValue(_name, out var val);
                        val++;
                        _requests[_name] = val;
                    }

                    return true;
                }

                public Token(float delay) 
                {
                    _delay = delay;
                    _checkVisibilityDelta = new();
                }

                public Token(string name, float delay)
                {
                    _name = name;
                    _delay = delay;
                    _checkVisibilityDelta = new();
                    _gotName = true;
                }

                private static readonly System.Collections.Generic.Dictionary<string, int> _requests = new();

                public static void InspectTokenStack() 
                {
                    "Requests".PL().Edit_Dictionary(_requests).NL();
                }

         
            }
        }
    }
}