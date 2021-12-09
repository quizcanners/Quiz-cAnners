using QuizCanners.Inspect;
using System;
using UnityEngine;

namespace QuizCanners.Utils
{
    public static class Gate
    {
        public abstract class GateBase 
        {
            protected bool initialized;
            public bool ValueIsDefined => initialized;
        }

        public abstract class GateGeneric<T> : GateBase
        {
            protected T previousValue;
            public virtual bool IsDirty(T newValue) => (!initialized) || (!newValue.Equals(previousValue));

            public virtual bool TryChange(T value)
            {
                if (!initialized)
                {
                    initialized = true;
                    previousValue = value;
                    return true;
                }

                if (value.Equals(previousValue))
                {
                    return false;
                }

                previousValue = value;

                return true;
            }

            public virtual bool TryChange(T value, out bool wasInitialized)
            {
                wasInitialized = initialized;
                return TryChange(value);
            }

        }

        public class Frame : GateBase
        {
            private int _frameIndex;
            private readonly Time _editorGateTime = new Time();
            private int _editorFrames;

            public bool TryEnter()
            {
                if (DoneThisFrame)
                    return false;

                DoneThisFrame = true;
                return true;
            }

            public bool TryEnter(out bool wasInitialized)
            {
                wasInitialized = initialized;
                return TryEnter();
            }

            private int CurrentFrame
            {
                get
                {
                    if (!Application.isEditor)
                        return UnityEngine.Time.frameCount;

                    if (_editorGateTime.TryUpdateIfTimePassed(0.01f))
                        _editorFrames++;
                        
                    return UnityEngine.Time.frameCount + _editorFrames;
                }
            }

            public bool DoneThisFrame
            {
                get
                {
                    if (!initialized)
                        return false;

                    return _frameIndex == CurrentFrame;
                }
                set
                {
                    initialized = true;

                    if (value)
                        _frameIndex = CurrentFrame;
                    else
                        _frameIndex = CurrentFrame - 1;
                }
            }
        }

        public class Time : GateBase, IPEGI
        {
            private DateTime _lastTime = new DateTime();
            private double _delta;

            public double GetSecondsDeltaAndUpdate()
            {
                _delta = GetDeltaWithoutUpdate();
                _lastTime = DateTime.Now;

                return _delta;
            }

            public DateTime LastTime
            {
                get
                {
                    WasInitialized();
                    return _lastTime;
                }
                set
                {
                    WasInitialized();
                    _lastTime = value;
                }
            }

            public bool TryUpdateIfTimePassed(double secondsPassed, out bool wasInitialized)
            {
                wasInitialized = initialized;
                return TryUpdateIfTimePassed(secondsPassed);
            }

            public bool TryUpdateIfTimePassed(double secondsPassed)
            {
                if (!WasInitialized())
                    return false;

                var delta = GetDeltaWithoutUpdate();
                if (delta >= secondsPassed)
                {
                    _lastTime = DateTime.Now;
                    return true;
                }

                return false;
            }

            public double GetDeltaWithoutUpdate()
            {
                if (!WasInitialized())
                    return 0;

                _delta = Math.Max(0, (DateTime.Now - _lastTime).TotalSeconds);
                
                return _delta;
            }

            private bool WasInitialized()
            {
                if (initialized)
                    return true;

                initialized = true;
                _lastTime = DateTime.Now;
                return false;

            }

            public void Inspect()
            {

                "Delta: ".F(TimeSpan.FromSeconds(GetDeltaWithoutUpdate()).ToShortDisplayString()).PegiLabel().write();
            }
        }

        public class Bool : GateGeneric<bool>
        {

            public bool CurrentValue => previousValue;

            public Bool() { }

            public Bool (bool value) 
            {
                initialized = true;
                previousValue = value;
            }
        }

        public class Integer : GateGeneric<int>, IGotReadOnlyName
        {
            public int CurrentValue => previousValue;

            public Integer()
            {

            }
            public Integer(int initialValue)
            {
                previousValue = initialValue;
                initialized = true;
            }

            public string GetReadOnlyName() => initialized ? previousValue.ToString() : "NOT INIT";
        }

        public class Double : GateBase
        {
            private double _previousValue;

            public double Value => _previousValue;

