using QuizCanners.Inspect;
using System;
using UnityEngine;

namespace QuizCanners.Utils
{
    [Serializable]
    public class SerializableTimeInterval : IPEGI
    {
        [SerializeField] private bool _isStarted;
        [SerializeField] private SerializableDateTime _startTime = new SerializableDateTime();
        [SerializeField] private double _durationInsSeconds;
        
        public TimeSpan Duration 
        {
            get => TimeSpan.FromSeconds(_durationInsSeconds);
            set => _durationInsSeconds = value.TotalSeconds;
        }

        public TimeSpan TimePassed => DateTime.Now - _startTime;
     
        public float FractionPassed
        {
            get
            {
                if (!_isStarted)
                    return 0;

                if (_durationInsSeconds <= 0)
                    return 1;

                return Mathf.Clamp01((float)(TimePassed.TotalSeconds / _durationInsSeconds));
            }
        }

        public State GetState() 
        {
            if (!_isStarted)
                return State.NeverStarted;

            if (TimePassed >= Duration)
                return State.Done;

            return State.InProgress;
        }

        public void SkipSeconds(double seconds) => _startTime.Value -= TimeSpan.FromSeconds(seconds);
        public void Skip(TimeSpan span) => _startTime.Value -= span;

        public void Start(TimeSpan duration)
        {
            _isStarted = true;
            _startTime.Value = DateTime.Now;
            Duration = duration;
        }

        public enum State { NeverStarted, InProgress, Done }

        #region Inspector
        void IPEGI.Inspect()
        {
            "Progress: {0}".F(Mathf.FloorToInt(FractionPassed * 100)).PegiLabel().Nl();

            "Duration (sec)".PegiLabel().Edit(ref _durationInsSeconds).Nl();

            if ("Start".PegiLabel().Click())
                Start(Duration);

            if ("Skip 30 sec".PegiLabel().Click())
                SkipSeconds(30);

        }
        #endregion
    }
}
