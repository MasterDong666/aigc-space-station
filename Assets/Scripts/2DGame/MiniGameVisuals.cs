using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 小游戏专用视觉层。只负责背景、圆角、阴影与微交互，不承载玩法状态。
/// 这样轨道、生态和基因任务可以共享一套触感，又不会影响空间站与主流程 UI。
/// </summary>
public enum MiniGameThemeId
{
    Orbit,
    Ecology,
    Gene,
}

public readonly struct MiniGameTheme
{
    public readonly string resourcePath;
    public readonly Color accent;
    public readonly Color accentWarm;
    public readonly Color ink;
    public readonly Color card;
    public readonly Color cardSoft;
    public readonly Color veil;

    public MiniGameTheme(
        string resourcePath,
        Color accent,
        Color accentWarm,
        Color ink,
        Color card,
        Color cardSoft,
        Color veil
    )
    {
        this.resourcePath = resourcePath;
        this.accent = accent;
        this.accentWarm = accentWarm;
        this.ink = ink;
        this.card = card;
        this.cardSoft = cardSoft;
        this.veil = veil;
    }
}

public static class MiniGameVisuals
{
    private static Sprite roundedSprite;
    private static Sprite circleSprite;

    public static MiniGameTheme Theme(MiniGameThemeId id)
    {
        switch (id)
        {
            case MiniGameThemeId.Ecology:
                return new MiniGameTheme(
                    "MinigameArt/EcologyBackdrop",
                    Hex("#7CE8A2"),
                    Hex("#FFC65C"),
                    Hex("#173B36"),
                    new Color(0.025f, 0.15f, 0.14f, 0.92f),
                    new Color(0.06f, 0.24f, 0.20f, 0.88f),
                    new Color(0.01f, 0.07f, 0.08f, 0.52f)
                );
            case MiniGameThemeId.Gene:
                return new MiniGameTheme(
                    "MinigameArt/GeneBackdrop",
                    Hex("#9E8BFF"),
                    Hex("#FFD66B"),
                    Hex("#251C45"),
                    new Color(0.075f, 0.05f, 0.18f, 0.93f),
                    new Color(0.14f, 0.10f, 0.29f, 0.89f),
                    new Color(0.015f, 0.01f, 0.06f, 0.56f)
                );
            default:
                return new MiniGameTheme(
                    "MinigameArt/OrbitBackdrop",
                    Hex("#61D9FF"),
                    Hex("#FFBC66"),
                    Hex("#09253B"),
                    new Color(0.025f, 0.10f, 0.18f, 0.93f),
                    new Color(0.06f, 0.18f, 0.29f, 0.89f),
                    new Color(0.005f, 0.025f, 0.07f, 0.58f)
                );
        }
    }

    public static void PrepareScreen(GameObject root, MiniGameThemeId id)
    {
        MiniGameTheme theme = Theme(id);
        Image rootImage = root.GetComponent<Image>();
        if (rootImage != null)
        {
            rootImage.color = theme.ink;
        }

        GameObject backdrop = new GameObject(
            "CinematicBackdrop",
            typeof(RectTransform),
            typeof(RawImage)
        );
        backdrop.transform.SetParent(root.transform, false);
        UIFactory.Stretch(backdrop.GetComponent<RectTransform>());
        RawImage raw = backdrop.GetComponent<RawImage>();
        raw.texture = Resources.Load<Texture2D>(theme.resourcePath);
        raw.color = raw.texture != null ? Color.white : theme.ink;
        raw.raycastTarget = false;
        backdrop.transform.SetAsFirstSibling();

        Image veil = UIFactory.CreatePanel("CinematicVeil", root.transform, theme.veil);
        UIFactory.Stretch(veil.rectTransform);
        veil.raycastTarget = false;
        veil.transform.SetSiblingIndex(1);

        Image topWash = UIFactory.CreatePanel(
            "TopReadabilityWash",
            root.transform,
            new Color(theme.ink.r, theme.ink.g, theme.ink.b, 0.55f)
        );
        topWash.rectTransform.anchorMin = new Vector2(0f, 0.77f);
        topWash.rectTransform.anchorMax = Vector2.one;
        topWash.rectTransform.offsetMin = Vector2.zero;
        topWash.rectTransform.offsetMax = Vector2.zero;
        topWash.raycastTarget = false;
        topWash.transform.SetSiblingIndex(2);
    }

    public static Image CreateCard(
        string name,
        Transform parent,
        Vector2 size,
        MiniGameThemeId id,
        bool soft = false
    )
    {
        MiniGameTheme theme = Theme(id);
        Image card = UIFactory.CreatePanel(name, parent, soft ? theme.cardSoft : theme.card);
        card.rectTransform.sizeDelta = size;
        Round(card);
        AddShadow(card.gameObject, new Vector2(0f, -10f), 0.32f);
        return card;
    }

