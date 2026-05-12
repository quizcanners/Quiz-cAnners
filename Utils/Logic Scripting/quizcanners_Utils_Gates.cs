using QuizCanners.Inspect;
using System;
using UnityEngine;

namespace QuizCanners.Utils
{
    public static class Gate
    {
        public abstract class GateBase 
        {
            public virtual bool ValueIsDefined { get; set; }

            public bool IsUndefined => !ValueIsDefined;
        }

        public abstract class GenericBase<T> : GateBase
        {
            protected T previousValue;

            public T CurrentValue => _valueIsDefined ? previousValue : default;

            private bool _valueIsDefined;
            public override bool ValueIsDefined 
            { 
                get => _valueIsDefined; 
                set
                {
                    _valueIsDefined = value;
                    if (!value)
                        previousValue = default;
                } 
            }

            public override string ToString() => ValueIsDefined ? previousValue.ToString() : "UNDEFINED {0}".F(typeof(T));

            protected void SetValue(T val) 
            {
                previousValue = val;
                ValueIsDefined = true;
            }

            protected abstract bool IsSameAsPrevious(T newValue);

            public bool IsDefinedAs(T value) => ValueIsDefined && IsSameAsPrevious(value);

            public bool IsDirty(T comparedTo) => !IsDefinedAs(comparedTo); //ValueIsDefined && IsSameAsPrevious(comparedTo));

            public virtual bool TryChange(T value)
            {
                if (!ValueIsDefined)
                {
                    SetValue(value);
                    return true;
                }

                if (IsSameAsPrevious(value))
                    return false;

                previousValue = value;

                return true;
            }

            public virtual bool TryChange(T value, out bool wasInitialized)
            {
                wasInitialized = ValueIsDefined;
                return TryChange(value);
            }
        }

        public class GenericValue<T> : GenericBase<T>
        {
            protected override bool IsSameAsPrevious(T newValue) 
            {
                if (newValue == null)
                    return previousValue == null;

                return newValue.Equals(previousValue);
            }
        }

        public class OncePerFrameUpdate<T>
        {
            private readonly Gate.Frame _check = new();
            private T _cached;
            public T Value
            {
                get => _cached;
                set
                {
                    _cached = value;
                    _check.TryConsume();
                }
            }

            public bool TryEnter() => _check.TryConsume();
        }

        public class Frame : GateBase
        {
            private int _startFrame;
            private readonly SystemTime _editorGateTime = new();
            private int _editorFrames;

            public bool DoneThisFrame
            {
                get
                {
                    if (!ValueIsDefined)
                        return false;

                    return _startFrame == CurrentFrame;
                }
            }

            public void Start()
            {
                ValueIsDefined = true;
                _startFrame = CurrentFrame;
            }

            // TRUE / CHECK / CHECK
            public bool TryConsume_IfElapsedOrFirst(int frames)
            {
                if (!ValueIsDefined)
                {
                    Start();
                    return true;
                }

                if ((CurrentFrame - _startFrame) < frames)
                    return false;

                Start();
                return true;
            }

            // FALSE / CHECK / CHECK
            public bool TryConsume_RestartIfFirst(float frames)
            {
                if (!ValueIsDefined)
                {
                    Start();
                    return false;
                }

                if ((CurrentFrame - _startFrame) < frames)
                    return false;

                Start();
                return true;
            }

            public bool TryPeekElapsed(out int frames)
            {
                if (!ValueIsDefined)
                {
                    frames = 0;
                    return false;
                }
                frames = CurrentFrame - _startFrame;
                return true;
            }

            public int Consume()
            {
                var passed = (CurrentFrame - _startFrame);
                Start();
                return passed;
            }

            public bool TryConsume()
            {
                if (DoneThisFrame)
                    return false;

                Start();
                return true;
            }

            public bool IsFramesPassed_SinceStart(int frameCount)
            {
                if (!ValueIsDefined)
                    return false;

                return (CurrentFrame - _startFrame) >= frameCount;
            }

            public bool IsFramesPassed_OrNotStarted(int frameCount)
            {
                if (!ValueIsDefined)
                    return true;

                return (CurrentFrame - _startFrame) >= frameCount;
            }

            public bool IsStartedWithin(int frameCount)
            {
                if (!ValueIsDefined)
                    return false;

                return (CurrentFrame - _startFrame) < frameCount;
            }

