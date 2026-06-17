using QuizCanners.Inspect;
using QuizCanners.Lerp;
using System;
using System.Threading.Tasks;
using UnityEngine;


namespace QuizCanners.Utils
{
    public static class LogicWrappers
    {
        public class MinMaxEnableGap : IPEGI
        {
            private float _enableAt, _disableAt;
            private bool disableWhenBigger;
            private readonly Gate.Bool _enableGate = new();

            public bool Enabled
            {
                get => _enableGate.CurrentValue;
                set => _enableGate.TryChange(value);
            }

            private bool IsEnabled_Internal(float value)
            {
                bool isEnabled = _enableGate.CurrentValue;

                if (isEnabled)
                {
                    bool isAboveDisable = value > _disableAt;
                    bool shouldDisable = isAboveDisable == disableWhenBigger;
                    return !shouldDisable;
                }
                else
                {
                    bool isAboveEnable = value > _enableAt;
                    return isAboveEnable == !disableWhenBigger;
                }
            }

            public bool TryChange_ByResult(bool v) => _enableGate.TryChange(v);
            
            /*
            public bool IsEnabled(float value) 
            {
                var isEnabled = IsEnabled_Internal(value);
                _enableGate.TryChange(isEnabled);
                return isEnabled;
            }*/

            public bool TryChange(float value, out bool isEnabled) 
            {
                isEnabled = IsEnabled_Internal(value);
                return _enableGate.TryChange(isEnabled);
            }

            public void Reset() => _enableGate.ValueIsDefined = false;

            public bool TryChange(float value, out bool isEnabled, out bool wasInitialized)
            {
                wasInitialized = _enableGate.ValueIsDefined;
                isEnabled = IsEnabled_Internal(value);
                return _enableGate.TryChange(isEnabled);
            }

            #region Inspector

            //  private float _testValue;

            public void Inspect()
            {
                var changes = pegi.ChangeTrackStart();

                "Disable at".ConstL().Edit(ref _disableAt).NL();
                "Enable at".ConstL().Edit(ref _enableAt).NL();

                if (changes)
                    disableWhenBigger = _disableAt > _enableAt;

                float mid = (_disableAt + _enableAt) * 0.5f;
                float diff = Mathf.Abs(_disableAt - mid);

                /*
                "Value".ConstL().Edit(ref _testValue, mid - diff*2, mid + diff*2);
                if (TryChange(_testValue, out bool isEnabled)) 
                {
                    Debug.Log("Changed");
                }*/

                (_enableGate.CurrentValue ? Icon.Active : Icon.InActive).Draw().NL();

            }

      



            #endregion

            public MinMaxEnableGap(float enableAt, float disableAt) 
            {
                _enableAt = enableAt;
                _disableAt = disableAt;
                disableWhenBigger = disableAt > enableAt;
            }
        }

        public class Request
        {
            private bool _requestCreated;
            private bool _requestActualized;

            public void CreateRequest() => _requestCreated = true;
            public bool IsRequested => _requestCreated;

            public bool TryUseRequest(string debugReason) 
            {
                var result = _requestCreated;
                _requestCreated = false;

               if (result && Application.isEditor)
                   Debug.Log("Request was used by {0}".F(debugReason));

                _requestActualized = false;

                return result;
            }

            public bool TryUseRequest()
            {
                if (!_requestCreated)
                    return false;

                _requestCreated = false;
                _requestActualized = false;
                return true;
            }

            public void ClearOrActualizeActualize() 
            {
                if (_requestActualized)
                {
                    _requestActualized = false;
                    _requestCreated = false;
                }  else if (_requestCreated)
                        _requestActualized = true;
            }

            public void Feed(bool createRequest) 
            {
                _requestCreated |= createRequest;
            }
        }

        public class NamedRequest
        {
            public string Reason { get; private set; }

            private bool _requestCreated;

            public void CreateRequest(string name)
            {
                _requestCreated = true;
                Reason = name;
            }

