using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum NoahRemoteVisual
{
    PlanetOverview,
    SchoolConnection
}

[Serializable]
public class NoahRemoteDialogueBeat
{
    public string speaker;

    [TextArea(2, 5)]
    public string line;

    public NoahRemoteVisual visual;
    public bool showPuzzleAction;

    public NoahRemoteDialogueBeat(
        string beatSpeaker,
        string beatLine,
        NoahRemoteVisual beatVisual,
        bool puzzleAction = false
    )
    {
        speaker = beatSpeaker;
        line = beatLine;
        visual = beatVisual;
        showPuzzleAction = puzzleAction;
    }
}

/// <summary>
/// Presents the Noah story as an in-station remote transmission. It owns no
/// mini-game implementation: the puzzle button exposes a UnityEvent so the
/// teammate's module can be connected without changing this narrative UI.
/// </summary>
public class NoahRemoteCommunicationController : MonoBehaviour
{
    [Header("界面")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image sisterPortrait;
    [SerializeField] private GameObject portraitFrame;
    [SerializeField] private Text connectionStatusText;
    [SerializeField] private Text locationText;
    [SerializeField] private Text speakerText;
    [SerializeField] private Text dialogueText;
    [SerializeField] private Text progressText;
    [SerializeField] private Text puzzleStatusText;
    [SerializeField] private Text nextButtonText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button puzzleButton;

    [Header("画面")]
    [SerializeField] private Sprite noahPlanetSprite;
    [SerializeField] private Sprite schoolCitySprite;
    [SerializeField] private Sprite sisterPortraitSprite;

    [Header("场景引用")]
    [SerializeField] private PlayerInteractor playerInteractor;
    [SerializeField] private StationArchiveController archiveController;

    [Header("叙事内容")]
    [SerializeField] private List<NoahRemoteDialogueBeat> beats = new();

    [Header("小游戏接入点")]
    [SerializeField] private UnityEvent onPuzzleRequested = new();

    private static Font runtimeChineseFont;
    private Coroutine fadeRoutine;
    private int currentBeatIndex;
    private bool ownsPlayerLock;
    private bool narrativeCompleted;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
    public bool IsNarrativeCompleted => narrativeCompleted;
    public int CurrentBeatIndex => currentBeatIndex;
    public int BeatCount => beats == null ? 0 : beats.Count;

    private void Awake()
    {
        ResolveSceneReferences();
        ApplyRuntimeFont();
        RegisterButtons();

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private void Update()
    {
        if (!IsOpen)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseTransmission();
        }
        else if (
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.Space)
        )
        {
            AdvanceDialogue();
        }
    }

    private void OnDisable()
    {
        ReleaseOwnedPlayerLock();
    }

    private void OnDestroy()
    {
        UnregisterButtons();
    }

    public void OpenTransmission()
    {
        ResolveSceneReferences();

        if (IsOpen)
        {
            return;
        }

        if (
            playerInteractor != null &&
            playerInteractor.IsPlayerControlLocked
        )
        {
            return;
        }

        currentBeatIndex = 0;

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        if (playerInteractor != null)
        {
            playerInteractor.SetPlayerControlLocked(true);
            ownsPlayerLock = true;
        }

        ShowCurrentBeat();
        StartPanelFade(0f, 1f, 0.28f);
    }

