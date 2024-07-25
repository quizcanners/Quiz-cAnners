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
        
        private static ChangesToken SetChangedTrue_Internal() { globChanged = true; return ChangesToken.True; }

        private static ChangesToken FeedChanges_Internal(this bool changed, LatestInteractionEvent evnt)
        {
            if (changed)
            {
                GameView.LatestEvent = evnt;
                globChanged = true;
            }

            return new ChangesToken(changed);
        }

        private static ChangesToken IgnoreChanges(this ChangesToken changed, LatestInteractionEvent evnt)
        {
            if (changed)
            {
                globChanged = false;
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
                globChanged = false;
            return changed;
        }

        private static bool wasChangedBefore;

        private static void _START()
        {
            CheckLine();
            wasChangedBefore = GUI.changed;
        }

        private static ChangesToken _END()
        {
            if (!wasChangedBefore) 
            {
                globChanged |= GUI.changed;
            }

            return new ChangesToken(globChanged && !wasChangedBefore);
        }
        #endregion

        #region Edit

        #region Vectors & Rects

        public static ChangesToken Edit(this TextLabel label, ref AnimationCurve curve)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Edit(label, ref curve);
#endif  

            label.Write();
            return ChangesToken.False;
        }


        public static ChangesToken Edit(this TextLabel label, ref Quaternion qt)
        {
            var eul = qt.eulerAngles;

            if (Edit(label, ref eul))
            {
                qt.eulerAngles = eul;
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }

       /* public static ChangesToken Edit(this TextLabel label, ref Quaternion qt)
        {
            Write(label, 0.33f);
            return Edit(ref qt);
        }*/

        public static ChangesToken Edit(ref Quaternion qt)
        {
            var eul = qt.eulerAngles;

            if (Edit(ref eul))
            {
                qt.eulerAngles = eul;
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }
        
        public static ChangesToken Edit(ref Vector4 val)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Edit(ref val);
#endif
            return "X".PegiLabel(15).Edit(ref val.x) | "Y".PegiLabel(15).Edit(ref val.y) | "Z".PegiLabel(15).Edit(ref val.z) | "W".PegiLabel(15).Edit(ref val.w);
        }

        public static ChangesToken Edit_01(ref float val) => Edit(ref val, 0, 1);
        public static ChangesToken Edit_N1_1(ref float val) => Edit(ref val, -1, 1);

        public static ChangesToken Edit_01(this TextLabel label, ref float val)
        {
            Write(label, 0.33f);
            return Edit(ref val, 0, 1);
        }

        public static ChangesToken Edit_N1_1(this TextLabel label, ref float val)
        {
            Write(label, 0.33f);
            return Edit(ref val, -1, 1);
        }

        public static ChangesToken Edit_01(ref Rect val)
        {
            var center = val.center;
            var size = val.size;

            if (
                "X".PegiLabel(30).Edit_01(ref center.x).Nl() |
                "Y".PegiLabel(30).Edit_01(ref center.y).Nl() |
                "W".PegiLabel(30).Edit_01(ref size.x).Nl() |
                "H".PegiLabel(30).Edit_01(ref size.y).Nl())
            {
                var half = size * 0.5f;
                val.min = center - half;
                val.max = center + half;
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }

        public static ChangesToken Edit_01(this TextLabel label, ref Vector2 val)
        {
            Write(label).Nl();
            using (Indent(2))
            {
                return Edit_01(ref val);
            }
        }

        public static ChangesToken Edit_N1_1(this TextLabel label, ref Vector2 val)
        {
            Write(label).Nl();
            using (Indent(2))
            {
                return Edit_N1_1(ref val);
            }
        }


        public static ChangesToken Edit_01(ref Vector2 val) =>
            "X".PegiLabel(20).Edit_01(ref val.x).Nl() |
            "Y".PegiLabel(20).Edit_01(ref val.y).Nl();

        public static ChangesToken Edit_N1_1(ref Vector2 val) =>
            "X".PegiLabel(20).Edit_N1_1(ref val.x).Nl() |
            "Y".PegiLabel(20).Edit_N1_1(ref val.y).Nl();

        public static ChangesToken Edit(this TextLabel label, ref Rect val)
        {
            var v4 = val.ToVector4(true);

            if (label.Edit(ref v4))
            {
                val = v4.ToRect(true);
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }

        public static ChangesToken Edit(ref RectOffset val, int min, int max)
        {
            int top = val.top;
            int bottom = val.bottom;
            int left = val.left;
            int right = val.right;

            if (
                "Left".PegiLabel(70).Edit(ref left, min, max).Nl() |
                "Right".PegiLabel(70).Edit(ref right, min, max).Nl() |
                "Top".PegiLabel(70).Edit(ref top, min, max).Nl() |
                "Bottom".PegiLabel(70).Edit(ref bottom, min, max).Nl())
            {
                val = new RectOffset(left: left, right: right, top: top, bottom: bottom);

                return ChangesToken.True;
            }

            return ChangesToken.False;
        }

        public static ChangesToken Edit(ref RectOffset val)
        {
            int top = val.top;
            int bottom = val.bottom;
            int left = val.left;
            int right = val.right;

            if (
                "Left".PegiLabel(70).Edit(ref left).Nl() |
                "Right".PegiLabel(70).Edit(ref right).Nl() |
                "Top".PegiLabel(70).Edit(ref top).Nl() |
                "Bottom".PegiLabel(70).Edit(ref bottom).Nl())
            {
                val = new RectOffset(left: left, right: right, top: top, bottom: bottom);

                return ChangesToken.True;
            }

            return ChangesToken.False;
        }
        
        public static ChangesToken Edit(this TextLabel label, ref Vector4 val)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Edit(label, ref val);
#endif

            Write(label, 0.33f);
            return
                Edit(ref val.x) |
                Edit(ref val.y) |
                Edit(ref val.z) |
                Edit(ref val.w);

        }

        public static ChangesToken Edit(ref Vector3 val) =>
           "X".PegiLabel(15).Edit(ref val.x) | "Y".PegiLabel(15).Edit(ref val.y) | "Z".PegiLabel(15).Edit(ref val.z);

        public static ChangesToken Edit(this TextLabel label, ref Vector3 val)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
            {
                if (label.GotWidth) 
                {
                    Write(label);
                    return Edit(ref val);
                }

                return PegiEditorOnly.Edit(label, ref val);
            }
#endif

            Write(label, 0.33f);
            Nl();
            return Edit(ref val);
        }

        public static ChangesToken Edit(this TextLabel label, ref Vector2 val)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Edit(label, ref val);

#endif
            Write(label, 0.33f);
            return Edit(ref val.x) | Edit(ref val.y);
        }

        public static ChangesToken Edit(this TextLabel label, ref Vector2 val, float min, float max)
        {
            "{0} [X: {1} Y: {2}]".F(label, val.x.RoundTo(2), val.y.RoundTo(2)).PegiLabel().Nl();
            return Edit(ref val, min, max);
        }

        public static ChangesToken Edit(ref Vector2 val, float min, float max) =>
            "X".PegiLabel(10).Edit(ref val.x, min, max) |
            "Y".PegiLabel(10).Edit(ref val.y, min, max);

        public static ChangesToken Edit(this TextLabel label, ref Vector2Int val)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Edit(label, ref val);

