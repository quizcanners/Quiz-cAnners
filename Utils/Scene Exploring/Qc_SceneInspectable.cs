using QuizCanners.Inspect;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

namespace QuizCanners.Utils
{

    [Serializable]
    public class Qc_SceneInspectable : IPEGI_ListInspect, Singleton.ILoadingProgressForInspector, INeedAttention
    {

        public SerializableSceneReference SceneReference = new();
        public AsyncOperation LoadOperation;

        private readonly Gate.Frame _onLoadedInitializationOneFrameDelay = new();
        private int _framesSinceLoaded;

        public bool LoadingFailed { get; private set; } 

        public bool IsValid => SceneReference != null && SceneReference.IsValid;

        public string ScenePath => IsValid ? SceneReference.ScenePath : "";

        public bool IsLoadedOrLoading
        {
            get
            {
                var l = (LoadOperation != null && !LoadOperation.isDone) || IsLoaded;

                if (IsLoaded && _onLoadedInitializationOneFrameDelay.TryEnter())
                    _framesSinceLoaded++;

                return l;
            }
            set
            {
                if (value)
                    Load(LoadSceneMode.Additive);
                else
                    Unload();
            }
        }

        public bool IsLoadedAndInitialized
        {
            get
            {
                var scene = SceneManager.GetSceneByPath(SceneReference.ScenePath);
                if (!scene.IsValid())
                    return false;

                if (IsLoaded && _onLoadedInitializationOneFrameDelay.TryEnter())
                    _framesSinceLoaded++;

                return _framesSinceLoaded >= 5;
            }
        }

        public bool IsLoaded
        {
            get
            {
                if (SceneReference == null)
                    return false;

                var sc = SceneManager.GetSceneByPath(SceneReference.ScenePath);
                if (!sc.IsValid())
                    return false;

                return sc.isLoaded;
            }
        }

        public void Load(LoadSceneMode mode)
        {
            if (Application.isPlaying == false)
            {
#if UNITY_EDITOR
                if (IsValid && !IsLoaded)
                {
                    var scene = EditorSceneManager.OpenScene(SceneReference.ScenePath, OpenSceneMode.Additive);
                    SceneManager.SetActiveScene(scene); // In Editor Scenes are usually opened to editing
                }
#endif

            }
            else
            {
                if (LoadOperation == null || LoadOperation.isDone)
                {
                    if (!IsLoaded)
                    {
                        try
                        {
                            LoadOperation = SceneManager.LoadSceneAsync(ScenePath, mode);
                            _framesSinceLoaded = 0;
                        } catch (Exception ex) 
                        {
                            LoadingFailed = true;
                            Debug.LogException(ex);
                        }
                    }
                }
            }
        }

        private void Unload()
        {
            if (IsLoaded)
            {
                SceneManager.UnloadSceneAsync(SceneManager.GetSceneByPath(ScenePath));
                LoadOperation = null;
                _framesSinceLoaded = 0;
            }
        }

        #region Inspector

        public bool IsLoading(ref string state, ref float progress01)
        {
            if (LoadOperation != null && !LoadOperation.isDone)
            {
                progress01 = LoadOperation.progress;
                state = ScenePath;
                return true;
            }

            return false;
        }

        public virtual void InspectInList(ref int edited, int ind)
        {
            if (IsLoadedOrLoading)
            {
                var scene = SceneManager.GetSceneByPath(ScenePath);

                "Unload".PegiLabel().Click(() =>
                {
                    IsLoadedOrLoading = false;
                    return;
                });

                SceneManager.GetSceneByPath(ScenePath).name.PegiLabel().Write();
            }
            else
            {

#if UNITY_EDITOR
                if (!Application.isPlaying && IsValid && Icon.Add.Click())
                    EditorSceneManager.OpenScene(SceneReference.ScenePath, OpenSceneMode.Additive);
#endif

                if (LoadOperation != null && LoadOperation.isDone == false)
                {
                    "Loading {0}... {1}%".F(ScenePath, Mathf.FloorToInt(LoadOperation.progress * 100)).PegiLabel().Write();
                }
                else
                {
                    SceneReference.Nested_Inspect(fromNewLine: false);

                    if (Application.isPlaying)
                    {
                        Icon.Add.Click(() => IsLoadedOrLoading = true);

                        Icon.Load.Click(() => Load(LoadSceneMode.Single));
                    }
#if UNITY_EDITOR
                    else if (IsValid && "Switch".PegiLabel(toolTip: "Save scene before switching to another. Sure you want to change?").ClickConfirm(
                        confirmationTag: "SwSc" + ScenePath))
                        EditorSceneManager.OpenScene(ScenePath);
#endif

                }
            }
#if UNITY_EDITOR

            if (IsValid)
            {
                bool match = false;
                var allScenes = EditorBuildSettings.scenes;
                foreach (var sc in allScenes)
                {
                    if (sc.path.Equals(ScenePath))
                    {
                        match = true;

                        var enbl = sc.enabled;

                        if (pegi.ToggleIcon(ref enbl))
                        {
                            sc.enabled = enbl;
                            EditorBuildSettings.scenes = allScenes;
                        }

                        break;
                    }
                }

                if (!match)
                    "Add To Build".PegiLabel().Click(() =>
                    {
                        var lst = new List<EditorBuildSettingsScene>(allScenes)
                            {
                                new EditorBuildSettingsScene(ScenePath, enabled: true)
                            };
                        EditorBuildSettings.scenes = lst.ToArray();
                    });
            }
#endif
        }

        public override string ToString()
        {
            if (ScenePath.IsNullOrEmpty())
                return "NO SCENE PATH";
            var separator = ScenePath.LastIndexOfAny(new char[] { '/', '\\' });

            string result = ScenePath;

            if (separator > 0)
                result = result.Substring(separator + 1);

            return result.Replace(".unity", "");
        }
        public string NeedAttention()
        {
            if (SceneReference == null)
                return "Scene Reference is null";

            if (LoadingFailed)
                return "Loading failed";

            return SceneReference.NeedAttention();
        }

        #endregion
    }
}