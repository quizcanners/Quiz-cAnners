using System;
using UnityEngine;

namespace QuizCanners.Utils
{
    public static partial class Pool
    {
        private static readonly System.Diagnostics.Stopwatch timer = new();
        private static readonly Gate.Frame _timerResetFrame = new(Gate.InitialValue.StartArmed);

        public static double GetFrameMiliseconds()
        {
            if (_timerResetFrame.TryEnter())
            {
                timer.Restart();
                return 0.0001;
            }

          //  var result = timer.Elapsed.Milliseconds;

            return timer.Elapsed.TotalSeconds;
        }

        public static bool IsVisibleByCamera(Vector3 pos, float objectSize = 1, float maxDistance = -1)
        {
            return Camera.main.IsInCameraViewArea(pos, objectSize: objectSize, maxDistance: maxDistance);
        }

        public static bool TrySpawn<T>(Vector3 position) where T : Component
        {
            if (Singleton.TryGet<PoolBehaviourCore<T>>(out var s))
            {
                try
                {
                    s.TrySpawn(position, out var result);
                } catch (Exception ex) 
                {
                    Debug.LogException(ex);
                }

                return true;
            }

            return false;
        }
        public static bool TrySpawn<T>(Vector3 position, out T instance) where T : Component
        {
            T result = null;

            Singleton.Try<PoolBehaviourCore<T>>(s => s.TrySpawn(position, out result));

            instance = result;

            return instance;
        }

        public static bool TrySpawn<T>(Vector3 position, Action<T> onInstanciated) where T : Component
            => Singleton.Try<PoolBehaviourCore<T>>(s => s.TrySpawn(position, onInstanciated), logOnServiceMissing: false);

        public static bool TrySpawnIfVisible<T>(Vector3 position) where T : Component => Singleton.Try<PoolBehaviourCore<T>>(s => s.TrySpawnIfVisible(position, out var result), logOnServiceMissing: false);

        public static bool TrySpawnIfVisible<T>(Vector3 position, out T instance) where T : Component
        {
            T result = null;

            Singleton.Try<PoolBehaviourCore<T>>(s => s.TrySpawnIfVisible(position, out result), logOnServiceMissing: false);

            instance = result;

            return instance;
        }

        public static bool TrySpawnIfVisible<T>(Vector3 position, Action<T> onInstanciated) where T : Component
            => Singleton.Try<PoolBehaviourCore<T>>(s => s.TrySpawnIfVisible(position, onInstanciated));

        public static float VacancyFraction<T>(float defaultValue = 1f) where T : Component => Singleton.GetValue<PoolBehaviourCore<T>, float>(s => s.VacancyPortion, defaultValue: defaultValue);

        public static void Return<T>(T instance) where T : Component => Singleton.Try<PoolBehaviourCore<T>>(onFound: s => s.ReturnToPool(instance), logOnServiceMissing: false);

        public static void TrySpawnIfVisible<T>(Vector3 position, int preferedCount, Action<T> onInstanciate) where T : Component
        {
            Singleton.Try<PoolBehaviourCore<T>>(pool =>
            {
                if (Camera.main.IsInCameraViewArea(position))
                {
                    int count = (int)Math.Max(1, preferedCount * pool.VacancyPortion);

                    for (int i = 0; i < count; i++)
                    {
                        if (!pool.TrySpawn(worldPosition: position, out var instance))
                            break;

                        onInstanciate.Invoke(instance);
                    }
                };
            });
        }
    }

}
