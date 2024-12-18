﻿#if UNITY_EDITOR
    using UnityEditor;
    using System.Linq.Expressions;
    using Type = System.Type;
    using ReorderableList = UnityEditorInternal.ReorderableList;
    using SceneManager = UnityEngine.SceneManagement.SceneManager;
#endif

using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using QuizCanners.Utils;
using UnityEngine;

namespace QuizCanners.Inspect
{

    using static pegi;

    internal static partial class PegiEditorOnly {

      

#if UNITY_EDITOR

        internal static void OnEndInspector() 
        {
            if (!Application.isPlaying)
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            if (inspectedUnityObject)
            {
                ClearFromPooledSerializedObjects(inspectedUnityObject);
                EditorUtility.SetDirty(inspectedUnityObject);
            }
        }

        public static SerializedObject SerObj;
        private static Editor _editor;
        private static PEGI_Inspector_Material _materialEditor;
        public static Object DrawDefaultInspector;

        private static Rect GetRect(int height = 50)
        {
            var rect = GUILayoutUtility.GetRect(1, height, GUILayout.ExpandWidth(true));
            if (rect.width <= 0)
                rect.width = 60;
            return rect;
        }
        public static void RepaintEditor() {
            if (_editor)
                _editor.Repaint();
            _materialEditor?.unityMaterialEditor.Repaint();
        }

        public static void Inspect_MB(Editor editor) 
        {
            _editor = editor;

            MonoBehaviour o = editor.target as MonoBehaviour;
            var so = editor.serializedObject;
            inspectedTarget = editor.target;

            var go = o.gameObject;


            bool paintedPegi = false;

            if (o is IPEGI pgi)
            {
               
                SerObj = so;
                if (!FullWindow.ShowingPopup())
                {
                    Toggle_DefaultInspector(o);

                    var change = ChangeTrackStart();

                    try
                    {
                        pgi.Inspect();
                    }
                    catch (Exception ex)
                    {
                        if (IsExitGUIException(ex))
                            throw ex;
                        else
                            Write_Exception(ex);
                    }

                    try
                    {
                        Nested_Inspect_Attention_MessageOnly(pgi);
                    }
                    catch (Exception ex)
                    {
                        Write_Exception(ex);
                    }

                    if (change)
                        ClearFromPooledSerializedObjects(o);

                    if (GlobChanged && o)
                        PrefabUtility.RecordPrefabInstancePropertyModifications(o);
                }
                
                Nl();
                paintedPegi = true;
            }

            if (!paintedPegi)
            {
                EditorGUI.BeginChangeCheck();
                editor.DrawDefaultInspector();
                if (EditorGUI.EndChangeCheck())
                {
                    GlobChanged = true;
                }
            }

            if (GlobChanged)
            {
                if (!Application.isPlaying)
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go ? go.scene : SceneManager.GetActiveScene());

                EditorUtility.SetDirty(o);
                EditorUtility.SetDirty(go);
                Undo.RecordObject(o, "PEGI Editor modifications");
            }
        }

