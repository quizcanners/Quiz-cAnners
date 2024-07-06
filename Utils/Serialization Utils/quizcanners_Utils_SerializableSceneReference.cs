using QuizCanners.Inspect;
using System;
using System.IO;
using UnityEngine;

namespace QuizCanners.Utils
{
    public static partial class QcScenes
    {
        [Serializable]
        public class SerializableSceneReference : IPEGI, INeedAttention
        {
            [SerializeField] public string ScenePath;

#if UNITY_EDITOR
            [SerializeField] private UnityEditor.SceneAsset _asset;
#endif

            public bool IsValid =>
                ScenePath.IsNullOrEmpty() == false
#if UNITY_EDITOR
                && _asset
#endif
                ;
            public bool ScenePathDirty
            {
                get
                {
                    var path = GetAssetPath();
                    return !path.IsNullOrEmpty() && !path.Equals(ScenePath);
                }
            }

            private string GetAssetPath() =>
#if UNITY_EDITOR
                _asset ? UnityEditor.AssetDatabase.GetAssetPath(_asset) : "";
#else
            "";
#endif

#if UNITY_EDITOR
            public void SetScene_Editor(UnityEditor.SceneAsset asset) 
            {
                _asset = asset;
                ScenePath = GetAssetPath();
            }
#endif

            #region Inspector
            void IPEGI.Inspect()
            {
                if (Application.isEditor)
                {
#if UNITY_EDITOR
                    pegi.Edit(ref _asset);
#endif
                    Inspect_SceneVaidity();
                }
                else
                {
                    pegi.Edit_Scene(ref ScenePath);
                }
            }

            public void Inspect_SceneVaidity() 
            {
                if (ScenePathDirty)
                {
                    var path = GetAssetPath();
                    Icon.Warning.Draw("Scene pas has changed");
                    if ("Update Path".PegiLabel(path).Click())
                        ScenePath = path;
                }
            }

            public string NeedAttention()
            {
                var path = GetAssetPath();

                if (!path.IsNullOrEmpty() && !path.Equals(ScenePath))
                    return "Path has changed";

                return null;
            }

            public override string ToString() => ScenePath;
            #endregion
        }
    }
}
