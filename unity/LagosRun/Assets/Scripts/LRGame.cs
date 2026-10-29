using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Zone {
    public string name, crew; public int len, ground; public bool bridge;
    public float hMin, hMax, glass, stalls, palms, density, people = 1, footbridge;
    public float towers = 0.5f, stilts = 0.35f;   // on a bridge: how often a tower stands on the far shore, and a stilt house in the water
}
public class City { public string name, state, motto, intro; public Zone[] zones; }
// gang: which set of chasers comes after you (several crews share the same men under a local name)
public class Crew { public string label, caught, warn, gang; public string[] taunts; }
public class CharDef { public string id, group, name, role, line; public int price, swA, swB; public Person look; }

// Runtown (first made as Lagos Run): a three-lane endless runner through Lagos, Port Harcourt, Owerri and Warri. Positions and angles are kept in the web version's
// coordinates (x across the road, y up, z toward the camera) and converted with V when applied to transforms.
public partial class LRGame : MonoBehaviour {
    static readonly float[] LANE_X = { -2.2f, 0, 2.2f };
    const float GRAVITY = -30, JUMP_V = 12, START_SPEED = 14, MAX_SPEED = 35, SPAWN_AHEAD = 175, SEG_LEN = 20;
    const int NSEG = 12;
    const float P_W = 0.7f, P_D = 0.6f, P_H = 1.7f, P_H_SLIDE = 0.75f, PI = Mathf.PI;

