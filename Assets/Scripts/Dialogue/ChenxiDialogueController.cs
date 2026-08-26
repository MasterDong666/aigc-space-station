using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Presents authored dialogue for the station AI, Chenxi. It intentionally
/// contains no online AI dependency so the competition demo remains stable.
/// </summary>
public class ChenxiDialogueController : MonoBehaviour
{
    [Header("底部对话 UI")]
    [SerializeField] private GameObject dialogueRoot;
    [SerializeField] private Image portraitImage;
    [SerializeField] private Text speakerText;
    [SerializeField] private Text bodyText;
    [SerializeField] private Text continueHintText;
    [SerializeField] private Button advanceButton;
    [SerializeField] private Sprite defaultPortrait;

    [Header("晨曦语音")]
    [SerializeField] private AudioSource voiceSource;
    [SerializeField, Range(0f, 1f)] private float voiceVolume = 0.86f;

    [Header("场景引用")]
    [SerializeField] private PlayerInteractor playerInteractor;
    [SerializeField] private MVPFlowController flowController;

    [Header("晨曦对话序列")]
    [SerializeField] private DialogueSequence introSequence;
    [SerializeField] private DialogueSequence orbitCompleteSequence;
    [SerializeField] private DialogueSequence ecologyCompleteSequence;
    [SerializeField] private DialogueSequence geneCompleteSequence;
    [SerializeField] private DialogueSequence allTasksCompleteSequence;

    [Header("显示节奏")]
    [SerializeField] private float characterInterval = 0.018f;

    private static Font runtimeChineseFont;

    private readonly Queue<DialogueSequence> pendingSequences = new();
    private DialogueSequence currentSequence;
    private int currentLineIndex;
    private Coroutine typewriterCoroutine;
    private bool isTyping;
    private bool ownsPlayerLock;

    public bool IsVisible =>
        dialogueRoot != null && dialogueRoot.activeSelf;

    public string CurrentSequenceId =>
        currentSequence == null
            ? string.Empty
            : currentSequence.SequenceId;

    public string CurrentBodyText =>
        bodyText == null ? string.Empty : bodyText.text;

    public bool IsVoicePlaying =>
        voiceSource != null && voiceSource.isPlaying;

    public string CurrentVoiceClipName =>
        voiceSource == null || voiceSource.clip == null
            ? string.Empty
            : voiceSource.clip.name;

    public float CurrentVoicePlaybackTime =>
        voiceSource == null ? 0f : voiceSource.time;

    private void Awake()
    {
        ResolveSceneReferences();
        ApplyRuntimeFont();

        if (voiceSource != null)
        {
            voiceSource.playOnAwake = false;
            voiceSource.loop = false;
            voiceSource.spatialBlend = 0f;
            voiceSource.volume = voiceVolume;
        }

        if (portraitImage != null)
        {
            portraitImage.sprite = defaultPortrait;
            portraitImage.preserveAspect = true;
        }

        if (advanceButton != null)
        {
            advanceButton.onClick.AddListener(Advance);
        }

        HideImmediately();
    }

    private void OnEnable()
    {
        ResolveSceneReferences();

        if (flowController != null)
        {
            flowController.TaskCompleted += HandleTaskCompleted;
            flowController.AllTasksCompleted += HandleAllTasksCompleted;
        }
    }

    private void Start()
    {
        ShowSequence(introSequence);
    }

