using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Menus and HUD, built in code: carved-wood panels edged with cowries over the 3D causeway.
public partial class JapaGame {
    static readonly Color INK = T.H(0x1a120c), WOOD = T.H(0xf3e4c4), SHRINE = T.H(0xb8321f), GOLD = T.H(0xe0a92a), LEAF = T.H(0x2f5d34), CHALK = T.H(0xfbf6ea), MUTED = T.H(0x6b4a2a);

    Canvas canvas; CanvasScaler scaler; RectTransform safe;
    float uiScale = 1, canvasW = 400, canvasH = 600;
    RectTransform startScreen, pauseScreen, overScreen, hud, areaBox, toastBox, powersBox;
    RectTransform startPanel, pausePanel, overPanel, picks, controls;
    readonly List<RawImage> stripes = new List<RawImage>();
    Text logoA, logoB, scoreText, coinsText, distText, areaName, toastMain, toastSub, bestStart, bestOver, overTitle, oScore, oCoins, oDist, oArea, tagline;
    GameObject newBest; CanvasGroup toastGroup; float toastTimer;
    Image muteIcon; Sprite muteOn, muteOff;
    readonly RectTransform[] powerRows = new RectTransform[3]; readonly RectTransform[] powerFill = new RectTransform[3];
    Texture2D stripeTex;
    static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

    static string Fmt(float n) { return Mathf.FloorToInt(n).ToString("N0", CultureInfo.InvariantCulture); }
    static string Hex(Color c) { return "#" + ColorUtility.ToHtmlStringRGB(c); }