    // Who chases you when you stumble depends on who controls the area.
    static readonly Dictionary<string, Crew> CREWS = new Dictionary<string, Crew> {
        { "agbero", new Crew { gang = "agbero", label = "Agberos", taunts = new[] { "Agbero: Where you dey go?!", "Owo da? Pay your levy!", "Oya drop something!", "You don enter our area!" }, caught = "The agberos don catch you!", warn = "Beware of agberos!" } },
        { "olopa", new Crew { gang = "olopa", label = "Olopa", taunts = new[] { "Olopa: Stop there!", "Oga, show your particulars!", "Na checkpoint! Stop!", "Where you dey run go?!" }, caught = "Olopa don arrest you!", warn = "Open your eyes, olopa dey there!" } },
        { "lastma", new Crew { gang = "lastma", label = "LASTMA", taunts = new[] { "LASTMA: You don commit offence!", "Who send you this lane?!", "Come and pay fine!" }, caught = "LASTMA don book you!", warn = "Shine your eye, LASTMA dey there!" } },
        { "taskforce", new Crew { gang = "taskforce", label = "Task Force", taunts = new[] { "Task Force: Park well!", "You don break law for here!", "Oya, enter the Hilux!", "Come and pay your fine!" }, caught = "Task Force don carry you go!", warn = "Shine your eye, Task Force dey there!" } },
        { "deve", new Crew { gang = "agbero", label = "Deve boys", taunts = new[] { "Deve boys: Where our deve?!", "You never settle us!", "Na our land be this!", "Area! Drop something!" }, caught = "Deve boys don hold you!", warn = "Beware of deve boys!" } },
    };
    // Playable characters. Prices are in naira from your purse (each coin = N10).
    static readonly CharDef[] CHARS = {
        new CharDef { id = "tunde", group = "Gents", name = "Tunde", role = "Super Eagles fan", price = 0, swA = 0x0a8a4a, swB = 0xffffff, line = "Oya, make we go!",
            look = new Person { model = "Tunde", dress = new[] { "suit", "#12a05a" } } },
        new CharDef { id = "konda", group = "Gents", name = "Sunday the Konda", role = "Danfo conductor", price = 2000, swA = 0xd9d4c4, swB = 0x1f4fa3, line = "Oshodi! Oshodi! Enter with your change!",
            look = new Person { model = "Dayo", hat = "cap", hatColor = 0x1f4fa3, prop = "cash" } },
        new CharDef { id = "okada", group = "Gents", name = "Baba Okada", role = "Okada rider", price = 5000, swA = 0xf08c1a, swB = 0xd22d2d, line = "Where you dey go? I fit carry you!",
            look = new Person { model = "Baba", dress = new[] { "shirt", "#f08c1a", "trouser", "x#555555", "short04", "hide" }, hat = "helmet", hatColor = 0xd22d2d } },
        new CharDef { id = "odogwu", group = "Gents", name = "Chief Odogwu", role = "Lekki big man", price = 10000, swA = 0xf2efe6, swB = 0x7b2d8f, line = "Spray money! Odogwu dey for road!",
            look = new Person { model = "Emeka", dress = new[] { "suit", "#f2efe6", "shoes", "x#8a5a30" }, hat = "fila", hatColor = 0x7b2d8f, shades = true } },
        new CharDef { id = "amaka", group = "Ladies", name = "Amaka", role = "UNILAG student", price = 1500, swA = 0xf5c518, swB = 0x2b4f7a, line = "I get 8am lecture! Comot for road!",
            look = new Person { model = "Amaka", dress = new[] { "tank", "#f5c518", "pants", "#2b4f7a" }, backpack = 0x16120c } },
        new CharDef { id = "basirat", group = "Ladies", name = "Iya Basirat", role = "Mile 12 market woman", price = 4000, swA = 0x1f4fa3, swB = 0xc9a227, line = "Fine customer, come buy!",
            look = new Person { model = "Zainab", dress = new[] { "suit", "blue", "afro", "hide" }, hat = "gele", hatColor = 0xc9a227 } },
        new CharDef { id = "tiwa", group = "Ladies", name = "Tiwa", role = "Lekki baddie", price = 8000, swA = 0xc2387a, swB = 0x16120c, line = "No time to check time!",
            look = new Person { model = "Ngozi", dress = new[] { "suit", "pink" }, shades = true, prop = "bag" } },
        new CharDef { id = "tobi", group = "Kids", name = "Tobi", role = "Primary school boy", price = 1000, swA = 0xf4f4f4, swB = 0x1f3a6b, line = "Mummy said make I run straight home!",
            look = new Person { model = "Tunde", scale = 0.72f, headScale = 1.22f, dress = new[] { "suit", "#e9edf2", "afro", "hide" }, backpack = 0xd22d2d } },
        new CharDef { id = "kemi", group = "Kids", name = "Kemi", role = "Primary school girl", price = 2500, swA = 0x2f5aa8, swB = 0xf4f4f4, line = "Last person to school na rotten egg!",
            look = new Person { model = "Zainab", scale = 0.72f, headScale = 1.22f, dress = new[] { "suit", "#2f5aa8" }, backpack = 0xf5c518 } },
    };
    // people on the pavement: traders, shoppers, office workers and school kids
    static readonly Person[] NPCS = {
        new Person { model = "Zainab", dress = new[] { "suit", "orange", "afro", "hide" }, hat = "bandana", hatColor = 0xe86a1c, tray = 0xf08c1a, pose = "tray" },
        new Person { model = "Ngozi", dress = new[] { "suit", "green", "short01", "hide" }, hat = "gele", hatColor = 0x2e9a5a, pose = "Idle_Talking" },
        new Person { model = "Ngozi", dress = new[] { "suit", "purple" }, pose = "Idle_TalkingPhone" },
        new Person { model = "Zainab", dress = new[] { "suit", "pink", "afro", "hide" }, hat = "gele", hatColor = 0xd64f8f, prop = "bag", pose = "Idle_FoldArms" },
        new Person { model = "Emeka", dress = new[] { "suit", "#3a6b8f" }, hat = "fila", hatColor = 0x2f5878, pose = "Idle_TalkingPhone" },
        new Person { model = "Tunde", dress = new[] { "suit", "x#c8d6e8" }, pose = "Consume" },
        new Person { model = "Dayo", hat = "cap", hatColor = 0x0a8a4a, tray = 0xd23c1e, pose = "tray" },
        new Person { model = "Baba", dress = new[] { "shirt", "x#e8dcc0" }, hat = "fila", hatColor = 0x7a2a12, pose = "Idle_Talking" },
        new Person { model = "Baba", dress = new[] { "shirt", "#2e7d5a" }, pose = "Yes" },
        new Person { model = "Dayo", dress = new[] { "camotee", "#f5c518" }, pose = "Dance_Simple" },
        new Person { model = "Ngozi", dress = new[] { "suit", "x#ffb0c8" }, pose = "Dance_Body_Roll" },
        new Person { model = "Emeka", pose = "Idle_FoldArms" },
        new Person { model = "Tunde", dress = new[] { "suit", "#c8452e" }, pose = "Cheering_Two_Hands" },
        new Person { model = "Tunde", scale = 0.72f, headScale = 1.22f, dress = new[] { "suit", "#e9edf2", "afro", "hide" }, backpack = 0x0a8a4a, pose = "Greeting" },
        new Person { model = "Zainab", scale = 0.72f, headScale = 1.22f, dress = new[] { "suit", "#2f5aa8" }, backpack = 0xd22d2d, pose = "Dance_Simple" },
        new Person { model = "Tunde", scale = 0.72f, headScale = 1.22f, dress = new[] { "suit", "#0a8a4a", "afro", "hide" }, pose = "Cheer_One_arm" },
        new Person { model = "Naija", pose = "Greeting" },
        new Person { model = "Naija", dress = new[] { "tankshirt", "#f4f4f4" }, pose = "Idle_A" },
    };
    // The towns you can run. Each is a route through its own areas, then round again; areas get longer the further you go.
    public static readonly City[] CITIES = {
        new City { name = "Lagos", state = "Lagos State", motto = "Centre of Excellence",
            intro = "Run across Lagos from Ikorodu to Epe. Dodge danfos, jump the potholes and grab every naira. Stumble twice and whoever controls the area go catch you: agberos, olopa or LASTMA.",
            zones = new[] {
            new Zone { name = "Ikorodu", len = 550, crew = "agbero", hMin = 4, hMax = 9, glass = 0, stalls = 0.8f, palms = 0.35f, density = 0.8f, people = 1.3f, ground = 0xa65f3a },
            new Zone { name = "Mile 12 Market", len = 550, crew = "agbero", hMin = 4, hMax = 8, glass = 0, stalls = 2.2f, palms = 0.05f, density = 0.9f, people = 1.8f, ground = 0xa8693f },
            new Zone { name = "Oshodi", len = 600, crew = "agbero", hMin = 5, hMax = 14, glass = 0.05f, stalls = 1.4f, palms = 0.05f, density = 1, people = 1.6f, ground = 0x9f7552, footbridge = 0.14f },
            new Zone { name = "Ikeja", len = 650, crew = "lastma", hMin = 5, hMax = 15, glass = 0.12f, stalls = 0.6f, palms = 0.15f, density = 1, people = 1.2f, ground = 0xb07a4f, footbridge = 0.06f },
            new Zone { name = "Yaba", len = 650, crew = "olopa", hMin = 5, hMax = 12, glass = 0.05f, stalls = 0.75f, palms = 0.1f, density = 1, people = 1.4f, ground = 0xb07a4f },
            new Zone { name = "Third Mainland Bridge", len = 850, crew = "olopa", bridge = true },
            new Zone { name = "Lagos Island", len = 700, crew = "agbero", hMin = 10, hMax = 34, glass = 0.45f, stalls = 1.1f, palms = 0.05f, density = 1, people = 1.5f, ground = 0x9c8a74 },
            new Zone { name = "Victoria Island", len = 750, crew = "olopa", hMin = 12, hMax = 44, glass = 0.7f, stalls = 0.2f, palms = 0.4f, density = 1, people = 0.8f, ground = 0xa99a80 },
            new Zone { name = "Lekki", len = 800, crew = "olopa", hMin = 6, hMax = 18, glass = 0.25f, stalls = 0.3f, palms = 0.6f, density = 0.9f, people = 0.8f, ground = 0xc2a275 },
            new Zone { name = "Ajah", len = 850, crew = "agbero", hMin = 4, hMax = 10, glass = 0.02f, stalls = 1.2f, palms = 0.4f, density = 0.85f, people = 1.2f, ground = 0xb07a4f },
            new Zone { name = "Epe", len = 950, crew = "olopa", hMin = 3, hMax = 6, glass = 0, stalls = 0.4f, palms = 1.6f, density = 0.35f, people = 0.6f, ground = 0x6f8a3c },
        } },
        new City { name = "Port Harcourt", state = "Rivers State", motto = "Treasure Base of the Nation",
            intro = "Run the Garden City from Rumuokoro down to Borokiri waterside. Dodge the traffic, jump the potholes and grab every naira. Stumble twice and agberos, olopa or Task Force go catch you.",
            zones = new[] {
            new Zone { name = "Rumuokoro", len = 550, crew = "agbero", hMin = 4, hMax = 9, glass = 0, stalls = 1.4f, palms = 0.3f, density = 0.85f, people = 1.5f, ground = 0x9c6238, footbridge = 0.08f },
            new Zone { name = "Rumuola", len = 550, crew = "taskforce", hMin = 5, hMax = 12, glass = 0.05f, stalls = 0.8f, palms = 0.2f, density = 0.95f, people = 1.3f, ground = 0xa06a40, footbridge = 0.1f },
            new Zone { name = "Mile 3", len = 600, crew = "agbero", hMin = 4, hMax = 10, glass = 0, stalls = 1.9f, palms = 0.05f, density = 1, people = 1.7f, ground = 0x9a6a44 },
            new Zone { name = "Mile 1 Market", len = 600, crew = "agbero", hMin = 4, hMax = 9, glass = 0, stalls = 2.3f, palms = 0.05f, density = 0.95f, people = 1.9f, ground = 0xa8693f },
            new Zone { name = "GRA Phase 2", len = 650, crew = "olopa", hMin = 6, hMax = 16, glass = 0.22f, stalls = 0.25f, palms = 0.8f, density = 0.85f, people = 0.8f, ground = 0x8f9a5a },
            new Zone { name = "Garrison", len = 650, crew = "taskforce", hMin = 6, hMax = 18, glass = 0.18f, stalls = 0.7f, palms = 0.15f, density = 1, people = 1.3f, ground = 0xa5825c, footbridge = 0.08f },
            new Zone { name = "Trans-Amadi", len = 700, crew = "olopa", hMin = 8, hMax = 26, glass = 0.4f, stalls = 0.3f, palms = 0.3f, density = 0.9f, people = 0.8f, ground = 0x9c8a74 },
            new Zone { name = "Woji Creek Bridge", len = 700, crew = "olopa", bridge = true, towers = 0.15f },
            new Zone { name = "Old GRA", len = 750, crew = "olopa", hMin = 5, hMax = 14, glass = 0.15f, stalls = 0.3f, palms = 0.9f, density = 0.8f, people = 0.8f, ground = 0x8a9a58 },
            new Zone { name = "Town", len = 800, crew = "agbero", hMin = 8, hMax = 24, glass = 0.3f, stalls = 1.2f, palms = 0.1f, density = 1, people = 1.5f, ground = 0x9c8a74 },
            new Zone { name = "Borokiri", len = 900, crew = "agbero", hMin = 3, hMax = 7, glass = 0, stalls = 0.9f, palms = 1.3f, density = 0.55f, people = 0.9f, ground = 0x6f8a3c },
        } },
        new City { name = "Owerri", state = "Imo State", motto = "Eastern Heartland",
            intro = "Run Owerri from Control Post through Douglas Road and on to Nekede. Dodge the traffic, jump the potholes and grab every naira. Stumble twice and agberos, olopa or Task Force go catch you.",
            zones = new[] {
            new Zone { name = "Control Post", len = 550, crew = "agbero", hMin = 4, hMax = 9, glass = 0, stalls = 1.0f, palms = 0.3f, density = 0.9f, people = 1.3f, ground = 0xb5552f },
            new Zone { name = "Douglas Road", len = 550, crew = "agbero", hMin = 4, hMax = 10, glass = 0, stalls = 1.8f, palms = 0.05f, density = 1, people = 1.7f, ground = 0xb05a34 },
            new Zone { name = "Ekeonunwa Market", len = 600, crew = "agbero", hMin = 4, hMax = 8, glass = 0, stalls = 2.3f, palms = 0.05f, density = 0.95f, people = 1.9f, ground = 0xb5552f },
            new Zone { name = "Wetheral Road", len = 650, crew = "taskforce", hMin = 5, hMax = 14, glass = 0.1f, stalls = 0.7f, palms = 0.2f, density = 1, people = 1.2f, ground = 0xaa6a44 },
            new Zone { name = "Ikenegbu", len = 650, crew = "olopa", hMin = 5, hMax = 12, glass = 0.12f, stalls = 0.5f, palms = 0.5f, density = 0.9f, people = 1.0f, ground = 0xb06240 },
            new Zone { name = "World Bank", len = 700, crew = "olopa", hMin = 4, hMax = 9, glass = 0.05f, stalls = 0.3f, palms = 0.8f, density = 0.8f, people = 0.8f, ground = 0xa8683e },
            new Zone { name = "New Owerri", len = 750, crew = "olopa", hMin = 8, hMax = 26, glass = 0.4f, stalls = 0.2f, palms = 0.5f, density = 0.9f, people = 0.8f, ground = 0xa88a6c },
            new Zone { name = "Orji", len = 800, crew = "agbero", hMin = 4, hMax = 10, glass = 0.02f, stalls = 1.2f, palms = 0.4f, density = 0.85f, people = 1.2f, ground = 0xb5552f },
            new Zone { name = "Otamiri Bridge", len = 500, crew = "olopa", bridge = true, towers = 0, stilts = 0 },
            new Zone { name = "Nekede", len = 900, crew = "agbero", hMin = 3, hMax = 6, glass = 0, stalls = 0.5f, palms = 1.6f, density = 0.35f, people = 0.6f, ground = 0x7c8a3a },
        } },
        new City { name = "Warri", state = "Delta State", motto = "The Big Heart",
            intro = "Run Warri from Effurun Roundabout, over Udu Bridge and beyond. Dodge the traffic, jump the potholes and grab every naira. Stumble twice and deve boys, olopa or Task Force go hold you. Warri no dey carry last!",
            zones = new[] {
            new Zone { name = "Effurun Roundabout", len = 550, crew = "deve", hMin = 4, hMax = 10, glass = 0, stalls = 1.3f, palms = 0.2f, density = 0.9f, people = 1.5f, ground = 0x9a6540 },
            new Zone { name = "PTI Road", len = 550, crew = "olopa", hMin = 4, hMax = 10, glass = 0.08f, stalls = 0.5f, palms = 0.5f, density = 0.85f, people = 1.0f, ground = 0x97703f },
            new Zone { name = "Jakpa Road", len = 600, crew = "deve", hMin = 4, hMax = 10, glass = 0, stalls = 1.2f, palms = 0.2f, density = 0.95f, people = 1.4f, ground = 0x9a6540 },
            new Zone { name = "Airport Road", len = 650, crew = "taskforce", hMin = 5, hMax = 16, glass = 0.15f, stalls = 0.6f, palms = 0.3f, density = 1, people = 1.1f, ground = 0xa5825c },
            new Zone { name = "Enerhen Junction", len = 650, crew = "deve", hMin = 4, hMax = 10, glass = 0, stalls = 1.7f, palms = 0.05f, density = 1, people = 1.7f, ground = 0x9c6238, footbridge = 0.08f },
            new Zone { name = "Okumagba Avenue", len = 700, crew = "deve", hMin = 5, hMax = 14, glass = 0.08f, stalls = 1.0f, palms = 0.15f, density = 1, people = 1.3f, ground = 0xa06a40 },
            new Zone { name = "Igbudu Market", len = 700, crew = "deve", hMin = 4, hMax = 8, glass = 0, stalls = 2.3f, palms = 0.05f, density = 0.95f, people = 1.9f, ground = 0xa8693f },
            new Zone { name = "Warri GRA", len = 750, crew = "olopa", hMin = 5, hMax = 14, glass = 0.2f, stalls = 0.2f, palms = 0.9f, density = 0.8f, people = 0.7f, ground = 0x8a9a58 },
            new Zone { name = "Okere", len = 800, crew = "deve", hMin = 4, hMax = 9, glass = 0, stalls = 1.1f, palms = 0.3f, density = 0.9f, people = 1.3f, ground = 0x9a6540 },
            new Zone { name = "Udu Bridge", len = 750, crew = "olopa", bridge = true, towers = 0.1f },
            new Zone { name = "Udu", len = 900, crew = "deve", hMin = 3, hMax = 6, glass = 0, stalls = 0.5f, palms = 1.5f, density = 0.4f, people = 0.7f, ground = 0x6f8a3c },
        } },
    };
    public static Zone[] ZONES = CITIES[0].zones;   // the areas of the city being run
    static int zoneTotal;
    static int ZoneIndexAt(float d) {
        float r = ((d % zoneTotal) + zoneTotal) % zoneTotal;
        for (int i = 0; i < ZONES.Length; i++) { if (r < ZONES[i].len) return i; r -= ZONES[i].len; }
        return 0;
    }
    static readonly Dictionary<string, KeyValuePair<string, float>> POWER_INFO = new Dictionary<string, KeyValuePair<string, float>> {
        { "magnet", new KeyValuePair<string, float>("Magnet", 10) }, { "jump", new KeyValuePair<string, float>("Spring sneakers", 10) }, { "double", new KeyValuePair<string, float>("Jollof 2× score", 12) },
    };
    static readonly string[] POWER_KEYS = { "magnet", "jump", "double" };

