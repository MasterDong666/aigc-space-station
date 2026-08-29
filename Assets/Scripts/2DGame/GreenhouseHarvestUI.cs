using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 太空大棚收获面板（剧情分镜：主角调出光屏查看大棚 → 确认收获 → 机器人收获过场 → 收到新数据）。
/// 正式机器人机械臂特写/大棚全景等媒体由其他成员提供，本类只做占位与可替换接口。
/// </summary>
public class GreenhouseHarvestUI : MonoBehaviour
{
    /// <summary>面板内阶段（供测试与调试读取）。</summary>
    public enum Phase
    {
        Overview,
        Harvesting,
        HarvestDone,
    }

    /// <summary>点击返回主界面时触发（收获前可退出；收获完成后点击【返回主界面】同样触发）。</summary>
    public event Action ReturnRequested;

    public Phase CurrentPhase { get; private set; }
    public bool HarvestCompleted { get; private set; }

    private GameObject subOverview;
    private GameObject subHarvesting;
    private GameObject subHarvestDone;

    private Text robotStatusText;
    private Coroutine harvestRoutine;
    private bool rewardAwarded;

    public void BuildUI()
    {
        Image bg = gameObject.AddComponent<Image>();
        bg.color = UIPalette.Background;
        MiniGameVisuals.PrepareScreen(gameObject, MiniGameThemeId.Gene);

        BuildOverviewSubPanel();
        BuildHarvestingSubPanel();
        BuildHarvestDoneSubPanel();

        MiniGameVisuals.PolishHierarchy(transform, MiniGameThemeId.Gene);

        ShowSubPanel(subOverview);
        gameObject.SetActive(false);
    }

    public void Show()
    {
        StopHarvestRoutine();
        CurrentPhase = Phase.Overview;
        HarvestCompleted = false;
        rewardAwarded = false;
        ShowSubPanel(subOverview);
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        StopHarvestRoutine();
        gameObject.SetActive(false);
    }

    private void BuildOverviewSubPanel()
    {
        subOverview = CreateSubPanel("SubGreenhouseOverview");

        Text title = UIFactory.CreateText("TxtGreenhouseTitle", subOverview.transform, "太空培育大棚", 48, UIPalette.TextMain);
        SetAnchored(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(900f, 70f));
        MiniGameVisuals.AddStepRail(subOverview.transform, MiniGameThemeId.Gene, new[] { "确认作物", "自动收获", "同步数据" }, 0);

        GameObject slot = MiniGameVisuals.CreateArtSlot(
            "MediaSlot_Greenhouse",
            subOverview.transform,
            new Vector2(900f, 340f),
            MiniGameThemeId.Gene,
            "太空大棚 · 首批培育作物"
        );
        SetAnchored(slot.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -180f), new Vector2(900f, 340f));

