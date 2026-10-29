using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Menus and HUD, built in code: hand-painted danfo-sign panels over the 3D road.
public partial class LRGame {
    static readonly Color INK = T.H(0x16120c), PAPER = T.H(0xfff7e2), DANFO = T.H(0xf5c518), NAIJA = T.H(0x0a8a4a), BRICK = T.H(0xc8452e), MUTED = T.H(0x6b5a3c);

    Canvas canvas; CanvasScaler scaler; RectTransform safe;
    float uiScale = 1, canvasW = 400, canvasH = 600;
    RectTransform startScreen, pauseScreen, overScreen, charScreen, hud, areaBox, toastBox, powersBox;
    RectTransform startPanel, pausePanel, overPanel, charPanel;
    readonly List<RawImage> stripes = new List<RawImage>();
    Text logoA, logoB, scoreText, coinsText, distText, areaName, areaCrew, toastMain, toastSub, bestStart, bestOver, overTitle, oScore, oCoins, oDist, oArea, earnedText, charLine, charPurse, charTitle, introText, cityName, cityTag;
    GameObject newBest; RectTransform charList; CanvasGroup toastGroup; float toastTimer;
    Image muteIcon; Sprite muteOn, muteOff;
    readonly RectTransform[] powerRows = new RectTransform[3]; readonly RectTransform[] powerFill = new RectTransform[3];
    Texture2D stripeTex;
    static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

