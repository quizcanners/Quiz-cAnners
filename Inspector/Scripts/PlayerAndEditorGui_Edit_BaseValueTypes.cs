using System;
using System.Collections.Generic;
using System.IO;
using QuizCanners.Utils;
using UnityEngine;
namespace QuizCanners.Inspect
{
    public static partial class pegi
    {
        #region Byte

        public static ChangesToken Edit(this TextLabel label, ref byte val)
        {
            Write(label);
            return Edit(ref val);
        }

        public static ChangesToken Edit(ref byte val, int width)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Edit(ref val);
#endif

            _START();

            var newValText = GUILayout.TextField(val.ToString(), GUILayout.MaxWidth(width));

            if (!_END()) return ChangesToken.False;

            if (byte.TryParse(newValText, out byte newValue))
                val = newValue;

            return SetChangedTrue_Internal();

        }


        public static ChangesToken Edit(this TextLabel label, ref byte val, int valueWidth)
        {
            Write(label);
            return Edit(ref val, valueWidth);
        }


        public static ChangesToken Edit(ref byte val)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Edit(ref val);
#endif
            _START();
            var newval = GUILayout.TextField(val.ToString(), Utils.GuiMaxWidthOption);
            if (!_END()) return ChangesToken.False;

            if (int.TryParse(newval, out int newValue))
                val = (byte)Mathf.Clamp(newValue, byte.MinValue, byte.MaxValue);
            //val = (byte) newValue;

            return ChangesToken.True;
        }

        #endregion

        #region UInt

        public static ChangesToken Edit(ref uint val)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Edit(ref val);
#endif
            _START();
            var newval = GUILayout.TextField(val.ToString(), Utils.GuiMaxWidthOption);
            if (!_END()) return ChangesToken.False;

            if (int.TryParse(newval, out int newValue))
                val = (uint)newValue;

            return ChangesToken.True;


        }

        public static ChangesToken Edit(ref uint val, int width)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Edit(ref val, width);
#endif

            _START();
            var strVal = GUILayout.TextField(val.ToString(), GUILayout.MaxWidth(width));
            if (!_END()) return ChangesToken.False;

            if (int.TryParse(strVal, out int newValue))
                val = (uint)newValue;

            return ChangesToken.True;

        }

        public static ChangesToken Edit(ref uint val, uint min, uint max)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Edit(ref val, min, max);
#endif

            _START();
            val = (uint)GUILayout.HorizontalSlider(val, min, max, Utils.GuiMaxWidthOption);
            return _END();

        }

        public static ChangesToken Edit(this TextLabel label, ref uint val)
        {
            Write(label);
            return Edit(ref val);
        }

        public static ChangesToken Edit(this TextLabel label, ref uint val, uint min, uint max)
        {
            label.SliderText_Internal(val);
            return Edit(ref val, min, max);
        }

        #endregion

        #region Int

        public static ChangesToken Edit_Layer(this TextLabel label, ref int layer)
        {
            label.Write();

            List<string> lst = new();

            for (int i = 0; i < 32; i++)
            {
                lst.Add("{0}: {1}".F(i, LayerMask.LayerToName(i)));
            }

            return Select(ref layer, lst);
        }

        public static ChangesToken Edit_LayerMask(this TextLabel label, ref string tag)
        {
            label.Write();
            return Edit_Tag(ref tag);
        }

        public static ChangesToken Edit_Tag(ref string tag)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.EditTag(ref tag);
#endif

            return ChangesToken.False;
        }

        public static ChangesToken Edit_LayerMask(this TextLabel label, ref int val)
        {
            label.ApproxWidth().Write();
            return Edit_LayerMask(ref val);
        }

        public static ChangesToken Edit_LayerMask(ref int val)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.EditLayerMask(ref val);
