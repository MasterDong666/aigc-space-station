using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// First-run tutorial shown before each daily task. It is intentionally
/// independent from the task controllers so teammate-owned mini-games remain
/// untouched and later tutorial art can replace this panel in one place.
/// </summary>
public class TaskTutorialUI : MonoBehaviour
{
    private Text titleText;
    private Text bodyText;
    private Button continueButton;
    private Action continueAction;
    private MiniGameId currentTask;

    public void BuildUI()
    {
        Image overlay = gameObject.AddComponent<Image>();
        overlay.color = new Color(0.005f, 0.015f, 0.03f, 0.92f);

        Image card = UIFactory.CreatePanel(
            "TutorialCard",
            transform,
            new Color(0.035f, 0.11f, 0.17f, 0.98f)
        );
        SetAnchored(
            card.rectTransform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(1040f, 650f)
        );

        Image accent = UIFactory.CreatePanel(
            "Accent",
            card.transform,
            UIPalette.Accent
        );
        accent.rectTransform.anchorMin = new Vector2(0f, 1f);
        accent.rectTransform.anchorMax = Vector2.one;
        accent.rectTransform.pivot = new Vector2(0.5f, 1f);
        accent.rectTransform.sizeDelta = new Vector2(0f, 5f);

        Text eyebrow = UIFactory.CreateText(
            "Eyebrow",
            card.transform,
            "CHENXI // 新手操作简报",
            20,
            UIPalette.Accent,
            TextAnchor.MiddleLeft
        );
        SetAnchored(
            eyebrow.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(58f, -42f),
            new Vector2(700f, 36f)
        );

        titleText = UIFactory.CreateText(
            "Title",
            card.transform,
            string.Empty,
            46,
            UIPalette.TextMain,
            TextAnchor.MiddleLeft
        );
        SetAnchored(
            titleText.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(58f, -105f),
            new Vector2(900f, 72f)
        );

        bodyText = UIFactory.CreateText(
            "Body",
            card.transform,
            string.Empty,
            28,
            UIPalette.TextDim,
            TextAnchor.UpperLeft
        );
        bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bodyText.verticalOverflow = VerticalWrapMode.Truncate;
        SetAnchored(
            bodyText.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(60f, -205f),
            new Vector2(920f, 260f)
        );

        Text note = UIFactory.CreateText(
            "Note",
            card.transform,
            "本简报仅在第一次进入该任务时显示",
            19,
            new Color(0.42f, 0.65f, 0.72f, 1f)
        );
        SetAnchored(
            note.rectTransform,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 122f),
            new Vector2(700f, 34f)
        );

        continueButton = UIFactory.CreateButton(
            "ContinueButton",
            card.transform,
            "开始任务",
            new Vector2(320f, 76f),
            UIPalette.AccentDim,
            30
        );
        SetAnchored(
            continueButton.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 42f),
            new Vector2(320f, 76f)
        );
        continueButton.onClick.AddListener(Continue);

        gameObject.SetActive(false);
    }

    public void Show(MiniGameId id, Action onContinue)
    {
        currentTask = id;
        continueAction = onContinue;
        ConfigureCopy(id);
        gameObject.SetActive(true);
    }

    private void Continue()
    {
        MVPGameSession.MarkTaskTutorialSeen(currentTask);
        gameObject.SetActive(false);
        Action action = continueAction;
        continueAction = null;
        action?.Invoke();
    }

    private void ConfigureCopy(MiniGameId id)
    {
        switch (id)
        {
            case MiniGameId.OrbitInspection:
                titleText.text = "任务 01  //  星际轨道巡检";
                bodyText.text =
                    "轨道参数会持续扫描。依次观察能量配比、粒子稳定值与输送倾角，" +
                    "在指针进入绿色标准区间时锁定参数。\n\n" +
                    "三项参数全部校准后，提交运维报告即可完成任务。";
                break;
            case MiniGameId.EcologyDeployment:
                titleText.text = "任务 02  //  生态营养液投放";
                bodyText.text =
                    "先读取地块污染数据，为目标区域选择正确的营养液浓度。" +
                    "随后拖拽框定投放范围并启动卫星阵列。\n\n" +
                    "投放结束后查看土壤报告，确认本次修复结果。";
                break;
            case MiniGameId.GeneCultivation:
                titleText.text = "任务 03  //  基因孢子播撒培育";
                bodyText.text =
                    "从基因库调取当前已授权样本，在地图中选择适宜播种区，" +
                    "再释放无人机群完成播撒。\n\n" +
                    "等待快速培育流程结束，进入太空大棚完成收获确认。";
                break;
        }
    }

    private static void SetAnchored(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 position,
        Vector2 size
    )
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = anchorMin;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