#endif
            Write(label, 0.33f);
            return Edit(ref val);
        }

        public static ChangesToken Edit(ref Vector2Int val) 
        {
            var x = val.x;
            var y = val.y;
            if ("X".PegiLabel(35).Edit(ref x) | "Y".PegiLabel(35).Edit(ref y))
            {
                val = new Vector2Int(x, y);
                return ChangesToken.True;
            }
            return ChangesToken.False;
        }
     
        #endregion

        #region Color

        public static ChangesToken Edit(ref Color32 col)
        {
            Color tcol = col;
            if (Edit(ref tcol))
            {
                col = tcol;
                return ChangesToken.True;
            }
            return ChangesToken.False;
        }

        public static ChangesToken Edit(ref Color col)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Edit(ref col);

#endif
     
            using (SetBgColorDisposable(col))
            {
                if ("Color".PegiLabel().IsFoldout())
                {
                    Nl();

                    return Icon.Red.Edit_ColorChannel(ref col, 0).Nl() |
                           Icon.Green.Edit_ColorChannel(ref col, 1).Nl() |
                           Icon.Blue.Edit_ColorChannel(ref col, 2).Nl() |
                           Icon.Alpha.Edit_ColorChannel(ref col, 3).Nl();
                }
            }

            return ChangesToken.False;
        }

        public static ChangesToken Edit(ref Color col, int width)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Edit(ref col, width);

