using System;
using System.Collections.Generic;
using QuizCanners.Inspect;
using UnityEngine;
using QuizCanners.Migration;

namespace QuizCanners.Utils 
{
    public static class ShaderProperty 
    {
        #region Base Abstract

        public abstract class BaseShaderPropertyIndex : IPEGI_ListInspect, IPEGI
        {
            protected int id;
            protected string name;
            public override bool Equals(object obj)
            {
                var bi = obj as BaseShaderPropertyIndex;

                if (bi?.name == null)
                    return false;
                
                return bi.id == id;
            }

            public override int GetHashCode() => id;
            private void UpdateIndex() => id = Shader.PropertyToID(name);

            public override string ToString() => name;

            public abstract void SetLatestValueOn(Material mat);
            public abstract void SetLatestValueOn(MaterialPropertyBlock block);

            public Renderer SetLatestValueOn(Renderer renderer, MaterialPropertyBlock block, int materialIndex = 0)
            {
                renderer.GetPropertyBlock(block, materialIndex);
                SetLatestValueOn(block);
                renderer.SetPropertyBlock(block, materialIndex);
                return renderer;
            }

            #region Inspector
            
            public virtual void InspectInList(ref int edited, int ind) =>
                name.PegiLabel(toolTip: "Id: {0}".F(id), width: 90).Write_ForCopy();
            
            public virtual void Inspect()=>
                name.PegiLabel().Write_ForCopy();
            #endregion

            #region Constructors
            protected BaseShaderPropertyIndex() { }
            protected BaseShaderPropertyIndex(string name)
            {
                this.name = name;
                UpdateIndex();
            }
            #endregion
        }

        public static bool Has<T>(this Material mat, T property) where T : BaseShaderPropertyIndex =>
            mat.HasProperty(property.GetHashCode());

        #endregion

        #region Generics
 
        public abstract class IndexGeneric<T> : BaseShaderPropertyIndex {
            
            [SerializeField] public T latestValue;
            public bool GlobalValueSet { get; private set; }
            private bool lastValueSet;

            public abstract T Get(Material mat);
            public abstract T Get(MaterialPropertyBlock block);

            protected abstract T GlobalValue_Internal { get; set; }

            public T GlobalValue
            {
                get
                {
                    if (lastValueSet) 
                        return latestValue;
                    latestValue = GlobalValue_Internal;
                    lastValueSet = true;
                    return latestValue;
                }
                set
                {
                    latestValue = value;
                    GlobalValue_Internal = value;
                    GlobalValueSet = true;
                }
            }

            public virtual Material SetOn(Material material, T value)
            {
                latestValue = value;

                if (material)
                    SetLatestValueOn(material);

                return material;
            }

            public virtual Renderer SetOn(Renderer renderer, MaterialPropertyBlock block,  T value)
            {
                latestValue = value;
                SetLatestValueOn(renderer, block);
                return renderer;
            }

            public virtual void SetOn(MaterialPropertyBlock block, T value)
            {
                latestValue = value;
                SetLatestValueOn(block);
            }

            public void SetGlobal() => GlobalValue = latestValue;
            public void SetGlobal(T value) => GlobalValue = value;
            public T GetGlobal() => GlobalValue;

            #region Inspector
            public override void InspectInList(ref int edited, int ind)
            {
                base.InspectInList(ref edited, ind);
                if (GlobalValueSet)
                    Icon.SelectAll.Draw(toolTip: "Set as Global value");
            }

            public override void Inspect()
            {
                base.Inspect();
                pegi.Nl();
                if (GlobalValueSet)
                    "Global Value Set: {0}".F(GlobalValue).PegiLabel().Nl();
            }

            #endregion

            protected IndexGeneric() { }
            protected IndexGeneric(string name) : base(name) {}
        }

        public abstract class IndexWithShaderFeatureGeneric<T> : IndexGeneric<T> {

            private readonly string _featureDirective;

