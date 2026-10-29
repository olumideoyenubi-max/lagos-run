using UnityEngine;

// A small 2D painter (y grows downward, like an HTML canvas) that draws into a RenderTexture and hands back a Texture2D.
// Every texture in the game is painted with this at startup; there are no image files.
public class LRCanvas {
    public readonly int w, h;
    readonly RenderTexture rt, prev;
    static Material solid, glyphs, photo;
    bool open;

    public static Color H(int hex) { return new Color(((hex >> 16) & 255) / 255f, ((hex >> 8) & 255) / 255f, (hex & 255) / 255f, 1); }
    public static Color H(int hex, float a) { Color c = H(hex); c.a = a; return c; }
    public static Color Rgba(float r, float g, float b, float a) { return new Color(r / 255f, g / 255f, b / 255f, a); }

    public LRCanvas(int w, int h, Color clear) {
        this.w = w; this.h = h;
        if (solid == null) {
            Shader sh = Resources.Load<Shader>("LRCanvas");
            solid = new Material(sh); solid.mainTexture = Texture2D.whiteTexture;
            glyphs = new Material(sh);
            photo = new Material(sh); photo.SetFloat("_UseRGB", 1);
        }
        rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
        prev = RenderTexture.active;
        RenderTexture.active = rt;
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, w, h, 0);
        GL.Clear(true, true, clear);
    }

    void Solid() { if (!open) { solid.SetPass(0); GL.Begin(GL.TRIANGLES); open = true; } }
    void Flush() { if (open) { GL.End(); open = false; } }
    void V(float x, float y, Color c) { GL.Color(c); GL.Vertex3(x, y, 0); }
    void Tri(float x1, float y1, Color c1, float x2, float y2, Color c2, float x3, float y3, Color c3) { Solid(); V(x1, y1, c1); V(x2, y2, c2); V(x3, y3, c3); }

    public void Rect4(float x, float y, float rw, float rh, Color tl, Color tr, Color br, Color bl) {
        Tri(x, y, tl, x + rw, y, tr, x + rw, y + rh, br);
        Tri(x, y, tl, x + rw, y + rh, br, x, y + rh, bl);
    }
    public void Rect(float x, float y, float rw, float rh, Color c) { Rect4(x, y, rw, rh, c, c, c, c); }
    public void StrokeRect(float x, float y, float rw, float rh, float lw, Color c) {
        float k = lw / 2;
        Rect(x - k, y - k, rw + lw, lw, c); Rect(x - k, y + rh - k, rw + lw, lw, c);
        Rect(x - k, y + k, lw, rh - lw, c); Rect(x + rw - k, y + k, lw, rh - lw, c);
    }
    // vertical gradient through stops given as (position 0..1, colour) pairs
    public void GradV(float x, float y, float rw, float rh, float[] at, Color[] col) {
        for (int i = 0; i < at.Length - 1; i++)
            Rect4(x, y + rh * at[i], rw, rh * (at[i + 1] - at[i]), col[i], col[i], col[i + 1], col[i + 1]);
    }
    public void Ellipse(float cx, float cy, float rx, float ry, Color c, float rot = 0) {
        const int n = 40; float cr = Mathf.Cos(rot), sr = Mathf.Sin(rot);
        float px = 0, py = 0;
        for (int i = 0; i <= n; i++) {
            float a = i * Mathf.PI * 2 / n, ex = Mathf.Cos(a) * rx, ey = Mathf.Sin(a) * ry;
            float x = cx + ex * cr - ey * sr, y = cy + ex * sr + ey * cr;
            if (i > 0) Tri(cx, cy, c, px, py, c, x, y, c);
            px = x; py = y;
        }
    }
    public void Circle(float cx, float cy, float r, Color c) { Ellipse(cx, cy, r, r, c); }
    // radial gradient: solid col[0] inside r0, then through the stops out to r1 (rx/ry stretch it into an ellipse)
    public void Radial(float cx, float cy, float r0, float r1, float[] at, Color[] col, float sx = 1, float sy = 1) {
        const int n = 48;
        Ellipse(cx, cy, r0 * sx, r0 * sy, col[0]);
        for (int k = 0; k < at.Length - 1; k++) {
            float ra = Mathf.Lerp(r0, r1, at[k]), rb = Mathf.Lerp(r0, r1, at[k + 1]);
            for (int i = 0; i < n; i++) {
                float a = i * Mathf.PI * 2 / n, b = (i + 1) * Mathf.PI * 2 / n;
                float ca = Mathf.Cos(a) * sx, sa = Mathf.Sin(a) * sy, cb = Mathf.Cos(b) * sx, sb = Mathf.Sin(b) * sy;
                Tri(cx + ca * ra, cy + sa * ra, col[k], cx + ca * rb, cy + sa * rb, col[k + 1], cx + cb * rb, cy + sb * rb, col[k + 1]);
                Tri(cx + ca * ra, cy + sa * ra, col[k], cx + cb * rb, cy + sb * rb, col[k + 1], cx + cb * ra, cy + sb * ra, col[k]);
            }
        }
    }
    // convex polygon from x,y pairs
    public void Poly(Color c, params float[] xy) {
        for (int i = 2; i + 3 < xy.Length; i += 2)
            Tri(xy[0], xy[1], c, xy[i], xy[i + 1], c, xy[i + 2], xy[i + 3], c);
    }
    public void Line(float x1, float y1, float x2, float y2, float lw, Color c, bool squareCap = false) {
        float dx = x2 - x1, dy = y2 - y1, len = Mathf.Sqrt(dx * dx + dy * dy);
        if (len < 0.0001f) return;
        dx /= len; dy /= len;
        if (squareCap) { x1 -= dx * lw / 2; y1 -= dy * lw / 2; x2 += dx * lw / 2; y2 += dy * lw / 2; }
        float nx = -dy * lw / 2, ny = dx * lw / 2;
        Tri(x1 + nx, y1 + ny, c, x2 + nx, y2 + ny, c, x2 - nx, y2 - ny, c);
        Tri(x1 + nx, y1 + ny, c, x2 - nx, y2 - ny, c, x1 - nx, y1 - ny, c);
    }
    public void Ring(float cx, float cy, float r, float lw, Color c, float a0 = 0, float a1 = Mathf.PI * 2) {
        const int n = 40; float ri = r - lw / 2, ro = r + lw / 2;
        for (int i = 0; i < n; i++) {
            float a = Mathf.Lerp(a0, a1, i / (float)n), b = Mathf.Lerp(a0, a1, (i + 1) / (float)n);
            float ca = Mathf.Cos(a), sa = Mathf.Sin(a), cb = Mathf.Cos(b), sb = Mathf.Sin(b);
            Tri(cx + ca * ri, cy + sa * ri, c, cx + ca * ro, cy + sa * ro, c, cx + cb * ro, cy + sb * ro, c);
            Tri(cx + ca * ri, cy + sa * ri, c, cx + cb * ro, cy + sb * ro, c, cx + cb * ri, cy + sb * ri, c);
        }
    }

    // a photo texture, tinted and tiled across the rectangle
    public void Image(Texture tex, float x, float y, float rw, float rh, Color tint, float tileU = 1, float tileV = 1) {
        Flush();
        photo.mainTexture = tex; photo.SetPass(0);
        GL.Begin(GL.QUADS); GL.Color(tint);
        GL.TexCoord2(0, tileV); GL.Vertex3(x, y, 0);
        GL.TexCoord2(tileU, tileV); GL.Vertex3(x + rw, y, 0);
        GL.TexCoord2(tileU, 0); GL.Vertex3(x + rw, y + rh, 0);
        GL.TexCoord2(0, 0); GL.Vertex3(x, y + rh, 0);
        GL.End();
    }
    public static float Measure(string s, Font f, int size) {
        f.RequestCharactersInTexture(s, size);
        float wsum = 0; CharacterInfo ci;
        foreach (char ch in s) if (f.GetCharacterInfo(ch, out ci, size)) wsum += ci.advance;
        return wsum;
    }
    // text centred on (cx, cy), shrunk until it fits maxW
    public void Text(string s, float cx, float cy, float size, Font f, Color c, float maxW = 0) {
        int px = Mathf.RoundToInt(size);
        if (maxW > 0) while (px > 8 && Measure(s, f, px) > maxW) px -= 2;
        float tw = Measure(s, f, px);
        Flush();
        f.RequestCharactersInTexture(s, px);
        glyphs.mainTexture = f.material.mainTexture;
        glyphs.SetPass(0);
        GL.Begin(GL.QUADS);
        GL.Color(c);
        float pen = cx - tw / 2, baseY = cy + px * 0.36f;
        CharacterInfo ci;
        foreach (char ch in s) {
            if (!f.GetCharacterInfo(ch, out ci, px)) continue;
            float x0 = pen + ci.minX, x1 = pen + ci.maxX, yt = baseY - ci.maxY, yb = baseY - ci.minY;
            GL.TexCoord(ci.uvTopLeft); GL.Vertex3(x0, yt, 0);
            GL.TexCoord(ci.uvTopRight); GL.Vertex3(x1, yt, 0);
            GL.TexCoord(ci.uvBottomRight); GL.Vertex3(x1, yb, 0);
            GL.TexCoord(ci.uvBottomLeft); GL.Vertex3(x0, yb, 0);
            pen += ci.advance;
        }
        GL.End();
    }

    public Texture2D End(bool repeat = false, bool mips = true) {
        Flush();
        GL.PopMatrix();
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, mips);
        t.ReadPixels(new Rect(0, 0, w, h), 0, 0, mips);
        t.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        t.filterMode = FilterMode.Trilinear;
        t.anisoLevel = 4;
        t.Apply(mips, true);
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        return t;
    }
}