    public void CloseTransmission()
    {
        if (!IsOpen)
        {
            return;
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        ReleaseOwnedPlayerLock();
    }

    public void AdvanceDialogue()
    {
        if (!IsOpen || beats == null || beats.Count == 0)
        {
            return;
        }

        if (currentBeatIndex >= beats.Count - 1)
        {
            CompleteNarrative();
            CloseTransmission();
            return;
        }

        currentBeatIndex++;
        ShowCurrentBeat();
    }

    public void RequestPuzzle()
    {
        if (puzzleStatusText != null)
        {
            puzzleStatusText.text =
                "生物拼图接口已就绪  //  等待小游戏模块绑定";
            puzzleStatusText.color = new Color(0.42f, 1f, 0.78f, 1f);
        }

        onPuzzleRequested.Invoke();
    }

    public void Configure(
        GameObject rootPanel,
        CanvasGroup canvasGroup,
        Image mainBackground,
        Image portrait,
        GameObject portraitContainer,
        Text connectionStatus,
        Text location,
        Text speaker,
        Text dialogue,
        Text progress,
        Text puzzleStatus,
        Text nextLabel,
        Button next,
        Button close,
        Button puzzle,
        Sprite planetSprite,
        Sprite citySprite,
        Sprite portraitSprite,
        PlayerInteractor interactor,
        StationArchiveController archive,
        List<NoahRemoteDialogueBeat> dialogueBeats
    )
    {
        panelRoot = rootPanel;
        panelCanvasGroup = canvasGroup;
        backgroundImage = mainBackground;
        sisterPortrait = portrait;
        portraitFrame = portraitContainer;
        connectionStatusText = connectionStatus;
        locationText = location;
        speakerText = speaker;
        dialogueText = dialogue;
        progressText = progress;
        puzzleStatusText = puzzleStatus;
        nextButtonText = nextLabel;
        nextButton = next;
        closeButton = close;
        puzzleButton = puzzle;
        noahPlanetSprite = planetSprite;
        schoolCitySprite = citySprite;
        sisterPortraitSprite = portraitSprite;
        playerInteractor = interactor;
        archiveController = archive;
        beats = dialogueBeats ?? new List<NoahRemoteDialogueBeat>();
    }

    private void ShowCurrentBeat()
    {
        if (beats == null || beats.Count == 0)
        {
            return;
        }

        currentBeatIndex = Mathf.Clamp(
            currentBeatIndex,
            0,
            beats.Count - 1
        );

        NoahRemoteDialogueBeat beat = beats[currentBeatIndex];
        bool schoolConnection =
            beat.visual == NoahRemoteVisual.SchoolConnection;

        if (backgroundImage != null)
        {
            backgroundImage.sprite = schoolConnection
                ? schoolCitySprite
                : noahPlanetSprite;
        }

        if (portraitFrame != null)
        {
            portraitFrame.SetActive(schoolConnection);
        }

        if (sisterPortrait != null)
        {
            sisterPortrait.sprite = sisterPortraitSprite;
        }

        if (connectionStatusText != null)
        {
            connectionStatusText.text = schoolConnection
                ? "量子链路稳定  ·  实时通讯"
                : "信号同步中  ·  延迟 3.2 光年";
        }

        if (locationText != null)
        {
            locationText.text = schoolConnection
                ? "NOAH // 第七码头学区"
                : "NOAH // 近地轨道观测";
        }

        if (speakerText != null)
        {
            speakerText.text = beat.speaker;
        }

        if (dialogueText != null)
        {
            dialogueText.text = beat.line;
        }

        if (progressText != null)
        {
            progressText.text =
                $"TRANSMISSION  {currentBeatIndex + 1:00}/{beats.Count:00}";
        }

        if (puzzleButton != null)
        {
            puzzleButton.gameObject.SetActive(beat.showPuzzleAction);
        }

        if (puzzleStatusText != null)
        {
            puzzleStatusText.gameObject.SetActive(beat.showPuzzleAction);
            puzzleStatusText.text = beat.showPuzzleAction
                ? "外部模块插槽：NOAH_BIO_PUZZLE"
                : string.Empty;
            puzzleStatusText.color = new Color(0.46f, 0.72f, 0.76f, 1f);
        }

        if (nextButtonText != null)
        {
            nextButtonText.text =
                currentBeatIndex >= beats.Count - 1
                    ? "结束通讯"
                    : "继续  ›";
        }

        if (currentBeatIndex >= beats.Count - 1)
        {
            CompleteNarrative();
        }
    }

    private void CompleteNarrative()
    {
        if (narrativeCompleted)
        {
            return;
        }

        narrativeCompleted = true;
        ResolveSceneReferences();

        if (archiveController != null)
        {
            archiveController.UnlockLegacyOfficerArchives();
        }
    }

    private void ResolveSceneReferences()
    {
        if (playerInteractor == null)
        {
            playerInteractor = FindObjectOfType<PlayerInteractor>();
        }

        if (archiveController == null)
        {
            archiveController = FindObjectOfType<StationArchiveController>();
        }
    }

    private void RegisterButtons()
    {
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(AdvanceDialogue);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseTransmission);
        }

        if (puzzleButton != null)
        {
            puzzleButton.onClick.AddListener(RequestPuzzle);
        }
    }

    private void UnregisterButtons()
    {
        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(AdvanceDialogue);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseTransmission);
        }

        if (puzzleButton != null)
        {
            puzzleButton.onClick.RemoveListener(RequestPuzzle);
        }
    }

    private void ReleaseOwnedPlayerLock()
    {
        if (!ownsPlayerLock)
        {
            return;
        }

        if (playerInteractor != null)
        {
            playerInteractor.SetPlayerControlLocked(false);
        }

        ownsPlayerLock = false;
    }

    private void StartPanelFade(float from, float to, float duration)
    {
        if (panelCanvasGroup == null)
        {
            return;
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(
            FadeCanvas(panelCanvasGroup, from, to, duration)
        );
    }

    private static IEnumerator FadeCanvas(
        CanvasGroup group,
        float from,
        float to,
        float duration
    )
    {
        float elapsed = 0f;
        group.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        group.alpha = to;
    }

    private void ApplyRuntimeFont()
    {
        if (runtimeChineseFont == null)
        {
            runtimeChineseFont = Font.CreateDynamicFontFromOSFont(
                new[]
                {
                    "PingFang SC",
                    "Hiragino Sans GB",
                    "Arial"
                },
                34
            );
        }

        if (runtimeChineseFont == null)
        {
            return;
        }

        foreach (Text text in GetComponentsInChildren<Text>(true))
        {
            text.font = runtimeChineseFont;
        }
    }
}
