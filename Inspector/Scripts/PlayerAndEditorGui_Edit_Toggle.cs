using QuizCanners.Utils;
using UnityEngine;

namespace QuizCanners.Inspect
{

#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable IDE0011 // Add braces
#pragma warning disable IDE0008 // Use explicit type

    public static partial class pegi
    {
        private const int DefaultToggleIconSize = 34;

        public static ChangesToken toggleInt(ref int val)
        {
            var before = val > 0;
            if (toggle(ref before))
            {
                val = before ? 1 : 0;
                return ChangesToken.True;
            }
            return ChangesToken.False;
        }

        public static ChangesToken toggle(ref bool val)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.toggle(ref val);
#endif

            _START();
            val = GUILayout.Toggle(val, "", GUILayout.MaxWidth(30));
            return _END();

        }

        private static ChangesToken toggle(ref bool val, icon TrueIcon, icon FalseIcon, string tip, int width, Styles.PegiGuiStyle style)
            => toggle(ref val, TrueIcon.GetIcon(), FalseIcon.GetIcon(), tip, width, style.Current);

        public static ChangesToken toggle(ref bool val, icon TrueIcon, icon FalseIcon, string tip, int width = defaultButtonSize)
            => toggle(ref val, TrueIcon.GetIcon(), FalseIcon.GetIcon(), tip, width);

        public static ChangesToken toggle(ref bool val, icon TrueIcon, icon FalseIcon, GUIStyle style = null) => toggle(ref val, TrueIcon.GetIcon(), FalseIcon.GetIcon(), "", defaultButtonSize, style);

        public static ChangesToken toggleIcon(ref bool val, string toolTip = "Toggle On/Off")
        {
            using (SetBgColorDisposable(Color.clear))
            {
                return toggle(ref val, icon.True, icon.False, toolTip, DefaultToggleIconSize, Styles.ToggleButton);
            }
        }
    
        public static ChangesToken toggleIcon(this TextLabel label, ref bool val, bool hideTextWhenTrue = false)
        {
            var changed = ChangeTrackStart();

            using (SetBgColorDisposable(Color.clear))
            {
               toggle(ref val, icon.True, icon.False, label.TooltipOrLabel, DefaultToggleIconSize, Styles.ToggleButton);
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

        public static ChangesToken toggleIconConfirm(this TextLabel label, ref bool val, string confirmationTag, string tip = null, bool hideTextWhenTrue = false)
        {
            var changed = ChangeTrackStart();
            using (SetBgColorDisposable(Color.clear))
            {
                if ((val ? icon.True : icon.False).ClickConfirm(confirmationTag: confirmationTag, toolTip: tip, DefaultToggleIconSize))
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


        private static ChangesToken toggle(ref bool val, Texture2D TrueIcon, Texture2D FalseIcon, string tip, int width = defaultButtonSize, GUIStyle style = null)
        {
            if (ClickImage(ImageAndTip(val ? TrueIcon : FalseIcon, tip), width, style))
            {
                val = !val;
                return ChangesToken.True;
            }
            return ChangesToken.False;
        }

    
        public static ChangesToken toggle(this Texture img, ref bool val)
        {
            draw(img, 25);
            return toggle(ref val);
        }

        public static ChangesToken toggleInt(this TextLabel text, ref int val)
        {
            write(text);
            return toggleInt(ref val);
        }

        public static ChangesToken toggle(this TextLabel text, ref bool val)
        {
            write(text);
            return toggle(ref val);
        }


        public static ChangesToken toggle_CompileDirective(string text, string keyword)
        {
    
#if UNITY_EDITOR
            var val = QcUnity.GetPlatformDirective(keyword);

            if (text.PegiLabel().toggleIconConfirm(ref val, confirmationTag: keyword, tip: "Changing Compile directive will force scripts to recompile. {0} {1}? ".F(val ? "Disable" : "Enable", keyword)))
                QcUnity.SetPlatformDirective(keyword, val);
#endif

            return ChangesToken.False;
        }

        public static bool toggleDefaultInspector(Object target)
        {
#if UNITY_EDITOR

            if (!PaintingGameViewUI)
                return PegiEditorOnly.toggleDefaultInspector(target);
#endif

            return ChangesToken.False;
        }



    }
}