    static float Rand(float a, float b) { return Random.Range(a, b); }
    static T2 Pick<T2>(params T2[] a) { return a[Random.Range(0, a.Length)]; }
    string Gang { get { return CREWS[crew].gang; } }
    static CharDef CharById(string id) { foreach (CharDef c in CHARS) if (c.id == id) return c; return CHARS[0]; }

    // ---------- saved progress ----------
    int best, purse, cityIdx; List<string> owned; string charId = "tunde";
    void Save() {
        PlayerPrefs.SetInt("lagosrun.best", best); PlayerPrefs.SetInt("lagosrun.purse", purse);
        PlayerPrefs.SetString("lagosrun.owned", string.Join(",", owned.ToArray())); PlayerPrefs.SetString("lagosrun.char", charId);
        PlayerPrefs.SetInt("lagosrun.city", cityIdx);
        PlayerPrefs.Save();
    }

    // ---------- world state ----------
    class Seg { public GameObject go, crowd; public float a; public List<Mesh> owned = new List<Mesh>(); public List<Runner> people = new List<Runner>(); }
    class Ob {
        public char type; public int lane; public float a, z, vRel, w, d, y0, y1; public bool stand, px, pz, honked; public string crash; public GameObject obj;
        // people in the road (see LRStreet.cs): who they are, their own speed and how fast they are going now, how far
        // they have come, how far they sway across the lane, and the things they roll along
        public Runner[] folk; public float v, cur, ph, dx; public bool soft; public Transform wheel, lid, stick;
    }
    class Coin { public float a, x, y; public GameObject obj; }
    class Pow { public string kind; public int lane; public float a; public GameObject obj; }
    class Chaser { public Runner r; public float ox, oz; public Vector3 pos; public bool armed; }

    Camera cam; Transform skyQuad; float camBack = 7.5f, camUp = 4.2f; int lastW, lastH;
    Runner player; LRAudio sound;
    readonly Dictionary<string, List<Chaser>> crews = new Dictionary<string, List<Chaser>>();
    readonly List<Seg> segments = new List<Seg>();
    readonly List<Ob> obstacles = new List<Ob>();
    readonly List<Coin> coinList = new List<Coin>();
    readonly List<Pow> powerups = new List<Pow>();
    readonly List<Transform> billboards = new List<Transform>();
    readonly Dictionary<string, Shape> powerShapes = new Dictionary<string, Shape>();

    string mode = "boot", crew = "agbero", reason = "";
    float distance, speed = START_SPEED, score, nextRow = 60, nextPower = 350, x, y, vy, slide, phase, invuln, chase, chaserZ = 14, chaserX, show, dyingT, camY, time, charCover = 0.7f;
    int coins, lane = 1, prevLane = 1, zone;
    bool grounded = true, caught, charView, shotMode;
    float npcRange = 80;
    const float danfoYaw = 90, kekeYaw = 0;
    readonly Dictionary<string, float> power = new Dictionary<string, float> { { "magnet", 0 }, { "jump", 0 }, { "double", 0 } };
    float roadOff, waterOff;

    // the screenshot tour must keep running even when its window is not in front
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void KeepTourRunning() {
        string[] args = System.Environment.GetCommandLineArgs();
        if (System.Array.IndexOf(args, "-lrshots") >= 0 || System.Array.IndexOf(args, "-lrlab") >= 0 || System.Array.IndexOf(args, "-lrstreet") >= 0) Application.runInBackground = true;
    }

    // switch to another town: its areas, its signs, and a fresh road
    void SetCity(int i) {
        cityIdx = (i % CITIES.Length + CITIES.Length) % CITIES.Length;
        ZONES = CITIES[cityIdx].zones;
        zoneTotal = 0; foreach (Zone z in ZONES) zoneTotal += z.len;
        T.Place(CITIES[cityIdx]);
        if (canvas == null) return;   // still starting up
        ResetRun(); PaintCity(); Save();
    }

    void Start() {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 1;
        QualitySettings.antiAliasing = Application.isMobilePlatform || Application.platform == RuntimePlatform.WebGLPlayer ? 2 : 4;
        Input.simulateMouseWithTouches = false;
        // the people are detailed models: on phones and in browsers skin them more cheaply and show fewer at once
        bool light = Application.isMobilePlatform || Application.platform == RuntimePlatform.WebGLPlayer;
        QualitySettings.skinWeights = light ? SkinWeights.TwoBones : SkinWeights.FourBones;
        npcRange = light ? 35 : 80;

        best = PlayerPrefs.GetInt("lagosrun.best", 0);
        purse = PlayerPrefs.GetInt("lagosrun.purse", 0);
        owned = new List<string>(PlayerPrefs.GetString("lagosrun.owned", "tunde").Split(','));
        if (!owned.Contains("tunde")) owned.Add("tunde");
        string savedChar = PlayerPrefs.GetString("lagosrun.char", "tunde");

        // light and haze, matched to the web version: warm hemisphere light, one sun, sandy fog
        Shader.SetGlobalColor("_LRSky", T.H(0xfff4dc) * 0.64f);
        Shader.SetGlobalColor("_LRGround", T.H(0x9a7650) * 0.8f);
        Shader.SetGlobalColor("_LRSun", T.H(0xfff0cf) * 0.68f);
        Shader.SetGlobalVector("_LRSunDir", V.P(-6, 12, 5).normalized);
        Shader.SetGlobalColor("_LRFogColor", T.H(0xe9d3a2));
        Shader.SetGlobalVector("_LRFogParams", new Vector4(50, 185, 0, 0));

        // the sun: only its shadows are used, the shader does its own lighting
        Light sun0 = new GameObject("Sun").AddComponent<Light>();
        sun0.type = LightType.Directional; sun0.transform.rotation = Quaternion.LookRotation(-V.P(-6, 12, 5).normalized);
        sun0.shadows = light ? LightShadows.Hard : LightShadows.Soft; sun0.shadowStrength = 1; sun0.shadowBias = 0.05f; sun0.shadowNormalBias = 0.4f;
        QualitySettings.shadows = light ? ShadowQuality.HardOnly : ShadowQuality.All;
        QualitySettings.shadowDistance = light ? 28 : 85;
        QualitySettings.shadowCascades = light ? 1 : 4;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        QualitySettings.shadowResolution = light ? ShadowResolution.Low : ShadowResolution.VeryHigh;
        QualitySettings.shadowProjection = ShadowProjection.StableFit;

        T.Build();
        SetCity(PlayerPrefs.GetInt("lagosrun.city", 0));
        P.Build();
        Moves.Load();

        GameObject cg = new GameObject("Camera");
        cg.tag = "MainCamera";
        cam = cg.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = T.H(0xe9d3a2);
        cam.nearClipPlane = 0.1f; cam.farClipPlane = 500; cam.allowMSAA = true; cam.allowHDR = false;
        cg.AddComponent<AudioListener>();
        if (!light) cg.AddComponent<LRPost>();
        LRPerf.Attach(cg);
        skyQuad = new Shape().Quad(1, 1, T.sky, Matrix4x4.identity).Make("sky", cg.transform).transform;
        skyQuad.localPosition = new Vector3(0, 0, 480);
        Transform sun = new Shape().Quad(1, 1, T.sun, Matrix4x4.identity).Make("sun").transform;
        sun.position = V.P(-70, 70, -420); sun.localScale = Vector3.one * 110;
        billboards.Add(sun);

        foreach (string k in POWER_KEYS) powerShapes[k] = new Shape().Quad(1.3f, 1.3f, T.Transparent(T.Basic(T.icon[k])), Matrix4x4.identity);

        sound = new LRAudio(gameObject);
        InitWorld(owned.Contains(savedChar) ? savedChar : "tunde");
        BuildUI();
        Resize();
        ResetRun();
        mode = "menu";

        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-lrshots") { shotMode = true; StartCoroutine(Shots(args[i + 1])); }
        for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-lrlab") { shotMode = true; StartCoroutine(Lab(args[i + 1])); }
        for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-lrstreet") { shotMode = true; StartCoroutine(StreetLab(args[i + 1])); }
    }