            [SerializeField] private bool _directiveGlobalValue;
            
            protected override T GlobalValue_Internal
            {
                set 
                {
                    latestValue = value;
                    
                    if (_directiveGlobalValue == DirectiveEnabledForLastValue)
                        return;

                    _directiveGlobalValue = DirectiveEnabledForLastValue;

                    QcUnity.SetShaderKeyword(_featureDirective, _directiveGlobalValue);
                }
            }

            public override Material SetOn(Material material, T value) 
            {
                var ret =  base.SetOn(material, value);
                material.SetShaderKeyword(_featureDirective, DirectiveEnabledForLastValue);
                return ret;
            }

            protected IndexWithShaderFeatureGeneric(string name, string featureDirective) : base(name)
            {
                _featureDirective = featureDirective;
            }

            protected abstract bool DirectiveEnabledForLastValue { get; }
        }

        public static MaterialPropertyBlock Set<T>(this MaterialPropertyBlock block, IndexGeneric<T> property)
        {
            property.SetLatestValueOn(block);
            return block;
        }

        public static MaterialPropertyBlock Set<T>(this MaterialPropertyBlock block, IndexGeneric<T> property, T value)
        {
            property.SetOn(block, value);
            return block;
        }

        public static Material Set<T>(this Material mat, IndexGeneric<T> property)
        {
            property.SetLatestValueOn(mat);
            return mat;
        }

        public static Material Set<T>(this Material mat, IndexGeneric<T> property, T value) =>
            property.SetOn(mat, value);

        public static Material Set(this Material mat, MaterialToggle property, bool isOn) =>
            property.SetOn(mat, isOn);


        public static T Get<T>(this Material mat, IndexGeneric<T> property) => property.Get(mat);

        public static bool Get(this Material mat, MaterialToggle property) => property.Get(mat);

        public static int Get(this Material mat, KeywordEnum property) => Mathf.RoundToInt(property.Get(mat));
        
        public static void Set(this Material mat, KeywordEnum property, int value) => property.Set(mat, value);    
        

        #endregion

        #region Float
        
        [Serializable]
        public class FloatValue : IndexGeneric<float>, ICfg
        {

            private readonly bool _usingRange;
            private readonly float _min;
            private readonly float _max;

            public override void SetLatestValueOn(Material material) => material.SetFloat(id, latestValue);
            public override float Get(Material material) => material ? material.GetFloat(id) : latestValue;
            public override float Get(MaterialPropertyBlock block) => block.GetFloat(id);
            public override void SetLatestValueOn(MaterialPropertyBlock block) => block.SetFloat(id, latestValue);

            protected override float GlobalValue_Internal
            {
                get => Shader.GetGlobalFloat(id);
                set => Shader.SetGlobalFloat(id, value);
            }

            public override void InspectInList(ref int edited, int ind) => InspectValue();
            
            public override void Inspect()
            {
                base.Inspect();
                pegi.Nl();
                InspectValue().Nl();
            }

            private pegi.ChangesToken InspectValue() =>
                (_usingRange ?
                    name.PegiLabel(0.25f).Edit(ref latestValue, min: _min, max: _max) :
                    name.PegiLabel(0.25f).Edit(ref latestValue))
              .OnChanged(() =>
              {
                  if (GlobalValueSet)
                      GlobalValue = latestValue;
              });

            public CfgEncoder Encode() => new CfgEncoder()
                .Add("val", GlobalValue);

            public void DecodeTag(string key, CfgData data)
            {
                switch (key)
                {
                    case "val": GlobalValue = data.ToFloat(); break;
                }
            }

            public FloatValue() {}

            public FloatValue(string name) : base(name) {}

            public FloatValue(string name, float min, float max) : base(name)
            {
                latestValue = min;
                _usingRange = true;
                _min = min;
                _max = max;
            }
        }

