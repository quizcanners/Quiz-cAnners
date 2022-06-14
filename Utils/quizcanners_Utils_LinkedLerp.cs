using QuizCanners.Migration;
using QuizCanners.Inspect;
using QuizCanners.Utils;
using System.Collections.Generic;
using UnityEngine;

using Graphic = UnityEngine.UI.Graphic;

namespace QuizCanners.Lerp
{

    public interface ILinkedLerping
    {
        void Portion(LerpData ld);
        void Lerp(LerpData ld, bool canSkipLerp);
    }

    public static class LinkedLerp
    {

        public enum LerpSpeedMode
        {
            SpeedThreshold = 0,
            Unlimited = 1,
            LerpDisabled = 2,
            UnlinkedSpeed = 3
        }

        #region Abstract Base

        public abstract class BaseLerp : ICfg, ILinkedLerping, IPEGI, IPEGI_ListInspect
        {

            public LerpSpeedMode lerpMode = LerpSpeedMode.SpeedThreshold;
            public float SpeedLimit = 1;

            protected bool defaultSet;

            public virtual bool UsingLinkedThreshold =>
                (lerpMode == LerpSpeedMode.SpeedThreshold && Application.isPlaying);
            public virtual bool Enabled => lerpMode != LerpSpeedMode.LerpDisabled;
            public override string ToString() => Name_Internal;

            protected abstract string Name_Internal { get; }
         

            #region Encode & Decode

            public virtual CfgEncoder Encode()
            {

                var cody = new CfgEncoder()
                    .Add("lm", (int)lerpMode);

                    if (lerpMode == LerpSpeedMode.SpeedThreshold)
                        cody.Add("sp", SpeedLimit);
                

                return cody;
            }

            public virtual void DecodeTag(string key, CfgData data)
            {
                switch (key)
                {
                    case "sp": SpeedLimit = data.ToFloat(); break;
                    case "lm": lerpMode = (LerpSpeedMode)data.ToInt(); break;
                }
            }

            #endregion

            public void Lerp(LerpData ld, bool canSkipLerp)
            {
                if (!Enabled) return;

                float portion;

                switch (lerpMode)
                {
                    case LerpSpeedMode.LerpDisabled:
                        portion = 0;
                        break;
                    case LerpSpeedMode.UnlinkedSpeed:
                        portion = 1;

                        if (Application.isPlaying)
                            Portion(ref portion, unscaledTime: ld._unscaledTime);

                        if (canSkipLerp)
                            portion = 1;

                        break;
                    default:
                        portion = ld.Portion(canSkipLerp);
                        break;
                }

                if (LerpInternal(portion) || !defaultSet)
                    ld.OnValueLerped();

                defaultSet = true;
            }

            protected abstract bool LerpInternal(float linkedPortion);

            public virtual void Portion(LerpData ld)
            {
                var lp = ld.MinPortion;

                if (UsingLinkedThreshold && Portion(ref lp, unscaledTime: ld._unscaledTime))
                {
                    ld.AddPortion(lp, this);
                }
                else if (lerpMode == LerpSpeedMode.UnlinkedSpeed)
                {
                    float portion = 1;
                    Portion(ref portion, unscaledTime: ld._unscaledTime);
                    ld.AddPortion(portion, this);
                }
            }

            protected abstract bool Portion(ref float linkedPortion, bool unscaledTime);

            #region Inspector

            public virtual void InspectInList(ref int edited, int ind)
            {
               
                if (Application.isPlaying)
                    (Enabled ? Icon.Active : Icon.InActive).Draw(Enabled ? "Lerp Possible" : "Lerp Not Possible");

                switch (lerpMode)
                {
                    case LerpSpeedMode.SpeedThreshold:
                        (Name_Internal + " Thld").PegiLabel(200).Edit(ref SpeedLimit);
                        break;
                    case LerpSpeedMode.UnlinkedSpeed:
                        (Name_Internal + " Speed").PegiLabel(200).Edit(ref SpeedLimit);
                        break;
                    default:
                        (Name_Internal + " Mode").PegiLabel(200).Edit_Enum(ref lerpMode);
                        break;
                }
                
                if (Icon.Enter.Click())
                    edited = ind;
            }

            public virtual void Inspect()
            {

                ToString().PegiLabel().Write_ForCopy().Nl();

                "Lerp Speed Mode ".PegiLabel(110).Edit_Enum(ref lerpMode).Nl();

                if (Application.isPlaying)
                    (Enabled ? Icon.Active : Icon.InActive).Nl(Enabled ? "Lerp Possible" : "Lerp Not Possible");

                switch (lerpMode)
                {
                    case LerpSpeedMode.SpeedThreshold:
                        ("Max Speed").PegiLabel(90).Edit(ref SpeedLimit);
                        break;
                    case LerpSpeedMode.UnlinkedSpeed:
                        ("Speed").PegiLabel(90).Edit(ref SpeedLimit);
                        break;
                        //default:
                        //("Mode").editEnum(ref lerpMode).changes(ref changed);
                        //break;
                }

            }



            #endregion

        }

        public abstract class BaseLerpGeneric<T> : BaseLerp
        {
            public abstract T TargetValue { get; set; }
            public abstract T CurrentValue { get; set; }

            public T TargetAndCurrentValue
            {
                set
                {
                    TargetValue = value;
                    CurrentValue = value;
                }
            }

            public virtual void Portion(LerpData ld, T targetValue)
            {
                TargetValue = targetValue;
                Portion(ld);
            }
        }

        public abstract class BaseVector2Lerp : BaseLerpGeneric<Vector2>
        {
            public Vector2 targetValue;

