using System;
using System.Collections.Generic;
using QuizCanners.Migration;
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

    public static class QcUtils
    {
        public const string QUIZ_cANNERS = "Quiz c'Anners";

        [Serializable]
        public class ScreenShootTaker : IPEGI
        {
            [SerializeField] public string folderName = "ScreenShoots";

            private bool _showAdditionalOptions;

            public void Inspect()
            {
                pegi.Nl();

                "Camera ".PegiLabel().SelectInScene(ref cameraToTakeScreenShotFrom);

                pegi.Nl();

                "Transparent Background".PegiLabel().ToggleIcon(ref AlphaBackground);

                if (!AlphaBackground && cameraToTakeScreenShotFrom)
                {
                    if (cameraToTakeScreenShotFrom.clearFlags == CameraClearFlags.Color &&
                        cameraToTakeScreenShotFrom.clearFlags == CameraClearFlags.SolidColor) {
                        var col = cameraToTakeScreenShotFrom.backgroundColor;
                        if (pegi.Edit(ref col))
                            cameraToTakeScreenShotFrom.backgroundColor = col;
                    }
                }

                pegi.Nl();

                var ssName = _screenShotName.GetValue();
                "Img Name".PegiLabel(90).Edit(ref ssName).OnChanged(()=> _screenShotName.SetValue(ssName));
                var path = System.IO.Path.Combine(QcFile.OutsideOfAssetsFolder, folderName);
                if (Icon.Folder.Click("Open Screen Shots Folder : {0}".F(path)))
                    QcFile.Explorer.OpenPath(path);

                pegi.Nl();

                "Up Scale".PegiLabel("Resolution of the texture will be multiplied by a given value", 60).Edit( ref UpScale);

                if (UpScale <= 0)
                    "Scale value needs to be positive".PegiLabel().WriteWarning();
                else
                if (cameraToTakeScreenShotFrom)
                {

                    if (UpScale > 4)
                    {
                        if ("Take Very large ScreenShot".PegiLabel("This will try to take a very large screen shot. Are we sure?").ClickConfirm("tbss"))
                            RenderToCameraAndSave();
                    }
                    else if (Icon.ScreenGrab.Click("Render Screenshoot from camera").Nl())
                        RenderToCameraAndSave();
                }

                pegi.FullWindow.DocumentationClickOpen("To Capture UI with this method, use Canvas-> Render Mode-> Screen Space - Camera. " +
                                                              "You probably also want Transparent Background turned on. Or not, depending on your situation. " +
                                                              "Who am I to tell you what to do, I'm just a hint.");

                pegi.Nl();

                if ("Other Options".PegiLabel().IsFoldout(ref _showAdditionalOptions).Nl())
                {

                    if (!grab)
                    {
                        if ("On Post Render()".PegiLabel().Click())
                            grab = true;
                    }
                    else
                        ("To grab screen-shot from Post-Render, OnPostRender() of this class should be called from OnPostRender() of the script attached to a camera." +
                         " Refer to Unity documentation to learn more about OnPostRender() call").PegiLabel()
                            .Write_Hint();


                    pegi.Nl();

                    if ("ScreenCapture.CaptureScreenshot".PegiLabel().Click())
                        CaptureByScreenCaptureUtility();


                    if (Icon.Folder.Click())
                        QcFile.Explorer.OpenPath(QcFile.OutsideOfAssetsFolder);

                    pegi.FullWindow.DocumentationClickOpen("Game View Needs to be open for this to work");

                }

                pegi.Nl();

            }

            private bool grab;

            [SerializeField] private Camera cameraToTakeScreenShotFrom;
            [SerializeField] private int UpScale = 1;
            [SerializeField] private bool AlphaBackground;

            [NonSerialized] private RenderTexture forScreenRenderTexture;
            [NonSerialized] private Texture2D screenShotTexture2D;

            public void CaptureByScreenCaptureUtility()
            {
                ScreenCapture.CaptureScreenshot("{0}".F(System.IO.Path.Combine(folderName, GetScreenShotName()) + ".png"), UpScale);
            }

            public void RenderToCameraAndSave()
            {
                var tex = RenderToCamera(cameraToTakeScreenShotFrom);
                QcFile.Save.TextureOutsideAssetsFolder(folderName, GetScreenShotName(), ".png", tex);
            }

            public Texture2D RenderToCamera(Camera camera)
            {
                var cam = camera;
                var w = cam.pixelWidth * UpScale;
                var h = cam.pixelHeight * UpScale;

                CheckRenderTexture(w, h);
                CheckTexture2D(w, h);

                cam.targetTexture = forScreenRenderTexture;
                var clearFlags = cam.clearFlags;

                if (AlphaBackground)
                {
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    var col = cam.backgroundColor;
                    col.a = 0;
                    cam.backgroundColor = col;
                }
                else
                {
                    var col = cam.backgroundColor;
                    col.a = 1;
                    cam.backgroundColor = col;
                }

                cam.Render();
                RenderTexture.active = forScreenRenderTexture;
                screenShotTexture2D.ReadPixels(new Rect(0, 0, w, h), 0, 0);

                if (!AlphaBackground)
                    MakeOpaque(screenShotTexture2D);

                screenShotTexture2D.Apply();

                cam.targetTexture = null;
                RenderTexture.active = null;
                cam.clearFlags = clearFlags;

                return screenShotTexture2D;
            }


            private void MakeOpaque(Texture2D tex)
            {
                var pixels = tex.GetPixels32();

                for (int i = 0; i < pixels.Length; i++)
                {
                    var col = pixels[i];
                    col.a = 255;
                    pixels[i] = col;
                }

                tex.SetPixels32(pixels);
            }

            public void OnPostRender()
            {
                if (grab)
                {

                    grab = false;

                    var w = Screen.width;
                    var h = Screen.height;

                    CheckTexture2D(w, h);

                    screenShotTexture2D.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0, false);
                    screenShotTexture2D.Apply();

                    QcFile.Save.TextureOutsideAssetsFolder("ScreenShoots", GetScreenShotName(), ".png",
                        screenShotTexture2D);

                }
            }

            private void CheckRenderTexture(int w, int h)
            {
                if (!forScreenRenderTexture || forScreenRenderTexture.width != w || forScreenRenderTexture.height != h)
                {

                    if (forScreenRenderTexture)
                        forScreenRenderTexture.DestroyWhatever();

                    forScreenRenderTexture = new RenderTexture(w, h, 32);
                }

            }

            private void CheckTexture2D(int w, int h)
            {
                if (!screenShotTexture2D || screenShotTexture2D.width != w || screenShotTexture2D.height != h)
                {

                    if (screenShotTexture2D)
                        screenShotTexture2D.DestroyWhatever();

                    screenShotTexture2D = new Texture2D(w, h, TextureFormat.ARGB32, false);
                }
            }

            private readonly PlayerPrefValue.String _screenShotName = new("qc_ScreenShotName", defaultValue: "Screen Shot");

           // public string screenShotName;

            private string GetScreenShotName()
            {
                var name = _screenShotName.GetValue();

                if (name.IsNullOrEmpty())
                    name = "SS-" + DateTime.Now.ToString("yyyy.MM.dd.HH.mm.ss");

                return name;
            }

        }

        private static readonly ScreenShootTaker screenShots = new();

        #region Inspect Debug Options 

        public static IPEGI CurrentProjectInspector;

        public static pegi.ChangesToken Inspect_TimeScaleOption()
        {
            
            var tScale = Time.timeScale;
            if ("Time.timescale".PegiLabel(toolTip: "For convenience will also modify Fixed Delta Time", 100).Edit(ref tScale, 0f, 4f))
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
                "Recursively entered".PegiLabel().WriteWarning().Nl();
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
                    pegi.Conditionally_Enter_Inspect(current.PegiLabel(), valid, CurrentProjectInspector).Nl();

                } catch (Exception ex) 
                {
                    pegi.Write_Exception(ex);
                }

                "Singletons".PegiLabel().IsEntered().Nl().If_Entered(Singleton.Collector.Inspect);

                "PEGI Documentation".PegiLabel().IsEntered(showLabelIfTrue: false).Nl_ifNotEntered().If_Entered(PlayerAndEditorGui_Documentation.Inspect);

                if ("Data".PegiLabel().IsEntered().Nl())
                {
                    using (_enteredData.StartContext())
                    {
                        if ("Cache".PegiLabel().IsEntered().Nl())
                        {
                            if ("Caching.ClearCache() [{0}]".F(Caching.cacheCount).PegiLabel().ClickConfirm("clCach").Nl())
                            {
                                if (Caching.ClearCache())
                                    pegi.GameView.ShowNotification("Bundles were cleared");
                                else
                                    pegi.GameView.ShowNotification("ERROR: Bundles are being used");
                            }

                            List<string> lst = new();

                            Caching.GetAllCachePaths(lst);

                            "Caches".PegiLabel().Edit_List(lst, path =>
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

                                path.PegiLabel().Write();

                                return path;
                            });
                        }

                        "Reflection".PegiLabel().IsEntered().Nl().If_Entered(QcSharp.Reflector.Inspect).Nl();

                        if (_enteredData.IsAnyEntered == false)
                        {
                            if ("Player Data Folder".PegiLabel().Click().Nl())
                            {
                                QcFile.Explorer.OpenPersistentFolder();
                                pegi.SetCopyPasteBuffer(Application.persistentDataPath, sendNotificationIn3Dview: true);
                            }

                            if (Application.isEditor && "Editor Data Folder".PegiLabel().Click().Nl())
                                QcFile.Explorer.OpenPath(
                                    "C:/Users/{0}/AppData/Local/Unity/Editor/Editor.log".F(Environment.UserName));

                            "Mono Heap Size Long {0}".F(Profiler.GetMonoHeapSizeLong().ToMegabytes()).PegiLabel().Nl();

                            "Mono Used Size Long {0}".F(Profiler.GetMonoUsedSizeLong().ToMegabytes()).PegiLabel().Nl();

                            "Temp Allocated Size {0}".F(ToMegabytes(Profiler.GetTempAllocatorSize())).PegiLabel().Nl();

                            "Total Allocated Memmory Long {0}".F(Profiler.GetTotalAllocatedMemoryLong().ToMegabytes()).PegiLabel().Nl();

                            "Total Unused Reserved Memmory Long {0}".F(Profiler.GetTotalUnusedReservedMemoryLong().ToMegabytes()).PegiLabel().Nl();

                            if ("Unload Unused Assets".PegiLabel().Click().Nl())
                            {
                                Resources.UnloadUnusedAssets();
                            }
                        }

                    }
                }

                "Logs".PegiLabel().IsEntered().Nl().If_Entered(() => QcLog.LogHandler.Nested_Inspect());

                "Profiler".PegiLabel().Enter_Inspect(TimeProfiler.Instance).Nl();
    
                if ("Time & Audio".PegiLabel().IsEntered().Nl())
                {
                    if (Application.isEditor && Application.isPlaying && "Debug.Break()".PegiLabel().Click().Nl())
                        Debug.Break();

                    var maxDt = Time.maximumDeltaTime;
                    "Time.maximumDeltaTime".PegiLabel().Edit_Delayed(ref maxDt).Nl().OnChanged(()=> Time.maximumDeltaTime = maxDt);

                    "Time.time: {0}".F(QcSharp.SecondsToReadableString(Time.time)).PegiLabel().Nl();

                    "AudioSettings.dspTime: {0}".F(QcSharp.SecondsToReadableString(AudioSettings.dspTime)).PegiLabel().Nl();

                    "Use it to schedule Audio Clips: audioSource.PlayScheduled(AudioSettings.dspTime + 0.5);".PegiLabel().Write_Hint();

                    "Clip Duration: double duration = (double)AudioClip.samples / AudioClip.frequency;".PegiLabel().Write_Hint();

                    "Time.unscaled time: {0}".F(QcSharp.SecondsToReadableString(Time.unscaledTime)).PegiLabel().Nl();

                    "Time.frameCount: {0}".F(Time.frameCount).PegiLabel().Nl();

                    var phA = Physics.autoSyncTransforms;

                    "Physics Auto Sync Transforms".PegiLabel().ToggleIcon(ref phA).Nl(()=> Physics.autoSyncTransforms = phA);

                    Inspect_TimeScaleOption();

                    if (Mathf.Approximately(Time.timeScale, 1) == false && Icon.Refresh.Click())
                        Time.timeScale = 1;

                    pegi.Nl();

                    var fTScale = Time.fixedDeltaTime;
                    if ("Time.fixedDeltaTime".PegiLabel(120).Edit(ref fTScale, 0, 0.5f))
                        Time.fixedDeltaTime = fTScale;

                    pegi.Nl();

                    "Time.deltaTime: {0}".F(QcSharp.SecondsToReadableString(Time.deltaTime)).PegiLabel().Nl();

                    "Time.realtimeSinceStartup {0}".F(QcSharp.SecondsToReadableString(Time.realtimeSinceStartup)).PegiLabel().Nl();

                    
                }

                if ("Graphics".PegiLabel().IsEntered().Nl()) 
                {
                    var fr = Application.targetFrameRate;
                    if ("Frame-Rate".PegiLabel().Edit(ref fr).Nl() && fr > 0)
                    {
                        Application.targetFrameRate = fr;
                    }

                    var res = Screen.currentResolution;

                    int width = res.width;
                    int height = res.height;

                    "Screen: {0}x{1}".F(Screen.width, Screen.height).PegiLabel().Nl();

                    "Display: {0}x{1}".F(Display.main.renderingWidth, Display.main.renderingHeight).PegiLabel().Nl();

                    "Resolution: {0}x{1}".F(width, height).PegiLabel().Nl();

                    var changes = pegi.ChangeTrackStart();

                    "Width".PegiLabel(60).Edit(ref width, 8, Display.main.renderingWidth).Nl();
                    "Height".PegiLabel(60).Edit(ref height, 8, Display.main.renderingHeight).Nl();

                    if (changes)
                        Screen.SetResolution(width, height, fullscreen: true);

                }

                if ("Screen Shots".PegiLabel().IsEntered().Nl())
                    screenShots.Nested_Inspect();

                if ("Texture Utils".PegiLabel().IsEntered().Nl())
                {
                    if (Application.isEditor)
                    {

                        Sprite sa = null;
                        "To extract a Texture from Sprite, Set Read/Write Enabled to True and make sure it's format is Uncompressed (RGBA32 should do it)".PegiLabel().Write_Hint();

                        if ("Extract Sprite Atlas Texture".PegiLabel().Edit(ref sa) && sa)
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
                        "Only in Editor".PegiLabel().WriteWarning();
                }

                "Debug".PegiLabel().IsEntered().Nl().If_Entered(QcDebug.Inspect);
            }
        }

        public static string ToMegabytes(uint bytes)
        {
            bytes >>= 10;
            bytes /= 1024; // On new line to workaround IL2CPP bug
            return "{0} Mb".F(bytes.ToString());
        }
        
        internal static string ToMegabytes(this long bytes)
        {
            bytes >>= 10;
            bytes /= 1024; // On new line to workaround IL2CPP bug
            return "{0} Mb".F(bytes.ToString());
        }

        #endregion
    }
}