        public class FloatFeature : IndexWithShaderFeatureGeneric<float>, ICfg
        {
            public override void SetLatestValueOn(Material material) => material.SetFloat(id, latestValue);
            public override float Get(Material material) => material.GetFloat(id);
            public override float Get(MaterialPropertyBlock block) => block.GetFloat(id);
            public override void SetLatestValueOn(MaterialPropertyBlock block) => block.SetFloat(id, latestValue);

            protected override float GlobalValue_Internal
            {
                get => Shader.GetGlobalFloat(id);
                set
                {
                    base.GlobalValue_Internal = value;
                    Shader.SetGlobalFloat(id, value);
                }
            }

            protected override bool DirectiveEnabledForLastValue => latestValue > float.Epsilon * 10;

            public override void InspectInList(ref int edited, int ind) => InspectValue();
            public override void Inspect()
            {
               // base.Inspect();
               // pegi.Nl();
                InspectValue().Nl();
            }

            private pegi.ChangesToken InspectValue()
            {
                var changes = pegi.ChangeTrackStart();

                if (GlobalValueSet == false)
                    Icon.InActive.Draw("Global value not set");

                bool useDefine = latestValue > 0;
                if (pegi.ToggleIcon(ref useDefine))
                {
                    latestValue = useDefine ? 1 : 0;
                    if (GlobalValueSet)
                        GlobalValue = latestValue;
                }

                name.PegiLabel(0.25f).Edit_01(ref latestValue).OnChanged(() =>
                {
                    if (GlobalValueSet)
                        GlobalValue = latestValue;
                });

                return changes;
            }

            public CfgEncoder Encode() => new CfgEncoder()
                .Add("val", GlobalValue);

            public void DecodeTag(string key, CfgData data)
            {
                switch (key)
                {
                    case "val": GlobalValue = data.ToFloat(); break;
                }
            }

            public FloatFeature(string name, string featureDirective) : base(name, featureDirective) { }
        }

        #endregion

        #region Int

        [Serializable]
        public class IntValue : IndexGeneric<int>
        {
            private readonly bool _usingRange;
            private readonly int _min;
            private readonly int _max;

            public override void SetLatestValueOn(Material material) => material.SetInteger(id, latestValue);

            public override int Get(Material material) => material.GetInteger(id);
            public override int Get(MaterialPropertyBlock block) => block.GetInteger(id);
            public override void SetLatestValueOn(MaterialPropertyBlock block) => block.SetInteger(id, latestValue);

            protected override int GlobalValue_Internal
            {
                get => Shader.GetGlobalInteger(id);
                set => Shader.SetGlobalInteger(id, value); 
            }

            public override void InspectInList(ref int edited, int ind) =>  InspectValue();
            
            public override void Inspect()
            {
                base.Inspect();
                pegi.Nl();
                InspectValue().Nl();
            }

            private pegi.ChangesToken InspectValue() =>
                (_usingRange ?
                    name.PegiLabel(0.25f).Edit(ref latestValue, minInclusiven: _min, maxInclusive: _max) :
                    name.PegiLabel(0.25f).Edit(ref latestValue))
              .OnChanged(() =>
              {
                  if (GlobalValueSet)
                      GlobalValue = latestValue;
              });


            public IntValue(){}

            public IntValue(string name) : base(name){}

            public IntValue(string name, int min, int max) : base(name)
            {
                _usingRange = true;
                _min = min;
                _max = max;
            }
        }

#endregion

        #region Color

        public class ColorFeature : IndexWithShaderFeatureGeneric<Color>, IPEGI {

            public static readonly ColorValue tintColor = new ("_TintColor");

            public override void SetLatestValueOn(Material material) => material.SetColor(id, latestValue);
            public override Color Get(Material material) => material.GetColor(id);
            public override Color Get(MaterialPropertyBlock material) => material.GetColor(id);
            public override void SetLatestValueOn(MaterialPropertyBlock block) => block.SetColor(id, latestValue);
            
