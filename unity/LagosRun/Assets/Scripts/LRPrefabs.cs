using System.Collections.Generic;
using UnityEngine;

// Reusable shapes: vehicles, obstacles, street furniture, bystanders.
public static class P {
    public static Shape[] danfo, banner, stalls, billboards, clutter;
    public static Shape kiosk;
    public static Shape keke, barrier, pothole, coin, palm, pole, lamp, stilt, footbridge, canoe, shadow;
    const float PI = Mathf.PI;

    static T2 Pick<T2>(T2[] a) { return a[Random.Range(0, a.Length)]; }

    public static void Build() {
        danfo = new Shape[T.danfoBack.Length];
        for (int i = 0; i < danfo.Length; i++) {
            Shape g = new Shape();
            g.Box6(1.9f, 1.7f, 4.6f, new[] { T.danfoSide, T.danfoSide, T.yellow, T.black, T.danfoBack[i], T.danfoFront }, 0, 1.25f, 0);
            g.Box(1.7f, 0.3f, 4.4f, T.black, 0, 0.3f, 0);
            g.Box(1.6f, 0.06f, 3.4f, T.metal, 0, 2.15f, 0);
            foreach (float x in new[] { -0.75f, 0.75f }) g.Box(0.06f, 0.12f, 3.4f, T.metal, x, 2.15f, 0);
            foreach (float x in new[] { -0.92f, 0.92f }) foreach (float z in new[] { 1.5f, -1.5f }) {
                g.Cyl(0.38f, 0.38f, 0.28f, 14, T.tyre, x, 0.38f, z, 0, 0, PI / 2);
                g.Cyl(0.18f, 0.18f, 0.3f, 10, T.rim, x, 0.38f, z, 0, 0, PI / 2);
            }
            danfo[i] = g;
        }
        keke = new Shape();
        keke.Box(1.35f, 0.85f, 2.1f, T.yellow, 0, 0.75f, 0);
        keke.Box(1.37f, 0.16f, 2.12f, T.green, 0, 0.95f, 0);
        keke.Box(1.35f, 0.08f, 2.2f, T.black, 0, 1.82f, 0.05f);
        foreach (float x in new[] { -0.64f, 0.64f }) foreach (float z in new[] { 0.7f, -0.6f }) keke.Box(0.07f, 0.75f, 0.07f, T.black, x, 1.42f, z);
        keke.Box(1.2f, 0.6f, 0.05f, T.Lam(0x334455), 0, 1.45f, -0.62f);
        keke.Box(0.7f, 0.6f, 0.6f, T.yellow, 0, 0.6f, -1.25f);
        keke.Cyl(0.3f, 0.3f, 0.2f, 12, T.tyre, -0.62f, 0.3f, 0.65f, 0, 0, PI / 2);
        keke.Cyl(0.3f, 0.3f, 0.2f, 12, T.tyre, 0.62f, 0.3f, 0.65f, 0, 0, PI / 2);
        keke.Cyl(0.3f, 0.3f, 0.2f, 12, T.tyre, 0, 0.3f, -1.35f, 0, 0, PI / 2);

        barrier = new Shape();
        barrier.Box(1.95f, 0.42f, 0.12f, T.barrier, 0, 0.75f, 0);
        foreach (float x in new[] { -0.8f, 0.8f }) { barrier.Box(0.1f, 0.95f, 0.1f, T.metal, x, 0.48f, 0); barrier.Box(0.5f, 0.06f, 0.5f, T.metal, x, 0.03f, 0); }

        banner = new Shape[T.banners.Length];
        for (int i = 0; i < banner.Length; i++) {
            Shape g = new Shape();
            foreach (float x in new[] { -1.05f, 1.05f }) g.Cyl(0.06f, 0.06f, 2.9f, 8, T.wood, x, 1.45f, 0);
            g.Box(2.1f, 1.15f, 0.05f, T.banners[i], 0, 1.85f, 0);
            banner[i] = g;
        }

        pothole = new Shape();
        pothole.Quad(1.9f, 2.2f, T.pothole, V.M(0, 0.02f, 0, -PI / 2, 0, 0));
        pothole.Cyl(0.02f, 0.22f, 0.6f, 10, T.cone, 0.85f, 0.3f, -0.9f);
        pothole.Box(0.42f, 0.04f, 0.42f, T.cone, 0.85f, 0.02f, -0.9f);

        coin = new Shape();
        coin.Cyl3(0.42f, 0.42f, 0.09f, 20, T.coinEdge, T.coinFace, T.coinFace, V.M(0, 0, 0, PI / 2, 0, 0));

        palm = new Shape();
        for (int i = 0; i < 5; i++) palm.Cyl(0.16f - i * 0.015f, 0.18f - i * 0.015f, 1.3f, 7, T.trunk, i * 0.08f, 0.65f + i * 1.25f, 0, 0, 0, -0.06f);
        for (int i = 0; i < 7; i++) {
            Shape frond = new Shape().BoxR(2.6f, 0.05f, 0.5f, T.leaf, 1.2f, 0, 0, 0, 0, -0.45f);
            palm.Add(frond, V.M(0.4f, 6.3f, 0, 0, i / 7f * PI * 2, 0));
        }

        stalls = new Shape[T.umbrella.Length];
        for (int i = 0; i < stalls.Length; i++) {
            Shape g = new Shape();
            g.Cyl(0.03f, 0.03f, 2.2f, 6, T.metal, 0, 1.1f, 0);
            g.Cyl(0.05f, 1.3f, 0.5f, 8, T.umbrella[i], 0, 2.2f, 0);
            g.Box(1.2f, 0.7f, 0.8f, T.wood, 0, 0.35f, 0);
            for (int k = 0; k < 6; k++) g.Box(0.22f, 0.18f, 0.22f, Pick(T.goods), -0.4f + (k % 3) * 0.4f, 0.8f, -0.15f + (k / 3) * 0.3f);
            stalls[i] = g;
        }

        pole = new Shape();
        pole.Cyl(0.11f, 0.14f, 8, 7, T.wood, 0, 4, 0);
        pole.Box(1.6f, 0.1f, 0.1f, T.wood, 0, 7.5f, 0);
        pole.Box(0.5f, 0.6f, 0.4f, T.metal, 0.2f, 6.2f, 0);

        billboards = new Shape[T.boards.Length];
        for (int i = 0; i < billboards.Length; i++) {
            Shape g = new Shape();
            foreach (float z in new[] { -1.8f, 1.8f }) g.Box(0.2f, 7, 0.2f, T.metal, 0, 3.5f, z);
            g.Box6(0.15f, 3.2f, 6.2f, new[] { T.metal, T.boards[i], T.metal, T.metal, T.metal, T.metal }, 0, 8, 0);
            billboards[i] = g;
        }

        lamp = new Shape();
        lamp.Cyl(0.07f, 0.09f, 6, 7, T.metal, 0, 3, 0);
        lamp.Box(1.4f, 0.08f, 0.08f, T.metal, -0.7f, 6, 0);
        lamp.Box(0.5f, 0.12f, 0.25f, T.Basic(0xfff3c4), -1.3f, 5.92f, 0);

        stilt = new Shape();
        foreach (float x in new[] { -1.4f, 1.4f }) foreach (float z in new[] { -1.4f, 1.4f }) stilt.Box(0.15f, 4, 0.15f, T.wood, x, 0, z);
        stilt.Box(3.2f, 0.2f, 3.2f, T.wood, 0, 2, 0);
        stilt.Box(3, 2, 3, T.Lam(0x8a6a48), 0, 3.1f, 0);
        stilt.BoxR(3.6f, 0.15f, 3.6f, T.zinc, 0, 4.3f, 0, 0, 0, 0.15f);

        // pedestrian bridge over the road (Oshodi / Ikeja)
        footbridge = new Shape();
        Material blue = T.Lam(0x2f5aa8);
        footbridge.Box(13.5f, 0.45f, 2.4f, T.concrete, 0, 7.4f, 0);
        foreach (float z in new[] { -1.15f, 1.15f }) footbridge.Box(13.5f, 0.9f, 0.1f, blue, 0, 8.05f, z);
        footbridge.Box(13.5f, 0.7f, 0.12f, T.boardStrip, 0, 8.05f, 1.22f);
        foreach (float s in new[] { -1f, 1f }) {
            footbridge.Box(0.4f, 7.4f, 0.4f, T.concrete, s * 4.8f, 3.7f, 0);
            footbridge.BoxR(1.4f, 0.3f, 10, T.concrete, s * 6.2f, 3.8f, -4.2f, -0.72f, 0, 0);
        }

        canoe = new Shape();
        canoe.Box(0.8f, 0.35f, 4, T.wood, 0, 0, 0);
        canoe.Box(0.35f, 0.8f, 0.3f, T.Lam(0x2f5aa8), 0, 0.55f, 0.6f);
        canoe.Box(0.3f, 0.3f, 0.3f, T.Lam(0x5a3420), 0, 1.1f, 0.6f);

        shadow = new Shape().Quad(1, 1, T.shadow, V.M(0, 0, 0, -PI / 2, 0, 0));

        // real models replace the box versions above where we have them
        GameObject palmModel = Resources.Load<GameObject>("Props/Palm"), cartModel = Resources.Load<GameObject>("Props/Cart");
        if (palmModel != null) palm = new Shape().Model(palmModel, 7.4f, true);
        Shape umbrella = Kit.Add(new Shape(), "UmbrellaStall", Matrix4x4.identity, null);
        if (cartModel != null) { Shape cart = new Shape().Model(cartModel, 2.7f, false); stalls = new[] { cart, cart, umbrella }; }
        pole = Kit.Add(new Shape(), "Pole", Matrix4x4.Rotate(Quaternion.Euler(0, 90, 0)), null);
        kiosk = Kit.Add(new Shape(), "Kiosk", Matrix4x4.identity, null);
        var bits = new List<Shape>();
        foreach (string n in new[] { "Drum", "Drum", "Generator", "Chair", "Chair", "TrashBag", "TrashBag", "Rubble", "Weeds", "Weeds", "Litter" }) bits.Add(Kit.Add(new Shape(), n, Matrix4x4.identity, null));
        clutter = bits.ToArray();
    }
}

