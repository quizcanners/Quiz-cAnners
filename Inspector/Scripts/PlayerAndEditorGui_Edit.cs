using System.Collections.Generic;
using QuizCanners.Utils;
using UnityEngine;

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable IDE0011 // Add braces
#pragma warning disable IDE0008 // Use explicit type

namespace QuizCanners.Inspect
{
    public static partial class pegi
    {

        #region Changes 

        public static ChangesTracker ChangeTrackStart() => new ChangesTracker();
        
        private static ChangesToken SetChangedTrue_Internal() { PegiEditorOnly.globChanged = true; return ChangesToken.True; }

        private static ChangesToken FeedChanges_Internal(this bool changed, LatestInteractionEvent evnt)
        {
            if (changed)
            {
                GameView.LatestEvent = evnt;
                PegiEditorOnly.globChanged = true;
            }

            return new ChangesToken(changed);
        }

        private static ChangesToken IgnoreChanges(this ChangesToken changed, LatestInteractionEvent evnt)
        {
            if (changed)
            {
                PegiEditorOnly.globChanged = false;
                GameView.LatestEvent = evnt;
            }
            return changed;
        }

        public static ChangesToken OnChanged(this ChangesToken changed, System.Action onChanged) 
        {
            if (changed)
                onChanged?.Invoke();
            return changed;
        }

        public static ChangesToken IgnoreChanges(this ChangesToken changed)
        {
            if (changed)
                PegiEditorOnly.globChanged = false;
            return changed;
        }

        private static bool wasChangedBefore;

        private static void _START()
        {
            checkLine();
            wasChangedBefore = GUI.changed;
        }

        private static ChangesToken _END() => new ChangesToken(PegiEditorOnly.globChanged |= GUI.changed && !wasChangedBefore);

        

        #endregion

        #region Edit


#region Vectors & Rects

        public static ChangesToken edit(this TextLabel label, ref Quaternion qt)
        {
            var eul = qt.eulerAngles;

            if (edit(label, ref eul))
            {
                qt.eulerAngles = eul;
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }

        public static ChangesToken edit(this TextToken label, ref Quaternion qt)
        {
            label.write();
            return edit(ref qt);
        }

        public static ChangesToken edit(ref Quaternion qt)
        {
            var eul = qt.eulerAngles;

            if (edit(ref eul))
            {
                qt.eulerAngles = eul;
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }
        
        public static ChangesToken edit(ref Vector4 val)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.edit(ref val);
#endif

            return "X".PegiLabel(15).edit(ref val.x) | "Y".PegiLabel(15).edit(ref val.y) | "Z".PegiLabel(15).edit(ref val.z) | "W".PegiLabel(15).edit(ref val.w);

        }

        public static ChangesToken edit01(this TextLabel label, ref Rect val)
        {
            label.write();
            return edit01(ref val);
        }

        public static ChangesToken edit01(ref float val) => edit(ref val, 0, 1);

        public static ChangesToken edit01(this TextLabel label, ref float val)
        {
            label.ApproxWidth().write();
            return edit(ref val, 0, 1);
        }
        public static ChangesToken edit01(ref Rect val)
        {
            var center = val.center;
            var size = val.size;

            if (
                "X".PegiLabel(30).edit01(ref center.x).nl() |
                "Y".PegiLabel(30).edit01(ref center.y).nl() |
                "W".PegiLabel(30).edit01(ref size.x).nl() |
                "H".PegiLabel(30).edit01(ref size.y).nl())
            {
                var half = size * 0.5f;
                val.min = center - half;
                val.max = center + half;
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }

        public static ChangesToken edit(this TextLabel label, ref Rect val)
        {
            var v4 = val.ToVector4(true);

            if (label.edit(ref v4))
            {
                val = v4.ToRect(true);
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }

        public static ChangesToken edit(ref RectOffset val, int min, int max)
        {
            int top = val.top;
            int bottom = val.bottom;
            int left = val.left;
            int right = val.right;

            if (
                "Left".PegiLabel(70).edit(ref left, min, max).nl() |
                "Right".PegiLabel(70).edit(ref right, min, max).nl() |
                "Top".PegiLabel(70).edit(ref top, min, max).nl() |
                "Bottom".PegiLabel(70).edit(ref bottom, min, max).nl())
            {
                val = new RectOffset(left: left, right: right, top: top, bottom: bottom);

                return ChangesToken.True;
            }

            return ChangesToken.False;
        }

        public static ChangesToken edit(ref RectOffset val)
        {
            int top = val.top;
            int bottom = val.bottom;
            int left = val.left;
            int right = val.right;

            if (
                "Left".PegiLabel(70).edit(ref left).nl() |
                "Right".PegiLabel(70).edit(ref right).nl() |
                "Top".PegiLabel(70).edit(ref top).nl() |
                "Bottom".PegiLabel(70).edit(ref bottom).nl())
            {
                val = new RectOffset(left: left, right: right, top: top, bottom: bottom);

                return ChangesToken.True;
            }

            return ChangesToken.False;
        }
        
        public static ChangesToken edit(this TextLabel label, ref Vector4 val)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.edit(label, ref val);
#endif

            label.TryWrite();
            return
                edit(ref val.x) |
                edit(ref val.y) |
                edit(ref val.z) |
                edit(ref val.w);

        }

        public static ChangesToken edit(ref Vector3 val) =>
           "X".PegiLabel(15).edit(ref val.x) | "Y".PegiLabel(15).edit(ref val.y) | "Z".PegiLabel(15).edit(ref val.z);

        public static ChangesToken edit(this TextLabel label, ref Vector3 val)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.edit(label, ref val);
#endif

            write(label);
            nl();
            return edit(ref val);
        }

        public static ChangesToken edit(this TextLabel label, ref Vector2 val)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.edit(label, ref val);

#endif
            label.TryWrite();
            return edit(ref val.x) | edit(ref val.y);
        }

