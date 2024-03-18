

using UnityEngine;

namespace QuizCanners.Utils
{
    

    public static class PerformanceTurnTable 
    {
        private static float longestWaitLastFrame = 0;
        private static float longestWaitThisFrame = 0;
        private static bool _permissionGranted;
        private static readonly Gate.Frame _newFrameGate = new();

        public static bool TryGetMyTurn(Gate.TimeBase<float> ticket, float minDelay = 0.01f) 
        {
            float delta = (float)ticket.GetDeltaWithoutUpdate();

            if (delta < minDelay)
                return false;

            if (_newFrameGate.TryEnter()) 
            {
                _permissionGranted = false;
                longestWaitLastFrame = longestWaitThisFrame;
                longestWaitThisFrame = 0;
            }

            if (!_permissionGranted && ticket.TryUpdateIfTimePassed(longestWaitLastFrame)) 
            {
                _permissionGranted = true;
                return true;
            }

            longestWaitThisFrame = Mathf.Max(longestWaitThisFrame, (float)ticket.GetDeltaWithoutUpdate());

            return false;
        }

        public class Token 
        {
            private float _delay;
            private Gate.UnityTimeUnScaled _checkVisibilityDelta = new(Gate.InitialValue.Uninitialized);

            public bool TryGetTurn() 
            {
                return TryGetMyTurn(_checkVisibilityDelta, _delay);
            }

            public Token(float delay) 
            {
                _delay = delay;
            }
        }

    }
}