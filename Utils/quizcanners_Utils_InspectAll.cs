using System;
using System.Collections.Generic;
using QuizCanners.Inspect;
using UnityEngine;

using Profiler = UnityEngine.Profiling.Profiler;
using QuizCanners.Inspect.Examples;
using static QuizCanners.Utils.QcDebug;

#if UNITY_EDITOR
using UnityEditor.Sprites;
#endif

namespace QuizCanners.Utils
{
#pragma warning disable IDE0018 // Inline variable declaration

    public static partial class QcUtils
    {
        public const string QUIZCANNERS = "Quiz c'Anners";
        public const string QUIZCANNERS_Scenes = QUIZCANNERS + "/Scenes";

        public static IPEGI CurrentProjectInspector;

        public static pegi.ChangesToken Inspect_TimeScaleOption()
        {
            
            var tScale = Time.timeScale;
            if ("Time.timescale".ConstL(toolTip: "For convenience will also modify Fixed Delta Time").Edit(ref tScale, 0f, 4f))
            {
                Time.timeScale = tScale;
                Time.fixedDeltaTime = Mathf.Clamp(Time.timeScale / 60, min: 0.001f, max: 0.02f);
                return pegi.ChangesToken.True;
            }

            return pegi.ChangesToken.False;
        }

        private static readonly pegi.EnterExitContext _context = new(playerPrefId: "inspEnt");
        private static readonly pegi.EnterExitContext _enteredData = new(playerPrefId: "inspEntDta");
        private static readonly LoopLock _inspectionLoopLock = new();


