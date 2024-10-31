using System;
using System.Collections.Generic;
using QuizCanners.Utils;
using UnityEngine;

using Object = UnityEngine.Object;
using System.Collections;
using System.Reflection;

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

        private static ChangesToken Try_NameInspect(object obj) =>
             obj.Try_NameInspect(out _, delayedEdit: true);

        private static ChangesToken Try_NameInspect(this object obj, out bool couldInspect, bool delayedEdit = false)
        {
            var changed = ChangeTrackStart();

            couldInspect = true;
            var iname = obj as IGotName;
            if (iname != null)
                return iname.inspect_Name(delayedEdit: delayedEdit);

            Object uObj = obj as ScriptableObject;

            if (!uObj)
                uObj = QcUnity.TryGetGameObjectFromObj(obj);

            if (!uObj)
                uObj = obj as Object;

            if (uObj)
            {
                var n = uObj.name;
                if (Edit_Delayed(ref n))
                {
                    uObj.name = n;
                    QcUnity.RenameAsset(uObj, n);
                }
            }
            else
                couldInspect = false;

            return changed;
        }

        private static bool focusPassedToTheNext;
        public static ChangesToken inspect_Name(this IGotName obj, bool delayedEdit = true)
        {
            var n = obj.NameForInspector;

            var uObj = obj as Object;

            if (uObj)
            {
                if (Edit_Delayed(ref n))
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
                    if (Edit_Delayed(ref n))
                    {
                        obj.NameForInspector = n;
                        return ChangesToken.True;
                    }
                }
                else
                {
                    if (Edit(ref n))
                    {
                        obj.NameForInspector = n;
                        return ChangesToken.True;
                    }
                }
            }

            return ChangesToken.False;
        }

        #endregion

        internal static void TryShow_AttentionMessage(this INeedAttention na) 
        {
            if (na != null)
            {
                var msg = na.NeedAttention();
                if (!msg.IsNullOrEmpty())
                {
                    Nl();
                    msg.PegiLabel().WriteWarning();
                }
            }
        }

        internal static void Nested_Inspect_Attention_MessageOnly(IPEGI ipg) => (ipg as INeedAttention).TryShow_AttentionMessage();

        public static ChangesToken Nested_Inspect(Action function, Object target = null)
        {
            using (PegiEditorOnly.InspectorStarted ? null : PegiEditorOnly.StartInspector(target))
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
                }
                catch (Exception ex)
                {
                    Write_Exception(ex);
                }

                IndentLevel = il;

                return changed;
            }
        }

        public static ChangesToken Nested_Inspect<T>(ref T pgi, bool fromNewLine = true, bool writeWhenNeedsAttention = true) where T : struct, IPEGI
          => Nested_Inspect_Internal(ref pgi, fromNewLine: fromNewLine, writeWhenNeedsAttention: writeWhenNeedsAttention);

        public static ChangesToken Nested_Inspect<T>(this TextLabel text, ref T pgi, bool fromNewLine = true, bool writeWhenNeedsAttention = true) where T : struct, IPEGI
        {
            text.Write();
            return Nested_Inspect_Internal(ref pgi, fromNewLine: fromNewLine, writeWhenNeedsAttention: writeWhenNeedsAttention);
        }

        public static ChangesToken Nested_Inspect_Value<T>(this TextLabel text, ref T pgi, bool fromNewLine = true, bool writeWhenNeedsAttention = true) where T : IPEGI
        {
            text.Write();
            return Nested_Inspect_Internal(ref pgi, fromNewLine: fromNewLine, writeWhenNeedsAttention: writeWhenNeedsAttention);
        }

        public static ChangesToken Nested_Inspect_Value<T>(ref T pgi, bool fromNewLine = true, bool writeWhenNeedsAttention = true) where T : IPEGI
        {
            return Nested_Inspect_Internal(ref pgi, fromNewLine: fromNewLine, writeWhenNeedsAttention: writeWhenNeedsAttention);
        }

        public static ChangesToken Nested_Inspect<T>(this T pgi, bool fromNewLine = true, bool writeWhenNeedsAttention = true) where T : class, IPEGI
        {
            if (fromNewLine)
                Nl();

            if (pgi.IsNullOrDestroyed_Obj())
            {
                "NULL".F(typeof(T).ToPegiStringType()).PegiLabel().Write();
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

        public static ChangesToken Nested_Context<T>(this T pgi, EnterExitContext context, bool writeWhenNeedsAttention = true) where T : class, IPEGI_Context
        {
            if (pgi.IsNullOrDestroyed_Obj())
            {
                "NULL".F(typeof(T).ToPegiStringType()).PegiLabel().Write();
                return ChangesToken.False;
            }

            var changes = Nested_Inspect_Internal(ref pgi, context, writeWhenNeedsAttention: writeWhenNeedsAttention);

            if (changes)
            {
#if UNITY_EDITOR
                PegiEditorOnly.ClearFromPooledSerializedObjects(pgi as Object);
#endif
                pgi.SetToDirty_Obj();
            }

            return changes;

        }

        public static ChangesToken Nested_Context<T>(ref T pgi, EnterExitContext context, bool writeWhenNeedsAttention = true) where T : struct, IPEGI_Context
        {
            if (pgi.IsNullOrDestroyed_Obj())
            {
                "NULL".F(typeof(T).ToPegiStringType()).PegiLabel().Write();
                return ChangesToken.False;
            }

            var changes = Nested_Inspect_Internal(ref pgi, context, writeWhenNeedsAttention: writeWhenNeedsAttention);

            if (changes)
                pgi.SetToDirty_Obj();
            
            return changes;
        }


        public static bool IsExitGUIException(Exception exception)
        {
            while (exception is System.Reflection.TargetInvocationException && exception.InnerException != null)
            {
                exception = exception.InnerException;
            }
            return exception is ExitGUIException;
        }

        private static ChangesToken Nested_Inspect_Internal<T>(ref T pgi, bool fromNewLine = true, bool writeWhenNeedsAttention = true) where T : IPEGI
        {
            using (PegiEditorOnly.InspectorStarted ? null : PegiEditorOnly.StartInspector(pgi))
            {

                if (pgi == null)
                {
                    "NULL".PegiLabel().WriteWarning().Nl();
                    return ChangesToken.False;
                }

                if (fromNewLine)
                    Nl();

                var changed = ChangeTrackStart();

                var isFOOE = FoldoutManager.isFoldedOutOrEntered;

                bool inDic = IpegiInspectionChain.TryGetValue(pgi, out int recurses);

                if (!inDic || recurses < 4)
                {
                    IpegiInspectionChain[pgi] = recurses + 1;

                    var indent = IndentLevel;

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

                    if (writeWhenNeedsAttention)
                    {
                        try
                        {
                            Nested_Inspect_Attention_MessageOnly(pgi);
                        }
                        catch (Exception ex)
                        {
                            Write_Exception(ex);
                        }
                    }

                    RestoreBGColor();
                    IndentLevel = indent;

                    int count;
                    if (IpegiInspectionChain.TryGetValue(pgi, out count))
                    {
                        if (count < 2)
                            IpegiInspectionChain.Remove(pgi);
                        else
                            IpegiInspectionChain[pgi] = count - 1;
                    }
                }
                else
                    "3rd recursion".PegiLabel().WriteWarning();

                FoldoutManager.isFoldedOutOrEntered = isFOOE;

                return changed;
            }
        }

        private static ChangesToken Nested_Inspect_Internal<T>(ref T pgi, EnterExitContext context, bool writeWhenNeedsAttention = true) where T : IPEGI_Context
        {
            using (PegiEditorOnly.InspectorStarted ? null : PegiEditorOnly.StartInspector(pgi))
            {

                if (pgi == null)
                {
                    "NULL".PegiLabel().WriteWarning().Nl();
                    return ChangesToken.False;
                }

                var changed = ChangeTrackStart();

                var isFOOE = FoldoutManager.isFoldedOutOrEntered;

                bool inDic = IpegiContInspectionChain.TryGetValue(pgi, out int recurses);

                if (!inDic || recurses < 4)
                {
                    IpegiContInspectionChain[pgi] = recurses + 1;

                    var indent = IndentLevel;

                    try
                    {
                        pgi.InspectContext(context);
                    }
                    catch (Exception ex)
                    {
                        if (IsExitGUIException(ex))
                            throw ex;
                        else
                            Write_Exception(ex);
                    }

                    if (writeWhenNeedsAttention)
                    {
                        try
                        {
                            (pgi as INeedAttention).TryShow_AttentionMessage();
                        }
                        catch (Exception ex)
                        {
                            Write_Exception(ex);
                        }
                    }

                    RestoreBGColor();
                    IndentLevel = indent;

                    int count;
                    if (IpegiContInspectionChain.TryGetValue(pgi, out count))
                    {
                        if (count < 2)
                            IpegiContInspectionChain.Remove(pgi);
                        else
                            IpegiContInspectionChain[pgi] = count - 1;
                    }
                }
                else
                    "3rd recursion".PegiLabel().WriteWarning();

                FoldoutManager.isFoldedOutOrEntered = isFOOE;

                return changed;
            }
        }


        public static ChangesToken InspectInList_Nested<T>(this T obj, ref int inspected, int current) where T : IPEGI_ListInspect
        {
            if (!EnterInternal.OptionsDrawn(ref inspected, current))
                return ChangesToken.False;

            var change = ChangeTrackStart();

            var il = IndentLevel;

            if (inspected == current)
            {
                if (Icon.Back.Click() | obj.GetNameForInspector().PegiLabel().ClickLabel().Nl())
                    inspected = -1;
                else
                    Try_Nested_Inspect(obj);
            }
            else
            {
                obj.InspectInList(ref inspected, current);
            }

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

        
        public static ChangesToken InspectInList_Nested<T>(this T obj) where T : class, IPEGI_ListInspect
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


            Nl();

            UnIndent();

            return changed;
        }

        public static ChangesToken Inspect_AsInList_Value<T>(ref T obj) where T : IPEGI_ListInspect
        {
            var pgi = obj as IPEGI_ListInspect;
            var ch = ChangeTrackStart();

            if (pgi != null)
            {
                int entered = -1;
                pgi.InspectInList_Nested(ref entered, 0);
                if (ch)
                    obj = (T)pgi;
            }

            Nl();

            UnIndent();

            return ch;
        }

        public static ChangesToken Try_Inspect_AsInList(ref object obj)
        {
            var pgi = obj as IPEGI_ListInspect;
            var ch = ChangeTrackStart();

            if (pgi != null)
            {
                int entered = -1;
                if (pgi.InspectInList_Nested(ref entered, 0))
                    obj = pgi;
            }

            Nl();

            UnIndent();

            return ch;
        }

        public static ChangesToken Try_Inspect_AsInList(ref object obj, ref int entered, int current)
        {
            var pgi = obj as IPEGI_ListInspect;
            var ch = ChangeTrackStart();

            if (pgi != null)
            {
                if (pgi.InspectInList_Nested(ref entered, current))
                    obj = pgi;
            }

            Nl();

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

                Nl();
                UnityEditor.EditorGUI.BeginChangeCheck();

                ed.DrawDefaultInspector();

                return EditorOnly_EndChangeCheck();

            }
#endif


            return ChangesToken.False;

        }

        public static ChangesToken TryDefaultInspect(ref object obj, EnterExitContext context = null)
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

                    Nl();
                    UnityEditor.EditorGUI.BeginChangeCheck();
                    ed.DrawDefaultInspector();


                    return EditorOnly_EndChangeCheck();
                }
            }
