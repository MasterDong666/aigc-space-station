using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 拖拽框选组件：在地块面板上按住拖动生成半透明矩形选择框，松开时判定范围。
/// 只在板面本地坐标内生效，超出部分自动裁剪。
/// </summary>
public class DragSelectionArea : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    /// <summary>每次松开指针时触发（无论范围是否合法，由调用方读取 HasValidSelection）。</summary>
    public event Action SelectionFinished;

    public RectTransform selectionBox;
    public float minArea;

    public Rect CurrentRect { get; private set; }
    public bool HasValidSelection { get; private set; }
    public bool IsDragging { get; private set; }

    private RectTransform rectT;
    private Vector2 startLocal;
    private Vector2 currentLocal;

    private void Awake()
    {
        rectT = GetComponent<RectTransform>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectT, eventData.position, eventData.pressEventCamera, out local))
        {
            return;
        }

        startLocal = ClampToBoard(local);
        currentLocal = startLocal;
        IsDragging = true;
        HasValidSelection = false;
        selectionBox.gameObject.SetActive(true);
        UpdateBox(startLocal, startLocal);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsDragging)
        {
            return;
        }

        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectT, eventData.position, eventData.pressEventCamera, out local))
        {
            return;
        }

        currentLocal = ClampToBoard(local);
        UpdateBox(startLocal, currentLocal);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!IsDragging)
        {
            return;
        }

        IsDragging = false;
        CurrentRect = RectFromPoints(startLocal, currentLocal);
        HasValidSelection = CurrentRect.width * CurrentRect.height >= minArea;

        if (SelectionFinished != null)
        {
            SelectionFinished();
        }
    }

    /// <summary>清空当前选择框。</summary>
    public void ResetSelection()
    {
        HasValidSelection = false;
        CurrentRect = default(Rect);
        selectionBox.gameObject.SetActive(false);
    }

    private Vector2 ClampToBoard(Vector2 local)
    {
        Vector2 half = rectT.rect.size * 0.5f;
        local.x = Mathf.Clamp(local.x, -half.x, half.x);
        local.y = Mathf.Clamp(local.y, -half.y, half.y);
        return local;
    }

    private void UpdateBox(Vector2 p0, Vector2 p1)
    {
        Rect rect = RectFromPoints(p0, p1);
        selectionBox.anchoredPosition = rect.center;
        selectionBox.sizeDelta = rect.size;
    }

    private static Rect RectFromPoints(Vector2 a, Vector2 b)
    {
        Vector2 min = Vector2.Min(a, b);
        Vector2 max = Vector2.Max(a, b);
        return new Rect(min, max - min);
    }
}

/// <summary>
/// 任务2【分区生态营养液精准投放】主流程控制器。
/// 阶段：归墟地表监测终端 → 浓度选择 → 规划投放范围（拖拽框选）→ 卫星投放动画 → 土壤检测报告。
/// 所有数值与判定规则读取 EcologyNutrientConfig。
/// </summary>
public class EcologyNutrientTaskController : MonoBehaviour
{
    /// <summary>任务内阶段（供测试与调试读取）。</summary>
    public enum Phase
    {
        Map,
        Planning,
        Delivering,
        Report,
    }

    /// <summary>错误浓度每次扣除的修复进度（可配置，下限由权威进度系统钳制为 0）。</summary>
    private const int WrongOperationPenalty = 1;

    /// <summary>点击返回按钮时触发。</summary>
    public event Action ExitRequested;

    /// <summary>任务完成并发放奖励后触发（参数为奖励值）。</summary>
    public event Action<int> ReportSubmitted;

    public Phase CurrentPhase { get; private set; }
    public string SelectedZoneId { get; private set; }
    public string SelectedTierId { get; private set; }
    public int MistakeCount { get; private set; }
    public bool DeliveryDone { get; private set; }
    public bool SelectionValid => dragArea != null && dragArea.HasValidSelection;

    private GameObject subMap;
    private GameObject subPlanning;
    private GameObject subDelivering;
    private GameObject subReport;

