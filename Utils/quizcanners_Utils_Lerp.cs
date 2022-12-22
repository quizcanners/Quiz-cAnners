using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QuizCanners.Utils;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace QuizCanners.Lerp
{
    public static class LerpUtils
    {
        const MethodImplOptions INLINE = MethodImplOptions.AggressiveInlining;

        #region Float

        [MethodImpl(INLINE)]
        public static float ExpLerp(float from, float to, float portion) => Mathf.Pow(from, 1 - portion) * Mathf.Pow(to, portion);
        
        private static float GetTime(bool unscaledTime) => unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        private static float SpeedToPortion(float speed, float dist, bool unscaledTime)
        {
            if (dist == 0)
                return 1;

            dist = Mathf.Abs(dist);

            var time = GetTime(unscaledTime);

            float portion = Mathf.Clamp01(speed * time / dist);

            return portion;
        }

        public static bool SpeedToMinPortion(float speed, float dist, ref float portion, bool unscaledTime)
        {
            var nPortion = SpeedToPortion(speed, dist, unscaledTime: unscaledTime);
            if (!(nPortion < portion))
                return (1 - portion) < float.Epsilon && dist > 0;
            portion = nPortion;
            return true;
        }

        public static bool IsLerpingBySpeed(ref float from, float to, float speed, bool unscaledTime)
        {
            if (Mathf.Approximately(from, to))
                return false;

            from = Mathf.LerpUnclamped(from, to, SpeedToPortion(speed, Mathf.Abs(from - to), unscaledTime: unscaledTime));
            return true;
        }

        public static float LerpBySpeed(float from, float to, float speed, bool unscaledTime)
            => Mathf.LerpUnclamped(from, to, SpeedToPortion(speed, Mathf.Abs(from - to), unscaledTime: unscaledTime));

        public static float LerpBySpeed(float from, float to, float speed, out float portion, bool unscaledTime)
        {
            portion = SpeedToPortion(speed, Mathf.Abs(from - to), unscaledTime: unscaledTime);
            return Mathf.LerpUnclamped(from, to, portion);
        }

        #endregion

        #region Double

        public static bool IsLerpingBySpeed(ref double from, double to, double speed, bool unscaledTime)
        {
            if (System.Math.Abs(from - to) < double.Epsilon * 10)
            {
                return false;
            }

            double diff = to - from;

            double dist = System.Math.Abs(diff);

            from += diff * QcMath.Clamp01(speed * GetTime(unscaledTime) / dist);
            return true;
        }

        public static double LerpBySpeed(double from, double to, double speed, bool unscaledTime)
        {
            if (System.Math.Abs(from - to) < double.Epsilon * 10)
                return from;

            double diff = to - from;

            double dist = System.Math.Abs(diff);

            return from + diff * QcMath.Clamp01(speed * GetTime(unscaledTime) / dist);
        }

        #endregion

        #region Vectors & Color

        public static bool IsLerpingBySpeed(ref Vector2 from, Vector2 to, float speed, bool unscaledTime)
        {
            if (from == to)
                return false;

            from = Vector2.LerpUnclamped(from, to, SpeedToPortion(speed, Vector2.Distance(from, to), unscaledTime: unscaledTime));
            return true;
        }

        public static Vector2 LerpBySpeed(Vector2 from, Vector2 to, float speed, bool unscaledTime)
        {
            float distance = Vector2.Distance(from, to);
            float portion = SpeedToPortion(speed, dist: distance, unscaledTime: unscaledTime);

            return Vector2.LerpUnclamped(from, to, portion);
        }
        public static Vector2 LerpBySpeed(Vector2 from, Vector2 to, float speed, out float portion, bool unscaledTime = false)
        {
            portion = SpeedToPortion(speed, Vector2.Distance(from, to), unscaledTime: unscaledTime);
            return Vector2.LerpUnclamped(from, to, portion);
        }

        public static Vector3 LerpBySpeed(Vector3 from, Vector3 to, float speed, bool unscaledTime) =>
            Vector3.LerpUnclamped(from, to, SpeedToPortion(speed, Vector3.Distance(from, to), unscaledTime: unscaledTime));

        public static bool IsLerpingBySpeed(ref Vector3 from, Vector3 to, float speed, bool unscaledTime)
        {
            if (from == to)
                return false;

            from = Vector3.LerpUnclamped(from, to, SpeedToPortion(speed, Vector3.Distance(from, to), unscaledTime: unscaledTime));
            return true;
        }

        public static Vector3 LerpBySpeed(Vector3 from, Vector3 to, float speed, out float portion, bool unscaledTime)
        {
            portion = SpeedToPortion(speed, Vector3.Distance(from, to), unscaledTime: unscaledTime);
            return Vector3.LerpUnclamped(from, to, portion);
        }

        public static Vector3 LerpByDistance(Vector3 from, Vector3 to, float moveDistance, out float portion)
        {
            float totalDistance = Vector3.Distance(from, to);

            if (totalDistance < moveDistance) 
            {
                portion = 1;
                return to;
            }

            portion = moveDistance / totalDistance;

            return Vector3.LerpUnclamped(from, to, portion);
        }

        public static Vector3 LerpBySpeed_DirectionFirst(Vector3 from, Vector3 to, float speed, bool unscaledTime)
        {

            const float precision = float.Epsilon * 10;

            var fromMagn = from.magnitude;
            var toMagn = to.magnitude;

            float dist = Vector3.Distance(from, to);

            float pathThisFrame = speed * Time.deltaTime;

            if (pathThisFrame >= dist)
                return to;

            if (fromMagn * toMagn < precision)
                return LerpBySpeed(from, to, speed, unscaledTime: unscaledTime);

            var toNormalized = to.normalized;

            var targetDirection = (fromMagn + toMagn) * 0.5f * toNormalized;

            var toTargetDirection = targetDirection - from;

            float rotDiffMagn = toTargetDirection.magnitude;

            if (pathThisFrame > rotDiffMagn)
            {

                pathThisFrame -= rotDiffMagn;

                from = targetDirection;

                var newDiff = to - from;

                from += newDiff.normalized * pathThisFrame;

            }
            else
                from += toTargetDirection * pathThisFrame / rotDiffMagn;


            return from;
        }

        public static Vector4 LerpBySpeed(Vector4 from, Vector4 to, float speed, bool unscaledTime) =>
            Vector4.LerpUnclamped(from, to, SpeedToPortion(speed, Vector4.Distance(from, to), unscaledTime: unscaledTime));

        public static Vector4 LerpBySpeed(Vector4 from, Vector4 to, float speed, out float portion, bool unscaledTime)
        {
            portion = SpeedToPortion(speed, Vector4.Distance(from, to), unscaledTime: unscaledTime);
            return Vector4.LerpUnclamped(from, to, portion);
        }

        public static Quaternion LerpBySpeed(Quaternion from, Quaternion to, float speedInDegrees, bool unscaledTime) =>
            Quaternion.LerpUnclamped(from, to, SpeedToPortion(speedInDegrees, Quaternion.Angle(from, to), unscaledTime: unscaledTime));

        public static Quaternion LerpBySpeed(Quaternion from, Quaternion to, float speedInDegrees, out float portion, bool unscaledTime)
        {
            portion = SpeedToPortion(speedInDegrees, Quaternion.Angle(from, to), unscaledTime: unscaledTime);
            return Quaternion.LerpUnclamped(from, to, portion);
        }

        public static float DistanceRgb(Color col, Color other)
            =>
                (Mathf.Abs(col.r - other.r) + Mathf.Abs(col.g - other.g) + Mathf.Abs(col.b - other.b));

        public static float DistanceRgba(Color col, Color other) =>
                ((Mathf.Abs(col.r - other.r) + Mathf.Abs(col.g - other.g) + Mathf.Abs(col.b - other.b)) * 0.33f +
                 Mathf.Abs(col.a - other.a));

        public static float DistanceRgba(this Color col, Color other, ColorMask mask) =>
             (mask.HasFlag(ColorMask.R) ? Mathf.Abs(col.r - other.r) : 0) +
             (mask.HasFlag(ColorMask.G) ? Mathf.Abs(col.g - other.g) : 0) +
             (mask.HasFlag(ColorMask.B) ? Mathf.Abs(col.b - other.b) : 0) +
             (mask.HasFlag(ColorMask.A) ? Mathf.Abs(col.a - other.a) : 0);

        public static Color LerpBySpeed(this Color from, Color to, float speed, bool unscaledTime) =>
            Color.LerpUnclamped(from, to, SpeedToPortion(speed, DistanceRgb(from, to), unscaledTime: unscaledTime));

        public static Color LerpRgb(this Color from, Color to, float speed, out float portion, bool unscaledTime)
        {
            portion = SpeedToPortion(speed, DistanceRgb(from, to), unscaledTime: unscaledTime);
            to.a = from.a;
            return Color.LerpUnclamped(from, to, portion);
        }

        public static Color LerpRgba(this Color from, Color to, float speed, out float portion, bool unscaledTime)
        {
            portion = SpeedToPortion(speed, DistanceRgba(from, to), unscaledTime: unscaledTime);
            return Color.LerpUnclamped(from, to, portion);
        }

        #endregion

        #region Components

        public static bool IsLerpingAlphaBySpeed(this CanvasGroup grp, float alpha, float speed)
        {
            if (!grp) 
                return false;

            var current = grp.alpha;

            if (IsLerpingBySpeed(ref current, alpha, speed, unscaledTime: true))
            {
                grp.alpha = current;
                return true;
            }

            return false;
        }

        public static bool IsLerpingAlphaBySpeed<T>(this List<T> graphicList, float alpha, float speed) where T : Graphic
        {

            if (graphicList.IsNullOrEmpty()) return false;

            var changing = false;

            foreach (var i in graphicList)
                changing |= i.IsLerpingAlphaBySpeed(alpha, speed);

            return changing;
        }

        public static bool IsLerpingAlphaBySpeed<T>(this T img, float alpha, float speed) where T : Graphic
        {
            if (!img) return false;

            var changing = false;

            var col = img.color;
            col.a = LerpBySpeed(col.a, alpha, speed, unscaledTime: true);

            img.color = col;
            changing |= !Mathf.Approximately(col.a, alpha);

            return changing;
        }

        public static bool IsLerpingRgbBySpeed<T>(this T img, Color target, float speed) where T : Graphic
        {
            bool changing = false;

            if (img)
            {
                img.color = img.color.LerpRgb(target, speed, out float portion, unscaledTime: true);

                changing = portion < 1;
            }

            return changing;
        }

        public static bool IsLerpingBySpeed_Volume(this AudioSource src, float target, float speed)
        {
            if (!src)
                return false;

            var vol = src.volume;

            if (IsLerpingBySpeed(ref vol, target, speed, unscaledTime: true))
            {
                src.volume = vol;
                return true;
            }

            return false;
        }

        #endregion

        public static void Update(this LerpData ld, ILinkedLerping target, bool canSkipLerp)
        {
            ld.Reset();
            target.Portion(ld);
            target.Lerp(ld, canSkipLerp: canSkipLerp);
        }

        public static void SkipLerp<T>(this T obj, LerpData ld) where T : ILinkedLerping
        {
            ld.Reset();
            obj.Portion(ld);
            ld.MinPortion = 1;
            obj.Lerp(ld, true);
        }

        public static void SkipLerp<T>(this T obj) where T : ILinkedLerping
        {
            var ld = new LerpData(unscaledTime: true);
            obj.Portion(ld);
            ld.MinPortion = 1;
            obj.Lerp(ld, true);
        }

        public static void Portion<T>(this T[] list, LerpData ld) where T : ILinkedLerping
        {

            if (typeof(Object).IsAssignableFrom(typeof(T)))
            {
                for (int i = list.Length - 1; i >= 0; i--)
                {

                    var e = list[i];
                    if (!QcUnity.IsNullOrDestroyed_Obj(e))
                        e.Portion(ld);
                }
            }
            else for (int i = list.Length - 1; i >= 0; i--)
                {
                    var e = list[i];
                    e?.Portion(ld);
                }
        }

        public static void Portion<T>(this List<T> list, LerpData ld) where T : ILinkedLerping
        {

            if (typeof(Object).IsAssignableFrom(typeof(T)))
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var e = list[i];
                    if (!QcUnity.IsNullOrDestroyed_Obj(e))
                        e.Portion(ld);
                }

            }
            else for (int i = list.Count - 1; i >= 0; i--)
                {
                    var e = list[i];
                    e?.Portion(ld);
                }

        }

        public static void Lerp<T>(this T[] array, LerpData ld, bool canSkipLerp = false) where T : ILinkedLerping
        {

            if (typeof(Object).IsAssignableFrom(typeof(T)))
            {
                for (int i = array.Length - 1; i >= 0; i--)
                {

                    var e = array[i];
                    if (!QcUnity.IsNullOrDestroyed_Obj(e))
                        e.Lerp(ld, canSkipLerp: canSkipLerp);
                }
            }
            else for (int i = array.Length - 1; i >= 0; i--)
                {
                    var e = array[i];
                    e?.Lerp(ld, canSkipLerp: canSkipLerp);
                }
        }

        public static void Lerp<T>(this List<T> list, LerpData ld, bool canSkipLerp = false) where T : ILinkedLerping
        {

            if (typeof(Object).IsAssignableFrom(typeof(T)))
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var e = list[i];
                    if (!QcUnity.IsNullOrDestroyed_Obj(e))
                        e.Lerp(ld, canSkipLerp: canSkipLerp);
                }

            }
            else for (int i = list.Count - 1; i >= 0; i--)
                {
                    var e = list[i];
                    e?.Lerp(ld, canSkipLerp: canSkipLerp);
                }

        }



        public class DurationLerp 
        {
            public abstract class Base 
            {
                private readonly float _speed = 1;
                private float _totalFraction;
                private float _deltaFraction;
                private readonly Gate.Frame _frameGate = new();

                public bool IsDone => _totalFraction >= 1f;

                protected abstract float DeltaTime { get; }

                float DeltaFraction => DeltaTime * _speed;

                public float GetLerpDelta()
                {
                    if (IsDone)
                        return 1;

                    CheckUpdate();

                    return _deltaFraction;
                }

                public float GetLerpTotal()
                {
                    if (IsDone)
                        return 1;

                    CheckUpdate();

                    return _totalFraction;
                }

                private void CheckUpdate() 
                {
                    if (_frameGate.TryEnter())
                    {
                        float newTotalFraction = Mathf.Clamp01(_totalFraction + DeltaFraction);
                        _deltaFraction = Mathf.Clamp01((newTotalFraction - _totalFraction) / (1 - _totalFraction));
                        _totalFraction = newTotalFraction;
                    }
                }

                public IEnumerator TotalFractionCoroutine(Action<float> onTotalFractionChange) 
                {
                    while (!IsDone) 
                    {
                        onTotalFractionChange.Invoke(GetLerpTotal());
                        yield return null;
                    }
                }

                public IEnumerator DeltaFractionCoroutine(Action<float> onDeltaFractionChange)
                {
                    while (!IsDone)
                    {
                        onDeltaFractionChange.Invoke(GetLerpDelta());
                        yield return null;
                    }
                }

                public Base(float speed = 1)
                {
                    _speed = speed;
                }
            }

            public class FloatUnscaled : Base
            {

                public FloatUnscaled(float speed = 1) : base (speed) { }

                protected override float DeltaTime => Time.unscaledDeltaTime;
            }

            public class FloatScaled : Base
            {

                public FloatScaled(float speed = 1) : base(speed) { }

                protected override float DeltaTime => Time.deltaTime;
            }
        }

    }
}