#endif

            if (obj != null && obj is string)
            {
                var txt = obj as string;
                if (Edit_Big(ref txt, 40))
                {
                    obj = txt;
                    return ChangesToken.True;
                }
            }
            else
            {
                TryReflectionInspect(obj, context);
            }

            return ChangesToken.False;

        }

        private static readonly List<object> _reflectiveInspectionDepth = new();
        private static readonly LoopLock _reflectiveInspectLoopLock = new();


        public static ChangesToken TryReflectionInspect(object obj, UnityEngine.Object objectToSetDirty,  EnterExitContext context = null) 
        {
            var changes = TryReflectionInspect(obj, context);

            if (changes && objectToSetDirty)
                objectToSetDirty.SetToDirty();

            return changes;
        }

        public static ChangesToken TryReflectionInspect(object obj, EnterExitContext unstartedContext = null)
        {
            var changes = ChangeTrackStart();

            Nl();

            using (_reflectiveInspectLoopLock.Unlocked ? _reflectiveInspectLoopLock.Lock() : null)
            {
                try
                {
                    TryReflectionInspectFields_Inernal(obj, unstartedContext);
                }
                catch (Exception ex)
                {
                    if (IsExitGUIException(ex))
                        throw (ex);
                    else
                        Write_Exception(ex);
                }
            }

            if (_reflectiveInspectLoopLock.Unlocked)
            {
                _reflectiveInspectionDepth.Clear();
            }

            return changes;
        }

        private static void TryReflectionInspectFields_Inernal(object obj, EnterExitContext context = null)
        {
            if (obj == null)
            {
                //"NULL Object".PegiLabel().WriteWarning().Nl();
                return;
            }

            using (context?.StartContext())
            {
                foreach (FieldInfo prop in obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    //if (prop.GetCustomAttribute<HideInInspector>() != null)
                      //  continue;

                    string name = prop.Name;

                    name = name.Replace("k__BackingField", "");

                    Type type = prop.FieldType;
                    object value = prop.GetValue(obj);

                    TryReflectionInspectElement(name, type, value, parentObject: obj, prop, context);
                }
            }
        }

        private static void TryReflectionInspectElement( string name, Type type, object value, object parentObject = null, FieldInfo prop = null, EnterExitContext context = null) 
        {
            bool IsEntered() => context != null && context.IsAnyEntered;

            if (value == null)
            {
                if (!IsEntered())
                    "NULL {0} ({1})".F(name, type.ToPegiStringType()).PegiLabel(Styles.BaldText).Nl();

                return;
            }

            if (type == typeof(string))
            {
                if (IsEntered())
                    return;

                var val = value as string;

                if (name.PegiLabel().Edit(ref val).Nl())
                {
                    prop?.SetValue(parentObject, val);
                }

                return;
            }

            if (type == typeof(byte[]))
            {
                if (IsEntered())
                    return;

                var val = QcSharp.ByteArrayToString(value as byte[]);

                if (val.Length > 32)
                    name.PegiLabel().Edit_Big(ref val).Nl();
                else
                    "{0} = {1}".F(name, val).PegiLabel().Nl();

                return;
            }

            if (type.IsEnum)
            {
                if (IsEntered())
                    return;

                Write(name.PegiLabel(), 0.33f);

                var underType = Enum.GetUnderlyingType(type);

                if (underType == typeof(int))
                {
                    var asInt = (int)value;

                    if (Edit_Enum(type, ref asInt).Nl() && prop != null)
                        prop.SetValue(parentObject, asInt);
                } else 
                {
                    "{0} ({1})".F(value, underType).PegiLabel().Nl();
                }

                return;
            }

            if (!type.IsPrimitive)
            {
                if (_reflectiveInspectionDepth.Count > 32)
                {
                    "Recursion LImit Reached: {0}".F(_reflectiveInspectionDepth).PegiLabel().WriteWarning();
                    return;
                }

                if (_reflectiveInspectionDepth.Contains(value))
                {
                    "Recursive reference to {0} = {1}".F(name, value.ToString()).PegiLabel().Nl();
                    return;
                }

                _reflectiveInspectionDepth.Add(value);

                try
                {
                    if (typeof(EnterExitContext).IsAssignableFrom(type))
                        return;

                    if (typeof(CollectionInspectorMeta).IsAssignableFrom(type))
                        return;

                    var isCollection = typeof(ICollection).IsAssignableFrom(type);

                    if (isCollection)
                    {
                        InspectCollection();
                        return;
                    }

                    InspectAsClass();

                    return;

                }
                catch (Exception ex)
                {
                    if (IsExitGUIException(ex))
                        throw (ex);
                    else
                        Write_Exception(ex);
                }

                _reflectiveInspectionDepth.Remove(value);

                return;
            }

            if (IsEntered())
                return;

            if (type == typeof(bool))
            {
                var val = (bool)value;

                if (name.PegiLabel().Toggle(ref val).Nl() && prop != null)
                    prop.SetValue(parentObject, val);

                return;
            }

            if (type == typeof(int))
            {
                var val = (int)value;

                if (name.PegiLabel().Edit(ref val).Nl() && prop != null)
                    prop.SetValue(parentObject, val);

                return;
            }

            if (type == typeof(long))
            {
                var val = (long)value;

                if (name.PegiLabel().Edit(ref val).Nl() && prop != null)
                    prop.SetValue(parentObject, val);

                return;
            }

            if (type == typeof(double))
            {
                var val = (double)value;

                if (name.PegiLabel().Edit(ref val).Nl() && prop != null)
                    prop.SetValue(parentObject, val);

                return;
            }

            if (type == typeof(float))
            {
                var val = (float)value;

                if (name.PegiLabel().Edit(ref val).Nl() && prop != null)
                    prop.SetValue(parentObject, val);

                return;
            }
            
            "{0} = {1}".F(name, value).PegiLabel().Nl();

            return;

            void InspectAsClass()
            {
                var asPgi = value as IPEGI;

                if (context != null)
                {
                    if (asPgi != null)
                    {
                        asPgi.Enter_Inspect().Nl();
                    }
                    else
                    {
                        if ("{0}: {1}".F(name, value.ToString()).PegiLabel().IsEntered().Nl())
                            TryReflectionInspectFields_Inernal(value);
                    }
                }
                else
                {
                    "{0}: {1}".F(name, value.GetNameForInspector()).PegiLabel(Styles.BaldText).Nl();

                    using (Indent())
                    {
                        if (asPgi != null)
                        {
                            asPgi.Nested_Inspect();
                        }
                        else
                        {
                            TryReflectionInspectFields_Inernal(value);
                        }
                    }
                }
            }

            void InspectCollection()
            {
                var col = value as ICollection;

                TextLabel listlabel = "{0} [{1} elements]".F(name, col.Count).PegiLabel(Styles.ListLabel);

                if (context != null)
                {
                    if (!listlabel.IsEntered().Nl())
                        return;
                }
                else
                    listlabel.Nl();

                const int MAX_ELEMENTS_TO_SHOW = 64;

                int counter = MAX_ELEMENTS_TO_SHOW;
                int index = 0;

                foreach (var el in col)
                {
                    if (el != null)
                        TryReflectionInspectElement(name: index.ToString(), type: el.GetType(), el);
                    else
                        "{0} = NULL".PegiLabel().Nl();
                    //TryReflectionInspectFields_Inernal(el);

                    Nl();

                    counter--;
                    if (counter <= 0)
                    {
                        "+ {0} elements".F(col.Count - MAX_ELEMENTS_TO_SHOW).PegiLabel().Write_Hint().Nl();
                        break;
                    }
                }
            }
        }

        public static ChangesToken Try_Nested_Inspect(object obj)
        {
            var pgi = obj as IPEGI;
            var ch = pgi?.Nested_Inspect() ?? TryDefaultInspect(ref obj);

            Nl();

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

            if (uobj != null)
                return !uobj;

            return obj == null;
        }

        private static bool IsDefaultOrNull<T>(this T obj) => (obj == null) || EqualityComparer<T>.Default.Equals(obj, default) || (obj is string && (obj as string).IsNullOrEmpty());

        public static void AddOrReplaceByIGotIndex<T>(this List<T> list, T newElement) where T : IGotIndex
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
            if (obj.ToPegiStringInterfacePart(out tmp)) 
                return tmp;

            var mbeh = obj as MonoBehaviour;
            if (mbeh)
                return obj.ToString();

            var cmp = obj as Component;
            return cmp ? "{0} ({1})".F(cmp.gameObject.name, cmp.GetType().ToPegiStringType()) : obj.name;
        }

        private static bool TryProcessKeyValuePair(object value, Action<object, object> ifPairAction) 
        {
            if (value != null)
            {
                Type valueType = value.GetType();
                if (valueType.IsGenericType)
                {
                    Type baseType = valueType.GetGenericTypeDefinition();
                    if (baseType == typeof(KeyValuePair<,>))
                    {
                        object kvpKey = valueType.GetProperty("Key").GetValue(value, null);
                        object kvpValue = valueType.GetProperty("Value").GetValue(value, null);

                        ifPairAction.Invoke(kvpKey, kvpValue);
                        return true;
                    }
                }
            }

            return false;
        }

        public static string GetNameForInspector<T>(this T obj)
        {
            if (obj.IsNullOrDestroyed_Obj())
                return "NULL ({0})".F(typeof(T).ToPegiStringType());

            var type = obj.GetType();

            if (type.IsClass)
            {
                if (obj is string)
                {
                    var str = obj as string;

                    if (str.IsNullOrEmpty())
                        return "";

                    // The following causes issues when F is used to generate request string
                    /*
                    if (str == null)
                        return "NULL String";

                    if (str.Length == 0)
                        return "Empty string";*/

                    return str;
                }

                if (obj.GetType().IsUnityObject())
                    return (obj as Object).GetNameForInspector_Uobj();

                return DefaultName();
            }

            string pairName = null;
            if (TryProcessKeyValuePair(obj, (key, val) => 
            {
                string keyName = key.GetNameForInspector();
                string valName = val.GetNameForInspector();

                if (valName.Contains(keyName))
                    pairName = valName;
                else 
                    pairName = "({0}: {1})".F(keyName, valName);
            }))
                return pairName;

            if (type.IsEnum)
            {
                return QcSharp.AddSpacesToSentence(obj.ToString(), preserveAcronyms: true);
            }

            if (!type.IsPrimitive)
            {
                return DefaultName(); 
            }

            return obj.ToString();

            string DefaultName() 
            {
                string tmp;
                if (obj.ToPegiStringInterfacePart(out tmp))
                    return tmp;

                try
                {
                    string typeName = obj.ToString(); // QcSharp.AddSpacesToSentence(obj.ToString(), preserveAcronyms: true);

                    var cnt = obj as IGotCount;

                    if (cnt != null)
                    {
                        typeName += " [{0}]".F(cnt.GetCount());
                    }

                    return typeName;
                } catch (Exception ex) 
                {
                    return "Error Getting name. " + ex.ToString();
                }
            }

        }

        public static T GetByIGotName<T>(this List<T> lst, string name) where T : IGotName
        {

            if (lst != null)
                foreach (var el in lst)
                    if (!el.IsNullOrDestroyed_Obj() && el.NameForInspector.SameAs(name))
                        return el;


            return default;
        }

        internal static V TryGetElementByIndex<T, V>(this Dictionary<T, V> list, int index, V defaultValue = default)
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

        private static readonly Dictionary<IPEGI, int> IpegiInspectionChain = new();
        private static readonly Dictionary<IPEGI_Context, int> IpegiContInspectionChain = new();

        internal static void ResetInspectedChain()
        {
            IpegiInspectionChain.Clear();
            IpegiContInspectionChain.Clear();
        }
