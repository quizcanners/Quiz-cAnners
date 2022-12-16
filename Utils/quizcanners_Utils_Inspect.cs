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

        #region Various Managers Classes
    
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

            private readonly PlayerPrefValue.String _screenShotName = new PlayerPrefValue.String("qc_ScreenShotName", defaultValue: "Screen Shot");

           // public string screenShotName;

            private string GetScreenShotName()
            {
                var name = _screenShotName.GetValue();

                if (name.IsNullOrEmpty())
                    name = "SS-" + DateTime.Now.ToString("yyyy.MM.dd.HH.mm.ss");

                return name;
            }

        }

        [Serializable]
        public struct DynamicRangeFloat : ICfgCustom, IPEGI
        {

            [SerializeField] public float min;
            [SerializeField] public float max;

            [SerializeField] private float _value;

            public float Value
            {
                get { return _value; }

                set
                {
                    _value = value;
                    min = Mathf.Min(min, value);
                    max = Mathf.Max(max, value);
                    UpdateRange();
                }
            }

            #region Inspector

            private float dynamicMin;
            private float dynamicMax;

            private void UpdateRange(float by = 1)
            {

                float width = dynamicMax - dynamicMin;

                width *= by * 0.5f;

                dynamicMin = Mathf.Max(min, _value - width);
                dynamicMax = Mathf.Min(max, _value + width);
            }

            private bool _showRange;

            public void Inspect()
            {
                var rangeChanged = false;

                if ("><".PegiLabel().Click())
                    UpdateRange(0.3f);

                pegi.Edit(ref _value, dynamicMin, dynamicMax);
                //    Value = _value;

                if ("<>".PegiLabel().Click())
                    UpdateRange(3f);
             

                if (!_showRange && Icon.Edit.ClickUnFocus("Edit Range", 20))
                    _showRange = true;

                if (_showRange)
                {
                  

                    if (Icon.FoldedOut.ClickUnFocus("Hide Range"))
                        _showRange = false;

                    pegi.Nl();

                    "[{0} : {1}] - {2}".F(dynamicMin, dynamicMax, "Focused Range").PegiLabel().Nl();

                    "Range: [".PegiLabel(60).Write();

                    var before = min;


                    if (pegi.Edit_Delayed(ref min, 40))
                    {
                        rangeChanged = true;

                        if (min >= max)
                            max = min + (max - before);
                    }

                    "-".PegiLabel(10).Write();

                    if (pegi.Edit_Delayed(ref max, 40))
                    {
                        rangeChanged = true;
                        min = Mathf.Min(min, max);
                    }

                    "]".PegiLabel(10).Write();

                    pegi.FullWindow.DocumentationClickOpen("Use >< to shrink range around current value for more precision. And <> to expand range.", "About <> & ><");

                    if (Icon.Refresh.Click())
                    {
                        dynamicMin = min;
                        dynamicMax = max;

                    }

                    pegi.Nl();

                    "Tap Enter to apply Range change in the field (will Clamp current value)".PegiLabel().Write_Hint();



                    pegi.Nl();

                    if (rangeChanged)
                    {
                        Value = Mathf.Clamp(_value, min, max);

                        if (Mathf.Abs(dynamicMin - dynamicMax) < (float.Epsilon * 10))
                        {
                            dynamicMin = Mathf.Clamp(dynamicMin - float.Epsilon * 10, min, max);
                            dynamicMax = Mathf.Clamp(dynamicMax + float.Epsilon * 10, min, max);
                        }
                    }


                }
            }

            #endregion

            #region Encode & Decode

            public CfgEncoder Encode() => new CfgEncoder()
                .Add_IfNotEpsilon("m", min)
                .Add_IfNotEpsilon("v", Value)
                .Add_IfNotEpsilon("x", max);

            public void DecodeInternal(CfgData data)
            {
              
                new CfgDecoder(data).DecodeTagsFor(ref this);
                dynamicMin = min;
                dynamicMax = max;
            }

            public void DecodeTag(string key, CfgData data)
            {
                switch (key)
                {
                    case "m":
                        min = data.ToFloat();
                        break;
                    case "v":
                        Value = data.ToFloat();
                        break;
                    case "x":
                        max = data.ToFloat();
                        break;
                }
            }

            #endregion

            public DynamicRangeFloat(float min = 0, float max = 1, float value = 0.5f)
            {
                this.min = min;
                this.max = max;
                dynamicMin = min;
                dynamicMax = max;
                _value = value;

                _showRange = false;

            }
        }

        private static readonly ScreenShootTaker screenShots = new ScreenShootTaker();

        #endregion

        #region Inspect Debug Options 

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

        private static readonly pegi.EnterExitContext _context = new pegi.EnterExitContext(playerPrefId: "inspEnt");
        private static readonly pegi.EnterExitContext _enteredData = new pegi.EnterExitContext(playerPrefId: "inspEntDta");
   
        public static void InspectAllUtils()
        {
            pegi.Nl();

            using (_context.StartContext())
            {
                if (!_context.IsAnyEntered && Application.isPlaying) 
                {
                    pegi.Nl();
                    FrameRate.Inspect();
                    pegi.Nl();
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

                            List<string> lst = new List<string>();

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

                "Profiler".PegiLabel().Enter_Inspect(QcDebug.TimeProfiler.Instance).Nl();
    
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

                    var fr = Application.targetFrameRate;
                    if ("Frame-Rate".PegiLabel().Edit(ref fr).Nl() && fr > 0)
                    {
                        Application.targetFrameRate = fr;
                    }
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