            public override Vector2 TargetValue
            {
                get { return targetValue; }
                set { targetValue = value; }
            }

            // protected override bool EaseInOutImplemented => true;

            // private float _easePortion = 0.1f;

            public override bool UsingLinkedThreshold => base.UsingLinkedThreshold && Enabled;

            protected override bool LerpInternal(float linkedPortion)
            {
                if (CurrentValue != targetValue || !defaultSet)
                    CurrentValue = Vector2.Lerp(CurrentValue, targetValue, linkedPortion);
                else return false;

                return true;
            }

            protected override bool Portion(ref float linkedPortion, bool unscaledTime)
            {

                var magnitude = (CurrentValue - targetValue).magnitude;

                var modSpeed = SpeedLimit;

                /* if (easeInOut)
                 {
                     _easePortion = Mathf.Lerp(_easePortion, magnitude > speedLimit * 0.5f ? 1 : 0.1f, Time.deltaTime * 2);
                     modSpeed *= _easePortion;
                 }*/

                return LerpUtils.SpeedToMinPortion(modSpeed, magnitude, ref linkedPortion, unscaledTime: unscaledTime);
            }

            #region Inspector

            public override void InspectInList(ref int edited, int ind)
            {
                var change = pegi.ChangeTrackStart();
                base.InspectInList(ref edited, ind);

                if (change)
                    targetValue = CurrentValue;

            }

            public override void Inspect()
            {
                pegi.Nl();

                var changed = pegi.ChangeTrackStart();

                base.Inspect();
                pegi.Nl();

                if (changed)
                {
                    targetValue = CurrentValue;
                }

                if (lerpMode != LerpSpeedMode.LerpDisabled)
                    "Target".PegiLabel().Edit(ref targetValue).Nl();
            }

            #endregion

            #region Encode & Decode

            public override CfgEncoder Encode()
            {
                var cody = new CfgEncoder()
                    .Add("b", base.Encode)
                    .Add("t", CurrentValue);

                return cody;
            }

            public override void DecodeTag(string key, CfgData data)
            {
                switch (key)
                {
                    case "b":
                        data.ToDelegate(base.DecodeTag);
                        break;
                    case "t":
                        targetValue = data.ToVector2();
                        break;
                }
            }

            #endregion


        }

        public abstract class BaseQuaternionLerp : BaseLerpGeneric<Quaternion>
        {
            protected Quaternion targetValue;

            public override Quaternion TargetValue
            {
                get { return targetValue; }
                set { targetValue = value; }
            }

            public override bool UsingLinkedThreshold => base.UsingLinkedThreshold && Enabled;

            protected override bool LerpInternal(float linkedPortion)
            {
                if (CurrentValue != targetValue || !defaultSet)
                    CurrentValue = Quaternion.Lerp(CurrentValue, targetValue, linkedPortion);
                else return false;

                return true;
            }

            protected override bool Portion(ref float linkedPortion, bool unscaledTime)
            {

                var magnitude = Quaternion.Angle(CurrentValue, targetValue);

                return  LerpUtils.SpeedToMinPortion(SpeedLimit, magnitude, ref linkedPortion, unscaledTime: unscaledTime);
            }

            #region Inspector

            public override void InspectInList(ref int edited, int ind)
            {
                base.InspectInList(ref edited, ind);

                if (pegi.ChangeTrackStart())
                    targetValue = CurrentValue;
            }

            public override void Inspect()
            {
                pegi.Nl();

                var changed = pegi.ChangeTrackStart();

                base.Inspect();
                pegi.Nl();

                if (changed)
                    targetValue = CurrentValue;

                if (lerpMode != LerpSpeedMode.LerpDisabled)
                    "Target".PegiLabel().Edit(ref targetValue).Nl();
            }

            #endregion

            #region Encode & Decode

            public override CfgEncoder Encode()
            {
                var cody = new CfgEncoder()
                    .Add("b", base.Encode)
                    .Add("t", targetValue);

                return cody;
            }

            public override void DecodeTag(string key, CfgData data)
            {
                switch (key)
                {
                    case "b": data.ToDelegate(base.DecodeTag); break;
                    case "t": targetValue = data.ToQuaternion(); break;
                }
            }

            #endregion

            protected BaseQuaternionLerp()
            {
                lerpMode = LerpSpeedMode.SpeedThreshold;
            }
        }

        public abstract class BaseFloatLerp : BaseLerpGeneric<float>
        {

            protected virtual bool CanLerp => true;

            protected override bool LerpInternal(float linkedPortion)
            {
                if (CanLerp && (!defaultSet || Mathf.Approximately(CurrentValue, TargetValue) == false))
                    CurrentValue = Mathf.Lerp(CurrentValue, TargetValue, linkedPortion);
                else return false;

                return true;
            }

            protected override bool Portion(ref float linkedPortion, bool unscaledTime) =>
                 LerpUtils.SpeedToMinPortion(SpeedLimit, CurrentValue - TargetValue, ref linkedPortion, unscaledTime: unscaledTime);

            #region Inspect

            public override void Inspect()
            {
                base.Inspect();

                if (Application.isPlaying)
                    "{0} => {1}".F(CurrentValue, TargetValue).PegiLabel().Nl();

            }

            #endregion

            #region Encode & Decode

            public override CfgEncoder Encode() => new CfgEncoder()
                .Add("b", base.Encode);

            public override void DecodeTag(string key, CfgData data)
            {
                switch (key)
                {
                    case "b": data.ToDelegate(base.DecodeTag); break;
                }
            }

            #endregion
        }

        public abstract class BaseMaterialTextureTransition : BaseFloatLerp, ICfgCustom
        {
            private float _portion;