    void InitWorld(string startChar) {
        new Shape().Quad(7.4f, 240, T.road, V.M(0, 0, -100, -PI / 2, 0, 0)).Make("road");
        new Shape().Quad(700, 700, T.water, V.M(0, -5, -150, -PI / 2, 0, 0)).Make("water");
        for (int i = 0; i < NSEG; i++) segments.Add(new Seg { go = new GameObject("segment"), crowd = new GameObject("crowd") });
        SetCharacter(startChar);

        // chasers, each with a formation offset (x, z) behind the player
        crews["agbero"] = new List<Chaser> {
            NewChaser(new Person { model = "Naija", hat = "cap", hatColor = 0xc8452e, prop = "machete" }, -0.6f, 0),
            NewChaser(new Person { model = "Dayo", dress = new[] { "camotee", "#c8452e" }, hat = "bandana", hatColor = 0x1f1f1f, prop = "stick" }, 0.6f, 0.2f),
            NewChaser(new Person { model = "Tunde", dress = new[] { "suit", "x#8a8a8a", "afro", "hide" }, hat = "bandana", hatColor = 0xc8452e, prop = "stick" }, -1.4f, 0.9f),
            NewChaser(new Person { model = "Naija", dress = new[] { "tankshirt", "#2d2d2d", "cargo", "x#9a8a70" }, prop = "machete" }, 1.4f, 1.1f),
        };
        crews["olopa"] = new List<Chaser> {
            NewChaser(new Person { model = "Emeka", dress = new[] { "suit", "#1b2030", "shoes", "x#333333" }, hat = "beret", hatColor = 0x111111, prop = "baton" }, -0.6f, 0),
            NewChaser(new Person { model = "Tunde", dress = new[] { "suit", "#1b2030", "afro", "hide" }, hat = "cap", hatColor = 0x161a24 }, 0.6f, 0.3f),
        };
        crews["lastma"] = new List<Chaser> {
            NewChaser(new Person { model = "Baba", dress = new[] { "shirt", "#e2b11f", "trouser", "#6e1e2a", "sandals", "x#333333" }, hat = "beret", hatColor = 0x6e1e2a }, 0.7f, 0),
        };
        crews["taskforce"] = new List<Chaser> {
            NewChaser(new Person { model = "Emeka", dress = new[] { "suit", "#4a5a32", "shoes", "x#222222" }, hat = "beret", hatColor = 0x2a3320, prop = "baton" }, -0.6f, 0),
            NewChaser(new Person { model = "Dayo", dress = new[] { "camotee", "#4a5a32" }, hat = "cap", hatColor = 0x2a3320 }, 0.6f, 0.3f),
        };
    }
    Chaser NewChaser(Person who, float ox, float oz) {
        Chaser c = new Chaser { r = People.Spawn(who, "chaser"), ox = ox, oz = oz, armed = who.prop != null };
        c.r.root.gameObject.SetActive(false);
        return c;
    }

    void SetCharacter(string id) {
        CharDef c = CharById(id);
        Runner r = People.Spawn(c.look, "player");
        Material steel = T.Emissive(T.Lam(0xd8dde2), 0x333333);
        r.cuffs = new Shape().Cyl(0.045f, 0.045f, 0.03f, 12, steel, -0.05f, 0.88f, 0.2f, 0, 0, PI / 2).Cyl(0.045f, 0.045f, 0.03f, 12, steel, 0.05f, 0.88f, 0.2f, 0, 0, PI / 2).Make("cuffs", r.root);
        r.cuffs.SetActive(false);
        if (player != null) { r.root.position = player.root.position; Destroy(player.root.gameObject); }
        player = r;
        charId = c.id;
        Save();
    }

    // segments: absolute position `a` is the near edge of the segment along the run
    void BuildSegment(Seg seg) {
        Shape g = new Shape();
        foreach (Runner old in seg.people) Destroy(old.root.gameObject);
        seg.people.Clear();
        int zi = ZoneIndexAt(seg.a + SEG_LEN / 2);
        Zone zone = ZONES[zi];
        float L = SEG_LEN;
        // area border: the green state welcome gantry
        if (ZoneIndexAt(seg.a - 1) != zi) {
            float px = zone.bridge ? 4.0f : 4.5f;
            foreach (float s in new[] { -1f, 1f }) g.Box(0.25f, 7.2f, 0.25f, T.metal, s * px, 3.6f, -L / 2 + 3);
            g.Box(px * 2 + 0.4f, 0.2f, 0.2f, T.metal, 0, 6.9f, -L / 2 + 3);
            g.Quad(8.6f, 1.6f, T.welcome[zi], V.M(0, 6.1f, -L / 2 + 3.13f));
        }
        if (zone.bridge) {
            g.Box(8.4f, 1.2f, L, T.darkConcrete, 0, -0.62f, 0);
            foreach (float s in new[] { -1f, 1f }) {
                g.Box(0.35f, 0.9f, L, T.concrete, s * 3.95f, 0.45f, 0);
                g.Box(0.1f, 0.1f, L, T.metal, s * 3.95f, 1.1f, 0);
            }
            g.Box(3, 5, 2.5f, T.darkConcrete, 0, -3.6f, 0);
            int side = Mathf.RoundToInt(seg.a / L) % 2 != 0 ? 1 : -1;
            g.Add(P.lamp, V.M(side * 4.2f, 0, 0, 0, side > 0 ? 0 : PI, 0));
            if (Random.value < zone.stilts) g.Add(P.stilt, V.M(Pick(-1f, 1f) * Rand(22, 45), -5.5f, Rand(-8, 8)));
            if (Random.value < 0.25f) g.Add(P.canoe, V.M(Pick(-1f, 1f) * Rand(12, 30), -4.85f, Rand(-8, 8), 0, Rand(0, 3), 0));
            if (Random.value < zone.towers) {
                float hgt = Rand(20, 60);
                g.Box(Rand(8, 14), hgt, Rand(8, 14), Pick(T.glass), Pick(-1f, 1f) * Rand(80, 130), hgt / 2 - 5, Rand(-5, 5));
            }
            g.Fill(seg.go, seg.owned);
            return;
        }
        // land
        foreach (float s in new[] { -1f, 1f }) {
            g.Box(0.5f, 0.1f, L, T.gutter, s * 3.95f, -0.03f, 0);
            g.Box(0.15f, 0.3f, L, T.curb, s * 4.25f, 0.12f, 0);
            g.Box(2.4f, 0.22f, L, T.sidewalk, s * 5.5f, 0.11f, 0);
            g.Box(84, 0.1f, L, T.Ground(zone.ground), s * 48.6f, -0.04f, 0);
            // buildings along this side, assembled from the kit: two or three bays wide, one to five storeys
            float z = -L / 2;
            while (z < L / 2 - 2) {
                bool gl = Random.value < zone.glass;
                if (gl) {
                    float tl = Pick(10f, 13f), th = Mathf.Round(Rand(Mathf.Max(zone.hMin, 18), zone.hMax + 10)), tsb = Rand(9.5f, 12);
                    if (z + tl > L / 2 + 3) break;
                    Material gm = Pick(T.glass);
                    Vector2 f4 = new Vector2(tl / 4, th / 4), t4 = new Vector2(3, tl / 4), e4 = new Vector2(3, th / 4);
                    g.Box6(12, th, tl, new[] { gm, gm, T.roofFlat, T.roofFlat, gm, gm }, s * (tsb + 6), th / 2, z + tl / 2, new[] { f4, f4, t4, t4, e4, e4 });
                    z += tl + Rand(0.3f, 1.5f); continue;
                }
                int cols = Pick(2, 2, 3);
                float len = cols * 3;
                if (z + len > L / 2 + 3) break;
                if (Random.value > zone.density) {
                    // empty plot: a palm or nothing
                    if (Random.value < 0.6f) g.Add(P.palm, V.M(s * Rand(8, 14), 0, z + len / 2, 0, Rand(0, 6), 0));
                    z += len + 1; continue;
                }
                AddBuilding(g, s, Rand(7.2f, 8.2f), z, cols, Mathf.Clamp(Mathf.RoundToInt(Rand(zone.hMin, zone.hMax) / 3f), 1, 5));
                z += len + Rand(0.3f, 1.5f);
            }
            // things left outside the shops
            int nBits = Random.Range(0, 4);
            for (int k = 0; k < nBits; k++) { float bx = Rand(6.3f, 7.0f); g.Add(Pick(P.clutter), V.M(s * bx, bx < 6.7f ? 0.22f : 0.02f, Rand(-9.5f, 9.5f), 0, Rand(0, 6), 0)); }
            if (Random.value < 0.14f) g.Add(P.kiosk, V.M(s * 5.7f, 0.22f, Rand(-7, 7), 0, s > 0 ? PI / 2 : -PI / 2, 0));
            int nStalls = Mathf.FloorToInt(zone.stalls) + (Random.value < zone.stalls % 1 ? 1 : 0);
            for (int k = 0; k < nStalls; k++) g.Add(Pick(P.stalls), V.M(s * Rand(5.3f, 6.2f), 0.22f, -8 + (k + Random.value * 0.6f) * (16f / Mathf.Max(1, nStalls)), 0, Rand(0, 3), 0));
            int nPalms = Mathf.FloorToInt(zone.palms) + (Random.value < zone.palms % 1 ? 1 : 0);
            for (int k = 0; k < nPalms; k++) g.Add(P.palm, V.M(s * 6.3f, 0.2f, Rand(-8, 8), 0, Rand(0, 6), 0));
            int nPeople = Mathf.FloorToInt(zone.people) + (Random.value < zone.people % 1 ? 1 : 0);
            for (int k = 0; k < nPeople; k++) {
                Runner n = People.Spawn(Pick(NPCS), "bystander", seg.crowd.transform);
                n.root.localPosition = V.P(s * Rand(4.9f, 6.4f), 0.22f, -9 + (k + Random.value) * (18f / nPeople));
                n.rootE.y = s * PI / 2 + Rand(-0.9f, 0.9f);
                n.root.gameObject.SetActive(false);
                seg.people.Add(n);
            }
        }
        if (zone.footbridge > 0 && Random.value < zone.footbridge && ZoneIndexAt(seg.a - 1) == zi) g.Add(P.footbridge, V.M(0, 0, Rand(-4, 4)));
        g.Add(P.pole, V.M(-4.7f, 0, 0));
        foreach (float dx in new[] { -0.6f, 0.6f }) g.Box(0.03f, 0.03f, L, T.wire, -4.7f + dx, 7.45f, 0);
        if (Random.value < 0.22f) {
            float s = Pick(-1f, 1f);
            g.Add(Pick(P.billboards), V.M(s * 6.4f, 0, 0, 0, (s > 0 ? PI : 0) + s * 0.35f, 0));
        }
        g.Fill(seg.go, seg.owned);
    }

