using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 星际轨道巡检运维面板（v3 完整交互）。
/// 阶段一：异常轨道选择——识别并选中当前工作日的异常轨道。
/// 阶段二：三条自动往返的扫描指针（能量配比 / 粒子稳定值 / 输送倾角）校准，
///         玩家在指针进入绿色目标区间时点击【锁定参数】。
/// 阶段三：手动提交运维报告；或启用【全自动托管】一键完成当天（奖励效率 -15%）。
/// 彩蛋：连续 7 个“完全手动”工作日解锁前代修复官日志碎片（一次性）。
/// 所有数值读取 OrbitCalibrationConfig，不在本类散落硬编码。
/// </summary>
public class OrbitTaskController : MonoBehaviour
{
    /// <summary>点击返回按钮时触发。</summary>
    public event Action ReturnRequested;

    /// <summary>提交运维报告成功并发放奖励后触发（参数为实际奖励值）。</summary>
    public event Action<int> ReportSubmitted;

    private const float TrackWidth = 820f;
    private const float RowYStep = 168f;

    private enum Stage
    {
        SelectingOrbit,
        Calibrating,
        Done,
    }

    private sealed class ParamGauge
    {
        public CalibrationParam param;
        public RectTransform pointer;
        public Image pointerImage;
        public Text valueLabel;
        public Image zoneImage;
        public bool locked;
        public float lockedValue;
        public float startTime;
    }

    private Stage stage;
    private int abnormalIndex;
    private ParamGauge[] gauges;
    private int currentIndex;
    private bool allCalibrated;
    private Image selectionRoot;
    private Image calibrationRoot;
    private Text[] orbitOptionLabels;

    private Text progressText;
    private Text statusText;
    private Button lockButton;
    private Button submitButton;
    private Button autoButton;
    private Image logPopup;
    private Text logPopupBody;
    private bool taskResultSent;
    private Coroutine flashRoutine;

    public int CurrentParamIndex => currentIndex;
    public int CalibratedCount => currentIndex;
    public bool AllCalibrated => allCalibrated;
    public bool IsManualDayUsed => MVPGameSession.Task1ManualStreak > 0;
    public bool IsLogUnlocked => MVPGameSession.Task1LogUnlocked;

    /// <summary>读取指定参数的当前数值（锁定后返回锁定值）。供测试与调试使用。</summary>
    public float GetGaugeValue(int index)
    {
        if (index < 0 || index >= gauges.Length)
        {
            return 0f;
        }

        ParamGauge gauge = gauges[index];
        return gauge.locked ? gauge.lockedValue : ComputeValue(gauge);
    }

    public void BuildUI()
    {
        Image bg = gameObject.AddComponent<Image>();
        bg.color = UIPalette.Background;
        MiniGameVisuals.PrepareScreen(gameObject, MiniGameThemeId.Orbit);

        MiniGameTheme theme = MiniGameVisuals.Theme(MiniGameThemeId.Orbit);

        Text eyebrow = UIFactory.CreateText(
            "TxtOrbitEyebrow",
            transform,
            "DAILY RESTORATION  ·  轨道稳定协议",
            20,
            theme.accent
        );
        eyebrow.fontStyle = FontStyle.Bold;
        SetAnchored(eyebrow.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(900f, 32f));

        // 标题
        Text title = UIFactory.CreateText(
            "TxtOrbitTitle",
            transform,
            "星际轨道巡检",
            52,
            UIPalette.TextMain
        );
        SetAnchored(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(900f, 70f));

        // 返回按钮（左上角）
        Button back = UIFactory.CreateButton(
            "BtnBackOrbit",
            transform,
            "返回",
            new Vector2(140f, 56f),
            UIPalette.PanelLight,
            24
        );
        SetAnchored(back.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -30f), new Vector2(140f, 56f));
        back.onClick.AddListener(() => ReturnRequested?.Invoke());