            private readonly ShaderProperty.FloatValue transitionProperty;
            protected readonly ShaderProperty.TextureValue currentTexturePrTextureValue;
            protected readonly ShaderProperty.TextureValue nextTexturePrTextureValue;

            private enum OnStart
            {
                Nothing = 0,
                ClearTexture = 1,
                LoadCurrent = 2
            }

            private OnStart _onStart = OnStart.Nothing;

            public override float TargetValue
            {
                get { return Mathf.Max(0, _targetTextures.Count - 1); }
                set { }
            }

            public override float CurrentValue
            {
                get { return _portion; }
                set
                {

                    _portion = value;

                    while (_portion >= 1)
                    {
                        _portion -= 1;
                        if (_targetTextures.Count > 1)
                        {
                            RemovePreviousTexture();
                            Current = _targetTextures[0];
                            if (_targetTextures.Count > 1)
                                Next = _targetTextures[1];
                        }
                    }

                    if (Material)
                        Material.Set(transitionProperty, _portion);
                }
            }

            protected readonly List<Texture> _targetTextures = new List<Texture>();

            protected abstract Material Material { get; }

            protected virtual Texture Current
            {
                get { return Material.Get(currentTexturePrTextureValue); }
                set { Material.Set(currentTexturePrTextureValue, value); }
            }

            protected virtual Texture Next
            {
                get { return Material.Get(nextTexturePrTextureValue); }
                set { Material.Set(nextTexturePrTextureValue, value); }
            }

            protected virtual void RemovePreviousTexture() => _targetTextures.RemoveAt(0);

            private bool TryRemoveTill<T>(List<T> list, int maxCountLeft)
            {
                if (list == null || list.Count <= maxCountLeft) return false;

                list.RemoveRange(maxCountLeft, list.Count - maxCountLeft);
                return true;

            }

            public virtual Texture TargetTexture
            {
                get { return _targetTextures.TryGetLast(); }

                set
                {
                    if (!value || !Material) return;

                    if (_targetTextures.Count == 0)
                    {
                        _targetTextures.Add(null);
                        _targetTextures.Add(value);
                        Current = null;
                        Next = value;
                    }
                    else
                    {

                        if (value == _targetTextures[0])
                        {
                            if (_targetTextures.Count > 1)
                            {
                                _targetTextures.Swap(0, 1);
                                CurrentValue = Mathf.Max(0, 1 - CurrentValue);
                                Current = Next;
                                Next = value;
                                TryRemoveTill(_targetTextures, 2);
                            }
                        }
                        else if (_targetTextures.Count > 1 && value == _targetTextures[1])
                        {
                            TryRemoveTill(_targetTextures, 2);
                        }
                        else
                        {
                            if (_targetTextures.Count == 1)
                            {
                                _targetTextures.Add(value);
                                Next = value;
                            }
                            else
                            {
                                if (_targetTextures[1] == value && _targetTextures.Count == 3)
                                    _targetTextures.RemoveAt(2);
                                else
                                    _targetTextures.ForceSet(2, value);
                            }
                        }
                    }
                }
            }

            protected BaseMaterialTextureTransition()
            {
                transitionProperty = TransitionPortion;
                currentTexturePrTextureValue = CurrentTexture;
                nextTexturePrTextureValue = NextTexture;
            }

            // private static readonly ShaderProperty.VectorValue ImageProjectionPosition = new ShaderProperty.VectorValue("_imgProjPos");
            private static readonly ShaderProperty.TextureValue NextTexture = new ShaderProperty.TextureValue("_Next_MainTex");
            private static readonly ShaderProperty.TextureValue CurrentTexture = new ShaderProperty.TextureValue("_MainTex_Current");
            private static readonly ShaderProperty.FloatValue TransitionPortion = new ShaderProperty.FloatValue("_Transition");

            #region Inspector

            public override void Inspect()
            {
                base.Inspect();

                var tex = Current;
          
                "On Start:".PegiLabel(60).Edit_Enum(ref _onStart).Nl();

                if ("Texture[{0}]".F(_targetTextures.Count).PegiLabel(90).Edit(ref tex).Nl())
                    TargetTexture = tex;
            }

            #endregion

            #region Encode & Decode

            public override CfgEncoder Encode()
            {

                var cody = new CfgEncoder()
                    .Add("b", base.Encode)
                    .Add_IfNotZero("onStart", (int)_onStart);
                  
                return cody;
            }

            public override void DecodeTag(string key, CfgData data)
            {
                switch (key)
                {
                    case "b": data.ToDelegate(base.DecodeTag); break;
                    case "clear": _onStart = OnStart.ClearTexture; break;
                    case "onStart": _onStart = (OnStart)data.ToInt(); break;
                }
            }

            public void DecodeInternal(CfgData data)
            {
                _onStart = OnStart.Nothing;
                this.DecodeTagsFrom(data);

                if (_onStart == OnStart.ClearTexture)
                {
                    Current = null;
                    Next = null;
                    _targetTextures.Clear();
                }
            }

            #endregion
        }

        public abstract class BaseMaterialAtlasedTextureTransition : BaseMaterialTextureTransition
        {
            private readonly Dictionary<Texture, Rect> offsets = new Dictionary<Texture, Rect>();

            private void NullOffset(Texture tex)
            {
                if (tex && offsets.ContainsKey(tex))
                    offsets.Remove(tex);
            }

            private Rect GetRect(Texture tex)
            {
                if (tex && offsets.TryGetValue(tex, out Rect rect))
                    return rect;
                return new Rect(0, 0, 1, 1);

            }

            public void SetTarget(Texture tex, Rect offset)
            {
                TargetTexture = tex;
                if (tex)
                    offsets[tex] = offset;
            }

