using QuizCanners.Inspect;
using System;
using System.Threading.Tasks;
using UnityEngine;


namespace QuizCanners.Utils
{
    public class LogicWrappers
    {
        public class Request
        {
            private bool _requestCreated;

            public bool CreateRequest() => _requestCreated = true;
            public bool IsRequested => _requestCreated;
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

            public void SetMax(float seconds)
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

            public void Inspect()
            {
                if (!Application.isPlaying && "Max Count".PegiLabel().EditDelayed(ref _maxCount).Nl())
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
        public class CountDownFromMax : IPEGI, IGotReadOnlyName
        {
            [SerializeField] private int _maxCount;
            [NonSerialized] private int _count;

            public int Count => _count;

            public bool IsFinished
            {
                get => _count <= 0;
                set
                {
                    _count = value ? 0 : _maxCount;
                }
            }

            public void RemoveOne() => _count--;

            public void Restart() => _count = _maxCount;

            public void Inspect()
            {
                if (!Application.isPlaying && "Count".PegiLabel().EditDelayed(ref _maxCount).Nl())
                    _maxCount = Math.Max(1, _maxCount);

                GetReadOnlyName().PegiLabel().Nl();
            }

            public string GetReadOnlyName()
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
            private bool _timeSet;
            private float _lastTime;
            private readonly float _defaultSegment = 1;
            private readonly int _returnOnFirstRequest;

            public int GetSegmentsWithouUpdate() => GetSegmentsWithouUpdate(_defaultSegment);

            public void Reset() 
            {
                _timeSet = false;
            }

            public int GetSegmentsWithouUpdate(float segment) 
            {
                if (!_timeSet) 
                {
                    _timeSet = true;
                    _lastTime = Time.time;
                    return _returnOnFirstRequest;
                }

                var gap = Time.time - _lastTime;
                var segments = Mathf.FloorToInt(gap / segment);
                return segments;
            }

            public int GetSegmentsAndUpdate() => GetSegmentsAndUpdate(_defaultSegment);

            public int GetSegmentsAndUpdate(float segment) 
            {
                var segments = GetSegmentsWithouUpdate(segment);
                _lastTime += segments * segment;
                return segments;
            }

            public TimeFixedSegmenter(float segmentLength = 1, int returnOnFirstRequest = 0) 
            {
                _returnOnFirstRequest = returnOnFirstRequest;
                _defaultSegment = segmentLength;
            }
        }

        public class TaskResultWaiter<T> : IPEGI, IPEGI_ListInspect, IGotName, IGotReadOnlyName
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
                    switch (stat) 
                    {
                        case StatusEnum.Other: return _task.Status.ToString();
                        default: return stat.ToString();
                    }
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
                result = default(T);
                _task = task;
                if (name.IsNullOrEmpty())
                    _name = QcSharp.AddSpacesInsteadOfCapitals(typeof(T).ToPegiStringType());
                else
                    _name = name;
            }

            #region Inspector
            public void Inspect()
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
                    var asIls = result as IPEGI_ListInspect;
                    if (asIls != null)
                    {
                        asIls.InspectInList_Nested(ref edited, index);
                        return;
                    }
                }

                if (GetReadOnlyName().PegiLabel().ClickLabel() | Icon.Enter.Click())
                    edited = index;
            }

            public string GetReadOnlyName() => "{0}".F(_name.IsNullOrEmpty() ? typeof(T).ToPegiStringType() : _name);


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
            Vector3 _previousPosition;

            public bool TryGetSegments(Vector3 newPosition, float delta, out Vector3[] points) 
            {
                var segment = (newPosition - _previousPosition);

                float totalDistance = segment.magnitude;

                int count = Mathf.FloorToInt(totalDistance / delta);
                points = new Vector3[count];

                if (count == 0) 
                    return false;
                
                float fraction = (delta * count) / totalDistance;

                var direction = segment.normalized;

                for (int i = 0; i < count; i++)
                    points[i] = _previousPosition + direction * delta * (i + 1);

                _previousPosition = Vector3.Lerp(_previousPosition, newPosition, fraction);

                return true;
            }

            public void Reset(Vector3 position) 
            {
                _previousPosition = position;
            }

        }

    }
}