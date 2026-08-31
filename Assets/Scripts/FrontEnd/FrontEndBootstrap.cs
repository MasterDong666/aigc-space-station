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
    private RawImage avatarPreview;
    private Text avatarPreviewLabel;
    private Texture2D[] avatarTextures;
    private Text narrativeEyebrow;
    private Text narrativeTitle;
    private Text narrativeBody;
    private Text narrativeMedia;
    private Text narrativeProgress;
    private Text narrativeContinueLabel;
    private Text mainStatusText;
    private NoahHolidayUI holidayUI;
    private EndingChoiceUI endingUI;
    private Button[] avatarButtons;
    private NarrativeSlide[] activeSlides = Array.Empty<NarrativeSlide>();
    private Action narrativeCompleted;
    private int slideIndex;
    private string selectedAvatar = "RESTORER_A";

    private void Awake()
    {
        Application.runInBackground = true;
        SaveManager.TryLoadIntoSession();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        EnsureCamera();
        EnsureEventSystem();

        Canvas canvas = UIFactory.CreateCanvas("FrontEndCanvas");
        BuildBackdrop(canvas.transform);
        BuildMainPanel(canvas.transform);
        BuildProfilePanel(canvas.transform);
        BuildNarrativePanel(canvas.transform);
        BuildExtendedFlow(canvas.transform);
        CinematicUIVisuals.PolishHierarchy(
            canvas.transform,
            CinematicUIVisuals.Sky,
            CinematicUIVisuals.Sun
        );

        if (MVPGameSession.TryConsumeNarrative(out GameNarrativeRoute route))
        {
            if (route == GameNarrativeRoute.FirstReturnToNoah)
            {
                StartFirstReturnNarrative();
            }
            else if (route == GameNarrativeRoute.FinalChoice)
            {
                ShowEndingFlow();
            }
            else
            {
                ShowOnly(mainPanel);
            }
        }
        else
        {
            ShowOnly(mainPanel);
        }

        GameFlowManager.HookProgressListener();

        if (mainPanel.activeSelf)
        {
            GameFlowManager.SetStage(FlowStage.MainMenu, "Awake");
        }
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F2))
        {
            ShowProfile();
        }
        else if (Input.GetKeyDown(KeyCode.F3))
        {
            if (!MVPGameSession.HasPlayerProfile)
            {
                MVPGameSession.SetPlayerProfile("预览修复官", "RESTORER_A");
            }

            StartOpeningSequence();
        }
        else if (Input.GetKeyDown(KeyCode.F4))
        {
            ShowHolidayFlow();
        }
        else if (Input.GetKeyDown(KeyCode.F5))
        {
            ShowEndingFlow();
        }
    }
