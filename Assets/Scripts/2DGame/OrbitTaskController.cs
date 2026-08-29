using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 星际轨道巡检运维面板（v2）：自动移动指针 + 时机点击判定。
/// 每个参数一条 0~100 轨道，指针自动 0→100→0 循环（PingPong），
/// 玩家在绿色目标区间内点击【锁定参数】即可锁定当前参数并进入下一项。
/// 三个参数全部锁定后出现【提交运维报告】。
/// 所有数值（范围/速度/奖励）读取 OrbitCalibrationConfig，不在本类散落硬编码。
/// </summary>
public class OrbitTaskController : MonoBehaviour
{
    /// <summary>点击返回按钮时触发。</summary>
    public event Action ReturnRequested;

    /// <summary>提交运维报告成功并发放奖励后触发（参数为奖励值）。</summary>
    public event Action<int> ReportSubmitted;

    private const float TrackWidth = 820f;

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

    private ParamGauge[] gauges;
    private int currentIndex;
    private bool allCalibrated;

    private Text progressText;
    private Text statusText;
    private Button lockButton;
    private Button submitButton;
    private Coroutine flashRoutine;

    public int CurrentParamIndex => currentIndex;
    public int CalibratedCount => currentIndex;
    public bool AllCalibrated => allCalibrated;

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

        Text instruction = UIFactory.CreateText(
            "TxtOrbitInstruction",
            consoleCard.transform,
            "观察扫描指针，在它进入发光稳定区时锁定读数",
            23,
            UIPalette.TextDim
        );
        SetAnchored(instruction.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(900f, 36f));

        // 三条参数轨道（自动指针）
        CalibrationParam[] config = OrbitCalibrationConfig.Params;
        gauges = new ParamGauge[config.Length];

        for (int i = 0; i < config.Length; i++)
        {
            CalibrationParam param = config[i];
            float rowY = -72f - i * 168f;

            Image rowCard = MiniGameVisuals.CreateCard(
                "GaugeCard_" + i,
                consoleCard.transform,
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

            // 深色轨道背景
            Image track = UIFactory.CreatePanel("Track_" + param.displayName, rowCard.transform, new Color(0.02f, 0.07f, 0.12f, 0.95f));
            SetAnchored(track.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -73f), new Vector2(TrackWidth, 32f));

            // 绿色目标区间（按 min/max 比例铺在轨道上）
            Image zone = UIFactory.CreatePanel("Zone_" + param.displayName, track.transform, ZoneColor(locked: false));
            zone.rectTransform.anchorMin = new Vector2(param.minValue / 100f, 0f);
            zone.rectTransform.anchorMax = new Vector2(param.maxValue / 100f, 1f);
            zone.rectTransform.offsetMin = new Vector2(0f, 4f);
            zone.rectTransform.offsetMax = new Vector2(0f, -4f);

            // 自动移动指针（白色竖线，玩家不可拖动）
            Image pointer = UIFactory.CreatePanel("Pointer_" + param.displayName, track.transform, UIPalette.TextMain);
            pointer.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            pointer.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            pointer.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            pointer.rectTransform.sizeDelta = new Vector2(10f, 48f);
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

        submitButton = UIFactory.CreateButton(
            "BtnSubmit",
            transform,
            "提交运维报告",
            new Vector2(320f, 72f),
            UIPalette.Ok,
            30
        );
        SetAnchored(submitButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(320f, 72f));
        submitButton.onClick.AddListener(OnSubmitClicked);
        submitButton.gameObject.SetActive(false);

        MiniGameVisuals.PolishHierarchy(transform, MiniGameThemeId.Orbit);
        MiniGameVisuals.AddEntrance(consoleCard.gameObject);

        gameObject.SetActive(false);
    }

    public void Show()
    {
        // 重置校准状态（任务未完成时每次进入都从第一项开始）
        currentIndex = 0;
        allCalibrated = false;

        CalibrationParam[] config = OrbitCalibrationConfig.Params;
        for (int i = 0; i < gauges.Length; i++)
        {
            ParamGauge gauge = gauges[i];
            gauge.locked = false;
            gauge.startTime = Time.time;
            gauge.valueLabel.text = "0";
            gauge.zoneImage.color = ZoneColor(false);
            gauge.pointerImage.color = UIPalette.TextMain;
            gauge.pointer.anchoredPosition = new Vector2(0f, 0f);
        }

        progressText.text = "校准进度：0 / 3";
        statusText.text = string.Empty;
        lockButton.gameObject.SetActive(true);
        submitButton.gameObject.SetActive(false);

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        StopFlash();
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (allCalibrated || currentIndex >= gauges.Length)
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

    private void OnLockClicked()
    {
        ParamGauge gauge = gauges[currentIndex];
        float value = ComputeValue(gauge);

        if (gauge.param.IsInRange(value))
        {
            // 成功：锁定当前参数
            gauge.locked = true;
            gauge.lockedValue = value;
            gauge.valueLabel.text = Mathf.RoundToInt(value) + "  ✓";
            gauge.zoneImage.color = ZoneColor(true);

            statusText.color = UIPalette.Ok;
            statusText.text = gauge.param.displayName + "校准成功";

            currentIndex++;

            if (currentIndex >= gauges.Length)
            {
                allCalibrated = true;
                progressText.text = "校准进度：3 / 3";
                statusText.text = "轨道校准完成。";
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
            // 失败：不重置任务，当前参数继续移动，可反复尝试
            statusText.color = UIPalette.Warn;
            statusText.text = "校准失败，请重新捕捉稳定区间。";
            StopFlash();
            flashRoutine = StartCoroutine(FlashPointer(gauge));
        }
    }

    private IEnumerator FlashPointer(ParamGauge gauge)
    {
        gauge.pointerImage.color = UIPalette.Warn;
        yield return new WaitForSeconds(0.5f);

        if (!gauge.locked)
        {
            gauge.pointerImage.color = UIPalette.TextMain;
        }

        flashRoutine = null;
    }

    private void OnSubmitClicked()
    {
        GameProgressManager progress = GameProgressManager.Instance;
        if (progress == null)
        {
            return;
        }

        if (progress.TryCompleteTask(OrbitCalibrationConfig.TaskId, OrbitCalibrationConfig.Reward))
        {
            ReportSubmitted?.Invoke(OrbitCalibrationConfig.Reward);
        }
        else
        {
            statusText.color = UIPalette.Warn;
            statusText.text = "该任务已完成，奖励不可重复领取。";
        }
    }

    private void StopFlash()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }
    }

    private static Color ZoneColor(bool locked)
    {
        Color ok = UIPalette.Ok;
        return new Color(ok.r, ok.g, ok.b, locked ? 0.85f : 0.45f);
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