            protected override Color GlobalValue_Internal
            {
                get => Shader.GetGlobalColor(id);
                set {
                    base.GlobalValue_Internal = value;
                    Shader.SetGlobalColor(id, value);
                }
            }

            protected override bool DirectiveEnabledForLastValue => latestValue.a > 0.01f;

            public override void Inspect()
            {
                ToString().PegiLabel().Write(); 
                (DirectiveEnabledForLastValue ? Icon.Active: Icon.InActive).Nl();
                
                if (pegi.Edit(ref latestValue).Nl())
                    GlobalValue = latestValue;
            }

            public ColorFeature(string name, string featureDirective) : base(name, featureDirective) { }
        }

        [Serializable]

        public class ColorValue : IndexGeneric<Color> {

            public static readonly ColorValue tintColor = new("_TintColor");

            public bool ConvertToLinear
            {
                get
                {
                    if (!_colorSpaceChecked)
                        CheckColorSpace();
                    return _convertToLinear;
                }
                set
                {
                    _colorSpaceChecked = true;
                    _convertToLinear = value;
                }
            }
            private bool _convertToLinear;
            private bool _colorSpaceChecked;

            private Color ConvertedColor => ConvertToLinear ? latestValue.linear : latestValue;

            public override void SetLatestValueOn(Material material) => material.SetColor(id, ConvertedColor);

            public override Color Get(Material material) => material.GetColor(id);
            public override Color Get(MaterialPropertyBlock block) => block.GetColor(id);
            public override void SetLatestValueOn(MaterialPropertyBlock block) => block.SetColor(id, ConvertedColor);

            protected override Color GlobalValue_Internal
            {
                get => Shader.GetGlobalColor(id);
                set
                {
                    latestValue = value;
                    Shader.SetGlobalColor(id, ConvertedColor);
                }
            }

            private void CheckColorSpace()
            {
                _colorSpaceChecked = true;
                #if UNITY_EDITOR
                ConvertToLinear = UnityEditor.PlayerSettings.colorSpace == ColorSpace.Linear;
                #endif
            }

            public override void Inspect()
            {
               //base.Inspect();
                if (name.PegiLabel().Edit(ref latestValue, hdr: true).Nl() && GlobalValueSet)
                    GlobalValue = latestValue;
            }

            public ColorValue()
            {
                latestValue = Color.grey;
            }

            public ColorValue(string name) : base(name)
            {
                latestValue = Color.grey;
            }
            
            public ColorValue(string name, bool convertToLinear) : base(name)
            {
                latestValue = Color.grey;
                ConvertToLinear = convertToLinear;
            }

            public ColorValue(string name, Color startingColor, bool convertToLinear) : base(name)
            {
                latestValue = startingColor;
                ConvertToLinear = convertToLinear;
            }

            public ColorValue(string name, Color startingColor) : base(name)
            {
                latestValue = startingColor;
            }
        }

        #endregion

        #region Vector

        public class VectorValue : IndexGeneric<Vector4>
        {
            public override void SetLatestValueOn(Material material) => material.SetVector(id, latestValue);
            public override void SetLatestValueOn(MaterialPropertyBlock block) => block.SetVector(id, latestValue);
            public override Vector4 Get(MaterialPropertyBlock block) => block.GetVector(id);
            public override Vector4 Get(Material mat) => mat.GetVector(id);

            protected override Vector4 GlobalValue_Internal
            {
                get => Shader.GetGlobalVector(id);
                set => Shader.SetGlobalVector(id, value);
            }

            public override void InspectInList(ref int edited, int ind) => InspectValue();

            public override void Inspect()
            {
                base.Inspect();
                pegi.Nl();
                InspectValue().Nl();
            }

            private pegi.ChangesToken InspectValue()
            {
                Icon.Copy.Click().OnChanged(() => pegi.SetCopyPasteBuffer(name, hint: "Valiable name copied to buffer"));

                return name.PegiLabel().Edit(ref latestValue).OnChanged(() =>
                {
                    if (GlobalValueSet)
                        GlobalValue = latestValue;
                });
            }