    // ---------- building blocks ----------
    static RectTransform Node(string name, Transform parent) {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }
    static void Stretch(RectTransform r, float l = 0, float t = 0, float rgt = 0, float b = 0) {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = new Vector2(l, b); r.offsetMax = new Vector2(-rgt, -t);
    }
    static void Pin(RectTransform r, float ax, float ay, float px, float py) {
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(ax, ay); r.anchoredPosition = new Vector2(px, py);
    }
    // rounded rectangle with an optional border, drawn at twice the size for crisp edges and nine-sliced
    static Sprite Rounded(Color fill, Color border, float radius, float bw) {
        string key = fill + "|" + border + "|" + radius + "|" + bw;
        Sprite sp;
        if (sprites.TryGetValue(key, out sp)) return sp;
        const int S = 2;
        int r = Mathf.CeilToInt(radius * S), b = Mathf.RoundToInt(bw * S), n = r * 2 + 4;
        Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        Color[] px = new Color[n * n];
        for (int yy = 0; yy < n; yy++) for (int xx = 0; xx < n; xx++) {
            float fx = xx + 0.5f, fy = yy + 0.5f;
            float d = r - Vector2.Distance(new Vector2(fx, fy), new Vector2(Mathf.Clamp(fx, r, n - r), Mathf.Clamp(fy, r, n - r)));
            Color c = b > 0 ? Color.Lerp(border, fill, Mathf.Clamp01(d - b + 0.5f)) : fill;
            c.a *= Mathf.Clamp01(d + 0.5f);
            px[yy * n + xx] = c;
        }
        t.SetPixels(px); t.Apply(false, true);
        t.wrapMode = TextureWrapMode.Clamp;
        sp = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100 * S, 0, SpriteMeshType.FullRect, new Vector4(r + 1, r + 1, r + 1, r + 1));
        return sprites[key] = sp;
    }
    static Image Box(Transform parent, string name, Color fill, Color border, float radius, float bw) {
        Image img = Node(name, parent).gameObject.AddComponent<Image>();
        img.sprite = Rounded(fill, border, radius, bw); img.type = Image.Type.Sliced;
        return img;
    }
    static Text Txt(Transform parent, string s, Font f, int size, Color c, TextAnchor a = TextAnchor.UpperLeft) {
        Text t = Node("text", parent).gameObject.AddComponent<Text>();
        t.font = f; t.fontSize = size; t.color = c; t.alignment = a; t.text = s; t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; t.supportRichText = true;
        return t;
    }
    static void Stroke(Text t, float outline, float drop) {
        if (outline > 0) { Outline o = t.gameObject.AddComponent<Outline>(); o.effectColor = INK; o.effectDistance = new Vector2(outline, -outline); }
        if (drop > 0) { Shadow s = t.gameObject.AddComponent<Shadow>(); s.effectColor = INK; s.effectDistance = new Vector2(drop, -drop); }
    }
    static void DropShadow(GameObject go, float d) { Shadow s = go.AddComponent<Shadow>(); s.effectColor = INK; s.effectDistance = new Vector2(d, -d); }
    static VerticalLayoutGroup VLG(GameObject go, int l, int r, int t, int b, float spacing, TextAnchor align = TextAnchor.UpperLeft) {
        VerticalLayoutGroup g = go.AddComponent<VerticalLayoutGroup>();
        g.padding = new RectOffset(l, r, t, b); g.spacing = spacing; g.childAlignment = align;
        g.childControlWidth = true; g.childControlHeight = true; g.childForceExpandWidth = true; g.childForceExpandHeight = false;
        return g;
    }
    static HorizontalLayoutGroup HLG(GameObject go, int l, int r, int t, int b, float spacing, bool expand = false, TextAnchor align = TextAnchor.MiddleLeft) {
        HorizontalLayoutGroup g = go.AddComponent<HorizontalLayoutGroup>();
        g.padding = new RectOffset(l, r, t, b); g.spacing = spacing; g.childAlignment = align;
        g.childControlWidth = true; g.childControlHeight = true; g.childForceExpandWidth = expand; g.childForceExpandHeight = false;
        return g;
    }
    static LayoutElement LE(Component c, float minW = -1, float prefW = -1, float flexW = -1, float minH = -1, float prefH = -1) {
        LayoutElement e = c.gameObject.AddComponent<LayoutElement>();
        e.minWidth = minW; e.preferredWidth = prefW; e.flexibleWidth = flexW; e.minHeight = minH; e.preferredHeight = prefH;
        return e;
    }
    static void Fit(GameObject go, bool horizontal, bool vertical) {
        ContentSizeFitter f = go.AddComponent<ContentSizeFitter>();
        f.horizontalFit = horizontal ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
        f.verticalFit = vertical ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
    }
    Button Btn(Transform parent, string label, bool primary, UnityEngine.Events.UnityAction onClick) {
        Image img = Box(parent, "button", primary ? SHRINE : GOLD, INK, 8, 3);
        DropShadow(img.gameObject, 4);
        Button b = img.gameObject.AddComponent<Button>();
        b.navigation = new Navigation { mode = Navigation.Mode.None };
        b.onClick.AddListener(sound.Click);
        b.onClick.AddListener(onClick);
        LE(img, -1, 0, 1, primary ? 56 : 46);
        Text t = Txt(img.transform, label, T.Display, primary ? 22 : 17, primary ? CHALK : INK, TextAnchor.MiddleCenter);
        t.resizeTextForBestFit = true; t.resizeTextMinSize = 9; t.resizeTextMaxSize = primary ? 22 : 17;
        Stretch(t.rectTransform, 8, 4, 8, 4);
        return b;
    }
    RectTransform MakeScreen(string name, float dim) {
        RectTransform r = Node(name, canvas.transform);
        Stretch(r);
        Image bg = r.gameObject.AddComponent<Image>(); bg.color = new Color(INK.r, INK.g, INK.b, dim);
        return r;
    }
    // a carved-wood panel edged with a strip of cowries; returns the body to fill
    RectTransform MakePanel(RectTransform screen, out RectTransform panel) {
        Image img = Box(screen, "panel", WOOD, INK, 10, 3);
        DropShadow(img.gameObject, 6);
        panel = img.rectTransform;
        Pin(panel, 0.5f, 0.5f, 0, 0);
        VLG(panel.gameObject, 3, 3, 3, 3, 0);
        Fit(panel.gameObject, false, true);
        RawImage st = Node("cowries", panel).gameObject.AddComponent<RawImage>();
        st.texture = stripeTex; st.raycastTarget = false; LE(st, -1, -1, -1, 20, 20); stripes.Add(st);
        Image line = Node("line", panel).gameObject.AddComponent<Image>(); line.color = INK; line.raycastTarget = false; LE(line, -1, -1, -1, 3, 3);
        RectTransform body = Node("body", panel);
        VLG(body.gameObject, 22, 22, 16, 18, 12);
        return body;
    }
    RectTransform Chip(Transform parent, Color bg, bool cowrie, out Text value) {
        Image img = Box(parent, "chip", bg, bg, 14, 0);
        img.raycastTarget = false;
        HLG(img.gameObject, cowrie ? 6 : 12, 12, 5, 5, 6);
        if (cowrie) {
            Image c = Box(img.transform, "cowrie", CHALK, CHALK, 7, 0);
            c.raycastTarget = false; LE(c, 20, 20, -1, 14, 14);
            Image slit = Node("slit", c.transform).gameObject.AddComponent<Image>(); slit.color = T.H(0x6b5a3c); slit.raycastTarget = false;
            Pin(slit.rectTransform, 0.5f, 0.5f, 0, 0); slit.rectTransform.sizeDelta = new Vector2(12, 2);
        }
        value = Txt(img.transform, "0", T.Body, 15, CHALK, TextAnchor.MiddleLeft);
        value.horizontalOverflow = HorizontalWrapMode.Overflow;
        return img.rectTransform;
    }
    Sprite IconSprite(System.Action<LRCanvas> draw) {
        LRCanvas g = new LRCanvas(80, 80, new Color(INK.r, INK.g, INK.b, 0));
        draw(g);
        return Sprite.Create(g.End(false, false), new Rect(0, 0, 80, 80), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect);
    }
    Button IconBtn(Transform parent, Sprite icon, UnityEngine.Events.UnityAction onClick, out Image iconImage) {
        Image img = Box(parent, "icon-button", GOLD, INK, 12, 2);
        DropShadow(img.gameObject, 2);
        LE(img, 42, 42, -1, 42, 42);
        Button b = img.gameObject.AddComponent<Button>();
        b.navigation = new Navigation { mode = Navigation.Mode.None };
        b.onClick.AddListener(onClick);
        iconImage = Node("icon", img.transform).gameObject.AddComponent<Image>();
        iconImage.sprite = icon; iconImage.raycastTarget = false;
        Pin(iconImage.rectTransform, 0.5f, 0.5f, 0, 0); iconImage.rectTransform.sizeDelta = new Vector2(22, 22);
        return b;
    }
    string BestText() { return "BEST SCORE <color=" + Hex(INK) + ">" + Fmt(best) + "</color>"; }

    // ---------- build ----------
    void BuildUI() {
        GameObject cgo = new GameObject("UI", typeof(RectTransform));
        canvas = cgo.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        scaler = cgo.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        cgo.AddComponent<GraphicRaycaster>();
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        // a strip of cowries on shrine red, repeated along panel tops
        LRCanvas cg = new LRCanvas(44, 40, SHRINE);
        cg.Ellipse(22, 20, 14, 10, CHALK); cg.Line(13, 20, 31, 20, 2.5f, T.H(0x6b5a3c));
        stripeTex = cg.End(true, false);

        safe = Node("safe", canvas.transform); Stretch(safe);

        // HUD
        hud = Node("hud", safe); Stretch(hud);
        scoreText = Txt(hud, "0", T.Display, 34, CHALK); Stroke(scoreText, 2, 3);
        scoreText.horizontalOverflow = HorizontalWrapMode.Overflow;
        Pin(scoreText.rectTransform, 0, 1, 16, -12); scoreText.rectTransform.sizeDelta = new Vector2(300, 44);
        RectTransform chips = Node("chips", hud); Pin(chips, 0, 1, 16, -62);
        HLG(chips.gameObject, 0, 0, 0, 0, 8); Fit(chips.gameObject, true, true);
        Chip(chips, INK, true, out coinsText);
        Chip(chips, new Color(INK.r, INK.g, INK.b, 0.7f), false, out distText);
        RectTransform right = Node("buttons", hud); Pin(right, 1, 1, -16, -14);
        HLG(right.gameObject, 0, 0, 0, 0, 8); Fit(right.gameObject, true, true);
        muteOn = IconSprite(g => {
            g.Rect(12, 32, 12, 16, INK); g.Poly(INK, 24, 32, 40, 16, 40, 64, 24, 48);
            g.Ring(38, 40, 20, 8, INK, -0.78f, 0.78f); g.Ring(38, 40, 34, 8, INK, -0.78f, 0.78f);
        });
        muteOff = IconSprite(g => {
            g.Rect(12, 32, 12, 16, INK); g.Poly(INK, 24, 32, 40, 16, 40, 64, 24, 48);
            g.Line(52, 28, 72, 52, 8, INK); g.Line(72, 28, 52, 52, 8, INK);
        });
        Sprite pauseIcon = IconSprite(g => { g.Rect(16, 12, 16, 56, INK); g.Rect(48, 12, 16, 56, INK); });
        Image unused;
        IconBtn(right, sound.Muted ? muteOff : muteOn, ToggleMute, out muteIcon);
        IconBtn(right, pauseIcon, () => SetPaused(true), out unused);

        Image ab = Box(safe, "area", WOOD, INK, 6, 2); DropShadow(ab.gameObject, 2); ab.raycastTarget = false;
        areaBox = ab.rectTransform; Pin(areaBox, 0.5f, 0, 0, 16);
        HLG(areaBox.gameObject, 12, 12, 6, 6, 6); Fit(areaBox.gameObject, true, true);
        Text nowIn = Txt(areaBox, "NOW IN", T.Body, 10, SHRINE, TextAnchor.MiddleLeft); nowIn.horizontalOverflow = HorizontalWrapMode.Overflow;
        areaName = Txt(areaBox, "Sacred Grove", T.Display, 14, INK, TextAnchor.MiddleLeft); areaName.horizontalOverflow = HorizontalWrapMode.Overflow;

        powersBox = Node("powers", safe); Pin(powersBox, 0, 0, 16, 60);
        VLG(powersBox.gameObject, 0, 0, 0, 0, 6, TextAnchor.LowerLeft); Fit(powersBox.gameObject, true, true);
        for (int i = 0; i < 3; i++) {
            Image row = Box(powersBox, "power", new Color(INK.r, INK.g, INK.b, 0.8f), INK, 8, 0); row.raycastTarget = false;
            HLG(row.gameObject, 10, 10, 6, 6, 8); LE(row, 170);
            Text label = Txt(row.transform, POWER_INFO[POWER_KEYS[i]].Key, T.Body, 13, CHALK, TextAnchor.MiddleLeft); label.horizontalOverflow = HorizontalWrapMode.Overflow;
            Image bar = Node("bar", row.transform).gameObject.AddComponent<Image>(); bar.color = new Color(1, 1, 1, 0.2f); bar.raycastTarget = false;
            LE(bar, 50, 50, 1, 6, 6);
            Image fill = Node("fill", bar.transform).gameObject.AddComponent<Image>(); fill.color = GOLD; fill.raycastTarget = false;
            Stretch(fill.rectTransform);
            powerRows[i] = row.rectTransform; powerFill[i] = fill.rectTransform;
            row.gameObject.SetActive(false);
        }

        toastBox = Node("toast", safe);
        toastBox.anchorMin = toastBox.anchorMax = new Vector2(0.5f, 0.74f); toastBox.pivot = new Vector2(0.5f, 0.5f);
        toastBox.localRotation = Quaternion.Euler(0, 0, 2);
        VLG(toastBox.gameObject, 0, 0, 0, 0, 6, TextAnchor.MiddleCenter); Fit(toastBox.gameObject, false, true);
        toastMain = Txt(toastBox, "", T.Display, 28, GOLD, TextAnchor.MiddleCenter); Stroke(toastMain, 2, 3);
        toastSub = Txt(toastBox, "", T.Body, 15, CHALK, TextAnchor.MiddleCenter); Stroke(toastSub, 1, 1);
        toastGroup = toastBox.gameObject.AddComponent<CanvasGroup>(); toastGroup.alpha = 0; toastGroup.blocksRaycasts = false; toastGroup.interactable = false;

        // start: sits to the side so the shrine scene stays in view
        startScreen = MakeScreen("start", 0.15f);
        RectTransform body = MakePanel(startScreen, out startPanel);
        RectTransform logo = Node("logo", body); VLG(logo.gameObject, 0, 0, 0, 0, -6);
        logoA = Txt(logo, "Japa!", T.Display, 72, GOLD); Stroke(logoA, 3, 4);
        logoB = Txt(logo, "from the Shrine", T.Display, 26, SHRINE); Stroke(logoB, 1.5f, 0);
        tagline = Txt(body, "You don carry the sacred calabash comot from the shrine, and the Egungun don wake. Run through the grove, across the river and all the way to the airport. No look back!", T.BodySemi, 15, INK);
        tagline.lineSpacing = 1.1f;
        controls = Node("controls", body); VLG(controls.gameObject, 0, 0, 0, 0, 5);
        string[][] rows = Application.isMobilePlatform
            ? new[] { new[] { "Swipe left / right", "Turn at corners, or change lane" }, new[] { "Swipe up", "Jump roots, fire and gaps" }, new[] { "Swipe down", "Slide under arches and fallen trees" } }
            : new[] { new[] { "Left / Right", "Turn at corners, or change lane" }, new[] { "Up / Space", "Jump roots, fire and gaps" }, new[] { "Down", "Slide under arches and fallen trees" }, new[] { "Phone", "Swipe left, right, up or down" } };
        foreach (string[] kv in rows) {
            RectTransform row = Node("row", controls); HLG(row.gameObject, 0, 0, 0, 0, 10, false, TextAnchor.UpperLeft);
            LE(Txt(row, kv[0], T.Body, 13, INK), Application.isMobilePlatform ? 124 : 92);
            LE(Txt(row, kv[1], T.BodySemi, 13, INK), -1, -1, 1);
        }
        Txt(body, "WHO DEY RUN?", T.Body, 12, MUTED);
        picks = Node("picks", body); HLG(picks.gameObject, 0, 3, 0, 3, 6, true);
        RectTransform btns = Node("buttons", body); HLG(btns.gameObject, 0, 4, 0, 4, 0, true);
        Btn(btns, "Japa!", true, StartRun);
        bestStart = Txt(body, "", T.BodySemi, 12, MUTED);

        // pause
        pauseScreen = MakeScreen("pause", 0.4f);
        body = MakePanel(pauseScreen, out pausePanel);
        Txt(body, "Catch your breath", T.Display, 28, INK);
        btns = Node("buttons", body); HLG(btns.gameObject, 0, 4, 0, 4, 10, true);
        Btn(btns, "Continue", true, () => SetPaused(false));
        Btn(btns, "End run", false, EndRunFromPause);

        // game over
        overScreen = MakeScreen("over", 0.4f);
        body = MakePanel(overScreen, out overPanel);
        overTitle = Txt(body, "Wahala!", T.Display, 28, SHRINE);
        RectTransform nb = Node("new-best", body); HLG(nb.gameObject, 0, 0, 0, 0, 0);
        Image nbBadge = Box(nb, "badge", GOLD, INK, 12, 2); HLG(nbBadge.gameObject, 10, 10, 3, 3, 0);
        Txt(nbBadge.transform, "New best score!", T.Body, 13, INK, TextAnchor.MiddleCenter).horizontalOverflow = HorizontalWrapMode.Overflow;
        newBest = nb.gameObject;
        RectTransform stats = Node("stats", body); VLG(stats.gameObject, 0, 0, 0, 0, 10);
        RectTransform r1 = Node("row", stats), r2 = Node("row", stats);
        HLG(r1.gameObject, 0, 0, 0, 0, 10, true); HLG(r2.gameObject, 0, 0, 0, 0, 10, true);
        oScore = Stat(r1, "SCORE", 22); oCoins = Stat(r1, "COWRIES", 22); oDist = Stat(r2, "DISTANCE", 22); oArea = Stat(r2, "REACHED", 16);
        btns = Node("buttons", body); HLG(btns.gameObject, 0, 4, 0, 4, 10, true);
        Btn(btns, "Japa again", true, StartRun);
        Btn(btns, "Change runner", false, ToMenu);
        bestOver = Txt(body, "", T.BodySemi, 12, MUTED);

        RenderPicks();
        ShowScreen(startScreen);
    }
    Text Stat(Transform parent, string label, int size) {
        Image box = Box(parent, "stat", Color.white, INK, 8, 2); box.raycastTarget = false;
        VLG(box.gameObject, 10, 10, 8, 8, 2); LE(box, 0, 0, 1);
        Txt(box.transform, label, T.Body, 11, MUTED);
        Text v = Txt(box.transform, "0", T.Display, size, INK);
        v.resizeTextForBestFit = true; v.resizeTextMinSize = 10; v.resizeTextMaxSize = size;
        LE(v, -1, -1, -1, 28, 28);
        return v;
    }
    // the four runners, as a row of buttons
    void RenderPicks() {
        for (int i = picks.childCount - 1; i >= 0; i--) { Transform c = picks.GetChild(i); c.SetParent(null); Destroy(c.gameObject); }
        foreach (CharDef c in CHARS) {
            bool on = c.id == charId;
            Image img = Box(picks, "pick", on ? GOLD : Color.white, INK, 8, 2);
            if (on) DropShadow(img.gameObject, 2);
            VLG(img.gameObject, 4, 4, 8, 8, 4, TextAnchor.UpperCenter); LE(img, 0, 0, 1);
            Button b = img.gameObject.AddComponent<Button>();
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            string id = c.id;
            b.onClick.AddListener(sound.Click);
            b.onClick.AddListener(() => PickChar(id));
            RectTransform holder = Node("sw", img.transform); LE(holder, -1, -1, -1, 24, 24);
            Image sw = Box(holder, "swatch", T.H(c.swA), INK, 12, 2); sw.raycastTarget = false;
            Pin(sw.rectTransform, 0.5f, 0.5f, 0, 0); sw.rectTransform.sizeDelta = new Vector2(24, 24);
            Image half = Box(sw.transform, "half", T.H(c.swB), T.H(c.swB), 6, 0); half.raycastTarget = false;
            Pin(half.rectTransform, 0.5f, 0.5f, 4, -4); half.rectTransform.sizeDelta = new Vector2(11, 11);
            Text name = Txt(img.transform, c.name, T.Body, 12, INK, TextAnchor.MiddleCenter);
            name.resizeTextForBestFit = true; name.resizeTextMinSize = 8; name.resizeTextMaxSize = 12; LE(name, -1, -1, -1, 16, 16);
        }
        bestStart.text = BestText();
    }

    // ---------- layout ----------
    void LayoutUI() {
        uiScale = Mathf.Clamp(Mathf.Min(Screen.width / 400f, Screen.height / 600f), 0.5f, 4);
        scaler.scaleFactor = uiScale;
        canvasW = Screen.width / uiScale; canvasH = Screen.height / uiScale;
        Rect sa = Screen.safeArea;
        safe.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
        safe.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
        safe.offsetMin = safe.offsetMax = Vector2.zero;
        float pw = Mathf.Min(440, canvasW - 32);
        foreach (RectTransform p in new[] { pausePanel, overPanel }) p.sizeDelta = new Vector2(pw, p.sizeDelta.y);
        bool wide = canvasW >= 760;
        // desktop: to the left, so the shrine shows on the right; phone: along the bottom, without the key list
        if (wide) { Pin(startPanel, 0, 0.5f, Mathf.Max(16, canvasW * 0.05f), 0); startPanel.sizeDelta = new Vector2(Mathf.Min(420, canvasW * 0.45f), startPanel.sizeDelta.y); }
        else { Pin(startPanel, 0.5f, 0, 0, 12); startPanel.sizeDelta = new Vector2(pw, startPanel.sizeDelta.y); }
        controls.gameObject.SetActive(wide);
        int logoSize = Mathf.RoundToInt(Mathf.Clamp(canvasW * (wide ? 0.07f : 0.14f), 48, 84));
        logoA.fontSize = logoSize; logoB.fontSize = Mathf.RoundToInt(logoSize * 0.36f);
        scoreText.fontSize = Mathf.RoundToInt(Mathf.Clamp(canvasW * 0.06f, 26, 40));
        toastMain.fontSize = Mathf.RoundToInt(Mathf.Clamp(canvasW * 0.055f, 22, 34));
        toastBox.sizeDelta = new Vector2(Mathf.Min(canvasW - 32, 640), toastBox.sizeDelta.y);
    }
    void TickUI(float dt) {
        foreach (RawImage st in stripes) if (st.gameObject.activeInHierarchy) st.uvRect = new Rect(0, 0, st.rectTransform.rect.width / 22f, 1);
        if (toastTimer > 0) toastTimer -= dt;
        toastGroup.alpha = Mathf.MoveTowards(toastGroup.alpha, toastTimer > 0 ? 1 : 0, dt * 5);
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null) EventSystem.current.SetSelectedGameObject(null);
    }

    // ---------- screens ----------
    void ShowScreen(RectTransform which) {
        foreach (RectTransform sc in new[] { startScreen, pauseScreen, overScreen }) sc.gameObject.SetActive(sc == which);
        bool playing = which == null || which == pauseScreen;
        hud.gameObject.SetActive(playing); areaBox.gameObject.SetActive(playing); powersBox.gameObject.SetActive(playing);
    }
    void ShowArea(string name) { if (areaName != null) areaName.text = name; }
    void Toast(string text, float seconds, string sub) {
        toastMain.text = text;
        toastSub.text = sub ?? ""; toastSub.gameObject.SetActive(!string.IsNullOrEmpty(sub));
        toastTimer = seconds;
    }
    void PaintHud() {
        scoreText.text = Fmt(score); coinsText.text = Fmt(coins); distText.text = Fmt(dist) + " m";
        for (int i = 0; i < 3; i++) {
            float left = power[POWER_KEYS[i]];
            if (powerRows[i].gameObject.activeSelf != left > 0) powerRows[i].gameObject.SetActive(left > 0);
            if (left > 0) powerFill[i].anchorMax = new Vector2(left / POWER_INFO[POWER_KEYS[i]].Value, 1);
        }
    }
    void PaintOver(int final, bool isBest) {
        overTitle.text = reason;
        newBest.SetActive(isBest);
        oScore.text = Fmt(final); oCoins.text = Fmt(coins); oDist.text = Fmt(dist) + " m"; oArea.text = ZONES[ZoneIndexAt(dist)].name;
        bestOver.text = BestText(); bestStart.text = BestText();
        ShowScreen(overScreen);
        toastTimer = 0;
    }
    void ToggleMute() { muteIcon.sprite = sound.Toggle() ? muteOff : muteOn; }
}