    // Archivo has no naira sign and browsers give Unity no fallback font, so body text on the web uses "N"
    static readonly string NGN = Application.platform == RuntimePlatform.WebGLPlayer ? "N" : "₦";
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
        Image img = Box(parent, "button", primary ? NAIJA : DANFO, INK, 8, 3);
        DropShadow(img.gameObject, 4);
        Button b = img.gameObject.AddComponent<Button>();
        b.navigation = new Navigation { mode = Navigation.Mode.None };
        b.onClick.AddListener(sound.Click);
        b.onClick.AddListener(onClick);
        LE(img, -1, 0, 1, primary ? 56 : 46);
        Text t = Txt(img.transform, label, T.Display, primary ? 20 : 16, primary ? Color.white : INK, TextAnchor.MiddleCenter);
        t.resizeTextForBestFit = true; t.resizeTextMinSize = 9; t.resizeTextMaxSize = primary ? 20 : 16;
        Stretch(t.rectTransform, 8, 4, 8, 4);
        return b;
    }
    RectTransform MakeScreen(string name, float dim) {
        RectTransform r = Node(name, canvas.transform);
        Stretch(r);
        Image bg = r.gameObject.AddComponent<Image>(); bg.color = new Color(INK.r, INK.g, INK.b, dim);
        return r;
    }
    // a paper panel with a danfo-stripe header; returns the body to fill
    RectTransform MakePanel(RectTransform screen, out RectTransform panel) {
        Image img = Box(screen, "panel", PAPER, INK, 10, 3);
        DropShadow(img.gameObject, 6);
        panel = img.rectTransform;
        Pin(panel, 0.5f, 0.5f, 0, 0);
        VLG(panel.gameObject, 3, 3, 3, 3, 0);
        Fit(panel.gameObject, false, true);
        RawImage st = Node("stripes", panel).gameObject.AddComponent<RawImage>();
        st.texture = stripeTex; st.raycastTarget = false; LE(st, -1, -1, -1, 18, 18); stripes.Add(st);
        Image line = Node("line", panel).gameObject.AddComponent<Image>(); line.color = INK; line.raycastTarget = false; LE(line, -1, -1, -1, 3, 3);
        RectTransform body = Node("body", panel);
        VLG(body.gameObject, 22, 22, 18, 20, 14);
        return body;
    }
    RectTransform Chip(Transform parent, Color bg, bool coin, out Text value) {
        Image img = Box(parent, "chip", bg, bg, 14, 0);
        img.raycastTarget = false;
        HLG(img.gameObject, coin ? 6 : 12, 12, 5, 5, 6);
        if (coin) {
            Image c = Box(img.transform, "coin", DANFO, T.H(0xc99a00), 10, 2);
            c.raycastTarget = false; LE(c, 20, 20, -1, 20, 20);
            Text n = Txt(c.transform, "₦", T.Display, 11, INK, TextAnchor.MiddleCenter); Stretch(n.rectTransform);
        }
        value = Txt(img.transform, "0", T.Body, 15, PAPER, TextAnchor.MiddleLeft);
        value.horizontalOverflow = HorizontalWrapMode.Overflow;
        return img.rectTransform;
    }
    Sprite IconSprite(System.Action<LRCanvas> draw) {
        LRCanvas g = new LRCanvas(80, 80, new Color(INK.r, INK.g, INK.b, 0));
        draw(g);
        return Sprite.Create(g.End(false, false), new Rect(0, 0, 80, 80), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect);
    }
    Button IconBtn(Transform parent, Sprite icon, UnityEngine.Events.UnityAction onClick, out Image iconImage) {
        Image img = Box(parent, "icon-button", DANFO, INK, 12, 2);
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
    Text BestLine(Transform parent) {
        Text t = Txt(parent, "", T.BodySemi, 12, MUTED);
        return t;
    }
    string BestText() { return "BEST SCORE <color=" + Hex(INK) + ">" + Fmt(best) + "</color>  ·  PURSE <color=" + Hex(INK) + ">" + NGN + Fmt(purse) + "</color>"; }

    // ---------- build ----------
    void BuildUI() {
        GameObject cgo = new GameObject("UI", typeof(RectTransform));
        canvas = cgo.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        scaler = cgo.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        cgo.AddComponent<GraphicRaycaster>();
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        stripeTex = new Texture2D(2, 1, TextureFormat.RGBA32, false);
        stripeTex.SetPixels(new[] { INK, DANFO }); stripeTex.Apply();
        stripeTex.filterMode = FilterMode.Point; stripeTex.wrapMode = TextureWrapMode.Repeat;

        safe = Node("safe", canvas.transform); Stretch(safe);

        // HUD
        hud = Node("hud", safe); Stretch(hud);
        scoreText = Txt(hud, "0", T.Display, 34, PAPER); Stroke(scoreText, 2, 3);
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

        Image ab = Box(safe, "area", PAPER, INK, 6, 2); DropShadow(ab.gameObject, 2); ab.raycastTarget = false;
        areaBox = ab.rectTransform; Pin(areaBox, 0.5f, 0, 0, 16);
        HLG(areaBox.gameObject, 12, 12, 6, 6, 6); Fit(areaBox.gameObject, true, true);
        Text nowIn = Txt(areaBox, "NOW IN", T.BodySemi, 10, BRICK, TextAnchor.MiddleLeft); nowIn.horizontalOverflow = HorizontalWrapMode.Overflow;
        areaName = Txt(areaBox, "Ikorodu", T.Display, 13, INK, TextAnchor.MiddleLeft); areaName.horizontalOverflow = HorizontalWrapMode.Overflow;
        Image badge = Box(areaBox, "crew", INK, INK, 4, 0); badge.raycastTarget = false; HLG(badge.gameObject, 7, 7, 2, 2, 0);
        areaCrew = Txt(badge.transform, "Agberos", T.Body, 11, DANFO, TextAnchor.MiddleLeft); areaCrew.horizontalOverflow = HorizontalWrapMode.Overflow;

        powersBox = Node("powers", safe); Pin(powersBox, 0, 0, 16, 60);
        VLG(powersBox.gameObject, 0, 0, 0, 0, 6, TextAnchor.LowerLeft); Fit(powersBox.gameObject, true, true);
        for (int i = 0; i < 3; i++) {
            Image row = Box(powersBox, "power", new Color(INK.r, INK.g, INK.b, 0.78f), INK, 8, 0); row.raycastTarget = false;
            HLG(row.gameObject, 10, 10, 6, 6, 8); LE(row, 170);
            Text label = Txt(row.transform, POWER_INFO[POWER_KEYS[i]].Key, T.Body, 13, PAPER, TextAnchor.MiddleLeft); label.horizontalOverflow = HorizontalWrapMode.Overflow;
            Image bar = Node("bar", row.transform).gameObject.AddComponent<Image>(); bar.color = new Color(1, 1, 1, 0.2f); bar.raycastTarget = false;
            LE(bar, 50, 50, 1, 6, 6);
            Image fill = Node("fill", bar.transform).gameObject.AddComponent<Image>(); fill.color = DANFO; fill.raycastTarget = false;
            Stretch(fill.rectTransform);
            powerRows[i] = row.rectTransform; powerFill[i] = fill.rectTransform;
            row.gameObject.SetActive(false);
        }

        toastBox = Node("toast", safe);
        toastBox.anchorMin = toastBox.anchorMax = new Vector2(0.5f, 0.74f); toastBox.pivot = new Vector2(0.5f, 0.5f);
        toastBox.localRotation = Quaternion.Euler(0, 0, 3);
        VLG(toastBox.gameObject, 0, 0, 0, 0, 6, TextAnchor.MiddleCenter); Fit(toastBox.gameObject, false, true);
        toastMain = Txt(toastBox, "", T.Display, 26, DANFO, TextAnchor.MiddleCenter); Stroke(toastMain, 2, 3);
        toastSub = Txt(toastBox, "", T.Body, 15, PAPER, TextAnchor.MiddleCenter); Stroke(toastSub, 1, 1);
        toastGroup = toastBox.gameObject.AddComponent<CanvasGroup>(); toastGroup.alpha = 0; toastGroup.blocksRaycasts = false; toastGroup.interactable = false;

        // start
        startScreen = MakeScreen("start", 0.45f);
        RectTransform body = MakePanel(startScreen, out startPanel);
        RectTransform logo = Node("logo", body); HLG(logo.gameObject, 0, 0, 0, 0, 0);
        logoA = Txt(logo, "RUN", T.Display, 60, DANFO); Stroke(logoA, 3, 4); logoA.horizontalOverflow = HorizontalWrapMode.Overflow;
        logoB = Txt(logo, "TOWN", T.Display, 60, NAIJA); Stroke(logoB, 3, 4); logoB.horizontalOverflow = HorizontalWrapMode.Overflow;
        // which town to run: arrows either side of its name
        RectTransform pick = Node("city", body); HLG(pick.gameObject, 0, 4, 0, 4, 10);
        LayoutElement arrow = Btn(pick, "<", false, () => SetCity(cityIdx - 1)).GetComponent<LayoutElement>(); arrow.minWidth = arrow.preferredWidth = 46; arrow.flexibleWidth = 0;
        Image plate = Box(pick, "plate", Color.white, INK, 8, 2); plate.raycastTarget = false;
        VLG(plate.gameObject, 8, 8, 5, 5, 0, TextAnchor.MiddleCenter); LE(plate, -1, 0, 1, 46);
        cityName = Txt(plate.transform, "", T.Display, 18, INK, TextAnchor.MiddleCenter);
        cityTag = Txt(plate.transform, "", T.BodySemi, 11, MUTED, TextAnchor.MiddleCenter);
        arrow = Btn(pick, ">", false, () => SetCity(cityIdx + 1)).GetComponent<LayoutElement>(); arrow.minWidth = arrow.preferredWidth = 46; arrow.flexibleWidth = 0;
        introText = Txt(body, "", T.BodySemi, 14, INK); introText.lineSpacing = 1.1f;
        RectTransform controls = Node("controls", body); VLG(controls.gameObject, 0, 0, 0, 0, 5);
        string[][] rows = Application.isMobilePlatform
            ? new[] { new[] { "Swipe left / right", "Change lane" }, new[] { "Swipe up", "Jump, even onto danfo roofs" }, new[] { "Swipe down", "Slide under banners" } }
            : new[] { new[] { "Left / Right", "Change lane" }, new[] { "Up / Space", "Jump, even onto danfo roofs" }, new[] { "Down", "Slide under banners" }, new[] { "Phone", "Swipe left, right, up or down" } };
        foreach (string[] kv in rows) {
            RectTransform row = Node("row", controls); HLG(row.gameObject, 0, 0, 0, 0, 10, false, TextAnchor.UpperLeft);
            LE(Txt(row, kv[0], T.Body, 13, INK), Application.isMobilePlatform ? 124 : 92);
            LE(Txt(row, kv[1], T.BodySemi, 13, INK), -1, -1, 1);
        }
        RectTransform btns = Node("buttons", body); HLG(btns.gameObject, 0, 4, 0, 4, 10, true);
        Btn(btns, "Start running", true, StartRun);
        Btn(btns, "Characters", false, OpenChars);
        bestStart = BestLine(body);

        // pause
        pauseScreen = MakeScreen("pause", 0.45f);
        body = MakePanel(pauseScreen, out pausePanel);
        Txt(body, "Hold up, small break", T.Display, 28, INK);
        btns = Node("buttons", body); HLG(btns.gameObject, 0, 4, 0, 4, 10, true);
        Btn(btns, "Continue", true, () => SetPaused(false));
        Btn(btns, "End run", false, EndRunFromPause);

        // game over
        overScreen = MakeScreen("over", 0.45f);
        body = MakePanel(overScreen, out overPanel);
        overTitle = Txt(body, "Wahala!", T.Display, 28, BRICK);
        RectTransform nb = Node("new-best", body); HLG(nb.gameObject, 0, 0, 0, 0, 0);
        Image nbBadge = Box(nb, "badge", DANFO, INK, 12, 2); HLG(nbBadge.gameObject, 10, 10, 3, 3, 0);
        Txt(nbBadge.transform, "New best score!", T.Body, 13, INK, TextAnchor.MiddleCenter).horizontalOverflow = HorizontalWrapMode.Overflow;
        newBest = nb.gameObject;
        RectTransform stats = Node("stats", body); VLG(stats.gameObject, 0, 0, 0, 0, 10);
        RectTransform r1 = Node("row", stats), r2 = Node("row", stats);
        HLG(r1.gameObject, 0, 0, 0, 0, 10, true); HLG(r2.gameObject, 0, 0, 0, 0, 10, true);
        oScore = Stat(r1, "SCORE", 22); oCoins = Stat(r1, "NAIRA", 22); oDist = Stat(r2, "DISTANCE", 22); oArea = Stat(r2, "REACHED", 15);
        earnedText = Txt(body, "", T.Body, 14, NAIJA);
        btns = Node("buttons", body); HLG(btns.gameObject, 0, 4, 0, 4, 10, true);
        Btn(btns, "Run again", true, StartRun);
        Btn(btns, "Characters", false, OpenChars);
        bestOver = BestLine(body);

        // character picker: panel to the side so the runner stays in view
        charScreen = MakeScreen("characters", 0.12f);
        body = MakePanel(charScreen, out charPanel);
        body.GetComponent<VerticalLayoutGroup>().spacing = 10;
        RectTransform head = Node("head", body); HLG(head.gameObject, 0, 0, 0, 0, 10);
        charTitle = Txt(head, "Pick your runner", T.Display, 22, INK, TextAnchor.MiddleLeft); LE(charTitle, -1, -1, 1);
        Chip(head, INK, true, out charPurse);
        charLine = Txt(body, "", T.BodySemi, 14, INK); LE(charLine, -1, -1, -1, 36);
        charList = Node("list", body); VLG(charList.gameObject, 0, 0, 0, 0, 8);
        btns = Node("buttons", body); HLG(btns.gameObject, 0, 4, 0, 4, 0, true);
        Btn(btns, "Done", true, CloseChars);

        ShowScreen(startScreen);
        PaintPurse();
        PaintCity();
    }
    void PaintCity() {
        City c = CITIES[cityIdx];
        cityName.text = c.name.ToUpperInvariant(); cityTag.text = c.state + "  ·  " + c.motto;
        introText.text = c.intro;
    }
    Text Stat(Transform parent, string label, int size) {
        Image box = Box(parent, "stat", Color.white, INK, 8, 2); box.raycastTarget = false;
        VLG(box.gameObject, 10, 10, 8, 8, 2); LE(box, -1, 0, 1);
        Txt(box.transform, label, T.Body, 11, MUTED);
        Text v = Txt(box.transform, "0", T.Display, size, INK);
        v.resizeTextForBestFit = true; v.resizeTextMinSize = 10; v.resizeTextMaxSize = size;
        LE(v, -1, -1, -1, 28, 28);
        return v;
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
        foreach (RectTransform p in new[] { startPanel, pausePanel, overPanel }) p.sizeDelta = new Vector2(pw, p.sizeDelta.y);
        int logoSize = Mathf.RoundToInt(Mathf.Clamp(canvasW * 0.125f, 40, 60));
        logoA.fontSize = logoB.fontSize = logoSize;
        scoreText.fontSize = Mathf.RoundToInt(Mathf.Clamp(canvasW * 0.06f, 26, 40));
        toastMain.fontSize = Mathf.RoundToInt(Mathf.Clamp(canvasW * 0.05f, 20, 30));
        toastBox.sizeDelta = new Vector2(Mathf.Min(canvasW - 32, 640), toastBox.sizeDelta.y);
        bool wide = canvasW >= 760;
        if (wide) { Pin(charPanel, 0, 0.5f, Mathf.Max(16, canvasW * 0.05f), 0); charPanel.sizeDelta = new Vector2(Mathf.Min(420, canvasW * 0.45f), charPanel.sizeDelta.y); }
        else { Pin(charPanel, 0.5f, 0, 0, 12); charPanel.sizeDelta = new Vector2(pw, charPanel.sizeDelta.y); }
        if (charView) MeasureCover();
    }
    void TickUI(float dt) {
        foreach (RawImage st in stripes) if (st.gameObject.activeInHierarchy) st.uvRect = new Rect(0, 0, st.rectTransform.rect.width / 28f, 1);
        if (toastTimer > 0) toastTimer -= dt;
        toastGroup.alpha = Mathf.MoveTowards(toastGroup.alpha, toastTimer > 0 ? 1 : 0, dt * 5);
        // stop Space and Enter from re-pressing whichever button was clicked last
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null) EventSystem.current.SetSelectedGameObject(null);
    }

    // ---------- screens ----------
    void ShowScreen(RectTransform which) {
        foreach (RectTransform s in new[] { startScreen, pauseScreen, overScreen, charScreen }) s.gameObject.SetActive(s == which);
        bool playing = which == null || which == pauseScreen;
        hud.gameObject.SetActive(playing); areaBox.gameObject.SetActive(playing); powersBox.gameObject.SetActive(playing);
    }
    void ShowArea(int i) { areaName.text = ZONES[i].name; areaCrew.text = CREWS[ZONES[i].crew].label; }
    void Toast(string text, float seconds, string sub) {
        toastMain.text = text;
        toastSub.text = sub ?? ""; toastSub.gameObject.SetActive(!string.IsNullOrEmpty(sub));
        toastTimer = seconds;
    }
    void PaintPurse() {
        bestStart.text = BestText(); bestOver.text = BestText();
        charPurse.text = Fmt(purse);
    }
    void PaintHud() {
        scoreText.text = Fmt(score); coinsText.text = Fmt(coins); distText.text = Fmt(distance) + " m";
        for (int i = 0; i < 3; i++) {
            float left = power[POWER_KEYS[i]];
            if (powerRows[i].gameObject.activeSelf != left > 0) powerRows[i].gameObject.SetActive(left > 0);
            if (left > 0) powerFill[i].anchorMax = new Vector2(left / POWER_INFO[POWER_KEYS[i]].Value, 1);
        }
    }
    void PaintOver(int final, bool isBest, int earned) {
        overTitle.text = reason;
        earnedText.text = earned > 0 ? "+" + NGN + Fmt(earned) + " added to your purse" : "No naira this time. Grab the coins!";
        newBest.SetActive(isBest);
        oScore.text = Fmt(final); oCoins.text = "₦" + Fmt(coins * 10); oDist.text = Fmt(distance) + " m"; oArea.text = ZONES[ZoneIndexAt(distance)].name + ", " + CITIES[cityIdx].name;
        PaintPurse();
        ShowScreen(overScreen);
        toastTimer = 0;
    }
    void ToggleMute() { muteIcon.sprite = sound.Toggle() ? muteOff : muteOn; }

    // ---------- character picker ----------
    void RenderChars(string msg) {
        for (int i = charList.childCount - 1; i >= 0; i--) { Transform c = charList.GetChild(i); c.SetParent(null); Destroy(c.gameObject); }
        foreach (string gname in new[] { "Ladies", "Gents", "Kids" }) {
            Txt(charList, gname.ToUpperInvariant(), T.Body, 11, MUTED);
            RectTransform row = null; int inRow = 0;
            foreach (CharDef c in CHARS) {
                if (c.group != gname) continue;
                if (row == null || inRow == 2) { row = Node("row", charList); HLG(row.gameObject, 0, 4, 0, 4, 8, true); inRow = 0; }
                CharButton(row, c); inRow++;
            }
            if (inRow == 1) LE(Node("spacer", row), -1, 0, 1);
        }
        CharDef cur = CharById(charId);
        charLine.text = msg ?? "<b>" + cur.name + ":</b> “" + cur.line + "”";
        PaintPurse();
    }
    void CharButton(Transform parent, CharDef c) {
        bool have = owned.Contains(c.id), on = c.id == charId;
        Image img = Box(parent, "char", on ? DANFO : have ? Color.white : T.H(0xf3ead6), INK, 8, 2);
        if (on) DropShadow(img.gameObject, 3);
        HLG(img.gameObject, 8, 6, 6, 6, 8); LE(img, 0, 0, 1);
        Button b = img.gameObject.AddComponent<Button>();
        b.navigation = new Navigation { mode = Navigation.Mode.None };
        string id = c.id;
        b.onClick.AddListener(() => PickChar(id));
        Image sw = Box(img.transform, "swatch", T.H(c.swA), INK, 5, 2); sw.raycastTarget = false; LE(sw, 24, 24, -1, 24, 24);
        Image half = Node("half", sw.transform).gameObject.AddComponent<Image>(); half.color = T.H(c.swB); half.raycastTarget = false;
        half.rectTransform.anchorMin = new Vector2(0.5f, 0); half.rectTransform.anchorMax = Vector2.one; half.rectTransform.offsetMin = new Vector2(0, 2); half.rectTransform.offsetMax = new Vector2(-2, -2);
        RectTransform txt = Node("text", img.transform); VLG(txt.gameObject, 0, 0, 0, 0, 0); LE(txt, -1, 0, 1);
        Txt(txt, c.name, T.Body, 13, INK);
        Txt(txt, c.role, T.BodySemi, 11, T.H(0x5b4c33));
        string state = on ? "Running now" : have ? "Tap to pick" : purse >= c.price ? "Buy for " + NGN + Fmt(c.price) : NGN + Fmt(c.price);
        Txt(txt, state, T.Body, 11, have ? INK : BRICK);
    }
    void PickChar(string id) {
        CharDef c = CharById(id);
        if (!owned.Contains(c.id)) {
            if (purse < c.price) { RenderChars(c.name + " costs " + NGN + Fmt(c.price) + ". You need " + NGN + Fmt(c.price - purse) + " more. Collect naira on the road."); MeasureCover(); return; }
            purse -= c.price; owned.Add(c.id); sound.Buy();
        }
        SetCharacter(c.id);
        RenderChars(null); MeasureCover();
    }
    void OpenChars() {
        if (mode == "over") { ResetRun(); mode = "menu"; }
        if (mode != "menu") return;
        charView = true;
        ShowScreen(charScreen);
        RenderChars(null); MeasureCover();
    }
    void MeasureCover() {
        LayoutRebuilder.ForceRebuildLayoutImmediate(charPanel);
        charCover = charPanel.rect.height / canvasH;
    }
    void CloseChars() {
        charView = false;
        ShowScreen(startScreen);
        PaintPurse();
    }
}
