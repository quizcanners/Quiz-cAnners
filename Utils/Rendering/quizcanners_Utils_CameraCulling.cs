using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Utils
{
    public static partial class QcUnity
    {


        public static bool IsMouseOutsideViewArea(this Camera cam, Vector3 mousePos)
        {
            // Skip viewport transform, check raw screen bounds
            return mousePos.x < 0 || mousePos.x > Screen.width
                || mousePos.y < 0 || mousePos.y > Screen.height;
        }

        public static bool IsInCameraViewArea(this Camera cam, Vector3 worldPosition, float objectSize = 1, float maxDistance = -1)
        {
            if (_cameraCullingCache.TryConsume())
            {
                s_camerasToRemove.Clear();

                foreach (var kvp in s_cameraStates)
                {
                    var camera = kvp.Key;

                    if (!camera)
                    {
                        s_camerasToRemove.Add(camera);
                        continue;
                    }

                    if (kvp.Value.HasChanged(camera))
                    {
                        kvp.Value.UpdateState(camera);
                        kvp.Value.ClearCache();
                    }
                }

                for (int i = 0; i < s_camerasToRemove.Count; i++)
                {
                    s_cameraStates.Remove(s_camerasToRemove[i]);
                }
            }

            if (!cam)
                return true;

            if (!s_cameraStates.TryGetValue(cam, out var state))
            {
                state = new CameraCullingState(cam);
                s_cameraStates[cam] = state;
            }

            return state.CheckVisibility(cam, worldPosition, objectSize, maxDistance);
        }

        private class CameraCullingState
        {
            private struct GridKey : IEquatable<GridKey>
            {
                public int X, Y, Z;

                public GridKey(Vector3 pos, float precision)
                {
                    X = Mathf.RoundToInt(pos.x / precision);
                    Y = Mathf.RoundToInt(pos.y / precision);
                    Z = Mathf.RoundToInt(pos.z / precision);
                }

                public bool Equals(GridKey other) => X == other.X && Y == other.Y && Z == other.Z;
                public override bool Equals(object obj) => obj is GridKey other && Equals(other);
                public override int GetHashCode() => (X * 73856093) ^ (Y * 19349663) ^ (Z * 83492791);
            }

            private readonly Dictionary<GridKey, bool> _cache = new();
            public Vector3 Position;
            public Quaternion Rotation;
            public float FieldOfView;

            public CameraCullingState(Camera cam)
            {
                var tf = cam.transform;
                Position = tf.position;
                Rotation = tf.rotation;
                FieldOfView = cam.fieldOfView;
            }

            public bool HasChanged(Camera cam, float posThresholdSqr = 0.01f, float rotThreshold = 0.5f)
            {
                var tf = cam.transform;

                if ((Position - tf.position).sqrMagnitude > posThresholdSqr)
                    return true;

                if (Quaternion.Angle(Rotation, tf.rotation) > rotThreshold)
                    return true;

                if (Mathf.Abs(FieldOfView - cam.fieldOfView) > 0.1f)
                    return true;

                return false;
            }

            public void UpdateState(Camera cam)
            {
                var tf = cam.transform;
                Position = tf.position;
                Rotation = tf.rotation;
                FieldOfView = cam.fieldOfView;
            }

            public void ClearCache() => _cache.Clear();

            private static float GetRoundingPrecision(float sqrDistance)
            {
                if (sqrDistance < 10000f)           // < 100m
                    return 1f;
                if (sqrDistance < 1000000f)         // < 1000m
                    return 10f;
                if (sqrDistance < 10000000f)        // < 3162m
                    return 50f;
                return 100f;
            }

            public bool CheckVisibility(Camera cam, Vector3 worldPosition, float objectSize, float maxDistance)
            {
                var camTf = cam.transform;
                var camPos = camTf.position;
                var camForward = camTf.forward;

                Vector3 toCamObject = worldPosition - camPos;
                Vector3 nearPlaneCenter = camPos + camForward * cam.nearClipPlane;
                Vector3 toNearPlane = worldPosition - nearPlaneCenter;

                float sqrDistance = toNearPlane.sqrMagnitude;
                float roundingPrecision = GetRoundingPrecision(sqrDistance);
                var cacheKey = new GridKey(worldPosition, roundingPrecision);

                if (_cache.TryGetValue(cacheKey, out var cached))
                    return cached;

                // Only compute sqrt on cache miss
                float distanceToCamera = Mathf.Sqrt(sqrDistance);
                float safetyMargin = roundingPrecision * 1.5f;
                float expandedSize = objectSize + safetyMargin;

                // Early exit for very large objects relative to distance
                if (objectSize > distanceToCamera * 0.5f)
                    return CacheAndReturn(cacheKey, true);

                float minDistSqr = (expandedSize + 2) * (expandedSize + 2);
                if (sqrDistance < minDistSqr)
                    return CacheAndReturn(cacheKey, true);

                if (maxDistance > 0)
                {
                    float maxDistSqr = (maxDistance - safetyMargin) * (maxDistance - safetyMargin);
                    if (sqrDistance > maxDistSqr)
                        return CacheAndReturn(cacheKey, false);
                }

                // Quick check if behind camera using dot product
                if (Vector3.Dot(camForward, toCamObject) < 0)
                    return CacheAndReturn(cacheKey, false);

                var pos = cam.WorldToViewportPoint(worldPosition);

                if (pos.z < 0)
                    return CacheAndReturn(cacheKey, false);

                float angularSize = 2.0f * Mathf.Atan(expandedSize / (2.0f * distanceToCamera));
                float fovRad = cam.fieldOfView * Mathf.Deg2Rad;
                float screenSpaceSize = angularSize / fovRad;

                if (screenSpaceSize > 0.5f)
                    return CacheAndReturn(cacheKey, true);

                bool isVisible = pos.x >= -screenSpaceSize && pos.x <= (1f + screenSpaceSize)
                                && pos.y >= -screenSpaceSize && pos.y <= (1f + screenSpaceSize);

                return CacheAndReturn(cacheKey, isVisible);
            }

            private bool CacheAndReturn(GridKey key, bool value)
            {
                _cache[key] = value;
                return value;
            }
        }

        private static readonly Gate.Frame _cameraCullingCache = new();

        private static readonly Dictionary<Camera, CameraCullingState> s_cameraStates = new();

        private static readonly List<Camera> s_camerasToRemove = new();

    }
}
