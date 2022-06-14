using QuizCanners.Utils;
using UnityEngine;

namespace QuizCanners.Inspect
{
    public static partial class pegi
    {
        public enum LatestInteractionEvent { None, Click, SliderScroll, Enter, Exit }

        public static class GameView
        {
            private static readonly Gate.Frame _interactionFrame = new Gate.Frame();
            private static LatestInteractionEvent _latestEvent;



            public static LatestInteractionEvent LatestEvent
            {
                get
                {
                    if (_interactionFrame.TryEnter())
                        _latestEvent = LatestInteractionEvent.None;

                    return _latestEvent;
                }

                set
                {
                    _interactionFrame.TryEnter();
                    _latestEvent = value;
                }
            }

            private static System.Type _gameViewType;
            private static int _mouseOverUi = -1;

            public static void ShowNotification(string text)
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                {
                    if (_gameViewType == null)
                        _gameViewType = typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.GameView");

                    if (_gameViewType != null)
                    {
                        UnityEditor.EditorWindow ed = UnityEditor.EditorWindow.GetWindow(_gameViewType);
                        if (ed != null)
                            ed.ShowNotification(new GUIContent(text));
                    }
                }
                else
                {
                    var lst = Resources.FindObjectsOfTypeAll<UnityEditor.SceneView>();

                    foreach (var w in lst)
                        w.ShowNotification(new GUIContent(text));

                }
#endif
            }

            public static bool MouseOverUI
            {
                get { return _mouseOverUi >= Time.frameCount - 1; }
                set
                {
                    if (value) _mouseOverUi = Time.frameCount;
                }
            }

            public delegate void WindowFunction();

            private static string _tooltip;
            private static int _tooltipDelay;

            public class Window
            {
                public float Upscale;
                private WindowFunction _function;
                private Rect _windowRect;
                private Vector2 _scrollPosition;

                private bool _useExactSize;

                private int _maxWidth;
                private int _maxHeight;
                private bool _foldedIn;
                public bool FoldedIn 
                {
                    get => _foldedIn;
                    set 
                    {
                        _foldedIn = value;

                        if (value)
                            Collapse();
                        else
                        {
                            if (_useExactSize)
                            {
                                _windowRect.width = _maxWidth;
                                _windowRect.height = _maxHeight;
                            }
                            else
                            {
                                _windowRect.width = Screen.width * 0.5f;
                                _windowRect.height = Screen.height - 20;
                            }
                        }
                    }
                }

                protected bool UseWindow => Mathf.Approximately(Upscale, 1);
                private void DrawFunctionWrapper(int windowID)
                {
                    PaintingGameViewUI = true;
                    PegiEditorOnly.StartObject();

                    try
                    {
                        if (!UseWindow)
                        {
                            GUI.matrix = Matrix4x4.TRS(new Vector3(0, 0, 0), Quaternion.identity,
                                new Vector3(Upscale, Upscale, 1));
                            GUILayout.BeginArea(new Rect(40 / Upscale, 20 / Upscale, Screen.width / Upscale,
                                Screen.height / Upscale));

                            FoldedIn = false;
                        }
                        else
                        {
                            if (Icon.FoldedOut.Click().Nl())
                                FoldedIn = !FoldedIn;
                        }

                        if (!FoldedIn)
                        {
                            if (UseWindow)
                            {
                                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition
                                   , GUILayout.Width(_windowRect.width * 0.9f)
                                   , GUILayout.Height(_windowRect.height * 0.9f));
                            }
                            else
                            {
                                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition
                                    , GUILayout.Width(Screen.width * 0.9f / Upscale)
                                    , GUILayout.Height(Screen.height * 0.9f / Upscale));
                            }

                            if (!FullWindow.ShowingPopup())
                                _function();

                            var gotTooltip = !GUI.tooltip.IsNullOrEmpty();

                            if (gotTooltip)
                            {
                                _tooltip = "{0}:{1}".F(Msg.ToolTip.GetText(), GUI.tooltip);
                                _tooltipDelay = 50;
                            }
                            else
                            {
                                _tooltipDelay--;
                                _tooltip = _tooltipDelay > 0 ? _tooltip : " ";
                            }


                            pegi.Nl();
                            _tooltip.PegiLabel(style: Styles.HintText).Nl();

                            UnIndent();

                        }

                        if (!FoldedIn)
                        {
                            GUILayout.EndScrollView();
                        }

                        if (UseWindow)
                        {
 #if ENABLE_LEGACY_INPUT_MANAGER
                            if (_windowRect.Contains(Input.mousePosition))
                                MouseOverUI = true;
#endif

                            GUI.DragWindow(new Rect(0, 0, 3000, 40 * Upscale));
                        }
                        else
                        {
                            MouseOverUI = true;
                            GUILayout.EndArea();
                        }

                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogException(ex);
                    }

                    PegiEditorOnly.EndObject(PegiEditorOnly.inspectedUnityObject);

                    PaintingGameViewUI = false;
                }
                public void Render(IPEGI p) => Render(p, p.Inspect, p.GetNameForInspector());
                public void Render(IPEGI p, string windowName) => Render(p, p.Inspect, windowName);
                public void Render(IPEGI target, WindowFunction doWindow, string c_windowName)
                {

                    PegiEditorOnly.ResetInspectionTarget(target);

                    _function = doWindow;

                    if (UseWindow)
                    {
                        _windowRect.x = Mathf.Clamp(_windowRect.x, 0, Screen.width - 10);
                        _windowRect.y = Mathf.Clamp(_windowRect.y, 0, Screen.height - 10);

                        _windowRect.width = Mathf.Clamp(_windowRect.width, 10, Screen.width);
                        _windowRect.height = Mathf.Clamp(_windowRect.height, 10, Screen.height);

                        _windowRect = GUILayout.Window(0, _windowRect, DrawFunctionWrapper, c_windowName,
                            GUILayout.MaxWidth(360 * Upscale), GUILayout.ExpandHeight(IsFoldedOut), GUILayout.ExpandWidth(IsFoldedOut));
                    }
                    else
                    {
                        DrawFunctionWrapper(0);
                    }

                }
                public void Collapse()
                {
                    _windowRect.width = 50;
                    _windowRect.height = 50;
                }

                public Window(int windowWidth, int windowHeight)
                {
                    this.Upscale = 1;
                    _maxWidth = windowWidth;
                    _maxHeight = windowHeight;
                    _useExactSize = true;
                    _windowRect = new Rect(20, 50, 350, 400);
                    FoldedIn = true;
                }

                public Window(float upscale = 1)
                {
                    this.Upscale = upscale;
                    _windowRect = new Rect(20, 50, 350 * upscale, 400 * upscale);
                }
            }

            public static float AspectRatio
            {
                get
                {
                    var res = Resolution;
                    return res.x / res.y;
                }
            }
            public static int Width => (int)Resolution.x;
            public static int Height => (int)Resolution.y;
            public static Vector2 Resolution
            {
                get
                {
#if UNITY_EDITOR
                    return UnityEditor.Handles.GetMainGameViewSize();
#else
                    return new Vector2(Screen.width, Screen.height);
#endif
                }
            }
        }


    }
}
