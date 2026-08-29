using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Runtime-built front end for the competition MVP. The media area is a stable
/// replacement point for the team's final videos; until those clips arrive,
/// concise narrative cards keep the complete flow testable.
/// </summary>
public class FrontEndBootstrap : MonoBehaviour
{
    private readonly struct NarrativeSlide
    {
        public readonly string title;
        public readonly string body;
        public readonly string mediaLabel;

        public NarrativeSlide(string title, string body, string mediaLabel)
        {
            this.title = title;
            this.body = body;
            this.mediaLabel = mediaLabel;
        }
    }

    private GameObject mainPanel;
    private GameObject profilePanel;
    private GameObject narrativePanel;
    private InputField nameInput;
    private Text profileHint;
    private Text avatarPreview;
    private Text narrativeEyebrow;
    private Text narrativeTitle;
    private Text narrativeBody;
    private Text narrativeMedia;
    private Text narrativeProgress;
    private Text narrativeContinueLabel;
    private Button[] avatarButtons;
    private NarrativeSlide[] activeSlides = Array.Empty<NarrativeSlide>();
    private Action narrativeCompleted;
    private int slideIndex;
    private string selectedAvatar = "RESTORER_A";

    private void Awake()
    {
        Application.runInBackground = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        EnsureCamera();
        EnsureEventSystem();

        Canvas canvas = UIFactory.CreateCanvas("FrontEndCanvas");
        BuildBackdrop(canvas.transform);
        BuildMainPanel(canvas.transform);
        BuildProfilePanel(canvas.transform);
        BuildNarrativePanel(canvas.transform);

        if (
            MVPGameSession.TryConsumeNarrative(
                out GameNarrativeRoute route
            ) &&
            route == GameNarrativeRoute.FirstReturnToNoah
        )
        {
            StartFirstReturnNarrative();
        }
        else
        {
            ShowOnly(mainPanel);
        }
    }

    private void BuildBackdrop(Transform parent)
    {
        Image background = UIFactory.CreatePanel(
            "Background",
            parent,
            new Color(0.008f, 0.025f, 0.05f, 1f)
        );
        UIFactory.Stretch(background.rectTransform);

        Image glow = UIFactory.CreatePanel(
            "EarthSignalGlow",
            parent,
            new Color(0.02f, 0.34f, 0.48f, 0.20f)
        );
        SetAnchored(
            glow.rectTransform,
            new Vector2(0.58f, 0.05f),
            new Vector2(1.04f, 0.95f),
            Vector2.zero,
            Vector2.zero
        );

        Text coordinates = UIFactory.CreateText(
            "Coordinates",
            parent,
            "EARTH RESTORATION NETWORK  //  03.20 LY  //  YEAR 2749",
            15,
            new Color(0.35f, 0.72f, 0.8f, 0.75f),
            TextAnchor.MiddleLeft
        );
        SetAnchored(
            coordinates.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(48f, -24f),
            new Vector2(-96f, 38f)
        );
    }

