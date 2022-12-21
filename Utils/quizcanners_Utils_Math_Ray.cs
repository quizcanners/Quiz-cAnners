using System;
using UnityEngine;
using static UnityEngine.UI.Image;

namespace QuizCanners.Utils
{
    public static partial class QcMath 
    {
       /* public struct QcRay
        {
            public Vector3 origin;
            public Vector3 direction;

            const float MIN_DIST = 0.001f;
            const float MAX_DIST = float.MaxValue * 0.5f;

       
          

            public struct ApproximatedCast 
            {
                public Ray ray;
                public float upscaleByDistance;
                public Vector3 origin;


                public ApproximatedCast (Camera cam, Vector3 uv, float thickness = 0.06f) 
                {
                    upscaleByDistance = cam.fieldOfView / 90;
                    upscaleByDistance = MathF.Pow(upscaleByDistance, 3) * thickness;
                    origin = cam.transform.position;
                    ray = cam.GetMousePosRay();
                }
                
                public bool TryHit(Sphere sphere, out RaycastHit hit) 
                {
                    float distance = Vector3.Distance(origin, sphere.position);
                    float upscaledRadius = sphere.radius + distance * upscaleByDistance;

                    return ray.TryHit(new Sphere() { position = origin, radius = upscaledRadius }, out hit);
                }

                
                using (pegi.SceneDraw.SetColorDisposible(color: UnityEngine.Color.blue))
                {
                    _gameECS.GetWorld().WithAll<ClickTargetSphere, Position>()
                    .Run((ClickTargetSphere sp, Position pos) =>
                    {
                    float distance = Vector3.Distance(cameraOrigin, pos.vector);
                    float upscaledRadius = sp.Radius + distance * upscaleFromFov * UPSCALE_THICKNESS;

                    if (ray.TryHit(new Ray.Sphere() { position = pos.vector, radius = upscaledRadius }, out var hit))
                    {
                        pegi.Gizmo.DrawWireCube(pos.vector.ToUnity(), Quaternion.Identity.ToUnity(), (Vector3.One * 2 * upscaledRadius).ToUnity());
                    }
                });
                }

            }
        }*/

        public struct Sphere
        {
            public Vector3 position;
            public float radius;
        }

        public struct Box
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 boxSize;

            public Box(Vector3 position)
            {
                this.position = position;
                this.rotation = Quaternion.identity;
                this.boxSize = Vector3.one;
            }

            public Box(Vector3 position, Vector3 boxSize)
            {
                this.position = position;
                this.rotation = Quaternion.identity;
                this.boxSize = boxSize;
            }

            public Box(Vector3 position, Quaternion rotation, Vector3 boxSize) 
            {
                this.position = position;
                this.rotation = rotation;
                this.boxSize = boxSize;
            }
        }

        public struct RayHit
        {
            public double dist;
            public Vector3 hitPoint;
            public Vector3 normal;
        }

        public static Vector3 Rotate(Vector3 vec, Quaternion q)
        {
            var xyz = -q.XYZ();
            Vector3 crossA = Vector3.Cross(xyz, vec) + q.w * vec;
            vec += 2f * Vector3.Cross(xyz, crossA);

            return vec;
        }

        public static bool TryHitYPlane(this Ray ray, Vector3 planePoint, Vector3 normal, out Vector3 hipPoint)
        {
            var diff = ray.origin - planePoint;
            var prod1 = Vector3.Dot(diff, normal);
            var prod2 = Vector3.Dot(ray.direction, normal);
            var prod3 = prod1 / prod2;
            hipPoint = ray.origin - ray.direction * prod3;

            return prod3 < 0;
        }


        public static bool TryHit(this Ray ray, Sphere sphere, out RaycastHit hit)
        {
            Vector3 ro = ray.origin - sphere.position;

            float b = Vector3.Dot(ro, ray.direction);
            float c = Vector3.Dot(ro, ro) - sphere.radius * sphere.radius;
            float h = b * b - c;

            hit = new RaycastHit();

            if (h <= 0)
            {
                return false;
            }
            else
            {
                h = UnityEngine.Mathf.Sqrt(h);
                float d1 = -b - h;
                float d2 = -b + h;
                if (d1 >= 0.0001 && d1 <= 10000)
                {
                    hit.normal = (ro + ray.direction * d1).normalized;
                    hit.distance = d1;
                    hit.point = ray.origin + ray.direction * d1;
                    return true;
                }
                else if (d2 >= 0.0001 && d2 <= 10000)
                {
                    hit.normal = (ro + ray.direction * d2).normalized;
                    hit.distance = d2;
                    hit.point = ray.origin + ray.direction * d1;
                    return true;
                }
                return false;
            }
        }

        public static bool TryHit(this Ray ray, Box box, out RayHit hit)
        {
            Vector3 ro = ray.origin - box.position;

            ro = Rotate(ro, box.rotation);
            Vector3 rd = Rotate(ray.direction, box.rotation);

            Vector3 abs = rd.Abs() + Vector3.one * 0.000001f;

            abs.Scale(abs);

            Vector3 m = rd.DivideBy(abs);

            Vector3 n = m.MultiplyBy(ro);

            Vector3 k = m.Abs().MultiplyBy(box.boxSize);

            Vector3 t1 = -n - k;
            Vector3 t2 = -n + k;

            float tN = MathF.Max(MathF.Max(t1.x, t1.y), t1.z);
            float tF = MathF.Min(MathF.Min(t2.x, t2.y), t2.z);

            hit = new RayHit();

            if (tN > tF || tF <= 0.0)
            {
                return false;
            }
            else
            {
                if (tN >= 0.00001f && tN <= 10000)
                {
                    // hit.normal = -signRd * step(t1.YZX(), t1) * step(t1.zxy, t1.xyz);
                    // hit.normal = Rotate(hit.normal, new Quaternion(q.X, q.Y, q.Z, q.W));
                    hit.hitPoint = ray.origin + ray.direction * tN;
                    return true;//tN;
                }
                else if (tF >= 0.00001f && tF <= 10000)
                {
                    //normal = -signRd * step(t2.xyz, t2.yzx) * step(t2.xyz, t2.zxy);
                    // normal = Rotate(normal, float4(q.x, q.y, q.z, q.w));
                    hit.hitPoint = ray.origin + ray.direction * tF;
                    return true; //tF;
                }

                return false;

            }
        }

        public static Ray GetMousePosRay(this Camera cam) 
        {
            Ray uRay = cam.ScreenPointToRay(Input.mousePosition);
            uRay.origin += uRay.direction * cam.nearClipPlane;
            return uRay;
        }

    }
}