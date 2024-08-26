using QuizCanners.Inspect;
using QuizCanners.Lerp;
using System;
using System.Threading.Tasks;
using UnityEngine;


namespace QuizCanners.Utils
{
    public static class LogicWrappers
    {
        public class Request
        {
            private bool _requestCreated;

            public bool CreateRequest() => _requestCreated = true;
            public bool IsRequested => _requestCreated;

            public void Use() => TryUseRequest();
            
            public bool TryUseRequest()
            {
                var result = _requestCreated;
                _requestCreated = false;
                return result;
            }

            public void Feed(bool createRequest) 
            {
                _requestCreated |= createRequest;
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
                if (!Application.isPlaying && "Max Count".PegiLabel().Edit_Delayed(ref _maxCount).Nl())
                    _maxCount = Math.Max(1, _maxCount);

                if (IsFinished)
                    "Finished".PegiLabel().Nl();
                else
                    "{0}% ({1}/{2})".F(_count*100 / _maxCount, _count, _maxCount).PegiLabel().Nl();
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
                if (!Application.isPlaying && "Count".PegiLabel().Edit_Delayed(ref _maxCount).Nl())
                    _maxCount = Math.Max(1, _maxCount);

                ToString().PegiLabel().Nl();
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


            public TimeFixedSegmenter(bool unscaledTime, float segmentLength = 1, int returnOnFirstRequest = 0) 
            {
                _unscaledTime = unscaledTime;
                _returnOnFirstRequest = returnOnFirstRequest;
                _defaultSegment = segmentLength;
                _defaultSegmentSet = true;
            }
        }

        public class TaskResultWaiter<T> : IPEGI, IPEGI_ListInspect, IGotName
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

            public string NameForInspector { get => _name; set => _name = value; }

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
                    pegi.Try_Nested_Inspect(result);
                }
                else
                {
                    Status.PegiLabel().Nl();
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

                if (ToString().PegiLabel().ClickLabel() | Icon.Enter.Click())
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

                if (Vector3.Distance(newValue, Current) >= difference)
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