#endif

            return ChangesToken.False;
        }

        private const int ADD_SUBTRACT_BUTTON_SIZE = 20;

        public static ChangesToken Edit_WithButtons(this TextLabel label, ref int value)
        {
            var change = ChangeTrackStart();
            label.Edit(ref value);
            Edit_WithButtons_Internal(ref value);
            return change;
        }

        public static ChangesToken Edit_WithButtons(this TextLabel label,  ref int value, int valueWidth)
        {
            var change = ChangeTrackStart();
            label.Edit(ref value, valueWidth);
            Edit_WithButtons_Internal(ref value);
            return change;
        }

        public static ChangesToken Edit_WithButtons(ref int value, int valueWidth)
        {
            var change = ChangeTrackStart();
            Edit(ref value, valueWidth);
            Edit_WithButtons_Internal(ref value);
            return change;
        }

        private static void Edit_WithButtons_Internal(ref int value) 
        {
            if (Icon.Subtract.Click(ADD_SUBTRACT_BUTTON_SIZE).UnfocusOnChange()) value--;
            if (Icon.Add.Click(ADD_SUBTRACT_BUTTON_SIZE).UnfocusOnChange()) value++;
        }

        public static ChangesToken Edit(ref int val)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Edit(ref val);
#endif

            _START();
            var intText = GUILayout.TextField(val.ToString(), Utils.GuiMaxWidthOption);
            if (!_END()) return ChangesToken.False;

            if (int.TryParse(intText, out int newValue))
                val = newValue;

            return ChangesToken.True;
        }

        public static ChangesToken Edit(ref int val, int width)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Edit(ref val, width);
#endif

            _START();

            var newValText = GUILayout.TextField(val.ToString(), GUILayout.MaxWidth(width));

            if (!_END()) return ChangesToken.False;

            if (int.TryParse(newValText, out int newValue))
                val = newValue;

            return SetChangedTrue_Internal();

        }

        public static ChangesToken Edit(ref int val, int minInclusive, int maxInclusive)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Edit(ref val, minInclusive: minInclusive, maxInclusive: maxInclusive);
#endif

            _START();
            val = (int)GUILayout.HorizontalSlider(val, minInclusive, maxInclusive, Utils.GuiMaxWidthOption);
            return _END();

        }

        private static int editedInteger;
        private static int editedIntegerIndex = -1;
        public static ChangesToken Edit_Delayed(ref int val, int width = -1)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Edit_Delayed(ref val, width);

#endif

            _elementIndex++;

            CheckLine();

            var tmp = (editedIntegerIndex == _elementIndex) ? editedInteger : val;

            if (KeyCode.Return.IsDown() && (_elementIndex == editedIntegerIndex))
            {
                Edit(ref tmp);
                val = editedInteger;
                editedIntegerIndex = -1;
                return SetChangedTrue_Internal();
            }

            if (Edit(ref tmp).IgnoreChanges())
            {
                editedInteger = tmp;
                editedIntegerIndex = _elementIndex;
            }

          

            return ChangesToken.False;

        }

        public static ChangesToken Edit_Delayed(this TextLabel label, ref int val)
        {
            label.FallbackHint = () => Msg.EditDelayed_HitEnter.GetText();
            label.Write();
            return Edit_Delayed(ref val);
        }

        public static ChangesToken Edit_Delayed(this TextLabel label, ref int val, int valueWidth)
        {
            label.FallbackHint = ()=> Msg.EditDelayed_HitEnter.GetText();
            label.Write();
            return Edit_Delayed(ref val, width: valueWidth);
        }

        public static ChangesToken Edit(this TextLabel label, ref int val)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Edit(label, ref val);
#endif
            label.TryWrite();
            return Edit(ref val);

          //  Write(label);
           // return Edit(ref val);
        }

        public static ChangesToken Edit(this TextLabel label, ref int val, int minInclusiven, int maxInclusive)
        {
            label.SliderText_Internal(val);
            return Edit(ref val, minInclusive: minInclusiven, maxInclusive: maxInclusive);
        }

        public static ChangesToken Edit(this TextLabel label, ref int val, int valueWidth)
        {
            Write(label);
            return Edit(ref val, valueWidth);
        }


        public static ChangesToken Edit_Range(this TextLabel label, ref int from, ref int to)
        {
            label.ApproxWidth().Write();
            var changed = ChangeTrackStart();
            if (Edit_Delayed(ref from))
                to = Mathf.Max(from, to);

            "-".PegiLabel(10).Write();

            if (Edit_Delayed(ref to))
                from = Mathf.Min(from, to);

            return changed;
        }

        #endregion

        #region Long

        public static ChangesToken Edit(ref long val)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Edit(ref val);