            private int CurrentFrame
            {
                get
                {
#if UNITY_EDITOR
                    if (Application.isPlaying)
                        return Time.frameCount;

                    if (_editorGateTime.TryConsume_IfElapsedOrFirst(0.01f))
                        _editorFrames++;
                        
                    return Time.frameCount + _editorFrames;
#else
                    return Time.frameCount;
#endif
                }
            }

            public Frame() { }
        }

        public abstract class TimeBase : GateBase, IPEGI
        {
            public abstract void Start();

            // TRUE / CHECK / CHECK
            public bool TryConsume_IfElapsedOrFirst(float secondsPassed)
            {
                if (!ValueIsDefined)
                {
                    Start();
                    return true;
                }

                if (ElapsedSeconds_Internal() < secondsPassed)
                    return false;

                Start();
                return true;
            }

            // FALSE / CHECK / CHECK
            public bool TryConsume_RestartIfFirst(float secondsPassed)
            {
                if (!ValueIsDefined)
                {
                    Start();
                    return false;
                }

                if (ElapsedSeconds_Internal() < secondsPassed)
                    return false;

                Start();
                return true;
            }

            public bool IsStartedWithin(float secondsPassed)
            {
                if (!ValueIsDefined)
                    return false;

                return ElapsedSeconds_Internal() < secondsPassed;
            }

            public bool IsPassed_OrNeverStarted(float secondsPassed)
            {
                if (!ValueIsDefined)
                    return true;

                return ElapsedSeconds_Internal() >= secondsPassed;
            }

            public bool IsPassed_SinceStart(float secondsPassed)
            {
                if (!ValueIsDefined)
                    return false;

                return ElapsedSeconds_Internal() >= secondsPassed;
            }

            // Start if Unstarted?
            // Return true if unstarted?
            // Start if returning true?
            // True if time passed?

            public float ElapsedSeconds
            {
                get
                {
                    if (!ValueIsDefined)
                        return 0;

                    return ElapsedSeconds_Internal();
                }
            }

            public bool TryPeekElapsed(out float time)
            {
                if (!ValueIsDefined)
                {
                    time = default;
                    return false;
                }
                time = ElapsedSeconds_Internal();
                return true;
            }

            // 0 / DELTA / 2 x DELTA

            public float Peek_OrFirstStart()
            {
                if (!ValueIsDefined)
                {
                    Start();
                    return 0;
                }

                return ElapsedSeconds_Internal();
            }

            // 0 / DELTA / DELTA
            public float Consume()
            {
                if (!ValueIsDefined)
                {
                    Start();
                    return 0;
                }

                var delta = ElapsedSeconds_Internal();
                Start();

                return delta;
            }

            // FALSE / EXT_START / INV_CHECK / 2xINV_CHECK

  

            protected abstract float ElapsedSeconds_Internal();

            #region Inspector

            void IPEGI.Inspect()
            {
                "Delta: ".F(TimeSpan.FromSeconds(Peek_OrFirstStart()).ToShortDisplayString()).PL().Write();
            }

            #endregion

            public TimeBase()
            {

            }
        }

        public abstract class TimeGeneric<T> : TimeBase
        {
            protected T _lastTime;
           
            protected abstract T GetCurrent { get; }

            public override void Start()
            {
                ValueIsDefined = true;
                _lastTime = GetCurrent;
            }

            public TimeGeneric() : base() { }
        }

        public class SystemTime : TimeGeneric<DateTime>
        {
            protected override DateTime GetCurrent => DateTime.Now;
            protected override float ElapsedSeconds_Internal() => (float)((GetCurrent - _lastTime).TotalSeconds);


        }

        public class UnityTimeScaled : TimeGeneric<float>, IPEGI
        {
            protected override float GetCurrent => Time.time;
            protected override float ElapsedSeconds_Internal() => (GetCurrent - _lastTime);

        }

        public class UnityTimeUnScaled : TimeGeneric<float>, IPEGI
        {
            protected override float GetCurrent => Time.unscaledTime;
            protected override float ElapsedSeconds_Internal() => (GetCurrent - _lastTime);


            public UnityTimeUnScaled() : base() { }
        }

        public class UnityTimeSinceStartup : TimeGeneric<float>, IPEGI
        {
            protected override float GetCurrent => (float)QcUnity.TimeSinceStartup();
            protected override float ElapsedSeconds_Internal() => (GetCurrent - _lastTime);

        }

      

