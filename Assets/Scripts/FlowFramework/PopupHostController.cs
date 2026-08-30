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
            new Color(0f, 0f, 0f, 0.72f)
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
            CinematicUIVisuals.Midnight,
            new Vector2(900f, 540f)
        );
        SetAnchored(
            card.rectTransform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(900f, 540f)
        );

        Image accentBar = UIFactory.CreatePanel("PopupAccentBar", card.transform, accent);
        accentBar.rectTransform.anchorMin = new Vector2(0f, 1f);
        accentBar.rectTransform.anchorMax = new Vector2(1f, 1f);
        accentBar.rectTransform.pivot = new Vector2(0.5f, 1f);
        accentBar.rectTransform.sizeDelta = new Vector2(0f, 6f);

        Text eyebrow = UIFactory.CreateText(
            "PopupEyebrow",
            card.transform,
            string.IsNullOrEmpty(request.eyebrow) ? "NOTICE" : request.eyebrow,
            20,
            accent,
            TextAnchor.MiddleCenter
        );
        SetAnchored(
            eyebrow.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -46f),
            new Vector2(700f, 32f)
        );

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
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0f, -190f),
                    new Vector2(220f, 220f)
                );
            }
        }

        titleText = UIFactory.CreateText(
            "PopupTitle",
            card.transform,
            request.title,
            40,
            UIPalette.TextMain
        );
        SetAnchored(
            titleText.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -150f),
            new Vector2(780f, 70f)
        );

        bodyText = UIFactory.CreateText(
            "PopupBody",
            card.transform,
            request.body,
            25,
            UIPalette.TextDim
        );
        SetAnchored(
            bodyText.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -260f),
            new Vector2(780f, 200f)
        );

        Button confirm = UIFactory.CreateButton(
            "Confirm",
            card.transform,
            string.IsNullOrEmpty(request.confirmLabel)
                ? "确认  ›"
                : request.confirmLabel,
            new Vector2(320f, 72f),
            accent,
            28
        );
        SetAnchored(
            confirm.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 48f),
            new Vector2(320f, 72f)
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
