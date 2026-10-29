using System.Collections;
using UnityEngine;

// People who share the road: a man pushing a child along in a wheelbarrow, a girl and a boy on roller blades, and a
// kid rolling a bucket cover with a stick. They go the same way as the runner, only slower, so you come up behind
// them; running into one is a stumble, not a crash.
public partial class LRGame {
    static readonly Person[] PUSHERS = {
        new Person { model = "Naija", dress = new[] { "tankshirt", "#d9d4c4", "cargo", "x#7a6a50" }, hat = "cap", hatColor = 0x2b4f7a },
        new Person { model = "Dayo", dress = new[] { "camotee", "#2e7d5a" }, hat = "bandana", hatColor = 0xc8452e },
        new Person { model = "Baba", dress = new[] { "shirt", "#8f3b2a", "trouser", "x#6b6b6b" } },
    };
    static readonly Person[] RIDERS = {
        new Person { model = "Tunde", scale = 0.7f, headScale = 1.22f, dress = new[] { "suit", "#f5c518", "afro", "hide" } },
        new Person { model = "Zainab", scale = 0.7f, headScale = 1.22f, dress = new[] { "suit", "orange" } },
        new Person { model = "Tunde", scale = 0.7f, headScale = 1.22f, dress = new[] { "suit", "#d64f8f", "afro", "hide" }, hat = "cap", hatColor = 0xf4f4f4 },
    };
    // a girl, then a boy, turn and turn about
    static readonly Person[] SKATERS = {
        new Person { model = "Amaka", scale = 0.86f, headScale = 1.08f, dress = new[] { "tank", "#e6399b", "pants", "#1f1f2a" }, hat = "helmet", hatColor = 0xffd23f, skates = 0xff5fa2 },
        new Person { model = "Tunde", scale = 0.86f, headScale = 1.08f, dress = new[] { "suit", "#1f6fd0", "afro", "hide" }, hat = "helmet", hatColor = 0xd22d2d, skates = 0x30c5d2 },
    };
    static readonly Person[] HOOP_KIDS = {
        new Person { model = "Tunde", scale = 0.68f, headScale = 1.25f, dress = new[] { "suit", "#c8452e", "afro", "hide" } },
        new Person { model = "Tunde", scale = 0.68f, headScale = 1.25f, dress = new[] { "suit", "#0a8a4a", "afro", "hide" } },
        new Person { model = "Zainab", scale = 0.68f, headScale = 1.25f, dress = new[] { "suit", "#f5c518" } },
    };
    Shape barrow, barrowWheel, hoopStick; Shape[] lids;
    int skaterTurn; bool keepPace;

    // a square bar from one point to another (in the game's coordinates)
    static void Bar(Shape g, float ax, float ay, float az, float bx, float by, float bz, float thick, Material m) {
        Vector3 a = V.P(ax, ay, az), b = V.P(bx, by, bz);
        g.BoxM(thick, thick, (b - a).magnitude, m, Matrix4x4.TRS((a + b) / 2, Quaternion.LookRotation(b - a), Vector3.one));
    }
    void BuildStreetShapes() {
        Material paint = T.Lam(0x2e7d4f), rimPaint = T.Lam(0x1f5a38), grip = T.Lam(0x1b1b1b), hub = T.Lam(0xd22d2d);
        // wheelbarrow, nose up the road: wheel at the front, tray in the middle, handles back to the pusher's hands
        barrow = new Shape();
        foreach (float s in new[] { -1f, 1f }) {
            Bar(barrow, s * 0.1f, 0.2f, -1.05f, s * 0.3f, 0.84f, 0.85f, 0.04f, T.metal);
            Bar(barrow, s * 0.284f, 0.789f, 0.7f, s * 0.302f, 0.847f, 0.87f, 0.058f, grip);
            Bar(barrow, s * 0.25f, 0.52f, 0.12f, s * 0.27f, 0, 0.3f, 0.035f, T.metal);
            barrow.Box(0.03f, 0.27f, 0.93f, paint, s * 0.315f, 0.655f, -0.38f);
            barrow.Box(0.05f, 0.03f, 0.95f, rimPaint, s * 0.315f, 0.795f, -0.38f);
        }
        barrow.Box(0.63f, 0.04f, 0.93f, paint, 0, 0.52f, -0.38f);
        barrow.Box(0.66f, 0.3f, 0.03f, paint, 0, 0.67f, -0.845f);
        barrow.Box(0.66f, 0.16f, 0.03f, paint, 0, 0.6f, 0.085f);
        barrow.Box(0.5f, 0.035f, 0.035f, T.metal, 0, 0.3f, 0.23f);
        barrowWheel = new Shape();
        barrowWheel.Cyl(0.2f, 0.2f, 0.09f, 16, T.tyre, 0, 0, 0, 0, 0, PI / 2);
        barrowWheel.Cyl(0.075f, 0.075f, 0.11f, 10, hub, 0, 0, 0, 0, 0, PI / 2);
        barrowWheel.Box(0.1f, 0.3f, 0.035f, hub); barrowWheel.Box(0.1f, 0.035f, 0.3f, hub);

        // bucket covers: a bright plastic lid with a moulded handle across it, so the spin shows
        int[] colours = { 0x2f6fd0, 0xd22d2d, 0x2e9a5a, 0xf08c1a };
        lids = new Shape[colours.Length];
        for (int i = 0; i < lids.Length; i++) {
            Color c = T.H(colours[i]);
            Material face = T.Lam(colours[i]), edge = T.Lam(Mathf.RoundToInt(c.r * 150) << 16 | Mathf.RoundToInt(c.g * 150) << 8 | Mathf.RoundToInt(c.b * 150));
            lids[i] = new Shape();
            lids[i].Cyl(0.26f, 0.26f, 0.035f, 22, edge, 0, 0, 0, 0, 0, PI / 2);
            lids[i].Cyl(0.235f, 0.235f, 0.046f, 22, face, 0, 0, 0, 0, 0, PI / 2);
            lids[i].Box(0.075f, 0.055f, 0.3f, edge);
            lids[i].Box(0.056f, 0.07f, 0.07f, T.Lam(0xf4f4f4), 0, 0.15f, 0);
        }
        hoopStick = new Shape().BoxM(0.024f, 0.024f, 1, T.wood, Matrix4x4.Translate(new Vector3(0, 0, 0.5f)));
    }