            public void SetGlobal(float x) => SetGlobal(new Vector4(x, 0));
            public void SetGlobal(float x, float y) => SetGlobal(new Vector4(x, y));
            public void SetGlobal(float x, float y, float z) => SetGlobal(new Vector4(x, y, z));
            public void SetGlobal(float x, float y, float z, float w) => SetGlobal(new Vector4(x, y, z, w));

            public VectorValue() { }
            public VectorValue(string name) : base(name) { }
        }

        #endregion

        #region Matrix

        public class MatrixValue : IndexGeneric<Matrix4x4>
        {
            public override void SetLatestValueOn(Material material) => material.SetMatrix(id, latestValue);
            public override void SetLatestValueOn(MaterialPropertyBlock block) => block.SetMatrix(id, latestValue);

            public override Matrix4x4 Get(Material mat) => mat.GetMatrix(id);
            public override Matrix4x4 Get(MaterialPropertyBlock block) => block.GetMatrix(id);

            protected override Matrix4x4 GlobalValue_Internal
            {
                get => Shader.GetGlobalMatrix(id);
                set => Shader.SetGlobalMatrix(id, value);
            }
            
            public MatrixValue() { }

            public MatrixValue(string name) : base(name) { }
        }

        #endregion

        #region Texture
        
        public class TextureValue : IndexGeneric<Texture>, IPEGI
        {
            public static readonly TextureValue mainTexture = new("_MainTex");
            public static readonly TextureValue bumpMap = new("_BumpMap");

            public override Texture Get(Material mat) => mat.GetTexture(id);
            public override Texture Get(MaterialPropertyBlock block) => block.GetTexture(id);
            public override void SetLatestValueOn(Material material) => material.SetTexture(id, latestValue);
            public override void SetLatestValueOn(MaterialPropertyBlock block) => block.SetTexture(id, latestValue);

            protected override Texture GlobalValue_Internal
            {
                get => Shader.GetGlobalTexture(id);
                set
                {
                    Shader.SetGlobalTexture(id, value);

                    if (_screenFillAspect!=null)
                        Set_ScreenFillAspect();
                }
            }

            #region Texture Specific

            public Vector2 GetOffset(Material mat) => mat ? mat.GetTextureOffset(id) : Vector2.zero;

            public Vector2 GetTiling(Material mat) => mat ? mat.GetTextureScale(id) : Vector2.one;

            public void Set(Material mat, Rect value)
            {
                if (mat) {
                    mat.SetTextureOffset(id, value.min);
                    mat.SetTextureScale(id, value.size);
                }
            }

            public void SetOffset(Material mat, Vector2 value)
            {
                if (mat)
                    mat.SetTextureOffset(id, value);
            }

            public void SetScale(Material mat, Vector2 value)
            {
                if (mat)
                    mat.SetTextureScale(id, value);
            }

            private const string FILL_ASPECT_RATION_SUFFIX = "_ScreenFillAspect";
            private  VectorValue _screenFillAspect;
            private VectorValue GetScreenFillAspect() 
            {
                _screenFillAspect ??= new VectorValue(name + FILL_ASPECT_RATION_SUFFIX);
                return _screenFillAspect;
            }
            public void Set_ScreenFillAspect()
            {
                if (!latestValue)
                {
                   // QcUtils.ChillLogger.LogErrorOnce(name+"noTex", ()=>"{0} was not set. Can't Update {1} ".F(name, FILL_ASPECT_RATION_SUFFIX));
                    return;
                }
                
                float screenAspect = pegi.GameView.AspectRatio;
                float texAspect = ((float)latestValue.width) / latestValue.height;

                Vector4 aspectCorrection = new(1,1, 1f/latestValue.width, 1f/latestValue.height);

                if (screenAspect > texAspect)
                    aspectCorrection.y = (texAspect / screenAspect);
                else
                    aspectCorrection.x = (screenAspect / texAspect);

                GetScreenFillAspect().GlobalValue = aspectCorrection;
            } 

