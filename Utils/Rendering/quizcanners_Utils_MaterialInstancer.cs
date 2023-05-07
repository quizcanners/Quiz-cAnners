using QuizCanners.Inspect;
using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

namespace QuizCanners.Utils
{
    public static class MaterialInstancer
    {
        [Serializable]
        public class ForUiGraphics : IPEGI
        {
            [SerializeField] public List<UnityEngine.UI.Graphic> materialUsers = new List<UnityEngine.UI.Graphic>();
            [NonSerialized] private Material _materialInstance;

            public Material MaterialInstance
            {
                get
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
            }

            public ForUiGraphics(UnityEngine.UI.Graphic graphic)
            {
                materialUsers.Add(graphic);
            }

            public void Inspect()
            {
                "Instance".PegiLabel().Edit(ref _materialInstance).Nl();
            }
        }

        [Serializable]
        public class ForMeshRenderer : IPEGI
        {

            [SerializeField] public bool InstantiateInEditor;
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

            public ForMeshRenderer()
            {

            }

            public ForMeshRenderer(MeshRenderer rendy)
            {
                materialUsers.Add(rendy);
            }

            public void Inspect()
            {
                "Material User".PegiLabel().Edit_List(materialUsers).Nl();

                if (materialUsers.Count > 0 && materialUsers[0])
                    "Is Part of the PRefab: {0} ".F(QcUnity.IsPartOfAPrefab(materialUsers[0].gameObject)).PegiLabel().Nl();

                "Instance".PegiLabel().Edit(ref materialInstance).Nl();
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


        public class ByShader : IPEGI
        {
            [NonSerialized] private Material instance;

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

            public void Inspect()
            {
                "Material Instance".PegiLabel().Edit(ref instance).Nl();
            }
        }
    }
}

