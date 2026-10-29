using System.Collections.Generic;
using UnityEngine;

// The game is written in the coordinates of the original web version (right-handed, running toward -z).
// V converts those to Unity's left-handed space (running toward +z) at the point of placing things.
public static class V {
    public static Vector3 P(float x, float y, float z) { return new Vector3(x, y, -z); }
    public static Quaternion R(float rx, float ry, float rz) {
        Quaternion q = Quaternion.AngleAxis(rx * Mathf.Rad2Deg, Vector3.right) * Quaternion.AngleAxis(ry * Mathf.Rad2Deg, Vector3.up) * Quaternion.AngleAxis(rz * Mathf.Rad2Deg, Vector3.forward);
        return new Quaternion(-q.x, -q.y, q.z, q.w);
    }
    public static Quaternion R(Vector3 e) { return R(e.x, e.y, e.z); }
    public static Matrix4x4 M(float x, float y, float z) { return Matrix4x4.Translate(P(x, y, z)); }
    public static Matrix4x4 M(float x, float y, float z, float rx, float ry, float rz) { return Matrix4x4.TRS(P(x, y, z), R(rx, ry, rz), Vector3.one); }
}

// Source meshes. Cube faces are separate submeshes in the order +x, -x, +y, -y, near (toward the camera), far.
public static class Meshes {
    public class Sub { public Vector3[] v, n; public Vector2[] uv; public int[] tris; public Vector3 tan = Vector3.right; public Vector4[] tans; }
    static readonly Dictionary<string, Sub[]> cache = new Dictionary<string, Sub[]>();

