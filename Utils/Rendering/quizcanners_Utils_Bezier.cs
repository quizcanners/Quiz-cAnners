using QuizCanners.Inspect;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Utils
{
    [Serializable]
    public class BezierCurve : IPEGI, IPEGI_Handles
    {
        public Vector3 StartVector;
        public Vector3 EndVector;
        [NonSerialized] private Vector3 _cachedStart;
        [NonSerialized] private Vector3 _cachedEnd;

        [NonSerialized] private List<Vector3> points = new List<Vector3>(); // BezierCurvePoints
        [NonSerialized] private List<Vector3> normalVectors;
        [NonSerialized] private float length;

       [NonSerialized] private Gate.Integer _vectorsCheck = new Gate.Integer();

        public void SwapVectors() 
        {
            var tmp = StartVector;
            StartVector = EndVector;
            EndVector = tmp;
            _cachedStart = Vector3.zero;
            _cachedEnd = Vector3.zero;
        }

        private void CheckPoints(Vector3 start, Vector3 end) 
        {
            if (!_vectorsCheck.TryChange((StartVector * 0.123f + EndVector).GetHashCode()))
            {
                if (_cachedStart.Equals(start) && _cachedEnd.Equals(end))
                    return;
            }
                

            _cachedStart = start;
            _cachedEnd = end;

           // Debug.Log("Recalculating curve");

            points = QcMath.BezierCurvePoints(start, start + StartVector, end + EndVector, end, out length);
            normalVectors = null;

        }

        public Vector3 GetPoint(float portion, Vector3 start, Vector3 end, bool inverted = false)
        {
            CheckPoints(start, end);

           // float pointSection = Mathf.Clamp01(portion) * (points.Count-1);

           // var from = points[Mathf.FloorToInt(pointSection)];
           // var to = points[Mathf.CeilToInt(pointSection - float.Epsilon * 10)];

            return QcMath.Interpolate(points, progress01: inverted ? 1-portion : portion); //Vector3.Lerp(from, to, pointSection % 1);
        }

        /*
        private Vector3 GetFromProgress(float progress, List<Vector3> p) 
        {
            float pointSection = Mathf.Clamp01(progress) * (p.Count - 1);

            var from = p[Mathf.FloorToInt(pointSection)];
            var to = p[Mathf.CeilToInt(pointSection - float.Epsilon * 10)];

            return Vector3.Lerp(from, to, pointSection % 1);
        }*/

        public Vector3 GetNormal(float portion, Vector3 start, Vector3 end, bool inverted) 
        {
            if (inverted)
                portion = 1 - portion;

           CheckPoints(start, end);

            if (normalVectors.IsNullOrEmpty()) 
            {
                normalVectors = new List<Vector3>();

              //  Debug.Log("Recalculating Normals");

                for (int i=0; i< points.Count-1; i++) 
                {
                    normalVectors.Add((points[i+1] - points[i]).normalized);
                }

                normalVectors.Add(normalVectors[normalVectors.Count - 1]);
            }

          
            var vec = QcMath.Interpolate(normalVectors, progress01: portion).normalized;

            if (inverted)
                return -vec;
            
            return vec;
        }

        public float CalculateLength(Vector3 start, Vector3 end) 
        {
            CheckPoints(start, end);
            return length;
        } // QcMath.BezierCurveLength(start, start + StartVector, end + EndVector, end, pointCount: GetPointCount(start, end));

        #region Inspector

        void IPEGI.Inspect()
        {
        }

        public void OnSceneDraw()
        {
            int ind = 0;
            foreach (var p in points)
            {
                pegi.Handle.Line(p, p + Vector3.up, Color.yellow);
                pegi.Handle.Label(ind.ToString(), p + Vector3.up);
                ind++;
            }
        }

        #endregion
    }
}