    public static GameObject CreateArtSlot(
        string name,
        Transform parent,
        Vector2 size,
        MiniGameThemeId id,
        string caption
    )
    {
        MiniGameTheme theme = Theme(id);
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Mask));
        root.transform.SetParent(parent, false);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = size;

        Image frame = root.GetComponent<Image>();
        frame.color = Color.white;
        Round(frame);
        Mask mask = root.GetComponent<Mask>();
        mask.showMaskGraphic = false;
        AddShadow(root, new Vector2(0f, -9f), 0.30f);

        GameObject art = new GameObject("Art", typeof(RectTransform), typeof(RawImage));
        art.transform.SetParent(root.transform, false);
        UIFactory.Stretch(art.GetComponent<RectTransform>());
        RawImage raw = art.GetComponent<RawImage>();
        raw.texture = Resources.Load<Texture2D>(theme.resourcePath);
        raw.color = raw.texture != null ? Color.white : theme.cardSoft;
        raw.raycastTarget = false;

        Image glaze = UIFactory.CreatePanel("ArtGlaze", root.transform, new Color(theme.ink.r, theme.ink.g, theme.ink.b, 0.16f));
        UIFactory.Stretch(glaze.rectTransform);
        glaze.raycastTarget = false;

        if (!string.IsNullOrWhiteSpace(caption))
        {
            Image captionPill = UIFactory.CreatePanel("CaptionPill", root.transform, new Color(theme.ink.r, theme.ink.g, theme.ink.b, 0.84f));
            Round(captionPill);
            captionPill.raycastTarget = false;
            captionPill.rectTransform.anchorMin = new Vector2(0f, 1f);
            captionPill.rectTransform.anchorMax = new Vector2(0f, 1f);
            captionPill.rectTransform.pivot = new Vector2(0f, 1f);
            captionPill.rectTransform.anchoredPosition = new Vector2(24f, -22f);
            captionPill.rectTransform.sizeDelta = new Vector2(Mathf.Max(240f, caption.Length * 30f + 60f), 48f);

            Text label = UIFactory.CreateText("Caption", captionPill.transform, caption, 22, Color.white, TextAnchor.MiddleLeft);
            UIFactory.Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(20f, 0f);
            label.rectTransform.offsetMax = new Vector2(-20f, 0f);
            label.fontStyle = FontStyle.Bold;
        }

        return root;
    }

    public static void AddStepRail(
        Transform parent,
        MiniGameThemeId id,
        string[] labels,
        int activeIndex
    )
    {
        MiniGameTheme theme = Theme(id);
        RectTransform rail = UIFactory.CreateRect("StepRail", parent);
        rail.anchorMin = new Vector2(0.5f, 1f);
        rail.anchorMax = new Vector2(0.5f, 1f);
        rail.pivot = new Vector2(0.5f, 1f);
        rail.anchoredPosition = new Vector2(0f, -128f);
        rail.sizeDelta = new Vector2(920f, 44f);

        float width = 190f;
        float gap = 20f;
        float total = labels.Length * width + (labels.Length - 1) * gap;
        for (int i = 0; i < labels.Length; i++)
        {
            bool active = i == activeIndex;
            bool done = i < activeIndex;
            Color bg = active
                ? theme.accent
                : done
                    ? new Color(theme.accent.r, theme.accent.g, theme.accent.b, 0.32f)
                    : new Color(theme.cardSoft.r, theme.cardSoft.g, theme.cardSoft.b, 0.78f);

            Image pill = UIFactory.CreatePanel("Step_" + i, rail, bg);
            Round(pill);
            pill.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            pill.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            pill.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            pill.rectTransform.sizeDelta = new Vector2(width, 38f);
            pill.rectTransform.anchoredPosition = new Vector2(-total * 0.5f + width * 0.5f + i * (width + gap), 0f);

            Text text = UIFactory.CreateText(
                "Label",
                pill.transform,
                (done ? "✓  " : (i + 1) + "  ") + labels[i],
                19,
                active ? theme.ink : Color.white
            );
            UIFactory.Stretch(text.rectTransform);
            text.fontStyle = FontStyle.Bold;
        }
    }

    public static void AddEntrance(GameObject target)
    {
        if (target.GetComponent<MiniGamePanelMotion>() == null)
        {
            target.AddComponent<MiniGamePanelMotion>();
        }
    }

    public static void PolishHierarchy(Transform root, MiniGameThemeId id)
    {
        MiniGameTheme theme = Theme(id);
        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            DecorateButton(buttons[i], theme);
        }

        Image[] images = root.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            string n = image.gameObject.name;
            if (image.type != Image.Type.Simple)
            {
                continue;
            }

            if (
                n.Contains("Card") ||
                n.Contains("Panel") ||
                n.Contains("Track_") ||
                n.Contains("Zone_") ||
                n.Contains("Progress") ||
                n.Contains("BoardZone") ||
                n.Contains("Selection")
            )
            {
                Round(image);
            }
        }

        Text[] texts = root.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            Text text = texts[i];
            if (text.fontSize >= 42 || text.gameObject.name == "Text")
            {
                text.fontStyle = FontStyle.Bold;
            }
        }
    }

    public static void Round(Image image)
    {
        image.sprite = RoundedSprite;
        image.type = Image.Type.Sliced;
    }

    public static void MakeCircle(Image image)
    {
        image.sprite = CircleSprite;
        image.type = Image.Type.Simple;
    }

    private static void DecorateButton(Button button, MiniGameTheme theme)
    {
        Image image = button.GetComponent<Image>();
        if (image == null)
        {
            return;
        }

        Round(image);
        AddShadow(button.gameObject, new Vector2(0f, -5f), 0.28f);
        if (button.GetComponent<MiniGameButtonMotion>() == null)
        {
            button.gameObject.AddComponent<MiniGameButtonMotion>();
        }

        Text label = button.GetComponentInChildren<Text>(true);
        if (label != null)
        {
            label.fontStyle = FontStyle.Bold;
            label.color = Color.white;
            Shadow labelShadow = label.GetComponent<Shadow>();
            if (labelShadow == null)
            {
                labelShadow = label.gameObject.AddComponent<Shadow>();
                labelShadow.effectColor = new Color(0f, 0f, 0f, 0.30f);
                labelShadow.effectDistance = new Vector2(0f, -2f);
            }
        }

        ColorBlock colors = button.colors;
        Color baseColor = image.color;
        colors.normalColor = baseColor;
        colors.highlightedColor = Color.Lerp(baseColor, theme.accentWarm, 0.16f);
        colors.pressedColor = Color.Lerp(baseColor, Color.black, 0.16f);
        colors.selectedColor = Color.Lerp(baseColor, theme.accent, 0.12f);
        colors.disabledColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.42f);
        colors.fadeDuration = 0.10f;
        button.colors = colors;
    }

    private static void AddShadow(GameObject target, Vector2 distance, float alpha)
    {
        Shadow shadow = target.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = target.AddComponent<Shadow>();
        }

        shadow.effectColor = new Color(0f, 0f, 0f, alpha);
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }

    private static Sprite RoundedSprite
    {
        get
        {
            if (roundedSprite == null)
            {
                roundedSprite = CreateProceduralSprite(64, 15f, false);
            }

            return roundedSprite;
        }
    }

    private static Sprite CircleSprite
    {
        get
        {
            if (circleSprite == null)
            {
                circleSprite = CreateProceduralSprite(64, 32f, true);
            }

            return circleSprite;
        }
    }

    private static Sprite CreateProceduralSprite(int size, float radius, bool circle)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = circle ? "UI_Circle_Runtime" : "UI_Rounded_Runtime";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        Color32[] pixels = new Color32[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float alpha;
                if (circle)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    alpha = Mathf.Clamp01(radius + 0.5f - d);
                }
                else
                {
                    float qx = Mathf.Abs(x - center.x) - (center.x - radius);
                    float qy = Mathf.Abs(y - center.y) - (center.y - radius);
                    float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
                    float inside = Mathf.Min(Mathf.Max(qx, qy), 0f);
                    float distance = outside + inside - radius;
                    alpha = Mathf.Clamp01(0.75f - distance);
                }

                pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        Vector4 border = circle ? Vector4.zero : new Vector4(16f, 16f, 16f, 16f);
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0u,
            SpriteMeshType.FullRect,
            border
        );
        sprite.name = texture.name;
        return sprite;
    }

    private static Color Hex(string value)
    {
        ColorUtility.TryParseHtmlString(value, out Color color);
        return color;
    }
}