        public static ChangesToken edit01(this TextLabel label, ref Vector2 val)
        {
            label.ApproxWidth().nl();
            return edit01(ref val);
        }

       

        public static ChangesToken edit01(ref Vector2 val) =>
            "X".PegiLabel(10).edit01(ref val.x).nl() |
            "Y".PegiLabel(10).edit01(ref val.y).nl();

        public static ChangesToken edit(this TextLabel label, ref Vector2 val, float min, float max)
        {
            "{0} [X: {1} Y: {2}]".F(label, val.x.RoundTo(2), val.y.RoundTo(2)).PegiLabel().nl();
            return edit(ref val, min, max);
        }

        public static ChangesToken edit(ref Vector2 val, float min, float max) =>
            "X".PegiLabel(10).edit(ref val.x, min, max) |
            "Y".PegiLabel(10).edit(ref val.y, min, max);

        public static ChangesToken edit(this TextLabel label, ref Vector2Int val)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.edit(label, ref val);

#endif
            label.ApproxWidth().write();
            return edit(ref val);
        }

        public static ChangesToken edit(ref Vector2Int val) 
        {
            var x = val.x;
            var y = val.y;
            if ("X".PegiLabel(35).edit(ref x) | "Y".PegiLabel(35).edit(ref y))
            {
                val = new Vector2Int(x, y);
                return ChangesToken.True;
            }
            return ChangesToken.False;
        }
     
        #endregion

        #region Color

        public static ChangesToken edit(ref Color32 col)
        {
            Color tcol = col;
            if (edit(ref tcol))
            {
                col = tcol;
                return ChangesToken.True;
            }
            return ChangesToken.False;
        }

        public static ChangesToken edit(ref Color col)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.edit(ref col);

#endif
     
            using (SetBgColorDisposable(col))
            {
                if ("Color".PegiLabel().isFoldout())
                {
                    nl();

                    return icon.Red.edit_ColorChannel(ref col, 0).nl() |
                           icon.Green.edit_ColorChannel(ref col, 1).nl() |
                           icon.Blue.edit_ColorChannel(ref col, 2).nl() |
                           icon.Alpha.edit_ColorChannel(ref col, 3).nl();
                }
            }

            return ChangesToken.False;
        }

        public static ChangesToken edit(ref Color col, int width)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.edit(ref col, width);

