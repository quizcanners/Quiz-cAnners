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


        [NonSerialized] private List<Vector3> points = new List<Vector3>(); // BezierCurvePoints
        [NonSerialized] private float length;

        [NonSerialized] private Gate.Integer _vectorsCheck = new Gate.Integer();

        public void SwapVectors() 
        {
            var tmp = StartVector;
            StartVector = EndVector;
            EndVector = tmp;
        }

        private void CheckPoints(Vector3 start, Vector3 end) 
        {
            if (_vectorsCheck.TryChange((start + StartVector + end + EndVector).GetHashCode()))
            {
                points = QcMath.BezierCurvePoints(start, start + StartVector, end + EndVector, end, out length);
            }
        }

        public Vector3 GetPoint(float portion, Vector3 start, Vector3 end)
        {
            CheckPoints(start, end);

            float pointSection = Mathf.Clamp01(portion) * (points.Count-1);

            var from = points[Mathf.FloorToInt(pointSection)];
            var to = points[Mathf.CeilToInt(pointSection - float.Epsilon * 10)];

            return Vector3.Lerp(from, to, pointSection % 1);
        }

        public float CalculateLength(Vector3 start, Vector3 end) 
        {
            CheckPoints(start, end);
            return length;
        } // QcMath.BezierCurveLength(start, start + StartVector, end + EndVector, end, pointCount: GetPointCount(start, end));

        #region Inspector

        public void Inspect()
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