using System.Collections.Generic;
using QuizCanners.Utils;
using UnityEngine;

using CultureInfo = System.Globalization.CultureInfo;

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0011 // Add braces
#pragma warning disable IDE0008 // Use explicit type

namespace QuizCanners.Inspect
{
    public static partial class pegi
    {
        #region UInt

        public static bool edit(ref uint val)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.edit(ref val);
#endif
            _START();
            var newval = GUILayout.TextField(val.ToString(), Utils.GuiMaxWidthOption);
            if (!_END()) return false;

            int newValue;
            if (int.TryParse(newval, out newValue))
                val = (uint)newValue;

            return true;


        }

        public static bool edit(ref uint val, int width)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.edit(ref val, width);
#endif

            _START();
            var strVal = GUILayout.TextField(val.ToString(), GUILayout.MaxWidth(width));
            if (!_END()) return false;

            int newValue;
            if (int.TryParse(strVal, out newValue))
                val = (uint)newValue;

            return true;

        }

        public static bool edit(ref uint val, uint min, uint max)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.edit(ref val, min, max);
#endif

            _START();
            val = (uint)GUILayout.HorizontalSlider(val, min, max, Utils.GuiMaxWidthOption);
            return _END();

        }

        public static bool edit(this TextLabel label, ref uint val)
        {
            write(label);
            return edit(ref val);
        }

        public static bool edit(this TextLabel label, ref uint val, uint min, uint max)
        {
            label.sliderText(val);
            return edit(ref val, min, max);
        }

        #endregion

        #region Int

        public static ChangesToken edit_Layer(this TextLabel label, ref int layer)
        {
            label.write();

            List<string> lst = new List<string>();

            for (int i = 0; i < 32; i++)
            {
                lst.Add("{0}: {1}".F(i, LayerMask.LayerToName(i)));
            }

            return select(ref layer, lst);
        }

        public static ChangesToken editLayerMask(this TextLabel label, ref string tag)
        {
            label.write();
            return editTag(ref tag);
        }

        public static ChangesToken editTag(ref string tag)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.editTag(ref tag);
#endif

            return ChangesToken.False;
        }

        public static ChangesToken editLayerMask(this TextLabel label, ref int val)
        {
            label.ApproxWidth().write();
            return editLayerMask(ref val);
        }

        public static ChangesToken editLayerMask(ref int val)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.editLayerMask(ref val);
#endif

            return ChangesToken.False;
        }

        private const int ADD_SUBTRACT_BUTTON_SIZE = 20;

        public static ChangesToken edit_WithButtons(this TextLabel label, ref int value)
        {
            var change = ChangeTrackStart();
            label.edit(ref value);
            edit_WithButtons_Internal(ref value);
            return change;
        }

        public static ChangesToken edit_WithButtons(this TextLabel label,  ref int value, int valueWidth)
        {
            var change = ChangeTrackStart();
            label.edit(ref value, valueWidth);
            edit_WithButtons_Internal(ref value);
            return change;
        }

        public static ChangesToken edit_WithButtons(ref int value, int valueWidth)
        {
            var change = ChangeTrackStart();
            edit(ref value, valueWidth);
            edit_WithButtons_Internal(ref value);
            return change;
        }

        private static void edit_WithButtons_Internal(ref int value) 
        {
            if (icon.Subtract.Click(ADD_SUBTRACT_BUTTON_SIZE).UnfocusOnChange()) value--;
            if (icon.Add.Click(ADD_SUBTRACT_BUTTON_SIZE).UnfocusOnChange()) value++;
        }

        public static ChangesToken edit(ref int val)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.edit(ref val);
#endif

            _START();
            var intText = GUILayout.TextField(val.ToString(), Utils.GuiMaxWidthOption);
            if (!_END()) return ChangesToken.False;

            int newValue;

            if (int.TryParse(intText, out newValue))
                val = newValue;

            return ChangesToken.True;
        }

        public static ChangesToken edit(ref int val, int width)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.edit(ref val, width);
#endif

            _START();

            var newValText = GUILayout.TextField(val.ToString(), GUILayout.MaxWidth(width));

            if (!_END()) return ChangesToken.False;

            int newValue;
            if (int.TryParse(newValText, out newValue))
                val = newValue;

            return SetChangedTrue_Internal();

        }

        public static ChangesToken edit(ref int val, int minInclusive, int maxInclusive)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.edit(ref val, minInclusive: minInclusive, maxInclusive: maxInclusive);