#endif
            return ChangesToken.False;
        }

        public static ChangesToken Edit_ColorChannel(this Icon ico, ref Color col, int channel)
        {
            var changed = ChangeTrackStart();

            if (channel < 0 || channel > 3)
                "Color has no channel {0} ".F(channel).PegiLabel().WriteWarning();
            else
            {
                var chan = col[channel];

                if (ico.Edit(ref chan, 0, 1))
                    col[channel] = chan;

            }

            return changed;
        }

        public static ChangesToken Edit_ColorChannel(this TextLabel label, ref Color col, int channel)
        {
            var changed = ChangeTrackStart();

            if (channel < 0 || channel > 3)
                "{0} color does not have {1}'th channel".F(label, channel).PegiLabel().WriteWarning();
            else
            {
                var chan = col[channel];

                if (label.Edit(ref chan, 0, 1))
                    col[channel] = chan;

            }

            return changed;
        }

        public static ChangesToken Edit(this TextLabel label, ref Color col, bool showEyeDropper = true, bool showAlpha = true, bool hdr = false)
        {
            if (PaintingGameViewUI)
            {
                if (label.IsFoldout())
                    return Edit(ref col);
            }
            else
            {
                //Write(label, 0.33f);
#if UNITY_EDITOR
                return PegiEditorOnly.Edit(label, ref col, showEyeDropper: showEyeDropper, showAlpha: showAlpha, hdr: hdr);
#endif
            }

            return ChangesToken.False;
        }

        #endregion

        #region Enum

        public static ChangesToken Edit_Enum<T>(ref T current, System.Func<T, object> nameGetter, int width)
        {
            return EditEnum_Internal(ref current, typeof(T), nameGetter, width: width);
        }

        public static ChangesToken Edit_Enum<T>(this TextLabel label, ref T current, int valueWidth)
        {
            Write(label, defaultWidthFraction: 0.33f);
            return Edit_Enum(ref current, width: valueWidth);
        }

        public static ChangesToken Edit_Enum<T>(this TextLabel label, ref T current, System.Func<T, object> nameGetter, int valueWidth = -1)
        {
            Write(label, defaultWidthFraction: 0.33f);
            return EditEnum_Internal(ref current, typeof(T), nameGetter, width: valueWidth);
        }

        public static ChangesToken Edit_Enum<T>(this TextLabel text, ref T eval)
        {
            Write(text, defaultWidthFraction: 0.33f);
            return Edit_Enum(ref eval);
        }

        public static ChangesToken Edit_Enum<T>(this TextLabel label, ref int current, List<int> options)
        {
            Write(label, defaultWidthFraction: 0.33f);
            return EditEnum_Internal<T>(ref current, options);
        }

        public static ChangesToken Edit_Enum<T>(ref T eval, int width = -1)
        {
            try
            {
                var val = System.Convert.ToInt32(eval);

                if (EditEnum_Internal(ref val, typeof(T), width))
                {
                    eval = (T)((object)val);
                    return ChangesToken.True;
                }

                return ChangesToken.False;

            } catch (System.Exception ex)
            {
                "Can't convert {0} to Enum".F(typeof(T).Name).PegiLabel().WriteWarning().Nl();
                if ("Log to Console".PegiLabel().Click())
                    Debug.LogException(ex);

                return ChangesToken.False;
            }
        }

        private static ChangesToken Edit_Enum<T>(ref T eval, List<int> options, int width = -1)
        {
            var val = System.Convert.ToInt32(eval);

            if (EditEnum_Internal(ref val, typeof(T), options, width))
            {
                eval = (T)((object)val);
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }

        public static ChangesToken Edit_Enum<T>(this TextLabel label, ref int current, int valueWidth = -1)
        {
            Write(label, 0.33f);
            return EditEnum_Internal(ref current, typeof(T), width: valueWidth);
        }

        public static ChangesToken Edit_Enum<T>(ref int current, int width = -1) => EditEnum_Internal(ref current, typeof(T), width: width);

        private static ChangesToken EditEnum_Internal<T>(ref int eval, List<int> options, int width = -1)
            => EditEnum_Internal(ref eval, typeof(T), options, width);

        private static ChangesToken EditEnum_Internal(ref int current, System.Type type, int width = -1, bool showIndex = false)
        {
            CheckLine();
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

            if (!Select(ref tmpVal, names, width)) 
                return ChangesToken.False;

            current = val[tmpVal];
            return ChangesToken.True;
        }

        private static ChangesToken EditEnum_Internal<T>(ref T value, System.Type type, System.Func<T, object> nameGetter, int width = -1, bool showIndex = false)
        {

           // var current = System.Convert.ToInt32(value);

            CheckLine();
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

            if (!Select(ref tmpVal, names, width)) return ChangesToken.False;

            value = val[tmpVal];
            return ChangesToken.True;
        }

        private static ChangesToken EditEnum_Internal(ref int current, System.Type type, List<int> options, int width = -1)
        {
            CheckLine();
            var tmpVal = -1;

            List<string> names = new List<string>(options.Count + 1);

            for (var i = 0; i < options.Count; i++)
            {
                var option = options[i];
                names.Add("{0}:".F(option)+ System.Enum.GetName(type, option));
                if (options[i] == current)
                    tmpVal = i;
            }

            if (width == -1 ? Select(ref tmpVal, names) : Select_Index(ref tmpVal, names, width))
            {
                current = options[tmpVal];
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }
        
#endregion

        #region Enum Flags

        public static ChangesToken Edit_EnumFlags<T>(this TextLabel text, ref T eval)
        {
            Write(text);
            return Edit_EnumFlags(ref eval);
        }

        public static ChangesToken Edit_EnumFlags<T>(ref T eval, int width = -1)
        {
            var val = System.Convert.ToInt32(eval);

            if (Edit_EnumFlags(ref val, typeof(T), width))
            {
                eval = (T)((object)val);
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }

        private static ChangesToken Edit_EnumFlags(ref int current, System.Type type, int width = -1)
        {

            CheckLine();

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

            return SelectFlags(ref current, snms, width);
        }
        #endregion

#endregion
        
    }
}