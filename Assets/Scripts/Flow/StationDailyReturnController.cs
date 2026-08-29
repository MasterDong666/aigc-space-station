using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Adds the end-of-workday hand-off to the station without editing the
/// completed 3D scene. It waits for Chenxi's queued task dialogue to release
/// player control, then offers the first return-to-Noah transition.
/// </summary>
public class StationDailyReturnController : MonoBehaviour
{
    private MVPFlowController flowController;
    private PlayerInteractor playerInteractor;
    private GameObject panelRoot;
    private Text eyebrowText;
    private Text titleText;
    private Text bodyText;
    private Text actionLabel;
    private Button actionButton;
    private bool ownsPlayerLock;
    private bool showQueued;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterInstaller()
    {
        SceneManager.sceneLoaded -= InstallForScene;
        SceneManager.sceneLoaded += InstallForScene;
    }

    private static void InstallForScene(Scene scene, LoadSceneMode mode)
    {
        if (scene.path != SceneTransitionManager.StationHubScenePath)
        {
            return;
        }

        if (FindObjectOfType<StationDailyReturnController>() != null)
        {
            return;
        }

        GameObject root = new("MVP_DailyReturnFlow");
        root.AddComponent<StationDailyReturnController>();
    }

    private void Awake()
    {
        ResolveReferences();
        BuildUI();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (flowController != null)
        {
            flowController.AllTasksCompleted += HandleAllTasksCompleted;
        }
    }

    private void Start()
    {
        if (
            flowController != null &&
            flowController.IsFlowComplete &&
            MVPGameSession.IsCurrentDayComplete
        )
        {
            QueueReturnPanel();
        }
    }

    private void OnDisable()
    {
        if (flowController != null)
        {
            flowController.AllTasksCompleted -= HandleAllTasksCompleted;
        }

        ReleasePlayerLock();
    }

    private void OnDestroy()
    {
        if (actionButton != null)
        {
            actionButton.onClick.RemoveListener(HandleAction);
        }
    }

    private void HandleAllTasksCompleted()
    {
        QueueReturnPanel();
    }

    private void QueueReturnPanel()
    {
        if (showQueued || (panelRoot != null && panelRoot.activeSelf))
        {
            return;
        }

        showQueued = true;
        StartCoroutine(ShowAfterDialogue());
    }

    private IEnumerator ShowAfterDialogue()
    {
        yield return new WaitForSecondsRealtime(0.4f);

        while (
            playerInteractor != null &&
            playerInteractor.IsPlayerControlLocked
        )
        {
            yield return null;
        }

        showQueued = false;
        ShowReturnPanel();
    }

    private void ShowReturnPanel()
    {
        bool firstReturn = !MVPGameSession.FirstReturnCompleted;
        bool endingReady =
            MVPGameSession.IsEndingUnlocked &&
            !MVPGameSession.EndingCompleted;

        eyebrowText.text = endingReady
            ? "CHENXI // 最终协议已解锁"
            : firstReturn
            ? "CHENXI // 月度工作周期完成"
            : $"CHENXI // 第 {MVPGameSession.Workday} 工作日完成";
        titleText.text = endingReady
            ? "地球重塑计划等待最终指令"
            : firstReturn
            ? "返程飞船已经抵达"
            : "今日修复任务已结算";
        bodyText.text = endingReady
            ? "地球修复进度已经达到 50。星际轨道、生态投放与基因播撒" +
              "共同触发了最终协议。晨曦已准备好结局前传和最后的选择。\n\n" +
              "这是不可回避的决定，但你可以在选择前完整阅读两份方案。"
            : firstReturn
            ? "恭喜 " + MVPGameSession.PlayerName +
              " 修复官完成今日三项任务。今天也是本月最后一个工作日，" +
              "返程飞船已停靠空间站，你可以随时返回诺亚星。\n\n" +
              "当前地球修复进度：" + MVPGameSession.EarthProgress + " / " +
              MVPGameSession.EndingProgress
            : "本工作日三项任务已经全部完成，修复数据已写入长期档案。" +
              "开始下一工作日后，三个终端会重新开放。\n\n" +
              "当前地球修复进度：" + MVPGameSession.EarthProgress + " / " +
              MVPGameSession.EndingProgress;
        actionLabel.text = endingReady
            ? "进入最终选择  ›"
            : firstReturn
                ? "去返程  ›"
                : "开始下一工作日  ›";

        panelRoot.SetActive(true);
        ResolveReferences();

        if (
            playerInteractor != null &&
            !playerInteractor.IsPlayerControlLocked
        )
        {
            playerInteractor.SetPlayerControlLocked(true);
            ownsPlayerLock = true;
        }
    }

