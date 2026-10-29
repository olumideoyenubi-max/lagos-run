using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

// Browser only: keeps the game near 60 fps by lowering the canvas resolution when frames run long,
// and raising it again when there is room. High-density (Retina) screens otherwise draw 3x the pixels.
public class LRPerf : MonoBehaviour {
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern float LRGetDPR();
    [DllImport("__Internal")] static extern void LRSetDPR(float r);
#else
    static float LRGetDPR() { return 1; }
    static void LRSetDPR(float r) { }
#endif
    const float Floor = 0.6f, Low = 54f, High = 58.5f;
    float top, dpr, settle = 2.5f, frames, time, goodFor, calm;
    float ceiling;   // a level that proved too slow is not tried again

    public static void Attach(GameObject go) {
        if (Application.platform == RuntimePlatform.WebGLPlayer) go.AddComponent<LRPerf>();
    }

    void Start() {
        dpr = LRGetDPR();
        top = dpr;   // the page's own setting is the most it ever draws
        ceiling = top;
        if (top > 1) Set(1);   // start at standard resolution and sharpen only once frames prove there is room
        settle = 2.5f;
    }

    void Set(float r) {
        dpr = Mathf.Clamp(Mathf.Round(r * 20) / 20, Floor, top);
        LRSetDPR(dpr); settle = 1.2f; frames = time = 0;
        Debug.Log("LRPerf resolution " + dpr.ToString("0.00"));
    }

    void Update() {
        float dt = Time.unscaledDeltaTime;
        if (settle > 0) { settle -= dt; return; }
        if (dt > 0.25f) return;   // a hitch or a hidden tab, not the steady rate
        frames++; time += dt;
        if (time < 1f) return;
        float fps = frames / time; frames = time = 0;
        if (fps < Low && dpr > Floor) {
            ceiling = Mathf.Min(ceiling, dpr - 0.05f);
            Set(dpr * Mathf.Clamp(fps / 60f, 0.7f, 0.9f));
            goodFor = calm = 0;
        } else if (fps >= High && (calm += 1) >= 30 && ceiling < top) {
            ceiling = top; calm = 0;   // a long smooth stretch: a slow moment earlier may have been a one-off
        } else if (fps >= High && dpr < ceiling) {
            goodFor += 1;
            if (goodFor >= 5) { Set(Mathf.Min(ceiling, dpr + 0.15f)); goodFor = 0; }
        } else goodFor = 0;
    }
}
