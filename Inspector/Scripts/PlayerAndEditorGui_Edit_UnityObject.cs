using QuizCanners.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Inspect
{
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0011 // Add braces
#pragma warning disable IDE0008 // Use explicit type

    public static partial class pegi
    {
        #region Audio Clip

        public static ChangesToken edit(this TextLabel label,  ref AudioClip field)
        {
            label.write();
            return edit(ref field);
        }

        public static ChangesToken edit(ref AudioClip clip, int width)
        {

            var ret =
#if UNITY_EDITOR
                !PaintingGameViewUI ? PegiEditorOnly.edit(ref clip, width) :
#endif
                    ChangesToken.False;

            clip.PlayButton();

            return ret;
        }

        public static ChangesToken edit(ref AudioClip clip)
        {

            var ret =
#if UNITY_EDITOR
                !PaintingGameViewUI ? PegiEditorOnly.edit(ref clip) :
#endif
                    ChangesToken.False;

            clip.PlayButton();

            return ret;
        }

        private static void PlayButton(this AudioClip clip)
        {
            if (clip && icon.Play.Click(20))
            {
                //var req = 
                clip.Play();
                //if (offset > 0)
                //req.FromTimeOffset(offset);
            }
        }

        #endregion

        #region UnityObject

        public static ChangesToken edit_Scene(ref string path, int width) =>
#if UNITY_EDITOR
                !PaintingGameViewUI ? PegiEditorOnly.edit_Scene(ref path, width) :
#endif
            ChangesToken.False;

        public static ChangesToken edit_Scene(ref string path) =>
#if UNITY_EDITOR
                !PaintingGameViewUI ? PegiEditorOnly.edit_Scene(ref path) :
#endif
            ChangesToken.False;

        public static ChangesToken edit_ifNull<T>(this TextLabel label, ref T component, GameObject parent) where T : Component
        {
            if (component)
                return ChangesToken.False;

            label.write();
            return edit_ifNull(ref component, parent);
        }

        public static ChangesToken edit_ifNull<T>(ref T component, GameObject parent) where T : Component
        {
            if (component)
                return ChangesToken.False;

            var changed = ChangeTrackStart();

            pegi.edit(ref component);
            if (icon.Refresh.Click("Get Component()"))
                component = parent.GetComponent<T>();
            if (icon.Add.Click("Add Component"))
                component = parent.AddComponent<T>();

            return changed;
        }

        public static ChangesToken edit<T>(ref T field, int width, bool allowSceneObjects = true) where T : Object =>
#if UNITY_EDITOR
                !PaintingGameViewUI ? PegiEditorOnly.edit(ref field, width, allowSceneObjects) :
#endif
            ChangesToken.False;

        public static ChangesToken edit<T>(this TextLabel label, ref T field, bool allowSceneObjects = true) where T : Object
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                write(label);
                return edit(ref field, allowSceneObjects);
            }
#endif

            "{0} [{1}]".F(label, field ? field.name : "NULL").PegiLabel(toolTip: field.GetNameForInspector_Uobj()).write();

            return ChangesToken.False;

        }

        public enum UnityObjectSource { Scene, Prefab }

        public static ChangesToken edit<T>(ref T field, UnityObjectSource source) where T: Component
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                var before = field;
                if (PegiEditorOnly.edit(ref field, allowSceneObjects: source == UnityObjectSource.Scene))
                {
                    if (field)
                    {
                        if (QcUnity.IsPrefab(field.gameObject) != (source == UnityObjectSource.Prefab))
                        {
                            Debug.LogWarning("The field expects {0} GameObject, {1} is not {0} GameObject".F(source, field.gameObject.name), field.gameObject);
                            field = before;
                            return ChangesToken.False;
                        }
                    }
                    return ChangesToken.True;
                }
                return ChangesToken.False;
            }
#endif
            return ChangesToken.False;
        }

        public static ChangesToken edit(ref GameObject field, UnityObjectSource source) 
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                var before = field;
                if (PegiEditorOnly.edit(ref field, allowSceneObjects: source == UnityObjectSource.Scene)) 
                {
                    if (field) 
                    {
                        if (QcUnity.IsPrefab(field) != (source == UnityObjectSource.Prefab)) 
                        {
                            Debug.LogWarning("The field expects {0} GameObject, {1} is not {0} GameObject".F(source, field), field);
                            field = before;
                            return ChangesToken.False;
                        }
                    }
                    return ChangesToken.True;
                }
                return ChangesToken.False;
            }
#endif
            return ChangesToken.False;
        }


        public static ChangesToken edit<T>(ref T field, bool allowSceneObjects = true) where T : Object =>
#if UNITY_EDITOR
            !PaintingGameViewUI ? PegiEditorOnly.edit(ref field, allowSceneObjects) :
#endif
                ChangesToken.False;

        public static ChangesToken edit(ref Object field, System.Type type, bool allowSceneObjects = true) =>
#if UNITY_EDITOR
            !PaintingGameViewUI ? PegiEditorOnly.edit(ref field, type, allowSceneObjects) :
#endif
                ChangesToken.False;

        public static ChangesToken edit(ref Object field, System.Type type, int width, bool allowSceneObjects = true) =>
#if UNITY_EDITOR
                     !PaintingGameViewUI ? PegiEditorOnly.edit(ref field, type, width, allowSceneObjects) :