/// <summary>按钮的轻微悬停与按压反馈，不改 Button 的业务回调。</summary>
public sealed class MiniGameButtonMotion : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    private Vector3 targetScale = Vector3.one;

    private void OnEnable()
    {
        transform.localScale = Vector3.one;
        targetScale = Vector3.one;
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            1f - Mathf.Exp(-18f * Time.unscaledDeltaTime)
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = Vector3.one * 1.025f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = Vector3.one;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = Vector3.one * 0.975f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        targetScale = Vector3.one * 1.025f;
    }
}

/// <summary>阶段页面启用时的短促淡入，使用 unscaled time，暂停状态下也能播放。</summary>
public sealed class MiniGamePanelMotion : MonoBehaviour
{
    private CanvasGroup group;
    private RectTransform rect;
    private float time;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = gameObject.AddComponent<CanvasGroup>();
        }

        rect = transform as RectTransform;
    }

    private void OnEnable()
    {
        time = 0f;
        if (group != null)
        {
            group.alpha = 0f;
        }

        if (rect != null)
        {
            rect.localScale = Vector3.one * 0.985f;
        }
    }

    private void Update()
    {
        time += Time.unscaledDeltaTime;
        float t = 1f - Mathf.Exp(-9f * time);
        if (group != null)
        {
            group.alpha = t;
        }

        if (rect != null)
        {
            rect.localScale = Vector3.Lerp(Vector3.one * 0.985f, Vector3.one, t);
        }

        if (t > 0.998f)
        {
            group.alpha = 1f;
            rect.localScale = Vector3.one;
        }
    }
}
