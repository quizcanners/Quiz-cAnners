using System;
using System.Collections.Generic;
using QuizCanners.Utils;
using UnityEngine;

using Object = UnityEngine.Object;
using System.Collections;

#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0011 // Add braces
#pragma warning disable IDE0008 // Use explicit type
#pragma warning disable IDE1006 // Naming Styles

namespace QuizCanners.Inspect
{
    public static partial class pegi
    {

        #region Inspect Name

        public static ChangesToken Try_NameInspect(object obj, string label = "", string tip = "", bool delayedEdit = false)=>
             obj.Try_NameInspect(out _, label, tip, delayedEdit: delayedEdit);
        
        private static ChangesToken Try_NameInspect(this object obj, out bool couldInspect, string label = "", string tip = "", bool delayedEdit = false)
        {

            var changed = ChangeTrackStart();

            bool gotLabel = !label.IsNullOrEmpty();

            couldInspect = true;
            var iname = obj as IGotName;
            if (iname != null)
                return iname.inspect_Name(label, delayedEdit: delayedEdit);

            Object uObj = obj as ScriptableObject;

            if (!uObj)
                uObj = QcUnity.TryGetGameObjectFromObj(obj);

            if (!uObj)
                uObj = obj as Object;

            if (uObj)
            {
                var n = uObj.name;
                if (gotLabel ? label.PegiLabel(tip, 80).editDelayed( ref n) : editDelayed(ref n))
                {
                    uObj.name = n;
                    QcUnity.RenameAsset(uObj, n);
                }
            }
            else
                couldInspect = false;

            return changed;
        }

        public static ChangesToken inspect_Name(this IGotName obj, bool delayedEdit = false) => obj.inspect_Name("", delayedEdit);

        private static bool focusPassedToTheNext;
        public static ChangesToken inspect_Name(this IGotName obj, string label, bool delayedEdit = false)
        {
            var n = obj.NameForInspector;

            bool gotLabel = !label.IsNullOrEmpty();

            var uObj = obj as Object;

            if (uObj)
            {
                if ((gotLabel && label.PegiLabel(80).editDelayed( ref n)) || (!gotLabel && editDelayed(ref n)))
                {
                    obj.NameForInspector = n;

                    return ChangesToken.True;
                }
            }
            else
            {
                string focusName = InspectedIndex.ToString() + obj.GetNameForInspector();

                if (focusPassedToTheNext)
                {
                    FocusedText = focusName;
                    focusPassedToTheNext = false;
                }

                if (FocusedName.Equals(focusName) && KeyCode.DownArrow.IsDown())
                    focusPassedToTheNext = true;

                NameNextForFocus(focusName);

                if (delayedEdit)
                {
                    if ((gotLabel && label.PegiLabel(80).editDelayed( ref n)) || (!gotLabel && editDelayed(ref n)))
                    {
                        obj.NameForInspector = n;
                        return ChangesToken.True;
                    }
                }
                else
                {

                    if ((gotLabel && label.PegiLabel(80).edit(ref n)) || (!gotLabel && edit(ref n)))
                    {
                        obj.NameForInspector = n;
                        return ChangesToken.True;
                    }
                }
            }

            return ChangesToken.False;
        }

        #endregion

        internal static void Nested_Inspect_Attention_MessageOnly(IPEGI ipg) 
        {
            var na = ipg as INeedAttention;
            if (na != null)
            {
                var msg = na.NeedAttention();
                if (msg.IsNullOrEmpty() == false)
                    msg.PegiLabel().writeWarning();
            }
        }

        public static ChangesToken Nested_Inspect(Action function, Object target = null)
        {
            var changed = ChangeTrackStart();

            var il = IndentLevel;

            try
            {
                function();

                if (changed)
                {
                    if (target)
                        target.SetToDirty();
                    else
                        function.Target.SetToDirty_Obj();
                }
            } catch (Exception ex) 
            {
                write(ex);
            }

            IndentLevel = il;

            return changed;
        }