    static Sub Face(Vector3 n, Vector3 up) {
        Vector3 r = Vector3.Cross(up, -n);
        Sub s = new Sub();
        s.v = new[] { (n - r - up) * 0.5f, (n - r + up) * 0.5f, (n + r + up) * 0.5f, (n + r - up) * 0.5f };
        s.n = new[] { n, n, n, n };
        s.uv = new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };
        s.tris = new[] { 0, 1, 2, 0, 2, 3 };
        s.tan = r;
        return s;
    }
    public static Sub[] Cube() {
        Sub[] c;
        if (cache.TryGetValue("cube", out c)) return c;
        c = new[] {
            Face(Vector3.right, Vector3.up), Face(Vector3.left, Vector3.up),
            Face(Vector3.up, Vector3.forward), Face(Vector3.down, Vector3.back),
            Face(Vector3.back, Vector3.up), Face(Vector3.forward, Vector3.up),
        };
        return cache["cube"] = c;
    }
    // upright quad facing the camera, 1 x 1
    public static Sub[] Quad() {
        Sub[] c;
        if (cache.TryGetValue("quad", out c)) return c;
        Sub s = Face(Vector3.back, Vector3.up);
        for (int i = 0; i < 4; i++) s.v[i].z = 0;
        return cache["quad"] = new[] { s };
    }
    // an imported, readable mesh split into its submeshes
    static readonly Dictionary<Mesh, Sub[]> imported = new Dictionary<Mesh, Sub[]>();
    public static Sub[] FromMesh(Mesh mesh) {
        Sub[] res;
        if (imported.TryGetValue(mesh, out res)) return res;
        Vector3[] v = mesh.vertices, n = mesh.normals; Vector2[] uv = mesh.uv; Vector4[] tg = mesh.tangents;
        res = new Sub[mesh.subMeshCount];
        for (int s = 0; s < res.Length; s++) {
            int[] t = mesh.GetTriangles(s);
            var map = new Dictionary<int, int>(); var sv = new List<Vector3>(); var sn = new List<Vector3>(); var su = new List<Vector2>(); var st = new List<Vector4>();
            for (int i = 0; i < t.Length; i++) {
                int k;
                if (!map.TryGetValue(t[i], out k)) { k = map[t[i]] = sv.Count; sv.Add(v[t[i]]); sn.Add(n.Length > 0 ? n[t[i]] : Vector3.up); su.Add(uv.Length > 0 ? uv[t[i]] : Vector2.zero); st.Add(tg.Length > 0 ? tg[t[i]] : new Vector4(1, 0, 0, -1)); }
                t[i] = k;
            }
            res[s] = new Sub { v = sv.ToArray(), n = sn.ToArray(), uv = su.ToArray(), tris = t, tans = st.ToArray() };
        }
        return imported[mesh] = res;
    }
    // sphere of diameter 1
    public static Sub[] Sphere() {
        Sub[] c;
        if (cache.TryGetValue("sphere", out c)) return c;
        const int U = 14, W = 9;
        var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        for (int j = 0; j <= W; j++) for (int i = 0; i <= U; i++) {
            float a = i * Mathf.PI * 2 / U, b = j * Mathf.PI / W;
            Vector3 d = new Vector3(Mathf.Cos(a) * Mathf.Sin(b), Mathf.Cos(b), Mathf.Sin(a) * Mathf.Sin(b));
            v.Add(d * 0.5f); n.Add(d); uv.Add(new Vector2(i / (float)U, 1 - j / (float)W));
            if (i < U && j < W) { int k = j * (U + 1) + i; t.AddRange(new[] { k, k + 1, k + U + 2, k, k + U + 2, k + U + 1 }); }
        }
        Sub s = new Sub { v = v.ToArray(), n = n.ToArray(), uv = uv.ToArray(), tris = t.ToArray() };
        return cache["sphere"] = new[] { s };
    }
    // submeshes: side, top cap, bottom cap
    public static Sub[] Cyl(float rt, float rb, float h, int seg) {
        string key = "c" + rt + "_" + rb + "_" + h + "_" + seg;
        Sub[] c;
        if (cache.TryGetValue(key, out c)) return c;
        Sub side = new Sub(), top = new Sub(), bot = new Sub();
        var sv = new List<Vector3>(); var sn = new List<Vector3>(); var su = new List<Vector2>(); var st = new List<int>();
        float slope = (rb - rt) / h;
        for (int i = 0; i <= seg; i++) {
            float a = i * Mathf.PI * 2 / seg, cs = Mathf.Cos(a), sn2 = Mathf.Sin(a);
            Vector3 nn = new Vector3(cs, slope, sn2).normalized;
            sv.Add(new Vector3(cs * rb, -h / 2, sn2 * rb)); sv.Add(new Vector3(cs * rt, h / 2, sn2 * rt));
            sn.Add(nn); sn.Add(nn);
            su.Add(new Vector2(i / (float)seg, 0)); su.Add(new Vector2(i / (float)seg, 1));
            if (i < seg) { int b = i * 2; st.AddRange(new[] { b, b + 1, b + 3, b, b + 3, b + 2 }); }
        }
        side.v = sv.ToArray(); side.n = sn.ToArray(); side.uv = su.ToArray(); side.tris = st.ToArray();
        for (int cap = 0; cap < 2; cap++) {
            Sub s = cap == 0 ? top : bot; float r = cap == 0 ? rt : rb, y = cap == 0 ? h / 2 : -h / 2, sign = cap == 0 ? 1 : -1;
            var v = new List<Vector3>(); var n = new List<Vector3>(); var u = new List<Vector2>(); var t = new List<int>();
            v.Add(new Vector3(0, y, 0)); n.Add(Vector3.up * sign); u.Add(new Vector2(0.5f, 0.5f));
            for (int i = 0; i <= seg; i++) {
                float a = i * Mathf.PI * 2 / seg, cs = Mathf.Cos(a), sn2 = Mathf.Sin(a);
                v.Add(new Vector3(cs * r, y, sn2 * r)); n.Add(Vector3.up * sign); u.Add(new Vector2(sign * cs * 0.5f + 0.5f, sn2 * 0.5f + 0.5f));
                if (i < seg) { if (cap == 0) t.AddRange(new[] { 0, i + 2, i + 1 }); else t.AddRange(new[] { 0, i + 1, i + 2 }); }
            }
            s.v = v.ToArray(); s.n = n.ToArray(); s.uv = u.ToArray(); s.tris = t.ToArray();
        }
        return cache[key] = new[] { side, top, bot };
    }
}

// A bag of mesh pieces with materials and transforms. Bake() merges them into one mesh per material, so a danfo or a
// whole 20 m stretch of street costs a handful of draw calls instead of hundreds.
public class Shape {
    struct Part { public Meshes.Sub sub; public Material mat; public Matrix4x4 m; public Vector2 uv; }
    readonly List<Part> parts = new List<Part>();
    List<KeyValuePair<Material, Mesh>> baked;
    public bool Empty { get { return parts.Count == 0; } }

    void Put(Meshes.Sub sub, Material mat, Matrix4x4 m, Vector2 uv) { parts.Add(new Part { sub = sub, mat = mat, m = m, uv = uv }); }