#endif

            _START();
            val = (int)GUILayout.HorizontalSlider(val, minInclusive, maxInclusive, Utils.GuiMaxWidthOption);
            return _END();

        }

        private static int editedInteger;
        private static int editedIntegerIndex = -1;
        public static ChangesToken editDelayed(ref int val, int width = -1)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.editDelayed(ref val, width);

#endif

            checkLine();

            var tmp = (editedIntegerIndex == _elementIndex) ? editedInteger : val;

            if (KeyCode.Return.IsDown() && (_elementIndex == editedIntegerIndex))
            {
                edit(ref tmp);
                val = editedInteger;
                editedIntegerIndex = -1;

                _elementIndex++;

                return SetChangedTrue_Internal();
            }

            if (edit(ref tmp).IgnoreChanges())
            {
                editedInteger = tmp;
                editedIntegerIndex = _elementIndex;
            }

            _elementIndex++;

            return ChangesToken.False;

        }

        public static ChangesToken editDelayed(this TextLabel label, ref int val)
        {
            label.FallbackHint = () => Msg.EditDelayed_HitEnter.GetText();
            label.write();
            return editDelayed(ref val);
        }

        public static ChangesToken editDelayed(this TextLabel label, ref int val, int valueWidth)
        {
            label.FallbackHint = ()=> Msg.EditDelayed_HitEnter.GetText();
            label.write();
            return editDelayed(ref val, width: valueWidth);
        }

        public static ChangesToken edit(this IPegiText label, ref int val)
        {
            write(label);
            return edit(ref val);
        }

        public static ChangesToken edit(this TextLabel label, ref int val, int minInclusiven, int maxInclusive)
        {
            label.sliderText(val);
            return edit(ref val, minInclusive: minInclusiven, maxInclusive: maxInclusive);
        }

        public static ChangesToken edit(this TextLabel label, ref int val, int valueWidth)
        {
            write(label);
            return edit(ref val, valueWidth);
        }


        public static ChangesToken edit_Range(this TextLabel label, ref int from, ref int to)
        {
            label.ApproxWidth().write();
            var changed = ChangeTrackStart();
            if (editDelayed(ref from))
                to = Mathf.Max(from, to);

            write("-", 10);

            if (editDelayed(ref to))
                from = Mathf.Min(from, to);

            return changed;
        }

        #endregion

        #region Long

        public static ChangesToken edit(ref long val)
        {
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.edit(ref val);
#endif

            _START();
            var intText = GUILayout.TextField(val.ToString(), Utils.GuiMaxWidthOption);
            if (!_END()) return ChangesToken.False;

            long newValue;

            if (long.TryParse(intText, out newValue))
                val = newValue;

            return ChangesToken.True;
        }

        public static ChangesToken edit(ref long val, int width)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.edit(ref val, width);
#endif

            _START();

            var newValText = GUILayout.TextField(val.ToString(), GUILayout.MaxWidth(width));

            if (!_END()) return ChangesToken.False;

            long newValue;
            if (long.TryParse(newValText, out newValue))
                val = newValue;

            return SetChangedTrue_Internal();

        }

        public static ChangesToken edit(this TextLabel label, ref long val)
        {
            write(label);
            return edit(ref val);
        }

        #endregion

        #region Float

        public static ChangesToken edit(ref float val)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.edit(ref val);
#endif

            _START();
            var newval = GUILayout.TextField(val.ToString(CultureInfo.InvariantCulture), Utils.GuiMaxWidthOption);

            if (!_END()) return ChangesToken.False;

            float newValue;
            if (float.TryParse(newval, out newValue))
                val = newValue;

            return SetChangedTrue_Internal();
        }

        public static ChangesToken edit(ref float val, int width)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.edit(ref val, width);
#endif

            _START();

            var newval = GUILayout.TextField(val.ToString(CultureInfo.InvariantCulture), GUILayout.MaxWidth(width));

            if (!_END()) return ChangesToken.False;

            float newValue;
            if (float.TryParse(newval, out newValue))
                val = newValue;

            return SetChangedTrue_Internal();

        }

        public static ChangesToken edit(this TextLabel label, ref float val)
        {
            label.TryWrite();
#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.edit(ref val);
#endif

            return edit(ref val);
        }

        public static ChangesToken edit(ref float val, float min, float max)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.edit(ref val, min, max);