        public class Bool : GenericBase<bool>
        {
            protected override bool IsSameAsPrevious(bool newValue) => newValue == previousValue;

            public Bool() { }

            public Bool (bool initialValue) 
            {
                SetValue(initialValue);
            }
        }

        public class Integer : GenericBase<int>
        {
            protected override bool IsSameAsPrevious(int newValue) => newValue == previousValue;

            public Integer() {}
            public Integer(int initialValue)
            {
                SetValue(initialValue);
            }

            public override string ToString() => ValueIsDefined ? previousValue.ToString() : "NOT INIT";
        }

        public class UnsignedInteger : GenericBase<uint>
        {
            protected override bool IsSameAsPrevious(uint newValue) => newValue == previousValue;

            public UnsignedInteger() { }
            public UnsignedInteger(uint initialValue)
            {
                SetValue(initialValue);
            }

            public override string ToString() => ValueIsDefined ? previousValue.ToString() : "NOT INIT";
        }

        public class Double : GenericBase<double>
        {
            protected override bool IsSameAsPrevious(double newValue) => Math.Abs(newValue - previousValue) <= double.Epsilon * 10;

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

        public class Float : GenericBase<float>
        {
            protected override bool IsSameAsPrevious(float newValue) => Math.Abs(newValue - previousValue) <= float.Epsilon * 10;

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

        public class String : GenericBase<string>
        {
            protected override bool IsSameAsPrevious(string newValue)
            {
                var newIsNull = newValue.IsNullOrEmpty();
                var oldIsNull = previousValue.IsNullOrEmpty();

                if (newIsNull && oldIsNull) // Both are null - same
                    return true;

                if (newIsNull || oldIsNull) // One is null - different
                    return false;

                return newValue.Equals(previousValue);
            }
            public String()
            {

            }

            public String(string initialValue)
            {
                SetValue(initialValue);
            }
        }


        public class ColorValue : GenericBase<Color>
        {
            protected override bool IsSameAsPrevious(Color newValue) => newValue.Equals(previousValue);

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

            public override string ToString() => ValueIsDefined ? previousValue.ToString() : "NOT INIT";
        }

        public class Vector2Value : GenericBase<Vector2>
        {
            protected override bool IsSameAsPrevious(Vector2 newValue) => newValue == previousValue;

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

        public class Vector3Value : GenericBase<Vector3>
        {
            protected override bool IsSameAsPrevious(Vector3 newValue) => newValue == previousValue;

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

        public class Vector4Value : GenericBase<Vector4>, IPEGI
        {
            protected override bool IsSameAsPrevious(Vector4 newValue) => newValue == previousValue;

            public bool TryChange(Vector4 value, double changeTreshold)
            {
                if (!ValueIsDefined)
                {
                    SetValue(value);
                    return true;
                }

                if (Vector4.Distance(value, previousValue) > changeTreshold)
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

            void IPEGI.Inspect()
            {
                "Previous: {0}".F(previousValue).PL().Write();
            }

            public Vector4Value()
            {

            }
            public Vector4Value(Vector4 initialValue)
            {
                SetValue(initialValue);
            }
        }

        public class QuaternionValue : GenericBase<Quaternion>
        {
            protected override bool IsSameAsPrevious(Quaternion newValue) => newValue == previousValue;

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

        public class ScreenSizeValue
        {
            private int _width;
            private int _height;

            private bool _isSet;

            private static readonly PerformanceTurnTable.Token _performanceToken = new("Screen Size", delay: 0.1f);

            public bool IsDirty => !_isSet || Screen.width != _width || Screen.height != _height;

            private void Change() 
            {
                _width = Screen.width;
                _height = Screen.height;
                _isSet = true;
            }

            public bool TryChange_Now() 
            {
                if (!IsDirty)
                    return false;

                Change();
                return true;
            }

            public void Clear() => _isSet = false;

            public bool TryChange_Performant() 
            {
                if (!IsDirty)
                    return false;

                if (_isSet && !_performanceToken.TryGetTurn())
                    return false;

                Change();

                return true;
            }
        }

        public class DirtyVersion 
        {
            private int _clearedVersion = 0;
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
                if ((Version - _clearedVersion) >= versionDifference)
                {
                    IsDirty = false;
                    return true;
                }

                return false;
            }

            public bool IsDirty
            {
                get =>  Version != _clearedVersion;
                set
                {
                    if (value)
                        Version++;
                    else
                        _clearedVersion = Version;
                }
            }
        }
    }
}