#endif

            _START();
            var intText = GUILayout.TextField(val.ToString(), Utils.GuiMaxWidthOption);
            if (!_END()) return ChangesToken.False;


            if (long.TryParse(intText, out long newValue))
                val = newValue;

            return ChangesToken.True;
        }

        public static ChangesToken Edit(ref long val, int width)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Edit(ref val, width);
#endif

            _START();

            var newValText = GUILayout.TextField(val.ToString(), GUILayout.MaxWidth(width));

            if (!_END()) return ChangesToken.False;

            if (long.TryParse(newValText, out long newValue))
                val = newValue;

            return SetChangedTrue_Internal();

        }

        public static ChangesToken Edit(this TextLabel label, ref long val)
        {
            #if UNITY_EDITOR
                    if (!PaintingGameViewUI)
                        return PegiEditorOnly.Edit(label, ref val);
            #endif

            label.TryWrite();
            return Edit(ref val);
            // Write(label);
            // return Edit(ref val);
        }

        public static ChangesToken Edit(ref long val, long min, long max)
        {
            #if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Edit(ref val, min, max);
            #endif

            _START();
            double tmp = val;
            val = (long)GUILayout.HorizontalSlider(val, min, max, Utils.GuiMaxWidthOption);
            return _END();

        }

        #endregion

        #region Float
        public static ChangesToken Edit(ref float val)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Edit(ref val);
#endif

            return Edit_Delayed(ref val);

            /*
            _START();
            var newval = GUILayout.TextField(val.ToString(CultureInfo.InvariantCulture), Utils.GuiMaxWidthOption);

            if (!_END()) return ChangesToken.False;

            float newValue;
            if (float.TryParse(newval, out newValue))
                val = newValue;

            return SetChangedTrue_Internal();*/
        }

        public static ChangesToken Edit(ref float val, int width)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Edit(ref val, width);
#endif

            return Edit_Delayed(ref val, width);
            /* _START();

             var newval = GUILayout.TextField(val.ToString(CultureInfo.InvariantCulture), GUILayout.MaxWidth(width));

             if (!_END()) return ChangesToken.False;

             float newValue;
             if (float.TryParse(newval, out newValue))
                 val = newValue;

             return SetChangedTrue_Internal();*/

        }

        public static ChangesToken Edit(this TextLabel label, ref float val)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Edit(label, ref val);
#endif
            label.TryWrite();
            return Edit(ref val);
        }

        public static ChangesToken Edit(ref float val, float min, float max)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Edit(ref val, min, max);
#endif

            _START();
            val = GUILayout.HorizontalSlider(val, min, max, Utils.GuiMaxWidthOption);
            return _END();

        }

        public static ChangesToken Edit(this TextLabel label, ref float val, int valueWidth)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Edit(label, ref val, valueWidth);
#endif
            label.TryWrite();
            return Edit(ref val, valueWidth);
        }


        public static ChangesToken Edit_Delayed(this TextLabel label, ref float val)
        {
            Write(label, 0.33f);
            return Edit_Delayed(ref val);
        }

        public static ChangesToken Edit_Delayed(ref float val, int width)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Edit_Delayed(ref val, width);
#endif

            return Edit_Delayed(ref val);
        }

        public static ChangesToken Edit_Delayed(ref float val)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Edit_Delayed(ref val);
