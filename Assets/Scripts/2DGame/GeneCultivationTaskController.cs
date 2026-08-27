using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 任务3【基因孢子无人机播撒培育】主流程控制器。
/// 阶段：诺亚基因库 → 播种区域 → 无人机播种 → 培育 → 查看结果 →（进入太空大棚）。
/// 结构上按阶段拆分 SubPanel，方便未来替换基因树逐级解锁与真实时长系统。
/// </summary>
public class GeneCultivationTaskController : MonoBehaviour
{
    /// <summary>任务内各阶段（供测试与调试读取）。</summary>
    public enum Phase
    {
        Library,
        Seeding,
        Sowing,
        Growth,
        Result,
    }

    /// <summary>点击返回按钮时触发。</summary>
    public event Action ExitRequested;

    /// <summary>培育完成并查看结果后，点击【进入太空大棚】时触发。</summary>
    public event Action EnterGreenhouse;

    public Phase CurrentPhase { get; private set; }
    public string SelectedSporeId { get; private set; }
    public string SelectedRegionId { get; private set; }
    public bool GrowthFinished { get; private set; }

    private GameObject subLibrary;
    private GameObject subSeeding;
    private GameObject subSowing;
    private GameObject subGrowth;
    private GameObject subResult;

    private Text sporeDetailText;
    private Button drawSampleButton;

    private Text seedingStatusText;
    private Button releaseDronesButton;
    private readonly Button[] regionButtons = new Button[GeneCultivationConfig.Regions.Length];
    private int selectedRegionIndex = -1;

    private Text sowingStatusText;
    private RectTransform[] droneIcons;

    private Text growthStatusText;
    private Button viewResultButton;

    private Text resultSporeValue;
    private Text resultRegionValue;
    private Text resultStatusValue;
    private Text resultCommunityValue;

    private Coroutine flowRoutine;

    public void BuildUI()
    {
        Image bg = gameObject.AddComponent<Image>();
        bg.color = UIPalette.Background;

        BuildTopBar();
        BuildLibrarySubPanel();
        BuildSeedingSubPanel();
        BuildSowingSubPanel();
        BuildGrowthSubPanel();
        BuildResultSubPanel();

        ShowSubPanel(subLibrary);
        gameObject.SetActive(false);
    }

    /// <summary>打开任务并重置到基因库阶段。</summary>
    public void OpenTask()
    {
        StopFlowRoutine();
        CurrentPhase = Phase.Library;
        SelectedSporeId = null;
        SelectedRegionId = null;
        GrowthFinished = false;
        selectedRegionIndex = -1;

        sporeDetailText.text = "请选择孢子类型";
        drawSampleButton.interactable = false;
        seedingStatusText.text = string.Empty;
        releaseDronesButton.gameObject.SetActive(false);
        viewResultButton.gameObject.SetActive(false);
        RestoreRegionColors();

        ShowSubPanel(subLibrary);
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        StopFlowRoutine();
        gameObject.SetActive(false);
    }

