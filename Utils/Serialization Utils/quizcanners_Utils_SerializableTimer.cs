using QuizCanners.Inspect;
using System;
using UnityEngine;

namespace QuizCanners.Utils
{
    [Serializable]
    public class SerializableTimer : IPEGI
    {
        public double Duration;
        [SerializeField] private SerializableDateTime _startTime;

        public TimeSpan TimePassed 
        {
            get 
            {
                var diff = DateTime.Now - _startTime;
                return diff;
            }
        }
        public float FractionPassed
        {
            get
            {
                if (Duration == 0)
                    return 1;

                return Mathf.Clamp01((float)(TimePassed.TotalSeconds / Duration));
            }
        }

        public void Skip(double seconds) => _startTime.Value -= TimeSpan.FromSeconds(seconds);
        public void Skip(TimeSpan span) => _startTime.Value -= span;
        public void Start() => _startTime.Value = DateTime.Now;

        public SerializableTimer(float duration)
        {
            _startTime = new SerializableDateTime();
            Start();
            Duration = duration;
        }
        public SerializableTimer()
        {
            _startTime = new SerializableDateTime();
            Start();
        }

        #region Inspector
        public void Inspect()
        {
            "Progress: {0}".F(Mathf.FloorToInt(FractionPassed * 100)).PegiLabel().nl();

            "Duration".PegiLabel().edit(ref Duration).nl();

            if ("Start".PegiLabel().Click())
                Start();

            if ("Skip 30 sec".PegiLabel().Click())
                Skip(30);

        }
        #endregion
    }
}
