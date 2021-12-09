#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using QuizCanners.Utils;
using System;
#endif


namespace QuizCanners.Inspect
{
    internal static partial class PegiEditorOnly 
    {
#if UNITY_EDITOR

        public static class SceneDraw 
        {
            public static void DrawWireCube(Vector3 position, Vector3 size) 
            {
                Handles.DrawWireCube(position, size);
            }

            public static void Label (string text, Vector3 pos) 
            {
                Handles.Label(pos, text);
            }

            public static Color Color
            {
                get => Handles.color;
                set
                {
                    Handles.color = value;
                    Gizmos.color = value;
                }
            }

            public static IDisposable SetColorDisposible(Color color) 
                => QcSharp.SetTemporaryValueDisposable(color, col => Color = col, () => Color);
        }
#endif
    }
}