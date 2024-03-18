using QuizCanners.Utils;
using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.IMGUI.Controls;
#endif
using UnityEngine;
using static QuizCanners.Inspect.pegi.SceneDraw;

namespace QuizCanners.Inspect
{
    public interface IPEGI_Handles { void OnSceneDraw(); }

    public static partial class pegi
    {
        internal static bool IsDrawingHandles;

        public static ChangesToken OnSceneDraw_Nested(this IPEGI_Handles handles) 
        {
            var changes = ChangeTrackStart();

            handles.OnSceneDraw();

            if (changes)
                handles.SetToDirty_Obj();

            return changes;
        }

        public static class Handle
        {
            public class PlaneHandleState : IPEGI, IPEGI_Handles
            {
                public PlaneProjection _projection;
                public Vector3 worldPosition;

                void IPEGI.Inspect()
                {

                }

                public void OnSceneDraw()
                {

                }

                public enum PlaneProjection { x,y,z}
            }

            //UnityEditor.Handles.Disc
            //UnityEditor.Handles.DrawLine
            //UnityEditor.Handles.PositionHandle
            //UnityEditor.Handles.RotationHandle

            public static void SceneSetDirty(UnityEngine.Object obj) 
            {
                #if UNITY_EDITOR
                     EditorUtility.SetDirty(obj);
                #endif
            }


            public static bool TryGetLeftMouseClickPosition(out Vector3 position) 
            {
                if (IsLeftMouseButtonDown() &&
                    TryGetRayFromMouse(out var ray) &&
                    Physics.Raycast(ray, out RaycastHit hit))
                {
                    position = hit.point;
                    return true;
                }

                position = Vector3.zero;

                return false;
            }


            private static readonly Gate.SystemTime _clickGate = new(Gate.InitialValue.StartArmed); // Event comes trough twice

            private static ChangesToken EndClickCheck()
            {
#if UNITY_EDITOR
                if (EditorOnly_EndChangeCheck())
                {
                    _clickGate.Update();
                    return ChangesToken.True;
                }
#endif

                return ChangesToken.False;
            }

            public static bool IsAlt() 
            {
#if UNITY_EDITOR
                if (IsDrawingHandles)
                {
                    return Event.current.alt;
                }
#endif

                return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            }

            public static bool IsLeftMouseButtonDown()
            {
                if (globChanged)
                    return false;

#if UNITY_EDITOR
                if (IsDrawingHandles)
                {
                    return Event.current.isMouse 
                        && Event.current.type == EventType.MouseDown
                        //&& Event.current.clickCount == 1
                        && Event.current.button == 0
                        && _clickGate.TryUpdateIfTimePassed(secondsPassed: 0.01f);
                }
#endif

                return Input.GetMouseButtonDown(0);
            }

            public static bool TryGetRayFromMouse(out Ray ray) 
            {
#if UNITY_EDITOR
                if (IsDrawingHandles)
                {
                    Vector3 mousePosition = Event.current.mousePosition;
                    ray = UnityEditor.HandleUtility.GUIPointToWorldRay(mousePosition);
                    return true;
                }
#endif

                if (Camera.main)
                {
                    ray = Camera.main.GetMousePosRay();
                    return true;
                }

                ray = new Ray();
                return false;
            }

            public static void Arrow(Vector3 from, Vector3 to, Color color, int handleId) 
            {
#if UNITY_EDITOR
                if (IsDrawingHandles)
                {
                    using (SetColorDisposible(color))
                    {
                        Handles.ArrowHandleCap(handleId, from, Quaternion.LookRotation(to-from), 3, EventType.Repaint);
                    }
                }
#endif
            }

            public static void Line(Vector3 from, Vector3 to, Color color, float thickness = -1)
            {
#if UNITY_EDITOR
                if (IsDrawingHandles)
                {
                    using (SetColorDisposible(color))
                    {
                        if (thickness <= 0)
                        {
                            Handles.DrawLine(from, to);
                        }
                        else
                        {
                            Handles.DrawLine(from, to, thickness: thickness);
                        }
                    }
                }
#endif
            }