            public override Texture TargetTexture
            {
                get { return base.TargetTexture; }

                set
                {
                    NullOffset(value);
                    base.TargetTexture = value;

                }
            }

            protected override void RemovePreviousTexture()
            {

                NullOffset(_targetTextures[0]);

                base.RemovePreviousTexture();
            }

            protected override Texture Current
            {
                get { return base.Current; }
                set
                {
                    base.Current = value;
                    currentTexturePrTextureValue.Set(Material, GetRect(value));
                }
            }

            protected override Texture Next
            {
                get { return base.Next; }
                set
                {
                    base.Next = value;
                    nextTexturePrTextureValue.Set(Material, GetRect(value));
                }
            }

        }

        public abstract class BaseShaderLerp<T> : BaseLerpGeneric<T>
        {

            public Material material;
            public Renderer rendy;

            protected Material Material => rendy ? rendy.MaterialWhatever() : material;

            public abstract ShaderProperty.IndexGeneric<T> Property { get; }

            protected override string Name_Internal
            {
                get
                {
                    if (rendy)
                        return rendy.gameObject.name;
                    if (material)
                        return material.name;
                    return "?";

                }
            }

            protected sealed override bool LerpInternal(float linkedPortion)
            {
                if (LerpSubInternal(linkedPortion))
                    Set();
                else
                    return false;

                return true;
            }

            protected abstract bool LerpSubInternal(float linkedPortion);

            protected void Set() => Set(Material);

            public abstract void Set(Material on);

            protected BaseShaderLerp(float startingSpeed = 1, Material m = null, Renderer renderer = null)
            {
                SpeedLimit = startingSpeed;
                material = m;
                rendy = renderer;
            }

            #region Encode & Decode

            public override CfgEncoder Encode()
            {
                var cody = new CfgEncoder()
                    .Add("b", base.Encode);

                return cody;
            }

            public override void DecodeTag(string key, CfgData data)
            {
                switch (key)
                {
                    case "b": data.ToDelegate(base.DecodeTag); break;
                }
            }

            #endregion

        }

        public abstract class BaseColorLerp : BaseLerpGeneric<Color>
        {
            protected override string Name_Internal => "Base Color";

            public Color targetValue = Color.white;

            public override Color TargetValue
            {
                get { return targetValue; }
                set { targetValue = value; }
            }

            protected override bool Portion(ref float linkedPortion, bool unscaledTime) =>
                 LerpUtils.SpeedToMinPortion(SpeedLimit, LerpUtils.DistanceRgba(CurrentValue, targetValue), ref linkedPortion, unscaledTime: unscaledTime);

            protected sealed override bool LerpInternal(float linkedPortion)
            {
                if (Enabled && (targetValue != CurrentValue || !defaultSet))
                    CurrentValue = Color.Lerp(CurrentValue, targetValue, linkedPortion);
                else return false;

                return true;
            }

            #region Encode & Decode

            public override CfgEncoder Encode() => new CfgEncoder()
                .Add("b", base.Encode)
                .Add("col", targetValue);

            public override void DecodeTag(string key, CfgData data)
            {
                switch (key)
                {
                    case "b": data.ToDelegate(base.DecodeTag); break;
                    case "col": targetValue = data.ToColor(); break;
                }
            }

            #endregion

            #region Inspector

            public override void Inspect()
            {
                base.Inspect();

                pegi.Edit(ref targetValue).Nl();
            }

            #endregion
        }

        #endregion

        #region Value Types

        public class FloatValue : BaseFloatLerp, IGotName
        {
            private readonly string _name;

            public float targetValue;

            private bool minMax;

            private float min;

            private float max = 1;

            public sealed override float CurrentValue { get; set; }

            public override float TargetValue
            {
                get { return targetValue; }
                set { targetValue = value; }
            }

            protected override string Name_Internal => _name;

            #region Inspect
            public string NameForInspector
            {
                get { return _name; }
                set { }
            }

            public override void InspectInList(ref int edited, int ind)
            {
                if (minMax)
                    _name.PegiLabel().Edit(ref targetValue, min, max);
                else
                    _name.PegiLabel().ApproxWidth().Edit(ref targetValue);
                
                if (Icon.Enter.Click())
                    edited = ind;
            }


            #endregion

            #region Encode & Decode

            public override CfgEncoder Encode()
            {
                var cody = new CfgEncoder()
                    .Add("b", base.Encode)
                    .Add("trgf", targetValue)
                    .Add_Bool("rng", minMax);
                if (minMax)
                    cody.Add("min", min)
                        .Add("max", max);

                return cody;
            }

            public override void DecodeTag(string key, CfgData data)
            {
                switch (key)
                {
                    case "b": data.ToDelegate(base.DecodeTag); break;
                    case "trgf": targetValue = data.ToFloat(); break;
                    case "rng": minMax = data.ToBool(); break;
                    case "min": min = data.ToFloat(); break;
                    case "max": max = data.ToFloat(); break;
                    default: base.DecodeTag(key, data); break;
                }
            }

            #endregion


            public FloatValue()
            {
                _name = "Float Value";
                targetValue = 1;
                SpeedLimit = 1;
                CurrentValue = 1;
            }

            public FloatValue(string name = "Float Value", float initialValue = 1, float maxSpeed = 1)
            {
                _name = name;
                targetValue = initialValue;
                CurrentValue = initialValue;
                SpeedLimit = maxSpeed;
            }

            public FloatValue(float initialValue, float maxSpeed, float min, float max, string name)
            {
                _name = name;
                targetValue = initialValue;
                CurrentValue = initialValue;
                SpeedLimit = maxSpeed;
                minMax = true;
                this.min = min;
                this.max = max;
            }

        }

