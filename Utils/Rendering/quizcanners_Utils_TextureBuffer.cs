using QuizCanners.Inspect;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Utils
{
    public static class OnDemandRenderTexture
    {
        public static void Set(this ShaderProperty.TextureValue tex, RenderTextureBufferBase buff) => tex.GlobalValue = buff.GetOrCreateTexture;

        public class ScreenSize : RenderTextureBufferBase, IPEGI
        {
            private readonly string _name;
            private readonly bool depth;
            private readonly bool _isColor;
            private RenderTexture _actualTexture;

            public bool IsInitialized
            {
                get => _actualTexture;
                set
                {
                    if (value)
                        _actualTexture = GetOrCreateTexture;
                    else if (_actualTexture)
                    {
                        _actualTexture.DestroyWhatever();
                    }
                }
            }

            public RenderTexture GetIfExists => _actualTexture;

            public override RenderTexture GetOrCreateTexture
            {
                get
                {
                    if (IsScreenSizeTextureInvalid(_actualTexture))
                    {
                        _actualTexture = new RenderTexture(width: Screen.width, height: Screen.height, depth ? 24 : 0, RenderTextureFormat.ARGB32, readWrite: _isColor ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear) { name = _name };
                    }
                    return _actualTexture;
                }
            }

            private bool IsScreenSizeTextureInvalid(Texture tex)
            {
                if (!tex)
                    return true;

                if (tex.width != Screen.width || tex.height != Screen.height)
                {
                    _actualTexture.DestroyWhatever();
                    return true;
                }

                return false;
            }

            void IPEGI.Inspect()
            {
                var init = IsInitialized;
                "{0}".F(_name).PegiLabel().ToggleIcon(ref init).Nl().OnChanged(() => IsInitialized = init);

                if (IsInitialized)
                {
                    pegi.Draw(_actualTexture, width: 256, alphaBlend: false).Nl();
                }
            }


            public void Clear()
            {
                if (!_actualTexture)
                    return;

                _actualTexture.DestroyWhatever();
                _actualTexture = null;
            }

            public ScreenSize(string name, bool useDepth, bool isColor)
            {
                depth = useDepth;
                _name = name;
                _isColor = isColor;
            }
        }


        public enum PrecisionType { Regular, Half, Float }

        public class DoubleBuffer : RenderTextureBufferBase, IPEGI, IPEGI_ListInspect
        {
            private readonly string _name;
            private readonly int _width;
            private readonly int _height;
            private PrecisionType _precision;

            private readonly bool _clearOnCreate;
            private readonly bool _isColor;
            private bool _latestIsZero;

            private static readonly ShaderProperty.TextureValue _PREVIOUS_TEXTURE = new("_PreviousTex");

            public bool IsInitialized => _renderTextures != null && _renderTextures.Count > 0;

            public void Swap() => _latestIsZero = !_latestIsZero;

            public ScreenSize OriginalScreenSizeSource { get; private set; }
            public ShaderProperty.TextureValue ValueToUpdate { get; private set; }
            private int _OriginalTexturesVersion;

            private bool IsScreenSizeTargetSet => OriginalScreenSizeSource != null && OriginalScreenSizeSource.Version == _OriginalTexturesVersion;

            private void TryReturnPreviousSetNew(ScreenSize newTargetTexture)
            {
                if (IsScreenSizeTargetSet && newTargetTexture != OriginalScreenSizeSource)
                {
                    BlitInternal(this, OriginalScreenSizeSource);
                    ValueToUpdate.GlobalValue = OriginalScreenSizeSource.GetOrCreateTexture;
                    OriginalScreenSizeSource = null;
                    ValueToUpdate = null;
                }

                OriginalScreenSizeSource = newTargetTexture;
                _OriginalTexturesVersion = newTargetTexture.Version;
            }

            protected List<RenderTexture> _renderTextures;

            private List<RenderTexture> RenderTextures
            {
                get
                {
                    if (!_renderTextures.IsNullOrEmpty())
                        return _renderTextures;

                    _renderTextures = new List<RenderTexture>();
                    for (int i = 0; i < 2; i++)
                    {
                        RenderTexture tex = _precision switch
                        {
                            PrecisionType.Float => new RenderTexture(width: _width, height: _height, depth: 0, RenderTextureFormat.ARGBFloat, readWrite: _isColor ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear),
                            PrecisionType.Half => new RenderTexture(width: _width, height: _height, depth: 0, RenderTextureFormat.ARGBHalf, readWrite: _isColor ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear),
                            _ => new RenderTexture(width: _width, height: _height, depth: 0, RenderTextureFormat.ARGB32, readWrite: _isColor ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear),
                        };

                        tex.name = "{0} {1}".F(_name, i);

                        if (_clearOnCreate)
                            tex.Clear(Color.clear);

                        _renderTextures.Add(tex);
                    }
                    

                    return _renderTextures;
                }
            }

            public void Clear() 
            {
                if (_renderTextures.IsNullOrEmpty())
                    return;

                foreach (var t in _renderTextures) 
                {
                    if (t)
                        t.DestroyWhatever();
                }
                _renderTextures = null;
            }

            public RenderTexture Target
            {
                get => _latestIsZero ? RenderTextures[0] : RenderTextures[1];
                private set
                {
                    if (_latestIsZero)
                        RenderTextures[0] = value;
                    else
                        RenderTextures[1] = value;
                }
            }
            public RenderTexture Previous
            {
                get => _latestIsZero ? RenderTextures[1] : RenderTextures[0];
                private set
                {
                    if (_latestIsZero)
                        RenderTextures[1] = value;
                    else
                        RenderTextures[0] = value;
                }
            }


            public override RenderTexture GetOrCreateTexture => Target;

         
            public void Blit(Material material, bool andRelease)
            {
                Swap();
                material.Set(_PREVIOUS_TEXTURE, Previous);

                Graphics.Blit(Previous, Target, material);

                if (andRelease)
                    Previous.Release();

                if (IsScreenSizeTargetSet)
                {
                    ValueToUpdate.GlobalValue = Target;
                }
            }

            public void Blit(Shader shader, bool andRelease)
            {
                Swap();
                var mat = RenderTextureBlit.MaterialReuse(shader);
                mat.Set(_PREVIOUS_TEXTURE, Previous);

                Graphics.Blit(Previous, Target, mat);

                if (andRelease)
                    Previous.Release();

                if (IsScreenSizeTargetSet)
                {
                    ValueToUpdate.GlobalValue = Target;
                }
            }

            public RenderTexture RenderFromAndSwapIn(RenderTexture previous, Shader shader, bool andRelease)
            {
                var result = Previous;
                Previous = previous;

                var mat = RenderTextureBlit.MaterialReuse(shader);
                mat.Set(_PREVIOUS_TEXTURE, previous);

                Graphics.Blit(previous, result, mat);

                if (andRelease)
                    previous.Release();

               return result;
            }

            public void BlitTargetWithPreviousAndSwap(ref RenderTexture previousResult, Shader shader, bool andRelease)
            {
                var mat = RenderTextureBlit.MaterialReuse(shader);
                mat.Set(_PREVIOUS_TEXTURE, previousResult);

                Graphics.Blit(Target, Previous, mat);

                if (andRelease)
                    Target.Release();

                (previousResult, Previous) = (Previous, previousResult);
            }

            public void SetSourceAndTarget(ScreenSize source, ScreenSize target, ShaderProperty.TextureValue shaderTexture, Shader copyDownscale)
            {
                TryReturnPreviousSetNew(target);

                BlitInternal(source, this, copyDownscale);
                ValueToUpdate = shaderTexture;
                Set(shaderTexture, source);
            }

            public void SetSourceAndTarget(ScreenSize sourceAndTarget, ShaderProperty.TextureValue shaderTexture, Shader copyDownscale)
            {
                TryReturnPreviousSetNew(sourceAndTarget);

                BlitInternal(sourceAndTarget, this, copyDownscale);

                ValueToUpdate = shaderTexture;

                Set(shaderTexture, sourceAndTarget);
            }

            public void BlitTo(RenderTexture newTarget, Shader shader, bool andRelease)
            {
                Graphics.Blit(Target, newTarget, RenderTextureBlit.MaterialReuse(shader));

                if (andRelease)
                    Target.Release();

            }

            #region Inspector
            void IPEGI.Inspect()
            {
                if (!_renderTextures.IsNullOrEmpty())
                {
                    _name.PegiLabel().Edit_List_UObj(_renderTextures).Nl();
                    if ("Precision".PegiLabel().Edit_Enum(ref _precision).Nl())
                    {
                        Clear();
                    }
                    Icon.Clear.Click(Clear);
                    pegi.Click(Swap).Nl();
                    pegi.Draw(Target, width: 256).Nl();
                }
                else
                    "{0} not initialized".F(_name).PegiLabel().Nl();

            
            }
            public override string ToString() => _name;

            public void InspectInList(ref int edited, int index)
            {
                _name.PegiLabel().ClickEnter(ref edited, index);

                 (IsInitialized ? Icon.Active : Icon.InActive).Draw();
            }
            #endregion

            public DoubleBuffer(string name, int size, PrecisionType precision, bool clearOnCreate, bool isColor)
            {
                _clearOnCreate = clearOnCreate;
                _precision = precision;
                _name = name;

                CheckPowerOfTwo(ref size);

                _width = size;
                _height = size;
                _isColor = isColor;
            }

            public DoubleBuffer(string name, int width, int height, PrecisionType precision, bool clearOnCreate, bool isColor)
            {
                _clearOnCreate = clearOnCreate;
                _precision = precision;
                _name = name;

                _width = width;
                _height = height;
                _isColor = isColor;
            }
        }

        public class Single : IPEGI
        {
            private readonly string _name;
            private readonly int _width;
            private readonly int _height;
            private readonly bool _floatBuffer;
            private readonly bool _singleChannel;
            private readonly bool _isColor;
            private readonly DepthPrecision _depth;


            protected RenderTexture _renderTexture;

            public bool IsInitialized() => _renderTexture;

            public void Blit(Shader shader)
            {
                var mat = RenderTextureBlit.MaterialReuse(shader);
                Graphics.Blit(null, GetRenderTexture(), mat);
            }

            public void Blit(Texture tex, Material material) => Graphics.Blit(tex, GetRenderTexture(), material);

            public void Blit(Material material) => Graphics.Blit(null, GetRenderTexture(), material);

            public void Blit(Texture textureToCopyFrom) => Graphics.Blit(textureToCopyFrom, GetRenderTexture());


            public RenderTexture GetRenderTexture()
            {
                if (_renderTexture)
                    return _renderTexture;

                _renderTexture = new RenderTexture(width: _width, height: _height, depth: (int)_depth,
                    _singleChannel ? (_floatBuffer ? RenderTextureFormat.RFloat : RenderTextureFormat.RHalf) :
                    (_floatBuffer ? RenderTextureFormat.ARGBFloat : RenderTextureFormat.ARGBHalf),
                    _isColor ? RenderTextureReadWrite.sRGB: RenderTextureReadWrite.Linear
                    )
                {
                    name = "{0} {1}".F(_name, _height == _width ? _width : "{0}x{1}".F(_width, _height))
                };

                return _renderTexture;
            }

            public Single(string name, int size, bool isFloat, bool isColor , bool singleChannel = false, DepthPrecision depth = DepthPrecision.None)
            {
                _depth = depth;
                _floatBuffer = isFloat;
                _name = name;
                _singleChannel = singleChannel;
                _isColor = isColor;
                CheckPowerOfTwo(ref size);

                _width = size;
                _height = size;
            }

            public Single(string name, int width, int height, bool isFloat, bool isColor, bool singleChannel = false, DepthPrecision depth = DepthPrecision.None)
            {
                _depth = depth;
                _floatBuffer = isFloat;
                _name = name;
                _singleChannel = singleChannel;
                _isColor = isColor;

                _width = width;
                _height = height;
            }

            public void Clear() 
            {
                if (!_renderTexture)
                    return;
                _renderTexture.DestroyWhatever();
                _renderTexture = null;
            }

            #region Inspector
            public override string ToString() => "{0} ({1}): {2}".F(_name, _floatBuffer ? "Floaf" : "Half", _renderTexture ? "Initialized" : "Uninitialized");

            void IPEGI.Inspect()
            {
                if (_renderTexture)
                {
                    pegi.Edit(ref _renderTexture).Nl();
                    pegi.Draw(_renderTexture, 256, alphaBlend: false);
                }
                else
                    "{0} not initialized".F(_name).PegiLabel().Nl();
            }
            #endregion
        }

        private static void CheckPowerOfTwo(ref int size)
        {
            if (!Mathf.IsPowerOfTwo(size))
            {
                var previous = size;
                size = Mathf.ClosestPowerOfTwo(size);
                Debug.LogWarning("Changing size ({0}) to Power of Two: {1}".F(previous, size));
            }
        }

        public abstract class RenderTextureBufferBase
        {
            public int Version;
            public abstract RenderTexture GetOrCreateTexture { get; }

            protected static void BlitInternal(RenderTextureBufferBase from, RenderTextureBufferBase to, Shader shader = null)
            {
                if (shader)
                {
                    Graphics.Blit(from.GetOrCreateTexture, to.GetOrCreateTexture, RenderTextureBlit.MaterialReuse(shader));
                }
                else
                    Graphics.Blit(from.GetOrCreateTexture, to.GetOrCreateTexture);
            }
        }

        public enum DepthPrecision { None = 0, D_16 = 16, D_24 = 24, D_32 = 32 }
    }

    public static class RenderTextureBlit
    {
        public static Material MaterialReuse(Shader shader) => MaterialInstancer.Get(shader);

        private static readonly MaterialInstancer.ByShader MaterialInstancer = new();

        public static void BlitGL(Texture source, RenderTexture destination, Material mat)
        {
            RenderTexture.active = destination;
            mat.mainTexture = source;//("_MainTex", source);
            GL.PushMatrix();
            GL.LoadOrtho();
            GL.invertCulling = true;
            mat.SetPass(0);
            GL.Begin(GL.QUADS);
            GL.MultiTexCoord2(0, 0.0f, 0.0f);
            GL.Vertex3(0.0f, 0.0f, 0.0f);
            GL.MultiTexCoord2(0, 1.0f, 0.0f);
            GL.Vertex3(1.0f, 0.0f, 0.0f);
            GL.MultiTexCoord2(0, 1.0f, 1.0f);
            GL.Vertex3(1.0f, 1.0f, 1.0f);
            GL.MultiTexCoord2(0, 0.0f, 1.0f);
            GL.Vertex3(0.0f, 1.0f, 0.0f);
            GL.End();
            GL.invertCulling = false;
            GL.PopMatrix();
        }

    }
}
