using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 主流程与叙事界面的统一视觉层。
/// 只负责背景、美术卡片、圆角、阴影和轻量动效，不承载任何游戏状态。
/// </summary>
public static class CinematicUIVisuals
{
    public static readonly Color Midnight = Hex("#10243A");
    public static readonly Color DeepInk = Hex("#0A1728");
    public static readonly Color Sky = Hex("#55C8E8");
    public static readonly Color Mint = Hex("#73D7B3");
    public static readonly Color Peach = Hex("#FF9E78");
    public static readonly Color Sun = Hex("#FFC85A");
    public static readonly Color Cream = Hex("#FFF4DF");
    public static readonly Color Lavender = Hex("#A99AF8");

    public static RawImage AddBackdrop(
        Transform parent,
        string resourcePath,
        Color veil,
        string name = "AnimatedFeatureBackdrop"
    )
    {
        GameObject art = new(name, typeof(RectTransform), typeof(RawImage));
        art.transform.SetParent(parent, false);
        UIFactory.Stretch(art.GetComponent<RectTransform>());

        RawImage raw = art.GetComponent<RawImage>();
        raw.texture = Resources.Load<Texture2D>(resourcePath);
        raw.color = raw.texture != null ? Color.white : Midnight;
        raw.raycastTarget = false;
        art.transform.SetAsFirstSibling();

        Image wash = UIFactory.CreatePanel(name + "Veil", parent, veil);
        UIFactory.Stretch(wash.rectTransform);
        wash.raycastTarget = false;
        wash.transform.SetSiblingIndex(1);
        return raw;
    }

    public static Image CreateCard(
        string name,
        Transform parent,
        Color color,
        Vector2 size
    )
    {
        Image card = UIFactory.CreatePanel(name, parent, color);
        card.rectTransform.sizeDelta = size;
        MiniGameVisuals.Round(card);
        AddShadow(card.gameObject, new Vector2(0f, -12f), 0.34f);
        return card;
    }

    public static RawImage AddFramedArt(
        string name,
        Transform parent,
        Texture texture,
        Vector2 size,
        Color frameColor
    )
    {
        GameObject frameObject = new(
            name,
            typeof(RectTransform),
            typeof(Image),
            typeof(Mask)
        );
        frameObject.transform.SetParent(parent, false);
        RectTransform frameRect = frameObject.GetComponent<RectTransform>();
        frameRect.sizeDelta = size;

        Image frame = frameObject.GetComponent<Image>();
        frame.color = frameColor;
        MiniGameVisuals.Round(frame);
        frameObject.GetComponent<Mask>().showMaskGraphic = true;
        AddShadow(frameObject, new Vector2(0f, -8f), 0.28f);

        GameObject artObject = new("Art", typeof(RectTransform), typeof(RawImage));
        artObject.transform.SetParent(frameObject.transform, false);
        RectTransform artRect = artObject.GetComponent<RectTransform>();
        UIFactory.Stretch(artRect);
        artRect.offsetMin = new Vector2(6f, 6f);
        artRect.offsetMax = new Vector2(-6f, -6f);
        RawImage art = artObject.GetComponent<RawImage>();
        art.texture = texture;
        art.color = texture != null ? Color.white : frameColor;
        art.raycastTarget = false;
        return art;
    }

    public static void PolishHierarchy(
        Transform root,
        Color accent,
        Color warmAccent
    )
    {
        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            Image image = button.GetComponent<Image>();
            if (image == null)
            {
                continue;
            }

            MiniGameVisuals.Round(image);
            AddShadow(button.gameObject, new Vector2(0f, -6f), 0.27f);
            if (button.GetComponent<MiniGameButtonMotion>() == null)
            {
                button.gameObject.AddComponent<MiniGameButtonMotion>();
            }

            ColorBlock colors = button.colors;
            Color baseColor = image.color;
            colors.normalColor = baseColor;
            colors.highlightedColor = Color.Lerp(baseColor, warmAccent, 0.18f);
            colors.pressedColor = Color.Lerp(baseColor, Color.black, 0.18f);
            colors.selectedColor = Color.Lerp(baseColor, accent, 0.16f);
            colors.disabledColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.40f);
            colors.fadeDuration = 0.10f;
            button.colors = colors;

            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.fontStyle = FontStyle.Bold;
                AddTextShadow(label, 0.28f);
            }
        }

        foreach (Image image in root.GetComponentsInChildren<Image>(true))
        {
            string objectName = image.gameObject.name;
            if (
                objectName.Contains("Card") ||
                objectName.Contains("Panel") ||
                objectName.Contains("Badge") ||
                objectName.Contains("Status") ||
                objectName.Contains("Field") ||
                objectName.Contains("Tray") ||
                objectName.Contains("Choice") ||
                objectName.Contains("Popup")
            )
            {
                MiniGameVisuals.Round(image);
            }
        }

        foreach (Text text in root.GetComponentsInChildren<Text>(true))
        {
            if (text.fontSize >= 38 || text.gameObject.name == "Text")
            {
                text.fontStyle = FontStyle.Bold;
            }

            if (text.fontSize >= 28)
            {
                AddTextShadow(text, 0.18f);
            }
        }
    }

    public static void AddEntrance(GameObject target)
    {
        if (target != null && target.GetComponent<MiniGamePanelMotion>() == null)
        {
            target.AddComponent<MiniGamePanelMotion>();
        }
    }

    public static void AddShadow(
        GameObject target,
        Vector2 distance,
        float alpha
    )
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

    private static void AddTextShadow(Text text, float alpha)
    {
        Shadow shadow = text.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = text.gameObject.AddComponent<Shadow>();
        }

        shadow.effectColor = new Color(0.02f, 0.04f, 0.08f, alpha);
        shadow.effectDistance = new Vector2(0f, -2f);
        shadow.useGraphicAlpha = true;
    }

    private static Color Hex(string value)
    {
        ColorUtility.TryParseHtmlString(value, out Color color);
        return color;
    }
}