            #endregion

            #region Constructors
        //    public TextureValue() { }

            public TextureValue(string name, bool set_ScreenFillAspect = false) : base(name)
            {
                if (set_ScreenFillAspect)
                    GetScreenFillAspect();
            }

            #endregion

  
            public override void Inspect()
            {
                pegi.Draw(mainTexture.GlobalValue);
                pegi.Nl();
            }

        }

        public static Vector2 GetOffset(this Material mat, TextureValue property) => property.GetOffset(mat);

        public static Vector2 GetTiling(this Material mat, TextureValue property) => property.GetTiling(mat);

        public static void SetOffset(this Material mat, TextureValue property, Vector2 value) =>
            property.SetOffset(mat, value);

        public static void SetTiling(this Material mat, TextureValue property, Vector2 value) =>
            property.SetScale(mat, value);

        public static List<TextureValue> MyGetTextureProperties_Editor(this Material m)
        {
            #if UNITY_EDITOR
            {
                var lst = new List<TextureValue>();
                foreach (var n in m.GetProperties(UnityEditor.MaterialProperty.PropType.Texture))
                    lst.Add(new TextureValue(n));

                return lst;
            }
            #else
            return new List<TextureValue>();
            #endif
        }

        #endregion

        #region Keywords & Toggles

        [Serializable]
        public class KeywordEnum : IPEGI
        {
            private readonly int id;
            private readonly string _name;
            public readonly string[] EnumValues;
            public readonly string[] Keywords;

            public override string ToString() => _name;

            private int lastIndex;

            public float Get(Material material) => material.GetFloat(id);

            public void Set(Material material, int value)
            {
                material.SetFloat(id, value);

                lastIndex = value;

                for (int i=0; i< Keywords.Length; i++) 
                {
                    material.SetShaderKeyword(Keywords[i], lastIndex == i);
                }
            }

            public bool this[int key]
            {
                get => key == lastIndex;
                set
                {
                    lastIndex = key;

                    for (int i = 0; i < Keywords.Length; i++)
                    {
                        QcUnity.SetShaderKeyword(Keywords[i], key == i);
                    }
                }
            }

            public KeywordEnum(string name, params string[] values)
            {
                _name = name;
                id = Shader.PropertyToID(name);
                EnumValues = values;

                Keywords = new string[EnumValues.Length];
                for (int i = 0; i < EnumValues.Length; i++)
                {
                    Keywords[i] = "{0}_{1}".F(_name, EnumValues[i].ToUpperInvariant());
                }
            }

            #region Inspector

            void IPEGI.Inspect()
            {
                _name.PegiLabel().Write_ForCopy();
                pegi.Nl();

                for ( int i=0; i<EnumValues.Length; i++) 
                {
                    var val = EnumValues[i]; 

                    if (lastIndex == i)
                        "Remove {0}".F(val).PegiLabel().Click(() => this[i] = false);
                    else
                        "Add {0}".F(val).PegiLabel().Click(()=> this[i] = true);

                    pegi.Nl();
                }
            }

            #endregion
        }

        [Serializable]
        public class Feature : IPEGI {

            private readonly string _name;

            public override string ToString() => _name;
            
            [SerializeField] private bool lastValue;

            public IDisposable EnableDisposible()
            {
                Enabled = true;
                return QcSharp.DisposableAction(() => Enabled = false);
            }

            public bool Enabled 
            {
                get => lastValue;
                set { lastValue = value; QcUnity.SetShaderKeyword(_name, value); }
            }

            public Feature(string name) {  _name = name; }

            void IPEGI.Inspect()
            {
                if (pegi.ToggleIcon(ref lastValue))
                    Enabled = lastValue;
                _name.PegiLabel().Write_ForCopy();
            }
        }

