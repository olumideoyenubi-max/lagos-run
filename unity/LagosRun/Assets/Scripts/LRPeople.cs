using System.Collections.Generic;
using UnityEngine;

// Who a person is: which MakeHuman model, how the clothes are recoloured, and what they wear or carry.
public class Person {
    public string model;
    public float scale = 1, headScale = 1;
    // pairs of (part of a material name, change). A change is "x#rrggbb" to tint, "#rrggbb" for a flat colour,
    // "hide" to remove the piece, or the name of an ankara print.
    public string[] dress = new string[0];
    public string hat, prop, pose = "stand";
    public int hatColor, backpack = -1, tray = -1, skates = -1;   // skates: the colour of a pair of roller blades
    public Material hatMat;
    public bool shades;
}

// Motion clips exported from Blender (see Tools/export_motion.py): for every sample, each bone's rotation away from a
// reference pose, expressed in the character's own space, so one clip plays on every body whatever its proportions.
[System.Serializable] public class MotionClip {
    public string name; public int samples; public bool loop, standRef; public float seconds;
    public float[] q, pelvis, arms;
    [System.NonSerialized] public int[] slot;   // for each bone in Moves.Bones, its index in this clip (or -1)
}
[System.Serializable] public class MotionFile { public string[] bones; public MotionClip[] clips; }
public static class Moves {
    public static string[] Bones;
    static readonly Dictionary<string, MotionClip> clips = new Dictionary<string, MotionClip>();
    public static void Load() {
        // later files win when two have a clip of the same name
        foreach (string path in new[] { "Motion/m2m_base", "Motion/m2m_addon", "Motion/m2m_mocap", "Motion/naija", "Motion/pack" }) {
            MotionFile f = JsonUtility.FromJson<MotionFile>(Resources.Load<TextAsset>(path).text);
            if (Bones == null) Bones = f.bones;
            foreach (MotionClip c in f.clips) {
                c.slot = new int[Bones.Length];
                for (int k = 0; k < Bones.Length; k++) c.slot[k] = System.Array.IndexOf(f.bones, Bones[k]);
                clips[c.name] = c;
            }
        }
    }
    public static MotionClip Get(string name) { MotionClip c; return name != null && clips.TryGetValue(name, out c) ? c : null; }
}

// A skinned person. The body follows motion clips (run, walk, idle, dance...), several at once when one has to be laid
// over another (a landing over the run, a weapon arm over running legs), and can blend into a posed stance. On top of
// the clips it closes the hands, turns the head toward a point of interest and keeps the feet out of the ground.
// Pose angles use the game's convention: for a limb, x swings it forward (positive) or back, z swings it sideways;
// knee and elbow are how far the joint is bent.
public class Runner {
    public Transform root;
    public GameObject cuffs;
    public Vector3 rootE, headE, spineE;
    public readonly Vector3[] legE = new Vector3[2], armE = new Vector3[2];
    public readonly float[] knee = new float[2], elbow = new float[2];
    public readonly bool[] armOverride = new bool[2];   // pose this arm by hand even while a clip plays (a raised weapon)
    public float hips, seed;
    public string pose = "Idle_A", clip;
    public float clipT, posed = 1;                       // posed: 0 = follow the clip, 1 = hold the pose
    public bool carry;                                   // holding something against the body with the left arm
    public readonly float[] hold = new float[2];         // how tightly each hand (left, right) grips what it carries: 0 nothing, 1 a fist
    public float grip = 0.3f;                            // how far the empty hands are closed
    public Vector3 look; public float lookW;             // a point in the world the head turns to, and how strongly (0 = not at all)
    public bool onFloor;                                 // always stand the lower foot on the ground, however the legs are bent
    public const int ALL = 0, UPPER = 1, RIGHT_ARM = 2;  // the part of the body a clip laid over another may move
    const float FADE = 0.22f;                            // seconds to blend from one clip into the next
    const float SLIDE_IN = 0.24f, SLIDE_OUT = 0.3f, LAND = 0.32f, HIT = 0.55f;

    struct Layer { public MotionClip c; public float t, w; public int part; }
    struct Finger { public int k; public Vector3 axis; public float max; }
    readonly Layer[] layers = new Layer[4]; int nLayers;
    string key, shownKey, was = "run";
    float fade, fadeLen = FADE, fadeFor = FADE, dt, clock;
    float slideT = 9, landT = 9, exitT = 9, hitT = 9, v0 = 12, lastVy, impact, sprintW, swerve, lookYaw, lookPitch;
    bool planted;
    readonly float[] gripS = { 0.3f, 0.3f }, curl = new float[2];
    Quaternion[] frozen, shown; Vector3 frozenLift, shownLift;