        public class ColorValue : BaseColorLerp, IGotName
        {
            protected readonly string _name = "Color value";

            protected override string Name_Internal => _name;

            protected Color currentValue;

            public override Color CurrentValue
            {
                get { return currentValue; }
                set { currentValue = value; }
            }

            public string NameForInspector
            {
                get { return _name; }
                set { }
            }

            public ColorValue()
            {
            }

            public ColorValue(string name)
            {
                _name = name;
            }

            public ColorValue(string name, float maxSpeed)
            {
                _name = name;
                this.SpeedLimit = maxSpeed;
            }


        }

        public class Vector3Value : BaseLerpGeneric<Vector3>
        {
            protected readonly string _name = "Vector3 value";

            public Vector3 targetValue;

            public Vector3 currentValue;

            public override Vector3 TargetValue
            {
                get { return targetValue; }
                set { targetValue = value; }
            }

            public override bool UsingLinkedThreshold => base.UsingLinkedThreshold && Enabled;

            public override Vector3 CurrentValue { get => currentValue; set { currentValue = value; } }

            protected override string Name_Internal => _name;

            public Vector3Value()
            {

            }

            public Vector3Value(string name)
            {
                _name = name;
            }

            public Vector3Value(string name, float maxSpeed)
            {
                _name = name;
                this.SpeedLimit = maxSpeed;
            }

            protected override bool LerpInternal(float linkedPortion)
            {
                if (CurrentValue != targetValue || !defaultSet)
                    CurrentValue = Vector3.Lerp(CurrentValue, targetValue, linkedPortion);
                else return false;

                return true;
            }

            protected override bool Portion(ref float linkedPortion, bool unscaledTime)
            {

                var magnitude = (CurrentValue - targetValue).magnitude;

                var modSpeed = SpeedLimit;

                return  LerpUtils.SpeedToMinPortion(modSpeed, magnitude, ref linkedPortion, unscaledTime: unscaledTime);
            }

            #region Inspector

            public override void InspectInList(ref int edited, int ind)
            {
                base.InspectInList(ref edited, ind);

                if (pegi.ChangeTrackStart())
                    targetValue = CurrentValue;
            }

            public override void Inspect()
            {
                pegi.Nl();

                var changed = pegi.ChangeTrackStart();

                base.Inspect();
                pegi.Nl();

                if (changed)
                    targetValue = CurrentValue;

                if (lerpMode != LerpSpeedMode.LerpDisabled)
                    "Target".PegiLabel().Edit(ref targetValue).Nl();
            }

            #endregion

            #region Encode & Decode

            public override CfgEncoder Encode()
            {
                var cody = new CfgEncoder()
                    .Add("b", base.Encode)
                    .Add("t", CurrentValue);

                return cody;
            }

            public override void DecodeTag(string key, CfgData data)
            {
                switch (key)
                {
                    case "b": data.ToDelegate(base.DecodeTag); break;
                    case "t": targetValue = data.ToVector3(); break;
                }
            }

            #endregion


        }

        public class Vector2Value : BaseVector2Lerp
        {
            protected readonly string _name = "Vector2 value";

            public Vector2 currentValue;

            public override bool UsingLinkedThreshold => base.UsingLinkedThreshold && Enabled;

            public override Vector2 CurrentValue { get => currentValue; set { currentValue = value; } }

            protected override string Name_Internal => _name;

            public Vector2Value()
            {

            }

            public Vector2Value(string name)
            {
                _name = name;
            }

            public Vector2Value(string name, float maxSpeed = 1)
            {
                _name = name;
                SpeedLimit = maxSpeed;
            }
        }

        public class QuaternionValue : BaseQuaternionLerp
        {
            private Quaternion current = Quaternion.identity;

            private readonly string _name;

            public sealed override Quaternion CurrentValue
            {
                get { return current; }
                set { current = value; }
            }

            protected override string Name_Internal => _name;

            #region Inspect

            public override void InspectInList(ref int edited, int ind)
            {
                Name_Internal.PegiLabel().ApproxWidth().Edit(ref targetValue);
                
                if (Icon.Enter.Click())
                    edited = ind;
            }

            #endregion

            #region Encode & Decode

            public override CfgEncoder Encode()
            {
                var cody = new CfgEncoder()
                    .Add("b", base.Encode)
                    .Add("trgf", targetValue);

                return cody;
            }

            public override void DecodeTag(string key, CfgData data)
            {
                switch (key)
                {
                    case "b": data.ToDelegate(base.DecodeTag); break;
                    case "trgf": targetValue = data.ToQuaternion(); break;
                }
            }

            #endregion

            public QuaternionValue()
            {
            }

            public QuaternionValue(string name)
            {
                _name = name;
            }

            public QuaternionValue(string name, Quaternion initialValue)
            {
                _name = name;
                targetValue = initialValue;
                CurrentValue = initialValue;
            }

            public QuaternionValue(string name, Quaternion initialValue, float maxSpeed)
            {
                _name = name;
                targetValue = initialValue;
                CurrentValue = initialValue;
                SpeedLimit = maxSpeed;
            }
        }

        #endregion

        #region Rect Transform

        public class RectTransformSizeDelta : BaseVector2Lerp
        {
            readonly RectTransform _transform;
            protected override string Name_Internal => "Size Delta";

            public void PortionX(LerpData ld, float targetValueX) => Portion(ld, targetValue: CurrentValue.X(targetValueX));
            public void PortionY(LerpData ld, float targetValueY) => Portion(ld, targetValue: CurrentValue.Y(targetValueY));

