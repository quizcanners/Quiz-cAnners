

using UnityEngine;

namespace QuizCanners.Utils
{
    

    public static class PerformanceTurnTable 
    {
        private static float longestWaitLastFrame = 0;
        private static float longestWaitThisFrame = 0;
        private static bool _permissionGranted;
        private static readonly Gate.Frame _newFrameGate = new(Gate.InitialValue.StartArmed);

        public static bool TryGetMyTurn(Gate.TimeBase<float> ticket, float minDelay = 0.01f) 
        {
           // if (_newFrameGate.DoneThisFrame)
             //   return false;

            //float delta = (float)ticket.GetDeltaWithoutUpdate();

           if (!ticket.WillAllowIfTimePassed(minDelay)) //delta < minDelay)
                return false;

            if (_newFrameGate.TryEnter()) 
                ResetFrame();
            
            if (!_permissionGranted && ticket.TryUpdateIfTimePassed(longestWaitLastFrame)) 
            {
                _permissionGranted = true;
                return true;
            }

            longestWaitThisFrame = Mathf.Max(longestWaitThisFrame, (float)ticket.GetDeltaWithoutUpdate());

            return false;

            void ResetFrame()
            {
                _permissionGranted = false;
                longestWaitLastFrame = longestWaitThisFrame;
                longestWaitThisFrame = 0;
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