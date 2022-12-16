using QuizCanners.Inspect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace QuizCanners.Utils
{
    public static partial class QcDebug
    {
        public static class FrameRate
        {
            private static bool _initialized;
            private static int _framesAtStart = 0;
            private static float _timeAtStart = 0.0f;

            public static int GetTotalFrames => Time.frameCount - _framesAtStart;

            public static float FrameRatePerSecond
            {
                get
                {
                    if (!_initialized) 
                    {
                        ResetTimer();
                        return 1;
                    }

                   return GetTotalFrames
                    / (0.0001f + Time.realtimeSinceStartup - _timeAtStart);
                }
            }

            public static void ResetTimer()
            {
                _initialized = true;
                _framesAtStart = Time.frameCount;
                _timeAtStart = Time.realtimeSinceStartup;
            }

            public static void Inspect() 
            {
                "Framerate: {0}".F(Mathf.RoundToInt(FrameRatePerSecond)).PegiLabel().Write();

                if (Icon.Refresh.Click())
                    ResetTimer();
            }
        }

        public static class TimeProfiler 
        {
            public class DisposableTimer : IDisposable
            {
                private Action _onDispose;
                private readonly TimerCollectionElements.Base _element;

                public string Description 
                {
                    set 
                    {
                        if (_element != null)
                            _element.details = value;
                    }
                }

                public void Dispose()
                {
                    try 
                    {
                        _onDispose?.Invoke();
                    } catch (Exception ex) 
                    {
                        UnityEngine.Debug.LogException(ex);
                    }
                    _onDispose = null;
                }

                internal DisposableTimer(TimerCollectionElements.Base element, Action onDone) 
                {
                    _onDispose = onDone;
                    _element = element;
                }
            }

            public static Timer Start(string measurementName) => new Timer().Start(measurementName);

            public static DictionaryOfParallelTimers Instance = new DictionaryOfParallelTimers();

            public class Timer : IDisposable
            {
                protected readonly System.Diagnostics.Stopwatch StopWatch = new System.Diagnostics.Stopwatch();
                protected string _timerStartLabel;
                public float LogTreshold;

                public Timer SetLogTreshold(float logTreshold)
                {
                    LogTreshold = logTreshold;
                    return this;
                }

                public Timer Start(string label)
                {
                    _timerStartLabel = label;
                    StopWatch.Restart();
                    return this;
                }

                public string End(string label = null, bool logInEditor = true, bool logInPlayer = false)
                {
                    StopWatch.Stop();

                    if (label == null)
                        label = _timerStartLabel;

                    _timerStartLabel = null;

                    var text = label + (label.IsNullOrEmpty() ? "" : ": ") + GetElapsedTimeString();

                    if ((Math.Abs(LogTreshold) < float.Epsilon || ((StopWatch.ElapsedTicks / TimeSpan.TicksPerSecond) > LogTreshold)) &&
                        ((Application.isEditor && logInEditor) || (!Application.isEditor && logInPlayer)))
                        UnityEngine.Debug.Log(text);

                    StopWatch.Reset();

                    return text;
                }

                public string GetElapsedTimeString()
                {
                    var text = (!_timerStartLabel.IsNullOrEmpty()) ? (_timerStartLabel + "->") : "";

                    text += QcSharp.TicksToReadableString(StopWatch.ElapsedTicks);

                    return text;
                }

                public override string ToString() => GetElapsedTimeString();

                public virtual void Dispose() => End();
            }
            
            public class ListOfTimers : IPEGI, IPEGI_ListInspect
            {
                private List<TimerCollectionElements.Base> _timings = new List<TimerCollectionElements.Base>();
                private List<TimerCollectionElements.Base> _sortedTimings = new List<TimerCollectionElements.Base>();
                protected readonly Stopwatch StopWatch = new Stopwatch();

                private readonly Gate.Frame _tickRecalculateGate = new Gate.Frame();
                private long _totalTicks;

                internal long TotalTicks()
                {
                    if (_tickRecalculateGate.TryEnter())
                    {
                        _totalTicks = 0;
                        foreach (var el in _timings)
                            _totalTicks += el.GetTotalTicksDuration();
                    }

                    return _totalTicks;
                }

                public TimerCollectionElements.SumValue Sum(string name)
                {
                    var val = new TimerCollectionElements.SumValue(name);
                    _timings.Add(val);
                    _sortedTimings = null;
                    
                    return val;
                }

                public TimerCollectionElements.AvgValue Avarage(string name)
                {
                    var val = new TimerCollectionElements.AvgValue(name);
                    _timings.Add(val);
                    _sortedTimings = null;

                    return val;
                }

                public DisposableTimer StartMax(string name)
                {
                    var val = new TimerCollectionElements.MaxValue(name);
                    _timings.Add(val);
                    _sortedTimings = null;

                    return val.Start();
                }

                #region Inspector

                public override string ToString() => "List";

                private readonly pegi.CollectionInspectorMeta collectionMeta = new pegi.CollectionInspectorMeta("Timings");
                private bool _sortByDuration;

                public void Inspect()
                {
                    if (_timings.IsNullOrEmpty())
                    {
                        "No timings".PegiLabel().Write_Hint();
                        return;
                    }

                    if (collectionMeta.IsAnyEntered == false)
                    {
                        if (_timings.Count > 500)
                            "Maximum Timings Count reached".PegiLabel().WriteWarning();

                        pegi.Nl();

                        if (_timings.Count > 1)
                            "Sort by duration.".PegiLabel().ToggleIcon(ref _sortByDuration);

                        if (_timings.Count > 0)
                            if ("Clear".PegiLabel().ClickConfirm(confirmationTag: "clLog"))
                                Clear();

                        pegi.Nl();
                    }

                    var percentages = QcMath.NormalizeToPercentage(_timings, el => el.GetTotalTicksDuration());

                    for (int i = 0; i < percentages.Count; i++)
                        _timings[i].Percentage = percentages[i];

                    if (!_sortByDuration)
                        collectionMeta.Edit_List(_timings).Nl();
                    else
                    {
                        if (_sortedTimings == null)
                        {
                            _sortedTimings = new List<TimerCollectionElements.Base>(_timings);
                            _sortedTimings.Sort((a, b) => (int)(b.GetTotalTicksDuration() - a.GetTotalTicksDuration()));
                        }

                        collectionMeta.Edit_List(_sortedTimings).Nl();
                    }
                }

                public void InspectInList(ref int edited, int index)
                {
                    "{0} [{1}]".F(QcSharp.TicksToReadableString(TotalTicks()), _timings.Count).PegiLabel().Write();

                    if (Icon.Enter.Click())
                        edited = index;
                }

                #endregion

                public void Clear()
                {
                    _timings = new List<TimerCollectionElements.Base>();
                    _sortedTimings = null;
                }
            }

            public class DictionaryOfParallelTimers : IPEGI, IPEGI_ListInspect
            {
                private Dictionary<string, TimerCollectionElements.Base> _timings = new Dictionary<string, TimerCollectionElements.Base>();
                private List<TimerCollectionElements.Base> _sortedTimings = new List<TimerCollectionElements.Base>();

                private readonly Gate.Frame _tickRecalculateGate = new Gate.Frame();
                private long _totalTicks;

                internal long TotalTicks()
                {
                    if (_tickRecalculateGate.TryEnter())
                    {
                        _totalTicks = 0;
                        foreach (var el in _timings)
                            _totalTicks += el.Value.GetTotalTicksDuration();
                    }

                    return _totalTicks;
                }

                public TimerCollectionElements.SumValue Sum(string key)
                {
                    if (!TryGetElement(key, out TimerCollectionElements.SumValue val))
                    {
                        val = new TimerCollectionElements.SumValue(key);
                        _timings[key] = val;
                        _sortedTimings = null;
                    }

                    return val;
                }

                public TimerCollectionElements.AvgValue Avg(string key)
                {
                    if (!TryGetElement(key, out TimerCollectionElements.AvgValue val))
                    {
                        val = new TimerCollectionElements.AvgValue(key);
                        _timings[key] = val;
                        _sortedTimings = null;
                    }

                    return val; 
                }

                public TimerCollectionElements.MaxValue Max(string key)
                {
                    if (!TryGetElement(key, out TimerCollectionElements.MaxValue val))
                    {
                        val = new TimerCollectionElements.MaxValue(key);
                        _timings[key] = val;
                        _sortedTimings = null;
                    }

                    return val;
                }

                public TimerCollectionElements.LastValue Last(string key)
                {
                    if (!TryGetElement(key, out TimerCollectionElements.LastValue val))
                    {
                        val = new TimerCollectionElements.LastValue(key);
                        _timings[key] = val;
                        _sortedTimings = null;
                    }

                    return val; 
                }

                public ListOfTimers List(string key) 
                {
                    if (!TryGetElement(key, out TimerCollectionElements.NestedCollection el))
                    {
                        el = new TimerCollectionElements.NestedCollection(key);
                        _timings[key] = el;
                        _sortedTimings = null;
                    }

                    return el.collection;
                }

                public DictionaryOfParallelTimers this[string key]
                {
                    get
                    {
                        if (!TryGetElement(key, out TimerCollectionElements.NestedDictionary el))
                        {
                            el = new TimerCollectionElements.NestedDictionary(key);
                            _timings[key] = el;
                            _sortedTimings = null;
                        }

                        return el.collection;
                    }
                }

                private bool TryGetElement<T>(string key, out T existing) where T : TimerCollectionElements.Base
                {
                    if (_timings.TryGetValue(key, out var el))
                    {
                        existing = el as T;
                        if (existing == null)
                        {
                            UnityEngine.Debug.LogError("Collection already contains timer with Key {0}, but type is {1} not {2}. Returning dummy.".F(key, el.GetType(), typeof(T).ToPegiStringType()));
                            return false;
                        }

                        return true;
                    }

                    existing = null;

                  
                    return false;
                }

                public void Clear()
                {
                    _timings = null;
                    _sortedTimings = null;
                }

                #region Inspector

                public override string ToString() => "Dictionary";

                private bool _sortByDuration;
                private readonly pegi.CollectionInspectorMeta _listMeta = new pegi.CollectionInspectorMeta("Timings", showDictionaryKey: false);

                public void Inspect()
                {
                    if (_timings.IsNullOrEmpty())
                    {
                        "No timings".PegiLabel().Write_Hint();
                        return;
                    }

                    if (_listMeta.IsAnyEntered == false)
                    {
                        if (_timings.Count > 500)
                            "Maximum Timings Count reached".PegiLabel().WriteWarning();

                        pegi.Nl();

                        if (_timings.Count > 1)
                            "Sort by duration.".PegiLabel().ToggleIcon(ref _sortByDuration);

                        if (_timings.Count > 0)
                            if ("Clear".PegiLabel().ClickConfirm(confirmationTag: "clLog"))
                                Clear();

                        pegi.Nl();
                    }

                    var vals = _timings.Values.ToList();

                    var percentages = QcMath.NormalizeToPercentage(vals, el => el.GetTotalTicksDuration());

                    for (int i = 0; i < percentages.Count; i++)
                        _timings.ElementAt(i).Value.Percentage = percentages[i];

                    if (!_sortByDuration)
                        _listMeta.Edit_Dictionary(_timings).Nl();
                    else
                    {
                        if (_sortedTimings == null)
                        {
                            _sortedTimings = new List<TimerCollectionElements.Base>(vals);
                            _sortedTimings.Sort((a, b) => (int)(b.GetTotalTicksDuration() - a.GetTotalTicksDuration()));
                        }

                        _listMeta.Edit_List(_sortedTimings).Nl();
                    }
                }

                public void InspectInList(ref int edited, int index)
                {
                    "{0} [{1}]".F(QcSharp.TicksToReadableString(TotalTicks()), _timings.Count).PegiLabel().Write();

                    if (Icon.Enter.Click())
                        edited = index;
                }

                #endregion
            }

            public class TimerCollectionElements
            {
                public abstract class Base
                {
                    internal string details;
                    public int Percentage { get; set; }
                    public abstract long GetTotalTicksDuration();

                    public TimeSpan ElapsedTime => TimeSpan.FromTicks(GetTotalTicksDuration());

                }

                public abstract class ValueBase : Base, IPEGI_ListInspect
                {
                    protected long _totalTicksDuration;
                    public string Key;
                    public double OperationsCount = -1;
                    protected int totalRuns;

                    protected List<Stopwatch> activeRuns = new List<Stopwatch>();

                    public sealed override long GetTotalTicksDuration()
                    {
                        for (int i = activeRuns.Count - 1; i >= 0; i--)
                            if (!activeRuns[i].IsRunning)
                                activeRuns.RemoveAt(i);

                        return GetTotalFromActiveRuns();
                    }
                    protected virtual long GetTotalFromActiveRuns() => _totalTicksDuration;

                    public void Measure(Action actionToTime, string details = "") 
                    {
                        var disp = Start(details);

                        try 
                        {
                            actionToTime?.Invoke();
                        } catch (Exception ex) 
                        {
                            details = ex.ToString();
                        }
                        finally 
                        {
                            disp.Dispose();
                        }
                    }

                    public DisposableTimer Start(string details = "", int operationsCount = -1)
                    {
                        Stopwatch stopWatch = new Stopwatch();
                        stopWatch.Start();

                        activeRuns.Add(stopWatch);
                        totalRuns++;

                        return new DisposableTimer(this, ()=>
                        {
                            try
                            {
                                OnComplete(stopWatch.ElapsedTicks, details, operationsCount);
                            }
                            catch (Exception ex)
                            {
                                UnityEngine.Debug.LogException(ex);
                            }
                            stopWatch.Stop();
                        });
                    }

                    protected abstract void OnComplete(long ticks, string details, int operationsCount);

                    #region Inspector
                    public void InspectInList(ref int edited, int index)
                    {
                        if (activeRuns.Count > 0) 
                        {
                            Icon.Wait.Draw();
                        }

                        ToString().PegiLabel().Write();
                    }

                    public override string ToString()
                    {
                        string keyParameter;

                        if (OperationsCount > 0 && _totalTicksDuration > 0)
                        {
                            var perSecond = Math.Round(OperationsCount / ElapsedTime.TotalSeconds);
                            keyParameter = "{0} / sec".F(perSecond.ToReadableString());
                        } else 
                        {
                            keyParameter = QcSharp.TicksToReadableString(GetTotalTicksDuration());
                        }

                        var val = "{0}%: {1}{2}{3} - {4}".F(
                            Percentage,
                            QcSharp.AddSpacesToSentence(Key),
                            activeRuns.Count > 0 ? "[{0} ({1})]".F(activeRuns.Count, totalRuns) : "",
                            details.IsNullOrEmpty() ? "" : " ({0}) ".F(details),
                            keyParameter
                            );

                        return val;
                    }

                    #endregion

                    protected ValueBase (string key) 
                    {
                        Key = key;
                    }
                }

                public class SumValue : ValueBase
                {
                    protected override long GetTotalFromActiveRuns()
                    {
                        var tmp = _totalTicksDuration;

                        foreach (var el in activeRuns)
                            tmp += el.ElapsedTicks;

                        return tmp;
                    }

                    protected override void OnComplete(long elapsed, string details, int operationsCount)
                    {
                        _totalTicksDuration += elapsed;
                        base.details = details;
                        OperationsCount += operationsCount;
                    }

                    public override string ToString() => "Sum " + base.ToString(); 

                    public SumValue( string key) : base(key) { }
                }

                public class LastValue : ValueBase
                {
                    protected override long GetTotalFromActiveRuns()
                    {
                        var tmp = _totalTicksDuration;

                        foreach (var el in activeRuns)
                            tmp = Math.Max(tmp, el.ElapsedTicks);

                        return tmp;
                    }

                    protected override void OnComplete(long elapsed, string details, int operationsCount)
                    {
                        _totalTicksDuration = elapsed;
                        base.details = details;
                        OperationsCount = operationsCount;
                    }


                    public override string ToString() => "Val " + base.ToString(); // {0} {1}".F(QcSharp.AddSpacesToSentence(Key), _details.IsNullOrEmpty() ? "" : " ({0}) ".F(_details));

                    public LastValue(string key) : base(key) { }
                }

                public class AvgValue : ValueBase
                {
                    private long total;
                    private int count;
                    private double totalOperations;

                    protected override long GetTotalFromActiveRuns()
                    {
                        var tmp = total;

                        foreach (var el in activeRuns)
                            tmp += el.ElapsedTicks;

                        return tmp / (count + activeRuns.Count);
                    }

                    protected override void OnComplete(long elapsed, string details, int operationsCount)
                    {
                        totalOperations += operationsCount;
                        total += elapsed;
                        count++;
                        _totalTicksDuration = total / count;
                        OperationsCount = Math.Floor(totalOperations / count);
                        base.details = details;
                    }

                    public override string ToString() => "Avg " + base.ToString(); // {0} {1}".F(QcSharp.AddSpacesToSentence(Key), _details.IsNullOrEmpty() ? "" : " ({0}) ".F(_details));

                    public AvgValue(string key) : base(key) { }
                }

                public class MaxValue : ValueBase
                {

                    protected override long GetTotalFromActiveRuns()
                    {
                        var tmp = _totalTicksDuration;

                        foreach (var el in activeRuns)
                            tmp = Math.Max(tmp, el.ElapsedTicks);

                        return tmp;
                    }

                    protected override void OnComplete(long elapsed, string details, int operationsCount)
                    {
                        if (elapsed > _totalTicksDuration)
                        {
                            _totalTicksDuration = elapsed;
                            base.details = details;
                            OperationsCount = operationsCount;
                        }
                    }

                    public override string ToString() => "Max " + base.ToString();

                    public MaxValue(string key) : base(key) { }
                }

                public class NestedCollection : Base, IPEGI_ListInspect, IPEGI
                {
                    public string Name;

                    public ListOfTimers collection = new ListOfTimers();

                    public override long GetTotalTicksDuration() => collection.TotalTicks();

                    public NestedCollection(string name)
                    {
                        Name = name;
                    }

                    #region Inspector
                    public void InspectInList(ref int edited, int index)
                    {
                        if (QcSharp.AddSpacesToSentence(Name).PegiLabel().ClickLabel())
                            edited = index;

                        "{0} %".F(Percentage).PegiLabel(65).Write();

                        collection.InspectInList(ref edited, index);
                    }

                    public override string ToString() => Name + " " + collection.GetNameForInspector();

                    public void Inspect()
                    {
                        collection.Nested_Inspect();
                    }
                    #endregion
                }

                public class NestedDictionary : Base, IPEGI_ListInspect, IPEGI
                {
                    public string Name;

                    public DictionaryOfParallelTimers collection = new DictionaryOfParallelTimers();

                    public override long GetTotalTicksDuration() => collection.TotalTicks();

                    public NestedDictionary(string name)
                    {
                        Name = name;
                    }

                    #region Inspector
                    public void InspectInList(ref int edited, int index)
                    {
                        if (QcSharp.AddSpacesToSentence(Name).PegiLabel().ClickLabel())
                            edited = index;

                        "{0} %".F(Percentage).PegiLabel(65).Write();

                        collection.InspectInList(ref edited, index);
                    }

                    public override string ToString() => Name + " " + collection.GetNameForInspector();

                    public void Inspect()
                    {
                        collection.Nested_Inspect();
                    }
                    #endregion
                }
            }

        }
    }
}