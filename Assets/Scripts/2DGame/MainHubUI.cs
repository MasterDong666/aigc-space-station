using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 主界面：标题 / 身份 / 地球修复进度 / 今日任务 / 预留入口。
/// 由 GameBootstrap 构建与串联，本类只负责主界面的显示与刷新。
/// </summary>
public class MainHubUI : MonoBehaviour
{
    /// <summary>点击【星际轨道巡检】时触发。</summary>
    public event Action OrbitTaskClicked;

    /// <summary>点击【基因孢子培育】时触发。</summary>
    public event Action GeneCultivationClicked;

    /// <summary>点击【生态营养液投放】时触发。</summary>
    public event Action EcologyNutrientClicked;

    /// <summary>点击【最终结局】时触发（仅在结局解锁后可用）。</summary>
    public event Action FinalEndingClicked;

    private Text progressValueText;
    private Image progressFill;
    private Button orbitButton;
    private Text orbitButtonLabel;
    private Button geneButton;
    private Text geneButtonLabel;
    private Button nutrientButton;
    private Text nutrientButtonLabel;
    private Button finalEndingButton;
    private Text finalEndingButtonLabel;
    private Text finalEndingStatusText;

    private static readonly string[] ReservedNames =
    {
        "诺亚星",
        "剧情回顾",
    };

    public void BuildUI()
    {
        Image bg = gameObject.AddComponent<Image>();
        bg.color = UIPalette.Background;

        // 顶部装饰线
        Image accentLine = UIFactory.CreatePanel("AccentLine", transform, UIPalette.Accent);
        RectTransform lineRect = accentLine.rectTransform;
        lineRect.anchorMin = new Vector2(0f, 1f);
        lineRect.anchorMax = new Vector2(1f, 1f);
        lineRect.pivot = new Vector2(0.5f, 1f);
        lineRect.sizeDelta = new Vector2(0f, 6f);

        // 标题
        Text title = UIFactory.CreateText(
            "TxtTitle",
            transform,
            "《地球重塑计划》",
            64,
            UIPalette.TextMain
        );
        SetAnchored(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(1100f, 90f));

        // 身份
        Text identity = UIFactory.CreateText(
            "TxtIdentity",
            transform,
            "第七十九任地球修复官",
            26,
            UIPalette.TextDim
        );
        SetAnchored(identity.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(800f, 40f));

        // 进度卡片
        Image card = UIFactory.CreatePanel("ProgressCard", transform, UIPalette.Panel);
        SetAnchored(card.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(760f, 190f));

        Text progressLabel = UIFactory.CreateText(
            "TxtProgressLabel",
            card.transform,
            "地球修复进度",
            28,
            UIPalette.TextDim
        );
        SetAnchored(progressLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(600f, 40f));

        progressValueText = UIFactory.CreateText(
            "TxtProgressValue",
            card.transform,
            "10%",
            52,
            UIPalette.Accent
        );
        SetAnchored(progressValueText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(600f, 64f));

        UIFactory.CreateProgressBar(
            "ProgressBar",
            card.transform,
            new Vector2(600f, 20f),
            out progressFill
        );
        SetAnchored(progressFill.transform.parent.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -134f), new Vector2(600f, 20f));