            public static ChangesToken Button(Vector3 pos, Vector3 direction = default, Vector3 offset = default, HandleCap shape = HandleCap.Rectangle, Scaling scaling = Scaling.RotateAndScale, string label = null, float size = 1)
            {
                #if UNITY_EDITOR
                if (IsDrawingHandles)
                {
                    RotateOffset(ref offset);
                    var scale = HandleSize(pos);

                    pos += offset * scale;

                    switch (scaling)
                    {
                        case Scaling.RotateAndScale: size *= scale; break;
                    }

                    if (!label.IsNullOrEmpty()) 
                    Label(label, pos, Styles.ClickableText);

                    var rotation = direction == default ? CameraRotation : Quaternion.LookRotation(direction);

                    if (Handles.Button(pos, rotation, size, pickSize: size, ToFunction(shape)))
                        return ChangesToken.True;
                }
                #endif

                return ChangesToken.False;
            }

            public static ChangesToken Bazier(BezierCurve be, Vector3 startPoint, Vector3 endPoint, Color color, Texture2D texture = null, float width = 1)
            {
                #if UNITY_EDITOR
                if (IsDrawingHandles)
                {
                    EditorGUI.BeginChangeCheck();

                    be.StartVector = (Handles.PositionHandle(startPoint + be.StartVector, Quaternion.identity) - startPoint);
                    be.EndVector = (Handles.PositionHandle(endPoint + be.EndVector, Quaternion.identity) - endPoint);

                    Handles.DrawBezier(
                        startPosition: startPoint, 
                        endPosition: endPoint, 
                        startTangent: startPoint + be.StartVector, 
                        endTangent: endPoint + be.EndVector, 
                        color: color, 
                        texture: texture, 
                        width: width);


                    return EndClickCheck();
                }
                #endif
 
                return ChangesToken.False;
            }

            public static ChangesToken Position(Vector3 position, out Vector3 newPosition) 
            {
#if UNITY_EDITOR
                if (IsDrawingHandles)
                {
                    EditorGUI.BeginChangeCheck();
                    newPosition = Handles.PositionHandle(position, Quaternion.identity);

                    return EndClickCheck();
                }
                else
#endif
                    newPosition = position;

                return ChangesToken.False;
            }




            public static ChangesToken BoxBoundsHandle(Transform transfrom, Color color)
            {
                var pos = transfrom.position;
                var size = transfrom.localScale;

                if (BoxBoundsHandle(ref pos, ref size, color)) 
                {
                    transfrom.position = pos;
                    transfrom.localScale = size;
                    return ChangesToken.True;
                }

                return ChangesToken.False;
            }

            public static ChangesToken BoxBoundsHandle(ref Vector3 center, ref Vector3 size, Color color)
            {
                var bnds = new Bounds(center, size);

                if (BoxBoundsHandle(ref bnds, color)) 
                {
                    center = bnds.center;
                    size = bnds.size;

                    return ChangesToken.True;
                }

                return ChangesToken.False;
            }

            public static ChangesToken BoxBoundsHandle(Transform transfrom, ref Vector3 center, ref Vector3 size, Color color)
            {
                var bnds = new Bounds(transfrom.position + center, size);

                if (BoxBoundsHandle(ref bnds, color))
                {
                    center = bnds.center - transfrom.position;
                    size = bnds.size;

                    return ChangesToken.True;
                }

                return ChangesToken.False;
            }

#if UNITY_EDITOR
            private static readonly BoxBoundsHandle m_BoundsHandle = new();

#endif


            public static ChangesToken BoxBoundsHandle(ref Bounds bounds, Color color)
            {

#if UNITY_EDITOR
                if (IsDrawingHandles)
                {
                    m_BoundsHandle.center = bounds.center;
                    m_BoundsHandle.size = bounds.size;
                    m_BoundsHandle.wireframeColor = color;

                    EditorGUI.BeginChangeCheck();
                    m_BoundsHandle.DrawHandle();
                    if (EndClickCheck())
                    {
                        bounds.center = m_BoundsHandle.center;
                        bounds.size = m_BoundsHandle.size;

                        return ChangesToken.True;
                    }
                }
#endif

                return ChangesToken.False;
            }

