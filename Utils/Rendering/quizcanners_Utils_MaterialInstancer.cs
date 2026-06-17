using QuizCanners.Inspect;
using System;
using System.Collections.Generic;
using UnityEngine;
using static QuizCanners.Utils.OnDemandRenderTexture;

namespace QuizCanners.Utils
{
    public static class MaterialInstancer
    {
        [Serializable]
        public abstract class Base 
        {
            public abstract Material GetInstance();

            public Texture this[ShaderProperty.TextureValue prop]
            {
                set
                {
                    prop.SetOn(GetInstance(), value);
                }
            }
        }

        [Serializable]
        public class ForUiGraphics : Base, IPEGI
        {
            [SerializeField] public List<UnityEngine.UI.Graphic> materialUsers = new();
            [NonSerialized] private Material _materialInstance;



            public override Material GetInstance() 
            {
                if (_materialInstance)
                    return _materialInstance;

                if (materialUsers.IsNullOrEmpty())
                {
                    QcLog.ChillLogger.LogErrorOnce("No Graphic Assigned", key: "NoGrph");
                    return null;
                }

                var first = materialUsers[0];

                if (!first)
                {
                    QcLog.ChillLogger.LogErrorOnce("First graphic is null", key: "NFGrNull");
                    return null;
                }

                if (!Application.isPlaying || QcUnity.IsPartOfAPrefab(first.gameObject))
                {
                    QcLog.ChillLogger.LogErrorOnce("Not playing or part of prefab", key: "NotPlOrPrf");
                    return first.material;
                }

                _materialInstance = UnityEngine.Object.Instantiate(first.material);

                foreach (var u in materialUsers)
                    if (u)
                        u.material = _materialInstance;

                return _materialInstance;
            }

            public ForUiGraphics(UnityEngine.UI.Graphic graphic)
            {
                materialUsers.Add(graphic);
            }

            void IPEGI.Inspect()
            {
                "Instance".PL().Edit(ref _materialInstance).NL();
            }
        }

        [Serializable]
        public class ForMeshRenderer : Base, IPEGI, IPEGI_ListInspect
        {

            [SerializeField] public bool InstantiateInEditor;
            [SerializeField] public List<Renderer> materialUsers = new();
            [NonSerialized] private Material materialInstance;

            public Material GetMaterialInstance(Renderer rendy)
            {
                if (materialInstance)
                    return materialInstance;

                materialUsers.Clear();
                materialUsers.Add(rendy);

                return GetInstance();
            }

            public ForMeshRenderer()
            {

            }

            public ForMeshRenderer(Renderer rendy)
            {
                materialUsers.Add(rendy);
            }

            void IPEGI.Inspect()
            {
                "Material User".PL().Edit_List(materialUsers).NL();

                if (materialUsers.Count > 0 && materialUsers[0])
                    "Is Part of the PRefab: {0} ".F(QcUnity.IsPartOfAPrefab(materialUsers[0].gameObject)).NL();

                "Instance".PL().Edit(ref materialInstance).NL();
            }

            public override Material GetInstance()
            {
                if (materialInstance)
                    return materialInstance;

                if (materialUsers.Count == 0)
                {
                    QcLog.ChillLogger.LogErrorOnce("No Renderer Assigned", key: "No Rnd");
                    return null;
                }

                var first = materialUsers[0];

                if (!first)
                    return null;

                if ((!InstantiateInEditor && !Application.isPlaying) || QcUnity.IsPartOfAPrefab(first.gameObject))
                    return first.sharedMaterial;

                if (!first.sharedMaterial)
                    return null;

                materialInstance = UnityEngine.Object.Instantiate(first.sharedMaterial);

                materialInstance.name = "Instanced material of {0}".F(first.name);

            //    Debug.Log("Instancing material for {0}".F(first.name));

                foreach (var u in materialUsers)
                    if (u)
                        u.sharedMaterial = materialInstance;

                return materialInstance;
            }

            public void InspectInList(ref int edited, int index)
            {
                if (Icon.Enter.Click())
                    edited = index;
            
                var first = materialUsers.TryGet(0);

                "Material User (x{0})".F(materialUsers.Count).ConstL().Edit(ref first).NL(()=> materialUsers.ForceSet(0,first));
            }
        }

        [Serializable]
        public class Unmanaged : Base, IPEGI, IPEGI_ListInspect
        {
            public Material sourceMaterial;
            [NonSerialized] private Material instance;

            public void Clear()
            {
                instance.DestroyWhateverUnityObject();
                instance = null;
            }

            public override Material GetInstance()
            {
                if (instance)
                    return instance;

                if (!sourceMaterial)
                    QcLog.ChillLogger.LogErrorOnce("No sourceMaterial in material instancer", key: "noSrcMat");
                else
                    instance = new Material(sourceMaterial);

                return instance;
            }

            void IPEGI.Inspect()
            {
                 "Source Material".ConstL().Edit(ref sourceMaterial).NL(Clear);

                if (instance)
                    "Instance".PL().Edit(ref instance).NL();

            }

            public void InspectInList(ref int edited, int index)
            {
                if (Icon.Enter.Click())
                    edited = index;

                "Source Material".ConstL().Edit(ref sourceMaterial).NL(Clear);
            }

            public Unmanaged()
            {

            }
        
            public Unmanaged(Material sourceMaterial)
            {
                this.sourceMaterial = sourceMaterial;
            }
        }

        [Serializable]
        public class ByShader : Base, IPEGI, IPEGI_ListInspect
        {
            [SerializeField] private Shader shaderToUse;
            [NonSerialized] private Material instance;

            public void Clear()
            {
                instance.DestroyWhateverUnityObject();
                instance = null;
            }

            public Material Get (Shader shader)
            {
                if (!instance)
                {
                    instance = new Material(shader);
                }
                else
                {
                    instance.shader = shader;
                }

                return instance;
            }

            public override Material GetInstance()
            {
                if (!shaderToUse)
                {
                    QcLog.ChillLogger.LogErrorOnce("Shader not set", key: "ShNst");
                    return null;
                }

                return Get(shaderToUse);
            }

            public ByShader() { }

            public ByShader(Shader shader)
            {
                this.shaderToUse = shader;
            }

            #region Inspector
            void IPEGI.Inspect()
            {
                "Shader to Use: ".PL().Edit(ref shaderToUse).NL(Clear);
                //"Material Instance".PegiLabel().Edit(ref instance).Nl();
            }

            public void InspectInList(ref int edited, int index)
            {
                if (Icon.Enter.Click())
                    edited = index;

                "Shader to Use: ".ConstL().Edit(ref shaderToUse).NL(Clear);
            }

            #endregion
        }
    }
}