    static readonly string[] Names = { "pelvis", "spine_01", "spine_02", "spine_03", "neck_01", "head", "upperarm_l", "lowerarm_l", "upperarm_r", "lowerarm_r", "thigh_l", "calf_l", "thigh_r", "calf_r" };
    const int PELVIS = 0, SPINE = 1, NECK = 4, HEAD = 5, ARM = 6, LEG = 10;
    const float ARM_REST = 0.72f;   // the models stand with arms held out; this brings them down to the sides
    readonly Transform[] bones = new Transform[Names.Length];
    readonly Transform[] mb; readonly Quaternion[] bindLocal, bindRoot, saved;
    readonly bool[] isArm; readonly bool[][] inPart = new bool[3][];
    readonly List<int>[] armChain = { new List<int>(), new List<int>() };
    readonly List<Finger>[] fingers = { new List<Finger>(), new List<Finger>() };
    readonly Transform[][] aim = new Transform[2][];
    readonly Transform[] foot = new Transform[2];
    readonly Transform pelvis; readonly Vector3 pelvisPos; readonly float pelvisHeight, footRest;
    Quaternion rr, inv;

    public Runner(Transform root, Dictionary<string, Transform> byName) {
        this.root = root;
        for (int i = 0; i < Names.Length; i++) bones[i] = byName[Names[i]];
        int n = Moves.Bones.Length;
        mb = new Transform[n]; bindLocal = new Quaternion[n]; bindRoot = new Quaternion[n]; saved = new Quaternion[n]; isArm = new bool[n];
        for (int p = 0; p < 3; p++) inPart[p] = new bool[n];
        Quaternion r0 = Quaternion.Inverse(root.rotation);
        for (int k = 0; k < n; k++) {
            string name = Moves.Bones[k];
            if (!byName.TryGetValue(name, out mb[k])) continue;
            bindLocal[k] = mb[k].localRotation; bindRoot[k] = r0 * mb[k].rotation;
            bool hand = name.StartsWith("hand_") || name.StartsWith("index_") || name.StartsWith("middle_") || name.StartsWith("ring_") || name.StartsWith("pinky_") || name.StartsWith("thumb_");
            isArm[k] = hand || name.StartsWith("clavicle_") || name.StartsWith("upperarm_") || name.StartsWith("lowerarm_");
            if (isArm[k] && !name.StartsWith("clavicle_")) armChain[name.EndsWith("_l") ? 0 : 1].Add(k);
            inPart[ALL][k] = true;
            inPart[UPPER][k] = isArm[k] || name == "spine_02" || name == "spine_03" || name == "neck_01" || name == "head";
            inPart[RIGHT_ARM][k] = isArm[k] && name.EndsWith("_r");
        }
        for (int i = 0; i < 2; i++) {
            string sd = i == 0 ? "_l" : "_r";
            aim[i] = new[] { byName["upperarm" + sd], byName["lowerarm" + sd], byName["hand" + sd], byName["middle_01" + sd] };
            foot[i] = byName["foot" + sd];
            // fingers close toward the palm, which faces the ground while the model stands with its arms out
            Vector3 h = aim[i][2].position, palm = Vector3.Cross(byName["index_01" + sd].position - h, byName["pinky_01" + sd].position - h).normalized;
            if (palm.y > 0) palm = -palm;
            foreach (string f in new[] { "index", "middle", "ring", "pinky", "thumb" }) for (int j = 1; j <= 3; j++) {
                int k = System.Array.IndexOf(Moves.Bones, f + "_0" + j + sd);
                if (k < 0 || mb[k] == null) continue;
                Transform next; byName.TryGetValue(f + "_0" + (j + 1) + sd, out next);
                Vector3 along = j < 3 && next != null ? next.position - mb[k].position : mb[k].position - mb[k].parent.position;
                Vector3 axis = Vector3.Cross(along, palm).normalized;
                if (Vector3.Dot(Quaternion.AngleAxis(10, axis) * along, palm) < 0) axis = -axis;
                float most = f == "thumb" ? (j == 1 ? 12 : 30) : j == 1 ? 72 : j == 2 ? 88 : 58;
                fingers[i].Add(new Finger { k = k, axis = Quaternion.Inverse(mb[k].rotation) * axis, max = most });
            }
        }
        pelvis = byName["pelvis"]; pelvisPos = pelvis.localPosition; pelvisHeight = pelvis.position.y - root.position.y;
        footRest = Mathf.Min(foot[0].position.y, foot[1].position.y) - root.position.y;
        frozen = new Quaternion[n]; shown = (Quaternion[])bindLocal.Clone();
    }
    void Turn(int i, Quaternion q) { bones[i].rotation = rr * q * inv * bones[i].rotation; }

