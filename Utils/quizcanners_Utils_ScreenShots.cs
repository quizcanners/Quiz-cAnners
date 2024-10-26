using QuizCanners.Inspect;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Utils
{

    public static partial class QcUtils
    {

        [Serializable]
        public class ScreenShootTaker : IPEGI
        {
            [SerializeField] public string folderName = "ScreenShoots";

            private bool _showAdditionalOptions;

            void IPEGI.Inspect()
            {
                pegi.Nl();

                "Camera ".PegiLabel().SelectInScene(ref cameraToTakeScreenShotFrom);

                pegi.Nl();

                "Transparent Background".PegiLabel().ToggleIcon(ref AlphaBackground);

                if (!AlphaBackground && cameraToTakeScreenShotFrom)
                {
                    if (cameraToTakeScreenShotFrom.clearFlags == CameraClearFlags.Color &&
                        cameraToTakeScreenShotFrom.clearFlags == CameraClearFlags.SolidColor)
                    {
                        var col = cameraToTakeScreenShotFrom.backgroundColor;
                        if (pegi.Edit(ref col))
                            cameraToTakeScreenShotFrom.backgroundColor = col;
                    }
                }

                pegi.Nl();

                var ssName = _screenShotName.GetValue();
                "Img Name".ConstLabel().Edit(ref ssName).OnChanged(() => _screenShotName.SetValue(ssName));
                var path = System.IO.Path.Combine(QcFile.OutsideOfAssetsFolder, folderName);
                if (Icon.Folder.Click("Open Screen Shots Folder : {0}".F(path)))
                    QcFile.Explorer.OpenPath(path);

                pegi.Nl();

                "Up Scale".PegiLabel("Resolution of the texture will be multiplied by a given value", 60).Edit(ref UpScale);

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

    }
}