#endif

            _START();
            val = GUILayout.HorizontalSlider(val, min, max, Utils.GuiMaxWidthOption);
            return _END();

        }

        public static ChangesToken editDelayed(this TextLabel label, ref float val)
        {
            label.write();
            return editDelayed(ref val);
        }

        public static ChangesToken editDelayed(ref float val, int width)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.editDelayed(ref val, width);
#endif

            return editDelayed(ref val);
        }

        public static ChangesToken editDelayed(ref float val)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.editDelayed(ref val);
#endif


            checkLine();

            var tmp = (editedFloatIndex == _elementIndex) ? editedFloat : val.ToString(CultureInfo.InvariantCulture);

            if (KeyCode.Return.IsDown() && (_elementIndex == editedFloatIndex))
            {
                edit(ref tmp);

                float newValue;
                if (float.TryParse(editedFloat, out newValue))
                    val = newValue;
                _elementIndex++;

                editedFloatIndex = -1;

                return SetChangedTrue_Internal();
            }

            if (edit(ref tmp).IgnoreChanges())
            {
                editedFloat = tmp;
                editedFloatIndex = _elementIndex;
            }

            _elementIndex++;

            return ChangesToken.False;
        }

        private static string editedFloat;
        private static int editedFloatIndex = -1;

       /* public static ChangesToken edit(this PegiText label, ref float val)
        {
            label.TryWrite();
            return edit(ref val);
        }*/

        public static ChangesToken edit_Range(this IPegiText label, ref float from, ref float to)
        {
            label.TryWrite();
            var changed = ChangeTrackStart();
            if (editDelayed(ref from))
                to = Mathf.Max(from, to);

            write("-", 10);

            if (editDelayed(ref to))
                from = Mathf.Min(from, to);

            return changed;
        }

        private static void sliderText(this TextLabel label, double val)
        {
            if (PaintingGameViewUI)
            {
                label.label = "{0} [{1}]".F(label, val.ToString());
                label.write();
            }
            else
                write(label);
        }

        private static void sliderText(this TextLabel label, int val)
        {
            if (PaintingGameViewUI)
            {

                label.label = "{0} [{1}]".F(label, val.ToString());
                label.write();
            }
            else
                write(label);
        }

        private static void sliderText(this TextLabel label, float val)
        {
            if (PaintingGameViewUI)
            {
                label.label = "{0} [{1}]".F(label.label, val.ToString("F3"));
                label.ApproxWidth();
                label.write();
            }
            else
                write(label);
        }

        public static ChangesToken edit(this TextLabel label, ref float val, float min, float max)
        {
            
            label.sliderText(val);
            return edit(ref val, min, max);
        }

        private static ChangesToken edit(this icon ico, ref float val, float min, float max)
        {
            ico.draw();
            return edit(ref val, min, max);
        }

        #endregion

        #region Double

        public static ChangesToken edit(this TextLabel label, ref double val, double min, double max)
        {
            label.sliderText(val);
            return edit(ref val, min, max);
        }

        public static ChangesToken edit(ref double val, double min, double max)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.edit(ref val, min, max);
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

        public static ChangesToken editDelayed(this TextToken label, ref double val)
        {
            label.write();
            return editDelayed(ref val);
        }

        public static ChangesToken editDelayed(ref double val, int width)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.editDelayed(ref val, width);
#endif

            return editDelayed(ref val);
        }

        public static ChangesToken editDelayed(ref double val)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.editDelayed(ref val);
#endif


            checkLine();

            var tmp = (editedDoubleIndex == _elementIndex) ? editedDouble : val.ToString(CultureInfo.InvariantCulture);

            if (KeyCode.Return.IsDown() && (_elementIndex == editedDoubleIndex))
            {
                edit(ref tmp);

                double newValue;
                if (double.TryParse(editedDouble, out newValue))
                    val = newValue;
                _elementIndex++;

                editedDoubleIndex = -1;

                return SetChangedTrue_Internal();
            }

            if (edit(ref tmp).IgnoreChanges())
            {
                editedDouble = tmp;
                editedDoubleIndex = _elementIndex;
            }

            _elementIndex++;

            return ChangesToken.False;
        }

        private static string editedDouble;
        private static int editedDoubleIndex = -1;

        public static ChangesToken edit(ref double val)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.edit(ref val);
