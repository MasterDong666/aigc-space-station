using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 结尾桥接面板（非最终结局）。
/// 用于证明：任务3 → 收获 → 后续剧情/Ending 的技术链路已打通。
/// 正式结局文本与视频由其他成员提供，可直接替换本面板内容。
/// </summary>
public class EndingBridgeUI : MonoBehaviour
{
    /// <summary>点击【返回空间站】时触发。</summary>
    public event Action ReturnRequested;

    public void BuildUI()
    {
        Image bg = gameObject.AddComponent<Image>();
        bg.color = CinematicUIVisuals.DeepInk;

        CinematicUIVisuals.AddBackdrop(
            transform,
            "FrontendArt/OpeningJourney",
            new Color(0.015f, 0.035f, 0.075f, 0.48f),
            "EndingJourneyBackdrop"
        );

        Image storyCard = CinematicUIVisuals.CreateCard(
            "EndingBridgeCard",
            transform,
            new Color(0.04f, 0.10f, 0.15f, 0.94f),
            new Vector2(940f, 520f)
        );
        SetAnchored(
            storyCard.rectTransform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(940f, 520f)
        );

        Text title = UIFactory.CreateText(
            "TxtEndingTitle",
            storyCard.transform,
            "新的行动阶段即将开启",
            50,
            CinematicUIVisuals.Cream
        );
        SetAnchored(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(820f, 72f));

        Text text = UIFactory.CreateText(
            "TxtEndingText",
            storyCard.transform,
            "三项修复数据已经汇总。归墟正在回应我们的努力。\n晨曦已为你准备好返航航线与下一段旅程。",
            29,
            new Color(0.82f, 0.92f, 0.94f, 1f)
        );
        SetAnchored(text.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(800f, 130f));

        Button returnButton = UIFactory.CreateButton(
            "BtnReturnStation",
            storyCard.transform,
            "返回空间站  ›",
            new Vector2(360f, 78f),
            new Color(0.94f, 0.46f, 0.24f, 1f),
            30
        );
        SetAnchored(returnButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 62f), new Vector2(360f, 78f));
        returnButton.onClick.AddListener(() => ReturnRequested?.Invoke());

        CinematicUIVisuals.PolishHierarchy(
            transform,
            CinematicUIVisuals.Sky,
            CinematicUIVisuals.Sun
        );
        CinematicUIVisuals.AddEntrance(storyCard.gameObject);

        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
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
