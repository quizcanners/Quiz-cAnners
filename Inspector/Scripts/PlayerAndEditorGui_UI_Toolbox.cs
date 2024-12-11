using QuizCanners.Utils;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace QuizCanners.Inspect
{
    public static partial class pegi
    {
        public static class Toolkit 
        {
            internal static State CurrentState;

            [Serializable]
            public class State
            {
                private readonly Gate.Integer documentVersion = new();
                [SerializeField] private UIDocument _document;
                [SerializeField] private MonoBehaviour _target;

                private int _dataVersion;
                private IPEGI ipegi;

                public void SetDirty() => _dataVersion++;

                public void Clear()
                {
                    if (_document != null && _document.rootVisualElement != null)
                        _document.rootVisualElement.Clear();

                    documentVersion.ValueIsDefined = false;
                }

                public void LateUpdate()
                {
                    if (!documentVersion.TryChange(_dataVersion))
                        return;

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
                }
            }

            private static StyleSheet styleSheet;

            private static VisualElement _root;
            private static VisualElement horizontalContainer;

            public static bool MouseOverUI;



            public static void Start() 
            {
                if (horizontalContainer == null)
                {
                    horizontalContainer = new VisualElement();
                    horizontalContainer.style.flexDirection = FlexDirection.Row;
                }
            }

            public static void NewLine() 
            {
                if (horizontalContainer == null)
                    return;
                
                _root.Add(horizontalContainer);
                horizontalContainer = null;
            }

            internal static void Write(string text, string toolTip, int width, Styles.PegiGuiStyle style)
            {
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
            }

            internal static void Write(string text, string toolTip, Styles.PegiGuiStyle style)
            {
                Start();
                Label label = new(text)
                {
                    tooltip = toolTip
                };
                //label.AddToClassList("red-label");
               // label.style.color = style.Current.normal.textColor;///Color.white;
                //label.style.fontSize = style.Current.fontSize;
                horizontalContainer.Add(label);
            }

            internal static void Write(string text, string toolTip)
            {
                Start();
                Label label = new(text)
                {
                    tooltip = toolTip
                };
                horizontalContainer.Add(label);
            }

            internal static ChangesToken Edit(TextLabel label, int current, Action<int> onValueChange)
            {
                label.Write();

                Start();
                IntegerField intField = new() { value = current };
                intField.RegisterValueChangedCallback(evt => onValueChange.Invoke(evt.newValue));
                horizontalContainer.Add(intField);
                return new ChangesToken(intField);
            }

            internal static ChangesToken Click(TextLabel text)
            {
                Start();
                Button button = new() { text = text.label };
                horizontalContainer.Add(button);
                return new ChangesToken(button);
            }

            internal static ChangesToken Click(Texture img, string toolTip, int size)
            {
                Start();
                Background iconImage;

                if (img is Texture2D texture)
                    iconImage = Background.FromTexture2D(texture);
                else
                    iconImage = Background.FromRenderTexture(img as RenderTexture);

                Button button = new()
                {
                    tooltip = toolTip,
                };
                button.style.width = size;

                horizontalContainer.Add(button);

                return new ChangesToken(button);
            }

            internal static ChangesToken Edit_Big(ref string val, int height)
            {
                Start();
                Write(val, "big Text");
                return ChangesToken.False;
            }

            public static StyleSheet Styles 
            {
                get 
                {
                    if (!styleSheet)
                        styleSheet = UnityEngine.Resources.Load<StyleSheet>("qc-pegi-style");
                    return styleSheet;
                }
            }
        }
    }
}