            public override Vector2 CurrentValue
            {
                get { return _transform.sizeDelta; }
                set { _transform.sizeDelta = value; }
            }

            public RectTransformSizeDelta(RectTransform transform, float maxSpeed)
            {
                _transform = transform;
                SpeedLimit = maxSpeed;
            }
        }

        #endregion

        #region Transform

        public abstract class TransformQuaternionBase : BaseLerpGeneric<Quaternion>
        {
            public override bool Enabled => base.Enabled && transform;

            protected readonly Transform transform;

            public sealed override Quaternion TargetValue
            {
                get;
                set;
            }

            protected TransformQuaternionBase(Transform transform, float maxSpeed)
            {
                this.transform = transform;
                SpeedLimit = maxSpeed;
            }

            protected override bool LerpInternal(float portion)
            {
                if (Enabled && CurrentValue != TargetValue)
                    CurrentValue = Quaternion.Lerp(CurrentValue, TargetValue, portion);
                else return false;

                return true;
            }

            protected override bool Portion(ref float portion, bool unscaledTime) =>
                 LerpUtils.SpeedToMinPortion(SpeedLimit, Quaternion.Angle(CurrentValue, TargetValue), ref portion, unscaledTime: unscaledTime);
        }

        public abstract class TransformVector3Base : BaseLerpGeneric<Vector3>
        {

            public override bool Enabled => base.Enabled && transform;

            public Transform transform;

            public Vector3 targetValue;

            public override Vector3 TargetValue { get { return targetValue; } set { targetValue = value; } }

            public TransformVector3Base(Transform transform, float nspeed)
            {
                this.transform = transform;
                SpeedLimit = nspeed;
            }

            protected override bool LerpInternal(float portion)
            {
                if (Enabled && CurrentValue != targetValue)
                    CurrentValue = Vector3.Lerp(CurrentValue, targetValue, portion);
                else return false;

                return true;
            }

            protected override bool Portion(ref float portion, bool unscaledTime) =>
                 LerpUtils.SpeedToMinPortion(SpeedLimit, (CurrentValue - targetValue).magnitude, ref portion, unscaledTime: unscaledTime);
        }

        public class TransformLocalScale : TransformVector3Base
        {
            protected override string Name_Internal => "Local Scale";

            public override Vector3 CurrentValue
            {
                get { return transform.localScale; }
                set { transform.localScale = value; }
            }

            public TransformLocalScale(Transform transform, float maxSpeed) : base(transform, maxSpeed)
            {
            }
        }

        public class TransformPosition : TransformVector3Base
        {
            protected override string Name_Internal => "Position";

            public override Vector3 CurrentValue
            {
                get { return transform.position; }
                set { transform.position = value; }
            }

            public TransformPosition(Transform transform, float maxSpeed) : base(transform, maxSpeed) { }
        }

        public class TransformLocalPosition : TransformVector3Base
        {
            protected override string Name_Internal => "Local Position " + (transform ? transform.name : "NULL TF");

            public override Vector3 CurrentValue
            {
                get { return transform.localPosition; }
                set { transform.localPosition = value; }
            }

            public TransformLocalPosition(Transform transform, float maxSpeed) : base(transform, maxSpeed)
            {

            }
        }

        public class TransformLocalRotation : TransformQuaternionBase
        {
            protected override string Name_Internal => "Local Rotation" + (transform ? transform.name : "NULL TF");

            public sealed override Quaternion CurrentValue
            {
                get => transform.localRotation;
                set => transform.localRotation = value;
            }

            public TransformLocalRotation(Transform transform, float maxSpeed) : base(transform, maxSpeed)
            {
                TargetValue = CurrentValue;
            }
        }


        #endregion

        #region Material

        public class ShaderFloatFeature : ShaderFloat
        {
           // public ShaderFloatFeature(string nName, float initialValue, float maxSpeed = 1, Renderer renderer = null, Material m = null) 
            //    : base( nName,  initialValue,  maxSpeed ,  renderer ,  m ) {}

            public ShaderFloatFeature(string nName, string featureDirective, float initialValue = 0, float maxSpeed = 1, Renderer renderer = null, Material m = null) 
                : base( nName,  featureDirective,  initialValue,  maxSpeed,  renderer,  m ) {}
        }

        public class ShaderFloat : BaseShaderLerp<float>
        {
            private readonly ShaderProperty.IndexGeneric<float>//ShaderProperty.FloatValue 
                _property;

            public override ShaderProperty.IndexGeneric<float> Property => _property;

            protected override string Name_Internal =>
                _property != null ? _property.ToString() : "Material Float";

            public override float TargetValue
            {
                get => targetValue;
                set => targetValue = value;
            }

            public sealed override float CurrentValue
            {
                get => _property.latestValue;
                set
                {
                    _property.latestValue = value;
                    defaultSet = false;
                }
            }

            private float targetValue;

            public override void Set(Material mat)
            {
                if (mat)
                    mat.Set(_property);
                else
                    _property.GlobalValue = _property.latestValue;
            }

            public ShaderFloat(string nName, float initialValue, float maxSpeed = 1, Renderer renderer = null,
                Material m = null) : base(maxSpeed, m, renderer)
            {
                _property = new ShaderProperty.FloatValue(nName);
                targetValue = initialValue;
                CurrentValue = initialValue;
            }

            public ShaderFloat(string nName, string featureDirective, float initialValue = 0, float maxSpeed = 1, Renderer renderer = null,
                Material m = null) : base(maxSpeed, m, renderer)
            {
                _property = new ShaderProperty.FloatFeature(nName, featureDirective);
                targetValue = initialValue;
                CurrentValue = initialValue;
            }