#endif

            _elementIndex++;

            CheckLine();

            var tmp = (editedFloatIndex == _elementIndex) ? editedFloat : val.ToString(System.Globalization.CultureInfo.InvariantCulture);

            if (KeyCode.Return.IsDown() && (_elementIndex == editedFloatIndex))
            {
                Edit(ref tmp);

                editedFloat = QcSharp.FixDecimalSeparator(editedFloat);

                if (float.TryParse(editedFloat, out float newValue))
                    val = newValue;

                editedFloatIndex = -1;

                return SetChangedTrue_Internal();
            }

            if (Edit(ref tmp).IgnoreChanges())
            {
                editedFloat = tmp;
                editedFloatIndex = _elementIndex;
            }

          

            return ChangesToken.False;
        }

        private static string editedFloat;
        private static int editedFloatIndex = -1;

       /* public static ChangesToken edit(this PegiText label, ref float val)
        {
            label.TryWrite();
            return edit(ref val);
        }*/

        public static ChangesToken Edit_Range(this TextLabel label, ref float from, ref float to)
        {
            label.TryWrite();
            var changed = ChangeTrackStart();
            if (Edit_Delayed(ref from))
                to = Mathf.Max(from, to);

            "-".PegiLabel(10).Write(); 

            if (Edit_Delayed(ref to))
                from = Mathf.Min(from, to);

            return changed;
        }

        private static void SliderText_Internal(this TextLabel label, double val)
        {
            label.FallbackWidthFraction = 0.33f;
            if (PaintingGameViewUI)
            {
                label.label = "{0} [{1}]".F(label, val.ToString());
                label.Write();
            }
            else
                Write(label);
        }

        private static void SliderText_Internal(this TextLabel label, int val)
        {
            label.FallbackWidthFraction = 0.33f;
            if (PaintingGameViewUI)
            {
                label.label = "{0} [{1}]".F(label, val.ToString());
                label.Write();
            }
            else
                Write(label);
        }

        private static void SliderText_Internal(this TextLabel label, float val)
        {
            label.FallbackWidthFraction = 0.33f;
            if (PaintingGameViewUI)
            {
                label.label = "{0} [{1}]".F(label.label, val.ToString("F3"));
                label.Write();
            }
            else
                Write(label);
        }

        public static ChangesToken Edit(this TextLabel label, ref float val, float min, float max)
        {
            label.FallbackWidthFraction = 0.3f;

            max = Mathf.Max(max, val);
            
            label.SliderText_Internal(val);
            return Edit(ref val, min, max);
        }

        public static ChangesToken Edit(this TextLabel label, ref float val, float min, float max, float defaultValue)
        {
            var cahnges = ChangeTrackStart();
            label.Edit(ref val, min, max);
            if (val != defaultValue && Icon.Refresh.Click("Set Default value of {0}".F(defaultValue)))
                val = defaultValue;

            return cahnges;
        }

        private static ChangesToken Edit(this Icon ico, ref float val, float min, float max)
        {
            ico.Draw();
            return Edit(ref val, min, max);
        }

        #endregion

        #region Double

        public static ChangesToken Edit(this TextLabel label, ref double val, int valueWidth)
        {
            label.Write();
            return Edit(ref val, valueWidth);
        }

        public static ChangesToken Edit(this TextLabel label, ref double val, double min, double max)
        {
            label.SliderText_Internal(val);
            return Edit(ref val, min, max);
        }

        public static ChangesToken Edit(ref double val, double min, double max)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Edit(ref val, min, max);
#endif

            _START();

            var tmpVal = val;

            if (System.Math.Abs(val) < float.MaxValue && System.Math.Abs(min) < float.MaxValue && System.Math.Abs(max) < float.MaxValue)
            {
                tmpVal = GUILayout.HorizontalSlider((float)val, (float)min, (float)max, Utils.GuiMaxWidthOption);
            }
            else
            {
                min *= 0.25d;
                max *= 0.25d;
                tmpVal *= 0.25d;

                var gap = max - min;
                double tmp = (val - min) / gap; // Remap to 01 range
                tmpVal = GUILayout.HorizontalSlider((float)tmp, 0f, 1f) * gap + min;

                tmpVal *= 4;
            }

            if (_END())
            {
                val = tmpVal;
                return ChangesToken.True;
            }

            return ChangesToken.False;
        }

        public static ChangesToken Edit_Delayed(this TextLabel label, ref double val, int valueWidth)
        {
            label.Write();
            return Edit_Delayed(ref val, width: valueWidth);
        }

        public static ChangesToken Edit_Delayed(this TextLabel label, ref double val)
        {
            label.Write();
            return Edit_Delayed(ref val);
        }

        public static ChangesToken Edit_Delayed(ref double val, int width)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Edit_Delayed(ref val, width);
#endif

            return Edit_Delayed(ref val);
        }

        public static ChangesToken Edit_Delayed(ref double val)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Edit_Delayed(ref val);