    void AddBuilding(Shape g, float s, float setback, float z0, int cols, int floors) {
        float len = cols * 3, h = floors * 3, dx = 9;
        Material wall = T.Wall(Random.Range(0, 5), Random.Range(0, T.WallColours.Length));
        // the block behind the front: sides, back and roof, open where the modelled front goes
        Material[] body = { wall, wall, T.roofFlat, T.roofFlat, wall, wall };
        body[s > 0 ? 1 : 0] = null;
        Vector2 back = new Vector2(len / 3, h / 3), top = new Vector2(dx / 3, len / 3), side = new Vector2(dx / 3, h / 3);
        g.Box6(dx, h, len, body, s * (setback + dx / 2), h / 2, z0 + len / 2, new[] { back, back, top, top, side, side });
        string upper = Pick("Wall_WindowBars", "Wall_WindowBars", "Wall_Window", "Wall_Louvre");
        Quaternion yaw = Quaternion.Euler(0, s > 0 ? 90 : -90, 0);
        for (int f = 0; f < floors; f++) for (int c = 0; c < cols; c++) {
            float zu = s > 0 ? -z0 - c * 3 : -(z0 + len) + c * 3;
            Matrix4x4 m = Matrix4x4.TRS(new Vector3(s * setback, f * 3, zu), yaw, Vector3.one);
            string piece = f == 0 ? Pick("Shop_Open", "Shop_Open", "Shop_Shutter", "Shop_Shutter", "Wall_Door")
                : floors > 1 && c == cols / 2 && Random.value < 0.35f ? "Wall_Balcony" : Random.value < 0.8f ? upper : "Wall_Window";
            Kit.Add(g, piece, m, wall);
            if (f == 0 && piece != "Wall_Door" && Random.value < 0.45f) Kit.Add(g, "Awning", m, wall);
            if (f > 0 && piece != "Wall_Balcony" && Random.value < 0.2f) Kit.Add(g, "AC_Unit", m * Matrix4x4.Translate(new Vector3(Rand(0.15f, 2.0f), 0.12f, 0)), wall);
            if (f == floors - 1) Kit.Add(g, "Parapet", Matrix4x4.TRS(new Vector3(s * setback, h, zu), yaw, Vector3.one), wall);
        }
        g.Quad(Mathf.Min(len - 0.8f, 6), 1.0f, Pick(T.signs), V.M(s * (setback - 0.24f), 3.3f, z0 + len / 2, 0, -s * PI / 2, 0));
        if (Random.value < 0.55f) Kit.Add(g, "WaterTank", Matrix4x4.Translate(new Vector3(s * (setback + Rand(2, 6)), h, -(z0 + Rand(1, len - 1)))), wall);
    }

    // ---------- obstacles / pickups ----------
    Ob AddObstacle(char type, int ln, float a) {
        Ob o = new Ob { type = type, lane = ln, a = a };
        Shape sh; float ry = 0;
        switch (type) {
            case 'D': o.w = 1.9f; o.d = 4.6f; o.y0 = 0; o.y1 = 2.1f; o.stand = true; o.crash = "You jam danfo!"; sh = Pick(P.danfo); break;
            case 'M': o.w = 1.9f; o.d = 4.6f; o.y0 = 0; o.y1 = 2.1f; o.stand = true; o.crash = "Oncoming danfo! Shine your eye!"; sh = Pick(P.danfo); ry = PI; o.vRel = 9; break;
            case 'K': o.w = 1.4f; o.d = 2.8f; o.y0 = 0; o.y1 = 1.86f; o.stand = true; o.crash = "Keke don knock you!"; sh = P.keke; ry = PI; break;
            case 'B': o.w = 1.9f; o.d = 0.35f; o.y0 = 0; o.y1 = 0.98f; o.crash = "Road block! You no see am?"; sh = P.barrier; break;
            case 'W': o.w = 1.0f; o.d = 2.5f; o.y0 = 0; o.y1 = 1.5f; o.v = 2.4f; o.soft = true; sh = null; break;
            case 'S': o.w = 0.8f; o.d = 0.8f; o.y0 = 0; o.y1 = 1.55f; o.v = 4.6f; o.soft = true; sh = null; break;
            case 'H': o.w = 0.9f; o.d = 1.7f; o.y0 = 0; o.y1 = 1.3f; o.v = 3.6f; o.soft = true; sh = null; break;
            case 'O': o.w = 2.0f; o.d = 0.3f; o.y0 = 1.25f; o.y1 = 2.6f; o.crash = "Banner hold your head!"; sh = Pick(P.banner); break;
            default: o.w = 1.5f; o.d = 1.7f; o.y0 = -1; o.y1 = 0.12f; o.crash = "Pothole swallow your leg!"; sh = P.pothole; break;
        }
        // real models for the danfo and keke; the box versions remain as a fallback
        if (o.soft) BuildFolk(o);
        else if (type == 'D' || type == 'M') o.obj = Vehicles.Make("Danfo", 1.95f, 2.15f, 4.6f, danfoYaw);
        else if (type == 'K') o.obj = Vehicles.Make("Keke", 1.45f, 1.9f, 2.8f, kekeYaw);
        if (o.obj == null) o.obj = sh.Make("obstacle");
        o.obj.transform.rotation = V.R(0, ry, 0);
        o.obj.transform.position = V.P(LANE_X[ln], 0, -500);
        obstacles.Add(o);
        return o;
    }
    void AddCoin(int ln, float a, float cy) {
        GameObject obj = P.coin.Make("coin");
        obj.transform.position = V.P(LANE_X[ln], cy, -500);
        obj.transform.rotation = V.R(0, a * 0.4f, 0);
        coinList.Add(new Coin { a = a, y = cy, x = LANE_X[ln], obj = obj });
    }
    void AddPower(string kind, int ln, float a) {
        GameObject obj = powerShapes[kind].Make("power");
        obj.transform.position = V.P(LANE_X[ln], 1.1f, -500);
        billboards.Add(obj.transform);
        powerups.Add(new Pow { kind = kind, lane = ln, a = a, obj = obj });
    }
    bool LaneBusyAhead(int ln) { foreach (Ob o in obstacles) if (o.lane == ln && o.a > distance - 3) return true; return false; }

    // How tough the run has become: 0 at the start, 1 by Lekki (2.6 km), then creeping on to 2 at 7.6 km and no
    // further. Speed, how close the rows come, how full they are, how long the chasers stay on you and how rare
    // the pick-ups are all follow it, so the run gets harder a little at a time rather than in steps.
    static float Tough(float d) { return d < 2600 ? Mathf.Max(0, d) / 2600 : 1 + Mathf.Min(1, (d - 2600) / 5000); }
    static float SpeedAt(float d) { float g = Tough(d); return g < 1 ? Mathf.Lerp(START_SPEED, 30, g) : Mathf.Lerp(30, MAX_SPEED, g - 1); }

    // returns the extra road to leave before the next row, when this one has people who will wander up it
    float SpawnRow(float a) {
        float g = Tough(distance - 60), t = Mathf.Min(1, g), late = Mathf.Max(0, g - 1);
        char[] keys = { '.', 'B', 'O', 'P', 'K', 'D', 'M', 'W', 'S', 'H' };
        float[] weights = { 3.6f - t * 1.4f - late * 0.6f, 1.1f, t > 0.04f ? 0.9f : 0, 0.8f, 0.8f, 1.5f, t > 0.12f ? 0.35f + g * 0.6f : 0, 0.3f, 0.3f, 0.3f };
        System.Func<char> choose = () => {
            float s = 0; foreach (float wv in weights) s += wv;
            float r = Random.value * s;
            for (int i = 0; i < keys.Length; i++) { r -= weights[i]; if (r <= 0) return keys[i]; }
            return '.';
        };
        char[] row = { choose(), choose(), choose() };
        // a row of three vehicles would be a wall; always leave a way through
        System.Func<char, bool> blocking = c => c == 'D' || c == 'K' || c == 'M';
        // people in the road come one group at a time with no vehicle beside them, so there is always room to go round
        System.Func<char, bool> people = c => c == 'W' || c == 'S' || c == 'H';
        int who = -1; float room = 0;
        for (int ln = 0; ln < 3; ln++) if (people(row[ln])) { if (who < 0) who = ln; else row[ln] = '.'; }
        if (who >= 0) for (int ln = 0; ln < 3; ln++) if (blocking(row[ln])) row[ln] = '.';
        if (blocking(row[0]) && blocking(row[1]) && blocking(row[2])) row[Random.Range(0, 3)] = '.';
        int flat = 0; foreach (char c in row) if (c == 'O' || c == 'B') flat++;
        if (flat == 3) row[Random.Range(0, 3)] = '.';
        for (int ln = 0; ln < 3; ln++) if (row[ln] == 'M' && LaneBusyAhead(ln)) row[ln] = 'D';
        for (int ln = 0; ln < 3; ln++) {
            char c = row[ln];
            if (c == '.') continue;
            Ob made = AddObstacle(c, ln, a + (c == 'D' || c == 'M' ? 2.3f : c == 'K' ? 1.4f : 0));
            if (made.soft) {
                room = 62 * made.v / Mathf.Max(4, speed - made.v);
                if (c == 'S' && Random.value < 0.45f) AddObstacle('S', ln, a - 2.8f);   // the girl and the boy skating together
            }
            // occasionally a convoy of danfos you can run across
            if (c == 'D' && Random.value < 0.25f + t * 0.2f) AddObstacle('D', ln, a + 2.3f + 5.2f);
        }
        // pickups
        if (a > nextPower) {
            var free = new List<int>();
            for (int i = 0; i < 3; i++) if (row[i] == '.') free.Add(i);
            if (free.Count > 0) { AddPower(Pick(POWER_KEYS), free[Random.Range(0, free.Count)], a - 6); nextPower = a + Rand(380, 560) * (1 + g * 0.35f); }
        }
        int cl = Random.Range(0, 3);
        char cc = row[cl];
        if (cc == 'M' || people(cc)) return room;
        if (cc == 'D') { for (int k = 0; k < 5; k++) AddCoin(cl, a + 0.6f + k * 0.9f, 2.9f); }
        else if (cc == 'K') { for (int k = 0; k < 4; k++) AddCoin(cl, a - 0.2f + k * 0.9f, 2.6f); }
        else if (cc == 'B' || cc == 'P') { for (int k = 0; k < 7; k++) { float f = k / 6f; AddCoin(cl, a - 4 + f * 8, 0.8f + Mathf.Sin(f * PI) * 1.6f); } }
        else if (cc == 'O') { for (int k = 0; k < 5; k++) AddCoin(cl, a - 4 + k * 2, 0.5f); }
        else { for (int k = 0; k < 6; k++) AddCoin(cl, a - 8 + k * 2.6f, 0.8f); }
        return room;
    }