    public void Apply() {
        root.localRotation = V.R(rootE);
        rr = root.rotation; inv = Quaternion.Inverse(rr);
        pelvis.localPosition = pelvisPos;
        MotionClip c = posed < 0.5f ? Moves.Get(clip) : null;
        string now = c == null ? "pose" : key ?? clip;
        if (now != shownKey) {   // a new move: hold on to the pose on screen and blend away from it
            if (shownKey != null) { System.Array.Copy(shown, frozen, shown.Length); frozenLift = shownLift; fade = fadeLen = fadeFor; }
            shownKey = now;
        }
        Vector3 lift = Vector3.zero;
        curl[0] = curl[1] = 1;   // how much of each hand is left for the fingers to close; clips that pose the fingers keep them
        if (c == null) ApplyPose();
        else {
            lift = ApplyClip(c, clipT, ALL);
            curl[0] = curl[1] = c.standRef ? 1 : 0;
            for (int n = 0; n < nLayers; n++) {
                Layer l = layers[n]; bool[] on = inPart[l.part];
                for (int k = 0; k < mb.Length; k++) if (on[k] && mb[k] != null) saved[k] = mb[k].localRotation;
                Vector3 lift2 = ApplyClip(l.c, l.t, l.part);
                for (int k = 0; k < mb.Length; k++) if (on[k] && mb[k] != null) mb[k].localRotation = Quaternion.Slerp(saved[k], mb[k].localRotation, l.w);
                if (l.part == ALL) lift = Vector3.Lerp(lift, lift2, l.w);
                for (int i = 0; i < 2; i++) if (l.part != RIGHT_ARM || i == 1) curl[i] = Mathf.Lerp(curl[i], l.c.standRef ? 1 : 0, l.w);
            }
            for (int i = 0; i < 2; i++) {
                if (!armOverride[i]) continue;
                foreach (int k in armChain[i]) mb[k].localRotation = bindLocal[k];
                PoseArm(i); curl[i] = 1;
            }
        }
        for (int i = 0; i < 2; i++) {
            gripS[i] += (Mathf.Max(hold[i], grip) - gripS[i]) * Mathf.Min(1, dt * 12);
            float g = gripS[i] * curl[i];
            if (g > 0.01f) foreach (Finger f in fingers[i]) mb[f.k].localRotation = mb[f.k].localRotation * Quaternion.AngleAxis(f.max * g, f.axis);
        }
        if (fade > 0) {
            fade -= dt;
            float w = Mathf.SmoothStep(0, 1, 1 - Mathf.Max(0, fade) / fadeLen);
            for (int k = 0; k < mb.Length; k++) if (mb[k] != null) mb[k].localRotation = Quaternion.Slerp(frozen[k], mb[k].localRotation, w);
            lift = Vector3.Lerp(frozenLift, lift, w);
        }
        for (int k = 0; k < mb.Length; k++) if (mb[k] != null) shown[k] = mb[k].localRotation;
        shownLift = lift;

        // the head, and a little of the neck and chest, turn toward what the person is watching
        float wantYaw = 0, wantPitch = 0;
        if (lookW > 0.01f) {
            Vector3 to = inv * (look - bones[HEAD].position);
            float yaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg, pitch = -Mathf.Atan2(to.y, new Vector2(to.x, to.z).magnitude) * Mathf.Rad2Deg;
            float w = lookW * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(80, 135, Mathf.Abs(yaw))));   // let go of what has passed behind
            wantYaw = Mathf.Clamp(yaw, -70, 70) * w; wantPitch = Mathf.Clamp(pitch, -25, 30) * w;
        }
        float ease = Mathf.Min(1, dt * 7);
        lookYaw += (wantYaw - lookYaw) * ease; lookPitch += (wantPitch - lookPitch) * ease;
        if (Mathf.Abs(lookYaw) + Mathf.Abs(lookPitch) > 0.2f) {
            Quaternion q = Quaternion.Euler(lookPitch, lookYaw, 0);
            Turn(SPINE + 2, Quaternion.Slerp(Quaternion.identity, q, 0.2f));
            Turn(NECK, Quaternion.Slerp(Quaternion.identity, q, 0.3f));
            Turn(HEAD, Quaternion.Slerp(Quaternion.identity, q, 0.5f));
        }

        float size = root.lossyScale.y, ground = root.position.y;
        pelvis.position += rr * lift * (pelvisHeight * size);
        if (planted || onFloor) {   // blended poses can push a foot through the road: stand the body back up on it
            float sunk = ground + (footRest - 0.015f) * size - Mathf.Min(foot[0].position.y, foot[1].position.y);
            if (sunk > 0 || onFloor) pelvis.position += Vector3.up * sunk;
        }
        // lying down (sliding, knocked over), keep the hips above the road rather than in it
        float floor = ground + 0.16f * size;
        if (pelvis.position.y < floor) pelvis.position = new Vector3(pelvis.position.x, floor, pelvis.position.z);
    }
    Vector3 ApplyClip(MotionClip c, float t, int part) {
        float x = Mathf.Repeat(t, 1) * c.samples;
        if (!c.loop) x = Mathf.Clamp01(t) * (c.samples - 1);
        int i0 = Mathf.Min((int)x, c.samples - 1), i1 = c.loop ? (i0 + 1) % c.samples : Mathf.Min(i0 + 1, c.samples - 1);
        float f = x - i0;
        int stride = c.q.Length / c.samples;
        bool[] on = inPart[part];
        for (int k = 0; k < mb.Length; k++) {
            if (mb[k] == null || !on[k]) continue;
            int ci = c.slot[k];
            if (ci < 0 || (c.standRef && isArm[k])) { mb[k].localRotation = bindLocal[k]; continue; }
            int a = i0 * stride + ci * 4, b = i1 * stride + ci * 4;
            Quaternion q = Quaternion.Slerp(new Quaternion(c.q[a], c.q[a + 1], c.q[a + 2], c.q[a + 3]), new Quaternion(c.q[b], c.q[b + 1], c.q[b + 2], c.q[b + 3]), f);
            mb[k].rotation = rr * q * bindRoot[k];
        }
        if (c.standRef) {
            // arms: point each segment the way the clip's arm points
            for (int side = part == RIGHT_ARM ? 1 : 0; side < 2; side++) for (int seg = 0; seg < 3; seg++) {
                int a = i0 * 18 + side * 9 + seg * 3, b = i1 * 18 + side * 9 + seg * 3;
                Vector3 want = rr * Vector3.Slerp(new Vector3(c.arms[a], c.arms[a + 1], c.arms[a + 2]), new Vector3(c.arms[b], c.arms[b + 1], c.arms[b + 2]), f);
                Transform t2 = aim[side][seg];
                t2.rotation = Quaternion.FromToRotation(aim[side][seg + 1].position - t2.position, want) * t2.rotation;
            }
        }
        int pa = i0 * 3, pb = i1 * 3;
        Vector3 p = Vector3.Lerp(new Vector3(c.pelvis[pa], c.pelvis[pa + 1], c.pelvis[pa + 2]), new Vector3(c.pelvis[pb], c.pelvis[pb + 1], c.pelvis[pb + 2]), f);
        if (c.loop) p.z = 0;   // looping clips stay on the spot; the game moves the runner
        return p;
    }
    void PoseArm(int i) {
        Turn(ARM + i * 2, V.R(armE[i]) * V.R(0, 0, i == 0 ? ARM_REST : -ARM_REST));
        Turn(ARM + i * 2 + 1, V.R(elbow[i], 0, 0));
    }
    void ApplyPose() {
        for (int k = 0; k < mb.Length; k++) if (mb[k] != null) mb[k].localRotation = bindLocal[k];
        Turn(PELVIS, V.R(0, hips, 0));
        Quaternion third = V.R(spineE.x / 3, (spineE.y - hips) / 3, spineE.z / 3);
        for (int i = 0; i < 3; i++) Turn(SPINE + i, third);
        Turn(NECK, V.R(headE * 0.4f));
        Turn(HEAD, V.R(headE * 0.6f));
        for (int i = 0; i < 2; i++) {
            PoseArm(i);
            Turn(LEG + i * 2, V.R(legE[i]));
            Turn(LEG + i * 2 + 1, V.R(-knee[i], 0, 0));
        }
    }
    void Clear() {
        for (int i = 0; i < 2; i++) { legE[i] = Vector3.zero; armE[i] = Vector3.zero; knee[i] = 0.05f; elbow[i] = 0.2f; armOverride[i] = false; }
        headE = Vector3.zero; spineE = Vector3.zero; hips = 0;
    }
    // stand still, ready to be posed by hand
    public void Stand() { Clear(); clip = null; posed = 1; nLayers = 0; planted = false; }
    // posed by hand from here on: the caller sets the limb angles before Apply
    public void Pose(float step) { Stand(); dt = step; }
    public float HipHeight { get { return pelvisHeight * root.lossyScale.y; } }
    public Vector3 Hand(int i) { return aim[i][2].position; }
    // after Apply: bend arm i (0 left, 1 right) so that the hand rests on a point in the world, elbow down and back
    public void Reach(int i, Vector3 target) {
        Transform a = aim[i][0], b = aim[i][1], c = aim[i][2];
        float ab = Vector3.Distance(a.position, b.position), bc = Vector3.Distance(b.position, c.position);
        Vector3 to = target - a.position;
        float d = Mathf.Clamp(to.magnitude, Mathf.Abs(ab - bc) + 0.01f, (ab + bc) * 0.995f);
        Vector3 dir = to.normalized, pole = root.rotation * new Vector3(i == 0 ? -0.5f : 0.5f, -0.5f, -0.7f);
        Vector3 bend = (pole - dir * Vector3.Dot(pole, dir)).normalized;
        float ang = Mathf.Acos(Mathf.Clamp((ab * ab + d * d - bc * bc) / (2 * ab * d), -1, 1));
        Vector3 elbowAt = a.position + (dir * Mathf.Cos(ang) + bend * Mathf.Sin(ang)) * ab;
        a.rotation = Quaternion.FromToRotation(b.position - a.position, elbowAt - a.position) * a.rotation;
        b.rotation = Quaternion.FromToRotation(c.position - b.position, a.position + dir * d - b.position) * b.rotation;
    }
    // back to the start of a run: nothing carried over from the last one
    public void ResetPose() {
        Stand(); rootE = Vector3.zero;
        was = "run"; slideT = landT = exitT = hitT = 9; sprintW = swerve = lookW = lookYaw = lookPitch = 0; shownKey = null; fade = 0;
        Apply();
    }
    // follow one clip; blend is how long to take getting into it from whatever was showing
    public void Play(string name, float t, float step, float blend = FADE) {
        clip = name; clipT = t; posed = 0; dt = step; key = name; fadeFor = blend; nLayers = 0; planted = false; grip = 0.3f;
    }
    // lay another clip over the one playing, as strongly as w (0 to 1), on the whole body or part of it
    public void Over(string name, float t, float w, int part = ALL) {
        MotionClip c = Moves.Get(name);
        if (c == null || w < 0.004f || nLayers >= layers.Length) return;
        layers[nLayers++] = new Layer { c = c, t = t, w = Mathf.Min(1, w), part = part };
    }
    static float Swell(float x) { return x < 0.3f ? Mathf.SmoothStep(0, 1, x / 0.3f) : Mathf.SmoothStep(1, 0, (x - 0.3f) / 0.7f); }

    // on the road. The run cycle follows the footsteps and leans into a sprint; a jump throws the arms up, tucks at
    // the top and reaches for the ground, and the knees give on landing; a slide drops down, holds and gets back up.
    // vy is the runner's upward speed, so the leap keeps time with the real arc however high it goes.
    public void Animate(float phase, string mode, float step, bool sprint = false, float vy = 0) {
        Clear();
        clock += step;
        if (mode != was) {
            if (mode == "air") v0 = Mathf.Max(vy, 7);
            else if (was == "air") { landT = 0; impact = Mathf.Clamp01(-lastVy / 13); }
            if (mode == "slide") slideT = 0;
            else if (was == "slide" && mode == "run") exitT = 0;
            was = mode;
        }
        lastVy = vy;
        slideT += step; landT += step; exitT += step; hitT += step;
        sprintW = Mathf.MoveTowards(sprintW, sprint ? 1 : 0, step * 2.5f);
        float cycle = phase / (Mathf.PI * 2);
        if (mode == "air") {
            float p = Mathf.Clamp01(0.5f - 0.5f * vy / v0);   // 0 leaving the ground, 0.5 at the top, 1 coming down as fast as it left
            Play("Jump_air", Mathf.Lerp(0.02f, 0.7f, p), step, 0.1f); key = "air";
            Over("Run_Jump", 0.3f, 0.62f - 0.3f * Mathf.Sin(p * Mathf.PI));   // legs split like a hurdler, most at take-off and coming down
            grip = 0.12f;
        } else if (mode == "slide") {
            if (slideT < SLIDE_IN) { Play("Slide_Start", Mathf.Lerp(0.12f, 0.6f, slideT / SLIDE_IN), step, 0.07f); key = "slide in"; }
            else Play("Slide", clock / 2, step, 0.1f);
            grip = 0.4f;
        } else if (exitT < SLIDE_OUT) {
            Play("Slide_Exit", Mathf.Lerp(0.25f, 1, exitT / SLIDE_OUT), step, 0.07f);
            planted = true;
        } else {
            Play("Run", cycle, step, 0.14f); key = "run";
            Over("Sprint", cycle, sprintW);
            if (landT < LAND) Over("Jump_Land", Mathf.Lerp(0.2f, 0.5f, landT / LAND), 0.6f * impact * Swell(landT / LAND));
            planted = true; grip = 0.75f;
        }
        if (hitT < HIT) Over("Hit_Chest", hitT / HIT, 0.8f * Swell(hitT / HIT), UPPER);   // knocked: head and chest thrown back
        Carry();
    }
    // a knock that does not stop the run
    public void Flinch() { hitT = 0; }
    // sidestepping: turn and lean into the move. side is how far there is still to go, in lanes (negative = to the right)
    public void Swerve(float side, float step) {
        swerve += (Mathf.Clamp(side, -1, 1) - swerve) * Mathf.Min(1, step * 12);
        rootE.y += swerve * 0.9f; rootE.z = swerve * 0.5f;
    }
    void Carry() { if (carry) { armOverride[0] = true; armE[0] = new Vector3(0.55f, 0, 0.12f); elbow[0] = 1.75f; } }
    // people on the pavement play whatever clip their pose names, or the reaction asked for; tray sellers keep one arm
    // up to the tray
    public void Idle(float time, float step, string react = null) {
        Clear();
        string name = react ?? (pose == "tray" ? "Idle_A" : pose);
        MotionClip c = Moves.Get(name);
        if (c == null) { name = "Idle_A"; c = Moves.Get(name); }
        Play(name, (time + seed) / (c != null ? c.seconds : 1), step, 0.3f);
        planted = true;
        if (pose == "tray") { armOverride[1] = true; armE[1] = new Vector3(2.85f, 0, -0.1f); elbow[1] = 0.55f; }
        Carry();
    }
}

