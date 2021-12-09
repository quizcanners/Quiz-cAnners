using QuizCanners.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Inspect
{

    public static partial class pegi
    {

#pragma warning disable CS0162 // Unreachable code detected

        public static class SceneDraw
        {

            public static void OnDrawGizmos(IPEGI_SceneDraw sceneDrawer) 
            {
                PaintingGameViewUI = true;

                try 
                {
                    sceneDrawer.DrawHandles();
                } catch (Exception ex) 
                {
                    Debug.LogException(ex);
                }

                PaintingGameViewUI = false;
            }

            public static void DrawWireCube(Vector3 position, Vector3 size)
            {
#if UNITY_EDITOR
                if (!PaintingGameViewUI)
                {
                    PegiEditorOnly.SceneDraw.DrawWireCube(position, size);
                    return;
                }
#endif
                Gizmos.DrawWireCube(position, size);
            }

            public static void Label(string text, Vector3 pos, Color col)
            {
                using (SetColorDisposible(col))
                {
#if UNITY_EDITOR
                    if (!PaintingGameViewUI)
                    {
                        PegiEditorOnly.SceneDraw.Label(text, pos);
                        return;
                    }
#endif
                }
            }

            public static void Label(string text, Vector3 pos)
            {
                #if UNITY_EDITOR
                    PegiEditorOnly.SceneDraw.Label(text, pos);
                #endif
            }

            public static IDisposable SetColorDisposible(Color color)
            {
#if UNITY_EDITOR
                return PegiEditorOnly.SceneDraw.SetColorDisposible(color);
#endif
                return QcSharp.SetTemporaryValueDisposable(color, col => Gizmos.color = col, () => Gizmos.color);
            }

        }
    }
}