        public static ChangesToken Nested_Inspect<T>(ref T pgi, bool fromNewLine = true, bool writeWhenNeedsAttention = true) where T : struct, IPEGI
          => Nested_Inspect_Internal(ref pgi, fromNewLine: fromNewLine, writeWhenNeedsAttention: writeWhenNeedsAttention);

        public static ChangesToken Nested_Inspect<T>(this T pgi, bool fromNewLine = true, bool writeWhenNeedsAttention = true) where T : class, IPEGI
        {
            if (fromNewLine)
                nl();

            if (pgi.IsNullOrDestroyed_Obj())
            {
                "NULL".F(typeof(T).ToPegiStringType()).PegiLabel().write();
                return ChangesToken.False;
            }

            var changes = Nested_Inspect_Internal(ref pgi, fromNewLine: fromNewLine, writeWhenNeedsAttention: writeWhenNeedsAttention);

            if (changes)
            {

#if UNITY_EDITOR
                PegiEditorOnly.ClearFromPooledSerializedObjects(pgi as Object);
#endif
                pgi.SetToDirty_Obj();
            }

            return changes;

        }

        private static ChangesToken Nested_Inspect_Internal<T>(ref T pgi, bool fromNewLine = true, bool writeWhenNeedsAttention = true) where T : IPEGI
        {
            if (fromNewLine)
                nl();

            var changed = ChangeTrackStart();

            var isFOOE = PegiEditorOnly.isFoldedOutOrEntered;

            int recurses;

            bool inDic = inspectionChain.TryGetValue(pgi, out recurses);

            if (!inDic || recurses < 4)
            {
                inspectionChain[pgi] = recurses + 1;

                var indent = IndentLevel;

                pgi.Inspect();

                if (writeWhenNeedsAttention) 
                    Nested_Inspect_Attention_MessageOnly(pgi);

                RestoreBGColor();
                IndentLevel = indent;

                int count;
                if (inspectionChain.TryGetValue(pgi, out count))
                {
                    if (count < 2)
                        inspectionChain.Remove(pgi);
                    else
                        inspectionChain[pgi] = count - 1;
                }
            }
            else
                "3rd recursion".PegiLabel().writeWarning();

            PegiEditorOnly.isFoldedOutOrEntered = isFOOE;

            return changed;
        }

        public static ChangesToken Inspect_AsInListNested<T>(this T obj, ref int inspected, int current) where T : IPEGI_ListInspect
        {
            if (!EnterOptionsDrawn_Internal(ref inspected, current))
                return ChangesToken.False;

            var change = ChangeTrackStart();

            var il = IndentLevel;

            if (inspected == current)
            {
                if (icon.Back.Click() | obj.GetNameForInspector().PegiLabel().ClickLabel().nl())
                    inspected = -1;
                else 
                    Try_Nested_Inspect(obj);
            }
            else
            {
                obj.InspectInList(ref inspected, current);
            }

            nl();

            IndentLevel = il;

            if (change)
            {
#if UNITY_EDITOR
                PegiEditorOnly.ClearFromPooledSerializedObjects(obj as Object);
#endif
                obj.SetToDirty_Obj();
            }

            return change;
        }

        public static ChangesToken Inspect_AsInList<T>(this T obj) where T: class, IPEGI_ListInspect
        {
            var tmp = -1;

            var il = IndentLevel;

            var changed = ChangeTrackStart();

            obj.InspectInList(ref tmp, 0);
            IndentLevel = il;

            if (changed)
            {
#if UNITY_EDITOR
                PegiEditorOnly.ClearFromPooledSerializedObjects(obj as Object);
#endif
                obj.SetToDirty_Obj();
            }

            return changed;
        }