            public bool TryChange(double value)
            {
                if (!initialized)
                {
                    initialized = true;
                    _previousValue = value;
                    return true;
                }

                if (Math.Abs(value - _previousValue) < double.Epsilon * 10)
                {
                    _previousValue = value;
                    return false;
                }

                _previousValue = value;

                return true;
            }

            public bool TryChange(double value, out bool wasInitialized)
            {
                wasInitialized = initialized;
                return TryChange(value);
            }

            public bool TryChange(double value, double changeTreshold)
            {
                if (!initialized)
                {
                    initialized = true;
                    _previousValue = value;
                    return true;
                }

                if (Math.Abs(value - _previousValue) >= changeTreshold)
                {
                    _previousValue = value;
                    return true;
                }

                return false;
            }

            public bool TryChange(double value, double changeTreshold, out bool wasInitialized)
            {
                wasInitialized = initialized;
                return TryChange(value, changeTreshold);
            }

            public Double()
            {

            }
            public Double(double initialValue)
            {
                TryChange(initialValue);
            }
        }

        public class ColorValue : GateBase, IGotReadOnlyName
        {
            private Color32 _previousValue;

            public bool TryChange(Color value) => TryChange_Internal(value);
            public bool TryChange(Color32 value) => TryChange_Internal(value);

            public bool TryChange_Internal(Color32 value)
            {
                if (!initialized)
                {
                    initialized = true;
                    _previousValue = value;
                    return true;
                }

                if (value.Equals(_previousValue))
                    return false;

                _previousValue = value;

                return true;
            }

            public bool TryChange(Color32 value, out bool wasInitialized)
            {
                wasInitialized = initialized;
                return TryChange(value);
            }

            public Color32 CurrentValue => _previousValue;

            public ColorValue()
            {

            }
            public ColorValue(Color32 initialValue)
            {
                _previousValue = initialValue;
                initialized = true;
            }

            public ColorValue(Color initialValue)
            {
                _previousValue = initialValue;
                initialized = true;
            }

            public string GetReadOnlyName() => initialized ? _previousValue.ToString() : "NOT INIT";
        }

        public class Vector3Value : GateBase
        {
            private Vector3 _previousValue;

            public Vector3 Value => _previousValue;

            public bool TryChange(Vector3 value)
            {
                if (!initialized)
                {
                    initialized = true;
                    _previousValue = value;
                    return true;
                }

                if (value == _previousValue)
                {
                    return false;
                }

                _previousValue = value;

                return true;
            }

            public bool TryChange(Vector3 value, out bool wasInitialized)
            {
                wasInitialized = initialized;
                return TryChange(value);
            }

            public bool TryChange(Vector3 value, double changeTreshold)
            {
                if (!initialized)
                {
                    initialized = true;
                    _previousValue = value;
                    return true;
                }

                if (Vector3.Distance(value, _previousValue)> changeTreshold)
                {
                    _previousValue = value;
                    return true;
                }

                return false;
            }

            public bool TryChange(Vector3 value, double changeTreshold, out bool wasInitialized)
            {
                wasInitialized = initialized;
                return TryChange(value, changeTreshold);
            }

            public Vector3Value()
            {

            }
            public Vector3Value(Vector3 initialValue)
            {
                TryChange(initialValue);
            }
        }

        public class Vector4Value : GateBase
        {
            private Vector4 _previousValue;

            public Vector4 Value => _previousValue;

            public bool TryChange(Vector4 value)
            {
                if (!initialized)
                {
                    initialized = true;
                    _previousValue = value;
                    return true;
                }

                if (value == _previousValue)
                {
                    return false;
                }

                _previousValue = value;

                return true;
            }

            public bool TryChange(Vector4 value, out bool wasInitialized)
            {
                wasInitialized = initialized;
                return TryChange(value);
            }

            public bool TryChange(Vector4 value, double changeTreshold)
            {
                if (!initialized)
                {
                    initialized = true;
                    _previousValue = value;
                    return true;
                }

                if (Vector4.Distance(value, _previousValue) > changeTreshold)
                {
                    _previousValue = value;
                    return true;
                }

                return false;
            }

            public bool TryChange(Vector4 value, double changeTreshold, out bool wasInitialized)
            {
                wasInitialized = initialized;
                return TryChange(value, changeTreshold);
            }

            public Vector4Value()
            {

            }
            public Vector4Value(Vector4 initialValue)
            {
                TryChange(initialValue);
            }
        }

    }
}