            public static ChangesToken FreeMove(Vector3 position, out Vector3 newPosition, HandleCap shape = HandleCap.Sphere, Scaling scaling = Scaling.RotateOnly, float size = 1, Vector3 snapSize = default)
            {
#if UNITY_EDITOR
                if (IsDrawingHandles)
                {
                    switch (scaling)
                    {
                        case Scaling.RotateAndScale: size *= UnityEditor.HandleUtility.GetHandleSize(position) * 0.5f; break;
                    }

                    EditorGUI.BeginChangeCheck();
                    newPosition = Handles.FreeMoveHandle(position, size, snapSize, ToFunction(shape));
                    
                    return EndClickCheck();

                } else
#endif
                newPosition = position;


                return ChangesToken.False;
            }

            public static void DrawWireCube(Vector3 position, Quaternion rotation, Vector3 size)
            {
#if UNITY_EDITOR
                if (IsDrawingHandles)
                {
                    DrawRotatedWireCube(position, rotation, size, (va, vb) => UnityEditor.Handles.DrawLine(va, vb));
                    return;
                }
#endif
            }

            public static void DrawWireCube(Vector3 position, Vector3 size)
            {
#if UNITY_EDITOR
                if (IsDrawingHandles)
                {
                    UnityEditor.Handles.DrawWireCube(position, size);
                    return;
                }
#endif
            }

            public static void DrawWireDisc(Vector3 position, float radius, Vector3 normal)
            {
#if UNITY_EDITOR
                if (IsDrawingHandles)
                {
                    Handles.DrawWireDisc(position, normal: normal, radius: radius);
                    return;
                }
#endif
            }

            public static void Label(string text, Vector3 pos, Vector3 offset, Color col, Scaling scaling = Scaling.RotateOnly, Styles.PegiGuiStyle style = null)
            {
#if UNITY_EDITOR
                if (IsDrawingHandles)
                {
                    using (SetColorDisposible(col))
                    {
                        Label(text, pos, offset, scaling, style);
                    }
                }
#endif
            }

            public static void Label(string text, Vector3 pos, Vector3 offset, Scaling scaling = Scaling.RotateOnly, Styles.PegiGuiStyle style = null)
            {
#if UNITY_EDITOR
                if (IsDrawingHandles)
                {
                    RotateOffset(ref offset);

                    switch (scaling)
                    {
                        case Scaling.RotateAndScale:
                            offset *= HandleSize(pos);
                            break;
                        case Scaling.RotateOnly:

                            break;
                    }


                    Label(text, pos + offset, style);
                }
#endif
            }

            public static void Label(string text, Vector3 pos, Color col, Styles.PegiGuiStyle style = null)
            {
#if UNITY_EDITOR
                if (IsDrawingHandles)
                {
                    using (SetColorDisposible(col))
                    {
                        if (!PaintingGameViewUI)
                        {
                            Label(text, pos, style);
                            return;
                        }
                    }
                }
#endif
            }

            public static void Label(string text, Vector3 pos, Styles.PegiGuiStyle style = null)
            {
#if UNITY_EDITOR
                if (IsDrawingHandles)
                {
                    if (style == null)
                        UnityEditor.Handles.Label(pos, text);
                    else
                        UnityEditor.Handles.Label(pos, text, style.Current);
                }
#endif
            }

            private static float HandleSize(Vector3 position)
            {
#if UNITY_EDITOR
                if (IsDrawingHandles)
                {
                    return UnityEditor.HandleUtility.GetHandleSize(position) * 0.5f;
                } else
#endif
                    return 0.5f;
            }

            private static Quaternion CameraRotation
            {
                get
                {
#if UNITY_EDITOR
                    var sv = UnityEditor.SceneView.currentDrawingSceneView;
                    if (sv != null)
                    {
                        return sv.camera.transform.rotation;
                    }
#endif
                    return Quaternion.identity;
                }
            }

            private static void RotateOffset(ref Vector3 offset)
            {
#if UNITY_EDITOR
                var sv = UnityEditor.SceneView.currentDrawingSceneView;
                if (sv != null)
                {
                    offset = sv.camera.transform.rotation * offset;
                }
#endif
            }

        }

        public static class Gizmo
        {
            public static void DrawSphere(Vector3 position, float radius) 
            {
                if (!IsDrawingHandles)
                {
                    Gizmos.DrawSphere(position, radius);
                }
            }