#endif
                ChangesToken.False;

        public static ChangesToken edit_enter_Inspect<T>(this TextLabel label, ref T obj, List<T> selectFrom = null, bool showLabelIfEntered = true) where T : Object
        {
            using (Context.IncrementDisposible(out bool canSkip))
            {
                if (canSkip)
                    return ChangesToken.False;

                var changed = ChangeTrackStart();

                if (!obj)
                {
                    if (!selectFrom.IsNullOrEmpty())
                        label.select_or_edit(ref obj, selectFrom);
                    else
                        label.edit(ref obj);
                }
                else
                {
                    var lst = obj as IPEGI_ListInspect;

                    if (lst != null)
                    {
                        Context.Internal_enter_Inspect_AsList(lst, label.label);
                    }
                    else
                    {
                        var pgi = QcUnity.TryGetInterfaceFrom<IPEGI>(obj);

                        if (Context.Internal_isConditionally_Entered(label, pgi != null, showLabelIfTrue: showLabelIfEntered))
                        {
                            nl();
                            pgi.Nested_Inspect();
                        }
                        else
                            obj.ClickHighlight();
                    }

                    if (!Context.IsEnteredCurrent && icon.Clear.ClickConfirm(confirmationTag: "Del " + label + obj.GetHashCode(), Msg.MakeElementNull.GetText()))
                        obj = null;
                }
                return changed;
            }
        }

        public static ChangesToken edit_enter_Inspect<T>(this TextLabel label, ref T obj, ref int entered, int thisOne, List<T> selectFrom = null, bool showLabelIfEntered = true) where T : Object
        {
            if (!EnterOptionsDrawn_Internal(ref entered, thisOne))
                return ChangesToken.False;

            var changed = ChangeTrackStart();

            if (!obj)
            {
                if (!selectFrom.IsNullOrEmpty())
                    label.select_or_edit(ref obj, selectFrom);
                else
                    label.edit(ref obj);
            }
            else
            {
                var lst = obj as IPEGI_ListInspect;

                if (lst != null)
                    lst.enter_Inspect_AsList(ref entered, thisOne, label.label);
                else
                {
                    var pgi = QcUnity.TryGetInterfaceFrom<IPEGI>(obj);

                    if (label.isConditionally_Entered(pgi != null, ref entered, thisOne, showLabelIfEntered: showLabelIfEntered).nl_ifEntered())
                        pgi.Nested_Inspect();
                    else
                        obj.ClickHighlight();
                }

                if ((entered == -1) && icon.Clear.ClickConfirm(confirmationTag: "Del " + label + thisOne, Msg.MakeElementNull.GetText()))
                    obj = null;
            }

            return changed;
        }

        #endregion

        #region Material

        public static ChangesToken editTexture(this Material mat, string name) => mat.editTexture(name, name);

        public static ChangesToken editTexture(this Material mat, string name, string display)
        {

            display.PegiLabel().ApproxWidth().write();
            var tex = mat.GetTexture(name);

            if (edit(ref tex))
            {
                mat.SetTexture(name, tex);
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }

        public static ChangesToken toggle(this Material mat, string keyword)
        {
            var val = System.Array.IndexOf(mat.shaderKeywords, keyword) != -1;

            if (!keyword.PegiLabel().toggleIcon(ref val)) 
                return ChangesToken.False;

            if (val)
                mat.EnableKeyword(keyword);
            else
                mat.DisableKeyword(keyword);

            return ChangesToken.True;
        }

        public static ChangesToken edit(this Material mat, ShaderProperty.FloatValue property, string name = null)
        {
            var val = mat.Get(property);

            if (name.IsNullOrEmpty())
                name = property.GetReadOnlyName();

            if (name.PegiLabel(name.Length * letterSizeInPixels).edit(ref val))
            {
                mat.Set(property, val);
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }

        public static ChangesToken edit(this Material mat, ShaderProperty.FloatValue property, string name, float min, float max)
        {
            var val = mat.Get(property);

            if (name.IsNullOrEmpty())
                name = property.GetReadOnlyName();

            if (name.PegiLabel().ApproxWidth().edit(ref val, min, max))
            {
                mat.Set(property, val);
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }

        public static ChangesToken edit(this Material mat, ShaderProperty.ColorFloat4Value property, string name = null)
        {
            var val = mat.Get(property);

            if (name.IsNullOrEmpty())
                name = property.GetReadOnlyName();

            if (name.PegiLabel(width: name.Length * letterSizeInPixels).edit(ref val))
            {
                mat.Set(property, val);
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }

        public static ChangesToken edit(this Material mat, ShaderProperty.VectorValue property, string name = null)
        {
            var val = mat.Get(property);

            if (name.IsNullOrEmpty())
                name = property.GetReadOnlyName();

            if (name.PegiLabel().edit(ref val))
            {
                mat.Set(property, val);
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }

        public static ChangesToken edit(this Material mat, ShaderProperty.TextureValue property, string name = null)
        {
            var val = mat.Get(property);

            if (name.IsNullOrEmpty())
                name = property.GetReadOnlyName();

            if (name.PegiLabel(name.Length * letterSizeInPixels).edit(ref val))
            {
                mat.Set(property, val);
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }

        #endregion

    }
}