    void BuildFolk(Ob o) {
        if (barrow == null) BuildStreetShapes();
        o.obj = new GameObject("street");
        Transform t = o.obj.transform;
        o.ph = Rand(0, 6);
        if (o.type == 'W') {
            barrow.Make("barrow", t);
            o.wheel = barrowWheel.Make("wheel", t).transform; o.wheel.localPosition = V.P(0, 0.2f, -1.05f);
            Runner man = People.Spawn(Pick(PUSHERS), "pusher", t), kid = People.Spawn(Pick(RIDERS), "rider", t);
            man.root.localPosition = V.P(0, 0, 0.98f); man.onFloor = true; man.hold[0] = man.hold[1] = 0.95f;
            kid.root.localPosition = V.P(0, 0.64f - kid.HipHeight, -0.16f); kid.hold[0] = kid.hold[1] = 0.8f;
            o.folk = new[] { man, kid };
        } else if (o.type == 'S') {
            Runner r = People.Spawn(SKATERS[skaterTurn++ % 2], "skater", t);
            r.root.localPosition = new Vector3(0, People.SKATE * r.root.localScale.y, 0); r.onFloor = true;
            o.folk = new[] { r };
        } else {
            Runner kid = People.Spawn(Pick(HOOP_KIDS), "hoop kid", t);
            kid.root.localPosition = V.P(-0.05f, 0, 0.4f); kid.onFloor = true; kid.hold[1] = 0.95f;
            o.lid = Pick(lids).Make("lid", t).transform;
            o.stick = hoopStick.Make("stick", t).transform;
            o.folk = new[] { kid };
        }
    }

    // how fast they are going: they set off as the runner comes up behind, and hold back from whatever is in the road ahead
    void Pace(Ob o, float dt) {
        float want = keepPace ? speed : o.v * Mathf.Clamp01((o.z + 70) / 8);
        if (!keepPace) foreach (Ob b in obstacles) if (b.folk == null && b.a > o.a + 1 && b.a - o.a < 15) want *= Mathf.Clamp01((b.a - o.a - 8) / 7);
        o.cur = Mathf.MoveTowards(o.cur, want, dt * 6);
        o.vRel = -o.cur;
    }