            public bool IsRequested => _requestCreated;

            public bool TryUseRequest()
            {
                var result = _requestCreated;
                _requestCreated = false;
                return result;
            }
        }

        public class CountDown 
        {
            public int Count { get; private set; }
            public bool IsFinished => Count <= 0;
            public void RemoveOne() => Count--;

            public void Clear() => Count = 0;

            public void ResetCountDown(int newCount) => Count = newCount;
            public void AddToCountdown(int valueToAdd) => Count += valueToAdd;
        }

        public class Timer 
        {
            private float _endTime;

            public void Restart(float seconds) 
            {
                IsInitialized = true;
                _endTime = Time.time + seconds;
            }

            public void SetIfBigger(float seconds)
            {
                IsInitialized = true;
                _endTime = Mathf.Max(Time.time + seconds, _endTime);
            }

            public void Clear() => IsInitialized = false;

            public bool IsInitialized { get; private set; }  
            public bool IsFinished => Time.time >= _endTime;

        }

        [Serializable]
        public class CountUpToMax : IPEGI
        {
            [SerializeField] private int _maxCount;
            [NonSerialized] private int _count;

            public int Count => _count;

            public bool IsFinished
            {
                get => _count >= _maxCount;
                set 
                {
                    _count = value ? _maxCount : 0;
                }
            }

            public void AddOne() => _count++;

            public void Restart() => _count = 0;

            void IPEGI.Inspect()
            {
                if (!Application.isPlaying && "Max Count".ConstL().Edit_Delayed(ref _maxCount).NL())
                    _maxCount = Math.Max(1, _maxCount);

                if (IsFinished)
                    "Finished".ConstL().NL();
                else
                    "{0}% ({1}/{2})".F(_count*100 / _maxCount, _count, _maxCount).NL();
            }

            public CountUpToMax(int maxCount) 
            {
                _maxCount = maxCount;
                _count = 0;
            }

        }

        [Serializable]
        public class CountDownFromMax : IPEGI
        {
            [SerializeField] private int _maxCount;
            [NonSerialized] private int _count;

            public int Count => _count;
            public int MaxCount => _maxCount;

            public int CompletedCount => _maxCount - _count;

            public bool IsFinished
            {
                get => _count <= 0;
                set
                {
                    _count = value ? 0 : _maxCount;
                }
            }

            public float Remaining01 => _maxCount > 0 ? ((float)_count / _maxCount) : 1;

            public void RemoveOne() => _count--;

            public void Restart() => _count = _maxCount;

            void IPEGI.Inspect()
            {
                if (!Application.isPlaying && "Count".ConstL().Edit_Delayed(ref _maxCount).NL())
                    _maxCount = Math.Max(1, _maxCount);

                ToString().NL();
            }

            public override string ToString()
            {
                if (IsFinished)
                    return "Finished";
                else
                    return "{0}% ({1}/{2})".F((_maxCount - _count) * 100 / _maxCount, _count, _maxCount);
            }

            public CountDownFromMax(int maxCount)
            {
                _maxCount = maxCount;
                _count = 0;
            }
        }

        public class TimeFixedSegmenter 
        {
            private readonly bool _unscaledTime;
            private bool _timeSet;
            private float _lastTime;
            private float _defaultSegment = 1;
            private readonly int _returnOnFirstRequest;
            private bool _defaultSegmentSet;

            public float SegmentDuration
            {
                get => _defaultSegment;
                set 
                { 
                    _defaultSegment = value;
                    _defaultSegmentSet = true;
                }
            }

            float CurrentTime => _unscaledTime ? Time.unscaledTime : Time.time;
          
            public void Reset() 
            {
                _timeSet = false;
            }

            public void Update_ClearFraction() 
            {
                _timeSet = true;
                _lastTime = CurrentTime;
            }

            public void Update_KeepFraction()
            {
#if UNITY_EDITOR
                CheckDefaultSegment();
#endif

                GetSegmentsAndUpdate(_defaultSegment);
            }