        public static void Inspect_SO(Editor editor) 
        {
            _editor = editor;

            var scrObj = editor.target as ScriptableObject;

            if (!scrObj)
            {
                "Target is not Scriptable Object. Check your PEGI_Inspector_OS.".PL().WriteWarning();
                editor.DrawDefaultInspector();
                return;
            }

            var so = editor.serializedObject;
            inspectedTarget = editor.target;

            if (scrObj is IPEGI pgi)
            {
                if (FullWindow.ShowingPopup())
                    return;

                SerObj = so;
                Toggle_DefaultInspector(scrObj);

                try
                {
                    pgi.Inspect();
                }
                catch (Exception ex)
                {
                    Write_Exception(ex);
                }

                try
                {
                    Nested_Inspect_Attention_MessageOnly(pgi);
                }
                catch (Exception ex)
                {
                    Write_Exception(ex);
                }

                return;
            }

            EditorGUI.BeginChangeCheck();
            editor.DrawDefaultInspector();
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(scrObj);
            }
        }

        public static ChangesToken Inspect_Material(PEGI_Inspector_Material editor) {
            
            _materialEditor = editor;
            var mat = editor.unityMaterialEditor.target as Material;
            var changed = false;

            using (StartInspector(editor.unityMaterialEditor.target, PegiPaintingMode.EditorInspector))
            {
                changed = !FullWindow.ShowingPopup() && editor.Inspect(mat);
            }

            return new ChangesToken(GlobChanged || changed);
        }
        
        public static bool ToggleDefaultInspector(Object target)
        {
            var changed = false;

            const string BACK_TO_CUSTOM = "Back to Custom Inspector";
            const int ICON_SIZE_FOR_DEFAULT = 30;
            const int ICON_SIZE_FOR_CUSTOM = 20;

            if (target is Material)
            {
                pegi.Toggle(ref PEGI_Inspector_Material.drawDefaultInspector, Icon.Exit, Icon.Debug,
                    "Toggle Between regular and PEGI Material inspector", PEGI_Inspector_Material.drawDefaultInspector ? ICON_SIZE_FOR_DEFAULT : ICON_SIZE_FOR_CUSTOM);

                if (PEGI_Inspector_Material.drawDefaultInspector &&
                    BACK_TO_CUSTOM.PL(style: Styles.Text.ExitLabel).ClickLabel().Nl())
                    PEGI_Inspector_Material.drawDefaultInspector = false;
            }
            else
            {

                if (target == inspectedUnityObject)
                {
                    bool isDefault = target == DrawDefaultInspector;

                    if (pegi.Toggle(ref isDefault, Icon.Exit, Icon.Debug,
                        "Toggle Between regular and PEGI inspector", isDefault ? ICON_SIZE_FOR_DEFAULT : ICON_SIZE_FOR_CUSTOM))
                        DrawDefaultInspector = isDefault ? target : null;

                    if (isDefault && BACK_TO_CUSTOM.PL(style: Styles.Text.ExitLabel).ClickLabel().Nl())
                        DrawDefaultInspector = null;
                }
                else
                {
                    pegi.ClickHighlight(target);
                }
            }

            return changed;
        }

        public static Object ClearFromPooledSerializedObjects(Object obj)
        {
            if (obj && SerializedObjects.ContainsKey(obj))
                SerializedObjects.Remove(obj);

            return obj;
        }

        private static ChangesToken FeedChanges_Internal(this bool result) 
        { 
            GlobChanged |= result;
            return new ChangesToken(result); 
        }

        private static ChangesToken FeedChanged() { GlobChanged = true; return ChangesToken.True; }

        private static void START() 
        {
            CheckLine_Editor();
            EditorGUI.BeginChangeCheck(); 
        }

        private static ChangesToken END()
        {
            var val = EditorGUI.EndChangeCheck();
            GlobChanged |= val;
            return new ChangesToken(val);
        }

        internal static void CheckLine_Editor()
        {
            if (_horizontalStarted) 
                return;

            if (nextBgStyle.TryGetLast(out var style))
                EditorGUILayout.BeginHorizontal(style.Get());
            else
                EditorGUILayout.BeginHorizontal();
            
            _horizontalStarted = true;

        }

        internal static void NewLine_Editor()
        {
            if (!_horizontalStarted) 
                return;

            _horizontalStarted = false;
            EditorGUILayout.EndHorizontal();
        }

        public static void Indent(int amount = 1) => EditorGUI.indentLevel+= amount;
        
        public static void UnIndent(int amount = 1) => EditorGUI.indentLevel = Mathf.Max(0, EditorGUI.indentLevel - amount);
        
        private static readonly GUIContent _textAndToolTip = new();

        //TextLabel

        #region Foldout
        private static StateToken StylizedFoldOut(bool foldedOut, TextLabel label, string hint = "FoldIn/FoldOut")
        {
            label.FallbackHint = () => hint;

            START();
            foldedOut = EditorGUILayout.Foldout(foldedOut, label.ToGUIContext());
            END();

            return  new StateToken(foldedOut);
        }

        public static StateToken Foldout(TextLabel txt)
        {
            using (FoldoutManager.StartFoldoutDisposable(out _))
            {
                Foldout(txt, ref FoldoutManager.selectedFold, _elementIndex);
            }

            return FoldoutManager.isFoldedOutOrEntered;
        }

        public static StateToken Foldout(TextLabel txt, ref bool state)
        {
            FoldoutManager.isFoldedOutOrEntered = StylizedFoldOut(state, txt);
            state = FoldoutManager.isFoldedOutOrEntered;
            return FoldoutManager.isFoldedOutOrEntered;
        }

        public static StateToken Foldout(TextLabel txt, ref int selected, int current)
        {

            FoldoutManager.isFoldedOutOrEntered = new pegi.StateToken(selected == current);

            if (StylizedFoldOut(FoldoutManager.isFoldedOutOrEntered, txt))
                selected = current;
            else
                if (FoldoutManager.isFoldedOutOrEntered) selected = -1;

            IsFoldedOutOrEntered = selected == current;

            return FoldoutManager.isFoldedOutOrEntered;
        }

        #endregion

        #region Select


        private static int originalPopUPFontSize;
        private static float originalPopUpFixedHeight;

        const float POPUP_HEIGHT = 22;

        private static IDisposable ChangeSizeDisposible()
        {
            originalPopUPFontSize = EditorStyles.popup.fontSize;
            originalPopUpFixedHeight = EditorStyles.popup.fixedHeight;

            EditorStyles.popup.fontSize = 16;
            EditorStyles.popup.fixedHeight = POPUP_HEIGHT;

            return QcSharp.DisposableAction(() =>
            {
                EditorStyles.popup.fontSize = originalPopUPFontSize;
                EditorStyles.popup.fixedHeight = originalPopUpFixedHeight;
            });
        }

        public static ChangesToken SelectFlags(ref int no, string[] from, int width)
        {
            START();
            no = EditorGUILayout.MaskField(no, from, GUILayout.MaxWidth(width), GUILayout.MinHeight(POPUP_HEIGHT));
            return END();
        }

        public static ChangesToken SelectFlags(ref int no, string[] from)
        {
            START();
            no = EditorGUILayout.MaskField(no, from);
            return END();
        }

        public static ChangesToken Select<T>(ref int no, List<T> lst, int width)
        {

            var listNames = new List<string>();
            var listIndexes = new List<int>();

            var current = -1;

            for (var j = 0; j < lst.Count; j++)
            {
                if (lst[j] == null) continue;

                if (no == j)
                    current = listIndexes.Count;
                listNames.Add("{0}: {1}".F(j, lst[j].GetNameForInspector()));
                listIndexes.Add(j);

            }

            if (Select(ref current, listNames.ToArray(), width))
            {
                no = listIndexes[current];
                return ChangesToken.True;
            }

            return ChangesToken.False;

        }

    
        private static void FixDuplicates(ref string[] from)
        {
            var set = new HashSet<string>();

            for (int i = 0; i < from.Length; i++)
            {
                var el = from[i];
                if (set.Contains(el))
                {
                    el = "{0} ({1})".F(el, i);
                    from[i] = el;
                }

                set.Add(el);
            }
        }

        public static ChangesToken Select(ref int no, string[] from, int width)
        {
            START();
            FixDuplicates(ref from);

            using (ChangeSizeDisposible())
            {
                no = EditorGUILayout.Popup(no, from, GUILayout.MaxWidth(width), GUILayout.MinHeight(POPUP_HEIGHT));
            }
            return END();

        }

        public static ChangesToken Select(ref int no, string[] from)
        {
            START();
            FixDuplicates(ref from);

            using (ChangeSizeDisposible())
            {
                no = EditorGUILayout.Popup(no, from, GUILayout.MinHeight(POPUP_HEIGHT));
            }
            return END();
        }

        public static ChangesToken Select(ref int no, Dictionary<int, string> from)
        {
            var options = new string[from.Count];

            var ind = -1;

            for (var i = 0; i < from.Count; i++)
            {
                var e = from.GetElementAt(i);
                options[i] = e.Value;
                if (no == e.Key)
                    ind = i;
            }


            using (ChangeSizeDisposible())
            {
                START();
                var newInd = EditorGUILayout.Popup(ind, options, EditorStyles.toolbarDropDown, GUILayout.MinHeight(POPUP_HEIGHT));

                if (!END())
                    return ChangesToken.False;

                no = from.GetElementAt(newInd).Key;

                return ChangesToken.True;
            }
        }

        public static ChangesToken Select(ref int no, Dictionary<int, string> from, int width)
        {
            var options = new string[from.Count];
            var ind = -1;

            for (var i = 0; i < from.Count; i++)
            {
                var e = from.GetElementAt(i);
                options[i] = e.Value;
                if (no == e.Key)
                    ind = i;
            }

            using (ChangeSizeDisposible())
            {
                START();
                var newInd = EditorGUILayout.Popup(ind, options, EditorStyles.toolbarDropDown, GUILayout.MaxWidth(width), GUILayout.MinHeight(POPUP_HEIGHT));
                if (!END()) return ChangesToken.False;

                no = from.GetElementAt(newInd).Key;
                return ChangesToken.True;
            }
        }

        public static ChangesToken Select(ref int index, Texture[] tex)
        {
            if (tex.Length == 0) return ChangesToken.False;

            var before = index;
            var texNames = new List<string>();
            var texIndexes = new List<int>();

            var tmpInd = 0;
            for (var i = 0; i < tex.Length; i++)
                if (tex[i])
                {
                    texIndexes.Add(i);
                    texNames.Add("{0}: {1}".F(i, tex[i].name));
                    if (index == i) tmpInd = texNames.Count - 1;
                }


            using (ChangeSizeDisposible())
            {
                START();
                tmpInd = EditorGUILayout.Popup(tmpInd, texNames.ToArray(), GUILayout.MinHeight(POPUP_HEIGHT));
                if (!END()) return ChangesToken.False;

                if (tmpInd >= 0 && tmpInd < texNames.Count)
                    index = texIndexes[tmpInd];

                return new ChangesToken(before != index);
            }
        }

        private static ChangesToken Select_Type(ref Type current, IReadOnlyList<Type> others, Rect rect)
        {

            var names = new string[others.Count];

            var ind = -1;

            for (var i = 0; i < others.Count; i++)
            {
                var el = others[i];
                names[i] = el.ToPegiStringType();
                if (el != null && el == current)
                    ind = i;
            }

            using (ChangeSizeDisposible())
            {
                START();

                var newNo = EditorGUI.Popup(rect, ind, names);

                if (!END()) return ChangesToken.False;

                current = others[newNo];
                return ChangesToken.True;
            }
        }

        private static ChangesToken Select(ref Component current, IReadOnlyList<Component> others, Rect rect)
        {

            var names = new string[others.Count];

            var ind = -1;

            for (var i = 0; i < others.Count; i++)
            {
                var el = others[i];
                names[i] = i + ": " + el.GetType().ToPegiStringType();
                if (el && el == current)
                    ind = i;
            }

            using (ChangeSizeDisposible())
            {
                START();

                var newNo = EditorGUI.Popup(rect, ind, names);

                if (!END()) return ChangesToken.False;

                current = others[newNo];

                return ChangesToken.True;
            }
        }

        #endregion

        public static void Space()
        {
            CheckLine_Editor();
            EditorGUILayout.Separator();
            Nl();
        }

        #region Edit

        #region Values

        public static ChangesToken Edit_Range(TextLabel label, ref float from, ref float to, float min, float max)
        {
            START();
            /*if (label.GotWidth)
                EditorGUILayout.MinMaxSlider(label.ToGUIContext(), ref from, ref to, minLimit: min, maxLimit: max, GUILayout.MaxWidth(label.width));
            else*/
                EditorGUILayout.MinMaxSlider(label.ToGUIContext(), ref from, ref to, minLimit: min, maxLimit: max);

            return END();
        }

        public static ChangesToken Edit_Range(ref float from, ref float to, float min, float max)
        {
            START();
            EditorGUILayout.MinMaxSlider(ref from, ref to, minLimit: min, maxLimit: max);
            return END();
        }


        public static ChangesToken Edit_Scene(ref string path)
        {
            START();

            var oldScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);

            var newScene = EditorGUILayout.ObjectField(oldScene, typeof(SceneAsset), allowSceneObjects: false) as SceneAsset;
            if (END()) 
            {
                path = AssetDatabase.GetAssetPath(newScene);
                return ChangesToken.True;
            }
            return ChangesToken.False;
        }

        public static ChangesToken Edit_Scene(ref string path, int width)
        {
            START();

            var oldScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);

            var newScene = EditorGUILayout.ObjectField(oldScene, typeof(SceneAsset), allowSceneObjects: false, GUILayout.MaxWidth(width)) as SceneAsset;
            if (END())
            {
                path = AssetDatabase.GetAssetPath(newScene);
                return ChangesToken.True;
            }
            return ChangesToken.False;
        }

        public static ChangesToken EditTag(ref string tag)
        {
            START();
            tag = EditorGUILayout.TagField(tag);
            return END();
        }

        public static ChangesToken EditLayerMask(ref int val)
        {
            START();
            val = EditorGUILayout.LayerField(val);
            return END();
        }

        public static ChangesToken Edit(ref string text)
        {
            START();
            text = EditorGUILayout.TextField(text);
            return END();
        }

        public static ChangesToken Edit(TextLabel label, ref string val)
        {
            if (label.GotWidth)
            {
                Write(label);
                return Edit(ref val);
            }

            START();
            val = EditorGUILayout.TextField(label.ToGUIContext(), val);
            return END();
        }

        public static ChangesToken Edit(ref string text, int width)
        {
            START();
            text = EditorGUILayout.TextField(text, GUILayout.MaxWidth(width));
            return END();
        }

        public static ChangesToken EditBig(ref string text, int height = 100)
        {
            START();
            text = EditorGUILayout.TextArea(text, GUILayout.MaxHeight(height));
            return END();
        }

        public static ChangesToken Edit<T>(ref T field, bool allowSceneObjects = true) where T : Object
        {
            START();
            field = (T)EditorGUILayout.ObjectField(field, typeof(T), allowSceneObjects);
            return END();
        }

        public static ChangesToken Edit(ref Object field, Type type, bool allowSceneObjects = true)
        {
            START();
            field = EditorGUILayout.ObjectField(field, type, allowSceneObjects);
            return END();
        }

        public static ChangesToken Edit(ref Object field, Type type, int width, bool allowSceneObjects = true) 
        {
            START();
            field = EditorGUILayout.ObjectField(field, type, allowSceneObjects, GUILayout.MaxWidth(width));
            return END();
        }

        public static ChangesToken Edit<T>(ref T field, int width, bool allowSceneObjects = true) where T : Object
        {
            START();
            field = (T)EditorGUILayout.ObjectField(field, typeof(T), allowSceneObjects, GUILayout.MaxWidth(width));
            return END();
        }

        public static ChangesToken Edit(TextLabel label, ref AnimationCurve curve)
        {
            START();
            curve = EditorGUILayout.CurveField(label.ToGUIContext(), curve);
            return END();
        }

        public static ChangesToken Edit(TextLabel label, ref int val)
        {
            if (label.GotWidth)
            {
                Write(label);
                return Edit(ref val);
            }

            START();
            val = EditorGUILayout.IntField(label.ToGUIContext(), val);
            return END();
        }

        public static ChangesToken Edit(TextLabel label, ref long val)
        {
            if (label.GotWidth)
            {
                Write(label);
                return Edit(ref val);
            }

            START();
            val = EditorGUILayout.LongField(label.ToGUIContext(), val);
            return END();
        }

        public static ChangesToken Edit(TextLabel label, ref float val)
        {
            if (label.GotWidth)
            {
                Write(label);
                return Edit(ref val);
            }

            START();
            val = EditorGUILayout.FloatField(label.ToGUIContext(), val);
            return END();
        }

        public static ChangesToken Edit(TextLabel label, ref float val, int valueWidth)
        {
            if (label.GotWidth)
            {
                Write(label);
                return Edit(ref val, width: valueWidth);
            }

            START();
            val = EditorGUILayout.FloatField(label.ToGUIContext(), val, GUILayout.MaxWidth(valueWidth));
            return END();
        }

        public static ChangesToken Edit(ref byte val)
        {
            START();
            int newValue = val;
            newValue = EditorGUILayout.IntField(newValue);
            var changed = END();

            if (changed)
                val = (byte)Mathf.Clamp(newValue, byte.MinValue, byte.MaxValue);

            return changed;
        }

        public static ChangesToken Edit(ref float val)
        {
            START();
            val = EditorGUILayout.FloatField(val);
            return END();
        }

        public static ChangesToken Edit(ref float val, int width)
        {
            START();
            val = EditorGUILayout.FloatField(val, GUILayout.MaxWidth(width));
            return END();
        }

        public static ChangesToken Edit(ref double val, int width)
        {
            START();
            val = EditorGUILayout.DoubleField(val, GUILayout.MaxWidth(width));
            return END();
        }

        public static ChangesToken Edit(ref double val)
        {
            START();
            val = EditorGUILayout.DoubleField(val);
            return END();
        }
        
        public static ChangesToken Edit(ref int val, int minInclusive, int maxInclusive)
        {
            START();
            val = EditorGUILayout.IntSlider(val, minInclusive, maxInclusive); 
            return END();
        }

        public static ChangesToken Edit(ref uint val, uint min, uint max)
        {
            START();
            val = (uint)EditorGUILayout.IntSlider((int)val, (int)min, (int)max); 
            return END();
        }

        public static ChangesToken Edit(ref float val, float min, float max)
        {
            START();
            val = EditorGUILayout.Slider(val, min, max);
            return END();
        }

        public static ChangesToken Edit(ref double val, double min, double max)
        {
            START();

            var tmpVal = val;

            if (Math.Abs(val) < float.MaxValue && Math.Abs(min) < float.MaxValue && Math.Abs(max) < float.MaxValue)
            {

                tmpVal = EditorGUILayout.Slider((float)val, (float)min, (float)max);
            } else 
            {
                min *= 0.25d;
                max *= 0.25d;
                tmpVal *= 0.25d;

                var gap = max - min;
                double tmp = (tmpVal - min) / gap; // Remap to 01 range
                tmpVal = EditorGUILayout.Slider((float)tmp, 0f, 1f) * gap + min;

                tmpVal *= 4;
            }

            if (END()) 
            {
                val = tmpVal;
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }

        public static ChangesToken Edit(ref long val, long min, long max)
        {
            START();

           

            if (Math.Abs(val) < float.MaxValue && Math.Abs(min) < float.MaxValue && Math.Abs(max) < float.MaxValue)
            {
                float tmpVal = EditorGUILayout.Slider((float)val, (float)min, (float)max);

                if (END())
                {
                    val = (long)tmpVal;
                    return ChangesToken.True;
                }
            }
            else
            {
                long gap = max - min;
                var in01Range = (val - min) / gap; // Remap to 01 range
                float tmpVal = EditorGUILayout.Slider(in01Range, 0f, 1f);

                if (END())
                {
                    val = (long)(tmpVal * gap) + min;
                    return ChangesToken.True;
                }
            }

            return ChangesToken.False;
        }


        public static ChangesToken Edit(ref Color col)
        {

            START();
            col = EditorGUILayout.ColorField(col);
            return END();

        }

        public static ChangesToken Edit(TextLabel label, ref Color col, bool showEyeDropper, bool showAlpha, bool hdr)
        {
            START();
             col = EditorGUILayout.ColorField(label.ToGUIContext(), col, showEyedropper: showEyeDropper, showAlpha: showAlpha, hdr: hdr);
            return END();

        }

        public static ChangesToken Edit(TextLabel label, ref Vector3 vec)
        {
            START();
                vec = EditorGUILayout.Vector3Field(label.ToGUIContext(), vec);
            return END();

        }
        
        public static ChangesToken Edit(ref Color col, int width)
        {

            START();
            col = EditorGUILayout.ColorField(col, GUILayout.MaxWidth(width));
            return END();

        }
     
        /*
        public static ChangesToken Edit(ref Dictionary<int, string> dic, int atKey)
        {
            var before = dic[atKey];
            if (Edit_Delayed(ref before))
            {
                dic[atKey] = before;
                return FeedChanged();
            }
            return ChangesToken.False;
        }*/

        public static ChangesToken Edit(ref int val)
        {
            START();
            val = EditorGUILayout.IntField(val);
            return END();
        }

        public static ChangesToken Edit(ref uint val)
        {
            START();
            val = (uint)EditorGUILayout.IntField((int)val);
            return END();
        }

        public static ChangesToken Edit(ref long val)
        {
            START();
            val = EditorGUILayout.LongField(val);
            return END();
        }

        public static ChangesToken Edit(ref int val, int width)
        {
            START();
            val = EditorGUILayout.IntField(val, GUILayout.MaxWidth(width));
            return END();
        }

        public static ChangesToken Edit(ref long val, int width)
        {
            START();
            val = EditorGUILayout.LongField(val, GUILayout.MaxWidth(width));
            return END();
        }

        public static ChangesToken Edit(ref uint val, int width)
        {
            START();
            val = (uint)EditorGUILayout.IntField((int)val, GUILayout.MaxWidth(width));
            return END();
        }
        /*
        public static bool edit(string name, ref AnimationCurve val)
        {

            BeginCheckLine();
            val = EditorGUILayout.CurveField(name, val);
            return EndCheckLine();
        }
        */
        public static ChangesToken Edit(TextLabel label, ref Vector4 val)
        {
            START();
            val = EditorGUILayout.Vector4Field(label.ToGUIContext(), val);
            return END();
        }

        public static ChangesToken Edit(TextLabel label, ref Vector2 val)
        {
            START();
            val = EditorGUILayout.Vector2Field(label.ToGUIContext(), val);
            return END();
        }

        public static ChangesToken Edit(TextLabel label, ref Vector2Int val)
        {
            START();
            val = EditorGUILayout.Vector2IntField(label.ToGUIContext(), val);
            return END();
        }

        public static ChangesToken Edit(ref Vector4 val) => "X".PL(15).Edit(ref val.x).Nl() | "Y".PL(15).Edit(ref val.y).Nl() | "Z".PL(15).Edit(ref val.z).Nl() | "W".PL(15).Edit(ref val.w).Nl();
        #endregion

        #region Delayed

        public static ChangesToken Edit_Delayed(ref string text)
        {

            START();
            text = EditorGUILayout.DelayedTextField(text);
            return END();

            /*  if (KeyCode.Return.IsDown())
              {
                  if (text.GetHashCode().ToString() == _editedHash)
                  {
                      checkLine();
                      EditorGUILayout.TextField(text);
                      text = _editedText;
                      return change;
                  }
              }

              var tmp = text;
              if (edit(ref tmp).ignoreChanges())
              {
                  _editedText = tmp;
                  _editedHash = text.GetHashCode().ToString();
              }

              return false;//(String.Compare(before, text) != 0);*/
        }

        public static ChangesToken Edit_Delayed(ref string text, int width)
        {
            START();
            text = EditorGUILayout.DelayedTextField(text, GUILayout.MaxWidth(width));
            return END();
        }

        public static ChangesToken Edit_Delayed(ref int val, int width)
        {

            START();

            if (width > 0)
                val = EditorGUILayout.DelayedIntField(val, GUILayout.MaxWidth(width));
            else
                val = EditorGUILayout.DelayedIntField(val);

            return END();

            /* if (KeyCode.Return.IsDown() && (_elementIndex == editedIntegerIndex))
             {
                 checkLine();
                 EditorGUILayout.IntField(val, GUILayout.Width(width));
                 val = editedInteger;
                 _elementIndex++; editedIntegerIndex = -1;
                 return change;
             }

             var tmp = val;
             if (edit(ref tmp).ignoreChanges())
             {
                 editedInteger = tmp;
                 editedIntegerIndex = _elementIndex;
             }

             _elementIndex++;

             return false;*/
        }

        public static ChangesToken Edit_Delayed(ref float val)
        {

            START();
            val = EditorGUILayout.DelayedFloatField(val);
            return END();
        }

        public static ChangesToken Edit_Delayed(ref float val, int width)
        {

            START();
            val = EditorGUILayout.DelayedFloatField(val, GUILayout.MaxWidth(width));
            return END();

            /* if (KeyCode.Return.IsDown() && (_elementIndex == _editedFloatIndex))
             {
                 checkLine();
                 EditorGUILayout.FloatField(val, GUILayout.Width(width));
                 val = _editedFloat;
                 _elementIndex++;
                 _editedFloatIndex = -1;
                 return change;
             }

             var tmp = val;
             if (edit(ref tmp, width).ignoreChanges())
             {
                 _editedFloat = tmp;
                 _editedFloatIndex = _elementIndex;
             }

             _elementIndex++;

             return false;*/
        }

        public static ChangesToken Edit_Delayed(ref double val)
        {

            START();
            val = EditorGUILayout.DelayedDoubleField(val);
            return END();
        }

        public static ChangesToken Edit_Delayed(ref double val, int width)
        {

            START();
            val = EditorGUILayout.DelayedDoubleField(val, GUILayout.MaxWidth(width));
            return END();


        }


        #endregion

        #region Property

        public static ChangesToken Edit_Property<T>(GUIContent content, List<string> memberPath, Expression<Func<T>> memberExpression, Object obj, int width, bool includeChildren)
        {
            SerializedObject serializedObject = (!obj ? SerObj : GetSerObj(obj));

            if (serializedObject == null)
            {
                "No SerObj".ConstL().Write();
                return ChangesToken.False;
            }

            SerializedProperty property = serializedObject.FindProperty(memberPath[0]);

            for (int i=1; i < memberPath.Count; i++)
            {
                if (property.isArray)
                    property = property.GetArrayElementAtIndex(pegi.InspectedIndex);

                property = property.FindPropertyRelative(memberPath[i]);
            }

            property = GetProperty(property, memberExpression);

            return Edit_Property_Internal(content, serializedObject: serializedObject, property: property, width: width, includeChildren: includeChildren);


        }

        public static ChangesToken Edit_Property<T>(GUIContent content, Expression<Func<T>> memberExpression, Object obj, int width, bool includeChildren)
        {
            var serializedObject = (!obj ? SerObj : GetSerObj(obj));

            SerializedProperty property = GetProperty(serializedObject, memberExpression);

            return Edit_Property_Internal(content, serializedObject: serializedObject, property: property, width: width, includeChildren: includeChildren);
        }

        private static SerializedProperty GetProperty<T>(SerializedObject serializedObject, Expression<Func<T>> memberExpression)
        {
            if (serializedObject == null)
            {
                "No SerObj".ConstL().Write();
                return null;
            }

            string name = QcSharp.GetPropertyName(memberExpression); //GetPropertyName(memberExpression);

            if (name  == null) 
                return null;
            
            var property = serializedObject.FindProperty(name);

            if (property == null)
                "{0} not found".F(name).PL().Write();

            return property;
        }

        private static SerializedProperty GetProperty<T>(SerializedProperty serializedProperty, Expression<Func<T>> memberExpression)
        {
            if (serializedProperty == null)
            {
                "No SerProp".ConstL().Write();
                return null;
            }

            string name = QcSharp.GetPropertyName(memberExpression);//GetPropertyName(memberExpression);

            if (name == null)
                return null;

            if (serializedProperty.isArray) 
            {
                serializedProperty = serializedProperty.GetArrayElementAtIndex(pegi.InspectedIndex);
            }

            var property = serializedProperty.FindPropertyRelative(name); 

            if (property == null)
                "{0} not found".F(name).PL(90).Write();

            return property;
        }

        private static ChangesToken Edit_Property_Internal(GUIContent cnt, SerializedObject serializedObject, SerializedProperty property, int width, bool includeChildren)
        {
            if (property == null)
                return ChangesToken.False;

            //  "WTF".PegiLabel().write(30);

            
            //pegi.nl();

            EditorGUI.BeginChangeCheck();

            cnt ??= new GUIContent(property.displayName);
            //cnt ??= GUIContent.none;

            if (width < 1)
                EditorGUILayout.PropertyField(property, cnt, includeChildren);
            else
                EditorGUILayout.PropertyField(property, cnt, includeChildren, GUILayout.MaxWidth(width));

            if (!EditorGUI.EndChangeCheck())
                return ChangesToken.False;

            serializedObject.ApplyModifiedProperties();

            return FeedChanged();
        }

        private static readonly Dictionary<Object, SerializedObject> SerializedObjects = new();

        private static SerializedObject GetSerObj(Object obj)
        {
            if (SerializedObjects.TryGetValue(obj, out SerializedObject so))
            {
                so.Update();
                return so;
            }

            so = new SerializedObject(obj);

            if (SerializedObjects.Count > 8)
                SerializedObjects.Clear();

            SerializedObjects.Add(obj, so);

            return so;

        }
        #endregion

        #endregion

        #region Toggle
       
        public static ChangesToken Toggle(ref bool val)
        {
            START();
            val = EditorGUILayout.Toggle(val, GUILayout.MaxWidth(40));
            return END();
        }

        /*
        public static ChangesToken Toggle(ref bool val, GUIContent cnt)
        {
            _START();
            val = EditorGUILayout.Toggle(cnt, val);
            return _END();
        }*/

        #endregion

        #region Click
        public static ChangesToken Click(TextLabel label)
        {
            CheckLine_Editor();

            if (label.GotWidth)
                return GUILayout.Button(label.ToGUIContext(), GUILayout.MaxWidth(label.width), GUILayout.MaxHeight(DEFAULT_BUTTON_SIZE)).FeedChanges_Internal();
            else
                return GUILayout.Button(label.ToGUIContext(), GUILayout.MaxHeight(DEFAULT_BUTTON_SIZE)).FeedChanges_Internal();
        }

        public static ChangesToken Click(GUIContent content)
        {
            CheckLine_Editor();
            return GUILayout.Button(content, GUILayout.MaxHeight(DEFAULT_BUTTON_SIZE)).FeedChanges_Internal();
        }

        public static ChangesToken Click(GUIContent content, GUIStyle style)
        {
            CheckLine_Editor();
            return GUILayout.Button(content, style, GUILayout.MaxHeight(DEFAULT_BUTTON_SIZE)).FeedChanges_Internal();
        }

        public static ChangesToken Click(GUIContent content, int width, GUIStyle style)
        {
            CheckLine_Editor();
            return GUILayout.Button(content, style, GUILayout.MaxWidth(width+5), GUILayout.MaxHeight(DEFAULT_BUTTON_SIZE)).FeedChanges_Internal();
        }

        public static ChangesToken Click(Texture image, int width, GUIStyle style = null)
        {
            style ??= Styles.ImageButton.Current;

            CheckLine_Editor();
            return GUILayout.Button(image, style, GUILayout.MaxWidth(width + 5), GUILayout.MaxHeight(DEFAULT_BUTTON_SIZE)).FeedChanges_Internal();
        }

        public static ChangesToken ClickImage(GUIContent cnt, int size, GUIStyle style = null)
        {
            int height = size;
            int width = Mathf.CeilToInt(((cnt.image.width)/ ((float)cnt.image.height)) * size);

            return ClickImage(cnt, width + 5, height, style);
        }
        public static ChangesToken ClickImage(GUIContent cnt, int width, int height, GUIStyle style = null)
        {
            style ??= Styles.ImageButton.Current;

            CheckLine_Editor();

            return GUILayout.Button(cnt, style, GUILayout.MaxWidth(width), GUILayout.MaxHeight(height)).FeedChanges_Internal();
        }

        #endregion

        #region write

        private static readonly GUIContent imageAndTip = new();
/*
        private static GUIContent ImageAndTip(Texture tex) => ImageAndTip(tex, tex.GetNameForInspector_Uobj());
       */
        private static GUIContent ImageAndTip(Texture tex, string toolTip)
        {
            imageAndTip.image = tex;
            imageAndTip.tooltip = toolTip;
            return imageAndTip;
        }

        public static void Write<T>(T field) where T : Object
        {
            CheckLine_Editor();
            EditorGUILayout.ObjectField(field, typeof(T), false);
        }

        public static void Write(TextLabel label)
        {

            label.TryWrite();

            /*
            var style = label.style == null ? EditorStyles.miniLabel : label.style.Current;

            CheckLine_Editor();
            if (label.GotWidth)
            {
                EditorGUILayout.LabelField(label.ToGUIContext(), style, GUILayout.MaxWidth(label.width));
            } else 
            {
                EditorGUILayout.LabelField(label.ToGUIContext(), style);
            }*/
        }

        public static void ProgressBar(TextLabel label, float value) {
            CheckLine_Editor();

            if (label.label.IsNullOrEmpty())
                label.label = "Unnamed";

            EditorGUI.ProgressBar(GetRect(height: 25), Mathf.Clamp01(value), label.label);
        }

        public static void Write(GUIContent cnt)
        {
            CheckLine_Editor();
            EditorGUILayout.LabelField(cnt, Styles.Text.Clipping.Current);
        }

        public static void Draw(Texture tex, string tip, int width, int height)
        {
            CheckLine_Editor();
            var cnt = ImageAndTip(tex, tip);
            GUI.enabled = false;
            using (SetBgColorDisposable(Color.clear))
            {
                GUILayout.Button(cnt, Styles.ImageButton.Current, GUILayout.MaxWidth(width + 10), GUILayout.MaxHeight(height));
            }
            GUI.enabled = true;
        }

        public static void Draw(Texture tex, int width, bool alphaBlend)
        {
            CheckLine_Editor();
            var rect = EditorGUILayout.GetControlRect(GUILayout.MaxWidth(width), GUILayout.MaxHeight(width));
            GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit, alphaBlend: alphaBlend);
        }

        public static void Write(GUIContent cnt, int width)
        {
            CheckLine_Editor();
            EditorGUILayout.LabelField(cnt, Styles.Text.Clipping.Current, GUILayout.MaxWidth(width));
        }

        public static void Write_ForCopy(TextLabel text)
        {
            CheckLine_Editor();

            var style = text.GotStyle ? text.style.Current : Styles.Text.Clipping.Current;

            if (text.GotWidth)
            {
                EditorGUILayout.SelectableLabel(text.label, style, GUILayout.Height(15), GUILayout.MaxWidth(text.width));
            } else 
            {
                EditorGUILayout.SelectableLabel(text.label, style, GUILayout.Height(15));
            }
        }

        public static void Write(GUIContent cnt, int width, GUIStyle style)
        {
            CheckLine_Editor();
            EditorGUILayout.LabelField(cnt, style, GUILayout.MaxWidth(width));
        }

        public static void Write(GUIContent cnt, GUIStyle style)
        {
            CheckLine_Editor();
            EditorGUILayout.LabelField(cnt, style);
        }

        public static void WriteHint(TextLabel text, MessageType type)
        {
            CheckLine_Editor();
            EditorGUILayout.HelpBox(text.label, type);
        }
        #endregion

       // private static bool _searchInChildren;

        public static IEnumerable<T> DropAreaGUI<T>() where T : Object
        {
            Nl();

            var evt = Event.current;
            var drop_area = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));

            GUILayout.Box("Drag & Drop {0}[] above".F(collectionInspector.GetCurrentListLabel<T>(null)));

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!drop_area.Contains(evt.mousePosition))
                        yield break;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform) {

                        DragAndDrop.AcceptDrag();

                        foreach (var o in DragAndDrop.objectReferences) {
                            var cnvrt = o as T;
                            if (cnvrt)
                                yield return cnvrt;
                            else {
                                var go = o as GameObject;

                                if (!go) 
                                    continue;

                                foreach (var c in 
                                    //_searchInChildren
                                    //? go.GetComponentsInChildren(typeof(T))
                                    //: 
                                    go.GetComponents(typeof(T))) {

                                    yield return c as T;
                                }
                            }
                        }
                    }
                    break;
            }
        }

        #region Reordable List

        private static readonly Dictionary<System.Collections.IList, ReorderableList> _reorderableLists = new();

        private static ReorderableList GetReordable<T>(this List<T> list, CollectionInspectorMeta metaDatas)
        {
            _reorderableLists.TryGetValue(list, out ReorderableList rl);

            if (rl != null) return rl;

            rl = new ReorderableList(list, typeof(T), metaDatas == null || metaDatas[CollectionInspectParams.allowReordering], true, false, false);//metaDatas == null || metaDatas.allowDelete);
            _reorderableLists.Add(list, rl);

            rl.drawHeaderCallback += DrawHeader;
            rl.drawElementCallback += DrawElement;

            return rl;
        }

        private static System.Collections.IList _currentReorderedList;
        private static Type _currentReorderedType;
        private static List<Type> _currentReorderedListTypes;
        private static CollectionInspectorMeta _listMetaData;
        private static Migration.TaggedTypes.DerrivedList _currentTaggedTypes;


        private static bool GetIsSelected(int ind) => (_listMetaData != null) 
            ? _listMetaData.GetIsSelected(ind) 
            : collectionInspector.selectedEls.Contains(ind); 

        private static void SetIsSelected(int ind, bool val)
        {
            if (_listMetaData != null)
                _listMetaData.SetIsSelected(ind, val);
            else 
                collectionInspector.selectedEls.SetContains(ind, val);
        }

   

        public static ChangesToken Reorder_List<T>(List<T> l, CollectionInspectorMeta metas)
        {
            _listMetaData = metas;

            START();
            //EditorGUI.BeginChangeCheck();

            if (_currentReorderedList != l)
            {

                var type = typeof(T);

                _currentReorderedListTypes = Migration.ICfgExtensions.TryGetDerivedClasses(type);

                if (_currentReorderedListTypes == null)
                {
                    _currentTaggedTypes = Migration.TaggedTypes<T>.DerrivedList; // (type); //typeof(T).TryGetTaggedClasses();
                    if (_currentTaggedTypes != null)
                        _currentReorderedListTypes = _currentTaggedTypes.Types;
                }
                else _currentTaggedTypes = null;

                _currentReorderedType = type;
                _currentReorderedList = l;
                if (metas == null)
                    collectionInspector.selectedEls.Clear();

            }

            l.GetReordable(metas).DoLayoutList();
            return END(); //EditorGUI.EndChangeCheck();
        }

        private static void DrawHeader(Rect rect) => GUI.Label(rect, "Ordering {0} {1}s".F(_currentReorderedList.Count.ToString(), _currentReorderedType.ToPegiStringType()));

        private static void DrawElement(Rect rect, int index, bool active, bool focused)
        {

            var el = _currentReorderedList[index];

            var selected = GetIsSelected(index);

            var after = EditorGUI.Toggle(new Rect(rect.x, rect.y, 30, rect.height), selected);

            if (after != selected)
                SetIsSelected(index, after);

            rect.x += 30;
            rect.width -= 30;

            if (el != null)
            {

                Type ty = el.GetType();

                bool exactType = ty == _currentReorderedType;

                _textAndToolTip.text = "{0} {1}".F(el.GetNameForInspector(), exactType ? "" : " {0} ".F(ty.ToPegiStringType()));
                _textAndToolTip.tooltip = el.ToString();

                var uo = el as Object;
                if (uo)
                {
                    GameObject go = uo as GameObject;

                    if (go)
                    {
                        EditorGUI.ObjectField(rect, _textAndToolTip, uo, _currentReorderedType, true);
                    } else {

                        Component cmp = uo as Component;
                        go = cmp ? cmp.gameObject : null;

                        if (!go)
                            EditorGUI.ObjectField(rect, _textAndToolTip, uo, _currentReorderedType, true);
                        else
                        {
                            var mbs = go.GetComponents(_currentReorderedType);

                            if (mbs.Length > 1)
                            {
                                rect.width = Screen.width * 0.5f;

                                rect.y -= 2;
                                EditorGUI.LabelField(rect, _textAndToolTip);
                                rect.y += 2;

                                rect.x += Screen.width * 0.5f;

                                rect.width = Math.Max(rect.width - 80, 10);

                                if (Select(ref cmp, mbs, rect))
                                    _currentReorderedList[index] = cmp;
                            }
                            else
                                EditorGUI.LabelField(rect, _textAndToolTip); //ObjectField(rect, _textAndToolTip, uo, _currentReorderedType, true);
                        }
                    }
                }
                else
                {
                    if (_currentReorderedListTypes != null)
                    {

                        _textAndToolTip.text = el.GetNameForInspector();

                        rect.width = 100;
                        EditorGUI.LabelField(rect, _textAndToolTip);
                        rect.x += 100;
                        rect.width = 100;

                        if (Select_Type(ref ty, _currentReorderedListTypes, rect))
                            Migration.TaggedTypesExtensions.TryChangeObjectType(_currentReorderedList, index, ty);
                    }
                    else
                        EditorGUI.LabelField(rect, _textAndToolTip);
                }
            }
            else
            {
               // var ed = 
                   // _listMetaData.TryGetElement(index);
                
               /* if (ed != null && ed.unrecognized)
                {

                    if (_currentTaggedTypes != null)
                    {

                        rect.width = 100;
                        EditorGUI.LabelField(rect, TextAndTip("UNREC {0}".F(ed.unrecognizedUnderTag), "Select New Class"));
                        rect.x += 100;
                        rect.width = 100;

                        Type ty = null;

                        if (select_Type(ref ty, _currentReorderedListTypes, rect))
                        {
                            el = Activator.CreateInstance(ty);
                            _currentReorderedList[index] = el;

                            var std = el as ICfg;

                            if (std != null)
                                std.Decode(ed.SetRecognized().stdDta);

                        }
                    }

                }
                else*/
                    EditorGUI.LabelField(rect, "Empty {0}".F(_currentReorderedType.ToPegiStringType()));
            }
        }

        /*private static void RemoveItem(ReorderableList list)
        {
            var i = list.index;
            var el = _currentReorderedList[i];
            if (el != null && _currentReorderedType.IsUnityObject())
                _currentReorderedList[i] = null;
            else
                _currentReorderedList.RemoveAt(i);
        }*/

        #endregion
        
#endif

    }


}