        // 今日任务
        Text taskHeader = UIFactory.CreateText(
            "TxtTaskHeader",
            transform,
            "今日任务",
            30,
            UIPalette.Accent
        );
        SetAnchored(taskHeader.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -430f), new Vector2(400f, 44f));

        // 任务按钮
        orbitButton = UIFactory.CreateButton(
            "BtnOrbit",
            transform,
            "星际轨道巡检",
            new Vector2(520f, 70f),
            UIPalette.AccentDim,
            28
        );
        SetAnchored(orbitButton.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -490f), new Vector2(520f, 70f));
        orbitButtonLabel = orbitButton.GetComponentInChildren<Text>();
        orbitButton.onClick.AddListener(() => OrbitTaskClicked?.Invoke());

        // 基因孢子培育（任务3，本阶段解锁；锁定/已完成状态在 Refresh 中处理）
        geneButton = UIFactory.CreateButton(
            "BtnGene",
            transform,
            "基因孢子培育",
            new Vector2(520f, 70f),
            UIPalette.AccentDim,
            28
        );
        SetAnchored(geneButton.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -575f), new Vector2(520f, 70f));
        geneButtonLabel = geneButton.GetComponentInChildren<Text>();
        geneButton.onClick.AddListener(() => GeneCultivationClicked?.Invoke());

        // 生态营养液投放（任务2，本阶段解锁；锁定/已完成状态在 Refresh 中处理）
        nutrientButton = UIFactory.CreateButton(
            "BtnNutrient",
            transform,
            "生态营养液投放",
            new Vector2(520f, 70f),
            UIPalette.AccentDim,
            28
        );
        SetAnchored(nutrientButton.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -660f), new Vector2(520f, 70f));
        nutrientButtonLabel = nutrientButton.GetComponentInChildren<Text>();
        nutrientButton.onClick.AddListener(() => EcologyNutrientClicked?.Invoke());

        // 预留入口（Disabled）
        for (int i = 0; i < ReservedNames.Length; i++)
        {
            Button reserved = UIFactory.CreateButton(
                "BtnReserved_" + ReservedNames[i],
                transform,
                ReservedNames[i],
                new Vector2(240f, 52f),
                UIPalette.Locked,
                22
            );
            SetAnchored(reserved.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-260f + i * 260f, 36f), new Vector2(240f, 52f));
            reserved.interactable = false;
        }

        // 最终结局入口（进度达到 GameProgressManager.EndingUnlockProgress 后解锁）
        finalEndingButton = UIFactory.CreateButton(
            "BtnFinalEnding",
            transform,
            "最终结局（锁定）",
            new Vector2(240f, 52f),
            UIPalette.AccentDim,
            22
        );
        SetAnchored(finalEndingButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(260f, 36f), new Vector2(240f, 52f));
        finalEndingButtonLabel = finalEndingButton.GetComponentInChildren<Text>();
        finalEndingButton.onClick.AddListener(() => FinalEndingClicked?.Invoke());

        finalEndingStatusText = UIFactory.CreateText(
            "TxtEndingStatus",
            transform,
            string.Empty,
            20,
            UIPalette.TextDim
        );
        SetAnchored(finalEndingStatusText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -14f), new Vector2(1000f, 56f));
    }

    public void Show()
    {
        gameObject.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /// <summary>从 GameProgressManager 读取最新进度并刷新界面。</summary>
    public void Refresh()
    {
        GameProgressManager progress = GameProgressManager.Instance;
        if (progress == null)
        {
            return;
        }

        int value = progress.EarthProgress;
        progressValueText.text = value + "%";
        progressFill.fillAmount = value / 100f;

        bool orbitDone = progress.IsTaskCompleted(OrbitCalibrationConfig.TaskId);
        orbitButton.interactable = !orbitDone;
        orbitButtonLabel.text = orbitDone ? "星际轨道巡检（已完成）" : "星际轨道巡检";

        bool geneDone = progress.IsTaskCompleted(GeneCultivationConfig.TaskId);
        geneButton.interactable = !geneDone;
        geneButtonLabel.text = geneDone ? "基因孢子培育（已完成）" : "基因孢子培育";

        bool nutrientDone = progress.IsTaskCompleted(EcologyNutrientConfig.TaskId);
        nutrientButton.interactable = !nutrientDone;
        nutrientButtonLabel.text = nutrientDone ? "生态营养液投放（已完成）" : "生态营养液投放";

        bool endingUnlocked = progress.IsEndingUnlocked;
        finalEndingButton.interactable = endingUnlocked;
        finalEndingButtonLabel.text = endingUnlocked ? "最终结局" : "最终结局（锁定）";
        finalEndingStatusText.text = endingUnlocked
            ? "最终修复方案已解锁"
            : "地球修复进度达到 " + GameProgressManager.EndingUnlockProgress + "% 后解锁\n当前修复进度：" + value + " / " + GameProgressManager.EndingUnlockProgress;
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
