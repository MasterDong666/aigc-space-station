using System;
using System.Collections.Generic;
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
    private GameObject savePanel;
    private GameObject narrativePanel;
    private GameObject mainBackdropRoot;
    private GameObject secondaryBackdropRoot;
    private InputField nameInput;
    private Text profileHint;
    private Text savePlayerName;
    private Text saveProgress;
    private Text saveRouteHint;
    private RawImage saveAvatar;
    private Button loadSaveButton;
    private Button previousSaveButton;
    private Button nextSaveButton;
    private Text saveSlotCounter;
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
    private Text mainProgressPercentText;
    private Image mainProgressFill;
    private NoahHolidayUI holidayUI;
    private EndingChoiceUI endingUI;
    private Button[] avatarButtons;
    private NarrativeSlide[] activeSlides = Array.Empty<NarrativeSlide>();
    private Action narrativeCompleted;
    private int slideIndex;
    private int selectedSaveSlotIndex;
    private List<SaveSlotSummary> availableSaveSlots = new();
    private string selectedAvatar = "RESTORER_A";
    private static readonly string[] AvatarRoleNames =
    {
        "男修复官",
        "女修复官",
        "狗狗修复官",
    };

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
        BuildSavePanel(canvas.transform);
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
        MVPGameSession.ProgressChanged += HandleMainProgressChanged;

        if (mainPanel.activeSelf)
        {
            GameFlowManager.SetStage(FlowStage.MainMenu, "Awake");
        }
    }

    private void OnDestroy()
    {
        MVPGameSession.ProgressChanged -= HandleMainProgressChanged;
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
        mainBackdropRoot = CreateFullPanel("MainBackdrop", parent);
        CinematicUIVisuals.AddBackdrop(
            mainBackdropRoot.transform,
            "FrontendArt/MainHero",
            new Color(0.015f, 0.035f, 0.08f, 0.06f),
            "FrontEndHero"
        );

        secondaryBackdropRoot = CreateFullPanel("SecondaryBackdrop", parent);
        AddSecondaryBackdrop(secondaryBackdropRoot.transform, "FrontEndSecondaryHero");
        secondaryBackdropRoot.SetActive(false);
    }

    private void BuildMainPanel(Transform parent)
    {
        mainPanel = CreateFullPanel("MainMenu", parent);

        Text englishTitle = UIFactory.CreateText(
            "EnglishTitle",
            mainPanel.transform,
            "PROJECT EARTH",
            43,
            UIPalette.TextMain,
            TextAnchor.MiddleCenter
        );
        englishTitle.fontStyle = FontStyle.Bold;
        SetAnchored(
            englishTitle.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -322f),
            new Vector2(720f, 58f)
        );

        mainStatusText = UIFactory.CreateText(
            "EarthProgressLabel",
            mainPanel.transform,
            string.Empty,
            27,
            UIPalette.TextMain,
            TextAnchor.MiddleCenter
        );
        SetAnchored(
            mainStatusText.rectTransform,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 68f),
            new Vector2(700f, 42f)
        );

        Image progressTrack = UIFactory.CreateProgressBar(
            "EarthProgressTrack",
            mainPanel.transform,
            new Vector2(780f, 18f),
            out mainProgressFill
        );
        progressTrack.color = new Color(0.03f, 0.08f, 0.15f, 0.92f);
        mainProgressFill.color = new Color(0.45f, 0.88f, 1f, 1f);
        SetAnchored(
            progressTrack.rectTransform,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(-32f, 28f),
            new Vector2(780f, 16f)
        );
        MiniGameVisuals.Round(progressTrack);

        mainProgressPercentText = UIFactory.CreateText(
            "EarthProgressPercent",
            mainPanel.transform,
            string.Empty,
            24,
            new Color(0.58f, 0.88f, 1f, 1f),
            TextAnchor.MiddleLeft
        );
        SetAnchored(
            mainProgressPercentText.rectTransform,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(405f, 28f),
            new Vector2(110f, 38f)
        );

        Button enter = UIFactory.CreateButton(
            "EnterGame",
            mainPanel.transform,
            "启程  成为修复官  ›",
            new Vector2(460f, 88f),
            new Color(0.96f, 0.20f, 0.06f, 1f),
            32
        );
        SetAnchored(
            enter.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -185f),
            new Vector2(460f, 88f)
        );
        enter.onClick.AddListener(ShowProfile);

        UpdateMainStatus();

        UIManager.Register("MainMenu", mainPanel);
    }

    private void BuildProfilePanel(Transform parent)
    {
        profilePanel = CreateFullPanel("ProfileSetup", parent);
        AddSecondaryBackdrop(profilePanel.transform, "ProfileBackdrop");

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

        for (int i = 0; i < avatarButtons.Length; i++)
        {
            int index = i;
            avatarButtons[i] = UIFactory.CreateButton(
                "Avatar_" + ids[i],
                card.transform,
                string.Empty,
                new Vector2(200f, 210f),
                i == 0 ? UIPalette.AccentDim : UIPalette.PanelLight,
                24
            );
            SetAnchored(
                avatarButtons[i].GetComponent<RectTransform>(),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(72f + i * 225f, -388f),
                new Vector2(200f, 210f)
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
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -10f),
                new Vector2(164f, 164f)
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
                new Vector2(0f, 42f)
            );
            Text avatarLabelText = UIFactory.CreateText(
                "AvatarName",
                labelPill.transform,
                "079-" + (char)('A' + i) + "  ·  " + AvatarRoleNames[i],
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
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -24f),
            new Vector2(432f, 432f)
        );

        avatarPreviewLabel = UIFactory.CreateText(
            "PreviewLabel",
            previewCard.transform,
            "男修复官  ·  079-A\nPERSONNEL RECORD // ACTIVE",
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

        Button openSave = UIFactory.CreateButton(
            "OpenSave",
            card.transform,
            "读取存档",
            new Vector2(260f, 80f),
            new Color(0.10f, 0.43f, 0.55f, 1f),
            27
        );
        SetAnchored(
            openSave.GetComponent<RectTransform>(),
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(482f, 32f),
            new Vector2(260f, 80f)
        );
        openSave.onClick.AddListener(ShowSavePanel);

        UIFactory.CreateBackButton(
            profilePanel.transform,
            () => ShowOnly(mainPanel),
            "ProfileBack"
        );

        UIManager.Register("ProfileSetup", profilePanel);
    }

    private void BuildSavePanel(Transform parent)
    {
        savePanel = CreateFullPanel("SaveSelection", parent);
        AddSecondaryBackdrop(savePanel.transform, "SaveBackdrop");

        Image veil = UIFactory.CreatePanel(
            "SaveVeil",
            savePanel.transform,
            new Color(0.01f, 0.025f, 0.055f, 0.62f)
        );
        UIFactory.Stretch(veil.rectTransform);

        Image card = CinematicUIVisuals.CreateCard(
            "SaveCard",
            savePanel.transform,
            new Color(0.045f, 0.105f, 0.15f, 0.97f),
            new Vector2(1120f, 620f)
        );
        SetAnchored(
            card.rectTransform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(1120f, 620f)
        );

        Text title = UIFactory.CreateText(
            "Title",
            card.transform,
            "选择修复官存档",
            46,
            UIPalette.TextMain,
            TextAnchor.MiddleLeft
        );
        SetAnchored(
            title.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(58f, -42f),
            new Vector2(-116f, 70f)
        );

        previousSaveButton = UIFactory.CreateButton(
            "PreviousSave",
            card.transform,
            "‹ 上一个",
            new Vector2(125f, 52f),
            new Color(0.08f, 0.28f, 0.36f, 1f),
            20
        );
        SetAnchored(
            previousSaveButton.GetComponent<RectTransform>(),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(745f, -50f),
            new Vector2(125f, 52f)
        );
        previousSaveButton.onClick.AddListener(() => ChangeSaveSlot(-1));

        saveSlotCounter = UIFactory.CreateText(
            "SaveSlotCounter",
            card.transform,
            "0 / 0",
            20,
            UIPalette.TextDim,
            TextAnchor.MiddleCenter
        );
        SetAnchored(
            saveSlotCounter.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(875f, -50f),
            new Vector2(90f, 52f)
        );

        nextSaveButton = UIFactory.CreateButton(
            "NextSave",
            card.transform,
            "下一个 ›",
            new Vector2(125f, 52f),
            new Color(0.08f, 0.28f, 0.36f, 1f),
            20
        );
        SetAnchored(
            nextSaveButton.GetComponent<RectTransform>(),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(970f, -50f),
            new Vector2(125f, 52f)
        );
        nextSaveButton.onClick.AddListener(() => ChangeSaveSlot(1));

        Image slot = CinematicUIVisuals.CreateCard(
            "SaveSlot",
            card.transform,
            new Color(0.02f, 0.07f, 0.105f, 0.98f),
            new Vector2(1000f, 350f)
        );
        SetAnchored(
            slot.rectTransform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -18f),
            new Vector2(1000f, 350f)
        );

        saveAvatar = CreateRawImage(
            "SaveAvatar",
            slot.transform,
            avatarTextures[0],
            Color.white
        );
        SetAnchored(
            saveAvatar.rectTransform,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(30f, 0f),
            new Vector2(290f, 290f)
        );

        savePlayerName = UIFactory.CreateText(
            "SavePlayerName",
            slot.transform,
            string.Empty,
            36,
            Color.white,
            TextAnchor.MiddleLeft
        );
        SetAnchored(
            savePlayerName.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(355f, -45f),
            new Vector2(-390f, 58f)
        );

        saveProgress = UIFactory.CreateText(
            "SaveProgress",
            slot.transform,
            string.Empty,
            25,
            UIPalette.TextDim,
            TextAnchor.UpperLeft
        );
        SetAnchored(
            saveProgress.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(355f, -125f),
            new Vector2(-390f, 105f)
        );

        saveRouteHint = UIFactory.CreateText(
            "SaveRouteHint",
            slot.transform,
            string.Empty,
            21,
            new Color(0.45f, 0.88f, 0.82f, 1f),
            TextAnchor.UpperLeft
        );
        SetAnchored(
            saveRouteHint.rectTransform,
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(355f, 40f),
            new Vector2(-390f, 82f)
        );

        loadSaveButton = UIFactory.CreateButton(
            "LoadSave",
            slot.transform,
            "进入此存档  ›",
            new Vector2(300f, 84f),
            new Color(0.10f, 0.55f, 0.48f, 1f),
            26
        );
        SetAnchored(
            loadSaveButton.GetComponent<RectTransform>(),
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(-35f, 0f),
            new Vector2(300f, 84f)
        );
        loadSaveButton.onClick.AddListener(LoadSelectedSave);

        Text note = UIFactory.CreateText(
            "SaveNote",
            card.transform,
            "读取存档将跳过已经完成的前置剧情。",
            19,
            UIPalette.TextDim,
            TextAnchor.MiddleCenter
        );
        SetAnchored(
            note.rectTransform,
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 24f),
            new Vector2(0f, 42f)
        );

        UIFactory.CreateBackButton(
            savePanel.transform,
            ShowProfile,
            "SaveSelectionBack"
        );
        UIManager.Register("SaveSelection", savePanel);
    }

    private void BuildNarrativePanel(Transform parent)
    {
        narrativePanel = CreateFullPanel("Narrative", parent);
        AddSecondaryBackdrop(narrativePanel.transform, "NarrativeBackdrop");

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

        UIFactory.CreateBackButton(
            narrativePanel.transform,
            BackNarrative,
            "NarrativeBack"
        );

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
        if (MVPGameSession.HasPlayerProfile)
        {
            selectedAvatar = MVPGameSession.AvatarId;
            int avatarIndex = selectedAvatar == "RESTORER_B"
                ? 1
                : selectedAvatar == "RESTORER_C" ? 2 : 0;
            SelectAvatar(selectedAvatar, avatarIndex);
        }
        ShowOnly(profilePanel);
    }

    private void ShowSavePanel()
    {
        availableSaveSlots = SaveManager.GetSaveSlots();
        selectedSaveSlotIndex = 0;
        RefreshSelectedSaveSlot();
        ShowOnly(savePanel);
    }

    private void ChangeSaveSlot(int direction)
    {
        if (availableSaveSlots.Count <= 1)
        {
            return;
        }

        selectedSaveSlotIndex = (
            selectedSaveSlotIndex + direction + availableSaveSlots.Count
        ) % availableSaveSlots.Count;
        RefreshSelectedSaveSlot();
    }

    private void RefreshSelectedSaveSlot()
    {
        bool available = availableSaveSlots.Count > 0;
        SaveSlotSummary slot = available
            ? availableSaveSlots[selectedSaveSlotIndex]
            : default;

        savePlayerName.text = available
            ? slot.PlayerName + " 修复官"
            : "暂无可读取的存档";
        saveProgress.text = available
            ? "地球修复进度  " + slot.EarthProgress + " / " +
              MVPGameSession.EndingProgress + "\n第 " + slot.Workday +
              " 工作日"
            : "完成身份创建后，游戏会自动保存为独立存档。";
        saveRouteHint.text = available
            ? (slot.EarthProgress >= MVPGameSession.EndingProgress
                ? "读取后直接进入最终结局选择"
                : "读取后直接返回空间站继续每日任务")
            : string.Empty;
        saveAvatar.texture = available
            ? GetAvatarTexture(slot.AvatarId)
            : avatarTextures[0];
        saveAvatar.color = available
            ? Color.white
            : new Color(1f, 1f, 1f, 0.18f);
        loadSaveButton.interactable = available;
        previousSaveButton.interactable = availableSaveSlots.Count > 1;
        nextSaveButton.interactable = availableSaveSlots.Count > 1;
        saveSlotCounter.text = available
            ? (selectedSaveSlotIndex + 1) + " / " + availableSaveSlots.Count
            : "0 / 0";
    }

    private Texture2D GetAvatarTexture(string avatarId)
    {
        int index = avatarId == "RESTORER_B"
            ? 1
            : avatarId == "RESTORER_C" ? 2 : 0;
        return avatarTextures[index];
    }

    private void LoadSelectedSave()
    {
        if (availableSaveSlots.Count == 0)
        {
            ShowSavePanel();
            return;
        }

        string saveId = availableSaveSlots[selectedSaveSlotIndex].SaveId;
        if (
            !SaveManager.TryLoadIntoSession(saveId) ||
            !MVPGameSession.HasPlayerProfile
        )
        {
            ShowSavePanel();
            return;
        }

        if (MVPGameSession.EarthProgress >= MVPGameSession.EndingProgress)
        {
            ShowEndingChoiceFromSave();
            return;
        }

        SceneTransitionManager.EnterStationHub();
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

        MVPGameSession.ResetAllProgress();
        SaveManager.BeginNewSave();
        MVPGameSession.SetPlayerProfile(enteredName, selectedAvatar);
        SaveManager.TrySave();
        StartOpeningSequence();
    }

    private void SelectAvatar(string id, int selectedIndex)
    {
        selectedAvatar = id;
        avatarPreview.texture = avatarTextures[selectedIndex];
        avatarPreviewLabel.text =
            AvatarRoleNames[selectedIndex] + "  ·  079-" +
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

    /// <summary>开场序列：序幕视频/叙事卡 →（AI 弹窗在完成后弹出）。</summary>
    private void StartOpeningSequence()
    {
        // 身份确认后立刻退出档案界面。后续视频与弹窗只叠加在公共背景上，
        // 避免仍可看到或误触创建角色面板。
        ShowOnly(null);
        GameFlowManager.SetStage(FlowStage.OpeningVideo, "confirm profile");
        PlayOpeningVideoOrNarrative();
    }

    /// <summary>
    /// 开场叙事入口：接入 opening.mp4（存在则播放并跳过叙事卡；缺失则回退既有叙事卡）。
    /// 这样团队放入正序视频后自动替换，无需改代码；无视频时流程照常推进。
    /// </summary>
    private void PlayOpeningVideoOrNarrative()
    {
        if (VideoManager.HasClip("opening"))
        {
            VideoManager.Play(
                "opening",
                () =>
                {
                    MVPGameSession.MarkOpeningCompleted();
                    ShowAIIntroPopup();
                },
                ShowProfile
            );
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
            PlayActOneVideo,
            PlayOpeningVideoOrNarrative
        );
    }

    /// <summary>第一幕视频（占位自动完成）。</summary>
    private void PlayActOneVideo()
    {
        GameFlowManager.SetStage(FlowStage.ActOneVideo, "ai intro closed");
        VideoManager.Play(
            "act_one",
            ShowGameIntroPopup,
            ShowAIIntroPopup
        );
    }

    /// <summary>游戏介绍弹窗，确认后完成开场并进入空间站。</summary>
    private void ShowGameIntroPopup()
    {
        GameFlowManager.SetStage(FlowStage.GameIntroPopup, "act one done");
        PopupManager.Show(
            PopupRequests.GameIntro(),
            () =>
            {
                SaveManager.TrySave();
                SceneTransitionManager.EnterStationHub();
            },
            PlayActOneVideo
        );
    }

    private void StartFirstReturnNarrative()
    {
        GameFlowManager.SetStage(FlowStage.HolidayVideo, "first return");
        ShowOnly(null);
        PlayActTwoVideo();
    }

    private void PlayActTwoVideo()
    {
        VideoManager.Play(
            "act_two",
            ShowHolidayIntroPopup,
            StartFirstReturnNarrative
        );
    }

    private void ShowHolidayIntroPopup()
    {
        MVPGameSession.CompleteFirstReturn();
        SaveManager.TrySave();
        GameFlowManager.SetStage(FlowStage.HolidayPopup, "act two done");
        PopupManager.Show(
            PopupRequests.HolidayIntro(MVPGameSession.PlayerName),
            ShowHolidayFlow,
            PlayActTwoVideo
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
            },
            ShowHolidayIntroPopup
        );
    }

    private void ShowEndingFlow()
    {
        GameFlowManager.SetStage(FlowStage.EndingChoice, "final choice");
        ShowOnly(null);
        endingUI.Show(
            () =>
            {
                GameFlowManager.SetStage(FlowStage.MainMenu, "ending back");
                ShowOnly(mainPanel);
            },
            () => SceneTransitionManager.EnterStationHub()
        );
    }

    private void ShowEndingChoiceFromSave()
    {
        GameFlowManager.SetStage(FlowStage.EndingChoice, "load completed save");
        ShowOnly(null);
        endingUI.ShowChoiceDirect(
            () =>
            {
                GameFlowManager.SetStage(FlowStage.MainMenu, "ending back");
                ShowOnly(mainPanel);
            },
            () => SceneTransitionManager.EnterStationHub()
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

    private void BackNarrative()
    {
        if (slideIndex > 0)
        {
            slideIndex--;
            PresentSlide();
            return;
        }

        narrativeCompleted = null;
        ShowProfile();
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
        if (mainBackdropRoot != null)
        {
            mainBackdropRoot.SetActive(target == mainPanel);
        }

        if (secondaryBackdropRoot != null)
        {
            secondaryBackdropRoot.SetActive(target == null);
        }

        mainPanel.SetActive(target == mainPanel);
        profilePanel.SetActive(target == profilePanel);
        savePanel.SetActive(target == savePanel);
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
        if (
            mainStatusText == null ||
            mainProgressFill == null ||
            mainProgressPercentText == null
        )
        {
            return;
        }

        int progress = Mathf.Clamp(
            MVPGameSession.EarthProgress,
            0,
            MVPGameSession.EndingProgress
        );
        float normalized = progress / (float)MVPGameSession.EndingProgress;
        mainStatusText.text =
            "地球修复进度  " + progress + " / " +
            MVPGameSession.EndingProgress;
        UIFactory.SetProgressFill(mainProgressFill, normalized);
        mainProgressPercentText.text = Mathf.RoundToInt(normalized * 100f) + "%";
    }

    private void HandleMainProgressChanged(int progress)
    {
        UpdateMainStatus();
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

    private static void AddSecondaryBackdrop(Transform parent, string name)
    {
        CinematicUIVisuals.AddBackdrop(
            parent,
            "FrontendArt/SecondaryHero",
            new Color(0.01f, 0.025f, 0.05f, 0.18f),
            name
        );
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