#endif
            return ChangesToken.False;
        }

        public static ChangesToken edit_ColorChannel(this icon ico, ref Color col, int channel)
        {
            var changed = ChangeTrackStart();

            if (channel < 0 || channel > 3)
                "Color has no channel {0} ".F(channel).PegiLabel().writeWarning();
            else
            {
                var chan = col[channel];

                if (ico.edit(ref chan, 0, 1))
                    col[channel] = chan;

            }

            return changed;
        }

        public static ChangesToken edit_ColorChannel(this TextLabel label, ref Color col, int channel)
        {
            var changed = ChangeTrackStart();

            if (channel < 0 || channel > 3)
                "{0} color does not have {1}'th channel".F(label, channel).PegiLabel().writeWarning();
            else
            {
                var chan = col[channel];

                if (label.edit(ref chan, 0, 1))
                    col[channel] = chan;

            }

            return changed;
        }

        public static ChangesToken edit(this TextLabel label, ref Color col)
        {
            if (PaintingGameViewUI)
            {
                if (label.isFoldout())
                    return edit(ref col);
            }
            else
            {
                write(label);
                return edit(ref col);
            }

            return ChangesToken.False;
        }

        #endregion



        #region Enum

        public static ChangesToken editEnum<T>(ref T current, System.Func<T, object> nameGetter, int width)
        {
            return editEnum_Internal(ref current, typeof(T), nameGetter, width: width);
        }

        public static ChangesToken editEnum<T>(this TextLabel label, ref T current, int valueWidth)
        {
            label.TryWrite();
            return editEnum(ref current, width: valueWidth);
        }

        public static ChangesToken editEnum<T>(this TextLabel label, ref T current, System.Func<T, object> nameGetter, int width = -1)
        {
            label.write();
            return editEnum_Internal(ref current, typeof(T), nameGetter, width: width);
        }

        public static ChangesToken editEnum<T>(this TextLabel text, ref T eval)
        {
            text.write();
            return editEnum(ref eval);
        }


        public static ChangesToken editEnum<T>(this TextLabel label, ref int current, List<int> options)
        {
            label.TryWrite();
            return editEnum_Internal<T>(ref current, options);
        }

        public static ChangesToken editEnum<T>(ref T eval, int width = -1)
        {
            var val = System.Convert.ToInt32(eval);

            if (editEnum_Internal(ref val, typeof(T), width))
            {
                eval = (T)((object)val);
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }

        private static ChangesToken editEnum<T>(ref T eval, List<int> options, int width = -1)
        {
            var val = System.Convert.ToInt32(eval);

            if (editEnum_Internal(ref val, typeof(T), options, width))
            {
                eval = (T)((object)val);
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }

        public static ChangesToken editEnum<T>(this TextLabel label, ref int current, int valueWidth = -1)
        {
            label.write();
            return editEnum_Internal(ref current, typeof(T), width: valueWidth);
        }

        public static ChangesToken editEnum<T>(ref int current, int width = -1) => editEnum_Internal(ref current, typeof(T), width: width);

        private static ChangesToken editEnum_Internal<T>(ref int eval, List<int> options, int width = -1)
            => editEnum_Internal(ref eval, typeof(T), options, width);

        private static ChangesToken editEnum_Internal(ref int current, System.Type type, int width = -1, bool showIndex = false)
        {
            checkLine();
            var tmpVal = -1;

            string[] names = System.Enum.GetNames(type);
            int[] val = (int[])System.Enum.GetValues(type);

            for (var i = 0; i < val.Length; i++)
            {
                var name = QcSharp.AddSpacesToSentence(names[i]);

                if (showIndex && !name.Contains(val[i].ToString()))
                    names[i] = "{0}:".F(val[i]) + name;
                else
                    names[i] = name;
                
                if (val[i] == current)
                    tmpVal = i;
            }

            if (!select(ref tmpVal, names, width)) 
                return ChangesToken.False;

            current = val[tmpVal];
            return ChangesToken.True;
        }

        private static ChangesToken editEnum_Internal<T>(ref T value, System.Type type, System.Func<T, object> nameGetter, int width = -1, bool showIndex = false)
        {

           // var current = System.Convert.ToInt32(value);

            checkLine();
            var tmpVal = -1;

            var names = System.Enum.GetNames(type);
            var val = (T[])System.Enum.GetValues(type);

            for (var i = 0; i < val.Length; i++)
            {
               // if (names[i].Contains(val[i].ToString()) == false)
               // {
                    string name;

                    try 
                    {
                        name = nameGetter.Invoke(val[i]).GetNameForInspector();
                    } catch  
                    {
                        name = "!Errr! " + names[i];   
                    }

                    names[i] = showIndex ? ("{0}:".F(val[i]) + name) : name;
               // }
                if (val[i].Equals(value))
                    tmpVal = i;
            }

            if (!select(ref tmpVal, names, width)) return ChangesToken.False;

            value = val[tmpVal];
            return ChangesToken.True;
        }

        private static ChangesToken editEnum_Internal(ref int current, System.Type type, List<int> options, int width = -1)
        {
            checkLine();
            var tmpVal = -1;

            List<string> names = new List<string>(options.Count + 1);

            for (var i = 0; i < options.Count; i++)
            {
                var option = options[i];
                names.Add("{0}:".F(option)+ System.Enum.GetName(type, option));
                if (options[i] == current)
                    tmpVal = i;
            }

            if (width == -1 ? select(ref tmpVal, names) : select_Index(ref tmpVal, names, width))
            {
                current = options[tmpVal];
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }
        
#endregion

#region Enum Flags

        public static ChangesToken editEnumFlags<T>(this TextLabel text, ref T eval)
        {
            write(text);
            return editEnumFlags(ref eval);
        }

        public static ChangesToken editEnumFlags<T>(ref T eval, int width = -1)
        {
            var val = System.Convert.ToInt32(eval);

            if (editEnumFlags(ref val, typeof(T), width))
            {
                eval = (T)((object)val);
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }

        private static ChangesToken editEnumFlags(ref int current, System.Type type, int width = -1)
        {

            checkLine();

            var names = System.Enum.GetNames(type);
            var values = (int[])System.Enum.GetValues(type);

            Countless<string> sortedNames = new Countless<string>();

            int currentPower = 0;

            int toPow = 1;

            for (var i = 0; i < values.Length; i++)
            {
                var val = values[i];
                while (val > toPow)
                {
                    currentPower++;
                    toPow = (int)Mathf.Pow(2, currentPower);
                }

                if (val == toPow)
                    sortedNames[currentPower] = names[i];
            }

            string[] snms = new string[currentPower + 1];

            for (int i = 0; i <= currentPower; i++)
                snms[i] = sortedNames[i];

            return selectFlags(ref current, snms, width);
        }
#endregion

#endregion
        
    }
}