#if UNITY_EDITOR
        private static readonly Dictionary<Object, UnityEditor.Editor> defaultEditors = new();
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

        public static bool TryGetAttentionMessage<T>(this T attention, out string warningMsg, bool canBeNull = false) where T: INeedAttention
        {
            warningMsg = null;

            if (attention.IsNullOrDestroyed_Obj())
            {
                if (canBeNull)
                    return false;

                warningMsg = "{0} is null".F(typeof(T).ToPegiStringType());

            }
            else
            {
                try
                {
                    warningMsg = attention.NeedAttention();
                } catch (Exception ex) 
                {
                    Debug.LogException(ex);
                } 

                if (!warningMsg.IsNullOrEmpty())
                    warningMsg = "{0}: {1}".F(attention.GetNameForInspector(), warningMsg);
            }

            return !warningMsg.IsNullOrEmpty();
        }

        public static void Write_Exception(Exception ex)
        {
            if (IsExitGUIException(ex))
                throw ex;
            
            QcLog.ChillLogger.LogExceptionExpOnly(ex, key: "InspEx");

            Nl();
            if (Icon.Debug.Click(toolTip: "Log Exception"))
                Debug.LogException(ex);

            ex.StackTrace.PegiLabel().Write_ForCopy_Big(showCopyButton: true, lines: 10).Nl();
        }
    }
}