public static class People {
    static readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
    static readonly Dictionary<Person, Material[]> dressed = new Dictionary<Person, Material[]>();
    static readonly Dictionary<Person, Shape[]> props = new Dictionary<Person, Shape[]>();
    static Material hidden;
    const float PI = Mathf.PI;
    public const float SKATE = 0.085f;   // how far a pair of roller blades lifts the wearer, in the model's own units
    static readonly Dictionary<int, Shape> skates = new Dictionary<int, Shape>();
    // one roller blade, modelled around the ankle of a foot that points up the road
    static Shape Skate(int colour, float ankle) {
        Shape s;
        if (skates.TryGetValue(colour, out s)) return s;
        Material shell = T.Lam(colour), dark = T.Lam(0x1b1b1b), wheel = T.Lam(0xf2efe6);
        s = new Shape();
        s.Box(0.115f, 0.2f, 0.13f, shell, 0, 0.06f, 0.005f);
        s.Box(0.118f, 0.035f, 0.135f, dark, 0, 0.1f, 0.005f);
        s.Box(0.115f, ankle + 0.02f, 0.3f, shell, 0, -ankle / 2 + 0.01f, -0.075f);
        s.Box(0.035f, 0.04f, 0.3f, dark, 0, -ankle - 0.012f, -0.07f);
        for (int k = 0; k < 4; k++) s.Cyl(0.037f, 0.037f, 0.032f, 12, wheel, 0, -ankle - SKATE + 0.037f, -0.2f + k * 0.085f, 0, 0, PI / 2);
        return skates[colour] = s;
    }

