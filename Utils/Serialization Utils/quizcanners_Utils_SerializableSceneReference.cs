using QuizCanners.Inspect;
using System;
using UnityEngine;

namespace QuizCanners.Utils
{
    public static partial class QcScenes
    {
        [Serializable]
        public class SerializableSceneReference : IPEGI, INeedAttention
        {
            [SerializeField] public string ScenePath;

            [SerializeField] private UnityEngine.Object _asset;//UnityEditor.SceneAsset _asset;

#if UNITY_EDITOR

            [NonSerialized] UnityEditor.SceneAsset _assetAsScene;
            [NonSerialized] private bool _triedToGet = false;
            private UnityEditor.SceneAsset Asset 
            {
                get 
                {
                    if (_triedToGet)
                        return _assetAsScene;

                    _triedToGet = true;

                    try
                    {
                        if (_asset)
                        {
                            _assetAsScene = _asset as UnityEditor.SceneAsset;
                        }

                        if (_assetAsScene)
                            return _assetAsScene;

                        _assetAsScene = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(ScenePath);
                        _asset = _assetAsScene;
                    } catch (Exception ex) 
                    {
                        Debug.LogException(ex);
                    }
                    return _assetAsScene;
                }
            }
#endif

            public bool IsValid =>
                ScenePath.IsNullOrEmpty() == false
#if UNITY_EDITOR
                && Asset
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
                Asset ? UnityEditor.AssetDatabase.GetAssetPath(Asset) : "";
#else
            "";
#endif

#if UNITY_EDITOR
            public void SetScene_Editor(UnityEditor.SceneAsset asset) 
            {
                _asset = asset;
                _triedToGet = true;
                ScenePath = GetAssetPath();
            }
#endif

            #region Inspector
            void IPEGI.Inspect()
            {
                if (Application.isEditor)
                {
#if UNITY_EDITOR
                    var ass = Asset;
                    pegi.Edit(ref ass).OnChanged(()=> _asset = ass);
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