        [Serializable]
        public class MaterialToggle : IPEGI
        {
            private readonly string _floatProperty;
            private readonly int _floatPropertyId;
            private readonly string _keyword;

            [SerializeField] public bool LastValue;
            public override string ToString() => _keyword;

            public bool Get(Material material) => material.GetFloat(_floatPropertyId) > 0;
            
            public Material SetOn(Material material)
            {
                material.SetFloat(_floatPropertyId, LastValue ? 1 : 0);
                material.SetShaderKeyword(_keyword, LastValue);
                return material;
            }

            public Material SetOn(Material material, bool value)
            {
                LastValue = value;
                SetOn(material);
                return material;
            }

            public MaterialToggle(string floatPropertyName, string keyword)
            {
                _floatProperty = floatPropertyName;
                _floatPropertyId = Shader.PropertyToID(floatPropertyName);
                _keyword = keyword;
            }

            void IPEGI.Inspect() => _floatProperty.PegiLabel().ToggleIcon(ref LastValue);
        }


        #endregion

        #region Array 

        [Serializable]
        public class VectorArrayValue : BaseShaderPropertyIndex
        {
            private Vector4[] _vectorArray;
            private readonly Fallback.Int _arraySize = new();

            public Vector4[] GlobalValue
            {
                get => Shader.GetGlobalVectorArray(id); 
                set 
                { 
                    if (_arraySize.IsSet) 
                    {
                        if (_arraySize.ManualValue != value.Length) 
                        {
                            QcLog.ChillLogger.LogErrorOnce(()=> "Trying to {0} of size {1} while previous length was {2}. Unsupported.".F(nameof(Shader.SetGlobalVectorArray), value.Length, _arraySize.ManualValue), key: "GlArSet");
                        }
                    } 
                    else 
                    {
                        _arraySize.ManualValue = value.Length;
                    }

                    _vectorArray = value;

                    Shader.SetGlobalVectorArray(id, value); 
                }
            }

            public VectorArrayValue(string name) : base(name) {}

            public override void SetLatestValueOn(Material mat) => mat.SetVectorArray(id, _vectorArray);

            public override void SetLatestValueOn(MaterialPropertyBlock block) => block.SetVectorArray(id, _vectorArray);
        }


        [Serializable]
        public class MatrixArrayValue : BaseShaderPropertyIndex
        {
            private Matrix4x4[] _vectorArray;
            private readonly Fallback.Int _arraySize = new();

            public Matrix4x4[] GlobalValue
            {
                get => Shader.GetGlobalMatrixArray(id);
                set
                {
                    if (_arraySize.IsSet)
                    {
                        if (_arraySize.ManualValue != value.Length)
                        {
                            QcLog.ChillLogger.LogErrorOnce(() => "Trying to {0} of size {1} while previous length was {2}. Unsupported.".F(nameof(Shader.SetGlobalVectorArray), value.Length, _arraySize.ManualValue), key: "GlArSet");
                        }
                    }
                    else
                    {
                        _arraySize.ManualValue = value.Length;
                    }

                    _vectorArray = value;

                    Shader.SetGlobalMatrixArray(id, value);
                }
            }

            public MatrixArrayValue(string name) : base(name) { }

            public override void SetLatestValueOn(Material mat) => mat.SetMatrixArray(id, _vectorArray);

            public override void SetLatestValueOn(MaterialPropertyBlock block) => block.SetMatrixArray(id, _vectorArray);
        }

        #endregion

        public class ShaderName
        {
            private readonly string _name;
            private Shader _shader;
            private bool _triedToFind;

            public bool Found() => _shader;
                
            public void Reload() 
            {
                _shader = Shader.Find(_name);
                _triedToFind = true;
                if (!_shader)
                    Debug.LogError("Failed to find {0}".F(_name));
            }

            public Shader Shader 
            {
                get 
                {
                    if (!_shader && !_triedToFind) 
                        Reload();
                       
                    return _shader;
                }
            }

            public ShaderName (string name) { _name = name; }
        }
    }

