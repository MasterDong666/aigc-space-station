using UnityEngine;
using UnityEngine.UI;

/// <summary>科幻风占位配色（Stage 1 色块风，非最终美术）。</summary>
public static class UIPalette
{
    public static readonly Color Background = Hex("#0A1420");
    public static readonly Color Panel = Hex("#12233C");
    public static readonly Color PanelLight = Hex("#1B3354");
    public static readonly Color Accent = Hex("#00E5FF");
    public static readonly Color AccentDim = Hex("#0A6E7E");
    public static readonly Color TextMain = Hex("#E8F4FF");
    public static readonly Color TextDim = Hex("#7FA3C7");
    public static readonly Color Ok = Hex("#3DDC97");
    public static readonly Color Warn = Hex("#FF6B6B");
    public static readonly Color Locked = Hex("#46586C");
    public static readonly Color Suitable = Hex("#1E4D3C");
    public static readonly Color Unsuitable = Hex("#4A3B44");

    private static Color Hex(string hex)
    {
        Color color;
        ColorUtility.TryParseHtmlString(hex, out color);
        return color;
    }
}

/// <summary>
/// 纯代码搭建 uGUI 的工厂方法（Stage 1 统一入口）。
/// 目标分辨率 1920x1080（CanvasScaler ScaleWithScreenSize）。
/// </summary>
public static class UIFactory
{
    private static Font uiFont;

    /// <summary>支持中文的系统动态字体（Windows：微软雅黑/黑体）。</summary>
    public static Font Font
    {
        get
        {
            if (uiFont == null)
            {
                uiFont = Font.CreateDynamicFontFromOSFont(
                    new[]
                    {
                        "PingFang SC",
                        "Hiragino Sans GB",
                        "Microsoft YaHei",
                        "SimHei",
                        "DengXian",
                        "Arial"
                    },
                    32
                );
            }

            return uiFont;
        }
    }

    public static Canvas CreateCanvas(string name)
    {
        GameObject go = new GameObject(
            name,
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    public static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    /// <summary>把 RectTransform 拉伸铺满父节点。</summary>
    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public static Image CreatePanel(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    public static Text CreateText(
        string name,
        Transform parent,
        string content,
        int fontSize,
        Color color,
        TextAnchor alignment = TextAnchor.MiddleCenter
    )
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        Text text = go.GetComponent<Text>();
        text.font = Font;
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }

    public static Button CreateButton(
        string name,
        Transform parent,
        string label,
        Vector2 size,
        Color bg,
        int fontSize = 28
    )
    {
        GameObject go = new GameObject(
            name,
            typeof(RectTransform),
            typeof(Image),
            typeof(Button)
        );
        go.transform.SetParent(parent, false);

        Image image = go.GetComponent<Image>();
        image.color = bg;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = bg;
        colors.highlightedColor = Color.Lerp(bg, Color.white, 0.15f);
        colors.pressedColor = Color.Lerp(bg, Color.black, 0.2f);
        colors.selectedColor = bg;
        colors.disabledColor = new Color(bg.r, bg.g, bg.b, 0.35f);
        button.colors = colors;

        RectTransform rect = button.GetComponent<RectTransform>();
        rect.sizeDelta = size;

        Text labelText = CreateText(
            "Text",
            go.transform,
            label,
            fontSize,
            UIPalette.TextMain
        );
        Stretch(labelText.rectTransform);

        return button;
    }

    /// <summary>
    /// 创建标准 uGUI Slider（0~100，默认值 50）。
    /// 结构与官方默认模板一致：Background / Fill Area / Handle Slide Area。
    /// </summary>
    public static Slider CreateSlider(string name, Transform parent, Vector2 size)
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(Slider));
        root.transform.SetParent(parent, false);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = size;

        Image bg = CreatePanel("Background", root.transform, UIPalette.PanelLight);
        bg.rectTransform.anchorMin = new Vector2(0f, 0.25f);
        bg.rectTransform.anchorMax = new Vector2(1f, 0.75f);
        bg.rectTransform.offsetMin = new Vector2(-8f, 0f);
        bg.rectTransform.offsetMax = new Vector2(8f, 0f);

        RectTransform fillArea = CreateRect("Fill Area", root.transform);
        fillArea.anchorMin = new Vector2(0f, 0.25f);
        fillArea.anchorMax = new Vector2(1f, 0.75f);
        fillArea.offsetMin = new Vector2(8f, 0f);
        fillArea.offsetMax = new Vector2(-8f, 0f);

        Image fill = CreatePanel("Fill", fillArea, UIPalette.Accent);
        fill.rectTransform.anchorMin = new Vector2(0f, 0f);
        fill.rectTransform.anchorMax = new Vector2(0f, 1f);
        fill.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        fill.rectTransform.sizeDelta = new Vector2(10f, 0f);

        RectTransform handleArea = CreateRect("Handle Slide Area", root.transform);
        handleArea.anchorMin = Vector2.zero;
        handleArea.anchorMax = Vector2.one;
        handleArea.offsetMin = new Vector2(10f, 0f);
        handleArea.offsetMax = new Vector2(-10f, 0f);

        Image handle = CreatePanel("Handle", handleArea, UIPalette.TextMain);
        handle.rectTransform.anchorMin = Vector2.zero;
        handle.rectTransform.anchorMax = Vector2.zero;
        handle.rectTransform.sizeDelta = new Vector2(20f, 20f);

        Slider slider = root.GetComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.value = 50f;

        return slider;
    }

    /// <summary>创建水平进度条（背景 + 填充），填充进度用 fillImage.fillAmount（0~1）。</summary>
    public static Image CreateProgressBar(
        string name,
        Transform parent,
        Vector2 size,
        out Image fillImage
    )
    {
        Image bg = CreatePanel(name, parent, UIPalette.PanelLight);
        bg.rectTransform.sizeDelta = size;

        Image fill = CreatePanel("Fill", bg.transform, UIPalette.Accent);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillAmount = 0f;
        Stretch(fill.rectTransform);

        fillImage = fill;
        return bg;
    }

    /// <summary>
    /// 创建媒体占位槽（正式 AI 图片/视频的接入点）。
    /// 团队美术完成后：替换本节点下的 Placeholder Image 为 RawImage/VideoPlayer，
    /// 或直接替换整个节点的子内容即可，外层布局不受影响。
    /// </summary>
    public static GameObject CreateMediaSlot(
        string nodeName,
        string label,
        Transform parent,
        Vector2 size
    )
    {
        GameObject root = new GameObject(nodeName, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        root.GetComponent<RectTransform>().sizeDelta = size;

        Image bg = CreatePanel("Placeholder", root.transform, UIPalette.Panel);
        Stretch(bg.rectTransform);

        Text hint = CreateText("Label", root.transform, label, 24, UIPalette.TextDim);
        Stretch(hint.rectTransform);

        Image line = CreatePanel("AccentLine", root.transform, UIPalette.AccentDim);
        line.rectTransform.anchorMin = new Vector2(0f, 0f);
        line.rectTransform.anchorMax = new Vector2(1f, 0f);
        line.rectTransform.pivot = new Vector2(0.5f, 0f);
        line.rectTransform.sizeDelta = new Vector2(0f, 3f);

        return root;
    }
}