    private void BuildMainPanel(Transform parent)
    {
        mainPanel = CreateFullPanel("MainMenu", parent);

        Text eyebrow = UIFactory.CreateText(
            "Eyebrow",
            mainPanel.transform,
            "PROJECT  EARTH  //  第七十九任修复官任期",
            20,
            UIPalette.Accent,
            TextAnchor.MiddleLeft
        );
        SetAnchored(
            eyebrow.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(120f, -180f),
            new Vector2(900f, 38f)
        );

        Text title = UIFactory.CreateText(
            "Title",
            mainPanel.transform,
            "地 球 重 塑 计 划",
            74,
            UIPalette.TextMain,
            TextAnchor.MiddleLeft
        );
        SetAnchored(
            title.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(116f, -242f),
            new Vector2(1100f, 110f)
        );

        Text subtitle = UIFactory.CreateText(
            "Subtitle",
            mainPanel.transform,
            "两颗星球相隔 3.2 光年。\n七十八任修复官之后，重返地球的选择交到你手中。",
            29,
            UIPalette.TextDim,
            TextAnchor.UpperLeft
        );
        SetAnchored(
            subtitle.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(122f, -380f),
            new Vector2(900f, 150f)
        );

        Image status = UIFactory.CreatePanel(
            "StatusCard",
            mainPanel.transform,
            new Color(0.025f, 0.10f, 0.15f, 0.94f)
        );
        SetAnchored(
            status.rectTransform,
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(120f, 220f),
            new Vector2(700f, 120f)
        );

        Text statusText = UIFactory.CreateText(
            "StatusText",
            status.transform,
            "归墟同步轨道空间站  ·  ONLINE\n地球修复进度  " +
            MVPGameSession.EarthProgress + " / " +
            MVPGameSession.EndingProgress,
            23,
            new Color(0.62f, 0.91f, 0.94f, 1f),
            TextAnchor.MiddleLeft
        );
        SetAnchored(
            statusText.rectTransform,
            Vector2.zero,
            Vector2.one,
            new Vector2(30f, 0f),
            new Vector2(-60f, 0f)
        );

        Button enter = UIFactory.CreateButton(
            "EnterGame",
            mainPanel.transform,
            "进入计划  ›",
            new Vector2(360f, 82f),
            UIPalette.AccentDim,
            32
        );
        SetAnchored(
            enter.GetComponent<RectTransform>(),
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(120f, 105f),
            new Vector2(360f, 82f)
        );
        enter.onClick.AddListener(ShowProfile);
    }

    private void BuildProfilePanel(Transform parent)
    {
        profilePanel = CreateFullPanel("ProfileSetup", parent);

        Image card = UIFactory.CreatePanel(
            "ProfileCard",
            profilePanel.transform,
            new Color(0.025f, 0.09f, 0.14f, 0.97f)
        );
        SetAnchored(
            card.rectTransform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(1420f, 790f)
        );

        Text title = UIFactory.CreateText(
            "Title",
            card.transform,
            "建立修复官身份档案",
            48,
            UIPalette.TextMain,
            TextAnchor.MiddleLeft
        );
        SetAnchored(
            title.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(70f, -60f),
            new Vector2(800f, 72f)
        );

        Text nameLabel = UIFactory.CreateText(
            "NameLabel",
            card.transform,
            "修复官呼号",
            24,
            UIPalette.Accent,
            TextAnchor.MiddleLeft
        );
        SetAnchored(
            nameLabel.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(72f, -170f),
            new Vector2(420f, 42f)
        );

        nameInput = CreateInputField(card.transform);
        SetAnchored(
            nameInput.GetComponent<RectTransform>(),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(72f, -222f),
            new Vector2(650f, 72f)
        );

        Text avatarLabel = UIFactory.CreateText(
            "AvatarLabel",
            card.transform,
            "选择身份头像",
            24,
            UIPalette.Accent,
            TextAnchor.MiddleLeft
        );
        SetAnchored(
            avatarLabel.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(72f, -330f),
            new Vector2(420f, 42f)
        );

        avatarButtons = new Button[3];
        string[] ids = { "RESTORER_A", "RESTORER_B", "RESTORER_C" };
        string[] labels = { "079-A\n轨道蓝", "079-B\n生态青", "079-C\n基因紫" };

        for (int i = 0; i < avatarButtons.Length; i++)
        {
            int index = i;
            avatarButtons[i] = UIFactory.CreateButton(
                "Avatar_" + ids[i],
                card.transform,
                labels[i],
                new Vector2(200f, 150f),
                i == 0 ? UIPalette.AccentDim : UIPalette.PanelLight,
                24
            );
            SetAnchored(
                avatarButtons[i].GetComponent<RectTransform>(),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(72f + i * 225f, -388f),
                new Vector2(200f, 150f)
            );
            avatarButtons[i].onClick.AddListener(
                () => SelectAvatar(ids[index], index)
            );
        }

        Image previewCard = UIFactory.CreatePanel(
            "PreviewCard",
            card.transform,
            new Color(0.015f, 0.045f, 0.075f, 1f)
        );
        SetAnchored(
            previewCard.rectTransform,
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(-80f, 0f),
            new Vector2(480f, 610f)
        );

        avatarPreview = UIFactory.CreateText(
            "AvatarPreview",
            previewCard.transform,
            "079\nA",
            112,
            UIPalette.Accent
        );
        SetAnchored(
            avatarPreview.rectTransform,
            new Vector2(0.5f, 0.55f),
            new Vector2(0.5f, 0.55f),
            Vector2.zero,
            new Vector2(360f, 300f)
        );

        Text previewLabel = UIFactory.CreateText(
            "PreviewLabel",
            previewCard.transform,
            "第七十九任地球修复官\nPERSONNEL RECORD // ACTIVE",
            21,
            UIPalette.TextDim
        );
        SetAnchored(
            previewLabel.rectTransform,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 60f),
            new Vector2(420f, 100f)
        );

