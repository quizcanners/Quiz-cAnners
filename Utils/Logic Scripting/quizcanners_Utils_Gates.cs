using QuizCanners.Inspect;
using System;
using UnityEngine;

namespace QuizCanners.Utils
{
    public static class Gate
    {
        public abstract class GateBase 
        {
            public bool ValueIsDefined;
        }

        public abstract class GateGenericBase<T> : GateBase
        {
            protected T previousValue;

            public T CurrentValue => previousValue;

            protected void SetValue(T val) 
            {
                previousValue = val;
                ValueIsDefined = true;
            }

            protected abstract bool DifferentFromPrevious(T newValue);

            public virtual bool IsDirty(T newValue) => (!ValueIsDefined) || (DifferentFromPrevious(newValue));

            public virtual bool TryChange(T value)
            {
                if (!ValueIsDefined)
                {
                    SetValue(value);
                    return true;
                }

                if (!DifferentFromPrevious(value))
                {
                    return false;
                }

                previousValue = value;

                return true;
            }

            public virtual bool TryChange(T value, out bool wasInitialized)
            {
                wasInitialized = ValueIsDefined;
                return TryChange(value);
            }
        }

        public class GateGenericValue<T> : GateGenericBase<T> where T: struct
        {
            protected override bool DifferentFromPrevious(T newValue) 
            {
                return !newValue.Equals(previousValue);
            }
        }

        public class Frame : GateBase
        {
            private int _frameIndex;
            private readonly SystemTime _editorGateTime = new SystemTime();
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
                wasInitialized = ValueIsDefined;
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
                    if (!ValueIsDefined)
                        return false;