            protected override bool Portion(ref float linkedPortion, bool unscaledTime) =>
                 LerpUtils.SpeedToMinPortion(SpeedLimit, CurrentValue - targetValue, ref linkedPortion, unscaledTime: unscaledTime);

            protected override bool LerpSubInternal(float portion)
            {
                if (Enabled && (!Mathf.Approximately(CurrentValue, targetValue) || !defaultSet))
                {
                    _property.latestValue = Mathf.Lerp(CurrentValue, targetValue, portion);
                    return true;
                }

                return false;
            }

            #region Inspector

            public override void Inspect()
            {
                base.Inspect();

                "Target".PegiLabel(70).Edit(ref targetValue).Nl();

                if ("Value".PegiLabel(60).Edit(ref _property.latestValue).Nl())
                    Set();
            }

            #endregion

            #region Encode & Decode

            public override CfgEncoder Encode()
            {

                var cody = new CfgEncoder()
                    .Add("b", base.Encode)
                    .Add("f", targetValue);

                return cody;
            }

            public override void DecodeTag(string key, CfgData data)
            {
                switch (key)
                {
                    case "b": data.ToDelegate(base.DecodeTag); break;
                    case "f": targetValue = data.ToFloat(); break;
                }
            }

            #endregion
        }

        public class ShaderColor : BaseShaderLerp<Color>
        {
            private readonly ShaderProperty.IndexGeneric<Color> _property;

            public override ShaderProperty.IndexGeneric<Color> Property => _property;
            protected override string Name_Internal => _property != null ? _property.ToString() : "Material Float";

            public override Color TargetValue { get; set; }

            public sealed override Color CurrentValue
            {
                get { return _property.latestValue; }
                set
                {
                    _property.latestValue = value;
                    defaultSet = false;
                }
            }

            public override void Set(Material mat)
            {
                if (mat)
                    mat.Set(_property);
                else
                    _property.GlobalValue = CurrentValue;
            }

            public ShaderColor(string nName, Color initialValue, float maxSpeed = 1, Material m = null,
                Renderer renderer = null) : base(maxSpeed, m, renderer)
            {
                _property = new ShaderProperty.ColorFloat4Value(nName);
                CurrentValue = initialValue;
            }

            public ShaderColor(string nName, Color initialValue, string shaderFeatureDirective, float maxSpeed = 1, Material m = null,
                Renderer renderer = null) : base(maxSpeed, m, renderer)
            {
                _property = new ShaderProperty.ColorFeature(nName, shaderFeatureDirective);
                CurrentValue = initialValue;
            }

            protected override bool LerpSubInternal(float portion)
            {
                if (CurrentValue != TargetValue || !defaultSet)
                {
                    _property.latestValue = Color.Lerp(CurrentValue, TargetValue, portion);
                    return true;
                }

                return false;
            }

            protected override bool Portion(ref float portion, bool unscaledTime) =>
                 LerpUtils.SpeedToMinPortion(SpeedLimit,  LerpUtils.DistanceRgba(CurrentValue, TargetValue), ref portion, unscaledTime: unscaledTime);

            #region Encode & Decode

            public override CfgEncoder Encode()
            {

                var cody = new CfgEncoder()
                    .Add("b", base.Encode)
                    .Add("c", TargetValue);

                return cody;
            }

            public override void DecodeTag(string key, CfgData data)
            {
                switch (key)
                {
                    case "b": data.ToDelegate(base.DecodeTag); break;
                    case "c": TargetValue = data.ToColor(); break;
                }
            }

            #endregion

        }

        public class ShaderVector4 : BaseShaderLerp<Vector4>
        {
            private readonly ShaderProperty.IndexGeneric<Vector4> _property;

            public override ShaderProperty.IndexGeneric<Vector4> Property => _property;
            protected override string Name_Internal => _property != null ? _property.ToString() : "Material Float4";

            public override Vector4 TargetValue
            {
                get;
                set;
            }

            public sealed override Vector4 CurrentValue
            {
                get => _property.latestValue;
                set
                {
                    _property.latestValue = value;
                    defaultSet = false;
                }
            }


            public override void Set(Material mat)
            {
                if (mat)
                    mat.Set(_property);
                else
                    _property.GlobalValue = CurrentValue;
            }

            public ShaderVector4(string nName, Vector4 initialValue, float maxSpeed = 1, Material m = null,
                Renderer renderer = null) : base(maxSpeed, m, renderer)
            {
                _property = new ShaderProperty.VectorValue(nName);
                CurrentValue = initialValue;
            }

            protected override bool LerpSubInternal(float portion)
            {
                if (CurrentValue != TargetValue || !defaultSet)
                {
                    _property.latestValue = Vector4.Lerp(CurrentValue, TargetValue, portion);
                    return true;
                }

                return false;
            }

            protected override bool Portion(ref float portion, bool unscaledTime) =>
                 LerpUtils.SpeedToMinPortion(SpeedLimit, (CurrentValue - TargetValue).magnitude, ref portion, unscaledTime: unscaledTime);

            #region Encode & Decode

            public override CfgEncoder Encode()
            {

                var cody = new CfgEncoder()
                    .Add("b", base.Encode)
                    .Add("v4", TargetValue);

                return cody;
            }

            public override void DecodeTag(string key, CfgData data)
            {
                switch (key)
                {
                    case "b": data.ToDelegate(base.DecodeTag); break;
                    case "v4": TargetValue = data.ToColor(); break;
                }
            }

            #endregion

        }

        #endregion

        #region UIElement Values

        public class CanvasGroupAlpha : BaseFloatLerp
        {
            public CanvasGroup CanvasGroup
            {
                get;
                set;
            }

