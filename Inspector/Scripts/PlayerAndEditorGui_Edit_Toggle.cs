using QuizCanners.Utils;
using UnityEngine;

namespace QuizCanners.Inspect
{
    public static partial class pegi
    {
        private const int DEFAULT_TOGGLE_BUTTON_SIZE = 30;

        public static ChangesToken ToggleInt(ref int val)
        {
            var before = val > 0;
            if (Toggle(ref before))
            {
                val = before ? 1 : 0;
                return ChangesToken.True;
            }
            return ChangesToken.False;
        }

        public static ChangesToken Toggle(ref bool val)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Toggle(ref val);
#endif

            _START();
            val = GUILayout.Toggle(val, "", GUILayout.MaxWidth(30));
            return _END();

        }

        private static ChangesToken Toggle(ref bool val, Icon TrueIcon, Icon FalseIcon, string tip, int width, Styles.PegiGuiStyle style)
            => Toggle(ref val, TrueIcon.GetIcon(), FalseIcon.GetIcon(), tip, width, style.Current);

        public static ChangesToken Toggle(ref bool val, Icon TrueIcon, Icon FalseIcon, string tip, int width = DEFAULT_BUTTON_SIZE)
            => Toggle(ref val, TrueIcon.GetIcon(), FalseIcon.GetIcon(), tip, width);

        public static ChangesToken Toggle(ref bool val, Icon TrueIcon, Icon FalseIcon, GUIStyle style = null) => Toggle(ref val, TrueIcon.GetIcon(), FalseIcon.GetIcon(), "", DEFAULT_BUTTON_SIZE, style);

        public static ChangesToken ToggleIcon(ref bool val, string toolTip = "Toggle On/Off")
        {
            using (SetBgColorDisposable(Color.clear))
            {
                return Toggle(ref val, Icon.True, Icon.False, toolTip, DEFAULT_TOGGLE_BUTTON_SIZE, Styles.ToggleButton);
            }
        }
    
        public static ChangesToken ToggleIcon(this TextLabel label, ref bool val, bool hideTextWhenTrue = false)
        {
            var changed = ChangeTrackStart();

            using (SetBgColorDisposable(Color.clear))
            {
               Toggle(ref val, Icon.True, Icon.False, label.TooltipOrLabel, DEFAULT_TOGGLE_BUTTON_SIZE, Styles.ToggleButton);
            }
            if ((!val || !hideTextWhenTrue))
            {
                label.style = Styles.ToggleLabel(val);
                if (label.ClickLabel())
                {
                    val = !val;
                }
            }

            return changed;
        }

        public static ChangesToken ToggleIconConfirm(this TextLabel label, ref bool val, string confirmationTag, string tip = null, bool hideTextWhenTrue = false)
        {
            var changed = ChangeTrackStart();
            using (SetBgColorDisposable(Color.clear))
            {
                if ((val ? Icon.True : Icon.False).ClickConfirm(confirmationTag: confirmationTag, toolTip: tip, DEFAULT_TOGGLE_BUTTON_SIZE))
                    val = !val;
            }

            if (!ConfirmationDialogue.IsRequestedFor(confirmationTag) && (!val || !hideTextWhenTrue))
            {
                label.style = Styles.ToggleLabel(val);
                if (label.ClickLabelConfirm(confirmationTag: confirmationTag))
                    val = !val;
            }

            return changed;
        }

        private static ChangesToken Toggle(ref bool val, Texture2D TrueIcon, Texture2D FalseIcon, string tip, int width = DEFAULT_BUTTON_SIZE, GUIStyle style = null)
        {
            if (ClickImage(ImageAndTip(val ? TrueIcon : FalseIcon, tip), width, style))
            {
                val = !val;
                return ChangesToken.True;
            }
            return ChangesToken.False;
        }

        public static ChangesToken Toggle(this Texture img, ref bool val)
        {
            Draw(img, 25);
            return Toggle(ref val);
        }

        public static ChangesToken ToggleInt(this TextLabel text, ref int val)
        {
            Write(text);
            return ToggleInt(ref val);
        }

        public static ChangesToken Toggle(this TextLabel text, ref bool val)
        {
            Write(text);
            return Toggle(ref val);
        }

        public static ChangesToken Toggle_CompileDirective(string text, string keyword)
        {
    
#if UNITY_EDITOR
            var val = QcUnity.GetPlatformDirective(keyword);

            if (text.PegiLabel().ToggleIconConfirm(ref val, confirmationTag: keyword, tip: "Changing Compile directive will force scripts to recompile. {0} {1}? ".F(val ? "Disable" : "Enable", keyword)))
                QcUnity.SetPlatformDirective(keyword, val);
#endif

            return ChangesToken.False;
        }

        public static ChangesToken Toggle_CompileDirective(string text, string keyword, bool expectedValue)
        {
#if UNITY_EDITOR
            var val = QcUnity.GetPlatformDirective(keyword);

            (expectedValue == val ? Icon.Done : Icon.Warning).Draw();

            if (text.PegiLabel().ToggleIconConfirm(ref val, confirmationTag: keyword, tip: "Changing Compile directive will force scripts to recompile. {0} {1}? ".F(val ? "Disable" : "Enable", keyword)))
                QcUnity.SetPlatformDirective(keyword, val);
#endif

            return ChangesToken.False;
        }

        public static bool Toggle_DefaultInspector(Object target)
        {
#if UNITY_EDITOR

            if (!PaintingGameViewUI)
                return PegiEditorOnly.ToggleDefaultInspector(target);
#endif

            return ChangesToken.False;
        }



    }
}
