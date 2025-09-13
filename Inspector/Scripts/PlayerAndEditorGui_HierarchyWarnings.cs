#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using QuizCanners.Utils;

namespace QuizCanners.Inspect
{
    [InitializeOnLoad]
    public class PlayerAndEditorGui_HierarchyWarnings
    {
        static PlayerAndEditorGui_HierarchyWarnings()
        {
            EditorApplication.hierarchyWindowItemOnGUI += HierarchyItemCB;
        }

        static void HierarchyItemCB(int instanceID, Rect selectionRect)
        {
            var inst = EditorUtility.InstanceIDToObject(instanceID);

            if (!inst)
                return;

            GameObject go = inst as GameObject;

            if (!go)
                return;

            var na = go.GetComponents<INeedAttention>();
            string msg = "";

            foreach (var a in na)
            {
                if (a.TryGetAttentionMessage(out var currentMessage))
                    msg += currentMessage;
            }

            if (msg.IsNullOrEmpty())
                return;

            Rect r = new Rect(selectionRect)
            {
                x = 30,
                width = 18
            };

            GUIContent cnt = new GUIContent(Icon.Warning.GetIcon().texture, msg);
            GUI.Label(r, cnt);
        }
    }
}
#endif