using QuizCanners.Inspect;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QuizCanners.Utils
{
    public static class QcDebug
    {
        private static bool _forceDebug = false; // To have Debug Access in a build
        private static readonly PlayerPrefValue.Bool _emultateRelease = new PlayerPrefValue.Bool("qc_emlRls", defaultValue: false); // To enable Release functionality in Debug
       
        public static bool IsRelease
        {
            get => (!ShowDebugOptions) || _emultateRelease.GetValue();
            set
            {
                _emultateRelease.SetValue(value);
            }
        }

        public static void ForceDebugOption() => _forceDebug = true;
        public static bool ShowDebugOptions => (_forceDebug || Debug.isDebugBuild);

        private static readonly Migration.ICfgObjectExplorer iCfgExplorer = new Migration.ICfgObjectExplorer();

        private static readonly EncodedJsonInspector jsonInspector = new EncodedJsonInspector();

        private static int _testSeed = 42;
        private static readonly pegi.EnterExitContext enterExitContext = new pegi.EnterExitContext();

        public static void Inspect() 
        {
            using (enterExitContext.StartContext())
            {

                if ("Probability Calculator".PegiLabel().isEntered().nl())
                {
                    Percentages = QcMath.NormalizeToPercentage(probabilities, prob => prob.Chances);
                    "Probabilities".PegiLabel().edit_List(probabilities).nl();
                }

                if ("Random Seed Test".PegiLabel().isEntered().nl())
                {
                    "Seed".PegiLabel().edit(ref _testSeed).nl();

                    using (QcMath.RandomBySeedDisposable(_testSeed))
                    {
                        for (int i = 0; i < 4; i++)
                            "Value {0}: {1}".F(i, UnityEngine.Random.value * 100).PegiLabel().nl();
                    }

                    using (QcMath.RandomBySeedDisposable(_testSeed))
                    {
                        for (int i = 0; i < 4; i++)
                            "B Value {0}: {1}".F(i, UnityEngine.Random.value * 100).PegiLabel().nl();
                    }
                }

                "Json Inspector".PegiLabel().enter_Inspect(jsonInspector).nl();

                if ("ICfg Inspector".PegiLabel().isEntered().nl())
                    iCfgExplorer.Inspect(null);

                pegi.nl();

                if ("Managed Coroutines [{0}]".F(QcAsync.DefaultCoroutineManager.GetActiveCoroutinesCount).PegiLabel().isEntered().nl())
                    QcAsync.DefaultCoroutineManager.Nested_Inspect();

                if ("Gui Styles".PegiLabel().isEntered().nl())
                {
                    pegi.Styles.Inspect();
                    pegi.nl();
                }

                if (enterExitContext.IsAnyEntered == false)
                {
                    var release = IsRelease;
                    if ("Release".PegiLabel().toggleIcon(ref release).nl())
                        IsRelease = release;
                }
            }
        }


        #region Probability Calculator

        private static readonly List<Probability> probabilities = new List<Probability>();
        private static List<int> Percentages;

        private struct Probability : IPEGI_ListInspect
        {
            private string name;
            public double Chances;

            public void InspectInList(ref int edited, int index)
            {
                pegi.edit(ref name);
                pegi.edit(ref Chances);

                "= {0}%".F(Percentages[index].ToString()).PegiLabel().nl();
            }
        }
        #endregion

        #region Timers
        public static Timer StartTimer(string measurementName) => new Timer().Start(measurementName);

        public static TimerDictionaryParallel timerGlobal = new TimerDictionaryParallel();

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

            public Timer Start()
            {
                _timerStartLabel = null;
                StopWatch.Restart();
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
                    Debug.Log(text);

                StopWatch.Reset();

                return text;
            }

            public float GetMiliseconds() => StopWatch.ElapsedMilliseconds;

            public float GetSeconds() => StopWatch.ElapsedMilliseconds / 1000f;

            public string GetElapsedTimeString()
            {
                var text = (!_timerStartLabel.IsNullOrEmpty()) ? (_timerStartLabel + "->") : "";

                text += QcSharp.TicksToReadableString(StopWatch.ElapsedTicks);

                return text;
            }

            public override string ToString() => GetElapsedTimeString();

            public string End_Restart(string labelForEndedSection = null, bool logInEditor = true, bool logInPlayer = false)
            {
                var txt = End(labelForEndedSection, logInEditor, logInPlayer);
                StopWatch.Start();
                return txt;
            }

            public virtual void Dispose() => End();

          
        }

        public class TimerCollection : IPEGI, IDisposable, IPEGI_ListInspect
        {
            private List<Element> _timings = new List<Element>();
            private List<Element> _sortedTimings = new List<Element>();
            protected readonly System.Diagnostics.Stopwatch StopWatch = new System.Diagnostics.Stopwatch();

            private bool _durationUpdated;
            private long _totalTicks;

            internal long TotalTicks() 
            {
                if (!_durationUpdated) 
                {
                    _durationUpdated = true;
                    RecalculateLength();
                }

                return _totalTicks;
            }

            private Element Current
            {
                get
                {
                    if (_timings.Count == 0) 
                        _timings.Add(new Element());

                    return _timings.Last();
                }
            }

            private List<Element> TimingDictionary
            {
                get
                {
                    if (_timings == null)
                        _timings = new List<Element>();

                    return _timings;
                }
            }

            public TimerCollection StartTimer(string measurementName)
            {
                Create(measurementName);
                Restart();
                return this;
            }

            public void End()
            {
                StopWatch.Stop();
                Current.Ticks = StopWatch.ElapsedTicks;
            }

            public TimerCollection End_StartNext(string measurementName)
            {
                End();
                return StartTimer(measurementName);
            }

            public void Clear()
            {
                _timings = new List<Element>();
                _sortedTimings = null;
            }

            private void Restart()
            {
                StopWatch.Restart();
            }

            private Element Create(string key)
            {
                if (TimingDictionary.Count > 500) 
                {
                    return TimingDictionary.Last();
                }

                var el = new Element(key);
                TimingDictionary.Add(el);
                _sortedTimings = null;
                return el;
            }

            private bool _sortByDuration;

            private void RecalculateLength() 
            {
                _totalTicks = 0;
                foreach (var el in _timings)
                    _totalTicks += el.Ticks;
            }

            public void Inspect()
            {
                if (_timings.IsNullOrEmpty())
                {
                    "No timings".PegiLabel().writeHint();
                    return;
                }

                if (TimingDictionary.Count > 500)
                    "Maximum Timings Count reached".PegiLabel().writeWarning();

                pegi.nl();

                if ("Clear".PegiLabel().ClickConfirm(confirmationTag: "clLog").nl())
                    Clear();

                "Sort by duration.".PegiLabel().toggleIcon(ref _sortByDuration).nl();

                var percentages = QcMath.NormalizeToPercentage(_timings, el => el.Ticks);

                for (int i = 0; i < percentages.Count; i++)
                    _timings[i].percentage = percentages[i];

                if (!_sortByDuration)
                    "Timings".PegiLabel().edit_List(_timings).nl();
                else 
                {
                    if (_sortedTimings == null)
                    {
                        _sortedTimings = new List<Element>(_timings);
                        _sortedTimings.Sort((a, b) => (int)(b.Ticks - a.Ticks));
                    }

                    "Timings".PegiLabel().edit_List(_sortedTimings).nl();
                }
            }

            public void Dispose()
            {
                End();
            }

            public void InspectInList(ref int edited, int index)
            {
                "{0} [{1}]".F(QcSharp.TicksToReadableString(TotalTicks()),_timings.Count).PegiLabel(90).write();

                if (icon.Enter.Click())
                    edited = index;
            }

            public class Element : IPEGI_ListInspect, IGotReadOnlyName
            {
                public string Name;
                private bool _finished = false;
                private long _ticks;
                public int percentage;

                public long Ticks
                {
                    get => _ticks;
                    set
                    {
                        _ticks = value;
                        _finished = true;
                    }
                }
                public Element() { }
                public Element(string name)
                {
                    Name = name;
                }

                public void InspectInList(ref int edited, int index)
                {
                    if (!_finished)
                        icon.Wait.draw();

                    GetReadOnlyName().PegiLabel().write();
                }

                public string GetReadOnlyName() => _finished ? "{0} ( {1} )".F(Name, QcSharp.TicksToReadableString(_ticks)) : "{0}....".F(Name);
            }

        }

        public class TimerDictionaryParallel : IPEGI
        {
            private Dictionary<string, ElementBase> _timings = new Dictionary<string, ElementBase>();
            private List<ElementBase> _sortedTimings = new List<ElementBase>();

            public MaxValueElement this[string key]
            {
                get
                {
                    var el = _timings.TryGet(key) as MaxValueElement;
                    if (el == null)
                    {
                        el = new MaxValueElement(this, key, "");
                        _timings[key] = el;
                        _sortedTimings = null;
                    }

                    return el;
                }
            }

            public IDisposable StartSaveMaxTimer(string key, string details = "") 
            {
                var ret = new MaxValueElement(this, key, QcSharp.AddSpacesToSentence(details));

                var el = _timings.TryGet(key);
                if (el == null)
                    _timings[key] = ret;

                _sortedTimings = null;

                return ret.Start();
            }

            public TimerCollection GetCollection(string key)
            {
                var el = _timings.TryGetValue(key, out var col) ? col as CollectionElement : null;
                
                if (el == null)
                {
                    el = new CollectionElement(key);
                    _timings[key] = el;
                    _sortedTimings = null;
                }

                return el.collection;

            }
            public void Clear()
            {
                _timings = null;
                _sortedTimings = null;
            }

            private bool _sortByDuration;
            private readonly pegi.CollectionInspectorMeta _listMeta = new pegi.CollectionInspectorMeta("Timings", showDictionaryKey: false);

            public void Inspect()
            {
                if (_timings.IsNullOrEmpty())
                {
                    "No timings".PegiLabel().writeHint();
                    return;
                }

                if (_timings.Count > 500)
                    "Maximum Timings Count reached".PegiLabel().writeWarning();

                pegi.nl();

                if ("Clear".PegiLabel().ClickConfirm(confirmationTag: "clLog").nl())
                    Clear();

                if (!_listMeta.IsInspectingElement)
                    "Sort by duration.".PegiLabel().toggleIcon(ref _sortByDuration).nl();

                var vals = _timings.Values.ToList();

                var percentages = QcMath.NormalizeToPercentage(vals, el => el.TotalDuration);

                for (int i = 0; i < percentages.Count; i++)
                    _timings.ElementAt(i).Value.Percentage = percentages[i];

                if (!_sortByDuration)
                    _listMeta.edit_Dictionary(_timings).nl();
                else
                {
                    if (_sortedTimings == null)
                    {
                        _sortedTimings = new List<ElementBase>(vals);
                        _sortedTimings.Sort((a, b) => (int)(b.TotalDuration - a.TotalDuration));
                    }

                    _listMeta.edit_List(_sortedTimings).nl();
                }
            }

            public abstract class ElementBase 
            {
                public int Percentage { get; set; } 
                public abstract long TotalDuration { get; }
            }

            public class MaxValueElement : ElementBase, IDisposable, IPEGI_ListInspect
            {
                protected readonly System.Diagnostics.Stopwatch StopWatch = new System.Diagnostics.Stopwatch();
                private readonly TimerDictionaryParallel _parentCollection;

                private long _totalDuration;
                private bool finished;
                public string Key;
                public string _details;
                public override long TotalDuration => _totalDuration;

                public IDisposable Start()
                {
                    StopWatch.Restart();
                    return this;
                }

                public IDisposable Start(string label)
                {
                    _details = label;
                    StopWatch.Restart();
                    return this;
                }

                public void End() 
                {
                    StopWatch.Stop();
                    _totalDuration = Math.Max(_totalDuration, StopWatch.ElapsedTicks);
                    finished = true;

                    var exist = _parentCollection._timings[Key];
                    if (exist.TotalDuration < TotalDuration)
                        _parentCollection._timings[Key] = this;
                }

                public void Dispose() => End();

                public void InspectInList(ref int edited, int index)
                {
                    if (finished)
                        "{0}{1}: {2}% - {3})".F(QcSharp.AddSpacesToSentence(Key), _details.IsNullOrEmpty() ? "" : " ({0}) ".F(_details), Percentage, QcSharp.TicksToReadableString(TotalDuration)).PegiLabel().write();
                    else 
                    {
                        icon.Wait.draw();
                        _details.PegiLabel().write();
                    }
                }

                public MaxValueElement (TimerDictionaryParallel parent, string key, string details) 
                {
                    _details = details;
                    _parentCollection = parent;
                    Key = key;
                }
            }

            public class CollectionElement : ElementBase, IPEGI_ListInspect, IGotReadOnlyName, IPEGI
            {
                public string Name;

                public TimerCollection collection = new TimerCollection();

                public override long TotalDuration => collection.TotalTicks();

                public void End() => collection.End();

                public CollectionElement(string name)
                {
                    Name = name;
                }

                #region Inspector
                public void InspectInList(ref int edited, int index)
                {
                    if (QcSharp.AddSpacesToSentence(Name).PegiLabel().ClickLabel())
                        edited = index;
                    collection.InspectInList(ref edited, index);
                }

                public string GetReadOnlyName() => collection.GetNameForInspector();

                public void Inspect()
                {
                    Name.PegiLabel(style: pegi.Styles.ListLabel).nl();
                    collection.Nested_Inspect();
                }
                #endregion
            }

        }



        #endregion

    }
}
