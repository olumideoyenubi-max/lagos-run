using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Zone { public string key, name; public int len, bg, ground; public float fogNear, fogFar; }
public class CharDef { public string id, name, line; public int swA, swB; public Person look; }

// Japa from the Shrine: run from the Egungun along a raised causeway that turns at right angles, from the Sacred
// Grove to the airport road. Positions and angles are kept in the web version's coordinates (x right, y up, z toward
// the camera) and converted with V when applied to transforms.
public partial class JapaGame : MonoBehaviour {
    const float HALF = 3.3f, PATH_W = HALF * 2, GRAVITY = -30, JUMP_V = 12, START_SPEED = 13, MAX_SPEED = 31;
    const float P_W = 0.7f, P_D = 0.6f, P_H = 1.7f, P_H_SLIDE = 0.75f, LOOKAHEAD = 230, GROUND_Y = P.GROUND_Y, PI = Mathf.PI;
    static readonly float[] LANES = { -2.2f, 0, 2.2f };

    public static readonly Zone[] ZONES = {
        new Zone { key = "grove", name = "Sacred Grove", len = 700, bg = 0x6f8c66, fogNear = 18, fogFar = 120, ground = 0x2d4a2a },
        new Zone { key = "ruins", name = "Laterite Ruins", len = 700, bg = 0xd2a07a, fogNear = 25, fogFar = 140, ground = 0x7a3f22 },
        new Zone { key = "river", name = "Osun River Bridge", len = 650, bg = 0x9cc2bd, fogNear = 25, fogFar = 150, ground = 0x2f6f73 },
        new Zone { key = "village", name = "Village Outskirts", len = 700, bg = 0xdcc497, fogNear = 28, fogFar = 150, ground = 0x8a6a3a },
        new Zone { key = "airport", name = "Road to the Airport", len = 800, bg = 0xa9cbe0, fogNear = 30, fogFar = 170, ground = 0x5f7a4a },
    };
    static int zoneTotal;
    static int ZoneIndexAt(float d) {
        float r = ((d % zoneTotal) + zoneTotal) % zoneTotal;
        for (int i = 0; i < ZONES.Length; i++) { if (r < ZONES[i].len) return i; r -= ZONES[i].len; }
        return 0;
    }
    static string ZoneKeyAt(float d) { return ZONES[ZoneIndexAt(d)].key; }
    // How tough the run has become: 0 at the shrine, 1 after 3 km, then creeping on to 2 at 8 km and no further.
    // Speed, how close the obstacles come, how often two lanes are blocked, how soon the next corner arrives, how
    // long the Egungun stay on you and how rare the pick-ups are all follow it, so the run hardens a little at a time.
    static float Tough(float d) { return d < 3000 ? Mathf.Max(0, d) / 3000 : 1 + Mathf.Min(1, (d - 3000) / 5000); }
    static float SpeedAt(float d) { float g = Tough(d); return g < 1 ? Mathf.Lerp(START_SPEED, 27, g) : Mathf.Lerp(27, MAX_SPEED, g - 1); }

    static readonly CharDef[] CHARS = {
        new CharDef { id = "tunde", name = "Tunde", swA = 0x0a8a4a, swB = 0xffffff, line = "Japa! No look back!",
            look = new Person { model = "Tunde", dress = new[] { "suit", "#12a05a" }, prop = "calabash" } },
        new CharDef { id = "amaka", name = "Amaka", swA = 0xf5c518, swB = 0x2b4f7a, line = "Mama, I don leave the shrine!",
            look = new Person { model = "Amaka", dress = new[] { "tank", "#f5c518", "pants", "#2b4f7a" }, backpack = 0x16120c, prop = "calabash" } },
        new CharDef { id = "basirat", name = "Iya Basirat", swA = 0x1f4fa3, swB = 0xc9a227, line = "Who send me come this shrine?!",
            look = new Person { model = "Zainab", dress = new[] { "suit", "blue", "afro", "hide" }, hat = "gele", hatColor = 0xc9a227, prop = "calabash" } },
        new CharDef { id = "kemi", name = "Kemi", swA = 0x2f5aa8, swB = 0xf4f4f4, line = "I just wan play, I no know!",
            look = new Person { model = "Zainab", scale = 0.74f, headScale = 1.22f, dress = new[] { "suit", "#2f5aa8" }, backpack = 0xf5c518, prop = "calabash" } },
    };
    static readonly Dictionary<string, KeyValuePair<string, float>> POWER_INFO = new Dictionary<string, KeyValuePair<string, float>> {
        { "magnet", new KeyValuePair<string, float>("Cowrie magnet", 10) }, { "jump", new KeyValuePair<string, float>("Spring legs", 10) }, { "double", new KeyValuePair<string, float>("Double cowries", 12) },
    };
    static readonly string[] POWER_KEYS = { "magnet", "jump", "double" };
    static readonly Dictionary<string, string> CRASH = new Dictionary<string, string> {
        { "root", "Root trip you!" }, { "bump", "Speed bump don stop you!" }, { "arch", "You knock head for arch!" }, { "fallen", "Tree don block you!" }, { "gate", "Visa check don hold you!" },
        { "idol", "You jam the shrine statue!" }, { "bags", "Ghana-must-go bags don block road!" }, { "fire", "Fire don burn your leg!" }, { "cone", "Cone trip you!" },
    };

    static float Rand(float a, float b) { return Random.Range(a, b); }
    static T2 Pick<T2>(params T2[] a) { return a[Random.Range(0, a.Length)]; }
    static Vector3 Dir(float th) { return new Vector3(-Mathf.Sin(th), 0, -Mathf.Cos(th)); }
    static Vector3 Right(float th) { return new Vector3(Mathf.Cos(th), 0, -Mathf.Sin(th)); }
    static float AngleLerp(float a, float b, float t) { float d = b - a; while (d > PI) d -= PI * 2; while (d < -PI) d += PI * 2; return a + d * t; }

    // ---------- the path ----------
    // A chain of straight pieces joined by 90-degree corners. Each piece has its own frame: s runs forward along it,
    // x runs to the right. Headings stay north, west or east, so the path never crosses itself.
    class Ob { public string type; public float s, s0, s1, x0, x1, y0, y1; public int lane = -1; public bool ps, px; }
    class Coin { public float s, x, y; public GameObject obj; }
    class Pow { public string kind; public float s, x; public GameObject obj; }
    class Piece {
        public int idx, heading, turn; public Vector3 O, C, dir, right; public float theta, L, d0; public string zone; public GameObject go;
        public List<Ob> obstacles = new List<Ob>(); public List<float[]> gaps = new List<float[]>(); public List<Coin> coins = new List<Coin>();
        public List<Pow> powers = new List<Pow>(); public List<Transform> fires = new List<Transform>(); public List<Vector3> firePos = new List<Vector3>();
        public List<Mesh> owned = new List<Mesh>();
    }
    class Trail { public float d, th; public Vector3 p; }
    class Egungun { public Transform root, body; public List<Transform> strips = new List<Transform>(); public List<float> angle = new List<float>(); public Vector3 pos; }

