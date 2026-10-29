using UnityEngine;

// Camera finishing pass (see LRPost.shader). Added to the camera on desktop only.
public class LRPost : MonoBehaviour {
    Material mat;
    void OnRenderImage(RenderTexture src, RenderTexture dst) {
        if (mat == null) mat = new Material(Resources.Load<Shader>("LRPost"));
        mat.SetVector("_Params", new Vector4(0.88f, 0.3f, 0.24f, 1.04f));   // bloom threshold, bloom strength, vignette, saturation
        int w = src.width / 4, h = src.height / 4;
        RenderTexture a = RenderTexture.GetTemporary(w, h, 0, src.format), b = RenderTexture.GetTemporary(w, h, 0, src.format);
        Graphics.Blit(src, a, mat, 0);
        for (int i = 0; i < 2; i++) {
            mat.SetVector("_Dir", new Vector4(1 + i, 0, 0, 0)); Graphics.Blit(a, b, mat, 1);
            mat.SetVector("_Dir", new Vector4(0, 1 + i, 0, 0)); Graphics.Blit(b, a, mat, 1);
        }
        mat.SetTexture("_Bloom", a);
        Graphics.Blit(src, dst, mat, 2);
        RenderTexture.ReleaseTemporary(a); RenderTexture.ReleaseTemporary(b);
    }
}
