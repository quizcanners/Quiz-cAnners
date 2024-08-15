using QuizCanners.Inspect;
using UnityEngine;

namespace QuizCanners.Utils
{
    public static class QcPhisix 
    {
        public class Accelerometer : IPEGI
        {
            private bool _positionInitialized = false;
            private Vector3 previosPosition;
            private float _smoothingSeconds = 0.04f;
            private Vector3 previousVelocity;

            public Vector3 AvaragedAcceleration { get; private set; }

            private float _previousTime;
     


            public void OnFixedUpdate(Rigidbody rb) 
            {
                if ((Time.time - _previousTime) > 1f)
                    _positionInitialized = false;

                if (!_positionInitialized) 
                {
                    _positionInitialized = true;
                    previosPosition = rb.position;
                    AvaragedAcceleration = Vector3.zero;
                    previousVelocity = rb.velocity;
                    _previousTime = Time.time;
                    
                    return;
                }

                float DELTA = Time.fixedTime - _previousTime;
              
                if (Mathf.Approximately(DELTA, 0))
                    return;

                var currentAcceleration = (rb.velocity - previousVelocity) / DELTA;

                AvaragedAcceleration = Vector3.Lerp(AvaragedAcceleration, currentAcceleration, Mathf.Clamp01(DELTA / _smoothingSeconds));

                _previousTime = Time.fixedTime;
            }

            #region Inspector
            void IPEGI.Inspect()
            {
                "Acceleration {0}".F(QcSharp.ToReadableString(AvaragedAcceleration.magnitude)).PegiLabel().DrawProgressBar(AvaragedAcceleration.magnitude * 0.1f);


            }
            #endregion

            public Accelerometer() { }

            public Accelerometer(float smoothingSeconds) 
            {
                _smoothingSeconds = smoothingSeconds;
            }
        }
    }
}