    private void Update()
    {
        if (!IsVisible)
        {
            return;
        }

        if (
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter)
        )
        {
            Advance();
        }
    }

    private void OnDisable()
    {
        if (flowController != null)
        {
            flowController.TaskCompleted -= HandleTaskCompleted;
            flowController.AllTasksCompleted -= HandleAllTasksCompleted;
        }

        StopVoice();
        ReleaseOwnedPlayerLock();
    }

    private void OnDestroy()
    {
        if (advanceButton != null)
        {
            advanceButton.onClick.RemoveListener(Advance);
        }
    }

    public void ShowSequence(DialogueSequence sequence)
    {
        if (sequence == null || sequence.Lines.Length == 0)
        {
            return;
        }

        if (currentSequence != null)
        {
            pendingSequences.Enqueue(sequence);
            return;
        }

        BeginSequence(sequence);
    }

    public void Advance()
    {
        if (currentSequence == null)
        {
            return;
        }

        if (isTyping)
        {
            FinishCurrentLineImmediately();
            return;
        }

        currentLineIndex++;

        if (currentLineIndex < currentSequence.Lines.Length)
        {
            PresentCurrentLine();
            return;
        }

        EndCurrentSequence();
    }

    public void ConfigureUI(
        GameObject root,
        Image portrait,
        Text speaker,
        Text body,
        Text continueHint,
        Button button,
        Sprite portraitSprite
    )
    {
        dialogueRoot = root;
        portraitImage = portrait;
        speakerText = speaker;
        bodyText = body;
        continueHintText = continueHint;
        advanceButton = button;
        defaultPortrait = portraitSprite;
    }

    public void ConfigureSceneReferences(
        PlayerInteractor interactor,
        MVPFlowController flow
    )
    {
        playerInteractor = interactor;
        flowController = flow;
    }

    public void ConfigureVoice(AudioSource source, float volume = 0.86f)
    {
        voiceSource = source;
        voiceVolume = Mathf.Clamp01(volume);

        if (voiceSource == null)
        {
            return;
        }

        voiceSource.playOnAwake = false;
        voiceSource.loop = false;
        voiceSource.spatialBlend = 0f;
        voiceSource.volume = voiceVolume;
    }

    public void ConfigureSequences(
        DialogueSequence intro,
        DialogueSequence orbitComplete,
        DialogueSequence ecologyComplete,
        DialogueSequence geneComplete,
        DialogueSequence allComplete
    )
    {
        introSequence = intro;
        orbitCompleteSequence = orbitComplete;
        ecologyCompleteSequence = ecologyComplete;
        geneCompleteSequence = geneComplete;
        allTasksCompleteSequence = allComplete;
    }

    public bool ReplayCurrentVoiceForDevelopment()
    {
        if (
            currentSequence == null ||
            currentLineIndex < 0 ||
            currentLineIndex >= currentSequence.Lines.Length
        )
        {
            return false;
        }

        PlayVoiceForLine(currentSequence.Lines[currentLineIndex]);
        return IsVoicePlaying;
    }

    private void HandleTaskCompleted(MiniGameId id)
    {
        DialogueSequence sequence = id switch
        {
            MiniGameId.OrbitInspection => orbitCompleteSequence,
            MiniGameId.EcologyDeployment => ecologyCompleteSequence,
            MiniGameId.GeneCultivation => geneCompleteSequence,
            _ => null
        };

        ShowSequence(sequence);
    }

    private void HandleAllTasksCompleted()
    {
        ShowSequence(allTasksCompleteSequence);
    }

    private void BeginSequence(DialogueSequence sequence)
    {
        currentSequence = sequence;
        currentLineIndex = 0;

        if (dialogueRoot != null)
        {
            dialogueRoot.SetActive(true);
        }

        if (
            sequence.LockPlayer &&
            playerInteractor != null &&
            !playerInteractor.IsPlayerControlLocked
        )
        {
            ownsPlayerLock = true;
            playerInteractor.SetPlayerControlLocked(true);
        }

        PresentCurrentLine();
    }

    private void PresentCurrentLine()
    {
        if (
            currentSequence == null ||
            currentLineIndex < 0 ||
            currentLineIndex >= currentSequence.Lines.Length
        )
        {
            return;
        }

        DialogueLine line = currentSequence.Lines[currentLineIndex];

        if (speakerText != null)
        {
            speakerText.text = string.IsNullOrWhiteSpace(line.Speaker)
                ? "晨曦"
                : line.Speaker;
        }

        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }

        PlayVoiceForLine(line);
        typewriterCoroutine = StartCoroutine(TypeLine(line.Text));
    }

    private IEnumerator TypeLine(string fullText)
    {
        isTyping = true;

        if (bodyText != null)
        {
            bodyText.text = string.Empty;
        }

        if (continueHintText != null)
        {
            continueHintText.text = "点击显示完整内容";
        }

        string safeText = fullText ?? string.Empty;
        float interval = Mathf.Max(0f, characterInterval);

        for (int index = 0; index < safeText.Length; index++)
        {
            if (bodyText != null)
            {
                bodyText.text += safeText[index];
            }

            if (interval > 0f)
            {
                yield return new WaitForSecondsRealtime(interval);
            }
        }

        isTyping = false;
        typewriterCoroutine = null;
        UpdateContinueHint();
    }

    private void FinishCurrentLineImmediately()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        isTyping = false;

        if (
            bodyText != null &&
            currentSequence != null &&
            currentLineIndex >= 0 &&
            currentLineIndex < currentSequence.Lines.Length
        )
        {
            bodyText.text = currentSequence.Lines[currentLineIndex].Text;
        }

        UpdateContinueHint();
    }

    private void UpdateContinueHint()
    {
        if (continueHintText == null || currentSequence == null)
        {
            return;
        }

        bool hasNextLine =
            currentLineIndex + 1 < currentSequence.Lines.Length;

        continueHintText.text = hasNextLine
            ? "点击或空格继续  ›"
            : "点击或空格关闭  ×";
    }

    private void EndCurrentSequence()
    {
        StopVoice();
        currentSequence = null;
        currentLineIndex = 0;

        if (pendingSequences.Count > 0)
        {
            BeginSequence(pendingSequences.Dequeue());
            return;
        }

        HideImmediately();
        ReleaseOwnedPlayerLock();
    }

    private void HideImmediately()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        isTyping = false;
        StopVoice();

        if (dialogueRoot != null)
        {
            dialogueRoot.SetActive(false);
        }
    }

    private void ReleaseOwnedPlayerLock()
    {
        if (ownsPlayerLock && playerInteractor != null)
        {
            playerInteractor.SetPlayerControlLocked(false);
        }

        ownsPlayerLock = false;
    }

    private void ResolveSceneReferences()
    {
        if (playerInteractor == null)
        {
            playerInteractor = FindObjectOfType<PlayerInteractor>();
        }

        if (flowController == null)
        {
            flowController = FindObjectOfType<MVPFlowController>();
        }
    }

    private void PlayVoiceForLine(DialogueLine line)
    {
        if (voiceSource == null)
        {
            return;
        }

        voiceSource.Stop();
        voiceSource.clip = line.VoiceClip;
        voiceSource.volume = voiceVolume;

        if (voiceSource.clip != null)
        {
            voiceSource.Play();
        }
    }

    private void StopVoice()
    {
        if (voiceSource == null)
        {
            return;
        }

        voiceSource.Stop();
        voiceSource.clip = null;
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
                32
            );
        }

        if (runtimeChineseFont == null)
        {
            return;
        }

        Text[] dialogueTexts =
        {
            speakerText,
            bodyText,
            continueHintText
        };

        foreach (Text text in dialogueTexts)
        {
            if (text != null)
            {
                text.font = runtimeChineseFont;
            }
        }
    }

    private void OnValidate()
    {
        characterInterval = Mathf.Max(0f, characterInterval);
        voiceVolume = Mathf.Clamp01(voiceVolume);
    }
}