// The NAIJA HUSTLE building kit: 3 m wall modules and street props with named material slots.
// Pieces are used in a fixed frame: a module spans x 0..3, y is up, and its front faces -z.
public static class Kit {
    static readonly Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>();
    static Matrix4x4 fix; static bool ready;
    public static GameObject Get(string name) {
        GameObject go;
        if (!cache.TryGetValue(name, out go)) cache[name] = go = Resources.Load<GameObject>("Kit/" + name);
        return go;
    }
    static Matrix4x4 Fix {
        get {
            if (ready) return fix;
            ready = true; fix = Matrix4x4.identity;
            GameObject w = Get("Wall_Plain"); MeshFilter mf = w.GetComponentInChildren<MeshFilter>();
            Matrix4x4 local = mf.transform.localToWorldMatrix;
            float cx = 0; foreach (Vector3 v in mf.sharedMesh.vertices) cx += local.MultiplyPoint3x4(v).x;
            if (cx < 0) fix = Matrix4x4.Rotate(Quaternion.Euler(0, 180, 0));   // the import mirrors x; turn it back round
            return fix;
        }
    }
    public static Shape Add(Shape g, string piece, Matrix4x4 m, Material wall) { return g.Piece(Get(piece), m * Fix, n => T.Slot(n, wall)); }
}