    public Shape Box(float w, float h, float d, Material mat, float x = 0, float y = 0, float z = 0) {
        return BoxM(w, h, d, mat, V.M(x, y, z));
    }
    public Shape BoxR(float w, float h, float d, Material mat, float x, float y, float z, float rx, float ry, float rz) {
        return BoxM(w, h, d, mat, V.M(x, y, z, rx, ry, rz));
    }
    public Shape BoxM(float w, float h, float d, Material mat, Matrix4x4 m) {
        m = m * Matrix4x4.Scale(new Vector3(w, h, d));
        foreach (Meshes.Sub s in Meshes.Cube()) Put(s, mat, m, Vector2.one);
        return this;
    }
    // one material per face; uv scales the texture per face (used to tile building fronts)
    public Shape Box6(float w, float h, float d, Material[] mats, float x, float y, float z, Vector2[] uv = null) {
        Matrix4x4 m = V.M(x, y, z) * Matrix4x4.Scale(new Vector3(w, h, d));
        Meshes.Sub[] c = Meshes.Cube();
        for (int i = 0; i < 6; i++) if (mats[i] != null) Put(c[i], mats[i], m, uv == null ? Vector2.one : uv[i]);   // a null material leaves that face open
        return this;
    }
    // a box whose texture repeats every `tile` metres on every face, whatever the box's size
    public Shape BoxTiled(float w, float h, float d, Material mat, float x, float y, float z, float tile = 3, float ry = 0) {
        Matrix4x4 m = V.M(x, y, z, 0, ry, 0) * Matrix4x4.Scale(new Vector3(w, h, d));
        Meshes.Sub[] c = Meshes.Cube();
        Vector2 sx = new Vector2(d / tile, h / tile), sy = new Vector2(w / tile, d / tile), sz = new Vector2(w / tile, h / tile);
        Vector2[] uv = { sx, sx, sy, sy, sz, sz };
        for (int i = 0; i < 6; i++) Put(c[i], mat, m, uv[i]);
        return this;
    }
    public Shape Cyl(float rt, float rb, float h, int seg, Material mat, float x = 0, float y = 0, float z = 0, float rx = 0, float ry = 0, float rz = 0) {
        return Cyl3(rt, rb, h, seg, mat, mat, mat, V.M(x, y, z, rx, ry, rz));
    }
    public Shape Cyl3(float rt, float rb, float h, int seg, Material side, Material top, Material bottom, Matrix4x4 m) {
        Meshes.Sub[] c = Meshes.Cyl(rt, rb, h, seg);
        Put(c[0], side, m, Vector2.one); Put(c[1], top, m, Vector2.one); Put(c[2], bottom, m, Vector2.one);
        return this;
    }
    // ellipsoid
    public Shape Ball(float w, float h, float d, Material mat, float x = 0, float y = 0, float z = 0, float rx = 0, float ry = 0, float rz = 0) {
        Put(Meshes.Sphere()[0], mat, V.M(x, y, z, rx, ry, rz) * Matrix4x4.Scale(new Vector3(w, h, d)), Vector2.one);
        return this;
    }
    // an imported model, scaled so its largest footprint side (or its height, if tall) is `size`, standing on the ground
    public Shape Model(GameObject prefab, float size, bool byHeight) {
        var found = new List<KeyValuePair<Matrix4x4, MeshFilter>>();
        Bounds b = new Bounds(); bool first = true;
        foreach (MeshFilter mf in prefab.GetComponentsInChildren<MeshFilter>()) {
            Matrix4x4 local = mf.transform.localToWorldMatrix;   // includes the turn the importer puts on the model's root
            found.Add(new KeyValuePair<Matrix4x4, MeshFilter>(local, mf));
            foreach (Vector3 p in mf.sharedMesh.vertices) { Vector3 q = local.MultiplyPoint3x4(p); if (first) { b = new Bounds(q, Vector3.zero); first = false; } else b.Encapsulate(q); }
        }
        float k = size / (byHeight ? b.size.y : Mathf.Max(b.size.x, b.size.z));
        Matrix4x4 fit = Matrix4x4.Scale(Vector3.one * k) * Matrix4x4.Translate(-new Vector3(b.center.x, b.min.y, b.center.z));
        foreach (var kv in found) {
            Meshes.Sub[] subs = Meshes.FromMesh(kv.Value.sharedMesh);
            Material[] mats = kv.Value.GetComponent<Renderer>().sharedMaterials;
            for (int i = 0; i < subs.Length; i++) Put(subs[i], mats[Mathf.Min(i, mats.Length - 1)], fit * kv.Key, Vector2.one);
        }
        return this;
    }
    // a kit piece placed as modelled, with a material chosen for each of its named slots
    public Shape Piece(GameObject prefab, Matrix4x4 m, System.Func<string, Material> slot) {
        foreach (MeshFilter mf in prefab.GetComponentsInChildren<MeshFilter>()) {
            Meshes.Sub[] subs = Meshes.FromMesh(mf.sharedMesh);
            Material[] mats = mf.GetComponent<Renderer>().sharedMaterials;
            Matrix4x4 local = m * mf.transform.localToWorldMatrix;
            for (int i = 0; i < subs.Length; i++) { Material mat = slot(mats[Mathf.Min(i, mats.Length - 1)].name); if (mat != null) Put(subs[i], mat, local, Vector2.one); }
        }
        return this;
    }
    public Shape Quad(float w, float h, Material mat, Matrix4x4 m) {
        Put(Meshes.Quad()[0], mat, m * Matrix4x4.Scale(new Vector3(w, h, 1)), Vector2.one);
        return this;
    }
    public Shape Add(Shape s, Matrix4x4 m) {
        foreach (Part p in s.parts) Put(p.sub, p.mat, m * p.m, p.uv);
        return this;
    }