    // 地图阶段
    private Text dataZoneValue;
    private Text dataPollutionValue;
    private Text dataActivityValue;
    private Text dataRecommendValue;
    private Text tierPromptText;
    private GameObject tierButtonsRoot;
    private Text mapStatusText;
    private Button resetTierButton;

    // 规划阶段
    private Text planningStatusText;
    private Button confirmAreaButton;
    private DragSelectionArea dragArea;
    private Image planningBoardImage;
    private Text planningBoardLabel;

    // 投放阶段
    private Text deliveryStatusText;
    private Image fogOverlay;
    private RectTransform scanLine;
    private Button launchButton;
    private Button viewReportButton;

    // 报告阶段
    private Text reportZoneValue;
    private Text reportTierValue;
    private Text reportCoverageValue;
    private Text reportResultValue;
    private Text reportActivityValue;

    private int coveragePercent;
    private Coroutine deliveryRoutine;

    public void BuildUI()
    {
        Image bg = gameObject.AddComponent<Image>();
        bg.color = UIPalette.Background;
        MiniGameVisuals.PrepareScreen(gameObject, MiniGameThemeId.Ecology);

        BuildTopBar();
        BuildMapSubPanel();
        BuildPlanningSubPanel();
        BuildDeliveringSubPanel();
        BuildReportSubPanel();

        MiniGameVisuals.PolishHierarchy(transform, MiniGameThemeId.Ecology);

        ShowSubPanel(subMap);
        gameObject.SetActive(false);
    }

    /// <summary>打开任务并重置到地图阶段。</summary>
    public void OpenTask()
    {
        StopDeliveryRoutine();
        CurrentPhase = Phase.Map;
        SelectedZoneId = null;
        SelectedTierId = null;
        MistakeCount = 0;
        DeliveryDone = false;
        coveragePercent = 0;

        dataZoneValue.text = "等待选择地块";
        dataPollutionValue.text = "—";
        dataActivityValue.text = "—";
        dataRecommendValue.text = "选择左侧区域开始扫描";
        tierPromptText.gameObject.SetActive(false);
        tierButtonsRoot.SetActive(false);
        mapStatusText.text = string.Empty;
        resetTierButton.gameObject.SetActive(false);
        planningStatusText.text = string.Empty;
        confirmAreaButton.gameObject.SetActive(false);
        deliveryStatusText.text = string.Empty;
        viewReportButton.gameObject.SetActive(false);
        launchButton.gameObject.SetActive(true);
        dragArea.ResetSelection();

        ShowSubPanel(subMap);
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        StopDeliveryRoutine();
        gameObject.SetActive(false);
    }