            public void Update_KeepFraction(float segment) => GetSegmentsAndUpdate(segment);

            public void UseSegment(int count = 1) 
            {
                _lastTime += _defaultSegment * count;
            }

            public float GetTimePassed() 
            {
                if (!_timeSet) 
                {
                    GetSegmentsWithouUpdate();
                    return 0;
                }

                return CurrentTime - _lastTime;
            }

            public void ClearDeltaTime() 
            {
                _lastTime = CurrentTime;
            }

            public int GetSegmentsWithouUpdate() 
            {
                if (!_timeSet)
                {
                    _timeSet = true;
                    _lastTime = CurrentTime;
                    return _returnOnFirstRequest;
                }

                var gap = CurrentTime - _lastTime;
                var segments = Mathf.FloorToInt(gap / _defaultSegment);
                return segments;
            }

            public int GetSegmentsWithouUpdate(float segment) 
            {
                SegmentDuration = segment;
                return GetSegmentsWithouUpdate();
            }

            public int GetSegmentsAndUpdate()
            {
#if UNITY_EDITOR
                CheckDefaultSegment();
#endif
                return GetSegmentsAndUpdate(_defaultSegment);
            }

            public int GetSegmentsAndUpdate(float segmentLength) 
            {
                var segmentCount = GetSegmentsWithouUpdate(segmentLength);
                _lastTime += segmentCount * segmentLength;
                return segmentCount;
            }

            private void CheckDefaultSegment() 
            {
                if (!_defaultSegmentSet)
                {
                    QcLog.ChillLogger.LogErrorOnce("Default Segment was not set", key: "No Dflt Sgm");
                }
            }

            public TimeFixedSegmenter(bool unscaledTime, float defaultSegmentLength, int returnOnFirstRequest) 
            {
                _unscaledTime = unscaledTime;
                _returnOnFirstRequest = returnOnFirstRequest;
                _defaultSegment = defaultSegmentLength;
                _defaultSegmentSet = true;
            }
        }

        public class TaskResultWaiter<T> : IPEGI, IPEGI_ListInspect, IGotStringId
        {
            private string _name;
            private Task<T> _task;
            private T result;

            public bool IsRunning => _task != null && (_task.Status == TaskStatus.Running || _task.Status == TaskStatus.WaitingForActivation || _task.Status == TaskStatus.WaitingForChildrenToComplete || _task.Status == TaskStatus.WaitingToRun || _task.Status == TaskStatus.Created);

            public string Status
            {
                get
                {
                    var stat = GetStatusEnum();
                    return stat switch
                    {
                        StatusEnum.Other => _task.Status.ToString(),
                        _ => stat.ToString(),
                    };
                }
            }

            public StatusEnum GetStatusEnum() 
            {
                if (TryGetValue(out _))
                    return StatusEnum.Ready;
                else if (_task == null)
                    return StatusEnum.NotStarted;
                else if (_task.Status == TaskStatus.WaitingForActivation)
                    return StatusEnum.Yielding;
                else
                    return StatusEnum.Other;
            }

            public enum StatusEnum { Ready, NotStarted, Yielding, Other }

            public string StringId { get => _name; set => _name = value; }

            public bool TryGetValue(out T val)
            {
                if (_task != null && _task.IsCompleted)
                {
                    try
                    {
                        result = _task.Result;
                    }
                    finally
                    {
                        _task = null;
                    }
                }

                val = result;

                return val != null;
            }

            public void Set(Task<T> task, string name = null)
            {
                result = default;
                _task = task;
                if (name.IsNullOrEmpty())
                    _name = QcSharp.AddSpacesInsteadOfCapitals(typeof(T).ToPegiStringType());
                else
                    _name = name;
            }

            #region Inspector
            void IPEGI.Inspect()
            {
                if (result != null)
                {
                    pegi.Nested_Inspect_Value_OrFallback(ref result);
                }
                else
                {
                    Status.NL();
                }
            }