        public static ChangesToken Nested_Inspect(ref object obj)
        {
            var pgi = obj as IPEGI;
            var changed = ChangeTrackStart();

            if (pgi != null)
                pgi.Nested_Inspect();
            else 
                TryDefaultInspect(ref obj);


            nl();

            UnIndent();

            return changed;
        }

        public static ChangesToken Inspect_AsInList_Value<T>(ref T obj) where T: struct, IPEGI_ListInspect
        {
            var pgi = obj as IPEGI_ListInspect;
            var ch = ChangeTrackStart();

            if (pgi != null)
            {
                int entered = -1;
                pgi.Inspect_AsInListNested(ref entered, 0);
                if (ch)
                    obj = (T)pgi;
            }

            nl();

            UnIndent();

            return ch;
        }

        public static ChangesToken Try_Inspect_AsInList(ref object obj, ref int entered, int current)
        {
            var pgi = obj as IPEGI_ListInspect;
            var ch = ChangeTrackStart();

            if (pgi != null)
            {
                if (pgi.Inspect_AsInListNested(ref entered, current))
                    obj = pgi;
            }

            nl();

            UnIndent();

            return ch;
        }
        
        public static ChangesToken TryDefaultInspect(Object uObj)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI && uObj)
            {
                UnityEditor.Editor ed = GetEditorFor(uObj);

                if (ed == null)
                    return ChangesToken.False;

                nl();
                UnityEditor.EditorGUI.BeginChangeCheck();
                ed.DrawDefaultInspector();
                var changed = UnityEditor.EditorGUI.EndChangeCheck();
                if (changed)
                    PegiEditorOnly.globChanged = true;

                return new ChangesToken(changed);

            }
#endif


            return ChangesToken.False;

        }

        private static ChangesToken TryDefaultInspect(ref object obj)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                var uObj = obj as Object;

                if (uObj)
                {
                    UnityEditor.Editor ed = GetEditorFor(uObj);

                    if (ed == null)
                        return ChangesToken.False;

                    nl();
                    UnityEditor.EditorGUI.BeginChangeCheck();
                    ed.DrawDefaultInspector();
                    var changed = UnityEditor.EditorGUI.EndChangeCheck();
                    if (changed)
                        PegiEditorOnly.globChanged = true;

                    return new ChangesToken(changed);
                }
            }
