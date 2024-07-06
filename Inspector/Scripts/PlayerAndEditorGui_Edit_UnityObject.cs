using QuizCanners.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Inspect
{
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0011 // Add braces
#pragma warning disable IDE0008 // Use explicit type

    public static partial class pegi
    {
        #region Audio Clip

        public static ChangesToken Edit(this TextLabel label,  ref AudioClip field)
        {
            label.FallbackWidthFraction = 0.25f;
            label.Write();
            return Edit(ref field);
        }

        public static ChangesToken Edit(ref AudioClip clip, int width)
        {

            var ret =
#if UNITY_EDITOR
                !PaintingGameViewUI ? PegiEditorOnly.Edit(ref clip, width) :
#endif
                    ChangesToken.False;

            clip.PlayButton();

            return ret;
        }

        public static ChangesToken Edit(ref AudioClip clip)
        {

            var ret =
#if UNITY_EDITOR
                !PaintingGameViewUI ? PegiEditorOnly.Edit(ref clip) :
#endif
                    ChangesToken.False;

            clip.PlayButton();

            return ret;
        }

        private static void PlayButton(this AudioClip clip)
        {
            if (clip && Icon.Play.Click(20))
            {
                //var req = 
                clip.Play();
                //if (offset > 0)
                //req.FromTimeOffset(offset);
            }
        }

        #endregion

        #region UnityObject

        public static ChangesToken RenameGameObject_Option(GameObject go, string name)
        {
            if (!go)
                return ChangesToken.False;

            if (go.name != name && Icon.Link.Click("Rename {0} to {1}".F(go.name, name)))
            {
                go.name = name;
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }

        public static ChangesToken RenameComponent_Option(Object uObj, string name)
        {
            if (uObj && uObj is Component)
            {
                var cmp = uObj as Component;
                RenameGameObject_Option(cmp.gameObject, name);
            }
            return ChangesToken.False;
        }

        public static ChangesToken Edit_Scene(ref string path, int width) =>
#if UNITY_EDITOR
                !PaintingGameViewUI ? PegiEditorOnly.Edit_Scene(ref path, width) :
#endif
            ChangesToken.False;

        public static ChangesToken Edit_Scene(ref string path) =>
#if UNITY_EDITOR
                !PaintingGameViewUI ? PegiEditorOnly.Edit_Scene(ref path) :
#endif
            ChangesToken.False;

        public static ChangesToken Edit_IfNull<T>(this TextLabel label, ref T so) where T : ScriptableObject
        {
            if (so)
                return ChangesToken.False;

            label.FallbackWidthFraction = 0.33f;

            label.Write();
            var res = Edit(ref so); 

            return res;
        }

        public static ChangesToken Edit_IfNull<T>(this TextLabel label, ref T component, GameObject parent, bool renameOnAttach = false) where T : Component
        {
            if (component)
                return ChangesToken.False;

            label.FallbackWidthFraction = 0.33f;

            label.Write();
            var res = Edit_IfNull(ref component, parent);

            if (renameOnAttach && res && component) 
            {
                component.gameObject.name = label.label;
            }
            //RenameGameObject_Option(component.gameObject, label.label);

            return res;
        }

        public static ChangesToken Edit_IfNull<T>(ref T component, GameObject parent) where T : Component
        {
            if (component)
                return ChangesToken.False;

            var changed = ChangeTrackStart();

            Edit(ref component);
            if (!component)
            {
                bool isGameObject = typeof(T) == typeof(Transform) || typeof(T) == typeof(GameObject);

                if (!isGameObject)
                {
                    if (Icon.Refresh.Click("Get Component()"))
                    {
                        component = parent.GetComponent<T>();
                        if (!component)
                            component = parent.GetComponentInChildren<T>();
                    }

                    if (typeof(T) != typeof(Transform) && typeof(T) != typeof(GameObject) && Icon.Add.Click("Add Component"))
                        component = parent.AddComponent<T>();
                }
            }
            else
            {
                if (Icon.Clear.Click())
                    component = null;
            }
            return changed;
        }

        public static ChangesToken Edit<T>(ref T field, int width, bool allowSceneObjects = true) where T : Object =>
#if UNITY_EDITOR
                !PaintingGameViewUI ? PegiEditorOnly.Edit(ref field, width, allowSceneObjects) :
#endif
            ChangesToken.False;



        public static ChangesToken Edit<T>(this TextLabel label, ref T field, bool allowSceneObjects = true, bool renameTarget_Option = false) where T : Object
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                Write(label, defaultWidthFraction: 0.33f);
                var result = Edit(ref field, allowSceneObjects);

                if (renameTarget_Option)
                    RenameComponent_Option(field, label.label);
                return result;
            }
#endif

            "{0} [{1}]".F(label, field ? field.name : "NULL").PegiLabel(toolTip: field.GetNameForInspector_Uobj()).Write();

            return ChangesToken.False;

        }

        public enum UnityObjectSource { Scene, Prefab }

        public static ChangesToken Edit<T>(ref T field, UnityObjectSource source) where T: Component
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                var before = field;
                if (PegiEditorOnly.Edit(ref field, allowSceneObjects: source == UnityObjectSource.Scene))
                {
                    if (field)
                    {
                        if (QcUnity.IsPartOfAPrefab(field.gameObject) != (source == UnityObjectSource.Prefab))
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

        public static ChangesToken Edit(ref GameObject field, UnityObjectSource source) 
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                var before = field;
                if (PegiEditorOnly.Edit(ref field, allowSceneObjects: source == UnityObjectSource.Scene)) 
                {
                    if (field) 
                    {
                        if (QcUnity.IsPartOfAPrefab(field) != (source == UnityObjectSource.Prefab)) 
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


        public static ChangesToken Edit<T>(ref T field, bool allowSceneObjects = true) where T : Object =>
#if UNITY_EDITOR
            !PaintingGameViewUI ? PegiEditorOnly.Edit(ref field, allowSceneObjects) :
#endif
                ChangesToken.False;

        public static ChangesToken Edit(ref Object field, System.Type type, bool allowSceneObjects = true) =>
#if UNITY_EDITOR
            !PaintingGameViewUI ? PegiEditorOnly.Edit(ref field, type, allowSceneObjects) :
#endif
                ChangesToken.False;

        public static ChangesToken Edit(ref Object field, System.Type type, int width, bool allowSceneObjects = true) =>
#if UNITY_EDITOR
                     !PaintingGameViewUI ? PegiEditorOnly.Edit(ref field, type, width, allowSceneObjects) :
#endif
                ChangesToken.False;


        public static ChangesToken Select_Enter_Inspect<T>(this TextLabel label, ref T obj, bool showLabelIfEntered = true) where T : Object
        {
            using (Context.IncrementDisposible(out bool canSkip))
            {
                if (canSkip)
                    return ChangesToken.False;

                var changed = ChangeTrackStart();

                if (!obj)
                {
                    Context.Internal_exitOptionOnly(label, showLabelIfTrue: showLabelIfEntered);
                    SelectInAssets(ref obj);
                }
                else
                {
                    var lst = obj as IPEGI_ListInspect;

                    if (lst != null)
                    {
                        Context.Internal_Enter_Inspect_AsList(lst, label.label);
                    }
                    else
                    {
                        if (Context.Internal_isEntered(label, showLabelIfEntered))
                        {
                            Nl();
                            var pgi = QcUnity.TryGetInterfaceFrom<IPEGI>(obj);
                            if (pgi != null)
                                pgi.Nested_Inspect();
                            else
                                TryDefaultInspect(obj as Object);
                        }
                        else
                        {
                            SelectInAssets(ref obj);
                        }
                    }

                    if (!Context.IsEnteredCurrent && Icon.Clear.ClickConfirm(confirmationTag: "Del " + label + obj.GetHashCode(), Msg.MakeElementNull.GetText()))
                        obj = null;
                }
                return changed;
            }
        }

        public static ChangesToken Edit_Enter_Inspect<T>(this TextLabel label, ref T obj, List<T> selectFrom = null, bool showLabelIfEntered = true) where T : Object
        {
            using (Context.IncrementDisposible(out bool canSkip))
            {
                if (canSkip)
                    return ChangesToken.False;

                var changed = ChangeTrackStart();

                if (!obj)
                {
                    Context.Internal_exitOptionOnly(label, showLabelIfTrue: showLabelIfEntered);

                    SelectOrEdit(ref obj);
                }
                else
                {
                    var lst = obj as IPEGI_ListInspect;

                    if (lst != null)
                    {
                        Context.Internal_Enter_Inspect_AsList(lst, label.label);
                    }
                    else
                    {
                        if (Context.Internal_isEntered(label, showLabelIfEntered))
                        {
                            Nl();
                            var pgi = QcUnity.TryGetInterfaceFrom<IPEGI>(obj);
                            if (pgi != null)
                                pgi.Nested_Inspect();
                            else
                                TryDefaultInspect(obj as Object);
                        }
                        else
                        {

                            SelectOrEdit(ref obj);
                            ClickHighlight(obj);
                        }
                    }

                    if (!Context.IsEnteredCurrent && Icon.Clear.ClickConfirm(confirmationTag: "Del " + label + obj.GetHashCode(), Msg.MakeElementNull.GetText()))
                        obj = null;
                }

                void SelectOrEdit(ref T obj) 
                {
                    if (!selectFrom.IsNullOrEmpty())
                        Select_or_edit(ref obj, selectFrom);
                    else
                        Edit(ref obj);
                }

                return changed;
            }
        }
        /*
        public static ChangesToken Edit_Enter_Inspect<T>(this TextLabel label, ref T obj, ref int entered, int thisOne, List<T> selectFrom = null, bool showLabelIfEntered = true) where T : Object
        {
            if (!EnterInternal.OptionsDrawn(ref entered, thisOne))
                return ChangesToken.False;

            var changed = ChangeTrackStart();

            if (!obj)
            {
                if (!selectFrom.IsNullOrEmpty())
                    label.Select_or_edit(ref obj, selectFrom);
                else
                    label.Edit(ref obj);
            }
            else
            {
                var lst = obj as IPEGI_ListInspect;

                if (lst != null)
                    lst.Enter_Inspect_AsList(ref entered, thisOne, label.label);
                else
                {
                    var pgi = QcUnity.TryGetInterfaceFrom<IPEGI>(obj);

                    if (EnterInternal.IsConditionally_Entered(label, pgi != null, ref entered, thisOne, showLabelIfEntered: showLabelIfEntered).Nl_ifEntered())
                        pgi.Nested_Inspect();
                    else
                        pegi.ClickHighlight(obj);
                }

                if ((entered == -1) && Icon.Clear.ClickConfirm(confirmationTag: "Del " + label + thisOne, Msg.MakeElementNull.GetText()))
                    obj = null;
            }

            return changed;
        }
        */
        public static ChangesToken Edit_Inspect<T>(this TextLabel label, ref T obj) where T : Object
        {
            var changed = ChangeTrackStart();

            if (!obj)
                 label.Edit(ref obj);
            else
                Try_Nested_Inspect(obj);
                
            return changed;
        }

        #endregion

        #region Material


        public static MaterialToken PegiToken(this Material mat) => new(mat);

        public class MaterialToken 
        {
            internal Material material;

            internal MaterialToken(Material mat) 
            {
                material = mat;
            }

            public ChangesToken Edit_Texture(string name) => Edit_Texture(name, name);

            public ChangesToken Edit_Texture(string name, string display)
            {
                var mat = material;
                display.PegiLabel().ApproxWidth().Write();
                var tex = mat.GetTexture(name);

                if (pegi.Edit(ref tex))
                {
                    mat.SetTexture(name, tex);
                    return ChangesToken.True;
                }

                return ChangesToken.False;
            }

            public ChangesToken Edit(string keyword)
            {
                var val = System.Array.IndexOf(material.shaderKeywords, keyword) != -1;

                if (!keyword.PegiLabel().ToggleIcon(ref val))
                    return ChangesToken.False;

                if (val)
                    material.EnableKeyword(keyword);
                else
                    material.DisableKeyword(keyword);

                return ChangesToken.True;
            }

            public ChangesToken Edit(ShaderProperty.FloatValue property, string name = null)
            {
                var val = material.Get(property);

                if (name.IsNullOrEmpty())
                    name = property.ToString();

                if (name.PegiLabel(name.Length * letterSizeInPixels).Edit(ref val))
                {
                    material.Set(property, val);
                    return ChangesToken.True;
                }

                return ChangesToken.False;
            }

            public ChangesToken Edit(ShaderProperty.FloatValue property, string name, float min, float max)
            {
                var mat = material;
                var val = mat.Get(property);

                if (name.IsNullOrEmpty())
                    name = property.ToString();

                if (name.PegiLabel().ApproxWidth().Edit(ref val, min, max))
                {
                    mat.Set(property, val);
                    return ChangesToken.True;
                }

                return ChangesToken.False;
            }

            public ChangesToken Edit(ShaderProperty.FloatValue property, float min, float max) =>
                Edit(property, property.ToString(), min, max);
            
            public ChangesToken Edit(ShaderProperty.ColorValue property, string name = null)
            {
                var mat = material;
                var val = mat.Get(property);

                if (name.IsNullOrEmpty())
                    name = property.ToString();

                if (name.PegiLabel(0.3f).Edit(ref val))
                {
                    mat.Set(property, val);
                    return ChangesToken.True;
                }

                return ChangesToken.False;
            }

            /*
            public ChangesToken Edit(ShaderProperty.VectorValue property, string name = null)
            {
                var mat = material;
                var val = mat.Get(property);

                if (name.IsNullOrEmpty())
                    name = property.ToString();

                if (name.PegiLabel().Edit(ref val))
                {
                    mat.Set(property, val);
                    return ChangesToken.True;
                }

                return ChangesToken.False;
            }*/

            public ChangesToken Edit(ShaderProperty.TextureValue property, string name = null)
            {
                var mat = material;
                var val = mat.Get(property);

                if (name.IsNullOrEmpty())
                    name = property.ToString();

                if (name.PegiLabel(name.Length * letterSizeInPixels).Edit(ref val))
                {
                    mat.Set(property, val);
                    return ChangesToken.True;
                }

                return ChangesToken.False;
            }

            public ChangesToken Edit_Enum(ShaderProperty.KeywordEnum property)
            {
                var changes = ChangeTrackStart();

                int val = Mathf.RoundToInt(material.Get(property));

                if (property.ToString().PegiLabel(0.3f).Select(ref val, property.Keywords))
                {
                    material.Set(property, val);
                }

                return changes;
            }


            public ChangesToken Toggle(ShaderProperty.MaterialToggle property, string name = null)
            {
                var val = property.Get(material);

                if (name.IsNullOrEmpty())
                    name = property.ToString();

                if (name.PegiLabel().ToggleIcon(ref val))
                {
                    property.SetOn(material, val);

                    return ChangesToken.True;
                }

                return ChangesToken.False;
            }

            /*
            public ChangesToken Toggle(ShaderProperty.FloatValue property, string name = null)
            {
                var val = material.Get(property) > 0;

                if (name.IsNullOrEmpty())
                    name = property.ToString();

                if (name.PegiLabel().ToggleIcon(ref val))
                {
                    material.Set(property, val ? 1 : 0);
                    return ChangesToken.True;
                }

                return ChangesToken.False;
            }*/


        }

        #endregion
    }
}