    #region Shader Tags
    public class ShaderTag 
    {
        public readonly string tag;
        public override string ToString() => tag;
        public bool Has(Material mat) => mat.HasTag(tag);
        public string Get(Material mat, bool searchFallBacks = false, string defaultValue = "") => mat.GetTag(tag, searchFallBacks, defaultValue);

        public string Get(Material mat, ShaderProperty.BaseShaderPropertyIndex property,
            bool searchFallBacks = false) =>
            Get(mat, property.ToString(), searchFallBacks);

        public string Get(Material mat, string prefix, bool searchFallBacks = false) =>
            mat.GetTag(prefix + tag, searchFallBacks);

        public ShaderTag(string nTag) { tag = nTag; }

        public List<Material> GetTaggedMaterialsFromAssets()
        {
            var mats = new List<Material>();

            var tmpMats = QcUnity.FindAssetsByType<Material>();

            foreach (var mat in tmpMats)
                if (Has(mat))
                    mats.AddIfNew(mat);

            return mats;
        }
    }

    public class ShaderTagValue 
    {
        private readonly ShaderTag tag;
        public override string ToString() => value;
        private readonly string value;

        public bool Has(Material mat, bool searchFallBacks = false) =>
            value.Equals(tag.Get(mat, searchFallBacks));

        public bool Has(Material mat, ShaderProperty.BaseShaderPropertyIndex property, bool searchFallBacks = false) =>
            mat && value.Equals(tag.Get(mat, property, searchFallBacks));

        public bool Equals(string tg) => value.Equals(tg);
        
        public ShaderTagValue(string newValue, ShaderTag nTag)
        {
            value = newValue;
            tag = nTag;
        }
    }

    public static class ShaderTags 
    {
        public static readonly ShaderTag ShaderTip = new("ShaderTip");
        public static readonly ShaderTag Queue = new("Queue");

        public static class Queues 
        {
            public static readonly ShaderTagValue Background = new("Background", Queue);
            public static readonly ShaderTagValue Geometry = new("Geometry", Queue);
            public static readonly ShaderTagValue AlphaTest = new("Geometry", Queue);
            public static readonly ShaderTagValue Transparent = new("Transparent", Queue);
            public static readonly ShaderTagValue Overlay = new("Overlay", Queue);
        }
    }

    public static class ShaderTagExtensions
    {
        public static string Get(this Material mat, ShaderTag tag, bool searchFallBacks = false, string defaultValue = "") =>
            tag.Get(mat, searchFallBacks, defaultValue);

        public static string Get(this Material mat, ShaderProperty.BaseShaderPropertyIndex propertyPrefix,
            ShaderTag tag, bool searchFallBacks = false) =>
            tag.Get(mat, propertyPrefix, searchFallBacks);

        public static string Get(this Material mat, string prefix, ShaderTag tag, bool searchFallBacks = false) =>
            tag.Get(mat, prefix, searchFallBacks);

        public static bool Has(this Material mat, ShaderTag tag) => tag.Has(mat);

        public static bool Has(this Material mat, ShaderTagValue val, bool searchFallBacks = false) =>
            val.Has(mat, searchFallBacks);

        public static bool Has(this Material mat, ShaderProperty.BaseShaderPropertyIndex propertyPrefix,
            ShaderTagValue val, bool searchFallBacks = false) =>
            val.Has(mat, propertyPrefix, searchFallBacks);
    }
    #endregion


    
#if UNITY_EDITOR

    [UnityEditor.CustomPropertyDrawer(typeof(ShaderProperty.TextureValue))]
    public class TextureValueDrawer : UnityEditor.PropertyDrawer
    {
        public override void OnGUI(Rect pos, UnityEditor.SerializedProperty prop, GUIContent label) 
        {
            if (prop.Inspect("latestValue", pos, label))
                prop.GetValue<ShaderProperty.TextureValue>().SetGlobal();
        }
    }

#endif
    
}
