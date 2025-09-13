using QuizCanners.Inspect;

namespace QuizCanners.Utils
{
    public static class PlayerPrefValue
    {
        public abstract class BaseGeneric<T>
        {
            protected string key;
            protected T defaultValue;
            protected T setValue;
            private bool _initialized;
            private bool _setByUser;

            public bool SetByUser 
            {
                get 
                {
                    if (!_initialized)
                        GetValue();

                    return _setByUser;
                } 
            }

            protected abstract void SaveValue(T value);

            protected abstract T LoadValue();

            public void SetValue(T value)
            {
                _setByUser = true;
                _initialized = true;
                setValue = value;
                SaveValue(value);
            }

            public T GetValue()
            {
                if (!_initialized)
                {
                    _initialized = true;
                    if (UnityEngine.PlayerPrefs.HasKey(key))
                    {
                        _setByUser = true;
                        setValue = LoadValue();
                    }
                }
                return SetByUser ? setValue : defaultValue;
            }

            public void Clear() => SetValue(defaultValue);

            public override string ToString() => GetValue().ToString();
            
            public BaseGeneric(string key, T defaultValue)
            {
                this.key = key;
                this.defaultValue = defaultValue;
            }
        }

        public class Int : BaseGeneric<int>, IPEGI
        {
            protected override void SaveValue(int value) => UnityEngine.PlayerPrefs.SetInt(key, value);
            protected override int LoadValue() => UnityEngine.PlayerPrefs.GetInt(key, defaultValue: defaultValue);
            public Int(string key, int defaultValue) : base(key, defaultValue) { }

            void IPEGI.Inspect()
            {
                var tmp = GetValue();
                if (key.PL().Edit(ref tmp))
                    SetValue(tmp);
            }
        }

        public class Float : BaseGeneric<float>, IPEGI
        {
            protected override void SaveValue(float value) => UnityEngine.PlayerPrefs.SetFloat(key, value);
            protected override float LoadValue() => UnityEngine.PlayerPrefs.GetFloat(key, defaultValue: defaultValue);
            public Float(string key, float defaultValue) : base(key, defaultValue) { }

            void IPEGI.Inspect()
            {
                var tmp = GetValue();
                if (key.PL().Edit(ref tmp))
                    SetValue(tmp);
            }
        }

        public class String : BaseGeneric<string>, IPEGI
        {
            protected override void SaveValue(string value) => UnityEngine.PlayerPrefs.SetString(key, value);
            protected override string LoadValue() => UnityEngine.PlayerPrefs.GetString(key, defaultValue: defaultValue);
            public String(string key, string defaultValue) : base(key, defaultValue) { }

            void IPEGI.Inspect()
            {
                var tmp = GetValue();
                if (key.PL().Edit(ref tmp))
                    SetValue(tmp);
            }
        }

        public class ColorValue : BaseGeneric<UnityEngine.Color>, IPEGI
        {
            protected override void SaveValue(UnityEngine.Color value) => UnityEngine.PlayerPrefs.SetString(key, UnityEngine.ColorUtility.ToHtmlStringRGBA(value));
            protected override UnityEngine.Color LoadValue()
            {
                string val = UnityEngine.PlayerPrefs.GetString(key, defaultValue: "");
                if (val.IsNullOrEmpty() || !UnityEngine.ColorUtility.TryParseHtmlString(val, out var color))
                    return defaultValue;

                return color;
            }

            public ColorValue(string key, UnityEngine.Color defaultValue) : base(key, defaultValue) 
            {

            }

            void IPEGI.Inspect()
            {
                UnityEngine.Color tmp = GetValue();
                if (key.PL().Edit(ref tmp))
                    SetValue(tmp);
            }
        }


        public class Bool : BaseGeneric<bool>, IPEGI
        {
            private bool From(int value) => value > 0;
            private int From(bool value) => value ? 1 : 0;

            protected override void SaveValue(bool value) => UnityEngine.PlayerPrefs.SetInt(key, From(value));
            protected override bool LoadValue() => From(UnityEngine.PlayerPrefs.GetInt(key, defaultValue: From(defaultValue)));

            public Bool(string key, bool defaultValue) : base(key, defaultValue) { }

            void IPEGI.Inspect()
            {
                var tmp = GetValue();
                if (key.PL().ToggleIcon(ref tmp))
                    SetValue(tmp);
            }
        }
    }
}