            public static void DrawCube(Vector3 position, Vector3 size, Color col)
            {
                if (!IsDrawingHandles)
                {
                    using (SetColorDisposible(col))
                    {
                        Gizmos.DrawCube(position, size);
                    }
                }
            }

            public static void DrawWireCube(Vector3 position, Vector3 size)
            {
                if (!IsDrawingHandles)
                {
                    DrawRotatedWireCube(position, Quaternion.identity, size, (va, vb) => Gizmos.DrawLine(va, vb));
                }
            }

            public static void DrawWireCube(Vector3 position, Quaternion rotation, Vector3 size)
            {
                if (!IsDrawingHandles)
                {
                    DrawRotatedWireCube(position, rotation, size, (va, vb) => Gizmos.DrawLine(va, vb)); 
                }
            }

            public static void Ray(Vector3 pos, Vector3 direction)
            {
                if (!IsDrawingHandles)
                {
                    Gizmos.DrawRay(new Ray(pos, direction));
                }
            }
        }

        public static class SceneDraw 
        {
            public static IDisposable SetColorDisposible(Color color) => QcSharp.SetTemporaryValueDisposable(color, col => Color = col, () => Color);

            public static Color Color
            {
                get => Gizmos.color;
                set
                {
#if UNITY_EDITOR
                    UnityEditor.Handles.color = value;
#endif
                    Gizmos.color = value;
                }
            }

            public enum Scaling { RotateOnly, RotateAndScale, }

            public enum HandleCap { Arrow, Circle, Cone, Cube, Cylinder, Dot, Rectangle, Sphere }

#if UNITY_EDITOR
            internal static UnityEditor.Handles.CapFunction ToFunction(HandleCap cap)
            {
                switch (cap)
                {
                    case HandleCap.Arrow: return UnityEditor.Handles.ArrowHandleCap;
                    case HandleCap.Circle: return UnityEditor.Handles.CircleHandleCap;
                    case HandleCap.Cone: return UnityEditor.Handles.ConeHandleCap;
                    case HandleCap.Cube: return UnityEditor.Handles.CubeHandleCap;
                    case HandleCap.Cylinder: return UnityEditor.Handles.CylinderHandleCap;
                    case HandleCap.Dot: return UnityEditor.Handles.DotHandleCap;
                    case HandleCap.Rectangle: return UnityEditor.Handles.RectangleHandleCap;
                    case HandleCap.Sphere: return UnityEditor.Handles.SphereHandleCap;
                    default:
                        Debug.LogError(QcLog.CaseNotImplemented(cap, nameof(ToFunction)));
                        return UnityEditor.Handles.CircleHandleCap;
                }
            }
#endif

            internal static void DrawRotatedWireCube(Vector3 position, Quaternion rotation, Vector3 size, Action<Vector3, Vector3> draw)
            {

                size *= 0.5f;

                LINE(new Vector3(1, 1, 1), new Vector3(1, 1, -1));
                LINE(new Vector3(1, 1, 1), new Vector3(1, -1, 1));
                LINE(new Vector3(1, 1, 1), new Vector3(-1, 1, 1));

                LINE(new Vector3(-1, 1, 1), new Vector3(-1, -1, 1));
                LINE(new Vector3(-1, 1, 1), new Vector3(-1, 1, -1));

                LINE(new Vector3(1, -1, 1), new Vector3(-1, -1, 1));
                LINE(new Vector3(1, -1, 1), new Vector3(1, -1, -1));

                LINE(new Vector3(1, 1, -1), new Vector3(-1, 1, -1));
                LINE(new Vector3(1, 1, -1), new Vector3(1, -1, -1));

                LINE(new Vector3(-1, -1, -1), new Vector3(-1, 1, -1));
                LINE(new Vector3(-1, -1, -1), new Vector3(-1, -1, 1));
                LINE(new Vector3(-1, -1, -1), new Vector3(1, -1, -1));


                void LINE(Vector3 A, Vector3 B)
                {
                    A.Scale(size);
                    B.Scale(size);

                    draw(position + rotation * A, position + rotation * B);

                }

            }


        }

    }
}
