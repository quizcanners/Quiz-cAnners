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
    public static partial class QcScenes
    {
        [Serializable]
        public class ReferenceInspectable : IPEGI_ListInspect, Singleton.ILoadingProgressForInspector, INeedAttention
        {
            public SerializableSceneReference SceneReference = new();
            public AsyncOperation LoadOperation;

            private readonly Gate.Frame _onLoadedInitializationOneFrameDelay = new();
            private int _framesSinceLoaded;

            public bool LoadingFailed { get; private set; }

            public bool IsValid => SceneReference != null && SceneReference.IsValid;

            public string ScenePath => IsValid ? SceneReference.ScenePath : "";

            private bool _fallbackState;

            public bool IsLoadedOrLoading
            {
                get
                {
                    if (LoadingFailed)
                        return _fallbackState;

                    if (s_scenesInQueueForLoading.Contains(this))
                        return true;

                    if (this == s_currentlyLoadingScene)
                        return true;

                    var result = IsLoaded; //(LoadOperation != null && !LoadOperation.isDone) || IsLoaded;

                    if (IsLoaded && _onLoadedInitializationOneFrameDelay.TryEnter())
                        _framesSinceLoaded++;

                    return result;
                }
                set
                {
                    _fallbackState = value;

                    if (value)
                        Load(); // LoadSceneMode.Additive);
                    else
                        Unload();
                }
            }

            public bool IsLoadedAndInitialized
            {
                get
                {
                    if (LoadingFailed)
                        return _fallbackState;

                    var scene = SceneManager.GetSceneByPath(SceneReference.ScenePath);
                    if (!scene.IsValid())
                        return false;

                    if (IsLoaded && _onLoadedInitializationOneFrameDelay.TryEnter())
                        _framesSinceLoaded++;

                    return _framesSinceLoaded >= 3;
                }
            }

            public bool IsLoaded
            {
                get
                {
                    if (LoadingFailed)
                        return _fallbackState;

                    if (SceneReference == null)
                        return false;

                    var sc = SceneManager.GetSceneByPath(SceneReference.ScenePath);
                    if (!sc.IsValid())
                        return false;

                    return sc.isLoaded;
                }
            }

#if UNITY_EDITOR
            public void SetScene_Editor(UnityEditor.SceneAsset asset)
            {
                SceneReference.SetScene_Editor(asset);
            }
#endif

            public void Load() //LoadSceneMode mode)
            {

                LoadingFailed = false;

                if (!Application.isPlaying)
                {
#if UNITY_EDITOR
                    if (IsValid && !IsLoaded)
                    {
                        var scene = EditorSceneManager.OpenScene(SceneReference.ScenePath, OpenSceneMode.Additive);
                        SceneManager.SetActiveScene(scene); // In Editor Scenes are usually opened to editing
                    }
#endif
                    return;
                }

                if (s_scenesInQueueForLoading.Contains(this))
                    return;

                if (LoadOperation != null && !LoadOperation.isDone)
                    return;

                if (IsLoaded)
                    return;

                if (s_currentlyLoadingScene != null)
                {
                    s_scenesInQueueForLoading.AddIfNew(this);
                }
                else
                {
                    StartLoad_Internal();
                }
            }

            internal void StartLoad_Internal()
            {
                try
                {
                    LoadOperation = SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Additive);
                    LoadOperation.completed += LoadNextScene;
                    _framesSinceLoaded = 0;
                }
                catch (Exception ex)
                {
                    LoadingFailed = true;
                    Debug.LogException(ex);
                    LoadNextScene(null);
                }
            }

            internal void Unload()
            {
                if (s_scenesInQueueForLoading.Remove(this))
                    return;

                if (this == s_currentlyLoadingScene) 
                {
                    s_scenesToUnload.Add(this);
                    return;
                }

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
                if (Application.isPlaying && LoadingFailed) 
                {
                    "Failed to load".PegiLabel().Write();

                    Icon.Warning.Draw();

                }
                else if (IsLoadedOrLoading)
                {
                    SceneUnloadOptions();

                    void SceneUnloadOptions()
                    {
                        var scene = SceneManager.GetSceneByPath(ScenePath);

#if UNITY_EDITOR
                        if (!Application.isPlaying && scene.isDirty)
                        {
                            if (Icon.Save.Click())
                                EditorSceneManager.SaveScene(scene);

                            if ("Unload".PegiLabel(toolTip: "The scene has unsaved changes. Are you sure you want to Unload and loose changes?").ClickConfirm(confirmationTag: "unload " + scene.name))
                            {
                                IsLoadedOrLoading = false;
                            }

                            return;
                        }
#endif

                        "Unload".PegiLabel().Click(() =>
                        {
                            IsLoadedOrLoading = false;
                            return;
                        });
                    }

                    SceneManager.GetSceneByPath(ScenePath).name.PegiLabel().Write();
                }
                else
                {

#if UNITY_EDITOR
                    if (!Application.isPlaying && IsValid && Icon.Add.Click())
                    {
                        Load();
                        //LoadingFailed = false;
                        //EditorSceneManager.OpenScene(SceneReference.ScenePath, OpenSceneMode.Additive);
                    }
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
                            Icon.Load.Click(() => Load());

                            if (pegi.PaintingGameViewUI)
                            {
                                ToString().PegiLabel().Write();
                            }

                        }
#if UNITY_EDITOR
                        else if (IsValid && !SceneReference.ScenePathDirty && "Switch".PegiLabel(toolTip: "Save scene before switching to another. Sure you want to change?").ClickConfirm(
                            confirmationTag: "SwSc" + ScenePath))
                        {
                            LoadingFailed = false;
                            EditorSceneManager.OpenScene(ScenePath);
                        }
#endif

                    }
                }
#if UNITY_EDITOR

                SceneReference.Inspect_SceneVaidity();

                if (IsValid && !SceneReference.ScenePathDirty)
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

                return QcSharp.GetFileNameFromPath(ScenePath);
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
}