        public static void InspectAllUtils()
        {   
            if (!_inspectionLoopLock.Unlocked) 
            {
                "Recursively entered".PL().WriteWarning().Nl();
                return;
            }

            pegi.Nl();


            using (_inspectionLoopLock.Lock())
            using (_context.StartContext())
            {
                if (!_context.IsAnyEntered && Application.isPlaying) 
                {
                    pegi.Nl();
                    FrameRate.Inspect();
                    pegi.Nl();
                }

                var valid = !QcUnity.IsNullOrDestroyed_Obj(CurrentProjectInspector);

                var current = valid ? CurrentProjectInspector.ToString() : "";

                try
                {
                    pegi.Conditionally_Enter_Inspect(current.PL(), valid, CurrentProjectInspector).Nl();

                } catch (Exception ex) 
                {
                    pegi.Write_Exception(ex);
                }

                "Singletons".PL().IsEntered().Nl().If_Entered(Singleton.Collector.Inspect);

                "PEGI Documentation".PL().IsEntered(showLabelIfTrue: false).Nl_ifNotEntered().If_Entered(PlayerAndEditorGui_Documentation.Inspect);

                if ("Data".PL().IsEntered().Nl())
                {
                    using (_enteredData.StartContext())
                    {
                        if ("Cache".PL().IsEntered().Nl())
                        {
                            if ("Caching.ClearCache() [{0}]".F(Caching.cacheCount).PL().ClickConfirm("clCach").Nl())
                            {
                                if (Caching.ClearCache())
                                    pegi.GameView.ShowNotification("Bundles were cleared");
                                else
                                    pegi.GameView.ShowNotification("ERROR: Bundles are being used");
                            }

                            List<string> lst = new();

                            Caching.GetAllCachePaths(lst);

                            "Caches".PL().Edit_List(lst, path =>
                            {
                                var c = Caching.GetCacheByPath(path);

                                if (Icon.Delete.Click())
                                {
                                    if (Caching.RemoveCache(c))
                                        pegi.GameView.ShowNotification("Bundle was cleared");
                                    else
                                        pegi.GameView.ShowNotification("ERROR: Bundle is being used");
                                }

                                Icon.Folder.Click(()=> QcFile.Explorer.OpenPath(path));

                                Icon.Copy.Click(() => pegi.SetCopyPasteBuffer(path));

                                path.PL().Write();

                                return path;
                            });
                        }

                        "Reflection".PL().IsEntered().Nl().If_Entered(QcSharp.Reflector.Inspect).Nl();

                        if (_enteredData.IsAnyEntered == false)
                        {
                            if ("Player Data Folder".PL().Click().Nl())
                            {
                                QcFile.Explorer.OpenPersistentFolder();
                                pegi.SetCopyPasteBuffer(Application.persistentDataPath, sendNotificationIn3Dview: true);
                            }

                            if (Application.isEditor && "Editor Data Folder".PL().Click().Nl())
                                QcFile.Explorer.OpenPath(
                                    "C:/Users/{0}/AppData/Local/Unity/Editor/Editor.log".F(Environment.UserName));

                            "Mono Heap Size Long {0}".F(QcSharp.ToMegabytes(Profiler.GetMonoHeapSizeLong())).PL().Nl();

                            "Mono Used Size Long {0}".F(QcSharp.ToMegabytes(Profiler.GetMonoUsedSizeLong())).PL().Nl();

                            "Temp Allocated Size {0}".F(QcSharp.ToMegabytes(Profiler.GetTempAllocatorSize())).PL().Nl();

                            "Total Allocated Memmory Long {0}".F(QcSharp.ToMegabytes(Profiler.GetTotalAllocatedMemoryLong())).PL().Nl();

                            "Total Unused Reserved Memmory Long {0}".F(QcSharp.ToMegabytes(Profiler.GetTotalUnusedReservedMemoryLong())).PL().Nl();

                            if ("Unload Unused Assets".PL().Click().Nl())
                            {
                                Resources.UnloadUnusedAssets();
                            }
                        }

                    }
                }

                "Logs".PL().IsEntered().Nl().If_Entered(() => QcLog.LogHandler.Nested_Inspect());

                "Profiler".PL().Enter_Inspect(TimeProfiler.Instance).Nl();
    
                if ("Time & Audio".PL().IsEntered().Nl_ifEntered())
                {
                    if (Application.isEditor && Application.isPlaying && "Debug.Break()".PL().Click().Nl())
                        Debug.Break();

                    var maxDt = Time.maximumDeltaTime;
                    "Time.maximumDeltaTime".PL().Edit_Delayed(ref maxDt).Nl().OnChanged(()=> Time.maximumDeltaTime = maxDt);

                    "Time.time: {0}".F(QcSharp.SecondsToReadableString(Time.time)).PL().Nl();

                    "AudioSettings.dspTime: {0}".F(QcSharp.SecondsToReadableString(AudioSettings.dspTime)).PL().Nl();

                    "Use it to schedule Audio Clips: audioSource.PlayScheduled(AudioSettings.dspTime + 0.5);".PL().Write_Hint();

                    "Clip Duration: double duration = (double)AudioClip.samples / AudioClip.frequency;".PL().Write_Hint();

                    "Time.unscaled time: {0}".F(QcSharp.SecondsToReadableString(Time.unscaledTime)).PL().Nl();

                    "Time.frameCount: {0}".F(Time.frameCount).PL().Nl();

                    var phA = Physics.autoSyncTransforms;

                    "Physics Auto Sync Transforms".PL().ToggleIcon(ref phA).Nl(()=> Physics.autoSyncTransforms = phA);

                    Inspect_TimeScaleOption();

                    if (Mathf.Approximately(Time.timeScale, 1) == false && Icon.Refresh.Click())
                        Time.timeScale = 1;

                    pegi.Nl();

                    var fTScale = Time.fixedDeltaTime;
                    if ("Time.fixedDeltaTime".ConstL().Edit(ref fTScale, 0, 0.5f))
                        Time.fixedDeltaTime = fTScale;

                    pegi.Nl();

                    "Time.deltaTime: {0}".F(QcSharp.SecondsToReadableString(Time.deltaTime)).PL().Nl();

                    "Time.realtimeSinceStartup {0}".F(QcSharp.SecondsToReadableString(Time.realtimeSinceStartup)).PL().Nl();

                    
                }

                if (!_context.IsAnyEntered && Application.isPlaying && Time.timeScale < 0.1f)
                    Icon.Warning.Draw("Delta time is "+ Time.timeScale);

                pegi.Nl();

                if ("Graphics".PL().IsEntered().Nl()) 
                {
                    var fr = Application.targetFrameRate;
                    if ("Frame-Rate".ConstL().Edit(ref fr).Nl() && fr > 0)
                    {
                        Application.targetFrameRate = fr;
                    }

                    var res = Screen.currentResolution;

                    int width = res.width;
                    int height = res.height;

                    "Screen: {0}x{1}".F(Screen.width, Screen.height).PL().Nl();

                    "Display: {0}x{1}".F(Display.main.renderingWidth, Display.main.renderingHeight).PL().Nl();

                    "Resolution: {0}x{1}".F(width, height).PL().Nl();

                    var changes = pegi.ChangeTrackStart();

                    "Width".ConstL().Edit(ref width, 8, Display.main.renderingWidth).Nl();
                    "Height".ConstL().Edit(ref height, 8, Display.main.renderingHeight).Nl();

                    if (changes)
                        Screen.SetResolution(width, height, fullscreen: true);

                }

                if ("Screen Shots".PL().IsEntered().Nl())
                    screenShots.Nested_Inspect();

                if ("Texture Utils".PL().IsEntered().Nl())
                {
                    if (Application.isEditor)
                    {

                        Sprite sa = null;
                        "To extract a Texture from Sprite, Set Read/Write Enabled to True and make sure it's format is Uncompressed (RGBA32 should do it)".PL().Write_Hint();

                        if ("Extract Sprite Atlas Texture".PL().Edit(ref sa) && sa)
                        {
#if UNITY_EDITOR
                            string atlasName;
                            Texture2D atlasTexture;

                            Packer.GetAtlasDataForSprite(sa, out atlasName, out atlasTexture);

                            //var atlas = SpriteUtility.GetSpriteTexture(sa, getAtlasData: true);
                            if (atlasTexture)
                            {
                                atlasTexture.Reimport_IfNotReadale_Editor();

                                string name = atlasName;//"From {0}".F(sa.name);
                                QcUnity.SaveTextureAsAsset(atlasTexture, "Atlas Textures", ref name, saveAsNew: true);
                            }
#endif
                        }
                    }
                    else
                        "Only in Editor".PL().WriteWarning();
                }

                "Debug".PL().IsEntered().Nl().If_Entered(QcDebug.Inspect);

                if (!_context.IsAnyEntered)
                    QcScenes.Inspect();

            }
        }


    }
}