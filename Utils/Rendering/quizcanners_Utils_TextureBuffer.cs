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
                        _actualTexture = new RenderTexture(width: Screen.width, height: Screen.height, depth ? 24 : 0) { name = _name };
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

            public void Inspect()
            {
                var init = IsInitialized;
                "{0}".F(_name).PegiLabel().ToggleIcon(ref init).Nl().OnChanged(() => IsInitialized = init);

                if (IsInitialized)
                {
                    pegi.Draw(_actualTexture, width: 256, alphaBlend: false).Nl();
                }
            }

            public ScreenSize(string name, bool useDepth)
            {
                depth = useDepth;
                _name = name;
            }
        }


        public class DoubleBuffer : RenderTextureBufferBase, IPEGI
        {
            private readonly string _name;
            private readonly int _size;
            private readonly bool _floatBuffer;

            private bool _latestIsZero;
           
            public void Swap() => _latestIsZero = !_latestIsZero;

            private bool IsTargetSet => OriginalSource != null && OriginalSource.Version == _OriginalTexturesVersion;

            private void TryReturnPreviousSetNew(ScreenSize newTargetTexture)
            {
                if (IsTargetSet && newTargetTexture != OriginalSource)
                {
                    BlitInternal(this, OriginalSource);
                    ValueToUpdate.GlobalValue = OriginalSource.GetOrCreateTexture;
                    OriginalSource = null;
                    ValueToUpdate = null;
                }

                OriginalSource = newTargetTexture;
                _OriginalTexturesVersion = newTargetTexture.Version;
            }

            protected List<RenderTexture> _renderTextures;

            private List<RenderTexture> RenderTextures
            {
                get
                {
                    if (_renderTextures.IsNullOrEmpty())
                    {
                        _renderTextures = new List<RenderTexture>();
                        for (int i = 0; i < 2; i++)
                        {
                            var tex = _floatBuffer ?
                                new RenderTexture(_size, _size, depth: 0, RenderTextureFormat.ARGBHalf) :
                                new RenderTexture(_size, _size, depth: 0, format: UnityEngine.Experimental.Rendering.DefaultFormat.LDR);

                            tex.name = "{0} {1}".F(_name, i);

                            _renderTextures.Add(tex);
                        }
                    }

                    return _renderTextures;
                }
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

            public ScreenSize OriginalSource { get; private set; }
            public ShaderProperty.TextureValue ValueToUpdate { get; private set; }
            private int _OriginalTexturesVersion;

            public override RenderTexture GetOrCreateTexture => Target;

            private static readonly ShaderProperty.TextureValue _PREVIOUS_TEXTURE = new("_PreviousTex");

            public void Blit(Shader shader)
            {
                Swap();
                var mat = RenderTextureBlit.MaterialReuse(shader);
                mat.Set(_PREVIOUS_TEXTURE, Previous);

                Graphics.Blit(Previous, Target, mat);

                if (IsTargetSet)
                {
                    ValueToUpdate.GlobalValue = Target;
                }
            }

            public RenderTexture RenderFromAndSwapIn(RenderTexture previous, Shader shader)
            {
                var result = Previous;
                Previous = previous;

                var mat = RenderTextureBlit.MaterialReuse(shader);
                mat.Set(_PREVIOUS_TEXTURE, previous);

                Graphics.Blit(previous, result, mat);
               
               return result;
            }

            public void BlitTargetWithPreviousAndSwap(ref RenderTexture previousResult, Shader shader)
            {
                var mat = RenderTextureBlit.MaterialReuse(shader);
                mat.Set(_PREVIOUS_TEXTURE, previousResult);

                Graphics.Blit(Target, Previous, mat);
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

       

            public void BlitTo(RenderTexture newTarget, Shader shader)
            {
                Graphics.Blit(Target, newTarget, RenderTextureBlit.MaterialReuse(shader));
            }

            #region Inspector
            public void Inspect()
            {
                if (!_renderTextures.IsNullOrEmpty())
                    "Render Textures".PegiLabel().Edit_List_UObj(_renderTextures).Nl();
                else
                    "Buffers not initialized".PegiLabel().Nl();

                pegi.Click(Swap).Nl();
            }
            public override string ToString() => _name;
            #endregion

            public DoubleBuffer(string name, int size, bool isFloat)
            {
                _floatBuffer = isFloat;
                _name = name;
                if (!Mathf.IsPowerOfTwo(size))
                {
                    Debug.LogError("Creating a Texture that is not a power of two: " + size);
                    size = Mathf.ClosestPowerOfTwo(size);
                }
                _size = size;
            }

            public DoubleBuffer(string name, bool isFloat)
            {
                _floatBuffer = isFloat;
                _name = name;
                _size = 512;
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

    }

}
