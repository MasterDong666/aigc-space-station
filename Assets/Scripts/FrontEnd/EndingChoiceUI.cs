using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Final chapter shown after restoration reaches 50. It keeps the two endings
/// in one UI-driven sequence so the MVP does not require another 3D scene.
/// </summary>
public class EndingChoiceUI : MonoBehaviour
{
    private GameObject preludeRoot;
    private GameObject choiceRoot;
    private GameObject resultRoot;
    private RawImage resultImage;
    private Text resultEyebrow;
    private Text resultTitle;
    private Text resultBody;
    private Texture2D noahEndingTexture;
    private Texture2D sacrificeEndingTexture;
    private Action returnToMenuAction;

    public void BuildUI()
    {
        noahEndingTexture = Resources.Load<Texture2D>("Story/EndingNoah");
        sacrificeEndingTexture = Resources.Load<Texture2D>(
            "Story/EndingSacrifice"
        );

        BuildPrelude();
        BuildChoice();
        BuildResult();
        CinematicUIVisuals.PolishHierarchy(
            transform,
            CinematicUIVisuals.Peach,
            CinematicUIVisuals.Sun
        );
        gameObject.SetActive(false);
    }

    public void Show(Action onReturnToMenu)
    {
        returnToMenuAction = onReturnToMenu;
        gameObject.SetActive(true);

        if (
            MVPGameSession.EndingCompleted &&
            MVPGameSession.EndingChoice != FinalEndingChoice.None
        )
        {
            ShowEndingResult(MVPGameSession.EndingChoice, false);
            return;
        }

        ShowOnly(preludeRoot);
    }

    private void BuildPrelude()
    {
        preludeRoot = CreateFullPanel("FinalPrelude", transform);
        RawImage background = CreateRawImage(
            "EarthRecovery",
            preludeRoot.transform,
            sacrificeEndingTexture,
            new Color(0.72f, 0.82f, 0.86f, 1f)
        );
        UIFactory.Stretch(background.rectTransform);

        Image veil = UIFactory.CreatePanel(
            "Veil",
            preludeRoot.transform,
            new Color(0.006f, 0.02f, 0.035f, 0.72f)
        );
        UIFactory.Stretch(veil.rectTransform);

        Image card = UIFactory.CreatePanel(
            "PreludeCard",
            preludeRoot.transform,
            new Color(0.025f, 0.08f, 0.11f, 0.97f)
        );
        SetAnchored(
            card.rectTransform,
            new Vector2(0.08f, 0.5f),
            new Vector2(0.08f, 0.5f),
            Vector2.zero,
            new Vector2(850f, 760f)
        );

        CreateTopAccent(card.transform, new Color(1f, 0.56f, 0.24f, 1f));

        Text eyebrow = UIFactory.CreateText(
            "Eyebrow",
            card.transform,
            "CHENXI // FINAL RESTORATION PROTOCOL",
            19,
            new Color(1f, 0.72f, 0.40f, 1f),
            TextAnchor.MiddleLeft
        );
        SetAnchored(
            eyebrow.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(54f, -42f),
            new Vector2(-108f, 38f)
        );

        Text title = UIFactory.CreateText(
            "Title",
            card.transform,
            "地球正在醒来",
            58,
            Color.white,
            TextAnchor.MiddleLeft
        );
        SetAnchored(
            title.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(52f, -105f),
            new Vector2(-104f, 88f)
        );

        Text body = UIFactory.CreateText(
            "Body",
            card.transform,
            "两轮工作日的修复数据跨过临界值。大气净化层重新运转，" +
            "绿色沿着大陆边缘扩散，第一场雨正在归墟落下。\n\n" +
            "然而核心修复网络只能维持一次最终指令：关闭计划，让生命" +
            "依靠已经建立的生态继续生长；或由修复官进入地球核心，" +
            "以自身生物信息完成最后一次不可逆的校准。\n\n" +
            "晨曦已经把两份方案放在你面前。",
            27,
            new Color(0.86f, 0.94f, 0.92f, 1f),
            TextAnchor.UpperLeft
        );
        body.horizontalOverflow = HorizontalWrapMode.Wrap;
        SetAnchored(
            body.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(54f, -235f),
            new Vector2(-108f, 355f)
        );

        Text progress = UIFactory.CreateText(
            "Progress",
            card.transform,
            "地球修复进度  50 / 50  ·  最终协议已解锁",
            22,
            new Color(0.55f, 0.92f, 0.78f, 1f),
            TextAnchor.MiddleLeft
        );
        SetAnchored(
            progress.rectTransform,
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(54f, 145f),
            new Vector2(-108f, 42f)
        );

        Button continueButton = UIFactory.CreateButton(
            "ReadPlans",
            card.transform,
            "阅读最终方案  ›",
            new Vector2(420f, 78f),
            new Color(0.12f, 0.52f, 0.48f, 1f),
            29
        );
        SetAnchored(
            continueButton.GetComponent<RectTransform>(),
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(54f, 48f),
            new Vector2(420f, 78f)
        );
        continueButton.onClick.AddListener(() => ShowOnly(choiceRoot));
    }