    private void BuildTopBar()
    {
        Button back = UIFactory.CreateButton(
            "BtnBackEcology",
            transform,
            "返回",
            new Vector2(140f, 56f),
            UIPalette.PanelLight,
            24
        );
        SetAnchored(back.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -30f), new Vector2(140f, 56f));
        back.onClick.AddListener(() => ExitRequested?.Invoke());
    }

    private void BuildMapSubPanel()
    {
        subMap = CreateSubPanel("SubEcologyMap");

        Text title = UIFactory.CreateText("TxtEcologyTitle", subMap.transform, "归墟地表监测终端", 48, UIPalette.TextMain);
        SetAnchored(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(900f, 70f));
        MiniGameVisuals.AddStepRail(subMap.transform, MiniGameThemeId.Ecology, new[] { "分析地块", "规划范围", "卫星投放", "检测报告" }, 0);

        GameObject mapSlot = MiniGameVisuals.CreateArtSlot(
            "MediaSlot_EarthSurface",
            subMap.transform,
            new Vector2(880f, 420f),
            MiniGameThemeId.Ecology,
            string.Empty
        );
        SetAnchored(mapSlot.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-260f, -170f), new Vector2(880f, 420f));

        // 四块污染地块（色块占位，正式美术替换节点内容）
        PollutionZone[] zones = EcologyNutrientConfig.Zones;
        for (int i = 0; i < zones.Length; i++)
        {
            PollutionZone zone = zones[i];
            int row = i / 2;
            int col = i % 2;

            Button zoneBtn = UIFactory.CreateButton(
                "BtnZone_" + zone.displayName,
                mapSlot.transform,
                (i + 1).ToString("00") + "  " + zone.displayName,
                new Vector2(400f, 180f),
                EcologyNutrientConfig.ParseColor(zone.colorHex),
                26
            );
            SetAnchored(zoneBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2((col - 0.5f) * 420f, -40f - row * 200f), new Vector2(400f, 180f));

            int index = i;
            zoneBtn.onClick.AddListener(() => OnZoneClicked(index));
        }

        // 右侧数据面板
        Image dataPanel = MiniGameVisuals.CreateCard("DataPanel", subMap.transform, new Vector2(480f, 420f), MiniGameThemeId.Ecology);
        SetAnchored(dataPanel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(500f, -170f), new Vector2(480f, 420f));

        Text dataHeader = UIFactory.CreateText("TxtDataHeader", dataPanel.transform, "地块数据", 30, UIPalette.Accent);
        SetAnchored(dataHeader.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(400f, 40f));

        dataZoneValue = CreateDataRow(dataPanel.transform, "区域", 0);
        dataPollutionValue = CreateDataRow(dataPanel.transform, "污染指数", 1);
        dataActivityValue = CreateDataRow(dataPanel.transform, "土壤活性", 2);
        dataRecommendValue = CreateDataRow(dataPanel.transform, "建议", 3);

        tierPromptText = UIFactory.CreateText("TxtTierPrompt", dataPanel.transform, "【请选择营养液浓度】", 24, UIPalette.Accent);
        SetAnchored(tierPromptText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -270f), new Vector2(400f, 34f));
        tierPromptText.gameObject.SetActive(false);

        // 浓度按钮
        tierButtonsRoot = new GameObject("TierButtons", typeof(RectTransform));
        tierButtonsRoot.transform.SetParent(dataPanel.transform, false);
        NutrientTier[] tiers = EcologyNutrientConfig.Tiers;
        for (int i = 0; i < tiers.Length; i++)
        {
            NutrientTier tier = tiers[i];
            Button tierBtn = UIFactory.CreateButton(
                "BtnTier_" + tier.displayName,
                tierButtonsRoot.transform,
                tier.displayName,
                new Vector2(100f, 60f),
                UIPalette.PanelLight,
                24
            );
            SetAnchored(tierBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2((i - 1.5f) * 112f, -320f), new Vector2(100f, 60f));

            int index = i;
            tierBtn.onClick.AddListener(() => OnTierClicked(index));
        }
        tierButtonsRoot.SetActive(false);

        mapStatusText = UIFactory.CreateText("TxtMapStatus", dataPanel.transform, "", 22, UIPalette.Warn);
        SetAnchored(mapStatusText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -380f), new Vector2(420f, 34f));

        resetTierButton = UIFactory.CreateButton("BtnResetTier", dataPanel.transform, "重置浓度", new Vector2(160f, 52f), UIPalette.PanelLight, 22);
        SetAnchored(resetTierButton.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -415f), new Vector2(160f, 52f));
        resetTierButton.onClick.AddListener(OnResetTierClicked);
        resetTierButton.gameObject.SetActive(false);
    }

    private Text CreateDataRow(Transform parent, string labelText, int rowIndex)
    {
        Text label = UIFactory.CreateText(
            "TxtDataLabel_" + rowIndex,
            parent,
            labelText,
            24,
            UIPalette.TextDim,
            TextAnchor.MiddleLeft
        );
        SetAnchored(label.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-190f, -70f - rowIndex * 52f), new Vector2(160f, 36f));

        Text value = UIFactory.CreateText(
            "TxtDataValue_" + rowIndex,
            parent,
            string.Empty,
            24,
            UIPalette.TextMain,
            TextAnchor.MiddleLeft
        );
        SetAnchored(value.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-20f, -70f - rowIndex * 52f), new Vector2(280f, 36f));
        return value;
    }

    private void BuildPlanningSubPanel()
    {
        subPlanning = CreateSubPanel("SubEcologyPlanning");

        Text title = UIFactory.CreateText("TxtPlanningTitle", subPlanning.transform, "规划投放范围", 48, UIPalette.TextMain);
        SetAnchored(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(900f, 70f));
        MiniGameVisuals.AddStepRail(subPlanning.transform, MiniGameThemeId.Ecology, new[] { "分析地块", "规划范围", "卫星投放", "检测报告" }, 1);

        // 目标地块板面（可拖拽框选）
        GameObject board = new GameObject("BoardZone", typeof(RectTransform), typeof(Image), typeof(DragSelectionArea));
        board.transform.SetParent(subPlanning.transform, false);
        planningBoardImage = board.GetComponent<Image>();
        planningBoardImage.color = MiniGameVisuals.Theme(MiniGameThemeId.Ecology).cardSoft;
        MiniGameVisuals.Round(planningBoardImage);
        SetAnchored(board.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(1000f, 420f));

        planningBoardLabel = UIFactory.CreateText("TxtBoardLabel", board.transform, string.Empty, 28, UIPalette.TextMain);
        SetAnchored(planningBoardLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(800f, 40f));

        // 半透明选择框
        Image box = UIFactory.CreatePanel("SelectionBox", board.transform, new Color(UIPalette.Accent.r, UIPalette.Accent.g, UIPalette.Accent.b, 0.35f));
        box.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        box.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        box.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        box.gameObject.SetActive(false);

        dragArea = board.GetComponent<DragSelectionArea>();
        dragArea.selectionBox = box.rectTransform;
        dragArea.minArea = 1000f * 420f * EcologyNutrientConfig.MinSelectionRatio;
        dragArea.SelectionFinished += OnSelectionFinished;

        Text hint = UIFactory.CreateText("TxtPlanningHint", subPlanning.transform, "按住鼠标在地块范围内拖动，框选投放区域", 24, UIPalette.TextDim);
        SetAnchored(hint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -640f), new Vector2(900f, 36f));

        planningStatusText = UIFactory.CreateText("TxtPlanningStatus", subPlanning.transform, "", 26, UIPalette.Warn);
        SetAnchored(planningStatusText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 220f), new Vector2(1000f, 44f));

        Button replan = UIFactory.CreateButton("BtnReplan", subPlanning.transform, "重新规划", new Vector2(280f, 72f), UIPalette.PanelLight, 30);
        SetAnchored(replan.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 140f), new Vector2(280f, 72f));
        replan.onClick.AddListener(OnReplanClicked);

        confirmAreaButton = UIFactory.CreateButton("BtnConfirmArea", subPlanning.transform, "确认区域", new Vector2(280f, 72f), UIPalette.Ok, 30);
        SetAnchored(confirmAreaButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(280f, 72f));
        confirmAreaButton.onClick.AddListener(OnConfirmAreaClicked);
        confirmAreaButton.gameObject.SetActive(false);
    }

    private void BuildDeliveringSubPanel()
    {
        subDelivering = CreateSubPanel("SubEcologyDelivering");

        Text title = UIFactory.CreateText("TxtDeliveringTitle", subDelivering.transform, "卫星投放阵列", 48, UIPalette.TextMain);
        SetAnchored(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(900f, 70f));
        MiniGameVisuals.AddStepRail(subDelivering.transform, MiniGameThemeId.Ecology, new[] { "分析地块", "规划范围", "卫星投放", "检测报告" }, 2);

        GameObject slot = MiniGameVisuals.CreateArtSlot(
            "MediaSlot_SatelliteDrop",
            subDelivering.transform,
            new Vector2(900f, 420f),
            MiniGameThemeId.Ecology,
            "轨道营养液投放实况"
        );
        SetAnchored(slot.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -180f), new Vector2(900f, 420f));

        // 白雾覆盖层（投放动画的一部分，正式版可替换为粒子/视频）
        fogOverlay = UIFactory.CreatePanel("FogOverlay", slot.transform, new Color(1f, 1f, 1f, 0f));
        fogOverlay.raycastTarget = false;
        UIFactory.Stretch(fogOverlay.rectTransform);

        // 扫描线
        Image line = UIFactory.CreatePanel("ScanLine", slot.transform, new Color(UIPalette.Accent.r, UIPalette.Accent.g, UIPalette.Accent.b, 0.8f));
        line.rectTransform.anchorMin = new Vector2(0f, 1f);
        line.rectTransform.anchorMax = new Vector2(1f, 1f);
        line.rectTransform.pivot = new Vector2(0.5f, 1f);
        line.rectTransform.sizeDelta = new Vector2(0f, 4f);
        scanLine = line.rectTransform;

        deliveryStatusText = UIFactory.CreateText("TxtDeliveryStatus", subDelivering.transform, "", 30, UIPalette.Accent);
        SetAnchored(deliveryStatusText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -640f), new Vector2(1000f, 50f));

        launchButton = UIFactory.CreateButton("BtnLaunchSatellite", subDelivering.transform, "启动卫星投放阵列", new Vector2(340f, 72f), UIPalette.AccentDim, 28);
        SetAnchored(launchButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(340f, 72f));
        launchButton.onClick.AddListener(OnLaunchClicked);

        viewReportButton = UIFactory.CreateButton("BtnViewReport", subDelivering.transform, "查看土壤检测报告", new Vector2(340f, 72f), UIPalette.Ok, 28);
        SetAnchored(viewReportButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(340f, 72f));
        viewReportButton.onClick.AddListener(OnViewReportClicked);
        viewReportButton.gameObject.SetActive(false);
    }

    private void BuildReportSubPanel()
    {
        subReport = CreateSubPanel("SubEcologyReport");

        Text title = UIFactory.CreateText("TxtReportTitle", subReport.transform, "土壤检测报告", 48, UIPalette.TextMain);
        SetAnchored(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(900f, 70f));
        MiniGameVisuals.AddStepRail(subReport.transform, MiniGameThemeId.Ecology, new[] { "分析地块", "规划范围", "卫星投放", "检测报告" }, 3);

        GameObject slot = MiniGameVisuals.CreateArtSlot(
            "MediaSlot_SoilReport",
            subReport.transform,
            new Vector2(760f, 240f),
            MiniGameThemeId.Ecology,
            "生态修复前后对照"
        );
        SetAnchored(slot.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -185f), new Vector2(760f, 240f));

        reportZoneValue = CreateReportRow("区域名称", 0);
        reportTierValue = CreateReportRow("投放浓度", 1);
        reportCoverageValue = CreateReportRow("投放覆盖率", 2);
        reportResultValue = CreateReportRow("修复结果", 3);
        reportActivityValue = CreateReportRow("土壤活性提升", 4);

        Button completeBtn = UIFactory.CreateButton("BtnCompleteEcology", subReport.transform, "完成本次任务", new Vector2(320f, 72f), UIPalette.Ok, 30);
        SetAnchored(completeBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(320f, 72f));
        completeBtn.onClick.AddListener(OnCompleteTaskClicked);
    }

    private Text CreateReportRow(string labelText, int rowIndex)
    {
        Text label = UIFactory.CreateText(
            "TxtReportLabel_" + rowIndex,
            subReport.transform,
            labelText,
            26,
            UIPalette.TextDim,
            TextAnchor.MiddleLeft
        );
        SetAnchored(label.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-350f, -445f - rowIndex * 46f), new Vector2(240f, 40f));

        Text value = UIFactory.CreateText(
            "TxtReportValue_" + rowIndex,
            subReport.transform,
            string.Empty,
            26,
            UIPalette.TextMain,
            TextAnchor.MiddleLeft
        );
        SetAnchored(value.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-100f, -445f - rowIndex * 46f), new Vector2(460f, 40f));
        return value;
    }

    // ===== 地图阶段 =====

    private void OnZoneClicked(int index)
    {
        PollutionZone zone = EcologyNutrientConfig.Zones[index];
        SelectedZoneId = zone.id;
        SelectedTierId = null;

        dataZoneValue.text = zone.displayName;
        dataPollutionValue.text = Mathf.RoundToInt(zone.pollutionIndex).ToString();
        dataActivityValue.text = Mathf.RoundToInt(zone.soilActivity) + "%";
        dataRecommendValue.text = zone.recommendation;

        mapStatusText.text = string.Empty;
        resetTierButton.gameObject.SetActive(false);
        tierPromptText.gameObject.SetActive(true);
        tierButtonsRoot.SetActive(true);
    }

    private void OnTierClicked(int index)
    {
        NutrientTier tier = EcologyNutrientConfig.Tiers[index];
        PollutionZone zone = FindZone(SelectedZoneId);
        NutrientTier recommended = EcologyNutrientConfig.GetRecommendedTier(zone.pollutionIndex);

        if (tier.id == recommended.id)
        {
            SelectedTierId = tier.id;
            mapStatusText.color = UIPalette.Ok;
            mapStatusText.text = "浓度匹配，进入投放范围规划。";
            resetTierButton.gameObject.SetActive(false);
            EnterPlanning(zone);
        }
        else
        {
            SelectedTierId = tier.id;
            MistakeCount++;
            ApplyEfficiencyPenalty();

            mapStatusText.color = UIPalette.Warn;
            mapStatusText.text = "浓度不匹配（修复进度 -" + WrongOperationPenalty + "）。错误操作不会记为成功，请重试。";
            resetTierButton.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 错误浓度反馈：选择明显错误的浓度时，通过权威进度系统扣除少量修复进度
    /// （WrongOperationPenalty，默认 1，可配置）。下限为 0。
    /// </summary>
    private void ApplyEfficiencyPenalty()
    {
        GameProgressManager progress = GameProgressManager.Instance;
        if (progress != null)
        {
            progress.DeductProgress(WrongOperationPenalty);
        }
    }

    private void OnResetTierClicked()
    {
        // 重置浓度：清空当前选择，允许重新选择污染地块与浓度档位。
        SelectedZoneId = null;
        SelectedTierId = null;
        mapStatusText.color = UIPalette.TextDim;
        mapStatusText.text = "已重置，请重新选择污染地块与浓度。";
        resetTierButton.gameObject.SetActive(false);
        tierButtonsRoot.SetActive(false);
        tierPromptText.gameObject.SetActive(false);
    }

    // ===== 规划阶段 =====

    private void EnterPlanning(PollutionZone zone)
    {
        CurrentPhase = Phase.Planning;
        planningBoardImage.color = EcologyNutrientConfig.ParseColor(zone.colorHex, 0.85f);
        planningBoardLabel.text = zone.displayName;
        dragArea.ResetSelection();
        planningStatusText.text = string.Empty;
        confirmAreaButton.gameObject.SetActive(false);
        ShowSubPanel(subPlanning);
    }

    private void OnSelectionFinished()
    {
        if (dragArea.HasValidSelection)
        {
            float boardArea = 1000f * 420f;
            float ratio = dragArea.CurrentRect.width * dragArea.CurrentRect.height / boardArea;
            coveragePercent = Mathf.RoundToInt(ratio * 100f);

            planningStatusText.color = UIPalette.Ok;
            planningStatusText.text = "投放范围已确认，覆盖率约 " + coveragePercent + "%。";
            confirmAreaButton.gameObject.SetActive(true);
        }
        else
        {
            planningStatusText.color = UIPalette.Warn;
            planningStatusText.text = "投放范围不足，请重新规划。";
            confirmAreaButton.gameObject.SetActive(false);
        }
    }

    private void OnReplanClicked()
    {
        dragArea.ResetSelection();
        planningStatusText.text = string.Empty;
        confirmAreaButton.gameObject.SetActive(false);
    }

    private void OnConfirmAreaClicked()
    {
        CurrentPhase = Phase.Delivering;
        deliveryStatusText.text = string.Empty;
        viewReportButton.gameObject.SetActive(false);
        launchButton.gameObject.SetActive(true);
        fogOverlay.color = new Color(1f, 1f, 1f, 0f);
        ShowSubPanel(subDelivering);
    }

    // ===== 投放阶段 =====

    private void OnLaunchClicked()
    {
        launchButton.gameObject.SetActive(false);
        deliveryRoutine = StartCoroutine(DeliveryRoutine());
    }

    private IEnumerator DeliveryRoutine()
    {
        float duration = EcologyNutrientConfig.DeliveryDuration;
        float elapsed = 0f;

        deliveryStatusText.text = "卫星投放启动";

        yield return new WaitForSeconds(0.8f);

        // 白雾覆盖 + 扫描线扫过
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            fogOverlay.color = new Color(1f, 1f, 1f, Mathf.Lerp(0f, 0.55f, t));
            scanLine.anchoredPosition = new Vector2(0f, -420f * t);

            yield return null;
        }

        deliveryStatusText.text = "白雾覆盖目标区域";

        yield return new WaitForSeconds(0.8f);

        fogOverlay.color = new Color(1f, 1f, 1f, 0.2f);
        deliveryStatusText.text = "投放完成";
        DeliveryDone = true;
        viewReportButton.gameObject.SetActive(true);
    }

    private void OnViewReportClicked()
    {
        CurrentPhase = Phase.Report;
        FillReport();
        ShowSubPanel(subReport);
    }

    // ===== 报告阶段 =====

    private void FillReport()
    {
        PollutionZone zone = FindZone(SelectedZoneId);
        NutrientTier tier = FindTier(SelectedTierId);

        float improved = zone.soilActivity + zone.pollutionIndex * EcologyNutrientConfig.ActivityRecoveryFactor;
        improved = Mathf.Clamp(improved, 0f, 100f);

        reportZoneValue.text = zone.displayName;
        reportTierValue.text = tier.displayName;
        reportCoverageValue.text = coveragePercent + "%";
        reportResultValue.text = "投放完成\n污染指数下降";
        reportActivityValue.text =
            Mathf.RoundToInt(zone.soilActivity) + "% → " + Mathf.RoundToInt(improved) + "%";
    }

    private void OnCompleteTaskClicked()
    {
        GameProgressManager progress = GameProgressManager.Instance;
        if (progress == null)
        {
            return;
        }

        if (progress.TryCompleteTask(EcologyNutrientConfig.TaskId, EcologyNutrientConfig.Reward))
        {
            ReportSubmitted?.Invoke(EcologyNutrientConfig.Reward);
        }
        else
        {
            mapStatusText.color = UIPalette.Warn;
            mapStatusText.text = "该任务已完成，奖励不可重复领取。";
        }
    }

    // ===== 工具 =====

    private static PollutionZone FindZone(string id)
    {
        PollutionZone[] zones = EcologyNutrientConfig.Zones;
        for (int i = 0; i < zones.Length; i++)
        {
            if (zones[i].id == id)
            {
                return zones[i];
            }
        }

        return zones[0];
    }

    private static NutrientTier FindTier(string id)
    {
        NutrientTier[] tiers = EcologyNutrientConfig.Tiers;
        for (int i = 0; i < tiers.Length; i++)
        {
            if (tiers[i].id == id)
            {
                return tiers[i];
            }
        }

        return tiers[0];
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
        subMap.SetActive(active == subMap);
        subPlanning.SetActive(active == subPlanning);
        subDelivering.SetActive(active == subDelivering);
        subReport.SetActive(active == subReport);
    }

    private void StopDeliveryRoutine()
    {
        if (deliveryRoutine != null)
        {
            StopCoroutine(deliveryRoutine);
            deliveryRoutine = null;
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