    static Material Dress(Material original, string change) {
        if (change == "hide") {
            if (hidden == null) { hidden = new Material(original); hidden.SetFloat("_Cutoff", 2); }
            return hidden;
        }
        Material m = new Material(original);
        if (change.StartsWith("x#")) m.SetColor("_Color", T.H(System.Convert.ToInt32(change.Substring(2), 16)));
        else if (change.StartsWith("#")) { m.SetTexture("_MainTex", null); m.SetColor("_Color", T.H(System.Convert.ToInt32(change.Substring(1), 16))); m.SetFloat("_Cutoff", 0); }
        else { m.SetTexture("_MainTex", T.ankara[change]); m.mainTextureScale = new Vector2(7, 7); m.SetColor("_Color", Color.white); m.SetFloat("_Cutoff", 0); }
        return m;
    }
    static Material[] MaterialsFor(Person p, SkinnedMeshRenderer smr) {
        Material[] res;
        if (dressed.TryGetValue(p, out res)) return res;
        res = smr.sharedMaterials;
        for (int i = 0; i < res.Length; i++)
            for (int k = 0; k + 1 < p.dress.Length; k += 2)
                if (res[i].name.ToLowerInvariant().Contains(p.dress[k])) res[i] = Dress(res[i], p.dress[k + 1]);
        return dressed[p] = res;
    }