        profileHint = UIFactory.CreateText(
            "Hint",
            card.transform,
            "昵称将在晨曦通讯和任务报告中显示",
            20,
            UIPalette.TextDim,
            TextAnchor.MiddleLeft
        );
        SetAnchored(
            profileHint.rectTransform,
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(72f, 82f),
            new Vector2(700f, 40f)
        );

        Button confirm = UIFactory.CreateButton(
            "ConfirmProfile",
            card.transform,
            "确认身份并继续",
            new Vector2(360f, 76f),
            UIPalette.AccentDim,
            29
        );
        SetAnchored(
            confirm.GetComponent<RectTransform>(),
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(72f, 24f),
            new Vector2(360f, 76f)
        );
        confirm.onClick.AddListener(ConfirmProfile);
    }

    private void BuildNarrativePanel(Transform parent)
    {
        narrativePanel = CreateFullPanel("Narrative", parent);

        Image media = UIFactory.CreatePanel(
            "MediaSlot",
            narrativePanel.transform,
            new Color(0.015f, 0.07f, 0.11f, 1f)
        );
        SetAnchored(
            media.rectTransform,
            new Vector2(0f, 0f),
            new Vector2(0.60f, 1f),
            new Vector2(50f, 70f),
            new Vector2(-75f, -140f)
        );

        narrativeMedia = UIFactory.CreateText(
            "MediaLabel",
            media.transform,
            string.Empty,
            26,
            new Color(0.40f, 0.78f, 0.84f, 0.9f)
        );
        UIFactory.Stretch(narrativeMedia.rectTransform);

        Image content = UIFactory.CreatePanel(
            "NarrativeCard",
            narrativePanel.transform,
            new Color(0.025f, 0.085f, 0.13f, 0.98f)
        );
        SetAnchored(
            content.rectTransform,
            new Vector2(0.63f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 70f),
            new Vector2(-60f, -140f)
        );

        narrativeEyebrow = UIFactory.CreateText(
            "Eyebrow",
            content.transform,
            "NARRATIVE TRANSMISSION",
            18,
            UIPalette.Accent,
            TextAnchor.MiddleLeft
        );
        SetAnchored(
            narrativeEyebrow.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(42f, -38f),
            new Vector2(-84f, 34f)
        );

        narrativeTitle = UIFactory.CreateText(
            "Title",
            content.transform,
            string.Empty,
            42,
            UIPalette.TextMain,
            TextAnchor.UpperLeft
        );
        SetAnchored(
            narrativeTitle.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(42f, -100f),
            new Vector2(-84f, 120f)
        );

        narrativeBody = UIFactory.CreateText(
            "Body",
            content.transform,
            string.Empty,
            27,
            UIPalette.TextDim,
            TextAnchor.UpperLeft
        );
        narrativeBody.horizontalOverflow = HorizontalWrapMode.Wrap;
        SetAnchored(
            narrativeBody.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(44f, -250f),
            new Vector2(-88f, 330f)
        );

        narrativeProgress = UIFactory.CreateText(
            "Progress",
            content.transform,
            string.Empty,
            17,
            new Color(0.40f, 0.68f, 0.74f, 1f),
            TextAnchor.MiddleLeft
        );
        SetAnchored(
            narrativeProgress.rectTransform,
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(44f, 120f),
            new Vector2(300f, 34f)
        );

        Button next = UIFactory.CreateButton(
            "Continue",
            content.transform,
            "继续  ›",
            new Vector2(280f, 72f),
            UIPalette.AccentDim,
            28
        );
        SetAnchored(
            next.GetComponent<RectTransform>(),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(-42f, 48f),
            new Vector2(280f, 72f)
        );
        narrativeContinueLabel = next.GetComponentInChildren<Text>();
        next.onClick.AddListener(AdvanceNarrative);

        Button skip = UIFactory.CreateButton(
            "Skip",
            narrativePanel.transform,
            "跳过剧情",
            new Vector2(180f, 48f),
            new Color(0.05f, 0.12f, 0.16f, 0.95f),
            20
        );
        SetAnchored(
            skip.GetComponent<RectTransform>(),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-58f, -18f),
            new Vector2(180f, 48f)
        );
        skip.onClick.AddListener(FinishNarrative);
    }

    private void ShowProfile()
    {
        nameInput.text = MVPGameSession.HasPlayerProfile
            ? MVPGameSession.PlayerName
            : string.Empty;
        ShowOnly(profilePanel);
    }

    private void ConfirmProfile()
    {
        string enteredName = nameInput.text == null
            ? string.Empty
            : nameInput.text.Trim();

        if (enteredName.Length == 0)
        {
            profileHint.text = "请输入 1～12 个字符的修复官呼号";
            profileHint.color = UIPalette.Warn;
            return;
        }

        MVPGameSession.SetPlayerProfile(enteredName, selectedAvatar);
        StartOpeningNarrative();
    }

    private void SelectAvatar(string id, int selectedIndex)
    {
        selectedAvatar = id;
        avatarPreview.text = "079\n" + (char)('A' + selectedIndex);

        for (int i = 0; i < avatarButtons.Length; i++)
        {
            Image image = avatarButtons[i].GetComponent<Image>();
            image.color = i == selectedIndex
                ? UIPalette.AccentDim
                : UIPalette.PanelLight;
        }
    }

    private void StartOpeningNarrative()
    {
        NarrativeSlide[] slides =
        {
            new(
                "一星璀璨，一星长眠",
                "无尽星尘横跨 3.2 光年。诺亚星依然蓝绿明亮，" +
                "而被称为归墟的地球，只剩灰黄荒漠与沉默裂谷。",
                "VIDEO SLOT // OPENING_COMBINED"
            ),
            new(
                "文明迁徙",
                "公元 2317 年，地球生态圈彻底归零。" +
                "公元 2419 年，人类完成百年星际迁徙，文明在诺亚星延续。",
                "VIDEO SLOT // PROLOGUE_01"
            ),
            new(
                "地球重塑计划",
                "三百二十九年，七十八任修复官独自驻守荒芜。" +
                "现在，第七十九任修复官 " + MVPGameSession.PlayerName +
                " 将接过这场跨越世代的修复任务。",
                "VIDEO SLOT // PROLOGUE_02"
            ),
            new(
                "晨曦系统已连接",
                MVPGameSession.PlayerName +
                " 修复官，你好。我是你的 AI 智能管家晨曦。" +
                "接下来你将体验星际修复工作、诺亚生活以及最终剧情抉择。",
                "CHENXI // IDENTITY VERIFIED"
            )
        };

        PlayNarrative(
            "OPENING SEQUENCE",
            slides,
            () =>
            {
                MVPGameSession.MarkOpeningCompleted();
                SceneTransitionManager.EnterStationHub();
            }
        );
    }

    private void StartFirstReturnNarrative()
    {
        NarrativeSlide[] slides =
        {
            new(
                "本月最后一个工作日",
                "三项修复任务已经完成。返程飞船与空间站完成对接，" +
                "舱门在气密提示音中缓缓关闭。",
                "VIDEO SLOT // ACT_02_DEPARTURE"
            ),
            new(
                "返航诺亚",
                "飞船离开归墟同步轨道。远方的诺亚星逐渐占满舷窗，" +
                "城市、森林与湖泊重新出现在视野中。",
                "VIDEO SLOT // ACT_02_NOAH"
            ),
            new(
                "休假生活已解锁",
                "欢迎 " + MVPGameSession.PlayerName +
                " 修复官回到诺亚。休假期间可以完成地球动植物拼图、" +
                "解锁图鉴，并继续积累地球修复进度。",
                "NOAH LIFE // MODULE RESERVED FOR P6-4"
            )
        };

        PlayNarrative(
            "RETURN TO NOAH",
            slides,
            () =>
            {
                MVPGameSession.CompleteFirstReturnAndBeginNextWorkday();
                SceneTransitionManager.EnterStationHub();
            }
        );
    }

    private void PlayNarrative(
        string eyebrow,
        NarrativeSlide[] slides,
        Action onCompleted
    )
    {
        activeSlides = slides ?? Array.Empty<NarrativeSlide>();
        narrativeCompleted = onCompleted;
        narrativeEyebrow.text = eyebrow;
        slideIndex = 0;
        ShowOnly(narrativePanel);
        PresentSlide();
    }

    private void AdvanceNarrative()
    {
        if (slideIndex >= activeSlides.Length - 1)
        {
            FinishNarrative();
            return;
        }

        slideIndex++;
        PresentSlide();
    }

    private void PresentSlide()
    {
        if (activeSlides.Length == 0)
        {
            FinishNarrative();
            return;
        }

        NarrativeSlide slide = activeSlides[slideIndex];
        narrativeTitle.text = slide.title;
        narrativeBody.text = slide.body;
        narrativeMedia.text = slide.mediaLabel;
        narrativeProgress.text =
            $"SEQUENCE  {slideIndex + 1:00}/{activeSlides.Length:00}";
        narrativeContinueLabel.text =
            slideIndex >= activeSlides.Length - 1
                ? "进入下一阶段  ›"
                : "继续  ›";
    }

    private void FinishNarrative()
    {
        Action completion = narrativeCompleted;
        narrativeCompleted = null;
        completion?.Invoke();
    }

    private void ShowOnly(GameObject target)
    {
        mainPanel.SetActive(target == mainPanel);
        profilePanel.SetActive(target == profilePanel);
        narrativePanel.SetActive(target == narrativePanel);
    }

    private static GameObject CreateFullPanel(string name, Transform parent)
    {
        RectTransform rect = UIFactory.CreateRect(name, parent);
        UIFactory.Stretch(rect);
        return rect.gameObject;
    }

    private static InputField CreateInputField(Transform parent)
    {
        GameObject root = new(
            "NameInput",
            typeof(RectTransform),
            typeof(Image),
            typeof(InputField)
        );
        root.transform.SetParent(parent, false);
        root.GetComponent<Image>().color = new Color(0.01f, 0.04f, 0.07f, 1f);

        Text text = UIFactory.CreateText(
            "Text",
            root.transform,
            string.Empty,
            27,
            UIPalette.TextMain,
            TextAnchor.MiddleLeft
        );
        SetAnchored(
            text.rectTransform,
            Vector2.zero,
            Vector2.one,
            new Vector2(22f, 0f),
            new Vector2(-44f, 0f)
        );

        Text placeholder = UIFactory.CreateText(
            "Placeholder",
            root.transform,
            "输入你的昵称",
            27,
            new Color(0.38f, 0.53f, 0.62f, 0.85f),
            TextAnchor.MiddleLeft
        );
        SetAnchored(
            placeholder.rectTransform,
            Vector2.zero,
            Vector2.one,
            new Vector2(22f, 0f),
            new Vector2(-44f, 0f)
        );

        InputField field = root.GetComponent<InputField>();
        field.textComponent = text;
        field.placeholder = placeholder;
        field.characterLimit = 12;
        field.lineType = InputField.LineType.SingleLine;
        field.contentType = InputField.ContentType.Standard;
        return field;
    }

    private static void EnsureCamera()
    {
        if (Camera.main != null)
        {
            return;
        }

        GameObject cameraObject = new("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.005f, 0.015f, 0.03f, 1f);
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        cameraObject.AddComponent<AudioListener>();
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject eventObject = new("EventSystem");
        eventObject.AddComponent<EventSystem>();
        eventObject.AddComponent<StandaloneInputModule>();
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