    private void BuildChoice()
    {
        choiceRoot = CreateFullPanel("FinalChoice", transform);
        Image background = choiceRoot.AddComponent<Image>();
        background.color = new Color(0.008f, 0.025f, 0.04f, 1f);

        Text eyebrow = UIFactory.CreateText(
            "Eyebrow",
            choiceRoot.transform,
            "FINAL DECISION // 选择将写入第七十九任修复官档案",
            19,
            new Color(0.43f, 0.83f, 0.82f, 1f),
            TextAnchor.MiddleCenter
        );
        SetAnchored(
            eyebrow.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, -40f),
            new Vector2(0f, 36f)
        );

        Text title = UIFactory.CreateText(
            "Title",
            choiceRoot.transform,
            "你希望把怎样的明天留给他们？",
            52,
            Color.white
        );
        SetAnchored(
            title.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, -92f),
            new Vector2(0f, 82f)
        );

        BuildChoiceCard(
            choiceRoot.transform,
            "EndProgram",
            new Vector2(0.25f, 0.48f),
            noahEndingTexture,
            "终止地球重塑计划",
            "相信已经重建的生态，让地球按照自己的节奏恢复。" +
            "修复官返回诺亚，与家人共同见证新的文明选择。",
            new Color(0.91f, 0.55f, 0.25f, 1f),
            FinalEndingChoice.EndRestorationProgram
        );

        BuildChoiceCard(
            choiceRoot.transform,
            "Sacrifice",
            new Vector2(0.75f, 0.48f),
            sacrificeEndingTexture,
            "牺牲自己，完成最终校准",
            "驾驶返回舱进入地球核心，将自己的生命信息交给修复网络，" +
            "让复苏提前跨过最后一道门槛。",
            new Color(0.20f, 0.68f, 0.72f, 1f),
            FinalEndingChoice.SacrificeForEarth
        );

        Text hint = UIFactory.CreateText(
            "Hint",
            choiceRoot.transform,
            "两个结局都代表一种责任，没有标准答案。",
            21,
            new Color(0.62f, 0.76f, 0.78f, 1f)
        );
        SetAnchored(
            hint.rectTransform,
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 28f),
            new Vector2(0f, 42f)
        );
        choiceRoot.SetActive(false);
    }

    private void BuildChoiceCard(
        Transform parent,
        string name,
        Vector2 anchor,
        Texture texture,
        string title,
        string body,
        Color accentColor,
        FinalEndingChoice choice
    )
    {
        Image card = UIFactory.CreatePanel(
            name,
            parent,
            new Color(0.025f, 0.08f, 0.11f, 0.98f)
        );
        SetAnchored(
            card.rectTransform,
            anchor,
            anchor,
            Vector2.zero,
            new Vector2(760f, 710f)
        );
        card.rectTransform.pivot = new Vector2(0.5f, 0.5f);

        RawImage preview = CreateRawImage(
            "Preview",
            card.transform,
            texture,
            Color.white
        );
        SetAnchored(
            preview.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            Vector2.zero,
            new Vector2(0f, 360f)
        );
        preview.rectTransform.pivot = new Vector2(0f, 1f);

        Image accent = UIFactory.CreatePanel(
            "Accent",
            card.transform,
            accentColor
        );
        SetAnchored(
            accent.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, -360f),
            new Vector2(0f, 7f)
        );

        Text titleText = UIFactory.CreateText(
            "Title",
            card.transform,
            title,
            35,
            Color.white,
            TextAnchor.MiddleLeft
        );
        SetAnchored(
            titleText.rectTransform,
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(36f, 240f),
            new Vector2(-72f, 70f)
        );

        Text bodyText = UIFactory.CreateText(
            "Body",
            card.transform,
            body,
            23,
            new Color(0.79f, 0.91f, 0.90f, 1f),
            TextAnchor.UpperLeft
        );
        bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        SetAnchored(
            bodyText.rectTransform,
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(38f, 95f),
            new Vector2(-76f, 135f)
        );

        Button choose = UIFactory.CreateButton(
            "Choose",
            card.transform,
            "选择这个结局",
            new Vector2(330f, 68f),
            Color.Lerp(accentColor, Color.black, 0.18f),
            26
        );
        choose.GetComponent<Image>().color = Color.Lerp(
            accentColor,
            Color.black,
            0.18f
        );
        SetAnchored(
            choose.GetComponent<RectTransform>(),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(-36f, 28f),
            new Vector2(330f, 68f)
        );
        choose.GetComponent<RectTransform>().pivot = new Vector2(1f, 0f);
        choose.onClick.AddListener(() => SelectEnding(choice));
    }

    private void BuildResult()
    {
        resultRoot = CreateFullPanel("EndingResult", transform);
        resultImage = CreateRawImage(
            "EndingImage",
            resultRoot.transform,
            noahEndingTexture,
            Color.white
        );
        UIFactory.Stretch(resultImage.rectTransform);

        Image gradientVeil = UIFactory.CreatePanel(
            "ReadableVeil",
            resultRoot.transform,
            new Color(0.005f, 0.015f, 0.025f, 0.56f)
        );
        UIFactory.Stretch(gradientVeil.rectTransform);

        Image card = UIFactory.CreatePanel(
            "ResultCard",
            resultRoot.transform,
            new Color(0.018f, 0.06f, 0.085f, 0.94f)
        );
        SetAnchored(
            card.rectTransform,
            new Vector2(0.07f, 0.5f),
            new Vector2(0.07f, 0.5f),
            Vector2.zero,
            new Vector2(820f, 690f)
        );

        resultEyebrow = UIFactory.CreateText(
            "Eyebrow",
            card.transform,
            string.Empty,
            19,
            new Color(1f, 0.73f, 0.40f, 1f),
            TextAnchor.MiddleLeft
        );
        SetAnchored(
            resultEyebrow.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(50f, -42f),
            new Vector2(-100f, 38f)
        );

        resultTitle = UIFactory.CreateText(
            "Title",
            card.transform,
            string.Empty,
            52,
            Color.white,
            TextAnchor.UpperLeft
        );
        SetAnchored(
            resultTitle.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(48f, -108f),
            new Vector2(-96f, 145f)
        );

        resultBody = UIFactory.CreateText(
            "Body",
            card.transform,
            string.Empty,
            27,
            new Color(0.86f, 0.94f, 0.92f, 1f),
            TextAnchor.UpperLeft
        );
        resultBody.horizontalOverflow = HorizontalWrapMode.Wrap;
        SetAnchored(
            resultBody.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(50f, -270f),
            new Vector2(-100f, 285f)
        );

        Button menu = UIFactory.CreateButton(
            "ReturnToMenu",
            card.transform,
            "结束演示 · 回到主菜单",
            new Vector2(430f, 76f),
            new Color(0.12f, 0.50f, 0.47f, 1f),
            27
        );
        SetAnchored(
            menu.GetComponent<RectTransform>(),
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(50f, 46f),
            new Vector2(430f, 76f)
        );
        menu.onClick.AddListener(ReturnToMenu);
        resultRoot.SetActive(false);
    }

    private void SelectEnding(FinalEndingChoice choice)
    {
        MVPGameSession.CompleteEnding(choice);
        ShowEndingResult(choice, true);
    }

    private void ShowEndingResult(
        FinalEndingChoice choice,
        bool newlySelected
    )
    {
        bool endProgram = choice == FinalEndingChoice.EndRestorationProgram;
        resultImage.texture = endProgram
            ? noahEndingTexture
            : sacrificeEndingTexture;
        resultEyebrow.text = endProgram
            ? "ENDING A // 把未来交给生命"
            : "ENDING B // 第七十九任的最后航程";
        resultTitle.text = endProgram
            ? "地球不再需要守望者"
            : "你成为复苏的一部分";
        resultBody.text = endProgram
            ? "你关闭了持续三百余年的地球重塑计划。最后一条指令不是控制，" +
              "而是等待。多年后，妹妹在诺亚的课堂里看见来自地球的第一张" +
              "森林影像。你与家人站在晨光中，终于不必隔着档案想象故乡。\n\n" +
              "晨曦：修复官，任务完成。欢迎回家。"
            : "返回舱穿过云层，你把七十八任修复官留下的记录与自己的生命" +
              "信息写入地球。净化塔依次点亮，雨水落进新的河床。诺亚收到的" +
              "最后一段影像里，一株银杏在风中展开叶片。\n\n" +
              "晨曦：第七十九任修复官，地球会记得你。";

        if (!newlySelected)
        {
            resultEyebrow.text += "  //  已完成";
        }

        ShowOnly(resultRoot);
    }

    private void ReturnToMenu()
    {
        gameObject.SetActive(false);
        returnToMenuAction?.Invoke();
    }

    private void ShowOnly(GameObject target)
    {
        preludeRoot.SetActive(target == preludeRoot);
        choiceRoot.SetActive(target == choiceRoot);
        resultRoot.SetActive(target == resultRoot);
    }

    private static void CreateTopAccent(Transform parent, Color color)
    {
        Image accent = UIFactory.CreatePanel("Accent", parent, color);
        SetAnchored(
            accent.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            Vector2.zero,
            new Vector2(0f, 7f)
        );
    }

    private static GameObject CreateFullPanel(string name, Transform parent)
    {
        RectTransform rect = UIFactory.CreateRect(name, parent);
        UIFactory.Stretch(rect);
        return rect.gameObject;
    }

    private static RawImage CreateRawImage(
        string name,
        Transform parent,
        Texture texture,
        Color color
    )
    {
        GameObject root = new(name, typeof(RectTransform), typeof(RawImage));
        root.transform.SetParent(parent, false);
        RawImage image = root.GetComponent<RawImage>();
        image.texture = texture;
        image.color = color;
        return image;
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
