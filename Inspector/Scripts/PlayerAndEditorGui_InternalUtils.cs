using UnityEngine;

namespace QuizCanners.Inspect
{
    public static partial class pegi
    {
        private static class Utils 
        {
            public static GUILayoutOption GuiMaxWidthOptionFrom(GUIContent cnt, pegi.Styles.PegiGuiStyle style) => GUILayout.MaxWidth(Mathf.Min(PLAYTIME_GUI_WIDTH, ApproximateLength(cnt.text, style.Current.fontSize)));
            public static GUILayoutOption GuiMaxWidthOptionFrom(string txt, GUIStyle style) => GUILayout.MaxWidth(Mathf.Min(PLAYTIME_GUI_WIDTH, ApproximateLength(txt, style.fontSize)));

            public static int GuiMaxWidthFrom(string text) => Mathf.Min(PLAYTIME_GUI_WIDTH, ApproximateLength(text));

            public static GUILayoutOption GuiMaxWidthOption => GUILayout.MaxWidth(PLAYTIME_GUI_WIDTH);
            public static GUILayoutOption GuiMaxWidthOptionFrom(string text) =>
                GUILayout.MaxWidth(GuiMaxWidthFrom(text));

            internal static bool IsMonoType<T>(System.Collections.Generic.IList<T> list, int i)
            {
                if (!(typeof(MonoBehaviour)).IsAssignableFrom(typeof(T))) return false;

                GameObject mb = null;
                if (Edit(ref mb))
                {
#pragma warning disable UNT0014 // Invalid type for call to GetComponent
                    list[i] = mb.GetComponent<T>();
#pragma warning restore UNT0014 // Invalid type for call to GetComponent
                    if (list[i] == null) GameView.ShowNotification(typeof(T) + " Component not found");
                }
                return true;
            }

            public static int ApproximateLength(string label, int fontSize = -1)
            {
                if (label == null || label.Length == 0)
                    return 1;

                if (fontSize <= 1)
                    fontSize = letterSizeInPixels;

                int length = fontSize * label.Length;

                if (PaintingGameViewUI && length > PLAYTIME_GUI_WIDTH)
                    return PLAYTIME_GUI_WIDTH;

                int count = 0;
                for (int i = 0; i < label.Length; i++)
                {
                    if (char.IsUpper(label[i])) count++;
                }

                length += (int)(count * fontSize * 0.5f);

                return Mathf.Max(30, length);
            }
        }


    }
}