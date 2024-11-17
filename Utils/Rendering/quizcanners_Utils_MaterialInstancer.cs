using QuizCanners.Inspect;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Utils
{
    public static class MaterialInstancer
    {

        public abstract class Base 
        {
            public abstract Material GetInstance();
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
                    return null;

                var first = materialUsers[0];

                if (!first)
                    return null;

                if (!Application.isPlaying || QcUnity.IsPartOfAPrefab(first.gameObject))
                    return first.material;

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
                "Instance".PegiLabel().Edit(ref _materialInstance).Nl();
            }
        }

        [Serializable]
        public class ForMeshRenderer : Base, IPEGI
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
                "Material User".PegiLabel().Edit_List(materialUsers).Nl();

                if (materialUsers.Count > 0 && materialUsers[0])
                    "Is Part of the PRefab: {0} ".F(QcUnity.IsPartOfAPrefab(materialUsers[0].gameObject)).PegiLabel().Nl();

                "Instance".PegiLabel().Edit(ref materialInstance).Nl();
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

                foreach (var u in materialUsers)
                    if (u)
                        u.sharedMaterial = materialInstance;

                return materialInstance;
            }
        }

        [Serializable]
        public class Unmanaged : Base, IPEGI
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
                 "Source Material".ConstLabel().Edit(ref sourceMaterial).Nl(Clear);

                if (instance)
                    "Instance".PegiLabel().Edit(ref instance).Nl();

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
        public class ByShader : Base, IPEGI
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

            #region Inspector
            void IPEGI.Inspect()
            {
                "Shader to Use: ".PegiLabel().Edit(ref shaderToUse).Nl(Clear);
                //"Material Instance".PegiLabel().Edit(ref instance).Nl();
            }

            #endregion
        }
    }
}

