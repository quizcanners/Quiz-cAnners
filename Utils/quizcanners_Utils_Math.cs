using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Utils
{

    public enum ColorChanel { R = 0, G = 1, B = 2, A = 3 }

    [Flags]
    public enum ColorMask { R = 1, G = 2, B = 4, A = 8, Color = 7, All = 15 }


    public static class QcMath
    {
        public static float SmoothStep(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        public static IDisposable RandomBySeedDisposable(int seed)
        {
            var state = UnityEngine.Random.state;
            UnityEngine.Random.InitState(seed);
            return QcSharp.DisposableAction(() => UnityEngine.Random.state = state );
        }

        #region Double

        public static double Clamp(double value, double min, double max) => value < min ? min : (value > max ? max : value);

        public static double Clamp01(double value) => value < 0 ? 0 : (value > 1 ? 1 : value);

        #endregion

        #region Time

        public static double Miliseconds_To_Seconds(double interval) => (interval * 0.001);

        public static double Seconds_To_Miliseconds(double interval) => (interval * 1000);

        public static float Miliseconds_To_Seconds(float interval) => (interval * 0.001f);

        public static float Seconds_To_Miliseconds(float interval) => (interval * 1000);

        #endregion

        #region Adjust

        public static Vector2 To01Space(this Vector2 v2)
        {

            v2.x %= 1;
            v2.y %= 1;

            v2 += Vector2.one;

            v2.x %= 1;
            v2.y %= 1;

            return v2;
        }

        public static Vector2 Floor(this Vector2 v2) => new Vector2(Mathf.Floor(v2.x), Mathf.Floor(v2.y));

        public static bool ClampIndexToCount(this ICollection list, ref int value, int min = 0)
        {
            if (!list.IsNullOrEmpty())
            {
                value = Mathf.Max(min, Mathf.Min(value, list.Count - 1));
                return true;
            }
            return false;
        }

        public static Vector3 RoundDiv(Vector3 v3, int by)
        {
            v3 /= by;
            return ((new Vector3(Mathf.Round(v3.x), Mathf.Round(v3.y), Mathf.Round(v3.z))) * by);
        }

        #endregion

        #region Trigonometry

        public static void Clamp(this Vector2Int value, int from, int to)
        {
            value.Clamp(new Vector2Int(from, from), new Vector2Int(to, to));
        }
        public static float BezierCurveLength(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int pointCount = 30) 
        {
            //The default 30 
            float length = 0.0f;
            Vector3 lastPoint = BezierPoint(0.0f / (float)pointCount, p0, p1, p2, p3);
            for (int i = 1; i <= pointCount; i++)
            {
                Vector3 point = BezierPoint((float)i / (float)pointCount, p0, p1, p2, p3);
                length += Vector3.Distance(point, lastPoint);
                lastPoint = point;
            }
            return length;
        }

        public static List<Vector3> BezierCurvePoints(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, out float length, int pointCount = 30)
        {
            var rawPoints = new List<Vector3>();

            length = 0.0f;
            Vector3 lastPoint = BezierPoint(0, p0, p1, p2, p3);

            rawPoints.Add(p0);
            for (int i = 1; i <= pointCount; i++)
            {
                var point = BezierPoint((float)i / (float)pointCount, p0, p1, p2, p3);
                length += Vector3.Distance(point, lastPoint);
                lastPoint = point;
                rawPoints.Add(point);
            }

            rawPoints.Add(p3);

            var adjustedPoints = new List<Vector3>();

            adjustedPoints.Add(p0);
            float segmentLength = length / (pointCount-1); // Because X points translates into X-1 segments
            float forNextAdjustedSegment = segmentLength;
            int rawIndex = 1;
            bool rawSegmentValid = false;
            float rawSegmentLength = 0;
            Vector3 rawPointA = Vector3.zero;
            Vector3 rawPointB = Vector3.zero;

            while (adjustedPoints.Count < pointCount-1) 
            {
                if (!rawSegmentValid)
                {
                    rawPointA = rawPoints[rawIndex - 1];
                    rawPointB = rawPoints[rawIndex];
                    rawSegmentLength = Vector3.Distance(rawPointA, rawPointB);
                }

                if (rawSegmentLength >= forNextAdjustedSegment)
                {
                    adjustedPoints.Add(Vector3.Lerp(rawPointA, rawPointB, forNextAdjustedSegment / rawSegmentLength));
                    forNextAdjustedSegment += segmentLength;
                } else 
                {
                    forNextAdjustedSegment -= rawSegmentLength;
                    rawSegmentValid = false;
                    rawIndex++;
                    if (rawIndex >= rawPoints.Count) 
                    {
                        Debug.LogError("Outside of Raw Indexes. Breaking");
                        break;
                    }
                }
                
            }

            adjustedPoints.Add(p3);

            return adjustedPoints;
        }


        public static Vector3 BezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            Vector3 p = uuu * p0;
            p += 3 * uu * t * p1;
            p += 3 * u * tt * p2;
            p += ttt * p3;

            return p;
        }

        public static Vector3 BezierCurve(float portion, Vector3 from, Vector3 mid, Vector3 to)
        {
            Vector3 m1 = Vector3.LerpUnclamped(from, mid, portion);
            Vector3 m2 = Vector3.LerpUnclamped(mid, to, portion);
            return Vector3.LerpUnclamped(m1, m2, portion);
        }

        public static float Angle(this Vector2 vec) =>
             (vec.x < 0) ? 360 - (Mathf.Atan2(vec.x, vec.y) * Mathf.Rad2Deg * -1) :
             Mathf.Atan2(vec.x, vec.y) * Mathf.Rad2Deg;
        
        public static bool IsAcute(float a, float b, float c)
        {
            if (c == 0) return true;
            float longest = Mathf.Max(a, b);
            longest *= longest;
            float side = Mathf.Min(a, b);


            return (longest > (c * c + side * side));

        }

        public static float DistanceFromPointToALine (Vector3 point, Vector3 startp, Vector3 endp) 
        {
            var a = Vector3.Distance(startp, endp);
            var b = Vector3.Distance(startp, point);
            var c = Vector3.Distance(endp, point);
            var s = (a + b + c) / 2;
            return 2 * Mathf.Sqrt(s * (s - a) * (s - b) * (s - c)) / a;
        }

        public static Vector3 GetClosestPointOnALine(Vector3 lineA, Vector3 lineB, Vector3 point) 
        {
            Vector3 a_to_p = point - lineA;
            Vector3 a_to_b = lineB - lineA;

            float atb2 = Vector3.Scale(a_to_b, a_to_b).magnitude; 
            
            var atp_dot_atb = Vector3.Dot(a_to_p, a_to_b);

            float t = atp_dot_atb / atb2; // new Vector3(atp_dot_atb.x/ atb2.x, atp_dot_atb.y / atb2.y, atp_dot_atb.z / atb2.z) ;

            return Vector3.Lerp(lineA, lineB, t);//lineA + Vector3.Scale( a_to_b; 
        }

        public static bool IsPointOnLine(float a, float b, float line, float percision)
        {
            percision *= line;
            float dist;

            if (IsAcute(a, b, line)) dist = Mathf.Min(a, b);
            else
            {
                float s = (a + b + line) / 2;
                float h = 4 * s * (s - a) * (s - b) * (s - line) / (line * line);
                dist = Mathf.Sqrt(h);
            }

            return dist < percision;
        }

        public static bool IsPointOnLine(Vector3 a, Vector3 b, Vector3 point, float percision)
        {
            float line = (b - a).magnitude;
            float pnta = (point - a).magnitude;
            float pntb = (point - b).magnitude;

            return ((line > pnta) && (line > pntb) && ((pnta + pntb) < line + percision));
        }

        public static float HeronHforBase(float _base, float a, float b)
        {
            float sidesSum = a + b;

            if (_base > sidesSum)
                _base = sidesSum * 0.98f;

            float s = (_base + sidesSum) * 0.5f;
            float area = Mathf.Sqrt(s * (s - _base) * (s - a) * (s - b));
            return area / (0.5f * _base);
        }

        public static bool LinePlaneIntersection(out Vector3 intersection, Vector3 linePoint, Vector3 lineVec, Vector3 planeNormal, Vector3 planePoint)
        {

            float dotNumerator = Vector3.Dot(planePoint - linePoint, planeNormal);
            float dotDenominator = Vector3.Dot(lineVec, planeNormal);

            //line and plane are not parallel
            if (Math.Abs(dotDenominator) > float.Epsilon)
            {

                float length = dotNumerator / dotDenominator;

                //get the coordinates of the line-plane intersection point
                intersection = linePoint + lineVec.normalized * length;

                return true;
            }

            //output not valid
            intersection = Vector3.zero;

            return false;

        }

        public static Vector3 GetNormalOfTheTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 p1 = b - a;
            Vector3 p2 = c - a;
            return Vector3.Cross(p1, p2).normalized;
        }

        public static Quaternion Avarage(this List<Quaternion> quaternions)
        {
            Quaternion average = new Quaternion(0, 0, 0, 0);

            int amount = 0;

            foreach (var quaternion in quaternions)
            {
                amount++;

                average = Quaternion.Slerp(average, quaternion, 1f / amount);
            }

            return average;
        }

        #endregion

        #region Transformations

        public static Vector2 Clamp01(this Vector2 v2)
        {
            v2.x = Mathf.Clamp01(v2.x);
            v2.y = Mathf.Clamp01(v2.y);

            return v2;
        }

        public static Vector3 Clamp01(this Vector3 v3)
        {
            v3.x = Mathf.Clamp01(v3.x);
            v3.y = Mathf.Clamp01(v3.y);
            v3.z = Mathf.Clamp01(v3.z);

            return v3;
        }

        public static Vector4 Clamp01(this Vector4 v4)
        {
            v4.x = Mathf.Clamp01(v4.x);
            v4.y = Mathf.Clamp01(v4.y);
            v4.z = Mathf.Clamp01(v4.z);
            v4.w = Mathf.Clamp01(v4.w);

            return v4;
        }

        public static Vector2 Abs(this Vector2 v2)
        {
            v2.x = Mathf.Abs(v2.x);
            v2.y = Mathf.Abs(v2.y);
            return v2;
        }

        public static Vector3 Abs(this Vector3 v3)
        {
            v3.x = Mathf.Abs(v3.x);
            v3.y = Mathf.Abs(v3.y);
            v3.z = Mathf.Abs(v3.z);
            return v3;
        }

        public static Vector4 Abs(this Vector4 v4)
        {
            v4.x = Mathf.Abs(v4.x);
            v4.y = Mathf.Abs(v4.y);
            v4.z = Mathf.Abs(v4.z);
            v4.w = Mathf.Abs(v4.w);
            return v4;
        }

        public static Vector2 Rotate(this Vector2 v, float degrees)
        {
            float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
            float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);

            float tx = v.x;
            float ty = v.y;
            v.x = (cos * tx) - (sin * ty);
            v.y = (sin * tx) + (cos * ty);
            return v;
        }

        public static Vector2 Rotate_Radians(this Vector2 v, float radians)
        {
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);

            float tx = v.x;
            float ty = v.y;
            v.x = (cos * tx) - (sin * ty);
            v.y = (sin * tx) + (cos * ty);
            return v;
        }

        public static Vector2 YX(this Vector2 vec) => new Vector2(vec.y, vec.x);

        public static Vector2 X(this Vector2 vec, float x) => new Vector2(x, vec.y);

        public static Vector2 Y(this Vector2 vec, float y) => new Vector2(vec.x, y);

        public static Vector2 XY(this Vector3 vec) => new Vector2(vec.x, vec.y);

        public static Vector2 YX(this Vector3 vec) => new Vector2(vec.y, vec.x);

        public static Vector2 XZ(this Vector3 vec) => new Vector2(vec.x, vec.z);

        public static Vector3 Z(this Vector3 vec, float z) => new Vector3(vec.x, vec.y, z);

        public static Vector3 Y(this Vector3 vec, float y) => new Vector3(vec.x, y ,vec.z);

        public static Vector3 XYZ(this Vector4 vec) => new Vector3(vec.x, vec.y, vec.z);

        public static Vector2 ZW(this Vector4 vec) => new Vector2(vec.z, vec.w);

        public static Vector2 XY(this Vector4 vec) => new Vector2(vec.x, vec.y);

        public static Vector2 XW(this Vector4 vec) => new Vector2(vec.x, vec.w);

        public static Vector2 ZY(this Vector4 vec) => new Vector2(vec.z, vec.y);

        public static Vector3 ToVector3(this Vector2 v2xy, float z = 0) => new Vector3(v2xy.x, v2xy.y, z);

        public static Vector3 ToVector3XZ(this Vector2 v2xz, float y = 0) => new Vector3(v2xz.x, y, v2xz.y);

        public static Vector4 ToVector4(this Vector2 v2xy, float z = 0, float w = 0) => new Vector4(v2xy.x, v2xy.y, z, w);

        public static Vector4 ToVector4(this Vector2 v2xy, Vector2 v2zw) => new Vector4(v2xy.x, v2xy.y, v2zw.x, v2zw.y);

        public static Vector4 ToVector4(this Vector3 v3xyz, float w = 0) => new Vector4(v3xyz.x, v3xyz.y, v3xyz.z, w);

        public static Vector4 ToVector4(this Rect rect, bool useMinMax) =>
            useMinMax ? new Vector4(rect.xMin, rect.yMin, rect.xMax, rect.yMax) : new Vector4(rect.x, rect.y, rect.width, rect.height);

        public static Vector4 ToVector4(this Color col) => new Vector4(col.r, col.g, col.b, col.a);

        public static Vector4 ToVector4(this Quaternion q) => new Vector4(q.x, q.y, q.z, q.w);

        public static Rect ToRect(this Vector4 v4, bool usingMinMax)
            => usingMinMax ? Rect.MinMaxRect(v4.x, v4.y, v4.z, v4.w) : new Rect(v4.x, v4.y, v4.z, v4.w);

        #endregion
        
        #region Color Channel and Mask
        
        public static string ToText(this ColorMask icon)
        {
            switch (icon)
            {
                case ColorMask.R: return "Red";
                case ColorMask.G: return "Green";
                case ColorMask.B: return "Blue";
                case ColorMask.A: return "Alpha";
                case ColorMask.Color: return "RGB";
                case ColorMask.All: return "All";
                default: return "Unknown channel";
            }
        }

        public static string ToText(this ColorChanel icon)
        {
            switch (icon)
            {
                case ColorChanel.R: return "Red";
                case ColorChanel.G: return "Green";
                case ColorChanel.B: return "Blue";
                case ColorChanel.A: return "Alpha";
                default: return "Unknown channel";
            }
        }
        
        public static float GetValueFrom(this ColorChanel chan, Color col)
        {
            switch (chan)
            {
                case ColorChanel.R:
                    return col.r;
                case ColorChanel.G:
                    return col.g;
                case ColorChanel.B:
                    return col.b;
                default:
                    return col.a;
            }
        }

        public static void SetValueOn(this ColorChanel chan, ref Color col, float value)
        {
            switch (chan)
            {
                case ColorChanel.R:
                    col.r = value;
                    break;
                case ColorChanel.G:
                    col.g = value;
                    break;
                case ColorChanel.B:
                    col.b = value;
                    break;
                case ColorChanel.A:
                    col.a = value;
                    break;
            }
        }

        public static void SetValuesOn(this ColorMask bm, ref Color target, Color source)
        {
            if ((bm & ColorMask.R) != 0)
                target.r = source.r;
            if ((bm & ColorMask.G) != 0)
                target.g = source.g;
            if ((bm & ColorMask.B) != 0)
                target.b = source.b;
            if ((bm & ColorMask.A) != 0)
                target.a = source.a;
        }

        public static void SetValuesOn(this ColorMask bm, ref Color target, Color source, float alpha)
        {
            float deAlpha = 1 - alpha;

            if ((bm & ColorMask.R) != 0)
                target.r = source.r * alpha + target.r * deAlpha;
            if ((bm & ColorMask.G) != 0)
                target.g = source.g * alpha + target.g * deAlpha;
            if ((bm & ColorMask.B) != 0)
                target.b = source.b * alpha + target.b * deAlpha;
            if ((bm & ColorMask.A) != 0)
                target.a = source.a * alpha + target.a * deAlpha;
        }

        public static Vector4 ToVector4(this ColorMask mask) => new Vector4(
            mask.HasFlag(ColorMask.R) ? 1 : 0,
            mask.HasFlag(ColorMask.G) ? 1 : 0,
            mask.HasFlag(ColorMask.B) ? 1 : 0,
            mask.HasFlag(ColorMask.A) ? 1 : 0);

        public static ColorChanel ToColorChannel(this ColorMask bm)
        {
            switch (bm)
            {
                case ColorMask.R:
                    return ColorChanel.R;
                case ColorMask.G:
                    return ColorChanel.G;
                case ColorMask.B:
                    return ColorChanel.B;
                case ColorMask.A:
                    return ColorChanel.A;
            }

            return ColorChanel.A;
        }

        public static void SetValuesOn(this ColorMask bm, ref Vector4 target, Color source)
        {
            if ((bm & ColorMask.R) != 0)
                target.x = source.r;
            if ((bm & ColorMask.G) != 0)
                target.y = source.g;
            if ((bm & ColorMask.B) != 0)
                target.z = source.b;
            if ((bm & ColorMask.A) != 0)
                target.w = source.a;
        }

        public static bool HasFlag(this ColorMask mask, int flag) => (mask & (ColorMask)(Mathf.Pow(2, flag))) != 0;

        public static bool HasFlag(this ColorMask mask, ColorMask flag) => (mask & flag) != 0;
        
        #endregion
        
        public static List<int> NormalizeToPercentage<T>(List<T> list, Func<T,double> getValue, float percentTrashold = 0.1f) 
        {
            List<int> resultPercentages = new List<int>(list.Count);

            if (list.Count == 0)
                return resultPercentages;

            double totalSum = 0;

            List<double> probabilities = new List<double>(list.Count);

            for(int i=0; i< list.Count; i++) 
            {
                var val = getValue(list[i]);
                totalSum += val;
                probabilities.Add(val);
            }

            int totalPercentages = 100;
            double onePercent = totalSum / 100;
            double onePercentCutoff = onePercent * percentTrashold;

            // Distribute 1 percent
            for (int i = 0; i < list.Count; i++)
            {
                var val = probabilities[i];

                if (val >= onePercentCutoff)
                {
                    totalSum -= Math.Min(onePercent, val);
                    totalPercentages -= 1;
                    val -= onePercent;
                    resultPercentages.Add(1);
                }
                else
                    resultPercentages.Add(0);

                probabilities[i] = val;
            }


            var toPercentages = totalPercentages / totalSum;

            // Calculate percentages
            for (int i = 0; i < list.Count; i++)
            {
                probabilities[i] *= toPercentages; // to Percentage
            }


            // Distribute the rest
            double leftOver = 0;
            double percisionErrorCorrection = 0.005d / list.Count; // No to generate an extra value

            for (int i = 0; i < list.Count; i++)
            {
                var val = probabilities[i];

                if (val > 0 )
                {
                    val += leftOver + percisionErrorCorrection;

                    int rounded = Math.Max(0, (int)Math.Floor(val));

                    leftOver = val - rounded;

                    if (rounded > totalPercentages)
                    {
                        rounded = totalPercentages;
                        if (totalPercentages == 0)
                            break;
                    }

                    totalPercentages -= rounded;

                    resultPercentages[i] += rounded;
                }
            }

            if (totalPercentages > 0)
                resultPercentages[list.Count - 1] += totalPercentages;

            return resultPercentages;
        }

    }
}