    readonly List<Piece> pieces = new List<Piece>();
    readonly List<Egungun> masks = new List<Egungun>();
    readonly List<Trail> history = new List<Trail>();
    readonly List<Transform> billboards = new List<Transform>();
    readonly Dictionary<string, Shape> powerShapes = new Dictionary<string, Shape>();
    readonly Dictionary<string, float> power = new Dictionary<string, float> { { "magnet", 0 }, { "jump", 0 }, { "double", 0 } };

    Camera cam; Transform skyQuad, groundQuad, plane; float camBack = 7, camUp = 4; int lastW, lastH;
    Runner player; LRAudio sound;
    string mode = "boot", charId = "tunde", reason = "";
    int best, pi, lane = 1, prevLane = 1, queued, coins, zone, hintCount, atmosTarget;
    float s, x, y, vy, slide, dist, score, speed = START_SPEED, phase, time, invuln, chase, chaseGap = 3.5f, showT, dyingT, nextPower = 300, camYaw, yaw, tumble, planeT;
    bool grounded = true, caught, fall, shotMode, auto, camSet;
    Vector3 camPos, lookPos;
    Color bgNow; float fogNear, fogFar; Color groundNow;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void KeepTourRunning() {
        if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-lrshots") >= 0) Application.runInBackground = true;
    }