    // ---------- game flow ----------
    void ClearRun() {
        foreach (Ob o in obstacles) Destroy(o.obj);
        foreach (Coin c in coinList) Destroy(c.obj);
        foreach (Pow p in powerups) { billboards.Remove(p.obj.transform); Destroy(p.obj); }
        obstacles.Clear(); coinList.Clear(); powerups.Clear();
    }
    void ResetRun() {
        ClearRun();
        distance = 0; speed = START_SPEED; score = 0; coins = 0; nextRow = 55; nextPower = 300; lane = 1; prevLane = 1; x = 0; y = 0; vy = 0;
        grounded = true; slide = 0; invuln = 0; chase = 0; chaserZ = 14; chaserX = 0; crew = ZONES[0].crew; caught = false; show = 0; dyingT = 0; reason = ""; zone = 0; camY = 0;
        foreach (string k in POWER_KEYS) power[k] = 0;
        for (int i = 0; i < segments.Count; i++) { segments[i].a = -20 + i * SEG_LEN; BuildSegment(segments[i]); }
        ShowArea(0);
        player.ResetPose();
        player.cuffs.SetActive(false);
        foreach (var list in crews.Values) foreach (Chaser c in list) { c.r.ResetPose(); c.r.root.gameObject.SetActive(false); }
    }
    void StartRun() {
        ResetRun();
        mode = "play";
        charView = false;
        ShowScreen(null);
        sound.Music(true);
        show = 5;
        Toast(CharById(charId).line, 2.4f, CREWS[ZONES[0].crew].warn);
    }
    void SetPaused(bool p) {
        if (p && mode == "play") { mode = "paused"; ShowScreen(pauseScreen); sound.Music(false); }
        else if (!p && mode == "paused") { mode = "play"; ShowScreen(null); sound.Music(true); }
    }
    void EndRunFromPause() {
        if (mode != "paused") return;
        pauseScreen.gameObject.SetActive(false);
        reason = "You rest small. Run ended."; mode = "dying"; dyingT = 1;
    }
    void Die(string why) {
        if (mode != "play") return;
        mode = "dying"; dyingT = 0; reason = why;
        if (caught) {   // clear the pickups so nothing floats through the scene
            foreach (Coin c in coinList) Destroy(c.obj);
            foreach (Pow p in powerups) { billboards.Remove(p.obj.transform); Destroy(p.obj); }
            coinList.Clear(); powerups.Clear();
        }
        sound.Crash(); sound.Music(false);
    }
    void ShowOver() {
        mode = "over";
        int final = Mathf.FloorToInt(score);
        bool isBest = final > best;
        if (isBest) best = final;
        int earned = coins * 10;
        purse += earned;
        Save();
        PaintOver(final, isBest, earned);
    }