    // hat, hand item and back item, modelled around the head centre / the grip / the upper back
    static Shape[] PropsFor(Person p) {
        Shape[] res;
        if (props.TryGetValue(p, out res)) return res;
        Shape head = new Shape(), right = new Shape(), left = new Shape(), back = new Shape();
        Material hw = p.hat == null ? null : (p.hatMat ?? T.Lam(p.hatColor)), dark = T.Lam(0x1b1b1b);
        if (p.hat == "cap") { head.Ball(0.25f, 0.17f, 0.27f, hw, 0, 0.075f, 0.005f); head.Ball(0.2f, 0.035f, 0.2f, hw, 0, 0.055f, -0.13f); }
        if (p.hat == "beret") { head.Ball(0.29f, 0.1f, 0.29f, hw, 0.025f, 0.105f, 0.01f, 0, 0, -0.2f); head.Ball(0.04f, 0.04f, 0.02f, T.Lam(0xc9a227), -0.06f, 0.1f, -0.12f); }
        if (p.hat == "bandana") head.Cyl(0.108f, 0.112f, 0.06f, 16, hw, 0, 0.07f, 0.005f);
        if (p.hat == "fila") { head.Cyl(0.1f, 0.108f, 0.12f, 16, hw, 0.008f, 0.1f, 0.005f, 0, 0, -0.16f); }
        if (p.hat == "helmet") { head.Ball(0.3f, 0.29f, 0.32f, hw, 0, 0.035f, 0.01f); head.Ball(0.22f, 0.09f, 0.06f, T.Lam(0x223344), 0, 0.01f, -0.135f); }
        if (p.hat == "gele") {
            head.Ball(0.31f, 0.2f, 0.31f, hw, 0, 0.1f, 0.01f);
            head.Ball(0.44f, 0.22f, 0.1f, hw, 0, 0.21f, 0.06f, -0.45f, 0, 0);
            head.Ball(0.3f, 0.16f, 0.09f, hw, 0.02f, 0.2f, -0.02f, -0.2f, 0, 0.25f);
        }
        if (p.shades) { head.Ball(0.075f, 0.045f, 0.02f, dark, -0.035f, 0.022f, -0.098f); head.Ball(0.075f, 0.045f, 0.02f, dark, 0.035f, 0.022f, -0.098f); head.Box(0.16f, 0.008f, 0.01f, dark, 0, 0.03f, -0.095f); }
        if (p.tray >= 0) {
            head.Cyl(0.25f, 0.2f, 0.05f, 18, T.Lam(0xb0b4b8), 0, 0.145f, 0);
            Material fruit = T.Lam(p.tray);
            for (int i = 0; i < 7; i++) { float a = i * PI * 2 / 6; head.Ball(0.1f, 0.1f, 0.1f, fruit, i == 6 ? 0 : Mathf.Cos(a) * 0.12f, i == 6 ? 0.27f : 0.21f, i == 6 ? 0 : Mathf.Sin(a) * 0.12f); }
        }
        if (p.prop == "machete") {
            right.Cyl(0.018f, 0.018f, 0.13f, 8, T.Lam(0x2a1a10), 0, 0, -0.02f, PI / 2, 0, 0);
            right.Box(0.012f, 0.085f, 0.55f, T.Emissive(T.Lam(0xc4c8ce), 0x333333), 0, 0.005f, -0.36f);
        } else if (p.prop == "stick") right.Cyl(0.02f, 0.024f, 0.95f, 8, T.Lam(0x7a5434), 0, 0, -0.32f, PI / 2, 0, 0);
        else if (p.prop == "baton") right.Cyl(0.02f, 0.02f, 0.5f, 8, T.Lam(0x111111), 0, 0, -0.17f, PI / 2, 0, 0);
        else if (p.prop == "cash") { right.Box(0.02f, 0.07f, 0.15f, T.Lam(0x3f8f5a), 0, 0, -0.05f); right.Box(0.022f, 0.02f, 0.152f, T.Lam(0xe8e2c8), 0, 0, -0.05f); }
        else if (p.prop == "bag") { left.Ball(0.1f, 0.24f, 0.3f, T.Lam(0xf5c518), 0, -0.2f, 0); left.Cyl(0.008f, 0.008f, 0.12f, 6, T.Lam(0x16120c), 0, -0.05f, 0); }
        else if (p.prop == "phone") left.Box(0.014f, 0.14f, 0.07f, dark, 0.02f, 0, 0);
        if (p.backpack >= 0) { Material bp = T.Lam(p.backpack); back.Ball(0.3f, 0.4f, 0.2f, bp, 0, 0, 0.1f); back.Ball(0.22f, 0.16f, 0.1f, bp, 0, -0.08f, 0.19f); }
        return props[p] = new[] { head, right, left, back };
    }