#endif
            _START();
            var newval = GUILayout.TextField(val.ToString(CultureInfo.InvariantCulture), Utils.GuiMaxWidthOption);
            if (!_END()) return ChangesToken.False;
            double newValue;
            if (!double.TryParse(newval, out newValue)) return ChangesToken.False;
            val = newValue;
            return SetChangedTrue_Internal();
        }

        public static ChangesToken edit(this TextLabel label, ref double val)
        {
            label.write();
            return edit(ref val);
        }

        public static ChangesToken edit(ref double val, int width)
        {

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.edit(ref val, width);
#endif

            _START();
            var newval = GUILayout.TextField(val.ToString(CultureInfo.InvariantCulture), GUILayout.MaxWidth(width));
            if (!_END()) return ChangesToken.False;

            double newValue;
            if (double.TryParse(newval, out newValue))
                val = newValue;

            return SetChangedTrue_Internal();

        }

        #endregion

        #region String

        private static string editedText;
        private static string editedHash = "";
        public static ChangesToken editDelayed(ref string val)
        {
            if (LengthIsTooLong(ref val)) return ChangesToken.False;

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.editDelayed(ref val);
#endif

            checkLine();

            if ((KeyCode.Return.IsDown() && (val.GetHashCode().ToString() == editedHash)))
            {
                GUILayout.TextField(val, Utils.GuiMaxWidthOption);
                val = editedText;

                return SetChangedTrue_Internal();
            }

            var tmp = val;
            if (edit(ref tmp).IgnoreChanges())
            {
                editedText = tmp;
                editedHash = val.GetHashCode().ToString();
            }

            return ChangesToken.False;

        }

        public static ChangesToken editDelayed(this TextLabel label, ref string val)
        {
            label.FallbackHint = () => Msg.EditDelayed_HitEnter.GetText();
            write(label);
            return editDelayed(ref val);
        }

        public static ChangesToken editDelayed(this TextLabel label, ref string val, int valueWidth)
        {
            label.FallbackHint = () => Msg.EditDelayed_HitEnter.GetText();
            write(label);
            return editDelayed(ref val, valueWidth);
        }

        public static ChangesToken editDelayed(ref string val, int width)
        {

            if (LengthIsTooLong(ref val)) return ChangesToken.False;

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.editDelayed(ref val, width);
#endif

            checkLine();

            if ((KeyCode.Return.IsDown() && (val.GetHashCode().ToString() == editedHash)))
            {
                GUILayout.TextField(val, Utils.GuiMaxWidthOption);
                val = editedText;
                return SetChangedTrue_Internal();
            }

            var tmp = val;
            if (edit(ref tmp, width).IgnoreChanges())
            {
                editedText = tmp;
                editedHash = val.GetHashCode().ToString();
            }

            return ChangesToken.False;

        }

    

        private const int maxStringSize = 1000;

        private static bool LengthIsTooLong(ref string label)
        {
            if (label == null || label.Length < maxStringSize)
                return false;

            if (icon.Delete.ClickUnFocus())
            {
                label = "";
                return false;
            }

            if ("String is too long: {0} COPY".F(label.Substring(0, 10)).PegiLabel().Click())
                SetCopyPasteBuffer(label);

            return true;
        }

        public static ChangesToken edit(ref string val)
        {

            if (LengthIsTooLong(ref val)) return ChangesToken.False;

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.edit(ref val);
#endif

            _START();
            val = GUILayout.TextField(val, GUILayout.MaxWidth(250));
            return _END();
        }

        public static ChangesToken edit(ref string val, int width)
        {

            if (LengthIsTooLong(ref val)) return ChangesToken.False;

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.edit(ref val, width);
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

        public static ChangesToken edit(this TextLabel label, ref string val)
        {

            if (LengthIsTooLong(ref val)) return ChangesToken.False;

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.edit(label, ref val);
#endif

            write(label);
            return edit(ref val);
        }

        public static ChangesToken edit(this IPegiText label, ref string val, int valueWidth)
        {
            write(label);
            return edit(ref val, width: valueWidth);
        }

        public static ChangesToken editBig(this IPegiText label, ref string val, int height = 100)
        {
            write(label);
            return editBig(ref val, height: height);
        }


        public static ChangesToken editBig(ref string val, int height = 100)
        {

            nl();

#if UNITY_EDITOR
            if (!PaintingGameViewUI)
                return PegiEditorOnly.editBig(ref val, height).nl();
#endif

            _START();
            val = GUILayout.TextArea(val, GUILayout.MaxHeight(height), Utils.GuiMaxWidthOption);
            return _END();

        }


        #endregion

    }
}