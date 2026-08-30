using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 任务3【基因孢子无人机播撒培育】主流程控制器。
/// 阶段：诺亚基因库（分层基因树）→ 播种区域（拖拽框选）→ 无人机群播种
/// → 培育（真实 72 小时倒计时，可 Debug 跳过）→ 群落面板 →（进入太空大棚）。
/// 基因树逐层解锁、地块成熟状态、变异彩蛋均通过 MVPGameSession 持久化。
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

    private RectTransform selectionRect;
    private readonly List<RegionCell> regionCells = new();
    private bool dragActive;
    private Vector2 dragBeginLocal;
    private int selectedRegionIndex = -1;

    private Text sowingStatusText;
    private readonly List<RectTransform> droneIcons = new();
    private const int DroneCount = 12;

    private Text growthStatusText;
    private Button viewResultButton;
    private GenePlotData activePlot;

    private Text resultSporeValue;
    private Text resultSowTimeValue;
    private Text resultQualityValue;
    private Text resultStatusValue;
    private Text resultCommunityValue;
    private Text resultMutationValue;
    private Image mutationSprout;
    private Button mutationSproutButton;
    private Image mutationPopup;
    private Text mutationPopupBody;

    private Coroutine flowRoutine;

    /// <summary>编辑器/测试用：把当前地块的播种时间回拨到已成熟（不污染正式逻辑）。</summary>
    public void DebugSkipTimerToMature()
    {
        if (string.IsNullOrEmpty(SelectedRegionId))
        {
            return;
        }

        GenePlotData plot = MVPGameSession.GetGenePlot(SelectedRegionId);
        if (plot == null)
        {
            return;
        }

        // 把播种时刻回拨到 73 小时前：既早于 72 小时成熟点（成为"已成熟"），
        // 又保留接近 1 小时的余量便于显示剩余时间。matureAt 显式重算以保持一致。
        plot.sowedAtUtcTicks =
            (DateTime.UtcNow - TimeSpan.FromHours(73f)).Ticks;
        plot.matureAtUtcTicks = plot.sowedAtUtcTicks + MVPGameSession.MatureTicks;
        SaveManager.TrySave();
    }

    public void BuildUI()
    {
        Image bg = gameObject.AddComponent<Image>();
        bg.color = UIPalette.Background;
        MiniGameVisuals.PrepareScreen(gameObject, MiniGameThemeId.Gene);

        BuildTopBar();
        BuildLibrarySubPanel();
        BuildSeedingSubPanel();
        BuildSowingSubPanel();
        BuildGrowthSubPanel();
        BuildResultSubPanel();
        BuildMutationPopup();

        MiniGameVisuals.PolishHierarchy(transform, MiniGameThemeId.Gene);

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
        activePlot = null;

        sporeDetailText.text = "请选择孢子类型（需逐层解锁基因树）";
        drawSampleButton.interactable = false;
        seedingStatusText.text = string.Empty;
        releaseDronesButton.gameObject.SetActive(false);
        viewResultButton.gameObject.SetActive(false);
        dragActive = false;
        HideSelectionRect();
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
        MiniGameVisuals.AddStepRail(subLibrary.transform, MiniGameThemeId.Gene, new[] { "基因样本", "播种区域", "无人机群", "快速培育", "结果" }, 0);

        GameObject librarySlot = MiniGameVisuals.CreateArtSlot(
            "MediaSlot_GeneLibrary",
            subLibrary.transform,
            new Vector2(900f, 180f),
            MiniGameThemeId.Gene,
            "诺亚基因样本库 · 分层基因树"
        );
        SetAnchored(librarySlot.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(900f, 180f));

        Text treeHint = UIFactory.CreateText(
            "TxtGeneTreeHint",
            subLibrary.transform,
            "已解锁层级 L" + MVPGameSession.GeneTreeUnlockedLevel +
            "（成熟当前层后自动解锁下一层）",
            22,
            UIPalette.TextDim
        );
        SetAnchored(treeHint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -238f), new Vector2(900f, 34f));

        SporeType[] spores = GeneCultivationConfig.Spores;
        for (int i = 0; i < spores.Length; i++)
        {
            SporeType spore = spores[i];
            bool selectable = GeneCultivationConfig.IsSporeSelectable(spore);
            string label = selectable
                ? "L" + spore.layer + "  ·  可用样本  ·  " + spore.displayName
                : "L" + spore.layer + "  ·  未解锁  ·  " + spore.displayName;

            Button btn = UIFactory.CreateButton(
                "BtnSpore_" + spore.displayName,
                subLibrary.transform,
                label,
                new Vector2(300f, 92f),
                selectable ? UIPalette.PanelLight : UIPalette.Locked,
                26
            );
            SetAnchored(btn.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2((i - 1) * 330f, -285f), new Vector2(300f, 92f));

            // 锁定节点仍可点击，用于在详情区提示解锁条件（见 OnSporeClicked）。
            // 视觉上通过 Locked 配色 + "未解锁"文案区分。
            btn.interactable = true;

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
        SetAnchored(sporeDetailText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -410f), new Vector2(1000f, 150f));

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
        MiniGameVisuals.AddStepRail(subSeeding.transform, MiniGameThemeId.Gene, new[] { "基因样本", "播种区域", "无人机群", "快速培育", "结果" }, 1);

        Text hint = UIFactory.CreateText("TxtSeedingHint", subSeeding.transform, "在地图上按住并拖拽，框选一块包含绿色适宜地块的播种区", 26, UIPalette.TextDim);
        SetAnchored(hint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(1000f, 40f));

        // 播种拖拽板
        Image board = UIFactory.CreatePanel("SeedingBoard", subSeeding.transform, new Color(0.02f, 0.05f, 0.12f, 0.9f));
        SetAnchored(board.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -255f), new Vector2(920f, 380f));
        MiniGameVisuals.Round(board);
        board.raycastTarget = true;

        RegionDragHandler dragHandler = board.gameObject.AddComponent<RegionDragHandler>();
        dragHandler.owner = this;

        // 选区高亮矩形
        Image selImage = UIFactory.CreatePanel("SelectionRect", board.transform, new Color(0.36f, 0.85f, 0.60f, 0.35f));
        selImage.raycastTarget = false;
        selectionRect = selImage.rectTransform;
        selectionRect.anchorMin = new Vector2(0.5f, 1f);
        selectionRect.anchorMax = new Vector2(0.5f, 1f);
        selectionRect.pivot = new Vector2(0.5f, 1f);
        selectionRect.sizeDelta = Vector2.zero;
        HideSelectionRect();

        // 地块格（3 列 x 2 行），存 board 局部坐标用于矩形相交判定
        SeedingRegion[] regions = GeneCultivationConfig.Regions;
        for (int i = 0; i < regions.Length; i++)
        {
            SeedingRegion region = regions[i];
            int row = i / 3;
            int col = i % 3;
            float cx = (col - 1) * 300f;
            float cy = (row == 0 ? 1f : -1f) * 110f;

            Image cell = UIFactory.CreatePanel(
                "CellRegion_" + region.displayName,
                board.transform,
                region.suitable ? UIPalette.Suitable : UIPalette.Unsuitable
            );
            RectTransform cellRect = cell.rectTransform;
            cellRect.anchorMin = new Vector2(0.5f, 1f);
            cellRect.anchorMax = new Vector2(0.5f, 1f);
            cellRect.pivot = new Vector2(0.5f, 1f);
            cellRect.sizeDelta = new Vector2(280f, 140f);
            cellRect.anchoredPosition = new Vector2(cx, cy);
            MiniGameVisuals.Round(cell);

            Text cellLabel = UIFactory.CreateText(
                "CellText",
                cell.transform,
                (region.suitable ? "✓  " : "×  ") + region.displayName,
                24,
                Color.white
            );
            UIFactory.Stretch(cellLabel.rectTransform);

            regionCells.Add(new RegionCell
            {
                index = i,
                region = region,
                rect = cellRect,
                image = cell,
                center = new Vector2(cx, cy),
                half = new Vector2(140f, 70f),
            });
        }

        seedingStatusText = UIFactory.CreateText(
            "TxtSeedingStatus",
            subSeeding.transform,
            string.Empty,
            28,
            UIPalette.Warn
        );
        SetAnchored(seedingStatusText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -485f), new Vector2(1000f, 50f));

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
        MiniGameVisuals.AddStepRail(subSowing.transform, MiniGameThemeId.Gene, new[] { "基因样本", "播种区域", "无人机群", "快速培育", "结果" }, 2);

        GameObject slot = MiniGameVisuals.CreateArtSlot(
            "MediaSlot_DroneSowing",
            subSowing.transform,
            new Vector2(900f, 300f),
            MiniGameThemeId.Gene,
            "无人机孢子播撒实况"
        );
        SetAnchored(slot.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -180f), new Vector2(900f, 300f));

        // 轻量"无人机群"光点（有限数量，运行时程序化生成）
        for (int i = 0; i < DroneCount; i++)
        {
            Image drone = UIFactory.CreatePanel("Drone" + (i + 1), slot.transform, UIPalette.Accent);
            drone.rectTransform.anchorMin = new Vector2(0f, 1f);
            drone.rectTransform.anchorMax = new Vector2(0f, 1f);
            drone.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            drone.rectTransform.sizeDelta = new Vector2(12f, 12f);
            MiniGameVisuals.MakeCircle(drone);
            drone.rectTransform.anchoredPosition = new Vector2(40f, -40f);
            droneIcons.Add(drone.rectTransform);
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
        MiniGameVisuals.AddStepRail(subGrowth.transform, MiniGameThemeId.Gene, new[] { "基因样本", "播种区域", "无人机群", "快速培育", "结果" }, 3);

        Image growthCard = MiniGameVisuals.CreateCard("GrowthStatusCard", subGrowth.transform, new Vector2(920f, 300f), MiniGameThemeId.Gene);
        SetAnchored(growthCard.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 25f), new Vector2(920f, 300f));

        Text growthGlyph = UIFactory.CreateText("GrowthGlyph", growthCard.transform, "✦  ◉  ✦", 46, MiniGameVisuals.Theme(MiniGameThemeId.Gene).accentWarm);
        SetAnchored(growthGlyph.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(500f, 60f));

        growthStatusText = UIFactory.CreateText(
            "TxtGrowthStatus",
            growthCard.transform,
            string.Empty,
            40,
            UIPalette.Accent
        );
        SetAnchored(growthStatusText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(860f, 90f));

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

        Text title = UIFactory.CreateText("TxtResultTitle", subResult.transform, "培育结果 · 群落面板", 48, UIPalette.TextMain);
        SetAnchored(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(900f, 70f));
        MiniGameVisuals.AddStepRail(subResult.transform, MiniGameThemeId.Gene, new[] { "基因样本", "播种区域", "无人机群", "快速培育", "结果" }, 4);

        GameObject slot = MiniGameVisuals.CreateArtSlot(
            "MediaSlot_CultivatedPlants",
            subResult.transform,
            new Vector2(760f, 220f),
            MiniGameThemeId.Gene,
            "新生群落培育舱"
        );
        SetAnchored(slot.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -175f), new Vector2(760f, 220f));

        resultSporeValue = CreateResultRow("孢子类型", 0);
        resultSowTimeValue = CreateResultRow("播种时间", 1);
        resultQualityValue = CreateResultRow("地块品质", 2);
        resultStatusValue = CreateResultRow("成熟状态", 3);
        resultCommunityValue = CreateResultRow("群落状态", 4);
        resultMutationValue = CreateResultRow("变异状态", 5);

        // 变异嫩芽：优质成熟地块按概率触发，闪烁提示；玩家点击后弹出变异说明。
        mutationSprout = UIFactory.CreatePanel(
            "MutationSprout",
            subResult.transform,
            new Color(1f, 0.86f, 0.40f, 1f)
        );
        RectTransform sproutRect = mutationSprout.rectTransform;
        sproutRect.anchorMin = new Vector2(0.5f, 1f);
        sproutRect.anchorMax = new Vector2(0.5f, 1f);
        sproutRect.pivot = new Vector2(0.5f, 0.5f);
        sproutRect.anchoredPosition = new Vector2(0f, -285f);
        sproutRect.sizeDelta = new Vector2(56f, 56f);
        MiniGameVisuals.MakeCircle(mutationSprout);
        Text sproutGlyph = UIFactory.CreateText(
            "SproutGlyph",
            mutationSprout.transform,
            "✦",
            34,
            Color.white
        );
        UIFactory.Stretch(sproutGlyph.rectTransform);
        sproutGlyph.fontStyle = FontStyle.Bold;
        mutationSproutButton = mutationSprout.gameObject.AddComponent<Button>();
        mutationSproutButton.transition = Selectable.Transition.None;
        mutationSproutButton.onClick.AddListener(OnMutationSproutClicked);
        mutationSprout.gameObject.SetActive(false);

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

    private void BuildMutationPopup()
    {
        mutationPopup = UIFactory.CreatePanel("MutationPopup", transform, new Color(0f, 0f, 0f, 0.74f));
        StretchFill(mutationPopup.rectTransform);

        Image card = MiniGameVisuals.CreateCard(
            "MutationCard",
            mutationPopup.transform,
            new Vector2(840f, 440f),
            MiniGameThemeId.Gene
        );
        SetAnchored(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(840f, 440f));

        Text eyebrow = UIFactory.CreateText(
            "MutationEyebrow",
            card.transform,
            "彩蛋  ·  变异嫩芽",
            20,
            UIPalette.Warn
        );
        eyebrow.fontStyle = FontStyle.Bold;
        SetAnchored(eyebrow.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(760f, 34f));

        mutationPopupBody = UIFactory.CreateText(
            "MutationBody",
            card.transform,
            "",
            26,
            UIPalette.TextMain,
            TextAnchor.MiddleLeft
        );
        mutationPopupBody.horizontalOverflow = HorizontalWrapMode.Wrap;
        SetAnchored(mutationPopupBody.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(740f, 250f));

        Button confirm = UIFactory.CreateButton(
            "MutationConfirm",
            card.transform,
            "标记入册",
            new Vector2(240f, 72f),
            UIPalette.Ok,
            28
        );
        SetAnchored(confirm.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(240f, 72f));
        confirm.onClick.AddListener(CloseMutationPopup);

        mutationPopup.gameObject.SetActive(false);
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
        SetAnchored(label.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-350f, -430f - rowIndex * 44f), new Vector2(280f, 40f));

        Text value = UIFactory.CreateText(
            "TxtResultValue_" + rowIndex,
            subResult.transform,
            string.Empty,
            26,
            UIPalette.TextMain,
            TextAnchor.MiddleLeft
        );
        SetAnchored(value.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-60f, -430f - rowIndex * 44f), new Vector2(420f, 40f));
        return value;
    }

    // ===== 基因库：分层基因树 =====

    private void OnSporeClicked(int index)
    {
        SporeType spore = GeneCultivationConfig.Spores[index];
        if (!GeneCultivationConfig.IsSporeSelectable(spore))
        {
            sporeDetailText.text = "该孢子位于基因树第 " + spore.layer +
                " 层，需先成熟上一层才能解锁。";
            // 锁定节点不可作为样本来源：禁用"调取基因样本"。
            drawSampleButton.interactable = false;
            return;
        }

        SelectedSporeId = spore.id;
        sporeDetailText.text =
            "名称：" + spore.displayName + "（L" + spore.layer + "）\n" +
            "说明：" + spore.description + "\n" +
            "状态：已解锁";
        drawSampleButton.interactable = true;
    }

    private void OnDrawSampleClicked()
    {
        CurrentPhase = Phase.Seeding;
        ShowSubPanel(subSeeding);
    }

    // ===== 播种区域：拖拽框选 =====

    private void OnRegionDragBegin(PointerEventData eventData)
    {
        dragActive = true;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            selectionRect.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out dragBeginLocal
        );
        UpdateSelectionRect(dragBeginLocal, dragBeginLocal);
    }

    private void OnRegionDrag(PointerEventData eventData)
    {
        if (!dragActive)
        {
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            selectionRect.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 current
        );
        UpdateSelectionRect(dragBeginLocal, current);
    }

    private void OnRegionDragEnd(PointerEventData eventData)
    {
        if (!dragActive)
        {
            return;
        }

        dragActive = false;
        Vector2 selCenter = selectionRect.anchoredPosition;
        Vector2 selHalf = selectionRect.sizeDelta * 0.5f;

        int firstSuitable = -1;
        for (int i = 0; i < regionCells.Count; i++)
        {
            RegionCell cell = regionCells[i];
            bool overlap =
                Mathf.Abs(cell.center.x - selCenter.x) < (cell.half.x + selHalf.x) &&
                Mathf.Abs(cell.center.y - selCenter.y) < (cell.half.y + selHalf.y);

            if (overlap)
            {
                cell.image.color = UIPalette.Accent;
                if (cell.region.suitable && firstSuitable < 0)
                {
                    firstSuitable = cell.index;
                }
            }
            else
            {
                cell.image.color = cell.region.suitable ? UIPalette.Suitable : UIPalette.Unsuitable;
            }
        }

        if (firstSuitable >= 0)
        {
            selectedRegionIndex = firstSuitable;
            SelectedRegionId = regionCells[firstSuitable].region.id;
            seedingStatusText.color = UIPalette.Ok;
            seedingStatusText.text = "已框选播种区：" + regionCells[firstSuitable].region.displayName;
            releaseDronesButton.gameObject.SetActive(true);
        }
        else
        {
            SelectedRegionId = null;
            releaseDronesButton.gameObject.SetActive(false);
            seedingStatusText.color = UIPalette.Warn;
            seedingStatusText.text = "框选区域未包含适宜地块，请重新框选。";
        }

        HideSelectionRect();
    }

    private void UpdateSelectionRect(Vector2 a, Vector2 b)
    {
        selectionRect.anchoredPosition = (a + b) * 0.5f;
        selectionRect.sizeDelta = new Vector2(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        selectionRect.gameObject.SetActive(true);
    }

    private void HideSelectionRect()
    {
        selectionRect.sizeDelta = Vector2.zero;
        selectionRect.gameObject.SetActive(false);
    }

    // ===== 无人机群播种 =====

    private void OnReleaseDronesClicked()
    {
        CurrentPhase = Phase.Sowing;
        ShowSubPanel(subSowing);
        flowRoutine = StartCoroutine(DroneSowingRoutine());
    }

    private IEnumerator DroneSowingRoutine()
    {
        for (int i = 0; i < droneIcons.Count; i++)
        {
            droneIcons[i].anchoredPosition = new Vector2(50f + (i % 4) * 26f, -40f - (i / 4) * 26f);
        }

        sowingStatusText.text = "无人机群释放中……";
        float elapsed = 0f;
        float flyDuration = 2.4f;
        while (elapsed < flyDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flyDuration);
            for (int i = 0; i < droneIcons.Count; i++)
            {
                float local = Mathf.Clamp01(t * 1.5f - i * 0.10f);
                Vector2 from = new Vector2(50f + (i % 4) * 26f, -40f - (i / 4) * 26f);
                Vector2 to = new Vector2(860f - (i % 4) * 26f, -270f + (i / 4) * 26f);
                droneIcons[i].anchoredPosition = Vector2.Lerp(from, to, local);
            }
            yield return null;
        }

        sowingStatusText.text = "无人机群已覆盖选定区域";

        // 播种落库（真实 72h 计时起点）
        int quality = ComputePlotQuality();
        MVPGameSession.SowGenePlot(SelectedRegionId, SelectedSporeId, quality);

        yield return new WaitForSeconds(0.8f);
        sowingStatusText.text = "播种完成，进入培育";
        yield return new WaitForSeconds(0.8f);

        CurrentPhase = Phase.Growth;
        ShowSubPanel(subGrowth);
        flowRoutine = StartCoroutine(GrowthRoutine());
    }

    private int ComputePlotQuality()
    {
        SeedingRegion region = FindRegion(SelectedRegionId);
        int quality = 40;
        if (region.suitable)
        {
            quality += GeneCultivationConfig.QualitySuitableBonus;
        }

        quality += UnityEngine.Random.Range(0, 21);
        return Mathf.Clamp(quality, 0, 100);
    }

    // ===== 培育：真实 72h 倒计时 =====

    private IEnumerator GrowthRoutine()
    {
        GenePlotData plot = MVPGameSession.GetGenePlot(SelectedRegionId);
        activePlot = plot;

        while (true)
        {
            if (plot == null)
            {
                break;
            }

            if (MVPGameSession.IsGenePlotMature(plot))
            {
                break;
            }

            long now = DateTime.UtcNow.Ticks;
            long remain = MVPGameSession.GetGenePlotMatureAtUtcTicks(plot) - now;
            if (remain <= 0)
            {
                break;
            }

            long totalSec = remain / TimeSpan.TicksPerSecond;
            long hours = totalSec / 3600;
            long minutes = (totalSec % 3600) / 60;
            long seconds = totalSec % 60;
            growthStatusText.text =
                "培育剩余  " + hours.ToString("0") + " 小时 " +
                minutes.ToString("00") + " 分 " +
                seconds.ToString("00") + " 秒";

            yield return new WaitForEndOfFrame();
        }

        // 地块真实成熟 → 显示完成，并解锁当前层基因树下一层（禁止越级，见 TryUnlockGeneTreeNextLevel）。
        if (plot != null && MVPGameSession.IsGenePlotMature(plot))
        {
            growthStatusText.text = "群落培育完成";
            GrowthFinished = true;
            viewResultButton.gameObject.SetActive(true);

            SporeType spore = FindSpore(plot.sporeId);
            if (MVPGameSession.TryUnlockGeneTreeNextLevel(spore.layer))
            {
                Debug.Log(
                    "[GENE] 已解锁基因树下一层 → L" +
                    MVPGameSession.GeneTreeUnlockedLevel
                );
            }

            SaveManager.TrySave();
        }
    }

    // ===== 结果 / 群落面板 =====

    private void OnViewResultClicked()
    {
        CurrentPhase = Phase.Result;
        GenePlotData plot = MVPGameSession.GetGenePlot(SelectedRegionId);
        activePlot = plot;

        SporeType spore = FindSpore(SelectedSporeId);
        bool mature = plot != null && MVPGameSession.IsGenePlotMature(plot);

        resultSporeValue.text = spore.displayName + "（L" + spore.layer + "）";
        resultSowTimeValue.text = plot != null
            ? FormatUtcTime(plot.sowedAtUtcTicks)
            : "—";
        resultQualityValue.text = plot != null ? plot.quality.ToString() : "—";
        resultStatusValue.text = mature ? "已成熟" : "未成熟";
        resultCommunityValue.text = plot != null
            ? CommunityStateText(plot.quality)
            : "群落未形成";

        // 变异状态（真实存档数据）：每块地一次，触发后永久记录。
        mutationSprout.gameObject.SetActive(false);
        if (plot != null && plot.mutationTriggered)
        {
            resultMutationValue.color = UIPalette.Warn;
            resultMutationValue.text = "✦ 已发生变异（已标记入册）";
        }
        else
        {
            resultMutationValue.color = UIPalette.TextDim;
            resultMutationValue.text = "本轮未检测到变异";
        }

        // 变异彩蛋：优质成熟地块按概率触发（每块地一次）。
        if (mature && plot != null &&
            MVPGameSession.TryTriggerGenePlotMutation(plot))
        {
            StartCoroutine(MutationFlashRoutine(plot));
        }

        ShowSubPanel(subResult);
    }

    private string CommunityStateText(int quality)
    {
        if (quality >= 80)
        {
            return "群落已形成（蓬勃）";
        }

        if (quality >= 60)
        {
            return "群落已形成（旺盛）";
        }

        if (quality >= 40)
        {
            return "群落已形成（稳定）";
        }

        return "群落已形成（初步）";
    }

    /// <summary>变异嫩芽闪烁提示：先闪光提示，之后保持可见，等待玩家点击查看说明。</summary>
    private IEnumerator MutationFlashRoutine(GenePlotData plot)
    {
        resultMutationValue.color = UIPalette.Warn;
        resultMutationValue.text = "✦ 检测到变异嫩芽！点击嫩芽查看";

        mutationSprout.gameObject.SetActive(true);
        float t = 0f;
        while (t < 4f)
        {
            t += Time.deltaTime;
            float pulse = 1f + 0.18f * Mathf.Sin(t * 9f);
            mutationSprout.rectTransform.localScale =
                new Vector3(pulse, pulse, 1f);
            yield return null;
        }

        mutationSprout.rectTransform.localScale = Vector3.one;
    }

    /// <summary>玩家点击闪光嫩芽 → 弹出"生态变异发现"说明（每块地一次）。</summary>
    private void OnMutationSproutClicked()
    {
        GenePlotData plot = activePlot ?? MVPGameSession.GetGenePlot(SelectedRegionId);
        if (plot == null)
        {
            return;
        }

        MVPGameSession.MarkGenePlotMutationViewed(plot.regionId);
        mutationSprout.gameObject.SetActive(false);
        resultMutationValue.color = UIPalette.Warn;
        resultMutationValue.text = "✦ 已发生变异（已标记入册）";

        mutationPopupBody.text =
            "—— 变异嫩芽 · 记录 ——\n\n" +
            "优质地块 [" + plot.regionId + "] 上的 " +
            FindSpore(plot.sporeId).displayName +
            " 群落出现了稀有变异。\n\n" +
            "叶脉泛起荧光，这是完全超出样本库的新性状。\n" +
            "已标记入册，供后续培育研究。\n";
        mutationPopup.gameObject.SetActive(true);
        SaveManager.TrySave();
    }

    private static string FormatUtcTime(long ticks)
    {
        if (ticks <= 0L)
        {
            return "—";
        }

        return new DateTime(ticks, DateTimeKind.Utc)
            .ToString("yyyy-MM-dd HH:mm") + " UTC";
    }

    private void CloseMutationPopup()
    {
        mutationPopup.gameObject.SetActive(false);
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
        for (int i = 0; i < regionCells.Count; i++)
        {
            regionCells[i].image.color =
                regionCells[i].region.suitable ? UIPalette.Suitable : UIPalette.Unsuitable;
        }
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

    private static void StretchFill(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
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

    /// <summary>播种地块拖拽检测器（运行在播种板上）。</summary>
    private sealed class RegionDragHandler : MonoBehaviour,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        public GeneCultivationTaskController owner;

        public void OnBeginDrag(PointerEventData eventData)
        {
            owner?.OnRegionDragBegin(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            owner?.OnRegionDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            owner?.OnRegionDragEnd(eventData);
        }
    }

    private sealed class RegionCell
    {
        public int index;
        public SeedingRegion region;
        public RectTransform rect;
        public Image image;
        public Vector2 center;
        public Vector2 half;
    }
}