#endif

            _elementIndex++;

            CheckLine();

            var curVal = val.ToString(System.Globalization.CultureInfo.InvariantCulture);

            bool isCurrent = (_elementIndex == editedDoubleIndex);

            var tmp = isCurrent ? editedDouble : curVal;

            if (KeyCode.Return.IsDown() && isCurrent)
            {
                Edit(ref tmp);

                SetValue(ref val);
                return SetChangedTrue_Internal();
            }

            if (Edit(ref tmp).IgnoreChanges())
            {
                editedDouble = tmp;
                editedDoubleIndex = _elementIndex;
            }

            if (isCurrent && editedDouble != curVal && Icon.Done.Click())
            {
                SetValue(ref val);
                return SetChangedTrue_Internal();
            }

            static void SetValue(ref double value)
            {
                editedDouble = QcSharp.FixDecimalSeparator(editedDouble);

                if (double.TryParse(editedDouble, out double newValue))
                    value = newValue;

                editedDoubleIndex = -1;
            }

            return ChangesToken.False;
        }

        private static string editedDouble;
        private static int editedDoubleIndex = -1;

        public static ChangesToken Edit(ref double val)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Edit(ref val);
#endif

            return Edit_Delayed(ref val);

            /*
            _START();
            var newval = GUILayout.TextField(val.ToString(CultureInfo.InvariantCulture), Utils.GuiMaxWidthOption);
            if (!_END()) return ChangesToken.False;
            double newValue;
            if (!double.TryParse(newval, out newValue)) return ChangesToken.False;
            val = newValue;
            return SetChangedTrue_Internal();*/
        }

        public static ChangesToken Edit(this TextLabel label, ref double val)
        {
            label.Write();
            return Edit(ref val);
        }

        public static ChangesToken Edit(ref double val, int width)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Edit(ref val, width);