#endif

    private void BuildBackdrop(Transform parent)
    {
        CinematicUIVisuals.AddBackdrop(
            parent,
            "FrontendArt/MainHero",
            new Color(0.015f, 0.035f, 0.08f, 0.20f),
            "FrontEndHero"
        );

        Text coordinates = UIFactory.CreateText(
            "Coordinates",
            parent,
            "EARTH RESTORATION NETWORK  //  03.20 LY  //  YEAR 2749",
            15,
            new Color(0.77f, 0.92f, 0.96f, 0.82f),
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

        Image storyCard = CinematicUIVisuals.CreateCard(
            "MainStoryCard",
            mainPanel.transform,
            new Color(0.035f, 0.09f, 0.15f, 0.78f),
            new Vector2(790f, 690f)
        );
        SetAnchored(
            storyCard.rectTransform,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(72f, 0f),
            new Vector2(790f, 690f)
        );
        CinematicUIVisuals.AddEntrance(storyCard.gameObject);

        Text eyebrow = UIFactory.CreateText(
            "Eyebrow",
            storyCard.transform,
            "PROJECT  EARTH  //  第七十九任修复官任期",
            20,
            UIPalette.Accent,
            TextAnchor.MiddleLeft
        );
        SetAnchored(
            eyebrow.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(52f, -62f),
            new Vector2(680f, 38f)
        );

        Text title = UIFactory.CreateText(
            "Title",
            storyCard.transform,
            "地 球 重 塑 计 划",
            74,
            UIPalette.TextMain,
            TextAnchor.MiddleLeft
        );
        SetAnchored(
            title.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(48f, -124f),
            new Vector2(690f, 110f)
        );

        Text subtitle = UIFactory.CreateText(
            "Subtitle",
            storyCard.transform,
            "两颗星球相隔 3.2 光年。\n七十八任修复官之后，重返地球的选择交到你手中。",
            29,
            UIPalette.TextDim,
            TextAnchor.UpperLeft
        );
        SetAnchored(
            subtitle.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(54f, -270f),
            new Vector2(660f, 150f)
        );

        Image status = UIFactory.CreatePanel(
            "StatusCard",
            storyCard.transform,
            new Color(0.08f, 0.20f, 0.25f, 0.82f)
        );
        SetAnchored(
            status.rectTransform,
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(52f, 150f),
            new Vector2(680f, 120f)
        );

        mainStatusText = UIFactory.CreateText(
            "StatusText",
            status.transform,
            string.Empty,
            23,
            new Color(0.62f, 0.91f, 0.94f, 1f),
            TextAnchor.MiddleLeft
        );
        SetAnchored(
            mainStatusText.rectTransform,
            Vector2.zero,
            Vector2.one,
            new Vector2(30f, 0f),
            new Vector2(-60f, 0f)
        );
        UpdateMainStatus();

        Button enter = UIFactory.CreateButton(
            "EnterGame",
            storyCard.transform,
            "启程，成为修复官  ›",
            new Vector2(360f, 82f),
            new Color(0.94f, 0.46f, 0.24f, 1f),
            32
        );
        SetAnchored(
            enter.GetComponent<RectTransform>(),
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(52f, 42f),
            new Vector2(360f, 82f)
        );
        enter.onClick.AddListener(ShowProfile);

        UIManager.Register("MainMenu", mainPanel);
    }

    private void BuildProfilePanel(Transform parent)
    {
        profilePanel = CreateFullPanel("ProfileSetup", parent);

        Image profileVeil = UIFactory.CreatePanel(
            "ProfileVeil",
            profilePanel.transform,
            new Color(0.02f, 0.04f, 0.08f, 0.55f)
        );
        UIFactory.Stretch(profileVeil.rectTransform);

        Image card = CinematicUIVisuals.CreateCard(
            "ProfileCard",
            profilePanel.transform,
            new Color(0.055f, 0.12f, 0.17f, 0.96f),
            new Vector2(1420f, 790f)
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
        avatarTextures = new[]
        {
            Resources.Load<Texture2D>("FrontendArt/RestorerA"),
            Resources.Load<Texture2D>("FrontendArt/RestorerB"),
            Resources.Load<Texture2D>("FrontendArt/RestorerC"),
        };
        string[] ids = { "RESTORER_A", "RESTORER_B", "RESTORER_C" };
        string[] labels = { "079-A\n轨道蓝", "079-B\n生态青", "079-C\n基因紫" };

        for (int i = 0; i < avatarButtons.Length; i++)
        {
            int index = i;
            avatarButtons[i] = UIFactory.CreateButton(
                "Avatar_" + ids[i],
                card.transform,
                string.Empty,
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

            Mask avatarMask = avatarButtons[i].gameObject.AddComponent<Mask>();
            avatarMask.showMaskGraphic = true;
            RawImage portrait = CreateRawImage(
                "Portrait",
                avatarButtons[i].transform,
                avatarTextures[i],
                Color.white
            );
            SetAnchored(
                portrait.rectTransform,
                Vector2.zero,
                Vector2.one,
                new Vector2(4f, 38f),
                new Vector2(-8f, -42f)
            );
            Image labelPill = UIFactory.CreatePanel(
                "AvatarLabelCard",
                avatarButtons[i].transform,
                new Color(0.04f, 0.08f, 0.12f, 0.88f)
            );
            SetAnchored(
                labelPill.rectTransform,
                Vector2.zero,
                new Vector2(1f, 0f),
                Vector2.zero,
                new Vector2(0f, 40f)
            );
            Text avatarLabelText = UIFactory.CreateText(
                "AvatarName",
                labelPill.transform,
                labels[i].Replace("\n", "  ·  "),
                18,
                Color.white
            );
            UIFactory.Stretch(avatarLabelText.rectTransform);
        }

        Image previewCard = CinematicUIVisuals.CreateCard(
            "PreviewCard",
            card.transform,
            new Color(0.015f, 0.045f, 0.075f, 1f),
            new Vector2(480f, 610f)
        );
        SetAnchored(
            previewCard.rectTransform,
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(-80f, 0f),
            new Vector2(480f, 610f)
        );

        avatarPreview = CreateRawImage(
            "AvatarPreview",
            previewCard.transform,
            avatarTextures[0],
            Color.white
        );
        SetAnchored(
            avatarPreview.rectTransform,
            new Vector2(0f, 0.22f),
            new Vector2(1f, 1f),
            new Vector2(16f, 16f),
            new Vector2(-32f, -32f)
        );

        avatarPreviewLabel = UIFactory.CreateText(
            "PreviewLabel",
            previewCard.transform,
            "第七十九任地球修复官  ·  079-A\nPERSONNEL RECORD // ACTIVE",
            21,
            UIPalette.TextDim
        );
        SetAnchored(
            avatarPreviewLabel.rectTransform,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 60f),
            new Vector2(420f, 100f)
        );

        Image hintPill = UIFactory.CreatePanel(
            "ProfileHintPill",
            card.transform,
            new Color(0.07f, 0.20f, 0.25f, 0.94f)
        );
        SetAnchored(
            hintPill.rectTransform,
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(72f, 130f),
            new Vector2(650f, 48f)
        );
        MiniGameVisuals.Round(hintPill);

        profileHint = UIFactory.CreateText(
            "Hint",
            hintPill.transform,
            "✦  昵称将在晨曦通讯和任务报告中显示",
            19,
            new Color(0.78f, 0.94f, 0.91f, 1f),
            TextAnchor.MiddleLeft
        );
        UIFactory.Stretch(profileHint.rectTransform);
        profileHint.rectTransform.offsetMin = new Vector2(20f, 0f);
        profileHint.rectTransform.offsetMax = new Vector2(-16f, 0f);

        Button confirm = UIFactory.CreateButton(
            "ConfirmProfile",
            card.transform,
            "确认身份并继续",
            new Vector2(390f, 80f),
            new Color(0.94f, 0.46f, 0.24f, 1f),
            29
        );
        SetAnchored(
            confirm.GetComponent<RectTransform>(),
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(72f, 32f),
            new Vector2(390f, 80f)
        );
        confirm.onClick.AddListener(ConfirmProfile);

        UIManager.Register("ProfileSetup", profilePanel);
    }

    private void BuildNarrativePanel(Transform parent)
    {
        narrativePanel = CreateFullPanel("Narrative", parent);

        Image narrativeVeil = UIFactory.CreatePanel(
            "NarrativeVeil",
            narrativePanel.transform,
            new Color(0.01f, 0.025f, 0.055f, 0.36f)
        );
        UIFactory.Stretch(narrativeVeil.rectTransform);

        Image media = CinematicUIVisuals.CreateCard(
            "MediaSlot",
            narrativePanel.transform,
            Color.white,
            Vector2.zero
        );
        SetAnchored(
            media.rectTransform,
            new Vector2(0f, 0f),
            new Vector2(0.60f, 1f),
            new Vector2(50f, 70f),
            new Vector2(-75f, -140f)
        );

        RawImage narrativeArt = CreateRawImage(
            "OpeningJourneyArt",
            media.transform,
            Resources.Load<Texture2D>("FrontendArt/OpeningJourney"),
            Color.white
        );
        UIFactory.Stretch(narrativeArt.rectTransform);

        Image captionCard = UIFactory.CreatePanel(
            "MediaCaptionCard",
            media.transform,
            new Color(0.035f, 0.08f, 0.12f, 0.84f)
        );
        SetAnchored(
            captionCard.rectTransform,
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(24f, 24f),
            new Vector2(-48f, 58f)
        );
        narrativeMedia = UIFactory.CreateText(
            "MediaLabel",
            captionCard.transform,
            string.Empty,
            20,
            CinematicUIVisuals.Cream,
            TextAnchor.MiddleLeft
        );
        UIFactory.Stretch(narrativeMedia.rectTransform);
        narrativeMedia.rectTransform.offsetMin = new Vector2(20f, 0f);
        narrativeMedia.rectTransform.offsetMax = new Vector2(-20f, 0f);

        Image content = CinematicUIVisuals.CreateCard(
            "NarrativeCard",
            narrativePanel.transform,
            new Color(0.045f, 0.10f, 0.15f, 0.95f),
            Vector2.zero
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
            CinematicUIVisuals.Sun,
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
            new Color(0.94f, 0.46f, 0.24f, 1f),
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
            new Color(0.12f, 0.21f, 0.26f, 0.95f),
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

        UIManager.Register("Narrative", narrativePanel);
    }

    private void BuildExtendedFlow(Transform parent)
    {
        holidayUI = CreateFlowModule<NoahHolidayUI>(
            "NoahHolidayFlow",
            parent
        );
        holidayUI.BuildUI();

        endingUI = CreateFlowModule<EndingChoiceUI>(
            "EndingChoiceFlow",
            parent
        );
        endingUI.BuildUI();

        UIManager.Register("NoahHolidayFlow", holidayUI.gameObject);
        UIManager.Register("EndingChoiceFlow", endingUI.gameObject);
    }

    private void ShowProfile()
    {
        GameFlowManager.SetStage(FlowStage.ProfileSetup, "EnterGame");
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
        SaveManager.TrySave();
        StartOpeningSequence();
    }

    private void SelectAvatar(string id, int selectedIndex)
    {
        selectedAvatar = id;
        avatarPreview.texture = avatarTextures[selectedIndex];
        avatarPreviewLabel.text =
            "第七十九任地球修复官  ·  079-" +
            (char)('A' + selectedIndex) +
            "\nPERSONNEL RECORD // ACTIVE";

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
                "两颗世界  ·  修复航线已建立"
            ),
            new(
                "文明迁徙",
                "公元 2317 年，地球生态圈彻底归零。" +
                "公元 2419 年，人类完成百年星际迁徙，文明在诺亚星延续。",
                "文明迁徙纪年  ·  2317—2419"
            ),
            new(
                "地球重塑计划",
                "三百二十九年，七十八任修复官独自驻守荒芜。" +
                "现在，第七十九任修复官 " + MVPGameSession.PlayerName +
                " 将接过这场跨越世代的修复任务。",
                "七十八任修复官  ·  接力档案"
            ),
            new(
                "晨曦系统已连接",
                MVPGameSession.PlayerName +
                " 修复官，你好。我是你的 AI 智能管家晨曦。" +
                "接下来你将体验星际修复工作、诺亚生活以及最终剧情抉择。",
                "晨曦 AI  ·  身份核验成功"
            )
        };

        PlayNarrative(
            "OPENING SEQUENCE",
            slides,
            () =>
            {
                MVPGameSession.MarkOpeningCompleted();
                ShowAIIntroPopup();
            }
        );
    }

    /// <summary>开场序列：结局引入视频 → 正序视频/叙事卡 →（AI 弹窗在完成后弹出）。</summary>
    private void StartOpeningSequence()
    {
        GameFlowManager.SetStage(FlowStage.EndingTeaserVideo, "confirm profile");
        VideoManager.Play("ending_teaser", () =>
        {
            GameFlowManager.SetStage(FlowStage.OpeningVideo, "teaser done");
            PlayOpeningVideoOrNarrative();
        });
    }

    /// <summary>
    /// 开场叙事入口：接入 opening.mp4（存在则播放并跳过叙事卡；缺失则回退既有叙事卡）。
    /// 这样团队放入正序视频后自动替换，无需改代码；无视频时流程照常推进。
    /// </summary>
    private void PlayOpeningVideoOrNarrative()
    {
        if (VideoManager.HasClip("opening"))
        {
            VideoManager.Play("opening", () =>
            {
                MVPGameSession.MarkOpeningCompleted();
                ShowAIIntroPopup();
            });
            return;
        }

        StartOpeningNarrative();
    }

    /// <summary>AI 弹窗介绍游戏（新增独立弹窗，不修改现有叙事卡）。</summary>
    private void ShowAIIntroPopup()
    {
        GameFlowManager.SetStage(FlowStage.AIIntroPopup, "opening done");
        PopupManager.Show(
            PopupRequests.AIIntro(MVPGameSession.PlayerName),
            PlayActOneVideo
        );
    }

    /// <summary>第一幕视频（占位自动完成）。</summary>
    private void PlayActOneVideo()
    {
        GameFlowManager.SetStage(FlowStage.ActOneVideo, "ai intro closed");
        VideoManager.Play("act_one", ShowGameIntroPopup);
    }

    /// <summary>游戏介绍弹窗，确认后完成开场并进入空间站。</summary>
    private void ShowGameIntroPopup()
    {
        GameFlowManager.SetStage(FlowStage.GameIntroPopup, "act one done");
        PopupManager.Show(PopupRequests.GameIntro(), () =>
        {
            SaveManager.TrySave();
            SceneTransitionManager.EnterStationHub();
        });
    }

    private void StartFirstReturnNarrative()
    {
        GameFlowManager.SetStage(FlowStage.HolidayVideo, "first return");

        // 接入 holiday.mp4（存在则播放并跳过返航叙事卡；缺失则回退既有叙事卡）。
        if (VideoManager.HasClip("holiday"))
        {
            VideoManager.Play("holiday", () =>
            {
                MVPGameSession.CompleteFirstReturn();
                ShowHolidayFlow();
            });
            return;
        }

        NarrativeSlide[] slides =
        {
            new(
                "本月最后一个工作日",
                "三项修复任务已经完成。返程飞船与空间站完成对接，" +
                "舱门在气密提示音中缓缓关闭。",
                "归墟同步轨道  ·  返航准备"
            ),
            new(
                "返航诺亚",
                "飞船离开归墟同步轨道。远方的诺亚星逐渐占满舷窗，" +
                "城市、森林与湖泊重新出现在视野中。",
                "诺亚航线  ·  家园信标已锁定"
            ),
            new(
                "休假生活已解锁",
                "欢迎 " + MVPGameSession.PlayerName +
                " 修复官回到诺亚。休假期间可以完成地球动植物拼图、" +
                "解锁图鉴，并继续积累地球修复进度。",
                "诺亚生活  ·  生物图鉴已上线"
            )
        };

        PlayNarrative(
            "RETURN TO NOAH",
            slides,
            () =>
            {
                MVPGameSession.CompleteFirstReturn();
                ShowHolidayFlow();
            }
        );
    }

    private void ShowHolidayFlow()
    {
        GameFlowManager.SetStage(FlowStage.PuzzleGame, "holiday hub");
        ShowOnly(null);
        holidayUI.Show(
            () =>
            {
                GameFlowManager.SetStage(FlowStage.FreePlay, "holiday depart");
                MVPGameSession.BeginNextWorkday();
                SceneTransitionManager.EnterStationHub();
            }
        );
    }

    private void ShowEndingFlow()
    {
        GameFlowManager.SetStage(FlowStage.EndingChoice, "final choice");
        ShowOnly(null);
        endingUI.Show(() =>
        {
            GameFlowManager.SetStage(FlowStage.MainMenu, "ending back");
            ShowOnly(mainPanel);
        });
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

        if (holidayUI != null)
        {
            holidayUI.gameObject.SetActive(false);
        }

        if (endingUI != null)
        {
            endingUI.gameObject.SetActive(false);
        }

        if (target == mainPanel)
        {
            UpdateMainStatus();
        }
    }

    private void UpdateMainStatus()
    {
        if (mainStatusText == null)
        {
            return;
        }

        string ending = MVPGameSession.EndingCompleted
            ? "\n最终档案  " + GetEndingLabel(MVPGameSession.EndingChoice)
            : string.Empty;
        mainStatusText.text =
            "归墟同步轨道空间站  ·  ONLINE\n地球修复进度  " +
            MVPGameSession.EarthProgress + " / " +
            MVPGameSession.EndingProgress + ending;
    }

    private static string GetEndingLabel(FinalEndingChoice choice)
    {
        return choice == FinalEndingChoice.EndRestorationProgram
            ? "把未来交给生命"
            : choice == FinalEndingChoice.SacrificeForEarth
                ? "第七十九任的最后航程"
                : "尚未选择";
    }

    private static GameObject CreateFullPanel(string name, Transform parent)
    {
        RectTransform rect = UIFactory.CreateRect(name, parent);
        UIFactory.Stretch(rect);
        return rect.gameObject;
    }

    private static T CreateFlowModule<T>(string name, Transform parent)
        where T : MonoBehaviour
    {
        GameObject root = CreateFullPanel(name, parent);
        return root.AddComponent<T>();
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
        image.raycastTarget = false;
        return image;
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
