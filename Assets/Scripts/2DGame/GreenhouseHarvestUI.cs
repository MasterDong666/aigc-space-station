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

    /// <summary>点击返回主界面时触发（收获未完成前可退出）。</summary>
    public event Action ReturnRequested;

    /// <summary>收获完成后点击【继续】时触发（进入结尾桥接）。</summary>
    public event Action ContinueToEnding;

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

        BuildOverviewSubPanel();
        BuildHarvestingSubPanel();
        BuildHarvestDoneSubPanel();

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

        // 媒体占位：太空大棚全景（正式美术/视频在此节点内替换）
        GameObject slot = UIFactory.CreateMediaSlot(
            "MediaSlot_Greenhouse",
            "【占位】太空大棚全景（待正式美术/视频）",
            subOverview.transform,
            new Vector2(900f, 340f)
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

        // 媒体占位：机器人机械臂特写（正式美术/视频在此节点内替换）
        GameObject slot = UIFactory.CreateMediaSlot(
            "MediaSlot_HarvestRobots",
            "【占位】机器人机械臂特写（待正式美术/视频）",
            subHarvesting.transform,
            new Vector2(900f, 340f)
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

        Text newData = UIFactory.CreateText(
            "TxtNewData",
            subHarvestDone.transform,
            "收到新的修复计划数据",
            44,
            UIPalette.Accent
        );
        SetAnchored(newData.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), new Vector2(1000f, 70f));

        Button continueButton = UIFactory.CreateButton(
            "BtnContinue",
            subHarvestDone.transform,
            "继续",
            new Vector2(320f, 72f),
            UIPalette.AccentDim,
            30
        );
        SetAnchored(continueButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(320f, 72f));
        continueButton.onClick.AddListener(() => ContinueToEnding?.Invoke());
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
