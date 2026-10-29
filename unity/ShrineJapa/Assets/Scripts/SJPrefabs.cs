using System.Collections.Generic;
using UnityEngine;

// Scenery and obstacles for Japa from the Shrine. Real models (tree, hut, mask, pots, pillar, palm, banana plant)
// are merged with textured shapes; see Assets/Credits.md for where the models come from.
public static class P {
    public const float GROUND_Y = -16, HALF = 3.3f, PATH_W = 6.6f;
    const float PI = Mathf.PI;
    public static Shape[] trees, idol, billboards;
    public static Shape banana, palm, palmVillage, pillar, ruin, wall, head, hut, ropePost, stilt, canoe, lamp, shrine, root, arch, fallenTree, gate, bump,
        bags, fireRing, flames, cone, cowrie, shadow;
    public static GameObject planeModel;

    static GameObject Load(string name) { return Resources.Load<GameObject>("Props/" + name); }
    static float Rand(float a, float b) { return Random.Range(a, b); }

    public static void Build() {
        GameObject treeM = Load("Tree"), palmM = Load("Palm"), bananaM = Load("Banana"), hutM = Load("Hut"), maskM = Load("Mask"), potM = Load("Pot"), pillarM = Load("Pillar");
        planeModel = Load("Plane");
        Shape mask = new Shape().Model(maskM, 1, true), pot = new Shape().Model(potM, 0.7f, true), palmTop = new Shape().Model(palmM, 9, true);

        // forest giants rooted in the valley floor, crowns above the causeway
        trees = new Shape[3];
        float[] heights = { 50, 44, 56 };
        for (int i = 0; i < 3; i++) trees[i] = new Shape().Model(treeM, heights[i], true);
        for (int i = 0; i < 3; i++) trees[i] = new Shape().Add(trees[i], V.M(0, GROUND_Y, 0));
        banana = new Shape().Model(bananaM, 4.5f, true);

        // a palm on a pillar of earth, so its crown sits beside the path
        System.Func<Material, Shape> plinthPalm = m => new Shape().BoxTiled(2.6f, 16.2f, 2.6f, m, 0, GROUND_Y + 8.1f, 0).Add(palmTop, V.M(0, 0.2f, 0));
        palmVillage = plinthPalm(T.lateriteDark);
        palm = plinthPalm(T.pillar["airport"]);

        pillar = new Shape().BoxTiled(2.2f, 22, 2.2f, T.laterite, 0, GROUND_Y + 11, 0).BoxTiled(2.8f, 0.6f, 2.8f, T.lateriteDark, 0, GROUND_Y + 22, 0);
        ruin = new Shape().BoxTiled(6, 16.2f, 6, T.lateriteDark, 0, GROUND_Y + 8.1f, 0).Add(new Shape().Model(pillarM, 6.5f, true), V.M(0, 0.2f, 0));
        wall = new Shape().BoxTiled(1.2f, 19, 9, T.laterite, 0, GROUND_Y + 9.5f, 0).BoxTiled(1.3f, 1, 3, T.lateriteDark, 0, GROUND_Y + 19.3f, -2);

        // a great carved face on a stone column
        head = new Shape().BoxTiled(2.2f, 16.6f, 2.2f, T.stoneDark, 0, GROUND_Y + 8.3f, 0).BoxTiled(2.8f, 0.5f, 2.8f, T.stone, 0, 0.75f, 0);
        head.Add(mask, V.M(0, 1.0f, 0) * Matrix4x4.Scale(Vector3.one * 4.2f));

        hut = new Shape().BoxTiled(6.4f, 16.2f, 6.4f, T.lateriteDark, 0, GROUND_Y + 8.1f, 0).Add(new Shape().Model(hutM, 6, false), V.M(0, 0.2f, 0));

        ropePost = new Shape().BoxTiled(0.18f, 1.3f, 0.18f, T.plankDark, 0, 0.65f, 0, 1);
        stilt = new Shape().BoxTiled(0.5f, 16, 0.5f, T.plankDark, 0, GROUND_Y + 8, 0, 2);
        canoe = new Shape().BoxTiled(0.9f, 0.4f, 4.5f, T.barkLight, 0, GROUND_Y + 0.2f, 0, 2).Box(0.35f, 0.8f, 0.3f, T.Lam(0x2f5aa8), 0, GROUND_Y + 0.8f, 0.6f).Ball(0.3f, 0.32f, 0.3f, T.Lam(0x5a3420), 0, GROUND_Y + 1.38f, 0.6f);
        lamp = new Shape().Cyl(0.08f, 0.1f, 6, 8, T.metal, 0, 3, 0).Box(1.3f, 0.08f, 0.08f, T.metal, -0.6f, 6, 0).Box(0.5f, 0.12f, 0.25f, T.lampGlow, -1.2f, 5.92f, 0);

        billboards = new Shape[T.boards.Length];
        for (int i = 0; i < billboards.Length; i++) {
            Shape g = new Shape();
            foreach (float x in new[] { -2f, 2f }) g.Box(0.25f, 24, 0.25f, T.metal, x, GROUND_Y + 16, 0);
            g.Box6(6.4f, 3.2f, 0.15f, new[] { T.metal, T.metal, T.metal, T.metal, T.boards[i], T.metal }, 0, 8.5f, 0);   // the sign faces the oncoming runner
            billboards[i] = g;
        }

        // the shrine at the start: its door faces down the path, and the pedestal stands empty
        shrine = new Shape();
        shrine.BoxTiled(10, 0.4f, 8, T.laterite, 0, 0.2f, 0);
        shrine.BoxTiled(10, 16, 8, T.lateriteDark, 0, GROUND_Y + 8, 0);
        shrine.BoxTiled(8, 3.4f, 6, T.mud, 0, 2.1f, 0.4f);
        shrine.Cyl(0.2f, 6.6f, 3.8f, 4, T.thatch, 0, 5.7f, 0.4f, 0, PI / 4, 0);
        shrine.Box(2.2f, 2.6f, 0.12f, T.dark, 0, 1.7f, -2.62f);
        foreach (float x in new[] { -1.6f, 1.6f }) {
            shrine.Cyl(0.24f, 0.28f, 3.6f, 10, T.barkLight, x, 2.2f, -2.8f);
            shrine.Cyl(0.3f, 0.3f, 0.4f, 12, T.red, x, 3.1f, -2.8f); shrine.Cyl(0.3f, 0.3f, 0.3f, 12, T.white, x, 2.6f, -2.8f);
            shrine.Add(mask, V.M(x, 0.9f, -3.1f) * Matrix4x4.Scale(Vector3.one * 1.2f));
        }
        shrine.Box(4, 0.3f, 0.6f, T.red, 0, 3.7f, -2.8f);
        for (int i = 0; i < 6; i++) shrine.Add(pot, V.M(-3.6f + (i % 3) * 0.65f + (i > 2 ? 6.5f : 0), 0.4f, -3.4f - (i % 2) * 0.35f, 0, i, 0));
        shrine.BoxTiled(0.8f, 0.8f, 0.8f, T.stone, 0, 0.8f, -3.9f, 1);

        // obstacles
        root = new Shape().Cyl(0.38f, 0.42f, PATH_W + 1.2f, 12, T.bark, 0, 0.36f, 0, 0, 0, PI / 2);
        foreach (float x in new[] { -1.8f, 0.4f, 2.4f }) root.Cyl(0.12f, 0.2f, 1.1f, 7, T.barkLight, x, 0.62f, 0.1f, 0.5f, 0, Rand(-0.8f, 0.8f));
        arch = new Shape();
        foreach (float x in new[] { -HALF + 0.1f, HALF - 0.1f }) arch.BoxTiled(0.5f, 3.4f, 0.6f, T.stone, x, 1.7f, 0, 1.5f);
        arch.BoxTiled(PATH_W + 0.6f, 1.5f, 0.7f, T.stone, 0, 2.12f, 0, 1.5f);
        for (int i = -2; i <= 2; i++) arch.BoxTiled(0.5f, 0.5f, 0.06f, T.stoneDark, i * 1.2f, 2.1f, -0.37f, 1);
        arch.Box(PATH_W + 0.4f, 0.3f, 0.72f, T.red, 0, 1.45f, 0);
        fallenTree = new Shape().Cyl(0.6f, 0.7f, PATH_W + 3, 12, T.bark, 0, 2.05f, 0, 0, 0, PI / 2 - 0.05f);
        foreach (float x in new[] { -HALF - 0.2f, HALF + 0.2f }) fallenTree.Cyl(0.2f, 0.24f, 2, 8, T.barkLight, x, 1, 0);
        fallenTree.Add(banana, V.M(HALF + 1.3f, 1.2f, 0) * Matrix4x4.Scale(Vector3.one * 0.6f));
        gate = new Shape();   // visa barrier on the airport road
        foreach (float x in new[] { -HALF, HALF }) gate.Box(0.4f, 3.6f, 0.4f, T.metal, x, 1.8f, 0);
        gate.Box6(PATH_W + 0.4f, 1.4f, 0.3f, new[] { T.metal, T.metal, T.metal, T.metal, T.gateBoard, T.gateBoard }, 0, 2.1f, 0);
        bump = new Shape().Box(PATH_W, 0.55f, 0.9f, T.bumpYellow, 0, 0.27f, 0);
        for (int i = -3; i <= 3; i += 2) bump.Box(0.5f, 0.56f, 0.92f, T.bumpBlack, i * 0.9f, 0.28f, 0);

        // shrine figures: a carved post with a mask for a face, wrapped in cloth, some with offerings at the foot
        idol = new Shape[3];
        for (int v = 0; v < 3; v++) {
            Material wood = v == 2 ? T.stone : T.barkLight;
            Shape g = new Shape();
            g.BoxTiled(1.3f, 0.5f, 1.1f, T.stoneDark, 0, 0.25f, 0, 1.5f);
            g.Cyl(0.38f, 0.46f, 1.6f, 12, wood, 0, 1.3f, 0);
            g.Cyl(0.42f, 0.42f, 0.28f, 12, v == 1 ? T.white : T.red, 0, 1.0f, 0);
            g.Add(mask, V.M(0, 2.05f, -0.12f) * Matrix4x4.Scale(Vector3.one * 1.35f));
            if (v == 0) foreach (float x in new[] { -0.6f, 0.6f }) g.Add(pot, V.M(x, 0.5f, -0.3f) * Matrix4x4.Scale(Vector3.one * 0.7f));
            idol[v] = g;
        }
        bags = new Shape();   // Ghana-must-go bags piled up
        float[][] bg = { new[] { 0, 0.45f, 0, 1.5f, 0.9f, 1 }, new[] { 0.05f, 1.3f, 0.05f, 1.3f, 0.8f, 0.9f }, new[] { -0.1f, 2.05f, 0, 1.1f, 0.7f, 0.8f }, new[] { 0.1f, 2.7f, 0, 0.9f, 0.6f, 0.7f } };
        foreach (float[] b in bg) bags.BoxTiled(b[3], b[4], b[5], T.gmg, b[0], b[1], b[2], 3, Rand(-0.2f, 0.2f));
        fireRing = new Shape();
        for (int i = 0; i < 8; i++) { float a = i / 8f * PI * 2; fireRing.Ball(0.42f, 0.32f, 0.4f, T.stoneDark, Mathf.Sin(a) * 0.75f, 0.14f, Mathf.Cos(a) * 0.75f); }
        fireRing.Cyl(0.08f, 0.1f, 1.1f, 6, T.bark, 0, 0.12f, 0, 0, 0.4f, PI / 2).Cyl(0.08f, 0.1f, 1.1f, 6, T.bark, 0, 0.14f, 0, 0, -0.9f, PI / 2);
        flames = new Shape();
        float[][] fl = { new[] { 0, 0, 1.0f, 0.42f }, new[] { 0.25f, 0.15f, 0.75f, 0.3f }, new[] { -0.25f, -0.1f, 0.7f, 0.3f } };
        foreach (float[] f in fl) flames.Cyl(0.02f, f[3], f[2], 8, T.fire, f[0], f[2] / 2 + 0.1f, f[1]);
        flames.Cyl(0.01f, 0.2f, 0.55f, 7, T.fireCore, 0, 0.38f, 0);
        cone = new Shape().Cyl(0.03f, 0.32f, 0.85f, 12, T.coneOrange, 0, 0.43f, 0).Box(0.7f, 0.05f, 0.7f, T.coneOrange, 0, 0.02f, 0).Cyl(0.16f, 0.2f, 0.12f, 12, T.white, 0, 0.5f, 0);

        cowrie = new Shape().Ball(0.48f, 0.64f, 0.29f, T.cowrie).Box(0.06f, 0.42f, 0.04f, T.cowrieSlit, 0, 0, -0.14f);
        shadow = new Shape().Quad(1, 1, T.shadow, V.M(0, 0, 0, -PI / 2, 0, 0));
    }
}