    private void HandleAction()
    {
        if (
            MVPGameSession.IsEndingUnlocked &&
            !MVPGameSession.EndingCompleted
        )
        {
            MVPGameSession.RequestNarrative(GameNarrativeRoute.FinalChoice);

            if (!SceneTransitionManager.EnterFrontEnd())
            {
                MVPGameSession.RequestNarrative(GameNarrativeRoute.None);
            }

            return;
        }

        if (!MVPGameSession.FirstReturnCompleted)
        {
            if (!MVPGameSession.TryStartFirstReturn())
            {
                return;
            }

            MVPGameSession.RequestNarrative(
                GameNarrativeRoute.FirstReturnToNoah
            );

            if (!SceneTransitionManager.EnterFrontEnd())
            {
                MVPGameSession.CancelFirstReturn();
            }

            return;
        }

        if (flowController != null)
        {
            flowController.BeginNextWorkday();
        }
        else
        {
            MVPGameSession.BeginNextWorkday();
        }

        panelRoot.SetActive(false);
        ReleasePlayerLock();
    }

    private void ResolveReferences()
    {
        if (flowController == null)
        {
            flowController = FindObjectOfType<MVPFlowController>();
        }

        if (playerInteractor == null)
        {
            playerInteractor = FindObjectOfType<PlayerInteractor>();
        }
    }

    private void BuildUI()
    {
        Canvas canvas = UIFactory.CreateCanvas("DailyReturnCanvas");
        canvas.sortingOrder = 280;
        canvas.transform.SetParent(transform, false);

        RectTransform panelRect = UIFactory.CreateRect(
            "ReturnPanel",
            canvas.transform
        );
        UIFactory.Stretch(panelRect);
        panelRoot = panelRect.gameObject;

        Image overlay = panelRoot.AddComponent<Image>();
        overlay.color = new Color(0.002f, 0.008f, 0.018f, 0.82f);

        Image card = UIFactory.CreatePanel(
            "ReturnCard",
            panelRoot.transform,
            new Color(0.018f, 0.085f, 0.13f, 0.98f)
        );
        SetAnchored(
            card.rectTransform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(1120f, 620f)
        );

        Image accent = UIFactory.CreatePanel(
            "TopAccent",
            card.transform,
            UIPalette.Accent
        );
        accent.rectTransform.anchorMin = new Vector2(0f, 1f);
        accent.rectTransform.anchorMax = Vector2.one;
        accent.rectTransform.pivot = new Vector2(0.5f, 1f);
        accent.rectTransform.sizeDelta = new Vector2(0f, 5f);

        eyebrowText = UIFactory.CreateText(
            "Eyebrow",
            card.transform,
            string.Empty,
            20,
            UIPalette.Accent,
            TextAnchor.MiddleLeft
        );
        SetAnchored(
            eyebrowText.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(64f, -42f),
            new Vector2(-128f, 38f)
        );

        titleText = UIFactory.CreateText(
            "Title",
            card.transform,
            string.Empty,
            48,
            UIPalette.TextMain,
            TextAnchor.MiddleLeft
        );
        SetAnchored(
            titleText.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(62f, -105f),
            new Vector2(-124f, 75f)
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
        SetAnchored(
            bodyText.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(64f, -220f),
            new Vector2(-128f, 240f)
        );

        actionButton = UIFactory.CreateButton(
            "ActionButton",
            card.transform,
            "去返程  ›",
            new Vector2(350f, 78f),
            UIPalette.AccentDim,
            30
        );
        actionLabel = actionButton.GetComponentInChildren<Text>();
        SetAnchored(
            actionButton.GetComponent<RectTransform>(),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(-64f, 58f),
            new Vector2(350f, 78f)
        );
        actionButton.onClick.AddListener(HandleAction);

        Text hint = UIFactory.CreateText(
            "Hint",
            card.transform,
            "任务进度已安全记录于本次运行会话",
            18,
            new Color(0.42f, 0.65f, 0.7f, 1f),
            TextAnchor.MiddleLeft
        );
        SetAnchored(
            hint.rectTransform,
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(64f, 70f),
            new Vector2(540f, 36f)
        );

        Image signalBadge = UIFactory.CreatePanel(
            "ChenxiSignalBadge",
            card.transform,
            new Color(0.18f, 0.62f, 0.72f, 1f)
        );
        MiniGameVisuals.MakeCircle(signalBadge);
        SetAnchored(
            signalBadge.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(64f, -168f),
            new Vector2(56f, 56f)
        );
        Text signalGlyph = UIFactory.CreateText(
            "ChenxiSignalGlyph",
            signalBadge.transform,
            "✦",
            30,
            Color.white
        );
        UIFactory.Stretch(signalGlyph.rectTransform);

        CinematicUIVisuals.PolishHierarchy(
            panelRoot.transform,
            CinematicUIVisuals.Sky,
            CinematicUIVisuals.Sun
        );
        CinematicUIVisuals.AddEntrance(card.gameObject);

        panelRoot.SetActive(false);
    }

    private void ReleasePlayerLock()
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
