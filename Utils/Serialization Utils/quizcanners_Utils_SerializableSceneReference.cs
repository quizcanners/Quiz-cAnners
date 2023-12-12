using QuizCanners.Inspect;
using System;
using System.IO;
using UnityEngine;

namespace QuizCanners.Utils
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

        #region Inspector
        void IPEGI.Inspect()
        {
            if (Application.isEditor)
            {
#if UNITY_EDITOR
                pegi.Edit(ref _asset);
#endif

               
                if (ScenePathDirty)
                {
                    var path = GetAssetPath();
                    Icon.Warning.Draw("Scene pas has changed");
                    if ("Update Path".PegiLabel(path).Click())
                        ScenePath = path;
                }
            }
            else
            {
                pegi.Edit_Scene(ref ScenePath);
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