#endif

            if (obj != null && obj is string)
            {
                var txt = obj as string;
                if (editBig(ref txt, 40))
                {
                    obj = txt;
                    return ChangesToken.True;
                }
            } else
            {
                nl();
                if (obj != null)
                    "Nothing to inspect inside {0}".F(obj).PegiLabel().writeHint();
                nl();
            }

            return ChangesToken.False;

        }

        public static ChangesToken Try_Nested_Inspect(object obj)
        {
            var pgi = obj as IPEGI;
            var ch = pgi?.Nested_Inspect() ?? TryDefaultInspect(ref obj);

            nl();

            UnIndent();

            return ch;
        }
        
        public static int CountForInspector<T>(this List<T> lst) where T : IGotCount
        {
            var count = 0;

            foreach (var e in lst)
                if (!e.IsNullOrDestroyed_Obj())
                    count += e.GetCount();

            return count;
        }

        private static bool IsNullOrDestroyed_Obj(this object obj)
        {
            var uobj = obj as Object;

            if (uobj!= null)
                return !uobj;

            return obj == null;
        }

        private static bool IsDefaultOrNull<T>(this T obj) => (obj == null) || EqualityComparer<T>.Default.Equals(obj, default);

        public static void AddOrReplaceByIGotIndex<T>(this List<T> list, T newElement) where T: IGotIndex
        {
            var newIndex = newElement.IndexForInspector;

            for (int i = 0; i < list.Count; i++)
            {
                var el = list[i];
                if (el != null && el.IndexForInspector == newIndex)
                {
                    list.RemoveAt(i);
                    list.Insert(i, newElement);
                    return;
                }
            }

            list.Add(newElement);
        }

        public static string GetNameForInspector_Uobj<T>(this T obj) where T : Object
        {
            if (obj == null)
                return "NULL UObj {0}".F(typeof(T).ToPegiStringType());

            if (!obj)
                return "Destroyed UObj {0}".F(typeof(T).ToPegiStringType());

            string tmp;
            if (obj.ToPegiStringInterfacePart(out tmp)) return tmp;

            var cmp = obj as Component;
            return cmp ? "{0} on {1}".F(cmp.GetType().ToPegiStringType(), cmp.gameObject.name) : obj.name;
        }

        public static string GetNameForInspector<T>(this T obj)
        {
            if (obj.IsNullOrDestroyed_Obj())
                return "NULL {0}".F(typeof(T).ToPegiStringType());

            var type = obj.GetType();

            if (type.IsClass)
            {
                if (obj is string)
                {
                    var str = obj as string;
                    if (str == null)
                        return "NULL String";
                    return str;
                }

                if (obj.GetType().IsUnityObject())
                    return (obj as Object).GetNameForInspector_Uobj();

                string tmp;
                return (obj.ToPegiStringInterfacePart(out tmp)) ? tmp : obj.ToString().SimplifyTypeName();
            }

            if (type.IsEnum) 
            {
                return QcSharp.AddSpacesToSentence(obj.ToString());
            }

            if (!type.IsPrimitive)
            {
                string tmp;
                return (obj.ToPegiStringInterfacePart(out tmp)) ? tmp : obj.ToString();
            }

            if (type == typeof(double)) 
            {
                return QcSharp.BigDoubleToString((double)((object)obj));
            }

            return obj.ToString();
        }

        public static T GetByIGotName<T>(this List<T> lst, string name) where T : IGotName
        {

            if (lst != null)
                foreach (var el in lst)
                    if (!el.IsNullOrDestroyed_Obj() && el.NameForInspector.SameAs(name))
                        return el;


            return default;
        }

        internal static V TryGetByElementIndex<T, V>(this Dictionary<T, V> list, int index, V defaultValue = default)
        {
            if (list == null || index < 0 || index >= list.Count)
                return defaultValue;

            return list.GetElementAt(index).Value;
        }

        internal static object SetToDirty_Obj(this object obj)
        {

#if UNITY_EDITOR
            (obj as Object).SetToDirty();
#endif

            return obj;
        }

        private static readonly Dictionary<IPEGI, int> inspectionChain = new Dictionary<IPEGI, int>();

        internal static void ResetInspectedChain() => inspectionChain.Clear();

#if UNITY_EDITOR
        private static readonly Dictionary<Object, UnityEditor.Editor> defaultEditors = new Dictionary<Object, UnityEditor.Editor>();
        private static UnityEditor.Editor GetEditorFor(Object obj)
        {
            if (!defaultEditors.TryGetValue(obj, out var editor))
            {
                if (defaultEditors.Count > 32)
                {
                    defaultEditors.Clear();
                }

                editor = UnityEditor.Editor.CreateEditor(obj);
                defaultEditors.Add(obj, editor);
            }
            return editor;
        }
#endif

        private static object TryGetObj(this IList list, int index)
        {
            if (list == null || index < 0 || index >= list.Count)
                return null;
            var el = list[index];
            return el;
        }

        private static bool ToPegiStringInterfacePart(this object obj, out string name)
        {
            name = null;

            var dn = obj as IGotReadOnlyName;
            if (dn != null)
            {
                name = dn.GetReadOnlyName();
                if (!name.IsNullOrEmpty())
                {
                    name = name.FirstLine();
                    return true;
                }

            }

            var sn = obj as IGotName;

            if (sn != null)
            {
                name = sn.NameForInspector;
                if (!name.IsNullOrEmpty())
                {
                    name = name.FirstLine();
                    return true;
                }
            }

            return false;
        }

    }

    internal static class Inspector_Extensions_Internal
    {
       
    }


}