        // 校准进度
        progressText = UIFactory.CreateText(
            "TxtCalibProgress",
            transform,
            "校准进度：0 / 3",
            24,
            theme.accentWarm
        );
        progressText.fontStyle = FontStyle.Bold;
        SetAnchored(progressText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-245f, -54f), new Vector2(360f, 42f));

        Image consoleCard = MiniGameVisuals.CreateCard(
            "OrbitConsoleCard",
            transform,
            new Vector2(1120f, 610f),
            MiniGameThemeId.Orbit
        );
        SetAnchored(consoleCard.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -155f), new Vector2(1120f, 610f));

        BuildSelectionRoot(consoleCard, theme);
        BuildCalibrationRoot(consoleCard, theme);

        // 状态文本
        statusText = UIFactory.CreateText("TxtStatus", transform, "", 30, UIPalette.Warn);
        SetAnchored(statusText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 250f), new Vector2(1000f, 44f));

        // 按钮
        lockButton = UIFactory.CreateButton(
            "BtnLockParam",
            transform,
            "锁定参数",
            new Vector2(280f, 72f),
            UIPalette.AccentDim,
            30
        );
        SetAnchored(lockButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(280f, 72f));
        lockButton.onClick.AddListener(OnLockClicked);

        // 全自动托管（新入口，置于锁定按钮左侧，不重排现有按钮）
        autoButton = UIFactory.CreateButton(
            "BtnAutoHost",
            transform,
            "全自动托管",
            new Vector2(280f, 72f),
            UIPalette.PanelLight,
            30
        );
        SetAnchored(autoButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-320f, 150f), new Vector2(280f, 72f));
        autoButton.onClick.AddListener(() => CompleteTask(autonomous: true));

        submitButton = UIFactory.CreateButton(
            "BtnSubmit",
            transform,
            "提交运维报告",
            new Vector2(320f, 72f),
            UIPalette.Ok,
            30
        );
        SetAnchored(submitButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(320f, 72f));
        submitButton.onClick.AddListener(() => CompleteTask(autonomous: false));
        submitButton.gameObject.SetActive(false);

        BuildLogPopup();

        MiniGameVisuals.PolishHierarchy(transform, MiniGameThemeId.Orbit);
        MiniGameVisuals.AddEntrance(consoleCard.gameObject);

        gameObject.SetActive(false);
    }

    private void BuildSelectionRoot(Image consoleCard, MiniGameTheme theme)
    {
        selectionRoot = UIFactory.CreatePanel("SelectionRoot", consoleCard.transform, new Color(0f, 0f, 0f, 0f));
        StretchFill(selectionRoot.rectTransform);

        Text instruction = UIFactory.CreateText(
            "TxtSelectInstruction",
            selectionRoot.transform,
            "扫描结果显示一条轨道出现异常，请识别并选中异常轨道",
            24,
            UIPalette.TextDim
        );
        SetAnchored(instruction.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(1000f, 40f));

        string[] names = OrbitCalibrationConfig.OrbitNames;
        orbitOptionLabels = new Text[names.Length];
        float startY = -140f;
        for (int i = 0; i < names.Length; i++)
        {
            int index = i;
            Button option = UIFactory.CreateButton(
                "OrbitOption_" + i,
                selectionRoot.transform,
                "",
                new Vector2(760f, 88f),
                UIPalette.PanelLight,
                28
            );
            SetAnchored(option.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, startY - i * 120f), new Vector2(760f, 88f));

            Text label = option.GetComponentInChildren<Text>();
            if (label == null)
            {
                label = UIFactory.CreateText("OrbitLabel", option.transform, "", 28, UIPalette.TextMain, TextAnchor.MiddleLeft);
                UIFactory.Stretch(label.rectTransform);
                label.rectTransform.offsetMin = new Vector2(40f, 0f);
                label.rectTransform.offsetMax = new Vector2(-40f, 0f);
            }

            orbitOptionLabels[i] = label;
            option.onClick.AddListener(() => OnSelectOrbit(index));
        }
    }

    private void BuildCalibrationRoot(Image consoleCard, MiniGameTheme theme)
    {
        calibrationRoot = UIFactory.CreatePanel("CalibrationRoot", consoleCard.transform, new Color(0f, 0f, 0f, 0f));
        StretchFill(calibrationRoot.rectTransform);

        Text instruction = UIFactory.CreateText(
            "TxtCalibInstruction",
            calibrationRoot.transform,
            "观察扫描指针，在它进入发光稳定区时点击锁定参数",
            23,
            UIPalette.TextDim
        );
        SetAnchored(instruction.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(1000f, 36f));

        CalibrationParam[] config = OrbitCalibrationConfig.Params;
        gauges = new ParamGauge[config.Length];

        for (int i = 0; i < config.Length; i++)
        {
            CalibrationParam param = config[i];
            float rowY = -72f - i * RowYStep;

            Image rowCard = MiniGameVisuals.CreateCard(
                "GaugeCard_" + i,
                calibrationRoot.transform,
                new Vector2(1020f, 142f),
                MiniGameThemeId.Orbit,
                true
            );
            SetAnchored(rowCard.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, rowY), new Vector2(1020f, 142f));

            Text indexBadge = UIFactory.CreateText(
                "TxtIndex_" + i,
                rowCard.transform,
                "0" + (i + 1),
                20,
                theme.accentWarm
            );
            indexBadge.fontStyle = FontStyle.Bold;
            SetAnchored(indexBadge.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -17f), new Vector2(54f, 34f));

            Text nameText = UIFactory.CreateText(
                "TxtName_" + param.displayName,
                rowCard.transform,
                param.displayName,
                28,
                UIPalette.TextMain,
                TextAnchor.MiddleLeft
            );
            nameText.fontStyle = FontStyle.Bold;
            SetAnchored(nameText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(82f, -14f), new Vector2(320f, 42f));

            Text valueLabel = UIFactory.CreateText(
                "TxtValue_" + param.displayName,
                rowCard.transform,
                "0",
                32,
                theme.accent,
                TextAnchor.MiddleRight
            );
            valueLabel.fontStyle = FontStyle.Bold;
            SetAnchored(valueLabel.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-178f, -14f), new Vector2(150f, 42f));

            Image track = UIFactory.CreatePanel(
                "Track_" + param.displayName,
                rowCard.transform,
                new Color(0.02f, 0.07f, 0.12f, 0.95f)
            );
            SetAnchored(track.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -73f), new Vector2(TrackWidth, 32f));
            track.raycastTarget = false;

            // 绿色目标区间。
            Image zone = UIFactory.CreatePanel("Zone_" + param.displayName, track.transform, ZoneColor(locked: false));
            zone.rectTransform.anchorMin = new Vector2(param.minValue / 100f, 0f);
            zone.rectTransform.anchorMax = new Vector2(param.maxValue / 100f, 1f);
            zone.rectTransform.offsetMin = new Vector2(0f, 4f);
            zone.rectTransform.offsetMax = new Vector2(0f, -4f);
            zone.raycastTarget = false;

            // 白色竖杠自动在整条轨道上来回扫描，不能拖动。
            Image pointer = UIFactory.CreatePanel("Pointer_" + param.displayName, track.transform, UIPalette.TextMain);
            pointer.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            pointer.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            pointer.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            pointer.rectTransform.sizeDelta = new Vector2(10f, 48f);
            pointer.raycastTarget = false;
            MiniGameVisuals.MakeCircle(pointer);

            Text rangeLabel = UIFactory.CreateText(
                "TxtRange_" + param.displayName,
                rowCard.transform,
                "稳定区  " + param.RangeText,
                20,
                UIPalette.TextDim
            );
            SetAnchored(rangeLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(400f, 28f));

            gauges[i] = new ParamGauge
            {
                param = param,
                pointer = pointer.rectTransform,
                pointerImage = pointer,
                valueLabel = valueLabel,
                zoneImage = zone,
            };
        }
    }

    private void BuildLogPopup()
    {
        logPopup = UIFactory.CreatePanel("LogPopup", transform, new Color(0f, 0f, 0f, 0.72f));
        StretchFill(logPopup.rectTransform);

        Image card = MiniGameVisuals.CreateCard(
            "LogCard",
            logPopup.transform,
            new Vector2(900f, 460f),
            MiniGameThemeId.Orbit
        );
        SetAnchored(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(900f, 460f));

        Text eyebrow = UIFactory.CreateText(
            "LogEyebrow",
            card.transform,
            "彩蛋  ·  前代修复官日志碎片",
            20,
            UIPalette.Warn
        );
        eyebrow.fontStyle = FontStyle.Bold;
        SetAnchored(eyebrow.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(800f, 34f));

        logPopupBody = UIFactory.CreateText(
            "LogBody",
            card.transform,
            "",
            26,
            UIPalette.TextMain,
            TextAnchor.MiddleLeft
        );
        logPopupBody.horizontalOverflow = HorizontalWrapMode.Wrap;
        SetAnchored(logPopupBody.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(780f, 260f));

        Button confirm = UIFactory.CreateButton(
            "LogConfirm",
            card.transform,
            "收下",
            new Vector2(240f, 72f),
            UIPalette.Ok,
            28
        );
        SetAnchored(confirm.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(240f, 72f));
        confirm.onClick.AddListener(() => logPopup.gameObject.SetActive(false));

        logPopup.gameObject.SetActive(false);
    }

    public void Show()
    {
        stage = Stage.SelectingOrbit;
        abnormalIndex = OrbitCalibrationConfig.GetAbnormalOrbitIndex();
        currentIndex = 0;
        allCalibrated = false;
        taskResultSent = false;

        RefreshOrbitOptions();
        selectionRoot.gameObject.SetActive(true);
        calibrationRoot.gameObject.SetActive(false);
        lockButton.gameObject.SetActive(false);
        autoButton.gameObject.SetActive(true);
        submitButton.gameObject.SetActive(false);
        logPopup.gameObject.SetActive(false);

        progressText.text = "校准进度：0 / 3";
        statusText.color = UIPalette.TextDim;
        statusText.text = "请识别并选中异常轨道";

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        StopFlash();
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (stage != Stage.Calibrating || allCalibrated || gauges == null || currentIndex >= gauges.Length)
        {
            return;
        }

        ParamGauge gauge = gauges[currentIndex];
        float value = ComputeValue(gauge);
        gauge.pointer.anchoredPosition = new Vector2(value / 100f * TrackWidth, 0f);
        gauge.valueLabel.text = Mathf.RoundToInt(value).ToString();
    }

    private static float ComputeValue(ParamGauge gauge)
    {
        return Mathf.PingPong((Time.time - gauge.startTime) * gauge.param.scanSpeed, 100f);
    }

    private void RefreshOrbitOptions()
    {
        if (orbitOptionLabels == null)
        {
            return;
        }

        string[] names = OrbitCalibrationConfig.OrbitNames;
        for (int i = 0; i < orbitOptionLabels.Length && i < names.Length; i++)
        {
            bool abnormal = i == abnormalIndex;
            orbitOptionLabels[i].text = (abnormal ? "⚠  异常  ·  " : "        ") + names[i];
            orbitOptionLabels[i].color = abnormal ? UIPalette.Warn : UIPalette.TextMain;
        }
    }

    private void OnSelectOrbit(int index)
    {
        if (stage != Stage.SelectingOrbit)
        {
            return;
        }

        if (index != abnormalIndex)
        {
            statusText.color = UIPalette.Warn;
            statusText.text = "该轨道读数正常，请重新识别异常轨道。";
            return;
        }

        stage = Stage.Calibrating;
        selectionRoot.gameObject.SetActive(false);
        calibrationRoot.gameObject.SetActive(true);
        currentIndex = 0;
        allCalibrated = false;

        for (int i = 0; i < gauges.Length; i++)
        {
            ParamGauge gauge = gauges[i];
            gauge.locked = false;
            gauge.startTime = Time.time;
            gauge.valueLabel.text = "0";
            gauge.zoneImage.color = ZoneColor(false);
            gauge.pointerImage.color = UIPalette.TextMain;
            gauge.pointer.anchoredPosition = Vector2.zero;
        }

        lockButton.gameObject.SetActive(true);
        submitButton.gameObject.SetActive(false);

        progressText.text = "校准进度：0 / 3";
        statusText.color = UIPalette.Ok;
        statusText.text = "已锁定异常轨道：" + OrbitCalibrationConfig.OrbitNames[abnormalIndex] + "。开始校准参数";
    }

    private void OnLockClicked()
    {
        if (stage != Stage.Calibrating)
        {
            return;
        }

        ParamGauge gauge = gauges[currentIndex];
        float value = ComputeValue(gauge);

        if (gauge.param.IsInRange(value))
        {
            gauge.locked = true;
            gauge.lockedValue = value;
            gauge.valueLabel.text = Mathf.RoundToInt(value) + "  ✓";
            gauge.zoneImage.color = ZoneColor(true);

            statusText.color = UIPalette.Ok;
            statusText.text = gauge.param.displayName + " 校准成功";

            currentIndex++;

            if (currentIndex >= gauges.Length)
            {
                allCalibrated = true;
                progressText.text = "校准进度：3 / 3";
                statusText.text = "轨道校准完成，可提交运维报告。";
                lockButton.gameObject.SetActive(false);
                submitButton.gameObject.SetActive(true);
            }
            else
            {
                gauges[currentIndex].startTime = Time.time;
                progressText.text = "校准进度：" + currentIndex + " / 3";
            }
        }
        else
        {
            statusText.color = UIPalette.Warn;
            statusText.text = "未捕捉到稳定区间，观察白色扫描杠后再次点击。";
            StopFlash();
            flashRoutine = StartCoroutine(FlashPointer(gauge));
        }
    }

    private IEnumerator FlashPointer(ParamGauge gauge)
    {
        gauge.pointerImage.color = UIPalette.Warn;
        yield return new WaitForSeconds(0.45f);

        if (!gauge.locked)
        {
            gauge.pointerImage.color = UIPalette.TextMain;
        }

        flashRoutine = null;
    }

    private void StopFlash()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }
    }

    /// <summary>
    /// 完成任务。手动方式计入连续手动天数并可能触发日志彩蛋；
    /// 全自动托管当天奖励 -15%，且不计入手动连击。
    /// </summary>
    private void CompleteTask(bool autonomous)
    {
        if (taskResultSent)
        {
            return;
        }

        GameProgressManager progress = GameProgressManager.Instance;
        if (progress == null)
        {
            return;
        }

        int reward;
        if (autonomous)
        {
            MVPGameSession.RegisterTask1AutonomousCompletion();
            reward = OrbitCalibrationConfig.GetAutonomousReward();
        }
        else
        {
            MVPGameSession.RegisterTask1ManualCompletion();
            reward = OrbitCalibrationConfig.Reward;
            if (MVPGameSession.TryUnlockTask1Log())
            {
                ShowLogPopup();
            }
        }

        if (progress.TryCompleteTask(OrbitCalibrationConfig.TaskId, reward))
        {
            taskResultSent = true;
            stage = Stage.Done;
            lockButton.gameObject.SetActive(false);
            autoButton.gameObject.SetActive(false);
            submitButton.gameObject.SetActive(false);

            statusText.color = UIPalette.Ok;
            statusText.text = autonomous
                ? "已全自动托管完成。奖励效率 -15%（+" + reward + " 点）"
                : "运维报告已提交（+" + reward + " 点）。";

            ReportSubmitted?.Invoke(reward);
        }
        else
        {
            statusText.color = UIPalette.Warn;
            statusText.text = "该任务已完成，奖励不可重复领取。";
        }
    }

    private void ShowLogPopup()
    {
        logPopupBody.text =
            "—— 前代修复官日志 · 第 447 天 ——\n\n" +
            "连续七个工作日，没有一天使用自动托管。\n" +
            "你坚持亲手推动每一次校准。\n\n" +
            "……或许，机器的效率从来不是终点。\n" +
            "真正的修复，是从人愿意留下来开始的。\n";
        logPopup.gameObject.SetActive(true);
    }

    private static Color ZoneColor(bool locked)
    {
        Color ok = UIPalette.Ok;
        return new Color(ok.r, ok.g, ok.b, locked ? 0.85f : 0.40f);
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
}
