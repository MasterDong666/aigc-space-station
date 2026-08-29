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
    private Text eyebrowText;
    private Text symbolText;
    private Image cardImage;
    private Image accentImage;
    private Image symbolBadge;
    private Button continueButton;
    private Action continueAction;
    private MiniGameId currentTask;

    public void BuildUI()
    {
        Image overlay = gameObject.AddComponent<Image>();
        overlay.color = new Color(0.005f, 0.015f, 0.03f, 0.97f);

        Image card = UIFactory.CreatePanel(
            "TutorialCard",
            transform,
            new Color(0.035f, 0.11f, 0.17f, 0.98f)
        );
        cardImage = card;
        MiniGameVisuals.Round(card);
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
        accentImage = accent;
        accent.rectTransform.anchorMin = new Vector2(0f, 1f);
        accent.rectTransform.anchorMax = Vector2.one;
        accent.rectTransform.pivot = new Vector2(0.5f, 1f);
        accent.rectTransform.sizeDelta = new Vector2(0f, 5f);

        Text eyebrow = UIFactory.CreateText(
            "Eyebrow",
            card.transform,
            "晨曦任务简报  ·  FIRST RUN",
            20,
            UIPalette.Accent,
            TextAnchor.MiddleLeft
        );
        eyebrowText = eyebrow;
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

        symbolBadge = UIFactory.CreatePanel("TaskSymbolBadge", card.transform, UIPalette.AccentDim);
        MiniGameVisuals.MakeCircle(symbolBadge);
        SetAnchored(
            symbolBadge.rectTransform,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-116f, -50f),
            new Vector2(74f, 74f)
        );

        symbolText = UIFactory.CreateText("TaskSymbol", symbolBadge.transform, "◎", 38, Color.white);
        UIFactory.Stretch(symbolText.rectTransform);
        symbolText.fontStyle = FontStyle.Bold;

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

        MiniGameVisuals.PolishHierarchy(transform, MiniGameThemeId.Orbit);
        MiniGameVisuals.AddEntrance(card.gameObject);

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
        MiniGameThemeId themeId = MiniGameThemeId.Orbit;
        switch (id)
        {
            case MiniGameId.OrbitInspection:
                themeId = MiniGameThemeId.Orbit;
                eyebrowText.text = "晨曦任务简报  ·  ORBIT 01";
                symbolText.text = "◎";
                titleText.text = "星际轨道巡检";
                bodyText.text =
                    "01   观察三项轨道参数的实时扫描\n\n" +
                    "02   指针进入绿色稳定区时锁定读数\n\n" +
                    "03   三项校准完成后提交运维报告";
                break;
            case MiniGameId.EcologyDeployment:
                themeId = MiniGameThemeId.Ecology;
                eyebrowText.text = "晨曦任务简报  ·  ECOLOGY 02";
                symbolText.text = "✦";
                titleText.text = "生态营养液投放";
                bodyText.text =
                    "01   读取污染数据并匹配营养液浓度\n\n" +
                    "02   在地块中拖拽规划投放范围\n\n" +
                    "03   启动卫星阵列并确认土壤报告";
                break;
            case MiniGameId.GeneCultivation:
                themeId = MiniGameThemeId.Gene;
                eyebrowText.text = "晨曦任务简报  ·  GENE 03";
                symbolText.text = "❈";
                titleText.text = "基因孢子播撒培育";
                bodyText.text =
                    "01   从诺亚基因库调取已授权样本\n\n" +
                    "02   选择适宜区域并释放无人机群\n\n" +
                    "03   完成快速培育与太空大棚收获";
                break;
        }

        MiniGameTheme theme = MiniGameVisuals.Theme(themeId);
        cardImage.color = theme.card;
        accentImage.color = theme.accent;
        eyebrowText.color = theme.accent;
        symbolBadge.color = theme.accent;
        Image buttonImage = continueButton.GetComponent<Image>();
        buttonImage.color = Color.Lerp(theme.ink, theme.accent, 0.55f);
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