            private readonly bool _disableInteractivityOnFade;

            private float targetValue;

            public override float TargetValue
            {
                get => targetValue; 
                set => targetValue = value; 
            }

            public override float CurrentValue
            {
                get => CanvasGroup ? CanvasGroup.alpha : targetValue;
                set
                {
                    if (_disableInteractivityOnFade) 
                    {
                        bool interactable = Mathf.Approximately(value, 1) || value > CanvasGroup.alpha;
                        CanvasGroup.interactable = interactable;
                        CanvasGroup.blocksRaycasts = interactable;
                    }

                    CanvasGroup.alpha = value;
                }
            }

            protected override string Name_Internal => "Graphic Alpha";

            public CanvasGroupAlpha() { }

            public CanvasGroupAlpha(CanvasGroup canvasGroup, float maxSpeed, bool disableInteractivityOnFade)
            {
                SpeedLimit = maxSpeed;
                CanvasGroup = canvasGroup;
                _disableInteractivityOnFade = disableInteractivityOnFade;
            }
        }

        public class GraphicAlpha : BaseFloatLerp, ICfgCustom
        {

            protected Graphic graphic;

            public Graphic Graphic
            {
                get { return graphic; }
                set
                {
                    graphic = value;
                    if (setZeroOnStart && !defaultSet)
                    {
                        graphic.TrySetAlpha(0);
                        defaultSet = true;
                    }
                }
            }

            protected float targetValue;
            public bool setZeroOnStart = true;

            public override float TargetValue
            {
                get { return targetValue; }
                set { targetValue = value; }
            }

            public override float CurrentValue
            {
                get { return graphic ? graphic.color.a : targetValue; }
                set { graphic.TrySetAlpha(value); }
            }

            protected override string Name_Internal => "Graphic Alpha";

            public GraphicAlpha()
            {
            }

            public GraphicAlpha(Graphic graphic)
            {
                this.graphic = graphic;
            }

            #region Encode & Decode

            public void DecodeInternal(CfgData data)
            {
                //base.Decode(data);
                this.DecodeTagsFrom(data);

                if (setZeroOnStart && !defaultSet)
                    graphic.TrySetAlpha(0);
            }

            public override CfgEncoder Encode() =>
                new CfgEncoder().Add("bb", base.Encode).Add_Bool("zero", setZeroOnStart);

            public override void DecodeTag(string key, CfgData data)
            {
                switch (key)
                {
                    case "bb":
                        data.ToDelegate(base.DecodeTag);
                        break;
                    case "zero":
                        setZeroOnStart = data.ToBool();
                        break;
                    default: base.DecodeTag(key, data); break;
                }
            }

            #endregion

            #region Inspect

            public override void Inspect()
            {
                base.Inspect();

                "Set zero On Start".PegiLabel().ToggleIcon(ref setZeroOnStart).Nl();
            }

            #endregion


        }

        public class GraphicColor : BaseColorLerp
        {

            protected override string Name_Internal => "Graphic Color";

            public Graphic graphic;

            public override Color CurrentValue
            {
                get { return graphic ? graphic.color : targetValue; }
                set { graphic.color = value; }
            }

            public GraphicColor()
            {
            }

            public GraphicColor(Graphic graphic)
            {
                this.graphic = graphic;
            }

        }

        #endregion
    }

    public class LerpData : IPEGI, IGotName, IGotCount, IPEGI_ListInspect
    {
        private int _lerpChangeVersion = -1;
        private float _linkedPortion = 1;
        internal bool _unscaledTime;
        public string dominantParameter = "None";

        public ChangesToken StartChangeTrack() => new ChangesToken(this);

        internal void OnValueLerped() => _lerpChangeVersion++;

        public void AddPortion(float value, ILinkedLerping lerp)
        {
            if (value < MinPortion)
            {
                dominantParameter = lerp.GetNameForInspector();
                MinPortion = value;
            }
        }

        public float Portion(bool skipLerp = false) => skipLerp ? 1 : _linkedPortion;

        public bool Done => Mathf.Approximately(_linkedPortion, 1);//Math.Abs(_linkedPortion - 1) < float.Epsilon*10;

        public float MinPortion
        {
            get { return _linkedPortion; }
            set { _linkedPortion = Mathf.Min(_linkedPortion, value); }
        }

        public void Reset()
        {
            _linkedPortion = 1;
            _resets++;
        }

        public LerpData (bool unscaledTime) 
        {
            _unscaledTime = unscaledTime;
        }

        #region Inspector
        private int _resets;

        public string NameForInspector
        {
            get { return dominantParameter; }
            set { dominantParameter = value; }
        }

        public int GetCount() => _resets;

        public void Inspect()
        {
            "Slowest:".PegiLabel().Write_ForCopy(dominantParameter).Nl();
            "Updates: {0}".F(_resets).PegiLabel().Nl();
        }

        public void InspectInList(ref int edited, int ind)
        {
            "Lerp DP: {0} [{1}]".F(dominantParameter, _resets).PegiLabel().Write();

            if (Icon.Refresh.Click("Reset stats"))
            {
                dominantParameter = "None";
                _resets = 0;
            }

            if (Icon.Enter.Click())
                edited = ind;
        }

        #endregion
    
        public class ChangesToken 
        {
            private readonly LerpData _data;
            private readonly int _originalVersion;

            public bool Changed => _originalVersion != _data._lerpChangeVersion;

            public static implicit operator bool(ChangesToken me) => me.Changed;

            public ChangesToken(LerpData data) 
            {
                _data = data;
                _originalVersion = _data._lerpChangeVersion;
            }
        }

    }
}