    public static Runner Spawn(Person p, string name, Transform parent = null) {
        GameObject prefab;
        if (!prefabs.TryGetValue(p.model, out prefab)) prefabs[p.model] = prefab = Resources.Load<GameObject>("Characters/" + p.model);
        GameObject go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        GameObject model = Object.Instantiate(prefab, go.transform);
        var byName = new Dictionary<string, Transform>();
        foreach (Transform t in model.GetComponentsInChildren<Transform>(true)) byName[t.name] = t;
        foreach (SkinnedMeshRenderer smr in model.GetComponentsInChildren<SkinnedMeshRenderer>(true)) {
            smr.sharedMaterials = MaterialsFor(p, smr);
            smr.updateWhenOffscreen = false;
            smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; smr.receiveShadows = true;
            smr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off; smr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            Bounds b = smr.localBounds; b.Expand(1.2f); smr.localBounds = b;
        }
        Runner r = new Runner(go.transform, byName);
        r.pose = p.pose; r.seed = Random.value * 20;
        if (p.prop == "machete" || p.prop == "stick" || p.prop == "baton" || p.prop == "cash") r.hold[1] = 0.95f;
        if (p.prop == "bag" || p.prop == "phone") r.hold[0] = 0.85f;

        // attach hat and hand items while the body is still in its import pose, then let the bones carry them
        Shape[] sh = PropsFor(p);
        Transform head = byName["head"];
        Attach(sh[0], head, head.position + new Vector3(0, 0.03f, 0.02f));
        for (int i = 0; i < 2; i++) {
            Transform hand = byName[i == 0 ? "hand_r" : "hand_l"], fore = byName[i == 0 ? "lowerarm_r" : "lowerarm_l"];
            Attach(sh[1 + i], hand, hand.position + (hand.position - fore.position).normalized * 0.085f);
        }
        if (p.skates >= 0) foreach (string f in new[] { "foot_l", "foot_r" }) {
            Transform foot = byName[f];
            Attach(Skate(p.skates, foot.position.y - go.transform.position.y), foot, foot.position);
        }
        Transform chest = byName["spine_03"];
        Attach(sh[3], chest, chest.position + new Vector3(0, 0.12f, -0.07f));
        head.localScale = Vector3.one * p.headScale;
        go.transform.localScale = Vector3.one * p.scale * 1.1f;   // a touch larger than life so they read at running distance
        return r;
    }
    static void Attach(Shape s, Transform bone, Vector3 worldPos) {
        if (s.Empty) return;
        Transform t = s.Make("prop").transform;
        t.position = worldPos; t.rotation = Quaternion.identity;
        t.SetParent(bone, true);
    }
}

// The danfo and keke are real models (see Assets/Credits.md), scaled to the size the game's collision expects.
public static class Vehicles {
    static readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
    // length runs front to back; yaw turns the model so its nose points up the road
    public static GameObject Make(string name, float width, float height, float length, float yaw) {
        GameObject prefab;
        if (!prefabs.TryGetValue(name, out prefab)) prefabs[name] = prefab = Resources.Load<GameObject>("Vehicles/" + name);
        if (prefab == null) return null;
        GameObject go = new GameObject(name);
        GameObject model = Object.Instantiate(prefab, go.transform);
        model.transform.localRotation = Quaternion.Euler(0, yaw, 0) * model.transform.localRotation;
        Bounds b = new Bounds(); bool first = true;
        foreach (Renderer r in model.GetComponentsInChildren<Renderer>()) {
            if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; r.receiveShadows = true;
            r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off; r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        }
        float k = Mathf.Min(length / b.size.z, width / b.size.x, height / b.size.y);
        model.transform.localScale *= k;
        model.transform.localPosition = -new Vector3(b.center.x, b.min.y, b.center.z) * k;
        return go;
    }
}
