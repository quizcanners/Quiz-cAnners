
using QuizCanners.Inspect;
using System;
using UnityEngine;

namespace QuizCanners.Utils
{
    public static class Fallback
    {

        public class FallbackValueGeneric<T> 
        {
            [SerializeField] public bool IsSet;
            [SerializeField] protected T manualValue;

            public T ManualValue
            {
                set
                {
                    IsSet = true;
                    manualValue = value;
                }
                get 
                {
                    if (!IsSet)
                        QcLog.ChillLogger.LogErrosExpOnly(() => "Getting unset {0} Value. Returning default".F(typeof(T)), "FallBackInt");

                    return manualValue;
                }
            }

            public T Get(T defaultValue) => IsSet ? manualValue : defaultValue;

            public T Get(Func<T> defaultValueGetter) => IsSet ? manualValue : defaultValueGetter.Invoke();

           // public T this[Func<T> defaultValueGetter] => Get(defaultValueGetter: defaultValueGetter);
        }

        [Serializable]
        public class Int : FallbackValueGeneric<int>
        {
            public Int() { }
            public Int(int startValue) { ManualValue = startValue; }

            public void Inspect(string label, int fallbackValue)
            {
                if (IsSet)
                {
                    Icon.Clear.Click(() => IsSet = false, toolTip: "Switch to default value");

                    if (label.PegiLabel(70).Edit(ref manualValue, 60))
                        ManualValue = manualValue;
                }
                else
                {
                    Icon.Edit.Click(() => ManualValue = fallbackValue, toolTip: "Input value manually");
                    "{0}: {1}".F(label, fallbackValue).PegiLabel().Write();
                }
            }
        }

        
        [Serializable]
        public class EnumValueUndefined : FallbackValueGeneric<int>
        {
            public void SetManualEnum<T>(T value) => ManualValue = Convert.ToInt32(value);
            
            public T GetManualEnum<T>() => (T)Enum.ToObject(typeof(T), manualValue);
            
            public EnumValueUndefined() { }
            public EnumValueUndefined(int startValue) { ManualValue = startValue; }
        }
    }
}