    // ---------- input ----------
    void Act(string dir) {
        if (mode != "play") return;
        if (dir == "left" || dir == "right") {
            int nl = Mathf.Clamp(lane + (dir == "left" ? -1 : 1), 0, 2);
            if (nl != lane) { prevLane = lane; lane = nl; }
        } else if (dir == "up") {
            if (grounded) { vy = JUMP_V * (power["jump"] > 0 ? 1.32f : 1); grounded = false; slide = 0; sound.Jump(); }
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
        } else if (mode == "menu" && !charView && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))) { StartRun(); return; }
        else if (mode == "menu" && !charView && Input.GetKeyDown(KeyCode.LeftArrow)) SetCity(cityIdx - 1);
        else if (mode == "menu" && !charView && Input.GetKeyDown(KeyCode.RightArrow)) SetCity(cityIdx + 1);
        else if (mode == "over" && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))) { StartRun(); return; }
        if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape)) {
            if (charView) CloseChars();
            else if (mode == "play") SetPaused(true);
            else if (mode == "paused") SetPaused(false);
        }
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
            // mouse drag for desktop trackpads
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
        cam.fieldOfView = portrait ? 74 : 62;
        camBack = portrait ? 9.2f : 7.5f;
        camUp = portrait ? 5.0f : 4.2f;
        float h = 2 * 480 * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        skyQuad.localScale = new Vector3(h * lastW / lastH * 1.02f, h * 1.02f, 1);
        LayoutUI();
    }

    // what a bystander does as the runner goes by: cheer them on, or shout for help when the area boys are after them
    string Reaction(Runner n) {
        if (n.pose != "Idle_A" && n.pose != "Idle_FoldArms" && n.pose != "Idle_Talking" && n.pose != "Yes") return null;
        int pick = (int)(n.seed * 3) % 5;
        if (chase > 0) return pick < 2 ? "Help_Two_Arms" : pick < 4 ? "Cheering_Two_Hands" : "Insult";
        return pick == 0 ? "Cheer_One_arm" : pick == 1 ? "Greeting" : pick == 2 ? "Victory" : null;
    }

    // ---------- update ----------
    bool OverlapX(Ob o) { return Mathf.Abs(x - LANE_X[o.lane]) < (o.w + P_W) / 2; }
    bool OverlapZ(Ob o, float z) { return Mathf.Abs(z) < (o.d + P_D) / 2; }
    void Aim(Vector3 from, Vector3 at) { cam.transform.position = V.P(from.x, from.y, from.z); cam.transform.LookAt(V.P(at.x, at.y, at.z)); }

    void Update() {
        if (mode == "boot") return;
        if (Screen.width != lastW || Screen.height != lastH) Resize();
        HandleInput();
        float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
        Step(dt);
        TickUI(dt);
    }
    void LateUpdate() {
        if (cam == null) return;
        foreach (Transform b in billboards) b.rotation = cam.transform.rotation;
    }

    void Step(float dt) {
        time += dt;
        bool playing = mode == "play", dying = mode == "dying";
        if (dying) {
            dyingT += dt;
            speed = Mathf.Max(0, speed - 60 * dt);
            if (dyingT > (caught ? 4.6f : 1.7f)) { ShowOver(); return; }
        } else if (playing) speed = SpeedAt(distance);
        else if (mode == "menu") speed = 9;
        else return;

        float dz = speed * dt;
        distance += dz;
        if (playing) score += dz * (power["double"] > 0 ? 2 : 1);
        roadOff = (roadOff + dz / 12) % 1; T.road.mainTextureOffset = new Vector2(0, roadOff);
        waterOff = (waterOff + dz * 0.002f) % 1; T.water.mainTextureOffset = new Vector2(0, waterOff);

        // scenery
        foreach (Seg s in segments) {
            while (s.a + SEG_LEN < distance - 22) { s.a += NSEG * SEG_LEN; if (s.a + SEG_LEN >= distance - 22) BuildSegment(s); }
            float sz = distance - (s.a + SEG_LEN / 2);
            s.go.transform.position = V.P(0, 0, sz); s.crowd.transform.position = V.P(0, 0, sz);
            // only the nearby bystanders are shown and animated
            foreach (Runner n in s.people) {
                float nz = sz - n.root.localPosition.z;
                bool near = nz > -npcRange && nz < (mode == "menu" ? 40 : 12);
                if (n.root.gameObject.activeSelf != near) n.root.gameObject.SetActive(near);
                if (!near) continue;
                // they watch the runner come past, and the ones with nothing better to do join in
                bool passing = mode != "menu" && nz > -24 && nz < 7;
                n.look = V.P(x, y + 1.5f, 0); n.lookW = passing ? 1 : 0;
                n.Idle(time, dt, passing && nz > -15 && nz < 4 ? Reaction(n) : null); n.Apply();
            }
        }

        if (mode == "menu") {
            phase += dt * speed * 0.6f;
            player.Animate(phase, "run", dt); player.Apply();
            player.root.position = Vector3.zero;
            if (charView) {
                // look back at the runner's face; keep them clear of the picker panel
                bool portrait = cam.aspect < 0.85f;
                if (portrait) Aim(new Vector3(0.4f, 1.4f, -6.2f), new Vector3(0, -2.1f - Mathf.Max(0, charCover - 0.7f) * 3.5f, 0));
                else Aim(new Vector3(-0.6f, 1.7f, -4.4f), new Vector3(1.25f, 1.0f, 0));
            } else Aim(new Vector3(Mathf.Sin(time * 0.3f) * 2.5f, 3.2f, 6.5f), new Vector3(0, 1.4f, -6));
            return;
        }

        // spawn rows ahead
        if (playing) {
            while (nextRow < distance + SPAWN_AHEAD) {
                float room = SpawnRow(nextRow);
                // the gap to the next row, in seconds of running: generous at first, tight later
                float g = Tough(distance);
                nextRow += room + speed * (g < 1 ? Mathf.Lerp(1.85f, 1.0f, g) : Mathf.Lerp(1.0f, 0.82f, g - 1)) + Rand(0, 9 - g * 3);
            }
        }

        // timers
        foreach (string k in POWER_KEYS) if (power[k] > 0) power[k] = Mathf.Max(0, power[k] - dt);
        invuln = Mathf.Max(0, invuln - dt);
        chase = Mathf.Max(0, chase - dt);
        show = Mathf.Max(0, show - dt);

        // player horizontal
        float tx = LANE_X[lane];
        x += (tx - x) * Mathf.Min(1, dt * 14);
        if (Mathf.Abs(tx - x) < 0.01f) x = tx;

        // vertical
        float yPrev = y;
        vy += GRAVITY * dt; y += vy * dt;
        if (slide > 0) slide -= dt;

        // move obstacles, find what the runner is standing on
        float support = 0;
        foreach (Ob o in obstacles) {
            if (o.folk != null) Pace(o, dt);
            o.a -= o.vRel * dt;
            o.z = distance - o.a;
            o.obj.transform.position = V.P(LANE_X[o.lane] + o.dx, 0, o.z);
            if (o.folk != null) Liven(o, dt);
            if (o.type == 'M' && !o.honked && o.z > -45 && o.lane == lane) { o.honked = true; sound.Horn(); }
            if (o.stand && OverlapX(o) && OverlapZ(o, o.z) && yPrev >= o.y1 - 0.45f) support = Mathf.Max(support, o.y1);
        }
        if (y <= support) { y = support; if (vy < 0) vy = 0; grounded = true; }
        else grounded = false;

        // collisions
        if (playing) {
            float h = slide > 0 ? P_H_SLIDE : P_H;
            foreach (Ob o in obstacles) {
                bool ox = OverlapX(o), oz = OverlapZ(o, o.z);
                bool oy = y < o.y1 - 0.02f && y + h > o.y0;
                if (ox && oz && oy && invuln <= 0) {
                    if ((o.pz && !o.px) || o.soft) {
                        // came in from the side, or ran into someone: bounce back and stumble
                        if (o.pz && !o.px) lane = prevLane;
                        invuln = o.soft ? 0.8f : 0.5f;
                        if (chase > 0) { caught = true; Die(CREWS[crew].caught); break; }
                        crew = ZONES[zone].crew;
                        chase = 7 + Tough(distance) * 2; sound.Bump(); if (Gang != "agbero") sound.Whistle();   // further on they stay on you longer
                        player.Flinch();
                        Toast(CREWS[crew].warn, 1.9f, Pick(CREWS[crew].taunts));
                    } else { Die(o.crash); break; }
                }
                o.px = ox; o.pz = oz;
            }
            dying = mode == "dying"; playing = mode == "play";
        }

        // cleanup obstacles
        for (int i = obstacles.Count - 1; i >= 0; i--) if (obstacles[i].z > 14) { Destroy(obstacles[i].obj); obstacles.RemoveAt(i); }

        // coins
        for (int i = coinList.Count - 1; i >= 0; i--) {
            Coin c = coinList[i];
            float z = distance - c.a;
            if (power["magnet"] > 0 && z > -16 && z < 2) {
                c.x += (x - c.x) * Mathf.Min(1, dt * 8);
                c.y += (y + 1 - c.y) * Mathf.Min(1, dt * 8);
                c.a += (distance - c.a) * Mathf.Min(1, dt * 6);
                z = distance - c.a;
            }
            c.obj.transform.position = V.P(c.x, c.y, z);
            c.obj.transform.Rotate(0, -dt * 4 * Mathf.Rad2Deg, 0, Space.World);
            if (playing && Mathf.Abs(z) < 0.8f && Mathf.Abs(c.x - x) < 0.9f && Mathf.Abs(c.y - (y + 0.8f)) < 1.25f) {
                coins++; score += 5; sound.Coin(); Destroy(c.obj); coinList.RemoveAt(i);
                if (coins % 100 == 0) Toast(Pick("Money dey!", "Cashout!", "Odogwu!"), 1.4f, null);
                continue;
            }
            if (z > 10) { Destroy(c.obj); coinList.RemoveAt(i); }
        }
        // power-ups
        for (int i = powerups.Count - 1; i >= 0; i--) {
            Pow p = powerups[i]; float z = distance - p.a;
            p.obj.transform.position = V.P(LANE_X[p.lane], 1.1f + Mathf.Sin(time * 4 + i) * 0.15f, z);
            bool got = playing && Mathf.Abs(z) < 1 && Mathf.Abs(LANE_X[p.lane] - x) < 1 && y < 2.4f;
            if (got) { power[p.kind] = POWER_INFO[p.kind].Value; sound.Power(); Toast(POWER_INFO[p.kind].Key + "!", 1.4f, null); }
            if (got || z > 10) { billboards.Remove(p.obj.transform); Destroy(p.obj); powerups.RemoveAt(i); }
        }

        // zone change
        int zi = ZoneIndexAt(distance);
        if (zi != zone) { zone = zi; ShowArea(zi); if (playing) { Toast("Welcome to " + ZONES[zi].name, 2.6f, CREWS[ZONES[zi].crew].warn); show = 5; } }

        // player pose
        float before = phase;
        phase += dt * speed * 0.55f;
        string pose = !grounded ? "air" : slide > 0 ? "slide" : "run";
        if (playing && pose == "run" && Mathf.Floor(phase / PI) != Mathf.Floor(before / PI)) sound.Step();   // a footfall every half cycle
        player.Animate(phase, pose, dt, speed > 25, vy);
        Vector3 pp = new Vector3(x, y, 0);
        player.rootE = Vector3.zero;
        player.Swerve((x - tx) / 2.2f, dt);
        // eyes stay on the road ahead while the body turns under them
        player.look = V.P(tx, y + 1.5f, -30); player.lookW = dying ? 0 : 1;
        bool caughtScene = dying && caught;
        if (dying && !caughtScene) { player.rootE.x = 0; player.Play("Hit_Knockback", dyingT / 0.85f, dt); }   // knocked flat by the crash
        if (caughtScene) {
            // the blow lands, then: cowering under the agberos' sticks, or down on the knees for olopa and LASTMA
            player.rootE = Vector3.zero;
            if (dyingT < 0.45f) player.Play("Hit_Chest", dyingT / 0.42f, dt);
            else if (Gang == "agbero") player.Play("Defend", (dyingT - 0.45f) / 0.92f, dt);
            else player.Play("Kneeling_Tired", (dyingT - 0.45f) / 0.79f, dt);
        }
        player.root.position = V.P(pp.x, pp.y, pp.z);
        player.Apply();

        // chasers: whoever controls the area runs in behind you
        if (chase <= 0 && chaserZ > 12 && !dying) crew = ZONES[zone].crew;
        float cz = caughtScene ? 0 : chase > 0 ? 1.5f : show > 0 ? 1.9f : 16;
        chaserZ += (cz - chaserZ) * Mathf.Min(1, dt * (chase > 0 || show > 0 ? 3 : 0.8f));
        chaserX = Mathf.Lerp(chaserX, x * 0.8f, Mathf.Min(1, dt * 5));
        float[][] spots = Gang == "agbero"
            ? new[] { new[] { -1.0f, 0.7f }, new[] { 1.0f, 0.7f }, new[] { -0.9f, -0.9f }, new[] { 0.9f, -0.9f } }   // surround the runner
            : new[] { new[] { -0.62f, 0.12f }, new[] { 0.62f, 0.12f }, new[] { 0f, 1f } };                           // one on each arm
        foreach (var kv in crews) {
            for (int i = 0; i < kv.Value.Count; i++) {
                Chaser m = kv.Value[i];
                GameObject mg = m.r.root.gameObject;
                if (kv.Key != Gang) { if (mg.activeSelf) mg.SetActive(false); continue; }
                if (!mg.activeSelf) { mg.SetActive(true); m.pos = new Vector3(Mathf.Clamp(chaserX + m.ox, -3.2f, 3.2f), 0, chaserZ + m.oz); }
                if (caughtScene) {
                    float sx = i < spots.Length ? spots[i][0] : m.ox, sz = i < spots.Length ? spots[i][1] : m.oz + 1;
                    float tX = x + sx, tZ = sz, k2 = Mathf.Min(1, dt * 5);
                    m.pos.x = Mathf.Lerp(m.pos.x, tX, k2); m.pos.z = Mathf.Lerp(m.pos.z, tZ, k2); m.pos.y = Mathf.Max(0, support);
                    bool far = new Vector2(m.pos.x - tX, m.pos.z - tZ).magnitude > 0.2f;
                    // run in, then turn on the runner: agberos lay into them, officers tell them off
                    string act = kv.Key == "agbero" ? (i == 1 ? "Punch_Cross" : i == 3 ? "Angry" : "Chop_Tree") : (i == 0 ? "Idle_Talking" : i == 1 ? "Angry" : "Reject");
                    if (kv.Key == "lastma") act = "Angry";
                    if (far) m.r.Animate(time * 14, "run", dt);
                    else { MotionClip mc = Moves.Get(act); m.r.Play(act, (time + i * 0.37f) / (mc != null ? mc.seconds : 1), dt); }
                    m.r.rootE = new Vector3(0, Mathf.Atan2(-(x - m.pos.x), -(0 - m.pos.z)), 0);
                } else {
                    m.r.rootE = Vector3.zero;
                    m.pos = new Vector3(Mathf.Clamp(chaserX + m.ox, -3.2f, 3.2f), Mathf.Max(0, support - 0.1f), chaserZ + m.oz);
                    m.r.Animate(phase * 1.05f + 1 + i * 0.9f, "run", dt, speed > 25);
                    if (m.armed) m.r.Over("Machete_Chop", time * 1.25f + i * 0.31f, 1, Runner.RIGHT_ARM);   // swinging the weapon as they come
                    else {
                        m.r.armOverride[1] = true;
                        m.r.armE[1] = new Vector3(2.5f + Mathf.Sin(time * 12 + i) * 0.3f, 0, 0);   // a fist in the air
                        m.r.elbow[1] = 0.6f;
                    }
                    m.r.look = V.P(x, y + 1.5f, 0); m.r.lookW = 1;
                }
                m.r.root.position = V.P(m.pos.x, m.pos.y, m.pos.z);
                m.r.Apply();
            }
        }

        // camera
        camY += (Mathf.Min(support, y) - camY) * Mathf.Min(1, dt * 4);
        Vector3 cp = new Vector3(x * 0.55f, camUp + camY * 0.8f, camBack), ct = new Vector3(x * 0.3f, 1.1f + camY * 0.8f, -9);
        if (dying && dyingT < 0.3f) cp.x += Mathf.Sin(dyingT * 80) * 0.12f;
        if (caughtScene) {
            float f = Mathf.Clamp01((dyingT - 0.2f) / 0.9f), e = f * f * (3 - 2 * f);
            float ang = 0.62f + Mathf.Max(0, dyingT - 1.1f) * 0.32f;   // swing round to the front, then drift slowly around the scene
            float side = x > 0.5f ? -1 : 1;                            // stay over the road, away from the nearer shop fronts
            cp = new Vector3(Mathf.Lerp(x * 0.55f, Mathf.Clamp(x + side * Mathf.Sin(ang) * 4.4f, -3.6f, 3.6f), e), Mathf.Lerp(camUp, 2.3f, e) + support, Mathf.Lerp(camBack, -Mathf.Cos(ang) * 4.4f, e));
            ct = new Vector3(x, support + 0.9f, 0);
        }
        Aim(cp, ct);

        if (playing) PaintHud();
    }

    // Development aid: `-lrshots <folder>` plays a scripted tour and saves screenshots, then quits.
    IEnumerator Shots(string dir) {
        System.IO.Directory.CreateDirectory(dir);
        System.Func<string, float, IEnumerator> snap = null;
        int n = 0;
        snap = (name, wait) => SnapAfter(dir + "/" + (++n).ToString("00") + "-" + name + ".png", wait);
        yield return snap("menu", 2);
        OpenChars(); yield return snap("characters", 1);
        foreach (string id in new[] { "odogwu", "basirat", "okada", "kemi", "amaka", "konda" }) { if (!owned.Contains(id)) owned.Add(id); PickChar(id); yield return snap("char-" + id, 0.6f); }
        PickChar("tunde"); CloseChars();
        StartRun(); yield return snap("start", 1.5f);
        invuln = 999; Act("right"); yield return snap("run", 3);
        Act("up"); yield return snap("jump", 0.35f);
        yield return new WaitForSecondsRealtime(0.8f); Act("down"); yield return snap("slide", 0.3f);
        power["magnet"] = 10; power["double"] = 12; chase = 7; yield return snap("chase", 1.2f);
        // the people of the road, kept just ahead of the runner so they stay in the picture
        ClearRun(); nextRow = distance + 400; chase = 0; keepPace = true;
        foreach (Ob o in new[] { AddObstacle('W', 1, distance + 9), AddObstacle('S', 0, distance + 8), AddObstacle('S', 0, distance + 5.2f), AddObstacle('H', 2, distance + 7) }) o.cur = speed;
        Act("left"); yield return snap("street", 2.5f);
        yield return snap("street-2", 0.7f);
        keepPace = false; yield return snap("street-passing", 0.25f);
        ClearRun(); distance = 2990; nextRow = distance + 40; yield return snap("bridge", 4);
        ClearRun(); distance = 4500; nextRow = distance + 40; yield return snap("victoria-island", 3);
        // the other towns: the menu, the start, a market and the bridge
        float[][] stops = { null, new[] { 1900f, 4450f }, new[] { 1300f, 5300f }, new[] { 4000f, 6050f } };
        for (int c = 1; c < CITIES.Length; c++) {
            mode = "menu"; ShowScreen(startScreen); SetCity(c);
            string tag = CITIES[c].name.ToLowerInvariant().Replace(' ', '-');
            yield return snap(tag + "-menu", 1.5f);
            StartRun(); invuln = 999; yield return snap(tag + "-start", 1.6f);
            ClearRun(); distance = stops[c][0]; nextRow = distance + 40; yield return snap(tag + "-market", 3.2f);
            ClearRun(); distance = stops[c][1]; nextRow = distance + 40; yield return snap(tag + "-bridge", 3.2f);
        }
        chase = 0; yield return new WaitForSecondsRealtime(0.3f);
        caught = true; invuln = 0; Die(CREWS[crew].caught); yield return snap("caught", 2.0f);
        yield return snap("caught-later", 1.6f);
        yield return snap("over", 1.5f);
        Application.Quit();
    }
    // Development aid: `-lrlab <folder>` saves a picture per move with eight copies of the runner lined up, each a
    // moment later than the one to its left: first every clip on its own, then the moves as the game plays them.
    IEnumerator Lab(string dir) {
        System.IO.Directory.CreateDirectory(dir);
        yield return new WaitForSecondsRealtime(1);
        mode = "lab"; ShowScreen(null);
        player.root.gameObject.SetActive(false);
        foreach (Seg s in segments) s.crowd.SetActive(false);
        new Shape().Quad(60, 30, T.Lam(0x6f7a86), V.M(0, 0, -2.5f)).Make("backdrop");
        Runner[] row = new Runner[8];
        for (int i = 0; i < row.Length; i++) row[i] = People.Spawn(CharById("tunde").look, "lab");
        cam.fieldOfView = 26;
        Aim(new Vector3(0, 1.25f, 7.1f), new Vector3(0, 1.25f, 0));
        foreach (string name in LabClips) {
            MotionClip c = Moves.Get(name);
            if (c == null) continue;
            for (int view = 0; view < 2; view++) {
                for (int i = 0; i < row.Length; i++) {
                    Runner r = row[i];
                    r.Stand(); r.rootE = new Vector3(0, view == 0 ? PI / 2 : PI, 0);
                    r.Play(name, c.loop ? i / (float)row.Length : i / (float)(row.Length - 1), 1);
                    r.root.position = V.P(-4.2f + i * 1.2f, 0, 0);
                    r.Apply();
                }
                yield return SnapAfter(dir + "/clip-" + name + (view == 0 ? "-side" : "-front") + ".png", 0.05f);
            }
        }
        // the moves in play: what, how long to show, and how high the picture has to reach
        Aim(new Vector3(0, 2.1f, 10.6f), new Vector3(0, 2.1f, 0));
        foreach (string what in new[] { "jump:1.0", "slide:1.25", "hit:0.7", "lane:0.45", "drop:0.7" }) {
            string[] w = what.Split(':'); float len = float.Parse(w[1], System.Globalization.CultureInfo.InvariantCulture);
            for (int view = 0; view < 3; view++) {
                for (int i = 0; i < row.Length; i++) LabPlay(row[i], w[0], len * i / (row.Length - 1), view == 0 ? PI / 2 : view == 1 ? PI : 0, -5.6f + i * 1.6f);
                yield return SnapAfter(dir + "/play-" + w[0] + (view == 0 ? "-side" : view == 1 ? "-front" : "-back") + ".png", 0.05f);
            }
        }
        // close on the hands, and the men who give chase
        foreach (Runner r in row) r.root.gameObject.SetActive(false);
        Runner[] crew = new Runner[8];
        for (int i = 0; i < crew.Length; i++) {
            Runner r = crew[i] = People.Spawn(new Person { model = "Naija", hat = "cap", hatColor = 0xc8452e, prop = i % 2 == 0 ? "machete" : "stick" }, "lab");
            r.root.position = V.P(-5.6f + i * 1.6f, 0, 0);
        }
        for (int view = 0; view < 2; view++) {
            for (int i = 0; i < crew.Length; i++) {
                Runner r = crew[i];
                r.rootE = new Vector3(0, view == 0 ? PI / 2 : PI, 0);
                r.Animate(i * 0.8f, "run", 1);
                r.Over("Machete_Chop", i / (float)crew.Length, 1, Runner.RIGHT_ARM);
                r.Apply();
            }
            yield return SnapAfter(dir + "/chasers" + (view == 0 ? "-side" : "-front") + ".png", 0.05f);
        }
        cam.fieldOfView = 14;
        Aim(new Vector3(-5.0f, 1.25f, 5), new Vector3(-5.0f, 1.2f, 0));
        yield return SnapAfter(dir + "/hands.png", 0.05f);
        Application.Quit();
    }
    // a second or so of the game's own running, jumping and sliding on a spare runner, stopped at the moment asked for
    void LabPlay(Runner r, string what, float until, float facing, float px) {
        r.ResetPose();
        float ly = what == "drop" ? 2.6f : 0, lvy = 0, lslide = 0, lphase = 0, side = 0, step = 1 / 60f;
        for (float t = -0.5f; t < until; t += step) {
            if (t >= 0 && t < step) {
                if (what == "jump") lvy = JUMP_V;
                if (what == "slide") lslide = 0.75f;
                if (what == "hit") r.Flinch();
                if (what == "lane") side = -2.2f;
            }
            if (t >= 0 || what != "drop") { lvy += GRAVITY * step; ly += lvy * step; }
            bool onGround = ly <= 0;
            if (onGround) { ly = 0; lvy = Mathf.Max(0, lvy); }
            if (what == "drop" && t < 0) onGround = true;
            if (lslide > 0) lslide -= step;
            side += (0 - side) * Mathf.Min(1, step * 14);
            lphase += step * 20 * 0.55f;
            r.Animate(lphase, !onGround ? "air" : lslide > 0 ? "slide" : "run", step, false, lvy);
            r.rootE = new Vector3(0, facing, 0);
            r.Swerve(side / 2.2f, step);
            r.look = V.P(px, ly + 1.5f, -30); r.lookW = facing == 0 ? 1 : 0;
            r.root.position = V.P(px + (what == "lane" ? side : 0), ly, 0);
            r.Apply();
        }
    }
    static readonly string[] LabClips = { "Run", "Sprint", "Machete_Chop", "Machete_Slash", "Angry", "Insult", "Cheer_One_arm", "Cheering_Two_Hands", "Help_Two_Arms", "Greeting", "Victory", "Reject",
        "Punch_Cross", "Chop_Tree", "Defend", "Kneeling_Tired", "Idle_A", "Idle_Talking", "Idle_FoldArms", "Yes", "Death_B", "Death_D" };
    IEnumerator SnapAfter(string path, float wait) {
        yield return new WaitForSecondsRealtime(wait);
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(path);
        yield return null; yield return null;
    }
}