        Text hint = UIFactory.CreateText(
            "TxtHarvestHint",
            subOverview.transform,
            "首批培育作物已达到收获标准。",
            30,
            UIPalette.Ok
        );
        SetAnchored(hint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -560f), new Vector2(1000f, 50f));

        Button confirm = UIFactory.CreateButton(
            "BtnConfirmHarvest",
            subOverview.transform,
            "确认收获",
            new Vector2(320f, 72f),
            UIPalette.Ok,
            30
        );
        SetAnchored(confirm.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(320f, 72f));
        confirm.onClick.AddListener(OnConfirmHarvestClicked);

        Button back = UIFactory.CreateButton(
            "BtnBackGreenhouse",
            subOverview.transform,
            "返回主界面",
            new Vector2(200f, 56f),
            UIPalette.PanelLight,
            24
        );
        SetAnchored(back.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -30f), new Vector2(200f, 56f));
        back.onClick.AddListener(() => ReturnRequested?.Invoke());
    }

    private void BuildHarvestingSubPanel()
    {
        subHarvesting = CreateSubPanel("SubHarvesting");

        Text title = UIFactory.CreateText("TxtHarvestingTitle", subHarvesting.transform, "自动收获", 48, UIPalette.TextMain);
        SetAnchored(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(900f, 70f));
        MiniGameVisuals.AddStepRail(subHarvesting.transform, MiniGameThemeId.Gene, new[] { "确认作物", "自动收获", "同步数据" }, 1);

        GameObject slot = MiniGameVisuals.CreateArtSlot(
            "MediaSlot_HarvestRobots",
            subHarvesting.transform,
            new Vector2(900f, 340f),
            MiniGameThemeId.Gene,
            "智能采收单元运行中"
        );
        SetAnchored(slot.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -180f), new Vector2(900f, 340f));

        robotStatusText = UIFactory.CreateText(
            "TxtRobotStatus",
            subHarvesting.transform,
            string.Empty,
            34,
            UIPalette.Accent
        );
        SetAnchored(robotStatusText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -560f), new Vector2(1000f, 60f));
    }

    private void BuildHarvestDoneSubPanel()
    {
        subHarvestDone = CreateSubPanel("SubHarvestDone");

        MiniGameVisuals.AddStepRail(subHarvestDone.transform, MiniGameThemeId.Gene, new[] { "确认作物", "自动收获", "同步数据" }, 2);

        Image resultCard = MiniGameVisuals.CreateCard("HarvestResultCard", subHarvestDone.transform, new Vector2(900f, 390f), MiniGameThemeId.Gene);
        SetAnchored(resultCard.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(900f, 390f));

        Text successGlyph = UIFactory.CreateText("SuccessGlyph", resultCard.transform, "✓", 82, MiniGameVisuals.Theme(MiniGameThemeId.Gene).accentWarm);
        SetAnchored(successGlyph.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(160f, 100f));

        Text newData = UIFactory.CreateText(
            "TxtNewData",
            resultCard.transform,
            "新一批修复计划数据已同步",
            44,
            UIPalette.Accent
        );
        SetAnchored(newData.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(780f, 70f));

        Text detail = UIFactory.CreateText("TxtHarvestDetail", resultCard.transform, "温室作物已入库，归墟生态恢复数据获得更新。", 25, UIPalette.TextDim);
        SetAnchored(detail.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 72f), new Vector2(760f, 42f));

        // 任务3到此结束：返回主界面（不再自动进入结局）
        Button backToHubButton = UIFactory.CreateButton(
            "BtnBackToHub",
            subHarvestDone.transform,
            "返回主界面",
            new Vector2(320f, 72f),
            UIPalette.AccentDim,
            30
        );
        SetAnchored(backToHubButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(320f, 72f));
        backToHubButton.onClick.AddListener(() => ReturnRequested?.Invoke());
    }

    private void OnConfirmHarvestClicked()
    {
        CurrentPhase = Phase.Harvesting;
        ShowSubPanel(subHarvesting);
        harvestRoutine = StartCoroutine(HarvestRoutine());
    }

    private IEnumerator HarvestRoutine()
    {
        robotStatusText.text = "自动收获机器人正在工作……";
        yield return new WaitForSeconds(2.5f);

        robotStatusText.text = "收获完成";

        // 收获完成 = 任务完成点：统一走 GameProgressManager，防重复发放
        GameProgressManager progress = GameProgressManager.Instance;
        if (progress != null && !rewardAwarded)
        {
            progress.TryCompleteTask(GeneCultivationConfig.TaskId, GeneCultivationConfig.Reward);
            rewardAwarded = true;
        }

        yield return new WaitForSeconds(0.8f);

        HarvestCompleted = true;
        CurrentPhase = Phase.HarvestDone;
        ShowSubPanel(subHarvestDone);
    }

    private GameObject CreateSubPanel(string name)
    {
        RectTransform rect = UIFactory.CreateRect(name, transform);
        UIFactory.Stretch(rect);
        MiniGameVisuals.AddEntrance(rect.gameObject);
        rect.gameObject.SetActive(false);
        return rect.gameObject;
    }

    private void ShowSubPanel(GameObject active)
    {
        subOverview.SetActive(active == subOverview);
        subHarvesting.SetActive(active == subHarvesting);
        subHarvestDone.SetActive(active == subHarvestDone);
    }

    private void StopHarvestRoutine()
    {
        if (harvestRoutine != null)
        {
            StopCoroutine(harvestRoutine);
            harvestRoutine = null;
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