    class Acc { public List<Vector4> tan = new List<Vector4>(); public List<Vector3> v = new List<Vector3>(), n = new List<Vector3>(); public List<Vector2> uv = new List<Vector2>(); public List<int> t = new List<int>(); }

    public List<KeyValuePair<Material, Mesh>> Bake() {
        var accs = new Dictionary<Material, Acc>();
        var order = new List<Material>();
        foreach (Part p in parts) {
            Acc a;
            if (!accs.TryGetValue(p.mat, out a)) { a = accs[p.mat] = new Acc(); order.Add(p.mat); }
            int b = a.v.Count;
            Matrix4x4 nm = p.m.inverse.transpose;
            for (int i = 0; i < p.sub.v.Length; i++) {
                a.v.Add(p.m.MultiplyPoint3x4(p.sub.v[i]));
                a.n.Add(nm.MultiplyVector(p.sub.n[i]).normalized);
                a.uv.Add(Vector2.Scale(p.sub.uv[i], p.uv));
            }
            Vector3 tw = p.m.MultiplyVector(p.sub.tan).normalized;
            for (int i = 0; i < p.sub.v.Length; i++) {
                if (p.sub.tans == null) { a.tan.Add(new Vector4(tw.x, tw.y, tw.z, -1)); continue; }
                Vector3 pt = p.m.MultiplyVector(p.sub.tans[i]).normalized;
                a.tan.Add(new Vector4(pt.x, pt.y, pt.z, p.sub.tans[i].w));
            }
            for (int i = 0; i < p.sub.tris.Length; i++) a.t.Add(b + p.sub.tris[i]);
        }
        var res = new List<KeyValuePair<Material, Mesh>>();
        foreach (Material mat in order) {
            Acc a = accs[mat];
            Mesh mesh = new Mesh();
            if (a.v.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(a.v); mesh.SetNormals(a.n); mesh.SetTangents(a.tan); mesh.SetUVs(0, a.uv); mesh.SetTriangles(a.t, 0);
            mesh.RecalculateBounds();
            res.Add(new KeyValuePair<Material, Mesh>(mat, mesh));
        }
        return res;
    }

    static void Attach(GameObject go, List<KeyValuePair<Material, Mesh>> list) {
        foreach (var kv in list) {
            GameObject c = new GameObject("m");
            c.transform.SetParent(go.transform, false);
            c.AddComponent<MeshFilter>().sharedMesh = kv.Value;
            MeshRenderer r = c.AddComponent<MeshRenderer>();
            r.sharedMaterial = kv.Key;
            bool solid = kv.Key.renderQueue < 2500 && kv.Key.GetFloat("_Lit") > 0.5f && kv.Key.renderQueue > 1500;
            r.shadowCastingMode = solid ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = true;
            r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        }
    }
    // instances share the baked meshes
    public GameObject Make(string name, Transform parent = null) {
        if (baked == null) baked = Bake();
        GameObject go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        Attach(go, baked);
        return go;
    }
    // rebuilds a container in place and frees the meshes it held before
    public void Fill(GameObject go, List<Mesh> owned) {
        for (int i = go.transform.childCount - 1; i >= 0; i--) Object.Destroy(go.transform.GetChild(i).gameObject);
        foreach (Mesh m in owned) Object.Destroy(m);
        owned.Clear();
        var list = Bake();
        foreach (var kv in list) owned.Add(kv.Value);
        Attach(go, list);
    }
}
