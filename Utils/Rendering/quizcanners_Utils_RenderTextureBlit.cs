using UnityEngine;

namespace QuizCanners.Utils
{

    public static class RenderTextureBlit 
    {
        public static Material MaterialReuse(Shader shader) => MaterialInstancer.Get(shader);

        private static MaterialInstancer.ByShader MaterialInstancer = new MaterialInstancer.ByShader();

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