    void Liven(Ob o, float dt) {
        bool near = o.z > -npcRange && o.z < 14;
        if (o.obj.activeSelf != near) o.obj.SetActive(near);
        if (!near) return;
        float roll = Mathf.Min(o.cur, o.v);   // on a tour they are carried along at the runner's speed; the legs keep their own
        o.ph += roll * dt;
        float go = Mathf.Clamp01(roll / 1.2f);
        bool passing = mode == "play" && o.z > -22 && o.z < 3;
        Transform t = o.obj.transform;
        if (o.type == 'W') {
            Runner man = o.folk[0], kid = o.folk[1];
            o.wheel.localRotation = Quaternion.Euler(o.ph / 0.2f * Mathf.Rad2Deg, 0, 0);
            if (go > 0.3f) man.Play("Jog", o.ph / 2.6f, dt, 0.3f); else man.Play("Idle_A", time / 3.12f, dt, 0.3f);
            man.Apply();
            for (int i = 0; i < 2; i++) man.Reach(i, t.TransformPoint(V.P(i == 0 ? -0.3f : 0.3f, 0.84f, 0.8f)));
            // the passenger sits with knees up, one hand on the side, and punches the air as the runner comes by
            kid.Pose(dt);
            float jolt = Mathf.Sin(o.ph * 5) * 0.04f * go;
            for (int i = 0; i < 2; i++) { kid.legE[i] = new Vector3(1.15f + jolt, 0, 0); kid.knee[i] = 0.95f; }
            kid.spineE = new Vector3(0.1f - jolt, 0, 0);
            if (passing) { kid.armE[1] = new Vector3(2.6f + Mathf.Sin(time * 11) * 0.3f, 0, 0); kid.elbow[1] = 0.5f; }
            kid.Apply();
            kid.Reach(0, t.TransformPoint(V.P(-0.315f, 0.81f, -0.42f)));
            if (!passing) kid.Reach(1, t.TransformPoint(V.P(0.315f, 0.81f, -0.42f)));
        } else if (o.type == 'S') {
            Runner r = o.folk[0];
            float stroke = o.ph * 1.3f;
            o.dx = Mathf.Sin(stroke) * 0.22f * go;
            if (go > 0.15f) Skate(r, stroke, go, dt); else r.Play("Idle_A", time / 3.12f, dt, 0.3f);
            r.Apply();
        } else {
            Runner kid = o.folk[0];
            if (go > 0.3f) kid.Play("Jog", o.ph / 1.9f, dt, 0.3f); else kid.Play("Idle_A", time / 3.12f, dt, 0.3f);
            kid.Apply();
            float wob = Mathf.Sin(o.ph * 1.7f) * go;
            o.lid.localPosition = V.P(0.3f + wob * 0.05f, 0.26f, -0.75f);
            o.lid.localRotation = Quaternion.Euler(0, wob * 5, wob * 7) * Quaternion.Euler(o.ph / 0.26f * Mathf.Rad2Deg, 0, 0);
            kid.Reach(1, t.TransformPoint(V.P(0.27f, 0.56f, 0.02f)));
            // the stick runs from the hand to the back of the lid, pushing it along
            Vector3 to = t.TransformPoint(V.P(0.3f + wob * 0.05f, 0.4f, -0.55f)), from = kid.Hand(1), dir = (to - from).normalized;
            from -= dir * 0.1f;
            o.stick.position = from; o.stick.rotation = Quaternion.LookRotation(to - from); o.stick.localScale = new Vector3(1, 1, (to - from).magnitude);
        }
    }

    // roller blading: crouched forward, each leg in turn pushing out to the side and back while the other glides
    static void Skate(Runner r, float stroke, float go, float dt) {
        r.Pose(dt);
        float s = Mathf.Sin(stroke);
        for (int i = 0; i < 2; i++) {
            float sd = i == 0 ? 1 : -1, push = Mathf.Max(0, s * sd) * go, glide = Mathf.Max(0, -s * sd) * go;
            r.legE[i] = new Vector3(0.12f + 0.3f * glide - 0.5f * push, 0, -sd * 0.36f * push);
            r.knee[i] = 0.45f + 0.3f * glide - 0.3f * push;
            r.armE[i] = new Vector3(0.75f * s * sd * go, 0, sd * 0.15f);
            r.elbow[i] = 0.75f;
        }
        r.spineE = new Vector3(-0.36f * go, 0, -s * 0.08f);
        r.headE = new Vector3(0.28f * go, 0, 0);
        r.rootE = new Vector3(0, -s * 0.12f * go, s * 0.07f * go);
    }

    // Development aid: `-lrstreet <folder>` lines up the people of the road, four of each a moment apart, seen from
    // the side, the front, behind and above.
    IEnumerator StreetLab(string dir) {
        System.IO.Directory.CreateDirectory(dir);
        yield return new WaitForSecondsRealtime(1);
        mode = "lab"; ShowScreen(null);
        player.root.gameObject.SetActive(false);
        foreach (Seg s in segments) s.crowd.SetActive(false);
        new Shape().Quad(60, 30, T.Lam(0x6f7a86), V.M(0, 0, -4)).Make("backdrop");
        cam.fieldOfView = 26;
        foreach (char type in new[] { 'W', 'S', 'H' }) {
            Ob[] row = new Ob[4];
            for (int i = 0; i < row.Length; i++) {
                Ob o = row[i] = AddObstacle(type, 1, 0);
                o.z = -5; o.cur = i == 3 ? 0 : o.v;
                for (int k = 0; k < 40 + i * 23; k++) Liven(o, 1 / 60f);
            }
            string[] views = { "side", "front", "back", "above" };
            for (int v = 0; v < views.Length; v++) {
                for (int i = 0; i < row.Length; i++) {
                    row[i].obj.transform.position = V.P(-4.8f + i * 3.2f, 0, 0);
                    row[i].obj.transform.rotation = V.R(0, v == 0 ? PI / 2 : v == 1 ? PI : v == 2 ? 0 : 0.6f, 0);
                    Liven(row[i], 0.0001f);
                }
                if (v == 3) Aim(new Vector3(0, 5.5f, 10.5f), new Vector3(0, 0.8f, 0)); else Aim(new Vector3(0, 1.1f, 11.5f), new Vector3(0, 1.0f, 0));
                yield return SnapAfter(dir + "/street-" + type + "-" + views[v] + ".png", 0.05f);
            }
            ClearRun();
            yield return null;
        }
        Application.Quit();
    }
}
