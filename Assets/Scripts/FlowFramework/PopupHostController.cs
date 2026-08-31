using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 统一弹窗宿主（由 PopupManager 创建与销毁）。
/// 结构：独立 Canvas（sortingOrder 300）→ 遮罩 → 剧情卡 → eyebrow/标题/正文/可选立绘 → 确认按钮。
/// 复用项目既有 UIFactory / UIPalette / CinematicUIVisuals 视觉语言。
/// </summary>
public class PopupHostController : MonoBehaviour
{
    private System.Action onClosed;
    private Text titleText;
    private Text bodyText;

    public void Show(PopupRequest request, System.Action closed)
    {
        onClosed = closed;
        BuildUI(request);
    }

    /// <summary>等效点击确认按钮（供验证脚本使用）。</summary>
    public void Close()
    {
        Confirm();
    }

    private void BuildUI(PopupRequest request)
    {
        Canvas canvas = UIFactory.CreateCanvas("FlowPopupCanvas");
        canvas.sortingOrder = 300;
        canvas.transform.SetParent(transform, false);

        Image veil = UIFactory.CreatePanel(
            "PopupVeil",
            canvas.transform,
            new Color(0.01f, 0.025f, 0.06f, 0.66f)
        );
        UIFactory.Stretch(veil.rectTransform);

        Color accent = UIPalette.Accent;
        if (!string.IsNullOrEmpty(request.accentHex))
        {
            ColorUtility.TryParseHtmlString(request.accentHex, out accent);
        }

        Image card = CinematicUIVisuals.CreateCard(
            "FlowPopupCard",
            canvas.transform,
            new Color(0.055f, 0.14f, 0.22f, 0.985f),
            new Vector2(1040f, 640f)
        );
        SetAnchored(
            card.rectTransform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(1040f, 640f)
        );
        CinematicUIVisuals.AddEntrance(card.gameObject);

        Image accentBar = UIFactory.CreatePanel("PopupAccentBar", card.transform, accent);
        accentBar.rectTransform.anchorMin = new Vector2(0f, 1f);
        accentBar.rectTransform.anchorMax = new Vector2(1f, 1f);
        accentBar.rectTransform.pivot = new Vector2(0.5f, 1f);
        accentBar.rectTransform.sizeDelta = new Vector2(0f, 12f);

        Image mascot = UIFactory.CreatePanel(
            "ChenxiMascot",
            card.transform,
            new Color(accent.r, accent.g, accent.b, 0.22f)
        );
        SetAnchored(
            mascot.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(-420f, -42f),
            new Vector2(78f, 78f)
        );
        MiniGameVisuals.MakeCircle(mascot);
        Image mascotCore = UIFactory.CreatePanel("ChenxiMascotCore", mascot.transform, accent);
        mascotCore.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        mascotCore.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        mascotCore.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        mascotCore.rectTransform.sizeDelta = new Vector2(28f, 28f);
        mascotCore.rectTransform.anchoredPosition = Vector2.zero;
        MiniGameVisuals.MakeCircle(mascotCore);

        Text eyebrow = UIFactory.CreateText(
            "PopupEyebrow",
            card.transform,
            string.IsNullOrEmpty(request.eyebrow) ? "NOTICE" : request.eyebrow,
            20,
            accent,
            TextAnchor.MiddleLeft
        );
        SetAnchored(
            eyebrow.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(140f, -48f),
            new Vector2(560f, 34f)
        );

        bool hasPortrait = false;
        if (!string.IsNullOrEmpty(request.textureKey))
        {
            Texture2D portrait = Resources.Load<Texture2D>(request.textureKey);
            if (portrait != null)
            {
                RawImage art = CinematicUIVisuals.AddFramedArt(
                    "PopupPortrait",
                    card.transform,
                    portrait,
                    new Vector2(220f, 220f),
                    CinematicUIVisuals.DeepInk
                );
                SetAnchored(
                    art.transform.parent.GetComponent<RectTransform>(),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(70f, -170f),
                    new Vector2(220f, 220f)
                );
                mascot.gameObject.SetActive(false);
                hasPortrait = true;
            }
        }

        titleText = UIFactory.CreateText(
            "PopupTitle",
            card.transform,
            request.title,
            42,
            CinematicUIVisuals.Cream,
            TextAnchor.MiddleLeft
        );
        SetAnchored(
            titleText.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            hasPortrait ? new Vector2(330f, -126f) : new Vector2(70f, -126f),
            hasPortrait ? new Vector2(640f, 70f) : new Vector2(900f, 70f)
        );

        Image dialogueBubble = UIFactory.CreatePanel(
            "PopupDialogueBubble",
            card.transform,
            new Color(0.08f, 0.23f, 0.30f, 0.96f)
        );
        SetAnchored(
            dialogueBubble.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            hasPortrait ? new Vector2(145f, -230f) : new Vector2(0f, -230f),
            hasPortrait ? new Vector2(610f, 220f) : new Vector2(900f, 220f)
        );
        MiniGameVisuals.Round(dialogueBubble);
        CinematicUIVisuals.AddShadow(dialogueBubble.gameObject, new Vector2(0f, -7f), 0.22f);

        Text sparkle = UIFactory.CreateText(
            "PopupSparkle",
            dialogueBubble.transform,
            "✦",
            34,
            CinematicUIVisuals.Sun
        );
        SetAnchored(
            sparkle.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(28f, -24f),
            new Vector2(50f, 50f)
        );

        bodyText = UIFactory.CreateText(
            "PopupBody",
            dialogueBubble.transform,
            request.body,
            24,
            new Color(0.88f, 0.95f, 0.96f, 1f),
            TextAnchor.MiddleLeft
        );
        UIFactory.Stretch(bodyText.rectTransform);
        bodyText.rectTransform.offsetMin = new Vector2(86f, 24f);
        bodyText.rectTransform.offsetMax = new Vector2(-34f, -22f);
        bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bodyText.verticalOverflow = VerticalWrapMode.Truncate;
        bodyText.lineSpacing = 1.14f;

        Text assistantTag = UIFactory.CreateText(
            "PopupAssistantTag",
            card.transform,
            "晨曦 AI  ·  陪伴模式已开启",
            19,
            new Color(accent.r, accent.g, accent.b, 0.92f),
            TextAnchor.MiddleLeft
        );
        SetAnchored(
            assistantTag.rectTransform,
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(70f, 62f),
            new Vector2(430f, 40f)
        );

        Button confirm = UIFactory.CreateButton(
            "Confirm",
            card.transform,
            string.IsNullOrEmpty(request.confirmLabel)
                ? "确认  ›"
                : request.confirmLabel,
            new Vector2(330f, 78f),
            accent,
            28
        );
        SetAnchored(
            confirm.GetComponent<RectTransform>(),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(-70f, 44f),
            new Vector2(330f, 78f)
        );
        confirm.onClick.AddListener(Confirm);
    }

    private void Confirm()
    {
        Destroy(gameObject);

        System.Action callback = onClosed;
        onClosed = null;
        callback?.Invoke();
    }

    private static void SetAnchored(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 sizeDelta
    )
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = anchorMin;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }
}