    private void BuildTopBar()
    {
        Button back = UIFactory.CreateButton(
            "BtnBackGene",
            transform,
            "返回",
            new Vector2(140f, 56f),
            UIPalette.PanelLight,
            24
        );
        SetAnchored(back.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -30f), new Vector2(140f, 56f));
        back.onClick.AddListener(() => ExitRequested?.Invoke());
    }

    private void BuildLibrarySubPanel()
    {
        subLibrary = CreateSubPanel("SubGeneLibrary");

        Text title = UIFactory.CreateText("TxtGeneTitle", subLibrary.transform, "诺亚基因库终端", 48, UIPalette.TextMain);
        SetAnchored(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(900f, 70f));

        // 媒体占位：基因库背景（正式美术在此节点内替换）
        GameObject librarySlot = UIFactory.CreateMediaSlot(
            "MediaSlot_GeneLibrary",
            "【占位】基因库背景（待正式美术）",
            subLibrary.transform,
            new Vector2(900f, 180f)
        );
        SetAnchored(librarySlot.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(900f, 180f));

        // 孢子选择（线性解锁：仅微生物可用）
        SporeType[] spores = GeneCultivationConfig.Spores;
        for (int i = 0; i < spores.Length; i++)
        {
            SporeType spore = spores[i];
            Button btn = UIFactory.CreateButton(
                "BtnSpore_" + spore.displayName,
                subLibrary.transform,
                spore.unlocked ? spore.displayName : spore.displayName + "（锁定）",
                new Vector2(300f, 84f),
                spore.unlocked ? UIPalette.PanelLight : UIPalette.Locked,
                28
            );
            SetAnchored(btn.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2((i - 1) * 330f, -280f), new Vector2(300f, 84f));
            btn.interactable = spore.unlocked;

            int index = i;
            btn.onClick.AddListener(() => OnSporeClicked(index));
        }

        sporeDetailText = UIFactory.CreateText(
            "TxtSporeDetail",
            subLibrary.transform,
            "请选择孢子类型",
            26,
            UIPalette.TextDim
        );
        SetAnchored(sporeDetailText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -400f), new Vector2(1000f, 150f));

        drawSampleButton = UIFactory.CreateButton(
            "BtnDrawSample",
            subLibrary.transform,
            "调取基因样本",
            new Vector2(320f, 72f),
            UIPalette.AccentDim,
            30
        );
        SetAnchored(drawSampleButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(320f, 72f));
        drawSampleButton.onClick.AddListener(OnDrawSampleClicked);
        drawSampleButton.interactable = false;
    }

    private void BuildSeedingSubPanel()
    {
        subSeeding = CreateSubPanel("SubSeeding");

        Text title = UIFactory.CreateText("TxtSeedingTitle", subSeeding.transform, "播种区域", 48, UIPalette.TextMain);
        SetAnchored(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(900f, 70f));

        Text hint = UIFactory.CreateText("TxtSeedingHint", subSeeding.transform, "请选择适宜播种的绿色区域", 26, UIPalette.TextDim);
        SetAnchored(hint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(800f, 40f));

        SeedingRegion[] regions = GeneCultivationConfig.Regions;
        for (int i = 0; i < regions.Length; i++)
        {
            SeedingRegion region = regions[i];
            int row = i / 3;
            int col = i % 3;

            Button btn = UIFactory.CreateButton(
                "BtnRegion_" + region.displayName,
                subSeeding.transform,
                region.displayName,
                new Vector2(280f, 100f),
                region.suitable ? UIPalette.Suitable : UIPalette.Unsuitable,
                26
            );
            SetAnchored(btn.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2((col - 1) * 310f, -200f - row * 120f), new Vector2(280f, 100f));

            regionButtons[i] = btn;
            int index = i;
            btn.onClick.AddListener(() => OnRegionClicked(index));
        }

        seedingStatusText = UIFactory.CreateText(
            "TxtSeedingStatus",
            subSeeding.transform,
            string.Empty,
            28,
            UIPalette.Warn
        );
        SetAnchored(seedingStatusText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -470f), new Vector2(1000f, 50f));

        releaseDronesButton = UIFactory.CreateButton(
            "BtnReleaseDrones",
            subSeeding.transform,
            "释放无人机群",
            new Vector2(320f, 72f),
            UIPalette.Ok,
            30
        );
        SetAnchored(releaseDronesButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(320f, 72f));
        releaseDronesButton.onClick.AddListener(OnReleaseDronesClicked);
        releaseDronesButton.gameObject.SetActive(false);
    }

    private void BuildSowingSubPanel()
    {
        subSowing = CreateSubPanel("SubSowing");

        Text title = UIFactory.CreateText("TxtSowingTitle", subSowing.transform, "无人机播种", 48, UIPalette.TextMain);
        SetAnchored(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(900f, 70f));

        // 媒体占位：无人机播种（正式美术/视频在此节点内替换）
        GameObject slot = UIFactory.CreateMediaSlot(
            "MediaSlot_DroneSowing",
            "【占位】无人机播种画面（待正式美术/视频）",
            subSowing.transform,
            new Vector2(900f, 300f)
        );
        SetAnchored(slot.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -180f), new Vector2(900f, 300f));

        // 简易"无人机"图标（占位动画）
        droneIcons = new RectTransform[3];
        for (int i = 0; i < droneIcons.Length; i++)
        {
            Image drone = UIFactory.CreatePanel("Drone" + (i + 1), slot.transform, UIPalette.Accent);
            drone.rectTransform.anchorMin = new Vector2(0f, 1f);
            drone.rectTransform.anchorMax = new Vector2(0f, 1f);
            drone.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            drone.rectTransform.sizeDelta = new Vector2(22f, 22f);
            drone.rectTransform.anchoredPosition = new Vector2(40f + i * 30f, -30f - i * 20f);
            droneIcons[i] = drone.rectTransform;
        }

        sowingStatusText = UIFactory.CreateText(
            "TxtSowingStatus",
            subSowing.transform,
            string.Empty,
            30,
            UIPalette.Accent
        );
        SetAnchored(sowingStatusText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -520f), new Vector2(900f, 50f));
    }

    private void BuildGrowthSubPanel()
    {
        subGrowth = CreateSubPanel("SubGrowth");

        Text title = UIFactory.CreateText("TxtGrowthTitle", subGrowth.transform, "培育中", 48, UIPalette.TextMain);
        SetAnchored(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(900f, 70f));

        growthStatusText = UIFactory.CreateText(
            "TxtGrowthStatus",
            subGrowth.transform,
            string.Empty,
            44,
            UIPalette.Accent
        );
        SetAnchored(growthStatusText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(1000f, 80f));

        viewResultButton = UIFactory.CreateButton(
            "BtnViewResult",
            subGrowth.transform,
            "查看培育结果",
            new Vector2(320f, 72f),
            UIPalette.AccentDim,
            30
        );
        SetAnchored(viewResultButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(320f, 72f));
        viewResultButton.onClick.AddListener(OnViewResultClicked);
        viewResultButton.gameObject.SetActive(false);
    }

    private void BuildResultSubPanel()
    {
        subResult = CreateSubPanel("SubResult");

        Text title = UIFactory.CreateText("TxtResultTitle", subResult.transform, "培育结果", 48, UIPalette.TextMain);
        SetAnchored(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(900f, 70f));

        // 媒体占位：培育植物群落（正式美术在此节点内替换）
        GameObject slot = UIFactory.CreateMediaSlot(
            "MediaSlot_CultivatedPlants",
            "【占位】培育植物群落（待正式美术）",
            subResult.transform,
            new Vector2(760f, 240f)
        );
        SetAnchored(slot.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(760f, 240f));

        resultSporeValue = CreateResultRow("孢子类型", 0);
        resultRegionValue = CreateResultRow("播种区域", 1);
        resultStatusValue = CreateResultRow("培育成功", 2);
        resultCommunityValue = CreateResultRow("群落状态", 3);

        // 预留：变异嫩芽系统（本阶段仅结构占位，正式随机系统待接入）
        Text mutation = UIFactory.CreateText(
            "TxtMutationPlaceholder",
            subResult.transform,
            "发现变异嫩芽：暂无（占位，正式随机系统待接入）",
            22,
            UIPalette.TextDim
        );
        SetAnchored(mutation.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -620f), new Vector2(900f, 34f));

        Button enterGreenhouse = UIFactory.CreateButton(
            "BtnEnterGreenhouse",
            subResult.transform,
            "进入太空大棚",
            new Vector2(320f, 72f),
            UIPalette.Ok,
            30
        );
        SetAnchored(enterGreenhouse.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(320f, 72f));
        enterGreenhouse.onClick.AddListener(() => EnterGreenhouse?.Invoke());
    }

    private Text CreateResultRow(string labelText, int rowIndex)
    {
        Text label = UIFactory.CreateText(
            "TxtResultLabel_" + rowIndex,
            subResult.transform,
            labelText,
            26,
            UIPalette.TextDim,
            TextAnchor.MiddleLeft
        );
        SetAnchored(label.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-350f, -430f - rowIndex * 48f), new Vector2(280f, 40f));

        Text value = UIFactory.CreateText(
            "TxtResultValue_" + rowIndex,
            subResult.transform,
            string.Empty,
            26,
            UIPalette.TextMain,
            TextAnchor.MiddleLeft
        );
        SetAnchored(value.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-60f, -430f - rowIndex * 48f), new Vector2(420f, 40f));
        return value;
    }

    private void OnSporeClicked(int index)
    {
        SporeType spore = GeneCultivationConfig.Spores[index];
        if (!spore.unlocked)
        {
            return;
        }

        SelectedSporeId = spore.id;
        sporeDetailText.text =
            "名称：" + spore.displayName + "\n" +
            "说明：" + spore.description + "\n" +
            "状态：已解锁";
        drawSampleButton.interactable = true;
    }

    private void OnDrawSampleClicked()
    {
        CurrentPhase = Phase.Seeding;
        ShowSubPanel(subSeeding);
    }

    private void OnRegionClicked(int index)
    {
        SeedingRegion region = GeneCultivationConfig.Regions[index];

        if (!region.suitable)
        {
            seedingStatusText.color = UIPalette.Warn;
            seedingStatusText.text = "该区域当前不适宜进行基因播种。";
            releaseDronesButton.gameObject.SetActive(false);
            return;
        }

        RestoreRegionColors();
        selectedRegionIndex = index;
        SelectedRegionId = region.id;

        // 高亮选中区域
        Image image = regionButtons[index].GetComponent<Image>();
        image.color = UIPalette.Accent;

        seedingStatusText.color = UIPalette.Ok;
        seedingStatusText.text = "已选择：" + region.displayName + "（适宜播种）";
        releaseDronesButton.gameObject.SetActive(true);
    }

    private void OnReleaseDronesClicked()
    {
        CurrentPhase = Phase.Sowing;
        ShowSubPanel(subSowing);
        flowRoutine = StartCoroutine(DroneSowingRoutine());
    }

    private IEnumerator DroneSowingRoutine()
    {
        // 复位无人机图标
        for (int i = 0; i < droneIcons.Length; i++)
        {
            droneIcons[i].anchoredPosition = new Vector2(40f + i * 30f, -30f - i * 20f);
        }

        sowingStatusText.text = "无人机群释放中……";
        float elapsed = 0f;
        float flyDuration = 2.2f;
        while (elapsed < flyDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flyDuration);
            for (int i = 0; i < droneIcons.Length; i++)
            {
                float local = Mathf.Clamp01(t * 1.4f - i * 0.25f);
                droneIcons[i].anchoredPosition = Vector2.Lerp(
                    new Vector2(40f + i * 30f, -30f - i * 20f),
                    new Vector2(860f - i * 70f, -270f + i * 40f),
                    local
                );
            }
            yield return null;
        }

        sowingStatusText.text = "无人机群已覆盖选定区域";
        yield return new WaitForSeconds(0.8f);

        sowingStatusText.text = "播种完成";
        yield return new WaitForSeconds(0.8f);

        CurrentPhase = Phase.Growth;
        ShowSubPanel(subGrowth);
        flowRoutine = StartCoroutine(GrowthRoutine());
    }

    private IEnumerator GrowthRoutine()
    {
        GrowthStage[] stages = GeneCultivationConfig.GrowthStages;

        for (int i = 0; i < stages.Length - 1; i++)
        {
            GrowthStage stage = stages[i];
            growthStatusText.text = "培育状态：" + stage.hoursRemaining.ToString("0") + " 小时（" + stage.displayName + "）";
            yield return new WaitForSeconds(1.2f);
        }

        growthStatusText.text = "群落培育完成";
        GrowthFinished = true;
        viewResultButton.gameObject.SetActive(true);
    }

    private void OnViewResultClicked()
    {
        CurrentPhase = Phase.Result;

        SporeType spore = FindSpore(SelectedSporeId);
        SeedingRegion region = FindRegion(SelectedRegionId);

        resultSporeValue.text = spore.displayName;
        resultRegionValue.text = region.displayName;
        resultStatusValue.text = "是";
        resultCommunityValue.text = "群落已形成（旺盛）";

        ShowSubPanel(subResult);
    }

    private static SporeType FindSpore(string id)
    {
        SporeType[] spores = GeneCultivationConfig.Spores;
        for (int i = 0; i < spores.Length; i++)
        {
            if (spores[i].id == id)
            {
                return spores[i];
            }
        }

        return spores[0];
    }

    private static SeedingRegion FindRegion(string id)
    {
        SeedingRegion[] regions = GeneCultivationConfig.Regions;
        for (int i = 0; i < regions.Length; i++)
        {
            if (regions[i].id == id)
            {
                return regions[i];
            }
        }

        return regions[0];
    }

    private void RestoreRegionColors()
    {
        SeedingRegion[] regions = GeneCultivationConfig.Regions;
        for (int i = 0; i < regions.Length; i++)
        {
            regionButtons[i].GetComponent<Image>().color =
                regions[i].suitable ? UIPalette.Suitable : UIPalette.Unsuitable;
        }
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
        subLibrary.SetActive(active == subLibrary);
        subSeeding.SetActive(active == subSeeding);
        subSowing.SetActive(active == subSowing);
        subGrowth.SetActive(active == subGrowth);
        subResult.SetActive(active == subResult);
    }

    private void StopFlowRoutine()
    {
        if (flowRoutine != null)
        {
            StopCoroutine(flowRoutine);
            flowRoutine = null;
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
