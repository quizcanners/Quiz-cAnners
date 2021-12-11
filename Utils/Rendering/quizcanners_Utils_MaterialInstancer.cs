using QuizCanners.Inspect;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Utils
{
    public class MaterialInstancer
    {
        [Serializable]
        public class ForUiGraphics
        {
            [SerializeField] public List<UnityEngine.UI.Graphic> materialUsers = new List<UnityEngine.UI.Graphic>();
            [NonSerialized] private Material labelMaterialInstance;

            public Material MaterialInstance
            {
                get
                {
                    if (labelMaterialInstance)
                        return labelMaterialInstance;

                    if (materialUsers.Count == 0)
                        return null;

                    var first = materialUsers[0];

                    if (!first)
                        return null;

                    if (!Application.isPlaying)
                        return first.material;

                    labelMaterialInstance = UnityEngine.Object.Instantiate(first.material);

                    foreach (var u in materialUsers)
                        if (u)
                            u.material = labelMaterialInstance;

                    return labelMaterialInstance;
                }
            }

            public ForUiGraphics(UnityEngine.UI.Graphic graphic)
            {
                materialUsers.Add(graphic);
            }
        }

        [Serializable]
        public class ForMeshRenderer
        {

            [SerializeField] public bool instantiateInEditor;
            [SerializeField] public List<MeshRenderer> materialUsers = new List<MeshRenderer>();
            [NonSerialized] private Material materialInstance;

            public Material GetMaterialInstance(MeshRenderer rendy)
            {
                if (materialInstance)
                    return materialInstance;

                materialUsers.Clear();
                materialUsers.Add(rendy);

                return MaterialInstance;
            }

            public Material MaterialInstance
            {
                get
                {
                    if (materialInstance)
                        return materialInstance;

                    if (materialUsers.Count == 0)
                        return null;

                    var first = materialUsers[0];

                    if (!first)
                        return null;

                    if (!Application.isPlaying && !instantiateInEditor)
                        return first.sharedMaterial;

                    materialInstance = UnityEngine.Object.Instantiate(first.sharedMaterial);

                    materialInstance.name = "Instanced material of {0}".F(first.name);

                    foreach (var u in materialUsers)
                        if (u)
                            u.sharedMaterial = materialInstance;

                    return materialInstance;
                }
            }

            public ForMeshRenderer()
            {

            }

            public ForMeshRenderer(bool instantiateInEditor)
            {
                this.instantiateInEditor = instantiateInEditor;
            }
        }

        [Serializable]
        public class Unmanaged : IPEGI
        {
            [SerializeField] protected Material sourceMaterial;
            [NonSerialized] private Material instance;

            public Material Instance
            {
                get
                {
                    if (!instance)
                    {
                        if (!sourceMaterial)
                            QcLog.ChillLogger.LogErrorOnce("No sourceMaterial in material instancer", key: "noSrcMat");
                        else
                            instance = new Material(sourceMaterial);
                    }
                    return instance;
                }
            }

            public void Inspect()
            {
                if (!sourceMaterial)
                    "Source Material".PegiLabel(90).Edit(ref sourceMaterial).Nl();
            }
        }

        [Serializable]
        public class ByShader
        {
            [NonSerialized] private Material instance;

            public Material Get (Shader shader)
            {
                if (!instance)
                {
                    instance = new Material(shader);
                }
                else
                    instance.shader = shader;

                return instance;
            }
        }
    }
}

