using System.Collections.Generic;
using UnityEngine;

// Textures and materials for Japa from the Shrine. Surfaces are photo textures (Poly Haven) tinted to the colours of
// the original web version; signs, cloth patterns and icons are painted at startup with LRCanvas.
public static class T {
    public static Font Display, Body, BodySemi;
    static Shader lit;
    static readonly Dictionary<int, Material> lamCache = new Dictionary<int, Material>();
    public static readonly Dictionary<string, Texture> ankara = new Dictionary<string, Texture>();
    public static readonly Dictionary<string, Texture2D> icon = new Dictionary<string, Texture2D>();

    public static Color H(int hex) { return LRCanvas.H(hex); }
    static Color Rgba(float r, float g, float b, float a) { return LRCanvas.Rgba(r, g, b, a); }
    static float Rand(float a, float b) { return Random.Range(a, b); }

    public static Material New(Color c, Texture tex, bool isLit) {
        if (lit == null) lit = Resources.Load<Shader>("LRLit");
        Material m = new Material(lit);
        m.SetColor("_Color", c);
        if (tex != null) m.SetTexture("_MainTex", tex);
        m.SetFloat("_Lit", isLit ? 1 : 0);
        return m;
    }
    public static Material Lam(int color) {
        Material m;
        if (!lamCache.TryGetValue(color, out m)) m = lamCache[color] = New(H(color), null, true);
        return m;
    }
    public static Material Lam(Texture tex) { return New(Color.white, tex, true); }
    public static Material Basic(Texture tex) { return New(Color.white, tex, false); }
    public static Material Basic(int color) { return New(H(color), null, false); }
    public static Material Emissive(Material m, int emissive) { m.SetColor("_Emission", H(emissive)); return m; }
    public static Material Transparent(Material m, bool fog = true) {
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0);
        m.SetFloat("_Fog", fog ? 1 : 0);
        m.renderQueue = 3000;
        return m;
    }
    public static Material DoubleSided(Material m) { m.SetFloat("_Cull", 0); return m; }
    public static Texture2D Surf(string name) { return Resources.Load<Texture2D>("Surfaces/" + name); }
    public static Material Bump(Material m, Texture normal, float scale = 1, float gloss = 0) {
        m.SetTexture("_BumpMap", normal); m.SetFloat("_BumpScale", scale); m.SetFloat("_Gloss", gloss); m.EnableKeyword("LR_BUMP");
        return m;
    }
    // a photo surface tinted so that it averages out to the colour asked for
    public static Material Photo(string surface, int avg, int colour, float bump = 1, float gloss = 0) {
        Color a = H(avg), c = H(colour);
        Material m = New(new Color(Mathf.Min(2, c.r / a.r), Mathf.Min(2, c.g / a.g), Mathf.Min(2, c.b / a.b), 1), Surf(surface + "_C"), true);
        return Bump(m, Surf(surface + "_N"), bump, gloss);
    }

    // per zone: path top, path sides, supports and kerbs
    public static readonly Dictionary<string, Material> floor = new Dictionary<string, Material>(), side = new Dictionary<string, Material>(),
        pillar = new Dictionary<string, Material>(), kerb = new Dictionary<string, Material>();
    public static Material bark, barkLight, palmLeaf, red, white, chalk, rope, plankDark, laterite, lateriteDark, thatch, mud, stone, stoneDark,
        clay, clayDark, metal, fire, fireCore, cowrie, cowrieSlit, calabash, gmg, net, ground, sky, shadow, gateBoard, lampGlow, bumpYellow, bumpBlack, coneOrange, dark, leaf;
    public static Material[] boards, arrowSign, roadSign, canopy, cloth;
    public static Texture2D shadowTex;

    static Texture2D Pattern(int bg, int a, int b) {
        LRCanvas g = new LRCanvas(128, 128, H(bg));
        for (int y = 0; y < 128; y += 32) for (int x = 0; x < 128; x += 32) {
            int ox = (y / 32) % 2 == 1 ? 16 : 0;
            g.Circle(x + ox + 8, y + 16, 11, H(a));
            g.Circle(x + ox + 8, y + 16, 5, H(b));
        }
        return g.End(true);
    }
    // aso-oke style woven stripes for the masquerades' cloth
    static Texture2D Stripes(int bg, int a, int b) {
        LRCanvas g = new LRCanvas(64, 128, H(bg));
        for (int x = 0; x < 64; x += 16) { g.Rect(x, 0, 5, 128, H(a)); g.Rect(x + 8, 0, 2, 128, H(b)); }
        for (int y = 0; y < 128; y += 4) g.Rect(0, y, 64, 1, Rgba(0, 0, 0, 0.12f));
        return g.End(true);
    }
    static Material Sign(int bg, int fg, string a, string b) {
        LRCanvas g = new LRCanvas(512, 256, H(bg));
        g.StrokeRect(10, 10, 492, 236, 10, H(fg));
        g.Text(a, 256, b == null ? 128 : 102, 84, Display, H(fg), 452);
        if (b != null) g.Text(b.Replace("₦", "N"), 256, 192, 38, Body, H(fg), 442);
        return Basic(g.End());
    }
    static Texture2D Icon(int bg, System.Action<LRCanvas> draw) {
        LRCanvas g = new LRCanvas(128, 128, new Color(1, 1, 1, 0));
        g.Circle(64, 64, 62, Rgba(255, 255, 255, 0.35f));
        g.Circle(64, 64, 52, H(bg));
        g.Ring(64, 64, 52, 6, H(0x16120c));
        draw(g);
        return g.End();
    }

    public static void Build() {
        Display = Resources.Load<Font>("TitanOne-Regular");
        Body = Resources.Load<Font>("Archivo-ExtraBold");
        BodySemi = Resources.Load<Font>("Archivo-SemiBold");
        LRCanvas g;

        // the causeway in each area
        floor["grove"] = Photo("Mossy_Cobble", 0x766c4c, 0x858c78, 1, 0.1f);
        floor["ruins"] = Photo("Laterite_Stones", 0x643f2e, 0xb2643c, 1);
        floor["river"] = Photo("Planks_Old", 0x625547, 0x93633a, 1);
        floor["village"] = Photo("Interlock_Paving", 0x5a4f40, 0xb9895a, 1);
        g = new LRCanvas(1024, 1024, H(0x45433f));
        g.Image(Surf("Asphalt_Worn_C"), 0, 0, 1024, 1024, Color.white, 2, 2);
        g.Rect(24, 0, 32, 1024, Rgba(242, 194, 48, 0.92f)); g.Rect(968, 0, 32, 1024, Rgba(242, 194, 48, 0.92f));
        foreach (float x in new[] { 341f, 683f }) { g.Rect(x - 12, 120, 24, 360, Rgba(245, 245, 240, 0.86f)); g.Rect(x - 12, 632, 24, 360, Rgba(245, 245, 240, 0.86f)); }
        Texture2D roadTex = g.End(true);
        g = new LRCanvas(1024, 1024, new Color(0.5f, 0.5f, 1, 1));
        g.Image(Surf("Asphalt_Worn_N"), 0, 0, 1024, 1024, Color.white, 2, 2);
        floor["airport"] = Bump(New(new Color(1.45f, 1.5f, 1.6f, 1), roadTex, true), g.End(true), 1, 0.22f);

        side["grove"] = Photo("Mossy_Wall", 0x4e4530, 0x5d6455, 1);
        side["ruins"] = Photo("Red_Sandstone", 0x8d7e6c, 0x8e4a2a, 1);
        side["river"] = Photo("Planks_Weathered", 0x6e6258, 0x5a3a20, 0.8f);
        side["village"] = Photo("Clay_Plaster", 0x725a3c, 0x8f6a42, 1);
        side["airport"] = Photo("Concrete_Dirty", 0x8a8680, 0x6f6c66, 0.8f);
        pillar["grove"] = Photo("Mossy_Wall", 0x4e4530, 0x4c5244, 1);
        pillar["ruins"] = Photo("Red_Sandstone", 0x8d7e6c, 0x7d3e22, 1);
        pillar["river"] = Photo("Planks_Weathered", 0x6e6258, 0x4a2f18, 0.8f);
        pillar["village"] = Photo("Clay_Plaster", 0x725a3c, 0x7d5a36, 1);
        pillar["airport"] = Photo("Concrete_Dirty", 0x8a8680, 0x8a8780, 0.8f);
        kerb["grove"] = Photo("Rock_Wall", 0x514b42, 0x9a9f8c, 1);
        kerb["ruins"] = Photo("Red_Sandstone", 0x8d7e6c, 0xc9824f, 1);
        kerb["river"] = Photo("Planks_Weathered", 0x6e6258, 0x6b4422, 0.8f);
        kerb["village"] = Photo("Clay_Plaster", 0x725a3c, 0xcaa070, 1);
        kerb["airport"] = Lam(0xe5e1d6);

        bark = Photo("Bark", 0x5f5640, 0x4e3622, 1.2f);
        barkLight = Photo("Bark", 0x5f5640, 0x7a5434, 1.2f);
        palmLeaf = Lam(0x2f7d32); leaf = Lam(0x2f6232);
        canopy = new[] { Lam(0x24502a), Lam(0x2f6232), Lam(0x3b7136) };
        red = Lam(0xb8321f); white = Lam(0xf4efe2); chalk = Lam(0xfbf6ea);
        rope = Lam(0xb89a62); plankDark = Photo("Planks_Weathered", 0x6e6258, 0x5a3a20, 0.8f);
        laterite = Photo("Red_Sandstone", 0x8d7e6c, 0xa65a32, 1); lateriteDark = Photo("Red_Sandstone", 0x8d7e6c, 0x7d3e22, 1);
        thatch = Photo("Thatch", 0x545049, 0xb08a4a, 1.2f); mud = Photo("Clay_Plaster", 0x725a3c, 0x9a6a3c, 1);
        stone = Photo("Rock_Wall", 0x514b42, 0x8b8f80, 1.1f); stoneDark = Photo("Rock_Wall", 0x514b42, 0x5f6457, 1.1f);
        clay = Lam(0x9b4f2a); clayDark = Lam(0x6e3519);
        metal = New(H(0x8a8a86), null, true); metal.SetFloat("_Gloss", 0.4f);
        fire = Emissive(Basic(0xff8a1c), 0x552200); fireCore = Emissive(Basic(0xffd25a), 0x664400);
        cowrie = Emissive(New(H(0xfbf6ea), null, true), 0x2a2620); cowrie.SetFloat("_Gloss", 0.5f);
        cowrieSlit = Basic(0x6b5a3c);
        dark = Lam(0x1a120c);
        bumpYellow = Lam(0xf5c518); bumpBlack = Lam(0x16120c); coneOrange = Lam(0xf06a1c);
        lampGlow = Emissive(Basic(0xfff3c4), 0x554422);

        g = new LRCanvas(128, 64, H(0xc98a3a));
        for (int x = 0; x < 128; x += 16) { g.Line(x, 20, x + 8, 32, 3, H(0x5a3416)); g.Line(x + 8, 32, x + 16, 20, 3, H(0x5a3416)); }
        g.Line(0, 40, 128, 40, 3, H(0x5a3416));
        calabash = Lam(g.End(true)); calabash.SetFloat("_Gloss", 0.25f);

        g = new LRCanvas(64, 64, H(0xf4f1ea));   // Ghana-must-go plaid
        for (int x = 0; x < 64; x += 16) g.Rect(x, 0, 6, 64, Rgba(200, 40, 40, 0.75f));
        for (int y = 0; y < 64; y += 16) g.Rect(0, y, 64, 6, Rgba(40, 70, 170, 0.65f));
        gmg = Lam(g.End(true)); gmg.mainTextureScale = new Vector2(3, 2);

        g = new LRCanvas(64, 64, H(0x1b1712));
        for (int i = 0; i <= 64; i += 8) { g.Line(i, 0, i, 64, 2, H(0x6b5a3c)); g.Line(0, i, 64, i, 2, H(0x6b5a3c)); }
        for (int x = 6; x < 64; x += 12) g.Ellipse(x, 6, 4, 3, H(0xfbf6ea));
        net = Lam(g.End());

        boards = new[] {
            Sign(0x0d6b3d, 0xffffff, "MMIA AIRPORT", "INTERNATIONAL DEPARTURES · 5 km"),
            Sign(0x1f4fa3, 0xffffff, "VISA PHOTOS", "PASSPORT SIZE · ₦2,000"),
            Sign(0xf5c518, 0x16120c, "STUDY ABROAD FAIR", "THIS SATURDAY · FREE ENTRY"),
            Sign(0xc8452e, 0xfff7e2, "BON VOYAGE!", "NO FORGET YOUR JOLLOF SPICE"),
            Sign(0x16120c, 0xf5c518, "JAPA EXPRESS", "NEXT EXIT · ALL DESTINATIONS"),
        };
        // corner signs: index 0 points left, 1 points right
        arrowSign = new Material[2]; roadSign = new Material[2];
        for (int k = 0; k < 2; k++) {
            float d = k == 0 ? -1 : 1;
            g = new LRCanvas(256, 128, H(0x6b4422));
            g.Image(Surf("Planks_Weathered_C"), 0, 0, 256, 128, new Color(0.85f, 0.62f, 0.4f), 1, 0.5f);
            g.StrokeRect(4, 4, 248, 120, 8, H(0x3a2412));
            Color cw = H(0xfbf6ea);
            g.Rect(d > 0 ? 58 : 108, 52, 90, 24, cw);
            g.Poly(cw, 128 + d * 20, 24, 128 + d * 80, 64, 128 + d * 20, 104);
            arrowSign[k] = DoubleSided(Basic(g.End()));
            g = new LRCanvas(256, 128, H(0x0d6b3d));
            g.StrokeRect(8, 8, 240, 112, 6, Color.white);
            g.Text("DEPARTURES", d < 0 ? 150 : 106, 64, 30, Display, Color.white, 150);
            float ax = d < 0 ? 44 : 212;
            g.Rect(ax - (d > 0 ? 26 : 4), 56, 30, 16, Color.white);
            g.Poly(Color.white, ax + d * 4, 42, ax + d * 28, 64, ax + d * 4, 86);
            roadSign[k] = DoubleSided(Basic(g.End()));
        }

        g = new LRCanvas(512, 96, Color.white);
        for (int x = -48; x < 560; x += 48) g.Poly(H((x / 48) % 2 != 0 ? 0xffffff : 0xd22d2d), x, 96, x + 48, 0, x + 96, 0, x + 48, 96);
        g.Rect(150, 18, 212, 60, H(0x16120c));
        g.Text("VISA CHECK", 256, 49, 34, Display, H(0xf5c518), 190);
        gateBoard = Basic(g.End());

        ankara["blue"] = Pattern(0x1f4fa3, 0xf5c518, 0xd22d2d);
        ankara["orange"] = Pattern(0xe86a1c, 0x0a8a4a, 0xfff7e2);
        ankara["pink"] = Pattern(0xc2387a, 0xf5c518, 0x16120c);

        g = new LRCanvas(64, 64, new Color(0, 0, 0, 0));
        g.Circle(32, 32, 30, new Color(0, 0, 0, 0.3f));
        shadowTex = g.End();
        shadow = Transparent(Basic(shadowTex));

        // sky: a soft gradient with clouds, multiplied by each area's haze colour
        g = new LRCanvas(1024, 512, Color.white);
        g.GradV(0, 0, 1024, 512, new[] { 0f, 0.55f, 1f }, new[] { new Color(0.62f, 0.74f, 0.9f), new Color(0.9f, 0.94f, 1f), Color.white });
        for (int i = 0; i < 8; i++) {
            float cx = Rand(40, 980), cy = Rand(50, 230), w = Rand(90, 190);
            for (int k = 0; k < 6; k++) g.Radial(cx + Rand(-w, w), cy + Rand(-12, 12), 4, Rand(26, 54), new[] { 0f, 0.4f, 1f }, new[] { Rgba(255, 255, 255, 0.3f), Rgba(255, 255, 255, 0.18f), Rgba(255, 255, 255, 0) }, 2.2f, 1);
        }
        sky = Basic(g.End(false, false)); sky.SetFloat("_Fog", 0); sky.SetFloat("_ZWrite", 0); sky.renderQueue = 1000;

        // the land far below, tinted per area
        ground = Bump(New(Color.white, Surf("Forest_Leaves_C"), true), Surf("Forest_Leaves_N"), 0.8f, 0.1f);
        ground.mainTextureScale = new Vector2(60, 60);

        icon["magnet"] = Icon(0xb8321f, c => {
            c.Ring(64, 58, 22, 16, Color.white, 0, Mathf.PI);
            c.Line(42, 58, 42, 38, 16, Color.white); c.Line(86, 58, 86, 38, 16, Color.white);
        });
        icon["jump"] = Icon(0x2f5d34, c => { c.Poly(Color.white, 64, 30, 88, 52, 40, 52); c.Poly(Color.white, 64, 60, 88, 82, 40, 82); });
        icon["double"] = Icon(0xe0a92a, c => c.Text("2×", 64, 68, 46, Display, H(0x16120c)));
    }

    // cloth for one masquerade: woven stripes in its five colours
    public static Material[] Cloth(int[] colours) {
        Material[] m = new Material[colours.Length];
        for (int i = 0; i < m.Length; i++) m[i] = DoubleSided(Lam(Stripes(colours[i], colours[(i + 1) % colours.Length], colours[(i + 2) % colours.Length])));
        return m;
    }
}