            public void InspectInList(ref int edited, int index)
            {
                var enm = GetStatusEnum();
                switch (enm) 
                {
                    case StatusEnum.Ready: Icon.Done.Draw(); break;
                    case StatusEnum.Yielding: Icon.Wait.Draw(); break;
                    case StatusEnum.NotStarted: Icon.InActive.Draw(); break;
                    case StatusEnum.Other: Icon.Warning.Draw(enm.ToString()); break;
                }

                if (result != null)
                {
                    if (result is IPEGI_ListInspect asIls)
                    {
                        asIls.InspectInList_Nested(ref edited, index);
                        return;
                    }
                }

                if (ToString().PL().ClickLabel() | Icon.Enter.Click())
                    edited = index;
            }

            public override string ToString() => "{0}".F(_name.IsNullOrEmpty() ? typeof(T).ToPegiStringType() : _name);


            #endregion

            public TaskResultWaiter(string name = null)
            {
                _name = name;
            }

            public TaskResultWaiter(Task<T> task, string name = null)
            {
                Set(task, name);

            }
        }

        public class DeltaPositionSegments 
        {
            private bool _initialized;
            Vector3 _previousPosition;

            public bool TryGetSegments(Vector3 newPosition, float delta, out Vector3[] points) 
            {
                if (!_initialized) 
                {
                    Reset(newPosition);
                    points = null;
                    return false;
                }

                var segment = (newPosition - _previousPosition);

                float totalDistance = segment.magnitude;

                int count = Mathf.FloorToInt(totalDistance / delta);
                points = new Vector3[count];

                if (count == 0) 
                    return false;
                
                float fraction = (delta * count) / totalDistance;

                var direction = segment.normalized;

                for (int i = 0; i < count; i++)
                    points[i] = _previousPosition + (i + 1) * delta * direction;

                _previousPosition = Vector3.Lerp(_previousPosition, newPosition, fraction);

                return true;
            }

            public void Reset(Vector3 position) 
            {
                _previousPosition = position;
                _initialized = true;
            }

        }

        public class DeltaVector3
        {
            public Vector3 Previous;
            public Vector3 Current;

            private bool _initialized;

            public Vector3 Delta => Current - Previous;

            public void Reset(Vector3 value) 
            {
                _initialized = true;
                Previous = value;
                Current = value;
            }

            public void Update(Vector3 newValue) 
            {
                if (!_initialized)
                {
                    Reset(newValue);
                    return;
                }

                Previous = Current;
                Current = newValue;
            }

            public bool TryUpdateIfChanged(Vector3 newValue) 
            {
                if (!_initialized) 
                {
                    Reset(newValue);
                    return false;
                }

                if (newValue != Current) 
                {
                    Update(newValue);
                    return true;
                }

                return false;
            }

            public bool TryUpdateIfChangedBy(Vector3 newValue, float difference)
            {
                if (!_initialized)
                {
                    Reset(newValue);
                    return false;
                }

                if ((newValue - Current).sqrMagnitude >= difference * difference)
                {
                    Update(newValue);
                    return true;
                }

                return false;
            }

        }

        [Serializable]
        public class FadeInOut 
        {
            [SerializeField] private float _speed = 1;
            [SerializeField] private bool _unscaledTime;

            private float _value;

            private int _lastFrame;

            public float CurrentValue 
            {
                get 
                {
                    if (_lastFrame < Time.frameCount-1) 
                    {
                        _value = QcLerp.LerpBySpeed(_value, 0, _speed, unscaledTime: _unscaledTime);
                        _lastFrame = Time.frameCount-1;
                    }

                    return _value;
                }
            }

            public void LerpUpThisFrame() 
            {
                if (Time.frameCount == _lastFrame)
                    return;

                _value = QcLerp.LerpBySpeed(_value, 1, _speed, unscaledTime: _unscaledTime);
                _lastFrame = Time.frameCount;
            }
        }
    }
}
