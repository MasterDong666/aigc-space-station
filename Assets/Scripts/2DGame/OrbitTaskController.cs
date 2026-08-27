using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 星际轨道巡检运维面板：三个校准 Slider + 校验 + 提交运维报告。
/// 所有参数范围读取 OrbitCalibrationConfig，不在本类散落硬编码。
/// </summary>
public class OrbitTaskController : MonoBehaviour
{
    /// <summary>点击返回按钮时触发。</summary>
    public event Action ReturnRequested;

    /// <summary>提交运维报告成功并发放奖励后触发（参数为奖励值）。</summary>
    public event Action<int> ReportSubmitted;

    private Slider[] sliders;
    private Text[] valueLabels;
    private Text statusText;
    private Button calibrateButton;
    private Button submitButton;

    public void BuildUI()
    {
        Image bg = gameObject.AddComponent<Image>();
        bg.color = UIPalette.Background;

        // 标题
        Text title = UIFactory.CreateText(
            "TxtOrbitTitle",
            transform,
            "星际轨道巡检运维",
            48,
            UIPalette.TextMain
        );
        SetAnchored(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(900f, 70f));

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

        // 三行校准参数（集中配置）
        CalibrationParam[] config = OrbitCalibrationConfig.Params;
        sliders = new Slider[config.Length];
        valueLabels = new Text[config.Length];

        for (int i = 0; i < config.Length; i++)
        {
            CalibrationParam param = config[i];
            float rowY = -190f - i * 175f;

            Text nameText = UIFactory.CreateText(
                "TxtName_" + param.displayName,
                transform,
                param.displayName,
                32,
                UIPalette.TextMain,
                TextAnchor.MiddleLeft
            );
            SetAnchored(nameText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-380f, rowY), new Vector2(320f, 44f));

            Text valueLabel = UIFactory.CreateText(
                "TxtValue_" + param.displayName,
                transform,
                "50",
                32,
                UIPalette.Accent,
                TextAnchor.MiddleRight
            );
            SetAnchored(valueLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(380f, rowY), new Vector2(160f, 44f));
            valueLabels[i] = valueLabel;

            Slider slider = UIFactory.CreateSlider(
                "Slider_" + param.displayName,
                transform,
                new Vector2(820f, 28f)
            );
            SetAnchored(slider.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, rowY - 50f), new Vector2(820f, 28f));

            Text rangeLabel = UIFactory.CreateText(
                "TxtRange_" + param.displayName,
                transform,
                param.RangeText,
                22,
                UIPalette.TextDim
            );
            SetAnchored(rangeLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, rowY - 88f), new Vector2(400f, 30f));

            int index = i;
            slider.onValueChanged.AddListener(v => UpdateValueLabel(index, v));
            sliders[i] = slider;
            UpdateValueLabel(i, slider.value);
        }

        // 状态文本
        statusText = UIFactory.CreateText("TxtStatus", transform, "", 30, UIPalette.Warn);
        SetAnchored(statusText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 230f), new Vector2(1000f, 44f));

        // 按钮
        calibrateButton = UIFactory.CreateButton(
            "BtnCalibrate",
            transform,
            "校准确认",
            new Vector2(280f, 72f),
            UIPalette.AccentDim,
            30
        );
        SetAnchored(calibrateButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(280f, 72f));
        calibrateButton.onClick.AddListener(OnCalibrateClicked);

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

        // 任务面板默认隐藏，由主界面点击任务后打开
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

    private void UpdateValueLabel(int index, float value)
    {
        valueLabels[index].text = Mathf.RoundToInt(value).ToString();
    }

    private void OnCalibrateClicked()
    {
        if (AreAllParamsValid())
        {
            statusText.color = UIPalette.Ok;
            statusText.text = "轨道校准完成。";
            submitButton.gameObject.SetActive(true);
        }
        else
        {
            statusText.color = UIPalette.Warn;
            statusText.text = "参数仍存在异常，请继续校准。";
            submitButton.gameObject.SetActive(false);
        }
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

    private bool AreAllParamsValid()
    {
        CalibrationParam[] config = OrbitCalibrationConfig.Params;
        for (int i = 0; i < config.Length; i++)
        {
            if (!config[i].IsInRange(sliders[i].value))
            {
                return false;
            }
        }

        return true;
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