#endif

            _START();
            var newval = GUILayout.TextField(val.ToString(System.Globalization.CultureInfo.InvariantCulture), GUILayout.MaxWidth(width));
            if (!_END()) return ChangesToken.False;

            if (double.TryParse(newval, out double newValue))
                val = newValue;

            return SetChangedTrue_Internal();

        }

        #endregion

        #region String

        private static string editedText;
        private static string editedHash = "";
        public static ChangesToken Edit_Delayed(ref string val)
        {
            if (LengthIsTooLong(ref val)) 
                return ChangesToken.False;

            val ??= "";

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Edit_Delayed(ref val);
#endif

            CheckLine();

            if ((KeyCode.Return.IsDown() && (val.GetHashCode().ToString() == editedHash)))
            {
                GUILayout.TextField(val, Utils.GuiMaxWidthOption);
                val = editedText;
                editedHash = "dummy";

                return SetChangedTrue_Internal();
            }

            var tmp = val;
            if (Edit(ref tmp).IgnoreChanges())
            {
                editedText = tmp;
                editedHash = val.GetHashCode().ToString();
            }

            return ChangesToken.False;

        }

        public static ChangesToken Edit_Delayed(this TextLabel label, ref string val)
        {
            label.FallbackHint = () => Msg.EditDelayed_HitEnter.GetText();
            Write(label);
            return Edit_Delayed(ref val);
        }

        public static ChangesToken Edit_Delayed(this TextLabel label, ref string val, int valueWidth)
        {
            label.FallbackHint = () => Msg.EditDelayed_HitEnter.GetText();
            Write(label);
            return Edit_Delayed(ref val, valueWidth);
        }

        public static ChangesToken Edit_Delayed(ref string val, int width)
        {

            if (LengthIsTooLong(ref val)) return ChangesToken.False;

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Edit_Delayed(ref val, width);
#endif

            CheckLine();

            if ((KeyCode.Return.IsDown() && (val.GetHashCode().ToString() == editedHash)))
            {
                GUILayout.TextField(val, Utils.GuiMaxWidthOption);
                val = editedText;
                return SetChangedTrue_Internal();
            }

            var tmp = val;
            if (Edit(ref tmp, width).IgnoreChanges())
            {
                editedText = tmp;
                editedHash = val.GetHashCode().ToString();
            }

            return ChangesToken.False;

        }

    

        private const int maxStringSize = 1000;

        private static bool LengthIsTooLong(ref string text, TextLabel label = default)
        {
            if (text == null || text.Length < maxStringSize)
                return false;

            if (label.IsInitialized)
                label.Write();

            var subtext = text[..10];

            if (Icon.Delete.ClickConfirm(confirmationTag: "delLngStr"+ subtext).UnfocusOnChange())
            {
                text = "";
                return false;
            }

            if (Icon.Copy.Click() | "Too long: {0}".F(subtext).PegiLabel().Click())
                SetCopyPasteBuffer(text);

            return true;
        }

        public static ChangesToken Edit(ref string val)
        {

            if (LengthIsTooLong(ref val)) return ChangesToken.False;

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Edit(ref val);
#endif

            _START();
            val = GUILayout.TextField(val, GUILayout.MaxWidth(250));
            return _END();
        }

        public static ChangesToken Edit(ref string val, int width)
        {

            if (LengthIsTooLong(ref val)) return ChangesToken.False;

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Edit(ref val, width);
#endif

            _START();
            var newval = GUILayout.TextField(val, GUILayout.MaxWidth(width));
            if (_END())
            {
                val = newval;
                return SetChangedTrue_Internal();
            }
            return ChangesToken.False;

        }

        public static ChangesToken Edit(this TextLabel label, ref string val)
        {
            if (LengthIsTooLong(ref val, label)) 
                return ChangesToken.False;

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.Edit(label, ref val);
#endif

            Write(label);
            return Edit(ref val);
        }

        public static ChangesToken Edit(this TextLabel label, ref string val, int valueWidth)
        {
            Write(label);
            return Edit(ref val, width: valueWidth);
        }

        public static ChangesToken Edit_Big(this TextLabel label, ref string val, int height = 100)
        {
            Write(label);
            return Edit_Big(ref val, height: height);
        }


        public static ChangesToken Edit_Big(ref string val, int height = 100)
        {

            Nl();

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.EditBig(ref val, height).Nl();
#endif

            _START();
            val = GUILayout.TextArea(val, GUILayout.MaxHeight(height), Utils.GuiMaxWidthOption);
            return _END();

        }

        public static ChangesToken Edit_Delayed_CopyPaste(this TextLabel label, ref string val, bool clearOnPaste = true) 
        {
            var valueChanged = ChangeTrackStart();

            label.Edit_Delayed(ref val);

            if (CopyPasteBuffer != val)
            {
                if (CopyPasteBuffer.IsNullOrEmpty())
                {
                    if (Icon.Copy.Click())
                        CopyPasteBuffer = val;
                }
                else
                {
                    if (Icon.Paste.Click(CopyPasteBuffer))
                    {
                        val = CopyPasteBuffer;
                        if (clearOnPaste)
                            CopyPasteBuffer = null;
                    }

                    if (Icon.Clear.Click("Clear buffer"))
                        CopyPasteBuffer = null;

                
                }
            }
            else
                Icon.Done.Draw("Value is in Copy Paste Buffer");

            return valueChanged;
        }


        #endregion

        #region DateTime

        public static ChangesToken Edit(this TextLabel label, ref DateTime val, bool time = true)
        {
            label.FallbackHint = () => Msg.EditDelayed_HitEnter.GetText();
            label.Write();

            var asString = val.ToString(time ? "yyyy-MM-dd HH:mm:ss" : "yyyy-MM-dd");
            
            if (Edit_Delayed(ref asString)) 
            {
                DateTime.TryParse(asString, out val);
                return ChangesToken.True;
            }

            if (!time) 
            {
                if (Icon.Subtract.ClickUnFocus())
                    val -= TimeSpan.FromDays(1);
                if (Icon.Add.ClickUnFocus())
                    val += TimeSpan.FromDays(1);
            }

            if ("Now".PegiLabel().ClickUnFocus())
                val = DateTime.Now;

            return ChangesToken.False;
        }

        #endregion

        #region URL

        public static ChangesToken Edit_FullAssetPath(ref string path)
        {
            #if !UNITY_EDITOR
            return ChangesToken.False;
            #else

            var changes = ChangeTrackStart();

            string projectPath = NormalizedPath(Application.dataPath);

            UnityEngine.Object tmp = null;

            if (!path.IsNullOrEmpty() && NormalizedPath(path).StartsWith(projectPath))
                tmp = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets" + path[projectPath.Length..]);
         
            if (Edit(ref tmp) &&  QcUnity.TryGetFullPath(tmp, out var newpath))
                path = newpath;

            return changes;

            static string NormalizedPath(string path) => Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToLowerInvariant();
            
            #endif
        }

        #endregion
    }
}