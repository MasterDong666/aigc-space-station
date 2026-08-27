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
        bg.color = UIPalette.Background;

        // 媒体占位：结尾桥接剧情画面（正式美术/视频在此节点内替换）
        GameObject slot = UIFactory.CreateMediaSlot(
            "MediaSlot_EndingBridge",
            "【占位】结尾桥接剧情画面（待正式美术/视频）",
            transform,
            new Vector2(900f, 300f)
        );
        SetAnchored(slot.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -180f), new Vector2(900f, 300f));

        Text title = UIFactory.CreateText(
            "TxtEndingTitle",
            transform,
            "新的行动阶段即将开启",
            44,
            UIPalette.TextMain
        );
        SetAnchored(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -520f), new Vector2(1000f, 60f));

        Text text = UIFactory.CreateText(
            "TxtEndingText",
            transform,
            "更多地球修复数据已经完成汇总，\n新的行动阶段即将开启。",
            30,
            UIPalette.TextDim
        );
        SetAnchored(text.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -600f), new Vector2(1000f, 100f));

        Button returnButton = UIFactory.CreateButton(
            "BtnReturnStation",
            transform,
            "返回空间站",
            new Vector2(320f, 72f),
            UIPalette.AccentDim,
            30
        );
        SetAnchored(returnButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(320f, 72f));
        returnButton.onClick.AddListener(() => ReturnRequested?.Invoke());

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
