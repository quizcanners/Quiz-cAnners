using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace QuizCanners.Lerp
{
    public static class QcLerp
    {
        const MethodImplOptions INLINE = MethodImplOptions.AggressiveInlining;

        public static class SmoothState
        {
            public abstract class Generic<T> : IPEGI
            {
                protected T _previousSpeed;
                protected T _speed;

                protected T _from;
                protected T _target;

                public T Target => _target;

                protected float _totalDistance;

                protected float _totalTimeToReach;
                protected float _currentTime;
                protected float _fraction01;

                //  protected virtual bool IsNotInitialized(T val) => val.Equals(default(T));

                public bool HasTarget { get; private set; }

                public bool IsLerping { get; private set; }

                private readonly Gate.Frame _updateFrame_Armed = new();

                public virtual void SetTarget(T newTarget) 
                {
                    _target = newTarget;
                    HasTarget = true;
                    IsLerping = true;
                }

                public virtual void Inspect() => "Time: {0}/{1}".F(_currentTime, _totalTimeToReach).NL();
                
                protected abstract float GetDistance(T from, T to);

                protected abstract void ActualLerp(ref T value, T target, T inertion, float fraction01, out T speed);
                protected abstract T Multiply(T speed, float value);

                public T SkipToFinish() 
                {
                    if (!IsLerping)
                        return  _target;

                    IsLerping = false;
                    _speed = default;
                    return _target;
                }

                public virtual T Update(ref T value)
                {
                    if (!IsLerping)
                    {
                        return HasTarget ? _target : value; 
                    }

                    if (!_updateFrame_Armed.TryConsume())
                        return value;

                    _currentTime += Time.deltaTime;

                    var rawfraction01 = Mathf.Clamp01(_currentTime / _totalTimeToReach);

                    if (Mathf.Approximately(rawfraction01, 1))
                    {
                        IsLerping = false;
                        _speed = default;
                        value = _target;
                        return value;
                    }

                    _fraction01 = Mathf.SmoothStep(0, 1, Mathf.SmoothStep(0, 1, rawfraction01));
                    T inertion = Multiply(_previousSpeed, _currentTime * (1 - rawfraction01));
                    ActualLerp(ref value, target: _target, inertion, _fraction01, out _speed);

                    return value;
                }

                public float GetHill() =>(0.5f - Mathf.Abs(_fraction01 - 0.5f));

                protected virtual void Reset(T from, T to) 
                {
                    _from = from;
                    SetTarget(to);
                    _currentTime = 0;

                    if (_updateFrame_Armed.TryConsume_IfElapsedOrFirst(2))
                        _speed = default;

                    _previousSpeed = _speed;

                    _totalDistance = GetDistance(to, from);
                    IsLerping = true; // _totalDistance > 0;
                }

                public void SetLerpByTime(ref T from, T to, float timeToReach)
                {
                    Reset(from, to);
                    _totalTimeToReach = timeToReach;
                    Update(ref from);
                }

                public void SetLerpBySpeed(ref T from, T to, float speed)
                {
                    Reset(from, to);
                    if (IsLerping)
                        _totalTimeToReach = _totalDistance / speed;
                    else
                        _totalTimeToReach = 0;
                    Update(ref from);
                }
            }

            public class Float : Generic<float>
            {
                protected override float Multiply(float speed, float value) => speed * value;
                
                protected override void ActualLerp(ref float value, float target, float inertion, float fraction01, out float speed)
                {
                    var prevValue = value;
                    value = Mathf.Lerp(_from, target, fraction01) + inertion;
                    speed = (value - prevValue) / Time.deltaTime;
                }

                protected override float GetDistance(float from, float to) => Math.Abs(to - from);
            }

            public class Vector2Value : Generic<Vector2>
            {
                protected override Vector2 Multiply(Vector2 speed, float value) => speed * value;

                protected override void ActualLerp(ref Vector2 value, Vector2 target, Vector2 inertion, float fraction01, out Vector2 speed)
                {
                    var prevValue = value;
                    value = Vector2.Lerp(_from , target, fraction01) + inertion;
                    speed = (value - prevValue) / Time.deltaTime;
                }

                protected override float GetDistance(Vector2 from, Vector2 to) => Vector2.Distance(to, from);
            }

            public class Vector3Value : Generic<Vector3>
            {
                protected override Vector3 Multiply(Vector3 speed, float value) => speed * value;

                protected override void ActualLerp(ref Vector3 value, Vector3 target, Vector3 inertion, float fraction01, out Vector3 speed)
                {
                    var prevValue = value;
                    value = Vector3.Lerp(_from, target, fraction01) + inertion;
                    speed = (value - prevValue) / Time.deltaTime;
                }

                protected override float GetDistance(Vector3 from, Vector3 to) => Vector3.Distance(to, from);
            }

            public class QuaternionValue : Generic<Quaternion>
            {
                public bool UseIntermediate;
                public Quaternion IntermediateValue;

                public void SetIntermediateValue(Quaternion quat) 
                {
                    UseIntermediate = true;
                    IntermediateValue = quat;
                }

                protected override void Reset(Quaternion from, Quaternion to)
                {
                    UseIntermediate = false;
                    base.Reset(from, to);
                }

                protected override Quaternion Multiply(Quaternion speed, float value) => Quaternion.Slerp(Quaternion.identity, speed, value);// speed * value;

                protected override void ActualLerp(ref Quaternion value, Quaternion target, Quaternion inertion, float fraction01, out Quaternion speed)
                {
                    var prevValue = value;
                    value = Quaternion.Lerp(_from, target, fraction01);
                    value *= inertion;

                    if (UseIntermediate)
                        value = Quaternion.Lerp(value, IntermediateValue, GetHill());

                    speed = Multiply(Quaternion.Inverse(prevValue) * value, 1f / Time.deltaTime);
                }

                protected override float GetDistance(Quaternion from, Quaternion to) => 2 * Mathf.Acos(Mathf.Clamp(Quaternion.Dot(from, to), -1.0f, 1.0f));
            }

        }

        #region Float

        /*
        public class SmoothLerpState 
        {
            private float _previousSpeed;
            private float _from;
            private float _target;

            private float _totalDistance;
            private float _speed;
            float _totalTime;
            float _currentTime;

            bool _enabled;

            private readonly Gate.Frame _updateFrame = new();

            public void Update(ref float value) 
            {
                if (!_enabled)
                    return;

                if (!_updateFrame.TryEnter())
                    return;

                _currentTime += Time.deltaTime;

                float previousInertion = _previousSpeed * _currentTime;

                float rawfraction01 = Mathf.Clamp01(_currentTime / _totalTime);

                if (Mathf.Approximately(rawfraction01, 1))
                {
                    _enabled = false;
                    _speed = 0;
                    value = _target;
                }
                else
                {
                    float fraction01 = Mathf.SmoothStep(0, 1, Mathf.SmoothStep(0, 1, rawfraction01));
                    float inertion = Mathf.Lerp(previousInertion, 0, 1f - Mathf.Pow(1 - rawfraction01, 3));
                    var prevValue = value;
                    value = Mathf.Lerp(_from + inertion, _target, fraction01);
                    _speed = (value - prevValue) / Time.deltaTime;
                }
            }

            public void SetLerpTarget(ref float from, float to, float timeToReach) 
            {
                _from = from;
                _target = to;
                _totalDistance = Mathf.Abs(to - from);
                _enabled = _totalDistance > 0;

                _totalTime = timeToReach;
                _currentTime = 0;
                _previousSpeed = _speed;

                Update(ref from);
            }
        }

        public static bool IsSmoothLerp(ref float from, float to, float duration, bool unscaledTime, float precision = 0.01f)
        {
            var deltaTime = GetDeltaTime(unscaledTime);

            float fraction = 1f - Mathf.Pow(precision, deltaTime / duration);

            from = Mathf.Lerp(from, to, fraction);

            return fraction > 0.999f;
        }*/

        [MethodImpl(INLINE)]
        public static float ExpLerp(float from, float to, float portion) => Mathf.Pow(from, 1 - portion) * Mathf.Pow(to, portion);
        
        private static float GetDeltaTime(bool unscaledTime) => unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        private static float SpeedToPortion(float speed, float dist, bool unscaledTime)
        {
            if (dist == 0)
                return 1;

            dist = Mathf.Abs(dist);

            var time = GetDeltaTime(unscaledTime);

            float portion = Mathf.Clamp01(speed * time / dist);

            return portion;
        }

        private static float SpeedToPortion_Unscaled(float speed, float dist)
        {
            if (dist == 0)
                return 1;

            return Mathf.Clamp01(speed * Time.unscaledDeltaTime / Mathf.Abs(dist));
        }

        private static float SpeedToPortion_Scaled(float speed, float dist)
        {
            if (dist == 0)
                return 1;

            dist = Mathf.Abs(dist);
            var time = Time.deltaTime;
            float portion = Mathf.Clamp01(speed * time / dist);
            return portion;
        }

        private static float SpeedToPortion_Fixed_Scaled(float speed, float dist)
        {
            if (dist == 0)
                return 1;

            dist = Mathf.Abs(dist);
            float portion = Mathf.Clamp01(speed * Time.fixedDeltaTime / dist);

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
            if (from == to)
                return false;

            from = Mathf.LerpUnclamped(from, to, SpeedToPortion(speed, Mathf.Abs(from - to), unscaledTime: unscaledTime));
            
            return true;
        }

        public static float LerpRadianBySpeed(float v1, float v2, float speed, bool unscaledTime)
        {
            float dist = Mathf.DeltaAngle(v1, v2);
            float portion = SpeedToPortion(speed: speed, dist: dist, unscaledTime: unscaledTime);
            return Mathf.LerpAngle(v1, v2, portion);
        }
        public static float LerpBySpeed_Scaled(float from, float to, float speed)
            => from == to ? to : Mathf.LerpUnclamped(from, to, SpeedToPortion_Scaled(speed, Mathf.Abs(from - to)));

        public static float LerpBySpeed_Unscaled(float from, float to, float speed)
            => from == to ? to : Mathf.LerpUnclamped(from, to, SpeedToPortion_Unscaled(speed, Mathf.Abs(from - to)));

        public static float LerpBySpeed(float from, float to, float speed, bool unscaledTime)
            => from == to ? to : Mathf.LerpUnclamped(from, to, SpeedToPortion(speed, Mathf.Abs(from - to), unscaledTime: unscaledTime));

        public static float LerpBySpeed(float from, float to, float speed, out float portion, bool unscaledTime)
        {
            if (from == to)
            {
                portion = 1;
                return to;
            }

            portion = SpeedToPortion(speed, Mathf.Abs(from - to), unscaledTime: unscaledTime);
            return Mathf.LerpUnclamped(from, to, portion);
        }

        public static float LerpBySpeed_Fixed_Scaled(float from, float to, float speed) =>
            from == to ? to : Mathf.LerpUnclamped(from, to, SpeedToPortion_Fixed_Scaled(speed, Mathf.Abs(from - to)));

        #endregion

        #region Double

        public static bool IsLerpingBySpeed(ref double from, double to, double speed, bool unscaledTime)
        {
            if (from == to) //System.Math.Abs(from - to) < double.Epsilon * 10)
            {
                return false;
            }

            double diff = to - from;

            double dist = System.Math.Abs(diff);

            from += diff * QcMath.Clamp01(speed * GetDeltaTime(unscaledTime) / dist);
            return true;
        }

        public static double LerpBySpeed(double from, double to, double speed, bool unscaledTime)
        {
            if ( from == to)
                return from;

            double diff = to - from;

            double dist = System.Math.Abs(diff);

            return from + diff * QcMath.Clamp01(speed * GetDeltaTime(unscaledTime) / dist);
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
            if (from == to)
                return to;

            float distance = Vector2.Distance(from, to);
            float portion = SpeedToPortion(speed, dist: distance, unscaledTime: unscaledTime);

            return Vector2.LerpUnclamped(from, to, portion);
        }
        public static Vector2 LerpBySpeed(Vector2 from, Vector2 to, float speed, out float portion, bool unscaledTime = false)
        {
            if (from == to)
            {
                portion = 1;
                return to;
            }

            portion = SpeedToPortion(speed, Vector2.Distance(from, to), unscaledTime: unscaledTime);
            return Vector2.LerpUnclamped(from, to, portion);
        }

        public static Vector3 LerpBySpeed(Vector3 from, Vector3 to, float speed, bool unscaledTime) =>
            from == to ? to : Vector3.LerpUnclamped(from, to, SpeedToPortion(speed, Vector3.Distance(from, to), unscaledTime: unscaledTime));

        public static Vector3 LerpBySpeed_Fixed_Scaled(Vector3 from, Vector3 to, float speed) =>
          from == to ? to : Vector3.LerpUnclamped(from, to, SpeedToPortion_Fixed_Scaled(speed, Vector3.Distance(from, to)));

        public static bool IsLerpingBySpeed(ref Vector3 from, Vector3 to, float speed, bool unscaledTime)
        {
            if (from == to)
                return false;

            from = Vector3.LerpUnclamped(from, to, SpeedToPortion(speed, Vector3.Distance(from, to), unscaledTime: unscaledTime));
            return true;
        }

        public static Vector3 LerpBySpeed(Vector3 from, Vector3 to, float speed, out float portion, bool unscaledTime)
        {
            if (from == to)
            {
                portion = 1;
                return to;
            }

            portion = SpeedToPortion(speed, Vector3.Distance(from, to), unscaledTime: unscaledTime);
            return Vector3.LerpUnclamped(from, to, portion);
        }

        public static Vector3 LerpByDistance(Vector3 from, Vector3 to, float moveDistance, out float portion)
        {
            float totalDistance = Vector3.Distance(from, to);

            if (totalDistance <= moveDistance) 
            {
                portion = 1;
                return to;
            }

            portion = moveDistance / totalDistance;

            return Vector3.LerpUnclamped(from, to, portion);
        }

        public static float LerpByDistance(float from, float to, float moveDistance, out float portion)
        {
            float totalDistance = Mathf.Abs(from - to);

            if (totalDistance <= moveDistance)
            {
                portion = 1;
                return to;
            }

            portion = moveDistance / totalDistance;

            return Mathf.LerpUnclamped(from, to, portion);
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
            from == to ? to : Vector4.LerpUnclamped(from, to, SpeedToPortion(speed, Vector4.Distance(from, to), unscaledTime: unscaledTime));

        public static Vector4 LerpBySpeed(Vector4 from, Vector4 to, float speed, out float portion, bool unscaledTime)
        {
            if (from == to)
            {
                portion = 1;
                return to;
            }

            portion = SpeedToPortion(speed, Vector4.Distance(from, to), unscaledTime: unscaledTime);
            return Vector4.LerpUnclamped(from, to, portion);
        }

        public static Quaternion LerpBySpeed(Quaternion from, Quaternion to, float speedInDegrees, bool unscaledTime) =>
            from == to ? to : Quaternion.LerpUnclamped(from, to, SpeedToPortion(speedInDegrees, Quaternion.Angle(from, to), unscaledTime: unscaledTime));

        public static Quaternion LerpBySpeed(Quaternion from, Quaternion to, float speedInDegrees, out float portion, bool unscaledTime)
        {
            if (from == to)
            {
                portion = 1;
                return to;
            }

            portion = SpeedToPortion(speedInDegrees, Quaternion.Angle(from, to), unscaledTime: unscaledTime);
            return Quaternion.LerpUnclamped(from, to, portion);
        }

        public static bool IsLerpingBySpeed(ref Quaternion from, Quaternion to, float speedInDegrees, bool unscaledTime)
        {
            if (from == to)
                return false;

            from = Quaternion.LerpUnclamped(from, to, SpeedToPortion(speedInDegrees, Quaternion.Angle(from, to), unscaledTime: unscaledTime));
            return true;
        }

        public static float DistanceRgb(Color col, Color other)
            =>
                (Mathf.Abs(col.r - other.r) + Mathf.Abs(col.g - other.g) + Mathf.Abs(col.b - other.b));

        public static float DistanceRgba(Color col, Color other) =>
                ((Mathf.Abs(col.r - other.r) + Mathf.Abs(col.g - other.g) + Mathf.Abs(col.b - other.b)) * 0.33f +
                 Mathf.Abs(col.a - other.a));

        public static float DistanceRgba(this Color col, Color other, ColorMask mask) =>
             ((mask & ColorMask.R) != 0 ? Mathf.Abs(col.r - other.r) : 0) +
             ((mask & ColorMask.G) != 0 ? Mathf.Abs(col.g - other.g) : 0) +
             ((mask & ColorMask.B) != 0 ? Mathf.Abs(col.b - other.b) : 0) +
             ((mask & ColorMask.A) != 0 ? Mathf.Abs(col.a - other.a) : 0);

        public static Color LerpBySpeed(this Color from, Color to, float speed, bool unscaledTime) =>
            from == to ? to : Color.LerpUnclamped(from, to, SpeedToPortion(speed, DistanceRgb(from, to), unscaledTime: unscaledTime));

        public static Color LerpRgb(this Color from, Color to, float speed, out float portion, bool unscaledTime)
        {
            if (from.r == to.r && from.g == to.g && from.b == to.b)
            {
                portion = 1;
                to.a = from.a;
                return to;
            }

            portion = SpeedToPortion(speed, DistanceRgb(from, to), unscaledTime: unscaledTime);
            to.a = from.a;
            return Color.LerpUnclamped(from, to, portion);
        }

        public static Color LerpRgba(this Color from, Color to, float speed, out float portion, bool unscaledTime)
        {
            if (from == to)
            {
                portion = 1;
                return to;
            }

            portion = SpeedToPortion(speed, DistanceRgba(from, to), unscaledTime: unscaledTime);
            return Color.LerpUnclamped(from, to, portion);
        }

        #endregion

        #region Components

        public static bool IsLerpingAlphaBySpeed(this CanvasGroup grp, float alpha, float speed = 4, bool controlRaycasts = true, bool disableGameObject = false)
        {
            if (!grp)
            {
                QcLog.ChillLogger.LogErrosExpOnly(()=> "Missing Canvas Group", "NoCvsGrp");
                return false;
            }

            var current = grp.alpha;

            bool isLerping = false;

            if (IsLerpingBySpeed(ref current, alpha, speed, unscaledTime: true))
            {
                grp.alpha = current;
                isLerping = true;
            }

            if (disableGameObject)
                grp.gameObject.SetActive(alpha > 0);

            if (controlRaycasts) 
            {
                bool rc = alpha > 0.25f;
                grp.interactable = rc;
                grp.blocksRaycasts = rc;
            }

            return isLerping;
        }

        public static bool IsLerpingAlphaBySpeed<T>(this List<T> graphicList, float alpha, float speed) where T : Graphic
        {

            if (graphicList.IsNullOrEmpty()) return false;

            var changing = false;

            foreach (var i in graphicList)
                changing |= i.IsLerpingAlphaBySpeed(alpha, speed);

            return changing;
        }

        public static bool IsLerpingAlphaBySpeed<T>(this T img, float alpha, float speed = 4) where T : Graphic
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

        public static bool IsLerpingColorBySpeed<T>(this List<T> graphicList, Color target, float speed) where T : Graphic
        {
            bool changing = false;

            if (graphicList.IsNullOrEmpty()) return false;

            foreach (var i in graphicList)
                changing |= i.IsLerpingRgbBySpeed(target, speed);

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

        public static void Update(this LerpContext ld, ILinkedLerping target, bool canSkipLerp)
        {
            ld.Reset();
            target.Portion(ld);
            target.Lerp(ld, canSkipLerp: canSkipLerp);
        }

        public static void SkipLerp<T>(this T obj, LerpContext ld) where T : ILinkedLerping
        {
            ld.Reset();
            obj.Portion(ld);
            ld.MinPortion = 1;
            obj.Lerp(ld, true);
        }

        public static void SkipLerp<T>(this T obj) where T : ILinkedLerping
        {
            var ld = new LerpContext(unscaledTime: true);
            obj.Portion(ld);
            ld.MinPortion = 1;
            obj.Lerp(ld, true);
        }

        public static void Portion<T>(this T[] list, LerpContext ld) where T : ILinkedLerping
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

        public static void Portion<T>(this List<T> list, LerpContext ld) where T : ILinkedLerping
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

        public static void Lerp<T>(this T[] array, LerpContext ld, bool canSkipLerp = false) where T : ILinkedLerping
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

        public static void Lerp<T>(this List<T> list, LerpContext ld, bool canSkipLerp = false) where T : ILinkedLerping
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

        public static Color LerpHeatmapColors(this Color[] colors, float value01)
        {
            int count = colors.Length;

            if (count == 0)
                return Color.magenta;

            if (count == 1)
                return colors[0];

            float step = Mathf.Clamp01(value01 - 0.01f) * (count - 1);

            int firstColorIndex = Mathf.FloorToInt(step);

            float transition = step - firstColorIndex;

            return Color.Lerp(colors[firstColorIndex], colors[firstColorIndex + 1], transition);
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
                    if (_frameGate.TryConsume())
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
