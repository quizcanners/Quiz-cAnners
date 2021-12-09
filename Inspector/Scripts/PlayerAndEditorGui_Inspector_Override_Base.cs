using System;
using QuizCanners.Utils;
using UnityEngine;
#if UNITY_EDITOR
using  UnityEditor;
#endif

namespace QuizCanners.Inspect {

    public abstract class PEGI_Inspector_Material
        #if UNITY_EDITOR
        : ShaderGUI
        #endif
    {

        #if UNITY_EDITOR
        public static bool drawDefaultInspector;
        public MaterialEditor unityMaterialEditor;
        private MaterialProperty[] _properties;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            unityMaterialEditor = materialEditor;
            _properties = properties;

            if (!drawDefaultInspector) {
                PegiEditorOnly.Inspect_Material(this);
                return;
            }

            pegi.toggleDefaultInspector(materialEditor.target);

            DrawDefaultInspector();

        }

        #endif

        public void DrawDefaultInspector()
        #if UNITY_EDITOR
            => base.OnGUI(unityMaterialEditor, _properties);
        #else
            {}
        #endif
        
        public abstract bool Inspect(Material mat);

    }


    public class PEGI_Inspector_OverrideAttribute :
#if UNITY_EDITOR
    CustomEditor
    {
        public PEGI_Inspector_OverrideAttribute(Type inspectedType) : base(inspectedType) { }
    }
#else
    Attribute 
    {
        public PEGI_Inspector_OverrideAttribute(Type inspectedType)  { }
    }
#endif

#if !UNITY_EDITOR
     public abstract class PEGI_Inspector_Override { }
#else
    public abstract class PEGI_Inspector_Override : Editor
    {

        public override void OnInspectorGUI()
        {
            PegiEditorOnly.ResetInspectionTarget(target);

            if (target != PegiEditorOnly.drawDefaultInspector)
            {
                if (target is MonoBehaviour)
                {
                    PegiEditorOnly.Inspect_MB(this);
                    pegi.RestoreBGColor();
                    return;
                }
                else if (target is ScriptableObject)
                {
                    PegiEditorOnly.Inspect_SO(this);
                    pegi.RestoreBGColor();
                    return;
                }
            }

            pegi.toggleDefaultInspector(target);

            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            if (EditorGUI.EndChangeCheck())
            {
                target.SetToDirty();
            }
        }
    }
    #endif
}

