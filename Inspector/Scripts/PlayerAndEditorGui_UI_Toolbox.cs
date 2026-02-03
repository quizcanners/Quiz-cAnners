//#define USE_UI_TOOLKIT

using QuizCanners.Utils;
using System;
using UnityEngine;

#if USE_UI_TOOLKIT
using UnityEngine.UIElements;
#endif

namespace QuizCanners.Inspect
{
    public static partial class pegi
    {
        
        public static class Toolkit 
        {
            //internal static State CurrentState;

            [Serializable]
            public class State
            {
                private readonly Gate.Integer documentVersion = new();
#if USE_UI_TOOLKIT
                [SerializeField] private UIDocument _document;
                   private IPEGI ipegi;
#endif
                [SerializeField] private MonoBehaviour _target;

                private int _dataVersion;
             

                public void SetDirty() => _dataVersion++;

                public void Clear()
                {
#if USE_UI_TOOLKIT
                    if (_document != null && _document.rootVisualElement != null)
                        _document.rootVisualElement.Clear();
#endif

                    documentVersion.ValueIsDefined = false;
                }

                public void LateUpdate()
                {
                    if (!documentVersion.TryChange(_dataVersion))
                        return;

#if USE_UI_TOOLKIT
                    UnityEngine.Debug.Log("Refreshing Toolkit state");

                    _root = _document.rootVisualElement;
                    _root.Clear();

                    ipegi ??= _target.GetComponent<IPEGI>();

                    _root.RegisterCallback<PointerEnterEvent>((evnt) =>
                    {
                        MouseOverUI = true;
                    });

                    _root.RegisterCallback<PointerLeaveEvent>((evnt) =>
                    {
                        MouseOverUI = false;
                    });

                    CurrentState = this;

                    using (StartInspector(null, PegiPaintingMode.UI_Toolkit))
                    {
                       
                        ipegi.Inspect();
                        NewLine();
                    }

                    CurrentState = null;
#endif
                }
            }

#if USE_UI_TOOLKIT
            private static StyleSheet styleSheet;

            private static VisualElement _root;
            private static VisualElement horizontalContainer;
#endif
            public static bool MouseOverUI;



            public static void Start() 
            {
#if USE_UI_TOOLKIT

                if (horizontalContainer == null)
                {
                    horizontalContainer = new VisualElement();
                    horizontalContainer.style.flexDirection = FlexDirection.Row;
                }
#endif
            }

            public static void NewLine() 
            {
#if USE_UI_TOOLKIT

                if (horizontalContainer == null)
                    return;
                
                _root.Add(horizontalContainer);
                horizontalContainer = null;
#endif
            }

            internal static void Write(string text, string toolTip, int width, Styles.PegiGuiStyle style)
            {
#if USE_UI_TOOLKIT

                Start();
                Label label = new(text)
                {
                    tooltip = toolTip
                };
                label.style.width = width;
                //label.AddToClassList("red-label");
                label.style.color = style.Current.normal.textColor;///Color.white;
                label.style.fontSize = style.Current.fontSize;
                horizontalContainer.Add(label);
#endif
            }

            internal static void Write(string text, string toolTip, Styles.PegiGuiStyle style)
            {
#if USE_UI_TOOLKIT

                Start();
                Label label = new(text)
                {
                    tooltip = toolTip
                };
                //label.AddToClassList("red-label");
               // label.style.color = style.Current.normal.textColor;///Color.white;
                //label.style.fontSize = style.Current.fontSize;
                horizontalContainer.Add(label);
#endif
            }

            internal static void Write(string text, string toolTip)
            {
#if USE_UI_TOOLKIT

                Start();
                Label label = new(text)
                {
                    tooltip = toolTip
                };
                horizontalContainer.Add(label);
#endif
            }

            internal static ChangesToken Edit(TextLabel label, int current, Action<int> onValueChange)
            {
#if USE_UI_TOOLKIT

                label.Write();

                Start();
                IntegerField intField = new() { value = current };
                intField.RegisterValueChangedCallback(evt => onValueChange.Invoke(evt.newValue));
                horizontalContainer.Add(intField);
                return new ChangesToken(intField);
#endif
                return ChangesToken.False;
            }

            internal static ChangesToken Click(TextLabel text)
            {
#if USE_UI_TOOLKIT

                Start();
                Button button = new() { text = text.label };
                horizontalContainer.Add(button);
                return new ChangesToken(button);
#endif
                return ChangesToken.False;
            }

            internal static ChangesToken Click(Texture img, string toolTip, int size)
            {
#if USE_UI_TOOLKIT

                Start();
                Background iconImage;

                if (img is Texture2D texture)
                    iconImage = Background.FromTexture2D(texture);
                else
                    iconImage = Background.FromRenderTexture(img as RenderTexture);

                Button button = new(iconImage: iconImage)
                {
                    tooltip = toolTip
                };
                button.style.width = size;

                horizontalContainer.Add(button);

                return new ChangesToken(button);
#endif
                return ChangesToken.False;
            }

            /*
            internal static ChangesToken Edit_Big(ref string val, int height)
            {
                Start();
                Write(val, "big Text");
                return ChangesToken.False;
            }*/


                        #if USE_UI_TOOLKIT

            public static StyleSheet Styles 
            {
                get 
                {
                    if (!styleSheet)
                        styleSheet = UnityEngine.Resources.Load<StyleSheet>("qc-pegi-style");
                    return styleSheet;
                }
            }
#endif
        }
        
    }
}