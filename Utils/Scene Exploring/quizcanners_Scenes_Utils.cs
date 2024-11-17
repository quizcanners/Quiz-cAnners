using QuizCanners.Inspect;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Utils
{
    public static partial class QcScenes
    {
        private static ReferenceInspectable s_currentlyLoadingScene;
        private static readonly List<ReferenceInspectable> s_scenesInQueueForLoading = new();

        private static readonly List<ReferenceInspectable> s_scenesToUnload = new();
       
        public static bool IsAnyLoading
        { 
            get 
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    return false;
#endif

                if (!_afterLastLoad.IsFramesPassed(3))
                    return true;

                if (s_currentlyLoadingScene != null || s_scenesInQueueForLoading.Count > 0)
                {
                    _afterLastLoad.TryEnter();
                    return true;
                }

                return false;
            }
        }
        private static readonly Gate.Frame _afterLastLoad = new(Gate.InitialValue.StartArmed);

        private static void LoadNextScene(AsyncOperation previous)
        {
            _afterLastLoad.TryEnter();

            s_currentlyLoadingScene = null;

            var toUnload = new List<ReferenceInspectable>(s_scenesToUnload);
            s_scenesToUnload.Clear();

            foreach (var s in toUnload) 
            {
                s.Unload();
            }

            if (s_scenesToUnload.Count > 0)
                Debug.LogError("Scenes were added to unload queue after Unloading loop");

            if (s_scenesInQueueForLoading.Count == 0)
                return;

            var first = s_scenesInQueueForLoading.TryTake(0);

            first.StartLoad_Internal();
        }

        private static readonly pegi.CollectionInspectorMeta _loadingQueue = new("Loading Queue");

        

        public static void Inspect() 
        {
            if (s_currentlyLoadingScene == null && s_scenesInQueueForLoading.Count ==0)
            {
                "All Scenes Loaded".PegiLabel().Nl();
                return;
            }

            if (s_currentlyLoadingScene == null)
                "No current loading".PegiLabel().Nl();
            else
                s_currentlyLoadingScene.InspectInList_Nested().Nl();

            if (s_scenesInQueueForLoading.Count == 0)
                "No scenes in queue".PegiLabel().Nl();
            else 
                _loadingQueue.Edit_List(s_scenesInQueueForLoading).Nl();
        }
    }
}