                    return _frameIndex == CurrentFrame;
                }
                set
                {
                    ValueIsDefined = true;

                    if (value)
                        _frameIndex = CurrentFrame;
                    else
                        _frameIndex = CurrentFrame - 1;
                }
            }
        }

        public abstract class TimeBase<T> : GateBase, IPEGI
        {
            protected T _lastTime;
            protected double _delta;

            protected abstract T GetCurrent { get; }

            public T LastTime
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

            public double GetSecondsDeltaAndUpdate()
            {
                _delta = GetDeltaWithoutUpdate();
                Update();

                return _delta;
            }

            public void Update() => _lastTime = GetCurrent;

            public bool TryUpdateIfTimePassed(double secondsPassed, out bool wasInitialized)
            {
                wasInitialized = ValueIsDefined;
                return TryUpdateIfTimePassed(secondsPassed);
            }

            public bool TryUpdateIfTimePassed(double secondsPassed)
            {
                if (!WasInitialized())
                    return false;

                var delta = GetDeltaWithoutUpdate();
                if (delta >= secondsPassed)
                {
                    Update();
                    return true;
                }

                return false;
            }

            public double GetDeltaWithoutUpdate()
            {
                if (!WasInitialized())
                    return 0;

                _delta = GetDeltaSeconds_Internal();

                return _delta;
            }

            protected abstract double GetDeltaSeconds_Internal();
            
            protected bool WasInitialized()
            {
                if (ValueIsDefined)
                    return true;

                ValueIsDefined = true;
                Update();
                return false;
            }

            public void Inspect()
            {
                "Delta: ".F(TimeSpan.FromSeconds(GetDeltaWithoutUpdate()).ToShortDisplayString()).PegiLabel().Write();
            }
        }

        public class SystemTime : TimeBase<DateTime>
        {
            protected override DateTime GetCurrent => DateTime.Now;
            protected override double GetDeltaSeconds_Internal() => (GetCurrent - _lastTime).TotalSeconds;
        }

        public class UnityTimeScaled : TimeBase<float>, IPEGI
        {
            protected override float GetCurrent => Time.time;
            protected override double GetDeltaSeconds_Internal() => (GetCurrent - _lastTime);
        }

        public class UnityTimeUnScaled : TimeBase<float>, IPEGI
        {
            protected override float GetCurrent => Time.unscaledTime;
            protected override double GetDeltaSeconds_Internal() => (GetCurrent - _lastTime);
        }

        public class UnityTimeSinceStartup : TimeBase<double>, IPEGI
        {
            protected override double GetCurrent => QcUnity.TimeSinceStartup();
            protected override double GetDeltaSeconds_Internal() => (GetCurrent - _lastTime);
        }

      

        public class Bool : GateGenericBase<bool>
        {
            protected override bool DifferentFromPrevious(bool newValue) => newValue != previousValue;

            public Bool() { }

            public Bool (bool initialValue) 
            {
                SetValue(initialValue);
            }
        }

        public class Integer : GateGenericBase<int>, IGotReadOnlyName
        {
            protected override bool DifferentFromPrevious(int newValue) => newValue != previousValue;

            public Integer() {}
            public Integer(int initialValue)
            {
                SetValue(initialValue);
            }

            public string GetReadOnlyName() => ValueIsDefined ? previousValue.ToString() : "NOT INIT";
        }

        public class Double : GateGenericBase<double>
        {
            protected override bool DifferentFromPrevious(double newValue) => Math.Abs(newValue - previousValue) > double.Epsilon * 10;

            protected virtual bool DifferentFromPrevious(double newValue, double changeTreshold) => Math.Abs(newValue - previousValue) >= changeTreshold;

            public virtual bool IsDirty(double newValue, double changeTreshold) => (!ValueIsDefined) || DifferentFromPrevious(newValue, changeTreshold); // (Math.Abs(newValue - previousValue) >= changeTreshold);

            public bool TryChange(double value, double changeTreshold)
            {
                if (!ValueIsDefined)
                {
                    SetValue(value);
                    return true;
                }

                if (DifferentFromPrevious(value, changeTreshold: changeTreshold)) //Math.Abs(value - previousValue) >= changeTreshold)
                {
                    previousValue = value;
                    return true;
                }

                return false;
            }

            public bool TryChange(double value, double changeTreshold, out bool wasInitialized)
            {
                wasInitialized = ValueIsDefined;
                return TryChange(value, changeTreshold);
            }

            public Double()
            {

            }
            public Double(double initialValue)
            {
                SetValue(initialValue);
            }
        }

        public class Float : GateGenericBase<float>
        {
            protected override bool DifferentFromPrevious(float newValue) => Math.Abs(newValue - previousValue) > float.Epsilon * 10;

            public bool TryChange(float value, float changeTreshold)
            {
                if (!ValueIsDefined)
                {
                    SetValue(value);
                    return true;
                }

                if (Math.Abs(value - previousValue) >= changeTreshold)
                {
                    previousValue = value;
                    return true;
                }

                return false;
            }

            public bool TryChange(float value, float changeTreshold, out bool wasInitialized)
            {
                wasInitialized = ValueIsDefined;
                return TryChange(value, changeTreshold);
            }

            public Float()
            {

            }
            public Float(float initialValue)
            {
                SetValue(initialValue);
            }
        }

        public class ColorValue : GateGenericBase<Color>, IGotReadOnlyName
        {
            protected override bool DifferentFromPrevious(Color newValue) => !newValue.Equals(previousValue);

            public bool TryChange32(Color32 value) => TryChange(value);

            public bool TryChange(Color32 value, out bool wasInitialized)
            {
                wasInitialized = ValueIsDefined;
                return TryChange(value);
            }

            public ColorValue() { }
            public ColorValue(Color32 initialValue)
            {
                SetValue(initialValue);
            }
            public ColorValue(Color initialValue)
            {
                SetValue(initialValue);
            }

            public string GetReadOnlyName() => ValueIsDefined ? previousValue.ToString() : "NOT INIT";
        }

        public class Vector2Value : GateGenericBase<Vector2>
        {
            protected override bool DifferentFromPrevious(Vector2 newValue) => newValue != previousValue;

            public bool TryChange(Vector2 value, double changeTreshold)
            {
                if (!ValueIsDefined)
                {
                    SetValue(value);
                    return true;
                }

                if (Vector2.Distance(value, previousValue) > changeTreshold)
                {
                    previousValue = value;
                    return true;
                }

                return false;
            }

            public bool TryChange(Vector2 value, double changeTreshold, out bool wasInitialized)
            {
                wasInitialized = ValueIsDefined;
                return TryChange(value, changeTreshold);
            }

            public Vector2Value()
            {

            }
            public Vector2Value(Vector2 initialValue)
            {
                SetValue(initialValue);
            }
        }


        public class Vector3Value : GateGenericBase<Vector3>
        {
            protected override bool DifferentFromPrevious(Vector3 newValue) => newValue != previousValue;

            public bool TryChange(Vector3 value, double changeTreshold)
            {
                if (!ValueIsDefined)
                {
                    SetValue(value);
                    return true;
                }

                if (Vector3.Distance(value, previousValue)> changeTreshold)
                {
                    previousValue = value;
                    return true;
                }

                return false;
            }

            public bool TryChange(Vector3 value, double changeTreshold, out bool wasInitialized)
            {
                wasInitialized = ValueIsDefined;
                return TryChange(value, changeTreshold);
            }

            public Vector3Value()
            {

            }
            public Vector3Value(Vector3 initialValue)
            {
                SetValue(initialValue);
            }
        }

        public class Vector4Value : GateGenericBase<Vector4>
        {
            protected override bool DifferentFromPrevious(Vector4 newValue) => newValue != previousValue;

            public bool TryChange(Vector4 value, double changeTreshold)
            {
                if (!ValueIsDefined)
                {
                    SetValue(value);
                    return true;
                }

                if (Vector3.Distance(value, previousValue) > changeTreshold)
                {
                    previousValue = value;
                    return true;
                }

                return false;
            }

            public bool TryChange(Vector4 value, double changeTreshold, out bool wasInitialized)
            {
                wasInitialized = ValueIsDefined;
                return TryChange(value, changeTreshold);
            }

            public Vector4Value()
            {

            }
            public Vector4Value(Vector4 initialValue)
            {
                SetValue(initialValue);
            }
        }

        public class QuaternionValue : GateGenericBase<Quaternion>
        {
            protected override bool DifferentFromPrevious(Quaternion newValue) => newValue != previousValue;

            public bool TryChange(Quaternion value, double changeTreshold)
            {
                if (!ValueIsDefined)
                {
                    SetValue(value);
                    return true;
                }

                if (Quaternion.Angle(value, previousValue) > changeTreshold)
                {
                    previousValue = value;
                    return true;
                }

                return false;
            }

            public bool TryChange(Quaternion value, double changeTreshold, out bool wasInitialized)
            {
                wasInitialized = ValueIsDefined;
                return TryChange(value, changeTreshold);
            }

            public QuaternionValue()
            {

            }
            public QuaternionValue(Quaternion initialValue)
            {
                SetValue(initialValue);
            }
        }

        public class DirtyVersion 
        {
            private int _dataVersion = 0;
            public int Version { get; private set; } = -1;

            public bool TryClear() 
            {
                if (IsDirty) 
                {
                    IsDirty = false;
                    return true;
                }

                return false;
            }


            public bool TryClear(int versionDifference)
            {
                if ((_dataVersion - Version) >= versionDifference)
                {
                    IsDirty = false;
                    return true;
                }

                return false;
            }


            public bool IsDirty
            {
                get =>  Version != _dataVersion;
                set
                {
                    if (value)
                        _dataVersion++;
                    else
                        Version = _dataVersion;
                }
            }

        }
    }
}
