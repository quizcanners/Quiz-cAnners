

using UnityEngine;
using static QuizCanners.Utils.QcDebug;

namespace QuizCanners.Utils
{
    

    public static class PerformanceTurnTable 
    {
        private static float longestWaitLastFrame = 0;
        private static float longestWaitThisFrame = 0;
        private static bool _permissionGranted;
        private static readonly Gate.Frame _newFrameGate = new(Gate.InitialValue.StartArmed);

        public static bool TryGetMyTurn(Gate.TimeGeneric<float> ticket, float minDelay = 0.01f) 
        {
           // if (_newFrameGate.DoneThisFrame)
             //   return false;

            //float delta = (float)ticket.GetDeltaWithoutUpdate();

           if (!ticket.WillAllowIfTimePassed(minDelay)) //delta < minDelay)
                return false;

            if (_newFrameGate.TryEnter()) 
                ResetFrame();
            
            if (!_permissionGranted) 
            {
                if (ticket.TryUpdateIfTimePassed(longestWaitLastFrame))
                {
                    _permissionGranted = true;
                    return true;
                }
            } else if (ticket.TryUpdateIfTimePassed(longestWaitLastFrame * 3))
            {
               // Debug.Log("Irregular token");
                return true;
            }

            float delay = (float)ticket.GetDeltaWithoutUpdate();

            longestWaitThisFrame = Mathf.Max(longestWaitThisFrame, delay);

            return false;

            void ResetFrame()
            {
                _permissionGranted = false;
                longestWaitLastFrame = longestWaitThisFrame;
                longestWaitThisFrame = 0;
            }

        }

        public class CollectionIndexToken 
        {
            private readonly Gate.Frame _frameGate = new(Gate.InitialValue.StartArmed);
            int max_Index;
            int max_Count_Previous;
            int next_Index;

            private bool loopInProgress;

            public bool TryGetMyTurn(int index, Gate.TimeBase sinceLastLoopGate, float delay)
            {
                if (index < max_Index && _frameGate.TryEnter())    
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
                            loopInProgress = sinceLastLoopGate.TryUpdateIfTimePassed(delay);
                            if (loopInProgress)
                                next_Index = 0;
                        }

                        return;
                    }
                  
                    loopInProgress = true;
                    sinceLastLoopGate.Update();
                }
            }

            public bool TryGetMyTurn(int index) 
            {
                if (index < max_Index && _frameGate.TryEnter())
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
            private float _delay;
            private readonly Gate.UnityTimeUnScaled _checkVisibilityDelta;

            public bool TryGetTurn(float delay)
            {
                return TryGetMyTurn(_checkVisibilityDelta, delay);
            }

            public bool TryGetTurn() 
            {
                return TryGetMyTurn(_checkVisibilityDelta, _delay);
            }

            public Token(float delay, Gate.InitialValue initialValue) 
            {
                _delay = delay;
                _checkVisibilityDelta = new(initialValue);
            }
        }

    }
}