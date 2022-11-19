using QuizCanners.Inspect;
using System;
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

        private string GetAssetPath() =>
        #if UNITY_EDITOR
            _asset ? UnityEditor.AssetDatabase.GetAssetPath(_asset) : "";
        #else
            "";
        #endif

        #region Inspector
        public void Inspect()
        {
            if (Application.isEditor)
            {
#if UNITY_EDITOR
                pegi.Edit(ref _asset);
#endif

                var path = GetAssetPath();
                if (!path.IsNullOrEmpty() && !path.Equals(ScenePath))
                {
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
