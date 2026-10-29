using System.Collections.Generic;
using UnityEngine;

// Every texture and material in the game, painted at startup. Names follow the original web version.
public static class T {
    public static Font Display, Body, BodySemi;
    static Shader lit;
    static readonly Dictionary<int, Material> lamCache = new Dictionary<int, Material>();

    public static Color H(int hex) { return LRCanvas.H(hex); }
    static Color Rgba(float r, float g, float b, float a) { return LRCanvas.Rgba(r, g, b, a); }
    static float Rand(float a, float b) { return Random.Range(a, b); }
    static bool Chance(float p) { return Random.value < p; }

    static Material New(Color c, Texture tex, bool isLit) {
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

    public static Texture2D skyTex, sunTex, roadTex, waterTex;
    public static Material glassPane, dark, door, appliance, tankBlack, roofFlat, trimFrames, trimFramesCut, trimShop, trimShopCut, weed, planks;
    public static Material[] plastic, paint;
    static readonly Dictionary<string, Material> walls = new Dictionary<string, Material>(), grounds = new Dictionary<string, Material>();
    static readonly string[] Plasters = { "Plaster", "Plaster_Painted", "Plaster_Patched", "Paint_Peeling", "Plaster_Rough" };
    static readonly int[] PlasterAvg = { 0xb0a89c, 0xaaa39f, 0x8d8a87, 0x966c5c, 0xbbb2a3 };
    public static readonly int[] WallColours = { 0xe9c46a, 0xf0a868, 0xa9d3b6, 0xa3c4e0, 0xf4efe3, 0xdc927d, 0xd2c19f, 0x86b9a8, 0xf2d0a4, 0xc9a6c8 };

    public static Texture2D Surf(string name) { return Resources.Load<Texture2D>("Surfaces/" + name); }
    public static Material Bump(Material m, Texture normal, float scale = 1, float gloss = 0) {
        m.SetTexture("_BumpMap", normal); m.SetFloat("_BumpScale", scale); m.SetFloat("_Gloss", gloss); m.EnableKeyword("LR_BUMP");
        return m;
    }
    // a photo surface tinted so that it averages out to the colour asked for
    static Material Photo(string surface, int avg, int colour, float tileU, float tileV, float bump = 1, float gloss = 0) {
        Color a = H(avg), c = H(colour);
        Material m = New(new Color(Mathf.Min(2, c.r / a.r), Mathf.Min(2, c.g / a.g), Mathf.Min(2, c.b / a.b), 1), Surf(surface + "_C"), true);
        m.mainTextureScale = new Vector2(tileU, tileV);
        return Bump(m, Surf(surface + "_N"), bump, gloss);
    }
    // painted plaster for one building: one of five plasters in one of ten Lagos colours
    public static Material Wall(int plaster, int colour) {
        string key = plaster + "/" + colour; Material m;
        if (!walls.TryGetValue(key, out m)) m = walls[key] = Photo(Plasters[plaster], PlasterAvg[plaster], WallColours[colour], 1, 1, 0.8f);
        return m;
    }
    public static Material Ground(int colour) {
        string key = colour.ToString(); Material m;
        if (!grounds.TryGetValue(key, out m)) m = grounds[key] = Photo("Laterite", 0x7e5435, colour, 21, 5, 0.7f);
        return m;
    }
    // which material fills each named slot of a kit piece
    public static Material Slot(string name, Material wall) {
        int dot = name.IndexOf('.'); if (dot > 0) name = name.Substring(0, dot);
        switch (name) {
            case "Wall": return wall;
            case "Glass": return glassPane;
            case "TrimFrames": return trimFrames;
            case "TrimFramesCutout": return trimFramesCut;
            case "TrimShop": return trimShop;
            case "TrimShopCutout": return trimShopCut;
            case "Dark": return dark;
            case "Door": return door;
            case "Concrete": return roofFlat;
            case "RoofZinc": case "Zinc": return roof;
            case "RoofFlat": return roofFlat;
            case "Appliance": return appliance;
            case "Tank": return tankBlack;
            case "Plastic": return plastic[Random.Range(0, plastic.Length)];
            case "Paint": return paint[Random.Range(0, paint.Length)];
            case "Wood": return planks;
            case "Weed": return weed;
        }
        return Lam(0x9a948a);
    }
    public static Texture2D[] boardTex;
    public static Material road, water, roof, tank, danfoSide, danfoFront, yellow, black, tyre, rim, green, metal, concrete, darkConcrete,
        sidewalk, curb, gutter, trunk, leaf, wire, wood, zinc, barrier, pothole, cone, coinFace, coinEdge,
        vest, schoolShirt, pinafore, pinaforeSkirt, officeShirt, sky, sun;
    public static Material[] facades, glass, signs, boards, danfoBack, banners, welcome, umbrella, goods;
    public static readonly Dictionary<string, Texture> ankara = new Dictionary<string, Texture>();
    public static readonly Dictionary<string, Texture2D> icon = new Dictionary<string, Texture2D>();

    static Texture2D BackTex(int bg, int fg, string text, float size = 30) {
        LRCanvas g = new LRCanvas(128, 128, H(bg));
        if (text.Length > 0) g.Text(text, 64, 62, size, Display, H(fg), 114);
        return g.End();
    }
    static Texture2D Pattern(int bg, int a, int b) {
        LRCanvas g = new LRCanvas(128, 128, H(bg));
        for (int y = 0; y < 128; y += 32) for (int x = 0; x < 128; x += 32) {
            int ox = (y / 32) % 2 == 1 ? 16 : 0;
            g.Circle(x + ox + 8, y + 16, 11, H(a));
            g.Circle(x + ox + 8, y + 16, 5, H(b));
            g.Line(x + ox + 20, y + 4, x + ox + 30, y + 14, 3, H(b));
        }
        return g.End(true);
    }
    // a canvas radial gradient: filled ellipses from the rim inwards, through stops given as (position 0..1, colour)
    static void Radial(LRCanvas g, float cx, float cy, float rx, float ry, float[] at, Color[] col) {
        const int n = 32;
        for (int i = n; i >= 1; i--) {
            float t = (float)i / n;
            int k = 0; while (k < at.Length - 2 && t > at[k + 1]) k++;
            Color c = t <= at[0] ? col[0] : Color.Lerp(col[k], col[k + 1], Mathf.InverseLerp(at[k], at[k + 1], t));
            g.Ellipse(cx, cy, rx * t, ry * t, c);
        }
    }
    static Material Shine(int c) {
        LRCanvas g = new LRCanvas(64, 64, H(c));
        for (int y = 0; y < 64; y += 8) g.Rect(0, y, 64, 3, Rgba(255, 255, 255, 0.35f));
        return Emissive(Lam(g.End(true)), 0x1a1400);
    }
    static Texture2D Icon(int bg, System.Action<LRCanvas> draw) {
        LRCanvas g = new LRCanvas(128, 128, new Color(1, 1, 1, 0));
        g.Circle(64, 64, 62, Rgba(255, 255, 255, 0.35f));
        g.Circle(64, 64, 52, H(bg));
        g.Ring(64, 64, 52, 6, H(0x16120c));
        draw(g);
        return g.End();
    }

    static Texture2D BoardTex(string title, string sub, int bg, int fgc) {
        Color fg = H(fgc);
        LRCanvas g = new LRCanvas(512, 256, H(bg));
        for (int k = 0; k < 6; k++) g.Rect(k * 100 - 30, 0, 40, 256, Rgba(255, 255, 255, 0.08f));
        g.Text(title, 256, 256 * 0.42f, 90, Display, fg, 462);
        g.Text(sub.Replace("₦", "N"), 256, 256 * 0.76f, 36, Body, fg, 442);
        g.StrokeRect(5, 5, 502, 246, 10, H(0x16120c));
        return g.End();
    }
    static Texture2D BannerTex(string text, int bg, int fgc) {
        Color fg = H(fgc);
        LRCanvas g = new LRCanvas(512, 256, H(bg));
        g.StrokeRect(14, 14, 484, 228, 8, fg);
        g.Text(text, 256, 132, 70, Display, fg, 442);
        return g.End();
    }
    // The signs that say where you are: the gantry at each area border, the state billboard (also the strip along
    // the footbridges) and the welcome banner. Repainted whenever the city changes.
    public static Material boardStrip;
    public static void Place(City c) {
        if (welcome != null) foreach (Material m in welcome) { Object.Destroy(m.mainTexture); Object.Destroy(m); }
        welcome = new Material[c.zones.Length];
        for (int i = 0; i < welcome.Length; i++) {
            LRCanvas g = new LRCanvas(1024, 192, H(0x0d6b3d));
            g.StrokeRect(12, 12, 1000, 168, 8, Color.white);
            g.Text(c.state.ToUpperInvariant(), 512, 44, 26, Body, Color.white, 400);
            g.Text("WELCOME TO " + c.zones[i].name.ToUpperInvariant(), 512, 118, 70, Display, Color.white, 944);
            welcome[i] = Basic(g.End());
        }
        Object.Destroy(boardTex[2]);
        boardTex[2] = BoardTex(c.name.ToUpperInvariant(), c.motto.ToUpperInvariant(), 0x0a8a4a, 0xffffff);
        boards[2].mainTexture = boardTex[2]; boardStrip.mainTexture = boardTex[2];
        Object.Destroy(banners[0].mainTexture);
        banners[0].mainTexture = BannerTex("WELCOME TO " + c.name.ToUpperInvariant(), 0x0a8a4a, 0xffffff);
    }

    public static void Build() {
        Display = Resources.Load<Font>("Bungee-Regular");
        Body = Resources.Load<Font>("Archivo-ExtraBold");
        BodySemi = Resources.Load<Font>("Archivo-SemiBold");
        LRCanvas g;
        int Y = 0xf5c518, K = 0x16120c;

        g = new LRCanvas(1024, 512, H(0xe9d3a2));
        g.GradV(0, 0, 1024, 512, new[] { 0f, 0.45f, 0.62f, 1f }, new[] { H(0x6fa9c6), H(0xb6d2cb), H(0xecd9ab), H(0xe9d3a2) });
        for (int i = 0; i < 9; i++) {
            float cx = Rand(40, 980), cy = Rand(60, 250), w = Rand(90, 190);
            for (int k = 0; k < 6; k++) g.Radial(cx + Rand(-w, w), cy + Rand(-12, 12), 4, Rand(26, 54), new[] { 0f, 0.4f, 1f }, new[] { Rgba(255, 252, 240, 0.34f), Rgba(255, 250, 236, 0.2f), Rgba(255, 250, 236, 0) }, 2.2f, 1);
        }
        skyTex = g.End(false, false);
        sky = Basic(skyTex); sky.SetFloat("_Fog", 0); sky.SetFloat("_ZWrite", 0); sky.renderQueue = 1000;

        g = new LRCanvas(128, 128, Rgba(255, 230, 180, 0));
        g.Radial(64, 64, 6, 64, new[] { 0f, 0.25f, 1f }, new[] { Rgba(255, 250, 230, 1), Rgba(255, 236, 190, 0.9f), Rgba(255, 230, 180, 0) });
        sunTex = g.End();
        sun = Transparent(Basic(sunTex), false);

        // road
        g = new LRCanvas(1024, 1024, H(0x4a4744));
        g.Image(Surf("Asphalt_Worn_C"), 0, 0, 1024, 1024, Color.white, 2, 3);
        for (int i = 0; i < 5; i++) g.Rect(Rand(80, 780), Rand(0, 860), Rand(80, 180), Rand(60, 160), Rgba(20, 18, 16, 0.16f));
        System.Func<float, float> px = x => (x + 3.7f) / 7.4f * 1024;
        g.Rect(px(-3.5f) - 8, 0, 16, 1024, Rgba(242, 194, 48, 0.92f)); g.Rect(px(3.5f) - 8, 0, 16, 1024, Rgba(242, 194, 48, 0.92f));
        foreach (float x in new[] { -1.1f, 1.1f }) for (int y = 0; y < 1024; y += 512) g.Rect(px(x) - 8, y + 80, 16, 240, Rgba(245, 245, 240, 0.86f));
        roadTex = g.End(true);
        g = new LRCanvas(1024, 1024, new Color(0.5f, 0.5f, 1, 1));
        g.Image(Surf("Asphalt_Worn_N"), 0, 0, 1024, 1024, Color.white, 2, 3);
        road = Bump(New(new Color(1.45f, 1.5f, 1.6f, 1), roadTex, true), g.End(true), 1, 0.22f);
        road.mainTextureScale = new Vector2(1, 20);

        g = new LRCanvas(256, 256, H(0x3f8a88));
        for (int i = 0; i < 160; i++) { float x = Rand(0, 256), y = Rand(0, 256); g.Line(x, y, x + Rand(8, 26), y, 2, Chance(0.5f) ? Rgba(255, 255, 255, 0.18f) : Rgba(10, 50, 50, 0.2f)); }
        waterTex = g.End(true);
        water = Lam(waterTex); water.mainTextureScale = new Vector2(60, 60);

        // building fronts: wall colour plus a burglar-proofed window
        int[] walls = { 0xe9c46a, 0xf0a868, 0xa9d3b6, 0xa3c4e0, 0xf4efe3, 0xdc927d, 0xd2c19f, 0x86b9a8, 0xf2d0a4, 0xc9a6c8 };
        facades = new Material[walls.Length];
        for (int i = 0; i < walls.Length; i++) {
            g = new LRCanvas(128, 128, H(walls[i]));
            for (int k = 0; k < 7; k++) g.Rect(Rand(0, 128), Rand(0, 51), Rand(3, 9), 128, Rgba(70, 45, 20, 0.07f));
            g.Rect(0, 118, 128, 10, Rgba(0, 0, 0, 0.18f));
            g.Rect(30, 26, 68, 62, H(i % 3 == 0 ? 0x2f4a5a : 0x3a3f4a));
            g.Rect(32, 28, 30, 58, Rgba(255, 255, 255, 0.18f));
            for (int x = 36; x < 98; x += 10) g.Line(x, 26, x, 88, 3, H(0x1e1e1e));
            g.Line(30, 57, 98, 57, 3, H(0x1e1e1e));
            g.StrokeRect(28, 24, 72, 66, 4, Rgba(255, 255, 255, 0.6f));
            if (i % 2 == 0) { g.Rect(70, 94, 26, 16, H(0xe8e8e2)); g.Rect(73, 97, 20, 2, H(0x9a9a92)); g.Rect(73, 101, 20, 2, H(0x9a9a92)); g.Rect(73, 105, 20, 2, H(0x9a9a92)); }
            facades[i] = Lam(g.End(true));
        }
        int[] glassCol = { 0x5f8fa6, 0x4f7f86, 0x7d98ad };
        glass = new Material[3];
        for (int i = 0; i < 3; i++) {
            g = new LRCanvas(128, 128, H(glassCol[i]));
            g.Rect4(0, 0, 128, 128, Rgba(255, 255, 255, 0.25f), Rgba(255, 255, 255, 0.12f), Rgba(255, 255, 255, 0), Rgba(255, 255, 255, 0.12f));
            for (int x = 0; x < 128; x += 32) g.Rect(x, 0, 3, 128, Rgba(20, 30, 40, 0.55f));
            for (int y = 0; y < 128; y += 42) g.Rect(0, y, 128, 4, Rgba(20, 30, 40, 0.55f));
            glass[i] = Lam(g.End(true));
        }
        roof = Photo("Zinc_Rusty", 0x572918, 0x8f7a68, 1, 1, 1, 0.25f);
        roofFlat = Photo("Concrete_Dirty", 0x8a8680, 0xa29a8c, 1, 1, 0.8f);
        tank = Lam(0x1d1d1d);
        glassPane = New(H(0x1f2c38), null, true); glassPane.SetFloat("_Gloss", 1.2f); glassPane.SetFloat("_Rim", 0.5f);
        dark = Lam(0x15120f);
        planks = Photo("Planks_Weathered", 0x6e6258, 0x7a5a3a, 1, 1, 0.8f);
        door = Photo("Planks_Weathered", 0x6e6258, 0x3d5a78, 1, 1, 0.8f);
        appliance = New(H(0xd8d8d2), null, true); appliance.SetFloat("_Gloss", 0.3f);
        tankBlack = New(H(0x1b1b1b), null, true); tankBlack.SetFloat("_Gloss", 0.6f);
        weed = Lam(0x4f8a35);
        trimFrames = Bump(Lam(Surf("Trim_Frames_C")), Surf("Trim_Frames_N"), 1, 0.15f);
        trimFramesCut = DoubleSided(Bump(Lam(Surf("Trim_Frames_C")), Surf("Trim_Frames_N"), 1, 0.15f)); trimFramesCut.SetFloat("_Cutoff", 0.5f);
        trimShop = Bump(Lam(Surf("Trim_Shop_C")), Surf("Trim_Shop_N"), 1, 0.2f);
        trimShopCut = DoubleSided(Bump(Lam(Surf("Trim_Shop_C")), Surf("Trim_Shop_N"), 1, 0.2f)); trimShopCut.SetFloat("_Cutoff", 0.5f);
        plastic = new[] { Lam(0xd22d2d), Lam(0x1f4fa3), Lam(0xf4f4f4), Lam(0x0a8a4a) };
        paint = new[] { Lam(0x1f4fa3), Lam(0xc8452e), Lam(0xf5c518), Lam(0x2e7d5a) };

        object[][] shops = {
            new object[] { "MAMA NKECHI BUKA", 0xc8452e, 0xffffff }, new object[] { "GOD'S GRACE PHARMACY", 0x0a8a4a, 0xffffff }, new object[] { "OLUWASEUN ELECTRONICS", 0x1f4fa3, 0xf5c518 },
            new object[] { "ALHAJI & SONS", 0x16120c, 0xf5c518 }, new object[] { "BLESSED HANDS SALON", 0xb0306e, 0xffffff }, new object[] { "SUYA SPOT", 0x7a2a12, 0xf5c518 },
            new object[] { "FAITH TAILORING", 0x2b6f8f, 0xffffff }, new object[] { "OMO NAIJA PHONES", 0xf5c518, 0x16120c }, new object[] { "JESUS IS LORD MOTORS", 0xffffff, 0xc8452e },
            new object[] { "IYA BASIRAT AMALA", 0x3d7a2a, 0xffffff }, new object[] { "DIVINE FAVOUR STORES", 0x5a3d8f, 0xffffff }, new object[] { "RECHARGE CARD HERE", 0xe86a1c, 0xffffff },
            new object[] { "MR. BIGGIE FOODS", 0xd22d2d, 0xffffff }, new object[] { "NO SHAKING BARBING", 0x16120c, 0xffffff },
        };
        signs = new Material[shops.Length];
        for (int i = 0; i < shops.Length; i++) {
            Color fg = H((int)shops[i][2]);
            g = new LRCanvas(512, 96, H((int)shops[i][1]));
            g.StrokeRect(8, 8, 496, 80, 6, fg);
            g.Text((string)shops[i][0], 256, 51, 48, Display, fg, 462);
            signs[i] = Basic(g.End());
        }

        object[][] bds = {
            new object[] { "ZOBO & CHAPMAN", "ICE COLD · ₦300", 0xc8452e, 0xfff7e2 }, new object[] { "SUYA NIGHT", "EVERY FRIDAY", 0x16120c, 0xf5c518 },
            new object[] { "LAGOS", "CENTRE OF EXCELLENCE", 0x0a8a4a, 0xffffff }, new object[] { "JOLLOF WARS", "THE FINALS · TBS", 0xf08c1a, 0x16120c },
            new object[] { "NO WAHALA BANK", "SAVE SMALL SMALL", 0x1f4fa3, 0xffffff }, new object[] { "OWAMBE", "THIS SATURDAY · ASO-EBI", 0x7b2d8f, 0xf5c518 },
        };
        boards = new Material[bds.Length]; boardTex = new Texture2D[bds.Length];
        for (int i = 0; i < bds.Length; i++) {
            boardTex[i] = BoardTex((string)bds[i][0], (string)bds[i][1], (int)bds[i][2], (int)bds[i][3]);
            boards[i] = Basic(boardTex[i]);
        }
        boardStrip = Basic(boardTex[2]);

        // danfo
        g = new LRCanvas(512, 192, H(Y));
        g.Rect(14, 14, 484, 66, H(0x2c3a45));
        g.Rect(14, 14, 484, 18, Rgba(255, 255, 255, 0.15f));
        for (int x = 100; x < 482; x += 95) g.Rect(x, 14, 8, 66, H(Y));
        g.Rect(0, 100, 512, 14, H(K)); g.Rect(0, 128, 512, 14, H(K));
        g.StrokeRect(300, 10, 120, 172, 3, Rgba(0, 0, 0, 0.35f));
        for (int i = 0; i < 6; i++) g.Circle(Rand(0, 512), Rand(150, 192), Rand(4, 14), Rgba(120, 70, 20, 0.25f));
        danfoSide = Lam(g.End());
        string[] slogans = { "NO TELEPHONE TO HEAVEN", "GOD'S TIME IS THE BEST", "NO KING AS GOD", "SHINE YOUR EYE", "NO CONDITION IS PERMANENT", "EXCUSE ME SIR", "EYES DON'T SEE" };
        danfoBack = new Material[slogans.Length];
        for (int i = 0; i < slogans.Length; i++) {
            g = new LRCanvas(256, 256, H(Y));
            g.Rect(16, 14, 224, 82, H(0x2c3a45));
            g.Rect(16, 14, 224, 22, Rgba(255, 255, 255, 0.15f));
            g.Rect(0, 132, 256, 10, H(K)); g.Rect(0, 152, 256, 10, H(K));
            g.Text(slogans[i], 128, 116, 22, Display, H(i % 2 == 1 ? 0xc8452e : K), 232);
            g.Rect(78, 182, 100, 30, Color.white); g.StrokeRect(78, 182, 100, 30, 2, H(K));
            g.Text("LAGOS", 128, 190, 11, Body, H(0x0a8a4a));
            g.Text("KJA " + (100 + i * 37) + " XY", 128, 204, 14, Body, H(K));
            g.Rect(14, 176, 26, 18, H(0xc8452e)); g.Rect(216, 176, 26, 18, H(0xc8452e));
            danfoBack[i] = Lam(g.End());
        }
        g = new LRCanvas(256, 256, H(Y));
        g.Rect(12, 12, 232, 104, H(0x2c3a45));
        g.Poly(Rgba(255, 255, 255, 0.2f), 20, 20, 100, 20, 40, 108, 20, 108);
        g.Rect(0, 140, 256, 10, H(K));
        g.Circle(44, 186, 20, H(0xfff6c8)); g.Circle(212, 186, 20, H(0xfff6c8));
        g.Rect(80, 172, 96, 34, H(0x333333));
        for (int y = 176; y < 204; y += 7) g.Rect(84, y, 88, 3, H(0x777777));
        g.Text("OSHODI–OBALENDE", 128, 127, 18, Display, H(K), 240);
        danfoFront = Lam(g.End());

        yellow = Lam(0xf5c518); black = Lam(0x1b1b1b); tyre = Lam(0x151515); rim = Lam(0x9a9a9a); green = Lam(0x0a8a4a); metal = Lam(0x777a7d);
        concrete = Lam(0xb9b1a3); darkConcrete = Lam(0x8a8478); sidewalk = Photo("Concrete_Pavement", 0x7f7566, 0xc2b294, 1.2f, 10, 1); curb = Lam(0xe7e1d2); gutter = Lam(0x2f3a2c);
        trunk = Lam(0x7d5a3a); leaf = Lam(0x2f7d32); wire = Basic(0x222222); wood = Lam(0x7a5434); zinc = Lam(0x8f8f88);

        g = new LRCanvas(256, 64, Color.white);
        for (int x = -64; x < 320; x += 40) g.Poly(H((x / 40) % 2 != 0 ? 0xffffff : 0xd22d2d), x, 64, x + 40, 0, x + 80, 0, x + 40, 64);
        g.Rect(70, 14, 116, 36, H(K));
        g.Text("ROAD WORKS", 128, 33, 20, Display, H(Y), 108);
        barrier = Lam(g.End());

        object[][] bn = { new object[] { "WELCOME TO LAGOS", 0x0a8a4a, 0xffffff }, new object[] { "SALE! SALE! SALE!", 0xd22d2d, 0xffffff }, new object[] { "CRUSADE THIS SUNDAY", 0x1f4fa3, 0xf5c518 },
            new object[] { "HAPPY 60TH MAMA", 0x7b2d8f, 0xffffff }, new object[] { "FUEL DEY!", 0xf5c518, 0x16120c } };
        banners = new Material[bn.Length];
        for (int i = 0; i < bn.Length; i++) banners[i] = DoubleSided(Lam(BannerTex((string)bn[i][0], (int)bn[i][1], (int)bn[i][2])));
        // pothole: a dark puddle fading out at the rim, with a glint of sky on the water
        g = new LRCanvas(128, 128, new Color(0, 0, 0, 0));
        Radial(g, 64, 64, 62, 60, new[] { 0.16f, 0.55f, 0.8f, 1 }, new[] { H(0x3d5652), H(0x2c2a26), H(0x1d1b18), Rgba(60, 55, 50, 0) });
        g.Ellipse(52, 56, 16, 8, Rgba(180, 210, 210, 0.35f), 0.4f);
        pothole = Transparent(Lam(g.End()));
        cone = Lam(0xf06a1c);

        // coin: gold disc with a green naira sign
        g = new LRCanvas(128, 128, H(0xc48d00));
        Radial(g, 60, 58, 64, 64, new[] { 0.12f, 0.6f, 1 }, new[] { H(0xfff2a8), H(0xf5c518), H(0xc48d00) });
        g.Ring(64, 64, 52, 6, H(0xb07d00));
        Color naira = H(0x0a6a38);
        g.Line(44, 92, 44, 36, 11, naira, true); g.Line(44, 36, 84, 92, 11, naira, true); g.Line(84, 92, 84, 36, 11, naira, true);
        g.Line(32, 58, 96, 58, 6, naira, true); g.Line(32, 72, 96, 72, 6, naira, true);
        coinFace = Emissive(Lam(g.End()), 0x332200);
        coinEdge = Emissive(Lam(0xd9a400), 0x332200);

        // ankara prints for dresses, by name
        ankara["blue"] = Pattern(0x1f4fa3, 0xf5c518, 0xd22d2d);
        ankara["orange"] = Pattern(0xe86a1c, 0x0a8a4a, 0xfff7e2);
        ankara["pink"] = Pattern(0xc2387a, 0xf5c518, 0x16120c);
        ankara["green"] = Pattern(0x0a8a4a, 0xf08c1a, 0xfff7e2);
        ankara["purple"] = Pattern(0x5a3d8f, 0xf5c518, 0xffffff);

        // hi-vis vest for the danfo crew
        g = new LRCanvas(128, 128, H(0xf08c1a));
        g.Rect(0, 70, 128, 12, H(0xe6e9ec)); g.Rect(0, 96, 128, 12, H(0xe6e9ec));
        g.Text("042", 64, 38, 30, Display, H(K));
        vest = Lam(g.End());
        schoolShirt = Lam(BackTex(0xf4f4f4, 0x1f3a6b, ""));
        g = new LRCanvas(128, 128, H(0xf4f4f4));
        g.Rect(14, 30, 100, 98, H(0x2f5aa8)); g.Rect(14, 0, 20, 30, H(0x2f5aa8)); g.Rect(94, 0, 20, 30, H(0x2f5aa8));
        pinafore = Lam(g.End());
        pinaforeSkirt = Lam(0x2f5aa8);
        g = new LRCanvas(128, 128, H(0xdfe9f3));
        g.Rect(56, 0, 16, 90, H(0x8f1d2c));
        officeShirt = Lam(g.End());

        int[][] um = { new[] { 0xd22d2d, 0xffffff }, new[] { 0x0a8a4a, 0xf5c518 }, new[] { 0x1f4fa3, 0xffffff }, new[] { 0xf08c1a, 0x16120c } };
        umbrella = new Material[4];
        for (int i = 0; i < 4; i++) {
            g = new LRCanvas(128, 16, H(um[i][0]));
            for (int k = 0; k < 8; k++) g.Rect(k * 16, 0, 16, 16, H(um[i][k % 2]));
            umbrella[i] = Lam(g.End());
        }
        goods = new[] { Lam(0xd23c1e), Lam(0xf5a623), Lam(0x6aa84f), Lam(0xffe599) };

        icon["magnet"] = Icon(0xd22d2d, c => {
            c.Ring(64, 58, 22, 16, Color.white, 0, Mathf.PI);
            c.Line(42, 58, 42, 38, 16, Color.white); c.Line(86, 58, 86, 38, 16, Color.white);
            c.Line(42, 40, 42, 30, 16, H(0xc0c0c0)); c.Line(86, 40, 86, 30, 16, H(0xc0c0c0));
        });
        icon["jump"] = Icon(0x0a8a4a, c => {
            c.Poly(Color.white, 64, 30, 88, 52, 40, 52);
            c.Poly(Color.white, 64, 60, 88, 82, 40, 82);
        });
        icon["double"] = Icon(0xf08c1a, c => c.Text("2×", 64, 68, 44, Display, Color.white));
    }
}
