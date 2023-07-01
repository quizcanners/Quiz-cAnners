using QuizCanners.Utils;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace QuizCanners.Inspect.Examples
{
#if UNITY_EDITOR
    public class PEGI_Utils_Window : EditorWindow
    {
        [MenuItem("Tools/Qc Utils")]
        static void Init() => GetWindow<PEGI_Utils_Window>(title: "Qc Utils").ShowTab();
        

        public override string ToString() => "Qc Utils";

        void OnGUI()
        {
            pegi.Nested_Inspect(QcUtils.InspectAllUtils);
        }
    }

#endif
}