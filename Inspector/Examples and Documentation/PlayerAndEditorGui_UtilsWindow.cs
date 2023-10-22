using QuizCanners.Utils;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

namespace QuizCanners.Inspect.Examples
{
#if UNITY_EDITOR
    public class PEGI_Utils_Window : EditorWindow
    {
        Vector2 scrollPos;


        [MenuItem("Tools/Qc Utils")]
        static void Init() => GetWindow<PEGI_Utils_Window>(title: "Qc Utils").ShowTab();
        

        public override string ToString() => "Qc Utils";

        void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Width(Screen.width-3), GUILayout.Height(Screen.height - 50));

            pegi.Nested_Inspect(QcUtils.InspectAllUtils);

            EditorGUILayout.EndScrollView();
        }
    }

#endif
}