    void Start() {
        zoneTotal = 0; foreach (Zone z in ZONES) zoneTotal += z.len;
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 1;
        QualitySettings.antiAliasing = Application.isMobilePlatform ? 2 : 4;
        Input.simulateMouseWithTouches = false;
        bool light = Application.isMobilePlatform || Application.platform == RuntimePlatform.WebGLPlayer;
        QualitySettings.skinWeights = light ? SkinWeights.TwoBones : SkinWeights.FourBones;

        best = PlayerPrefs.GetInt("japa.best", 0);
        charId = PlayerPrefs.GetString("japa.char", "tunde");

        // dusky light under the trees: a warm sky, a dim bounce from the ground, one sun
        Shader.SetGlobalColor("_LRSky", T.H(0xfff1d6) * 0.62f);
        Shader.SetGlobalColor("_LRGround", T.H(0x4a3a28) * 0.9f);
        Shader.SetGlobalColor("_LRSun", T.H(0xfff0cf) * 0.72f);
        Shader.SetGlobalVector("_LRSunDir", V.P(-6, 12, 5).normalized);

        Light sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional; sun.transform.rotation = Quaternion.LookRotation(-V.P(-6, 12, 5).normalized);
        sun.shadows = light ? LightShadows.Hard : LightShadows.Soft; sun.shadowStrength = 1; sun.shadowBias = 0.05f; sun.shadowNormalBias = 0.4f;
        QualitySettings.shadows = light ? ShadowQuality.HardOnly : ShadowQuality.All;
        QualitySettings.shadowDistance = light ? 35 : 80;
        QualitySettings.shadowCascades = light ? 1 : 4;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        QualitySettings.shadowResolution = light ? ShadowResolution.Medium : ShadowResolution.VeryHigh;
        QualitySettings.shadowProjection = ShadowProjection.StableFit;

        T.Build();
        P.Build();
        Moves.Load();

        GameObject cg = new GameObject("Camera");
        cg.tag = "MainCamera";
        cam = cg.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.nearClipPlane = 0.1f; cam.farClipPlane = 400; cam.allowMSAA = true; cam.allowHDR = false;
        cg.AddComponent<AudioListener>();
        if (!light) cg.AddComponent<LRPost>();
        skyQuad = new Shape().Quad(1, 1, T.sky, Matrix4x4.identity).Make("sky", cg.transform).transform;
        skyQuad.localPosition = new Vector3(0, 0, 390);
        groundQuad = new Shape().Quad(600, 600, T.ground, V.M(0, 0, 0, -PI / 2, 0, 0)).Make("ground").transform;
        foreach (string k in POWER_KEYS) powerShapes[k] = new Shape().Quad(1.3f, 1.3f, T.Transparent(T.Basic(T.icon[k])), Matrix4x4.identity);

        sound = new LRAudio(gameObject);
        int[][] palettes = {
            new[] { 0xc8452e, 0xf5c518, 0x1f4fa3, 0x0a8a4a, 0xfbf6ea }, new[] { 0x7b2d8f, 0xf08c1a, 0x16120c, 0xe9d3a2, 0xd22d2d }, new[] { 0x0a6a6a, 0xc9a227, 0xc2387a, 0xfbf6ea, 0x3a2a1a },
        };
        foreach (int[] pal in palettes) masks.Add(MakeEgungun(pal));
        if (P.planeModel != null) {
            plane = new GameObject("plane").transform;
            GameObject m = Instantiate(P.planeModel, plane);
            Bounds b = new Bounds(); bool first = true;
            foreach (Renderer r in m.GetComponentsInChildren<Renderer>()) { if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds); }
            m.transform.localScale *= 34 / Mathf.Max(b.size.x, b.size.z);
            plane.gameObject.SetActive(false);
        }
        SetCharacter(charId);
        BuildUI();
        Resize();
        ResetWorld(0);
        mode = "menu";
        ShowScreen(startScreen);

        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-lrshots") { shotMode = true; StartCoroutine(Shots(args[i + 1])); }
    }

    // Egungun masquerade: rings of cloth panels over a dark body, flaring as it moves
    Egungun MakeEgungun(int[] colours) {
        Material[] cloth = T.Cloth(colours);
        Egungun e = new Egungun { root = new GameObject("egungun").transform };
        e.body = new GameObject("body").transform; e.body.SetParent(e.root, false);
        Shape core = new Shape();
        core.Cyl(0.34f, 0.5f, 2.3f, 12, T.Lam(0x1b1712), 0, 1.15f, 0);
        core.Box(0.7f, 0.7f, 0.7f, cloth[0], 0, 2.4f, 0);
        core.Box(0.56f, 0.5f, 0.05f, T.net, 0, 2.42f, -0.36f);
        core.Box(0.8f, 0.14f, 0.8f, cloth[1], 0, 2.82f, 0);
        core.Box(0.18f, 0.6f, 0.7f, cloth[2], 0, 3.2f, 0);
        for (int i = 0; i < 5; i++) core.Ball(0.12f, 0.12f, 0.12f, T.cowrie, -0.24f + i * 0.12f, 2.7f, -0.4f);
        core.Make("core", e.body);
        Shape[] strip = new Shape[cloth.Length];
        for (int i = 0; i < strip.Length; i++) strip[i] = new Shape().Box(0.27f, 1.0f, 0.05f, cloth[i], 0, -0.5f, 0);
        for (int ring = 0; ring < 3; ring++) {
            int n = 12; float r = 0.5f - ring * 0.06f, top = 1.0f + ring * 0.72f;
            for (int k = 0; k < n; k++) {
                float a = k / (float)n * PI * 2 + ring * 0.25f;
                Transform piv = strip[(k + ring * 2) % strip.Length].Make("strip", e.body).transform;
                piv.localPosition = V.P(Mathf.Sin(a) * r, top, Mathf.Cos(a) * r);
                e.strips.Add(piv); e.angle.Add(a);
            }
        }
        e.root.localScale = Vector3.one * 0.9f;
        return e;
    }
    static Quaternion YawPitch(float ry, float rx) {
        Quaternion q = Quaternion.AngleAxis(ry * Mathf.Rad2Deg, Vector3.up) * Quaternion.AngleAxis(rx * Mathf.Rad2Deg, Vector3.right);
        return new Quaternion(-q.x, -q.y, q.z, q.w);
    }
    void AnimateEgungun(Egungun e, float t, bool run) {
        // every few seconds the masquerade whirls right round, and the cloth flies out as it does
        float turn = Mathf.Clamp01(Mathf.Repeat(t, 4.2f) / 0.8f), flare = Mathf.Sin(turn * PI);
        float whirl = run ? Mathf.SmoothStep(0, 1, turn) * PI * 2 : 0;
        e.body.localRotation = V.R(0, Mathf.Sin(t * 2.2f) * 0.5f + whirl, Mathf.Sin(t * 4.5f) * (run ? 0.14f : 0.04f));
        e.body.localPosition = new Vector3(0, run ? Mathf.Abs(Mathf.Sin(t * 9)) * 0.18f : Mathf.Sin(t * 1.7f) * 0.03f, 0);
        float lift = (run ? 0.22f : 0.06f) + (run ? flare * 0.3f : 0);
        for (int i = 0; i < e.strips.Count; i++) e.strips[i].localRotation = YawPitch(e.angle[i], -(0.12f + lift * (1 + Mathf.Sin(t * 10 + i * 0.7f))));
    }

    void SetCharacter(string id) {
        CharDef c = CHARS[0];
        foreach (CharDef k in CHARS) if (k.id == id) c = k;
        Runner r = People.Spawn(c.look, "player");
        if (player != null) { r.root.position = player.root.position; Destroy(player.root.gameObject); }
        player = r; charId = c.id;
        PlayerPrefs.SetString("japa.char", c.id); PlayerPrefs.Save();
    }
    CharDef CurrentChar() { foreach (CharDef k in CHARS) if (k.id == charId) return k; return CHARS[0]; }

    Piece MakePiece(float startDist) {
        Piece prev = pieces.Count > 0 ? pieces[pieces.Count - 1] : null;
        bool first = prev == null;
        Piece p = new Piece();
        p.O = first ? Vector3.zero : prev.C;
        p.heading = first ? 0 : prev.heading + prev.turn;
        p.theta = p.heading * PI / 2;
        p.L = first ? 95 : Mathf.Round(Rand(50, 90) * Mathf.Lerp(1.12f, 0.78f, Tough(prev.d0 + prev.L) / 2));   // corners come sooner later on
        p.turn = p.heading == 0 ? Pick(-1, 1) : -p.heading;
        p.d0 = first ? startDist : prev.d0 + prev.L;
        p.dir = Dir(p.theta); p.right = Right(p.theta);
        p.C = p.O + p.dir * p.L;
        p.zone = ZoneKeyAt(p.d0 + 15);
        p.idx = first ? 0 : prev.idx + 1;
        p.go = new GameObject("piece");
        p.go.transform.position = V.P(p.O.x, 0, p.O.z);
        p.go.transform.rotation = V.R(0, p.theta, 0);
        Shape g = new Shape();
        bool origin = first && startDist == 0;   // the very start of a run has the shrine and a clear run-up
        Populate(p, origin, g);
        BuildFloor(p, origin, g);
        Decorate(p, origin, g);
        g.Fill(p.go, p.owned);
        foreach (Coin c in p.coins) { c.obj = P.cowrie.Make("cowrie", p.go.transform); c.obj.transform.localPosition = V.P(c.x, c.y, -c.s); }
        foreach (Pow w in p.powers) { w.obj = powerShapes[w.kind].Make("power", p.go.transform); w.obj.transform.localPosition = V.P(w.x, 1.15f, -w.s); billboards.Add(w.obj.transform); }
        foreach (Vector3 f in p.firePos) { Transform fl = P.flames.Make("flames", p.go.transform).transform; fl.localPosition = V.P(f.x, f.y, f.z); p.fires.Add(fl); }
        pieces.Add(p);
        return p;
    }

    void Populate(Piece p, bool first, Shape g) {
        float sAt = first ? 34 : 18, endS = p.L - 18;
        while (sAt < endS) {
            float d = p.d0 + sAt, tough = Tough(d), t = Mathf.Min(1, tough);
            string zk = ZoneKeyAt(d);
            string[] keys = { "root", "arch", "gap", "idol", "fire", "empty" };
            float[] w = { 1, d > 60 ? 0.9f : 0, d > 120 ? 0.55f + (zk == "river" ? 0.8f : 0) : 0, 1.3f, zk == "river" ? 0.2f : 0.8f, Mathf.Lerp(0.9f, 0.2f, t) };
            float tot = 0; foreach (float v in w) tot += v;
            float r = Random.value * tot; string type = "empty";
            for (int i = 0; i < keys.Length; i++) { r -= w[i]; if (r <= 0) { type = keys[i]; break; } }
            int coinLane = Random.Range(0, 3); string coinStyle = "line";
            if (type == "root") { AddObstacle(p, g, zk == "airport" ? "bump" : "root", sAt, -HALF, HALF, 0, zk == "airport" ? 0.55f : 0.78f, -1); coinStyle = "arc"; }
            else if (type == "arch") { AddObstacle(p, g, zk == "airport" ? "gate" : zk == "grove" || zk == "village" ? "fallen" : "arch", sAt, -HALF, HALF, 1.35f, 3.4f, -1); coinStyle = "low"; }
            else if (type == "gap") { p.gaps.Add(new[] { sAt - 1.9f, sAt + 1.9f }); coinStyle = "arc"; }
            else if (type == "idol" || type == "fire") {
                int[] lanes = { 0, 1, 2 };
                for (int i = 2; i > 0; i--) { int j = Random.Range(0, i + 1); int tmp = lanes[i]; lanes[i] = lanes[j]; lanes[j] = tmp; }
                int n = Random.value < 0.3f + tough * 0.25f ? 2 : 1;
                for (int i = 0; i < n; i++) {
                    bool flame = type == "fire" && i == 0;
                    float lx = LANES[lanes[i]];
                    if (flame) AddObstacle(p, g, zk == "airport" ? "cone" : "fire", sAt, lx - 0.85f, lx + 0.85f, 0, 0.9f, lanes[i]);
                    else AddObstacle(p, g, zk == "airport" ? "bags" : "idol", sAt, lx - 0.8f, lx + 0.8f, 0, 3.4f, lanes[i]);
                }
                coinLane = lanes[n];
                if (n == 1 && type == "fire" && Random.value < 0.5f) { coinLane = lanes[0]; coinStyle = "arc"; }
            }
            // cowries
            if (coinStyle == "arc") for (int k = 0; k < 7; k++) { float f = k / 6f; AddCowrie(p, sAt - 4 + f * 8, LANES[coinLane], 0.8f + Mathf.Sin(f * PI) * 1.7f); }
            else if (coinStyle == "low") for (int k = 0; k < 5; k++) AddCowrie(p, sAt - 4 + k * 2, LANES[coinLane], 0.5f);
            else for (int k = 0; k < 6; k++) AddCowrie(p, sAt - 9 + k * 2.2f, LANES[coinLane], 0.8f);
            // power-ups now and then, in a clear lane before the obstacle
            if (d > nextPower && type != "gap") {
                var free = new List<int>();
                for (int l = 0; l < 3; l++) { bool taken = false; foreach (Ob o in p.obstacles) if (Mathf.Abs(o.s - sAt) < 1 && o.lane == l) taken = true; if (!taken) free.Add(l); }
                int pl = free.Count > 0 ? free[Random.Range(0, free.Count)] : coinLane;
                p.powers.Add(new Pow { kind = Pick(POWER_KEYS), s = sAt - 10, x = LANES[pl] });
                nextPower = d + Rand(380, 560) * (1 + tough * 0.35f);
            }
            sAt += SpeedAt(d) * Rand(1.15f, 1.75f) * (tough < 1 ? Mathf.Lerp(1.2f, 0.85f, tough) : Mathf.Lerp(0.85f, 0.72f, tough - 1));
        }
    }
    void AddObstacle(Piece p, Shape g, string type, float sAt, float x0, float x1, float y0, float y1, int ln) {
        Shape sh; float ry = 0;
        switch (type) {
            case "root": sh = P.root; break;
            case "bump": sh = P.bump; break;
            case "arch": sh = P.arch; break;
            case "fallen": sh = P.fallenTree; break;
            case "gate": sh = P.gate; break;
            case "idol": sh = Pick(P.idol); ry = PI + Rand(-0.2f, 0.2f); break;   // faces the runner
            case "bags": sh = P.bags; break;
            case "fire": sh = P.fireRing; p.firePos.Add(new Vector3((x0 + x1) / 2, 0, -sAt)); break;
            default: sh = P.cone; break;
        }
        g.Add(sh, V.M(ln < 0 ? 0 : (x0 + x1) / 2, 0, -sAt, 0, ry, 0));
        float depth = type == "idol" || type == "bags" || type == "fire" || type == "cone" ? 1.2f : type == "fallen" ? 1.3f : 0.8f;
        p.obstacles.Add(new Ob { type = type, s = sAt, s0 = sAt - depth / 2, s1 = sAt + depth / 2, x0 = x0, x1 = x1, y0 = y0, y1 = y1, lane = ln });
    }
    void AddCowrie(Piece p, float sAt, float cx, float cy) {
        foreach (float[] gp in p.gaps) if (sAt > gp[0] - 0.5f && sAt < gp[1] + 0.5f && cy < 1.2f) cy = 1.6f;
        p.coins.Add(new Coin { s = sAt, x = cx, y = cy });
    }

    void BuildFloor(Piece p, bool first, Shape g) {
        float start = first ? -26 : HALF, end = p.L + HALF;
        var cuts = new List<float> { start };
        var sorted = new List<float[]>(p.gaps); sorted.Sort((a, b) => a[0].CompareTo(b[0]));
        foreach (float[] gp in sorted) { cuts.Add(gp[0]); cuts.Add(gp[1]); }
        cuts.Add(end);
        Material top = T.floor[p.zone], sideM = T.side[p.zone];
        int turnSide = -p.turn;   // +1 = right side (local +x)
        for (int i = 0; i < cuts.Count; i += 2) {
            float a = cuts[i], b = cuts[i + 1], len = b - a; if (len <= 0.05f) continue;
            Vector2 tu = p.zone == "airport" ? new Vector2(1, len / PATH_W) : new Vector2(PATH_W / 4, len / 4), su = new Vector2(len / 3, 1), eu = new Vector2(PATH_W / 3, 1);
            g.Box6(PATH_W, 3, len, new[] { sideM, sideM, top, sideM, sideM, sideM }, 0, -1.5f, -(a + b) / 2, new[] { su, su, tu, tu, eu, eu });
            // kerbs, broken where the next piece leaves the corner
            foreach (int sd in new[] { -1, 1 }) {
                float kb = sd == turnSide ? Mathf.Min(b, p.L - HALF) : b;
                if (kb - a > 0.2f) g.BoxTiled(0.25f, 0.22f, kb - a, T.kerb[p.zone], sd * (HALF - 0.12f), 0.11f, -(a + kb) / 2, 1.5f);
            }
            if (p.zone == "river") {   // rope rails
                for (float sp = Mathf.Ceil(a / 4) * 4; sp < b; sp += 4) foreach (int sd in new[] { -1, 1 }) {
                    if (sd == turnSide && sp > p.L - HALF - 1) continue;
                    g.Add(P.ropePost, V.M(sd * (HALF - 0.1f), 0, -sp));
                }
                foreach (int sd in new[] { -1, 1 }) {
                    float rb = sd == turnSide ? Mathf.Min(b, p.L - HALF) : b;
                    if (rb - a < 0.5f) continue;
                    g.Box(0.06f, 0.06f, rb - a, T.rope, sd * (HALF - 0.1f), 1.15f, -(a + rb) / 2);
                    g.Box(0.05f, 0.05f, rb - a, T.rope, sd * (HALF - 0.1f), 0.7f, -(a + rb) / 2);
                }
            }
        }
        // front kerb at the corner and a sign pointing the way
        g.BoxTiled(PATH_W, 0.4f, 0.3f, T.kerb[p.zone], 0, 0.2f, -(p.L + HALF - 0.15f), 1.5f);
        Material signMat = p.zone == "airport" ? T.roadSign[p.turn > 0 ? 0 : 1] : T.arrowSign[p.turn > 0 ? 0 : 1];
        g.Cyl(0.1f, 0.12f, 2.6f, 8, T.barkLight, 0, 1.3f, -(p.L + HALF + 0.4f));
        g.Quad(2.4f, 1.2f, signMat, V.M(0, 2.5f, -(p.L + HALF + 0.28f)));
        // supports under the causeway
        for (float sp = first ? -20 : 8; sp < p.L; sp += 16) {
            bool overGap = false; foreach (float[] gp in p.gaps) if (sp > gp[0] - 2 && sp < gp[1] + 2) overGap = true;
            if (overGap) continue;
            if (p.zone == "river") foreach (float px in new[] { -2f, 2f }) g.Add(P.stilt, V.M(px, 0, -sp));
            else g.BoxTiled(3.6f, 13, 3.6f, T.pillar[p.zone], 0, -9.5f, -sp);
        }
        g.BoxTiled(3.6f, 13, 3.6f, T.pillar[p.zone], 0, -9.5f, -p.L);
    }

    void Decorate(Piece p, bool first, Shape g) {
        int turnSide = -p.turn;
        foreach (int sd in new[] { -1, 1 }) {
            float sEnd = sd == turnSide ? p.L - HALF - 8 : p.L + 6;
            for (float sp = first ? -24 : 9; sp < sEnd; sp += Rand(7, 12)) {
                float px = sd * Rand(7.5f, 15), face = sd > 0 ? PI / 2 : -PI / 2, rot = Rand(0, 6.28f);
                Shape obj;
                if (p.zone == "grove") {
                    float r = Random.value;
                    if (r < 0.2f) { obj = P.head; rot = face; px = sd * Rand(6.5f, 9); }
                    else { obj = Pick(P.trees); px = sd * Rand(11, 19); if (Random.value < 0.5f) g.Add(P.banana, V.M(sd * Rand(4.4f, 6), -0.6f, -sp - Rand(-2, 2), 0, Rand(0, 6), 0)); }
                } else if (p.zone == "ruins") {
                    float r = Random.value;
                    if (r < 0.3f) obj = P.pillar; else if (r < 0.52f) { obj = P.ruin; px = sd * Rand(8.5f, 13); } else if (r < 0.78f) obj = P.wall; else { obj = Pick(P.trees); px = sd * Rand(10, 17); }
                } else if (p.zone == "river") {
                    if (Random.value < 0.18f) g.Add(P.canoe, V.M(sd * Rand(10, 30), 0, -sp, 0, Rand(0, 3), 0));
                    continue;
                } else if (p.zone == "village") {
                    if (Random.value < 0.4f) { obj = P.hut; rot = face; px = sd * Rand(8.5f, 13); } else obj = P.palmVillage;
                } else {
                    float r = Random.value;
                    if (r < 0.3f) { g.Add(P.lamp, V.M(sd * (HALF + 0.4f), 0, -sp, 0, sd > 0 ? 0 : PI, 0)); continue; }
                    if (r < 0.48f) { obj = Pick(P.billboards); rot = -sd * 0.5f; } else obj = P.palm;
                }
                g.Add(obj, V.M(px, 0, -sp, 0, rot, 0));
            }
        }
        // a wall of trees past the corner, so the turn reads clearly
        for (int i = 0; i < 3; i++) {
            if (p.zone == "river") break;
            Shape o = p.zone == "airport" ? P.palm : p.zone == "village" ? P.palmVillage : Pick(P.trees);
            g.Add(o, V.M(Rand(-7, 7), 0, -(p.L + HALF + Rand(8, 14)), 0, Rand(0, 6), 0));
        }
        if (first) g.Add(P.shrine, V.M(0, 0, 16));
    }

    void RemovePiece(Piece p) {
        foreach (Pow w in p.powers) billboards.Remove(w.obj.transform);
        foreach (Mesh m in p.owned) Destroy(m);
        Destroy(p.go);
    }
    bool FloorAt(Piece p, float sAt, float px) {
        if (Mathf.Abs(px) > HALF + 0.15f) return false;
        float start = p.idx == 0 ? -26 : -HALF;
        if (sAt < start || sAt > p.L + HALF) return false;
        if (auto) return true;
        foreach (float[] gp in p.gaps) if (sAt > gp[0] + 0.25f && sAt < gp[1] - 0.25f) return false;
        return true;
    }
    Vector3 WorldPos(Piece p, float sAt, float px, float py) { Vector3 v = p.O + p.dir * sAt + p.right * px; v.y = py; return v; }

    void ResetWorld(float startDist) {
        foreach (Piece p in pieces) RemovePiece(p);
        pieces.Clear(); history.Clear();
        pi = 0; s = 0; x = 0; lane = 1; prevLane = 1; y = 0; vy = 0; grounded = true; slide = 0; queued = 0; dist = startDist; score = 0; coins = 0;
        speed = START_SPEED; invuln = 0; chase = 0; chaseGap = 3.5f; showT = 0; caught = false; dyingT = 0; reason = ""; fall = false; hintCount = startDist > 0 ? 2 : 0;
        camYaw = 0; yaw = 0; tumble = 0; camSet = false; nextPower = startDist + 300;
        foreach (string k in POWER_KEYS) power[k] = 0;
        MakePiece(startDist);
        while (pieces[pieces.Count - 1].d0 < startDist + LOOKAHEAD) MakePiece(startDist);
        zone = ZoneIndexAt(startDist);
        ApplyZone(zone, true);
        player.ResetPose();
        player.root.position = Vector3.zero;
    }

    // ---------- zone atmosphere ----------
    void ApplyZone(int i, bool instant) {
        atmosTarget = i;
        if (instant) { Zone z = ZONES[i]; bgNow = T.H(z.bg); fogNear = z.fogNear; fogFar = z.fogFar; groundNow = T.H(z.ground); PushAtmos(); }
        ShowArea(ZONES[i].name);
    }
    void StepAtmos(float dt) {
        Zone z = ZONES[atmosTarget]; float k = Mathf.Min(1, dt * 1.2f);
        bgNow = Color.Lerp(bgNow, T.H(z.bg), k); fogNear = Mathf.Lerp(fogNear, z.fogNear, k); fogFar = Mathf.Lerp(fogFar, z.fogFar, k);
        groundNow = Color.Lerp(groundNow, T.H(z.ground), k);
        PushAtmos();
    }
    void PushAtmos() {
        cam.backgroundColor = bgNow;
        Shader.SetGlobalColor("_LRFogColor", bgNow);
        // haze sits a little further off than in the web version so the scenery reads
        Shader.SetGlobalVector("_LRFogParams", new Vector4(fogNear * 1.3f, fogFar * 1.25f, 0, 0));
        T.sky.SetColor("_Color", bgNow * 1.05f);
        Color a = T.H(0x8d713a);
        T.ground.SetColor("_Color", new Color(Mathf.Min(2, groundNow.r / a.r), Mathf.Min(2, groundNow.g / a.g), Mathf.Min(2, groundNow.b / a.b), 1));
    }

    // ---------- flow ----------
    void StartRun() { StartRunAt(0); }
    void StartRunAt(float startDist) {
        ResetWorld(startDist);
        mode = "play"; showT = 5;
        ShowScreen(null);
        sound.Music(true); sound.Growl();
        Toast(CurrentChar().line, 2.4f, "The Egungun don wake. Japa!");
    }
    void SetPaused(bool p) {
        if (p && mode == "play") { mode = "paused"; ShowScreen(pauseScreen); sound.Music(false); }
        else if (!p && mode == "paused") { mode = "play"; ShowScreen(null); sound.Music(true); }
    }
    void EndRunFromPause() {
        if (mode != "paused") return;
        pauseScreen.gameObject.SetActive(false);
        mode = "dying"; dyingT = 2; reason = "You rest small. Run ended."; caught = fall = false;
    }
    void Die(string why, bool wasCaught = false, bool fell = false) {
        if (mode != "play") return;
        mode = "dying"; dyingT = 0; reason = why; caught = wasCaught; fall = fell;
        sound.Music(false);
        if (fall) sound.Fall(); else sound.Crash();
        if (caught) foreach (Piece q in pieces) {   // clear the pickups so nothing floats through the scene
            foreach (Coin c in q.coins) Destroy(c.obj);
            foreach (Pow w in q.powers) { billboards.Remove(w.obj.transform); Destroy(w.obj); }
            q.coins.Clear(); q.powers.Clear();
        }
        if (caught) sound.Growl();
    }
    void ShowOver() {
        mode = "over";
        int final = Mathf.FloorToInt(score);
        bool isBest = final > best;
        if (isBest) { best = final; PlayerPrefs.SetInt("japa.best", best); PlayerPrefs.Save(); }
        PaintOver(final, isBest);
    }
    void ToMenu() {
        ResetWorld(0); mode = "menu";
        ShowScreen(startScreen); RenderPicks();
    }
    void PickChar(string id) { SetCharacter(id); RenderPicks(); }

    // ---------- input ----------
    void Act(string dir) {
        if (mode != "play") return;
        Piece p = pieces[pi];
        if (dir == "left" || dir == "right") {
            int tdir = dir == "left" ? 1 : -1;
            // near a corner, left/right means "turn here"
            if (p.L - s < 16 && tdir == p.turn) { queued = tdir; return; }
            int nl = Mathf.Clamp(lane + (dir == "left" ? -1 : 1), 0, 2);
            if (nl != lane) { prevLane = lane; lane = nl; }
        } else if (dir == "up") {
            if (grounded) { vy = JUMP_V * (power["jump"] > 0 ? 1.3f : 1); grounded = false; slide = 0; sound.Jump(); }
        } else if (dir == "down") {
            if (!grounded) vy = Mathf.Min(vy, -22);
            slide = 0.75f; sound.Slide();
        }
    }
    Vector2 touchStart, mouseStart; bool touchLive, touchDone, mouseLive;
    void Swipe(Vector2 d) { Act(Mathf.Abs(d.x) > Mathf.Abs(d.y) ? (d.x < 0 ? "left" : "right") : (d.y > 0 ? "up" : "down")); }
    void HandleInput() {
        if (mode == "play") {
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) Act("left");
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) Act("right");
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space)) Act("up");
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) Act("down");
        } else if ((mode == "menu" || mode == "over") && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))) { StartRun(); return; }
        if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape)) { if (mode == "play") SetPaused(true); else if (mode == "paused") SetPaused(false); }
        if (Input.GetKeyDown(KeyCode.M)) ToggleMute();

        float thr = 28 * uiScale;
        if (Input.touchCount > 0) {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began) { touchStart = t.position; touchLive = true; touchDone = false; }
            else if (touchLive && !touchDone && (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Ended)) {
                Vector2 d = t.position - touchStart;
                if (Mathf.Max(Mathf.Abs(d.x), Mathf.Abs(d.y)) > thr) { touchDone = true; Swipe(d); }
            }
            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) touchLive = false;
        } else {
            if (Input.GetMouseButtonDown(0)) { mouseStart = Input.mousePosition; mouseLive = true; }
            if (mouseLive && Input.GetMouseButtonUp(0)) {
                mouseLive = false;
                Vector2 d = (Vector2)Input.mousePosition - mouseStart;
                if (Mathf.Max(Mathf.Abs(d.x), Mathf.Abs(d.y)) > thr) Swipe(d);
            }
        }
    }
    void OnApplicationFocus(bool focus) { if (!focus && !shotMode) SetPaused(true); }
    void OnApplicationPause(bool paused) { if (paused && !shotMode) SetPaused(true); }

    void Resize() {
        lastW = Screen.width; lastH = Screen.height;
        bool portrait = (float)lastW / lastH < 0.85f;
        cam.fieldOfView = portrait ? 76 : 64;
        camBack = portrait ? 8.6f : 7; camUp = portrait ? 4.8f : 4;
        float h = 2 * 390 * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        skyQuad.localScale = new Vector3(h * lastW / lastH * 1.02f, h * 1.02f, 1);
        LayoutUI();
    }

    // ---------- update ----------
    void Update() {
        if (mode == "boot") return;
        if (Screen.width != lastW || Screen.height != lastH) Resize();
        HandleInput();
        float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
        Step(dt);
        UpdatePlane(dt);
        TickUI(dt);
    }
    void LateUpdate() {
        if (cam == null) return;
        foreach (Transform b in billboards) if (b != null) b.rotation = cam.transform.rotation;
    }
    void Aim(Vector3 from, Vector3 at) { cam.transform.position = V.P(from.x, from.y, from.z); cam.transform.LookAt(V.P(at.x, at.y, at.z)); }
    void MoveGround(Vector3 at) {
        groundQuad.position = V.P(at.x, GROUND_Y, at.z);
        T.ground.mainTextureOffset = new Vector2(groundQuad.position.x / 10f, groundQuad.position.z / 10f);   // the quad follows the runner; keep its texture still
    }
    Trail SampleHistory(float d) {
        if (history.Count == 0) return null;
        if (d <= history[0].d) return history[0];
        for (int i = history.Count - 1; i > 0; i--) if (history[i - 1].d <= d) {
            Trail a = history[i - 1], b = history[i]; float f = (d - a.d) / Mathf.Max(1e-6f, b.d - a.d);
            return new Trail { d = d, p = Vector3.Lerp(a.p, b.p, f), th = f < 0.5f ? a.th : b.th };
        }
        return history[history.Count - 1];
    }

    void Step(float dt) {
        time += dt;
        bool playing = mode == "play", dying = mode == "dying";
        foreach (Piece q in pieces) for (int i = 0; i < q.fires.Count; i++) {
            q.fires[i].localScale = new Vector3(1, 1 + Mathf.Sin(time * 18 + i) * 0.15f, 1);
            q.fires[i].Rotate(0, dt * 115, 0, Space.Self);
        }

        if (mode == "menu") {
            // the runner stands outside the shrine; masquerades wait in the doorway
            player.Idle(time, dt);
            player.rootE = Vector3.zero; player.root.position = Vector3.zero; player.Apply();
            for (int i = 0; i < masks.Count; i++) {
                masks[i].pos = new Vector3(-1.6f + i * 1.6f, 0, 11.4f + (i == 1 ? 0.6f : 0));
                masks[i].root.position = V.P(masks[i].pos.x, 0, masks[i].pos.z); masks[i].root.rotation = V.R(0, PI, 0);
                AnimateEgungun(masks[i], time + i, false);
            }
            bool portrait = cam.aspect < 0.85f;
            // desktop: panel on the left, so frame the scene to the right; phone: panel at the bottom, so aim low
            Aim(new Vector3(Mathf.Sin(time * 0.25f) * 1.2f + (portrait ? 0 : -0.6f), portrait ? 3.4f : 2.4f, portrait ? -9.5f : -7.5f), new Vector3(portrait ? 0 : 3.2f, portrait ? -2.6f : 1.8f, 6));
            foreach (Piece q in pieces) foreach (Coin c in q.coins) c.obj.transform.Rotate(0, -dt * 170, 0, Space.Self);
            MoveGround(Vector3.zero);
            StepAtmos(dt);
            return;
        }
        if (!playing && !dying) return;

        if (dying) {
            dyingT += dt;
            speed = fall ? speed * Mathf.Pow(0.4f, dt) : Mathf.Max(0, speed - 50 * dt);
            if (dyingT > (caught ? 4.2f : fall ? 2.0f : 1.7f)) { ShowOver(); return; }
        } else speed = SpeedAt(dist);

        Piece p = pieces[pi];
        float ds = speed * dt;
        if (playing || fall) { s += ds; dist += ds; }
        if (playing) score += ds;
        if (auto && playing) { if (p.L - s < 14) queued = p.turn; invuln = 1; }

        // corners
        if (playing && s >= p.L && queued == p.turn && y > -0.6f) {
            Piece next = pieces[pi + 1];
            Vector3 rel = WorldPos(p, s, x, 0) - next.O;
            pi++; p = next;
            s = Vector3.Dot(rel, next.dir); x = Vector3.Dot(rel, next.right);
            lane = 1; prevLane = 1; queued = 0;
            foreach (Ob o in p.obstacles) { o.ps = false; o.px = false; }
            sound.Turn();
        }
        // tutorial hint for the first two corners
        if (playing && hintCount < 2 && p.L - s < 26 && p.L - s > 20 && queued == 0) {
            hintCount++; Toast(p.turn > 0 ? "Turn left!" : "Turn right!", 1.3f, "Swipe or press the arrow at the corner");
        }
        // keep the path ahead built and the path behind cleared
        while (pieces[pieces.Count - 1].d0 < dist + LOOKAHEAD) MakePiece(0);
        while (pieces.Count > 0 && pieces[0].idx < pieces[pi].idx - 1) { RemovePiece(pieces[0]); pieces.RemoveAt(0); pi--; }

        // timers
        foreach (string k in POWER_KEYS) if (power[k] > 0) power[k] = Mathf.Max(0, power[k] - dt);
        invuln = Mathf.Max(0, invuln - dt); chase = Mathf.Max(0, chase - dt); showT = Mathf.Max(0, showT - dt);

        // lateral
        if (playing) x += (LANES[lane] - x) * Mathf.Min(1, dt * 14);

        // vertical
        float yPrev = y;
        vy += GRAVITY * dt; y += vy * dt;
        if (slide > 0) slide -= dt;
        bool onFloor = FloorAt(p, s, x);
        if (onFloor && !(dying && fall) && y <= 0 && yPrev >= -0.35f) { y = 0; if (vy < 0) vy = 0; grounded = true; }
        else grounded = false;
        if (playing && !onFloor && y < -0.8f) {
            bool missedTurn = s > p.L + HALF - 0.4f;
            Die(missedTurn ? Pick("You no turn! Bush don swallow you.", "Corner don pass you by!") : Pick("You fall inside pit!", "The ground open mouth swallow you!"), false, true);
            dying = true; playing = false;
        }

        // collisions
        if (playing) {
            float h = slide > 0 ? P_H_SLIDE : P_H;
            foreach (Ob o in p.obstacles) {
                bool os = Mathf.Abs(s - (o.s0 + o.s1) / 2) < (o.s1 - o.s0 + P_D) / 2;
                bool ox = x + P_W / 2 > o.x0 && x - P_W / 2 < o.x1;
                bool oy = y < o.y1 - 0.02f && y + h > o.y0;
                if (os && ox && oy && invuln <= 0) {
                    if (o.ps && !o.px) {
                        lane = prevLane; invuln = 0.5f;
                        if (chase > 0) { Die("The Egungun don catch you!", true); break; }
                        chase = 7 + Tough(dist) * 2; sound.Bump(); sound.Growl(); player.Flinch();   // further on they stay on you longer
                        Toast("Egungun dey come!", 1.7f, Pick("Who carry our calabash?!", "You no fit run pass us!", "Bring am back!"));
                    } else { Die(CRASH[o.type]); break; }
                }
                o.ps = os; o.px = ox;
            }
            dying = mode == "dying"; playing = mode == "play";
        }

        // cowries and power-ups (this piece and the next, since corners overlap)
        for (int qi = pi; qi <= pi + 1 && qi < pieces.Count; qi++) {
            Piece q = pieces[qi]; bool same = qi == pi;
            for (int i = q.coins.Count - 1; i >= 0; i--) {
                Coin c = q.coins[i];
                c.obj.transform.Rotate(0, -dt * 230, 0, Space.Self);
                if (!same || !playing) continue;
                if (power["magnet"] > 0 && c.s - s < 16 && c.s - s > -2) {
                    float k = Mathf.Min(1, dt * 7); c.s += (s - c.s) * k; c.x += (x - c.x) * k; c.y += (y + 1 - c.y) * k;
                    c.obj.transform.localPosition = V.P(c.x, c.y, -c.s);
                }
                if (Mathf.Abs(c.s - s) < 0.85f && Mathf.Abs(c.x - x) < 0.9f && Mathf.Abs(c.y - (y + 0.8f)) < 1.25f) {
                    int v = power["double"] > 0 ? 2 : 1;
                    coins += v; score += 5 * v; sound.Coin(); Destroy(c.obj); q.coins.RemoveAt(i);
                    if (coins % 100 == 0) Toast(Pick("Cowrie plenty!", "You don rich!", "Money for japa!"), 1.4f, null);
                }
            }
            for (int i = q.powers.Count - 1; i >= 0; i--) {
                Pow w = q.powers[i];
                w.obj.transform.localPosition = V.P(w.x, 1.15f + Mathf.Sin(time * 4 + i) * 0.15f, -w.s);
                if (same && playing && Mathf.Abs(w.s - s) < 1 && Mathf.Abs(w.x - x) < 1 && y < 2.4f) {
                    power[w.kind] = POWER_INFO[w.kind].Value; sound.Power(); Toast(POWER_INFO[w.kind].Key + "!", 1.4f, null);
                    billboards.Remove(w.obj.transform); Destroy(w.obj); q.powers.RemoveAt(i);
                }
            }
        }

        // zone
        int zi = ZoneIndexAt(dist);
        if (zi != zone) { zone = zi; ApplyZone(zi, false); if (playing) Toast(ZONES[zi].name, 2.0f, zi == 4 ? "Airport don near. Japa!" : "The Egungun still dey follow!"); }
        StepAtmos(dt);

        // player
        Vector3 P0 = WorldPos(p, s, x, y);
        float before = phase;
        phase += dt * Mathf.Max(speed, 4) * 0.55f;
        string pose = !grounded ? "air" : slide > 0 ? "slide" : "run";
        if (playing && pose == "run" && Mathf.Floor(phase / PI) != Mathf.Floor(before / PI)) sound.Step();
        yaw = AngleLerp(yaw, p.theta, Mathf.Min(1, dt * 16));
        player.Animate(phase, pose, dt, speed > 23, vy);
        player.rootE = new Vector3(0, yaw, 0);
        player.Swerve((x - LANES[lane]) / 2.2f, dt);
        // eyes on the path ahead, so the head leads the body round a corner
        Vector3 ahead = P0 + p.dir * 30;
        player.look = V.P(ahead.x, P0.y + 1.5f, ahead.z); player.lookW = dying ? 0 : 1;
        if (dying && caught) {
            // the masquerades close in: stagger, then cower under their whirling cloth
            if (dyingT < 0.45f) player.Play("Hit_Chest", dyingT / 0.42f, dt); else player.Play("Defend", (dyingT - 0.45f) / 0.92f, dt);
        } else if (dying && fall) { tumble += dt * 3; player.rootE.x = tumble; player.Play("Jump_air", time / 3.1f, dt); }
        else if (dying) player.Play("Hit_Knockback", dyingT / 0.85f, dt);
        player.root.position = V.P(P0.x, P0.y, P0.z);
        player.Apply();

        // history for the chasers
        if (playing) {
            history.Add(new Trail { d = dist, p = WorldPos(p, s, x * 0.5f, 0), th = p.theta });
            while (history.Count > 2 && history[1].d < dist - 60) history.RemoveAt(0);
        }

        // Egungun: right behind at the start of a run and after a stumble, far behind otherwise
        bool caughtScene = dying && caught;
        float targetGap = caughtScene ? 0 : chase > 0 ? 2.4f : showT > 0 ? 3.0f : 18;
        chaseGap = Mathf.Lerp(chaseGap, targetGap, Mathf.Min(1, dt * (targetGap < chaseGap ? 2.5f : 0.7f)));
        float[][] off = { new[] { -1.9f, 0f }, new[] { 1.9f, 0.4f }, new[] { 0f, 8.5f } };   // two flank you; the third trails behind the camera
        for (int i = 0; i < masks.Count; i++) {
            Egungun m = masks[i];
            if (caughtScene) {
                float a = time * 1.6f + i * (PI * 2 / 3);
                Vector3 want0 = new Vector3(P0.x + Mathf.Sin(a) * 2.1f, 0, P0.z + Mathf.Cos(a) * 2.1f);
                m.pos = Vector3.Lerp(m.pos, want0, Mathf.Min(1, dt * 4));
                m.root.position = V.P(m.pos.x, 0, m.pos.z); m.root.rotation = V.R(0, a + PI, 0);
                AnimateEgungun(m, time * 1.5f + i, true);
                continue;
            }
            Trail smp = SampleHistory(dist - chaseGap - off[i][1]);
            if (smp == null) continue;
            m.pos = smp.p + Right(smp.th) * off[i][0];
            m.root.position = V.P(m.pos.x, 0, m.pos.z); m.root.rotation = V.R(0, smp.th, 0);
            AnimateEgungun(m, time + i, true);
        }

        // camera: follows behind, swinging round at corners
        camYaw = AngleLerp(camYaw, p.theta, Mathf.Min(1, dt * 5));
        Vector3 back = new Vector3(Mathf.Sin(camYaw), 0, Mathf.Cos(camYaw));
        float camY = fall ? Mathf.Max(0, y) : Mathf.Max(0, y) * 0.5f;
        Vector3 flat = new Vector3(P0.x, 0, P0.z);
        Vector3 want = flat + back * camBack; want.y = camUp + camY;
        if (caughtScene) {
            float f = Mathf.Clamp01((dyingT - 0.2f) / 0.9f), e = f * f * (3 - 2 * f);
            Vector3 high = flat + Right(p.theta) * 4.2f + p.dir * 4.8f; high.y = 4.6f;
            want = Vector3.Lerp(want, high, e);
        }
        if (fall) { want = flat + back * camBack * 0.9f; want.y = camUp + 2; }   // hover at the edge and watch them drop
        if (!camSet) { camPos = want; camSet = true; }
        camPos = Vector3.Lerp(camPos, want, Mathf.Min(1, dt * 10));
        Vector3 shake = dying && !fall && !caught && dyingT < 0.3f ? new Vector3(Mathf.Sin(dyingT * 80) * 0.12f, 0, 0) : Vector3.zero;
        Vector3 look;
        if (dying && (caught || fall)) { look = P0; look.y = fall ? y + 0.8f : 0.9f; }
        else { look = flat + Dir(camYaw) * 9; look.y = 1.1f + camY; }
        lookPos = (lookPos - look).sqrMagnitude > 400 ? look : Vector3.Lerp(lookPos, look, Mathf.Min(1, dt * 10));
        Aim(camPos + shake, lookPos);
        MoveGround(P0);

        if (playing) PaintHud();
    }

    void UpdatePlane(float dt) {
        if (plane == null) return;
        bool show = zone == 4 && mode != "menu";
        if (plane.gameObject.activeSelf != show) plane.gameObject.SetActive(show);
        if (!show) return;
        planeT += dt;
        float t = planeT % 14; Vector3 at = player.root.position;
        plane.position = new Vector3(at.x - 60 + t * 9, 40 + t * 1.5f, at.z + 70);
        plane.rotation = Quaternion.Euler(-7, 90, 0);
    }

    // Development aid: `-lrshots <folder>` plays a scripted tour on autopilot and saves screenshots, then quits.
    IEnumerator Shots(string dir) {
        System.IO.Directory.CreateDirectory(dir);
        int n = 0;
        System.Func<string, float, IEnumerator> snap = (name, wait) => SnapAfter(dir + "/" + (++n).ToString("00") + "-" + name + ".png", wait);
        yield return snap("menu", 2.5f);
        PickChar("basirat"); yield return snap("menu-basirat", 1);
        PickChar("tunde");
        auto = true;
        StartRun(); yield return snap("start", 1.6f);
        yield return snap("grove", 2.5f);
        Act("up"); yield return snap("jump", 0.4f);
        yield return new WaitForSecondsRealtime(0.9f); Act("down"); yield return snap("slide", 0.35f);
        yield return snap("grove-later", 3.5f);
        StartRunAt(760); yield return snap("ruins", 4);
        StartRunAt(1460); yield return snap("river", 4);
        StartRunAt(2110); yield return snap("village", 4);
        StartRunAt(2810); yield return snap("airport", 5);
        power["magnet"] = 10; chase = 7; yield return snap("chase", 1.5f);
        Die("The Egungun don catch you!", true); yield return snap("caught", 2.2f);
        yield return snap("over", 2.6f);
        Application.Quit();
    }
    IEnumerator SnapAfter(string path, float wait) {
        yield return new WaitForSecondsRealtime(wait);
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(path);
        yield return null; yield return null;
    }
}
