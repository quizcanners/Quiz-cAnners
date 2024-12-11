using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using QuizCanners.Inspect;
using UnityEngine;

using Debug = UnityEngine.Debug;
using Enm = System.Linq.Enumerable;

namespace QuizCanners.Utils
{
    public static partial class QcAsync
    {
        public static TimedCoroutine.CallAgainRequest CallAgain() => new();

        public static TimedCoroutine.CallAgainRequest CallAgain(string message) => new(message: message);

        /*
        public static TimedEnumeration.CallAgain CallAfter(float seconds) =>
            new TimedEnumeration.CallAgain(task: Task.Delay(TimeSpan.FromMilliseconds(QcMath.Seconds_To_Miliseconds(seconds))));

        public static TimedEnumeration.CallAgain CallAfter(float seconds, string message) =>
            new TimedEnumeration.CallAgain(task: Task.Delay(TimeSpan.FromMilliseconds(QcMath.Seconds_To_Miliseconds(seconds))), message: message);

        public static TimedEnumeration.CallAgain CallAfter(TimeSpan timeSpan) =>
            new TimedEnumeration.CallAgain(task: Task.Delay(timeSpan));

        public static TimedEnumeration.CallAgain CallAfter(TimeSpan timeSpan, string message) =>
            new TimedEnumeration.CallAgain(task: Task.Delay(timeSpan), message: message);*/

        public static TimedCoroutine.CallAgainRequest CallAfter_Thread(Action afterThisTask) =>
            new(task: Task.Run(afterThisTask));

        public static TimedCoroutine.CallAgainRequest CallAfter_Thread(Action afterThisTask, string message) =>
            new(task: Task.Run(afterThisTask), message: message);

        public class TimedCoroutinesManager : IPEGI
        {
            private static readonly List<TimedCoroutine> pool = new();

            private readonly List<TimedCoroutine> _enumerators = new();

            public int GetActiveCoroutinesCount => _enumerators.Count;

            public TimedCoroutine Add(IEnumerator enumerator, Action onExit = null, Action onFullyDone = null)
            {
                var enm = (pool.Count > 0) ? pool.TryTake(0) : new TimedCoroutine();
                enm.Reset(enumerator, onExitAction: onExit, onDoneFullyAction: onFullyDone);
                _enumerators.Insert(0, enm);
                return enm;
            }

            public void UpdateManagedCoroutines()
            {
                for (int i = _enumerators.Count - 1; i >= 0; i--)
                    if (!_enumerators[i].MoveNext())
                        pool.Add(_enumerators.TryTake(i));
            }

            #region Inspector 
            private readonly pegi.CollectionInspectorMeta coroutinesListMeta = new("Managed Coroutines", showAddButton: false);

            private Task _debugTask;
            void IPEGI.Inspect()
            {
                pegi.Nl();

                if (!coroutinesListMeta.IsAnyEntered)
                {
                    "Pool Size: {0}".F(pool.Count).PL().Nl();

                    if ("Run an Example Managed Coroutine".PL().Click().Nl())
                        DefaultCoroutineManager.Add(Coroutine_Test());

                    if (_debugTask != null)
                    {
                        "Task status:{0}".F(_debugTask.Status).PL().Nl();

                        _debugTask.Exception?.ToString().PL().WriteBig();

                        if ("Clear".PL().Click())
                            _debugTask = null;
                    }
                    else if ("Run an Example Task".PL().Click().Nl())
                    {
                        var tmp = new TimedCoroutine(Coroutine_Test());
                        _debugTask = tmp.StartTask();
                    }
                }

                if ("Yield 1 frame".PL().Click().Nl())
                    UpdateManagedCoroutines();

                coroutinesListMeta.Edit_List(_enumerators).Nl();

                if (!coroutinesListMeta.IsAnyEntered)
                {
                    ("Managed Timed coroutines can run in Editor, but need an object to send an update call to them every frame: QcAsync.UpdateManagedCoroutines()." +
                     " Alternatively a TimedEnumerator can be started with Unity's " +
                     "StartCoroutine(new TimedEnumeration(enumerator)). It will in turn call yield on it multiple times with care for performance.").PL().Write_Hint();

                    ("Examples are in QcAsync.cs class").PL().Write_Hint();
                }
            }

            #endregion
        }

        public static Coroutine StartTimedCoroutine(this IEnumerator enumerator, MonoBehaviour behaviour, Action onExit = null) =>
            behaviour.StartCoroutine(new TimedCoroutine(enumerator).GetCoroutine(onExitAction: onExit));

        #region Inspector
        public static TimedCoroutinesManager DefaultCoroutineManager = new();

        private static string CalculatePi_Test(int digits)
        {

            //Stanley Rabinowitz and Stan Wagon - Spigot Algorithm

            digits++;

            uint xlen = (uint)(digits * 10 / 3 + 2);

            uint[] x = new uint[xlen];
            uint[] r = new uint[digits * 10 / 3 + 2];

            uint[] pi = new uint[digits];

            for (int j = 0; j < x.Length; j++)
                x[j] = 20;

            for (int i = 0; i < digits; i++)
            {
                uint carry = 0;
                for (int j = 0; j < xlen; j++)
                {
                    uint num = (uint)(xlen - j - 1);
                    uint dem = num * 2 + 1;

                    x[j] += carry;

                    uint q = x[j] / dem;
                    r[j] = x[j] % dem;

                    carry = q * num;
                }


                pi[i] = (x[xlen - 1] / 10);


                r[xlen - 1] = x[xlen - 1] % 10;

                for (int j = 0; j < xlen; j++)
                    x[j] = r[j] * 10;
            }

            var sb = new System.Text.StringBuilder(pi.Length);

            uint c = 0;

            for (int i = pi.Length - 1; i >= 0; i--)
            {
                var p = pi[i];
                p += c;
                c = p / 10;

                sb.Append(p.ToString());
            }

            return new string(Enm.ToArray(Enm.Reverse(sb.ToString())));

        }

        private static IEnumerator NestedCoroutine_Test()
        {

            Debug.Log("Starting nested coroutine. Frame: {0}".F(Time.frameCount));

            int sum = 0;

            for (int i = 0; i < 1000; i++)
            {
                sum += i;

                yield return CallAgain();
            }

            Debug.Log("Done with nested coroutine. Frame: {0}; Result: {1}".F(Time.frameCount, sum));

        }

        private static IEnumerator Coroutine_Test()
        {
            yield return NestedCoroutine_Test();


            for (int i = 0; i < 5; i++)
            {
                Debug.Log("{0}: Frame: {1}".F(i, Time.frameCount));
                yield return CallAgain("Asking to execute this function again if we have enough time this frame"); // Communication token
            }

            for (int i = 0; i < 5; i++)
            {
                Debug.Log("With wait {0}. Frame: {1}".F(i, Time.frameCount));
                yield return new TimedCoroutine.CallAgainRequest(task: Task.Delay(TimeSpan.FromMilliseconds(QcMath.Seconds_To_Miliseconds(0.3f))), message: "Sending communication token that will ask to delay execution by 0.3 seconds");
                // yield return CallAfter(0.3f, "Sending communication token that will ask to delay execution by 0.3 seconds");
            }

            Debug.Log("Will start Nested Coroutine.");

         

            Debug.Log("Calculating Pi");

            string pi = "";

            yield return CallAfter_Thread(() =>
            {
                pi = CalculatePi_Test(10000);
            }, "Now we are calculating Pi in a task");

            //  yield return CallAgain_StoreReturnData(pi);

            Debug.Log("Done calculating Pi : {0}".F(pi));

        }

        #endregion
    }

}