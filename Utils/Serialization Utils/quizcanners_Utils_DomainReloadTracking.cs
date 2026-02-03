#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
#endif

namespace QuizCanners.Utils
{

    /*
#if UNITY_EDITOR

    [InitializeOnLoad]
    internal static class ScriptVersion
    {
        const string Key = "YourPkg.ScriptVersion";

        static ScriptVersion()
        {
            // Any compilation that produces an assembly -> bump version
            CompilationPipeline.assemblyCompilationFinished += (_, __) => Bump();
        }

        public static int Current => EditorPrefs.GetInt(Key, 0);

        static void Bump()
        {
            //Debug.Log("Version changed Bump {0}".F(Current));

            EditorPrefs.SetInt(Key, Current + 1);
        }
    }
#endif

    
    public static partial class QcUnity
    {

        public class DomainReloadGate
        {
#if UNITY_EDITOR
            int _seenScriptVersion = -1;
#endif

            public bool TryChange()
            {
#if UNITY_EDITOR
                var v = ScriptVersion.Current;
                if (v != _seenScriptVersion)
                {
                    Debug.Log("Version changed from {0} to {1}".F(v, _seenScriptVersion));

                    _seenScriptVersion = v;
                    return true;
                }    
#endif
                return false;

            }
        }
    }*/
}