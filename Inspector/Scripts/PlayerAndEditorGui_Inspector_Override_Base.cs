using System;
using QuizCanners.Utils;
using UnityEngine;
using Codice.Client.BaseCommands;
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

            pegi.Toggle_DefaultInspector(materialEditor.target);

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
            using (PegiEditorOnly.StartInspector(target))
            {
                if (target != PegiEditorOnly.DrawDefaultInspector)
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

                pegi.Toggle_DefaultInspector(target);

                EditorGUI.BeginChangeCheck();
                DrawDefaultInspector();
                if (EditorGUI.EndChangeCheck())
                {
                    target.SetToDirty();
                }
            }
        }

        public void OnSceneGUI()
        {
            if (target is IPEGI_Handles trg)
            {
                pegi.IsDrawingHandles = true;
                try
                {
                    trg.OnSceneDraw();
                    if (GUI.changed)
                        target.SetToDirty_Obj();
                }
                catch (Exception ex)
                {
                    QcLog.ChillLogger.LogExceptionExpOnly(ex, "OnGuiInsp", target);
                }
                pegi.IsDrawingHandles = false;
            }
        }

        public override bool HasPreviewGUI()
        {
            return target is IPEGI_Preview;
        }

        public override void DrawPreview(Rect previewArea)
        {
            var tex = (target as IPEGI_Preview).GetPreview();

            if (!tex)
                return;

            Vector2 res = new(tex.width, tex.height);
            float texRelation = res.x / res.y;
            float windowRelation = previewArea.width / previewArea.height;
            float relation = texRelation / windowRelation;
            Vector2 proportions = relation > 1 ? new Vector2(1, 1 / relation) : new Vector2(relation, 1);

            GUI.DrawTexture(new Rect(previewArea.x, previewArea.y, previewArea.width * proportions.x, previewArea.height * proportions.y), tex);
        }

        public override Texture2D RenderStaticPreview(string assetPath, UnityEngine.Object[] subAssets, int width, int height)
        {
            var asPrev = target as IPEGI_Preview;

            if (asPrev == null)
                return null;

            var rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            var sourceTex = asPrev.GetPreview();

            if (sourceTex == null)
                return null;

            Graphics.Blit(sourceTex, rt);
            RenderTexture.active = rt;
            Texture2D tex = new(width, height, TextureFormat.ARGB32, true, true);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            rt.Release();
            DestroyImmediate(rt, allowDestroyingAssets: false);

            return tex;
        }
    }
#endif
}

