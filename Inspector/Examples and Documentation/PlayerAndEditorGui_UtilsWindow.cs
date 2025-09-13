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
        private const string NAME = "Qc Utils";

        Vector2 scrollPos;

        [MenuItem("Tools/"+ NAME)]
        static void Init() => GetWindow<PEGI_Utils_Window>(title: NAME).ShowTab();
        
        public override string ToString() => NAME;

        void OnGUI()
        {
            pegi.InspectEditorWindowOnGUI(QcUtils.InspectAllUtils, ref scrollPos);
        }
    }

#endif
}