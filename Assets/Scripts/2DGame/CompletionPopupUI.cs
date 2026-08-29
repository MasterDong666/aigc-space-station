using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>任务完成弹窗：显示奖励信息，【返回空间站】关闭并回到主界面。</summary>
public class CompletionPopupUI : MonoBehaviour
{
    private Text messageText;
    private Button returnButton;
    private Action onReturn;

    public void BuildUI()
    {
        // 半透明遮罩（挡住下层界面的点击）
        Image overlay = gameObject.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.75f);

        // 弹窗卡片
        Image card = UIFactory.CreatePanel("PopupCard", transform, UIPalette.Panel);
        SetAnchored(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(780f, 470f));
        MiniGameVisuals.Round(card);

        Image badge = UIFactory.CreatePanel("SuccessBadge", card.transform, UIPalette.Ok);
        MiniGameVisuals.MakeCircle(badge);
        SetAnchored(badge.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -52f), new Vector2(104f, 104f));

        Text check = UIFactory.CreateText("SuccessCheck", badge.transform, "✓", 58, Color.white);
        UIFactory.Stretch(check.rectTransform);
        check.fontStyle = FontStyle.Bold;

        Text eyebrow = UIFactory.CreateText("SuccessEyebrow", card.transform, "RESTORATION PROGRESS UPDATED", 18, UIPalette.Ok);
        SetAnchored(eyebrow.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -166f), new Vector2(620f, 30f));

        messageText = UIFactory.CreateText("TxtPopup", card.transform, "", 38, UIPalette.TextMain);
        SetAnchored(messageText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -202f), new Vector2(680f, 120f));

        returnButton = UIFactory.CreateButton(
            "BtnReturn",
            card.transform,
            "返回空间站",
            new Vector2(300f, 76f),
            UIPalette.AccentDim,
            30
        );
        SetAnchored(returnButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(300f, 76f));
        returnButton.onClick.AddListener(OnReturnClicked);

        MiniGameVisuals.PolishHierarchy(transform, MiniGameThemeId.Orbit);
        MiniGameVisuals.AddEntrance(card.gameObject);

        gameObject.SetActive(false);
    }

    public void Show(string message, Action onReturn)
    {
        messageText.text = message;
        this.onReturn = onReturn;
        gameObject.SetActive(true);
    }

    private void OnReturnClicked()
    {
        gameObject.SetActive(false);
        if (onReturn != null)
        {
            onReturn();
        }
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
