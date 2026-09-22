using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 空间站专用 ESC 暂停菜单。运行时自动安装，不修改场景资产。
/// 可继续游戏、完整重开，或保存现有进度后退至主菜单。
/// </summary>
[DefaultExecutionOrder(-1000)]
public sealed class StationPauseMenuController : MonoBehaviour
{
    private GameObject menuRoot;
    private GameObject restartConfirmationRoot;
    private Text progressText;
    private PlayerInteractor playerInteractor;
    private bool ownsPlayerLock;
    private bool transitionStarted;
    private float previousTimeScale = 1f;

    public bool IsOpen => menuRoot != null && menuRoot.activeSelf;

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

        if (FindObjectOfType<StationPauseMenuController>() != null)
        {
            return;
        }

        GameObject root = new("StationPauseMenu");
        root.AddComponent<StationPauseMenuController>();
    }

    private void Awake()
    {
        ResolvePlayerInteractor();
        BuildUI();
    }

    private void Update()
    {
        if (transitionStarted || !Input.GetKeyDown(KeyCode.Escape))
        {
            return;
        }

        if (IsOpen)
        {
            if (
                restartConfirmationRoot != null &&
                restartConfirmationRoot.activeSelf
            )
            {
                CloseRestartConfirmation();
            }
            else
            {
                CloseMenu();
            }

            return;
        }

        // 这些界面原本就使用 ESC 关闭。让本次按键只关闭它们，
        // 避免同一帧又打开暂停菜单。
        if (AnotherEscapePanelIsOpen())
        {
            return;
        }

        OpenMenu();
    }

    private void OnDisable()
    {
        RestoreGameplayState();
    }

    private void OnDestroy()
    {
        RestoreGameplayState();
    }

    private void BuildUI()
    {
        Canvas canvas = UIFactory.CreateCanvas("StationPauseCanvas");
        canvas.sortingOrder = 1000;
        canvas.transform.SetParent(transform, false);

        RectTransform root = UIFactory.CreateRect("PauseRoot", canvas.transform);
        UIFactory.Stretch(root);
        menuRoot = root.gameObject;

        Image veil = UIFactory.CreatePanel(
            "Veil",
            root,
            new Color(0.012f, 0.025f, 0.06f, 0.82f)
        );
        UIFactory.Stretch(veil.rectTransform);

        Image card = CinematicUIVisuals.CreateCard(
            "PauseCard",
            root,
            new Color(0.055f, 0.14f, 0.20f, 0.99f),
            new Vector2(780f, 680f)
        );
        SetAnchored(card.rectTransform, Vector2.zero, new Vector2(780f, 680f));
        MiniGameVisuals.Round(card);
        CinematicUIVisuals.AddShadow(
            card.gameObject,
            new Vector2(0f, -14f),
            0.36f
        );

        Image accent = UIFactory.CreatePanel(
            "WarmAccent",
            card.transform,
            new Color(1f, 0.67f, 0.28f, 1f)
        );
        SetAnchored(
            accent.rectTransform,
            new Vector2(0f, 284f),
            new Vector2(620f, 10f)
        );
        MiniGameVisuals.Round(accent);

        Image icon = UIFactory.CreatePanel(
            "PauseIcon",
            card.transform,
            new Color(0.25f, 0.76f, 0.76f, 0.22f)
        );
        SetAnchored(
            icon.rectTransform,
            new Vector2(0f, 228f),
            new Vector2(78f, 78f)
        );
        MiniGameVisuals.MakeCircle(icon);

        Text iconText = UIFactory.CreateText(
            "IconText",
            icon.transform,
            "Ⅱ",
            37,
            new Color(0.86f, 0.97f, 0.91f, 1f)
        );
        iconText.fontStyle = FontStyle.Bold;
        UIFactory.Stretch(iconText.rectTransform);

        Text eyebrow = UIFactory.CreateText(
            "Eyebrow",
            card.transform,
            "SPACE STATION  ·  SETTINGS",
            17,
            new Color(0.46f, 0.86f, 0.86f, 1f)
        );
        SetAnchored(
            eyebrow.rectTransform,
            new Vector2(0f, 168f),
            new Vector2(600f, 32f)
        );

        Text title = UIFactory.CreateText(
            "Title",
            card.transform,
            "暂停一下",
            44,
            CinematicUIVisuals.Cream
        );
        title.fontStyle = FontStyle.Bold;
        SetAnchored(
            title.rectTransform,
            new Vector2(0f, 114f),
            new Vector2(620f, 62f)
        );

        Image progressPanel = UIFactory.CreatePanel(
            "ProgressPanel",
            card.transform,
            new Color(0.13f, 0.31f, 0.35f, 0.88f)
        );
        SetAnchored(
            progressPanel.rectTransform,
            new Vector2(0f, 42f),
            new Vector2(560f, 58f)
        );
        MiniGameVisuals.Round(progressPanel);

        progressText = UIFactory.CreateText(
            "Progress",
            progressPanel.transform,
            string.Empty,
            22,
            new Color(0.82f, 0.94f, 0.90f, 1f)
        );
        UIFactory.Stretch(progressText.rectTransform);

        Button continueButton = CreateMenuButton(
            card.transform,
            "BtnContinue",
            "继续游戏",
            new Vector2(0f, -48f),
            new Color(0.18f, 0.69f, 0.48f, 1f)
        );
        continueButton.onClick.AddListener(CloseMenu);

        Button restartButton = CreateMenuButton(
            card.transform,
            "BtnRestart",
            "重新开始",
            new Vector2(0f, -136f),
            new Color(0.94f, 0.43f, 0.20f, 1f)
        );
        restartButton.onClick.AddListener(OpenRestartConfirmation);

        Button mainMenuButton = CreateMenuButton(
            card.transform,
            "BtnMainMenu",
            "退至主菜单",
            new Vector2(0f, -224f),
            new Color(0.08f, 0.27f, 0.38f, 1f)
        );
        mainMenuButton.onClick.AddListener(ReturnToMainMenu);

        Text hint = UIFactory.CreateText(
            "Hint",
            card.transform,
            "按 ESC 也可以继续游戏",
            17,
            new Color(0.60f, 0.76f, 0.79f, 1f)
        );
        SetAnchored(
            hint.rectTransform,
            new Vector2(0f, -303f),
            new Vector2(560f, 28f)
        );

        BuildRestartConfirmation(root);
        menuRoot.SetActive(false);
    }

    private void BuildRestartConfirmation(Transform parent)
    {
        RectTransform root = UIFactory.CreateRect(
            "RestartConfirmation",
            parent
        );
        UIFactory.Stretch(root);
        restartConfirmationRoot = root.gameObject;

        Image veil = UIFactory.CreatePanel(
            "ConfirmationVeil",
            root,
            new Color(0.01f, 0.02f, 0.05f, 0.72f)
        );
        UIFactory.Stretch(veil.rectTransform);

        Image card = CinematicUIVisuals.CreateCard(
            "ConfirmationCard",
            root,
            new Color(0.075f, 0.16f, 0.22f, 1f),
            new Vector2(700f, 410f)
        );
        SetAnchored(card.rectTransform, Vector2.zero, new Vector2(700f, 410f));
        MiniGameVisuals.Round(card);
        CinematicUIVisuals.AddShadow(
            card.gameObject,
            new Vector2(0f, -12f),
            0.38f
        );

        Text eyebrow = UIFactory.CreateText(
            "Eyebrow",
            card.transform,
            "RESTART RESTORATION",
            17,
            new Color(1f, 0.67f, 0.31f, 1f)
        );
        SetAnchored(
            eyebrow.rectTransform,
            new Vector2(0f, 138f),
            new Vector2(560f, 28f)
        );

        Text title = UIFactory.CreateText(
            "Title",
            card.transform,
            "确定重新开始吗？",
            38,
            CinematicUIVisuals.Cream
        );
        title.fontStyle = FontStyle.Bold;
        SetAnchored(
            title.rectTransform,
            new Vector2(0f, 82f),
            new Vector2(600f, 54f)
        );

        Text body = UIFactory.CreateText(
            "Body",
            card.transform,
            "当前修复进度、任务记录和剧情解锁都会清空，\n游戏将回到最初的 0 / 50。",
            22,
            new Color(0.82f, 0.91f, 0.92f, 1f)
        );
        body.lineSpacing = 1.18f;
        SetAnchored(
            body.rectTransform,
            new Vector2(0f, 12f),
            new Vector2(610f, 86f)
        );

        Button cancelButton = UIFactory.CreateButton(
            "BtnCancelRestart",
            card.transform,
            "取消",
            new Vector2(250f, 70f),
            new Color(0.12f, 0.31f, 0.39f, 1f),
            24
        );
        SetAnchored(
            cancelButton.GetComponent<RectTransform>(),
            new Vector2(-142f, -116f),
            new Vector2(250f, 70f)
        );
        MiniGameVisuals.Round(cancelButton.targetGraphic as Image);
        cancelButton.onClick.AddListener(CloseRestartConfirmation);

        Button confirmButton = UIFactory.CreateButton(
            "BtnConfirmRestart",
            card.transform,
            "确认重新开始",
            new Vector2(250f, 70f),
            new Color(0.94f, 0.33f, 0.16f, 1f),
            24
        );
        SetAnchored(
            confirmButton.GetComponent<RectTransform>(),
            new Vector2(142f, -116f),
            new Vector2(250f, 70f)
        );
        MiniGameVisuals.Round(confirmButton.targetGraphic as Image);
        CinematicUIVisuals.AddShadow(
            confirmButton.gameObject,
            new Vector2(0f, -5f),
            0.24f
        );
        confirmButton.onClick.AddListener(RestartGame);

        restartConfirmationRoot.SetActive(false);
    }

    private Button CreateMenuButton(
        Transform parent,
        string objectName,
        string label,
        Vector2 position,
        Color color
    )
    {
        Button button = UIFactory.CreateButton(
            objectName,
            parent,
            label,
            new Vector2(480f, 68f),
            color,
            25
        );
        SetAnchored(
            button.GetComponent<RectTransform>(),
            position,
            new Vector2(480f, 68f)
        );
        MiniGameVisuals.Round(button.targetGraphic as Image);
        CinematicUIVisuals.AddShadow(
            button.gameObject,
            new Vector2(0f, -5f),
            0.22f
        );
        return button;
    }

    private void OpenMenu()
    {
        ResolvePlayerInteractor();
        progressText.text =
            "地球修复进度  " + MVPGameSession.EarthProgress +
            " / " + MVPGameSession.EndingProgress;

        if (
            playerInteractor != null &&
            !playerInteractor.IsPlayerControlLocked
        )
        {
            playerInteractor.SetPlayerControlLocked(true);
            ownsPlayerLock = true;
        }

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        menuRoot.SetActive(true);
        menuRoot.transform.SetAsLastSibling();
        restartConfirmationRoot.SetActive(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void CloseMenu()
    {
        if (!IsOpen || transitionStarted)
        {
            return;
        }

        restartConfirmationRoot.SetActive(false);
        menuRoot.SetActive(false);
        RestoreGameplayState();
    }

    private void OpenRestartConfirmation()
    {
        if (transitionStarted)
        {
            return;
        }

        restartConfirmationRoot.SetActive(true);
        restartConfirmationRoot.transform.SetAsLastSibling();
    }

    private void CloseRestartConfirmation()
    {
        if (restartConfirmationRoot != null && !transitionStarted)
        {
            restartConfirmationRoot.SetActive(false);
        }
    }

    private void RestartGame()
    {
        if (transitionStarted || !CanEnterFrontEnd())
        {
            return;
        }

        transitionStarted = true;
        RestoreGameplayState();
        ProgressManager.ResetRuntimeState();
        GameFlowManager.ResetRuntimeState();
        SaveManager.DeleteSave();
        MVPGameSession.ResetAllProgress();
        SaveManager.BeginNewSave();
        SceneTransitionManager.EnterFrontEnd();
    }

    private void ReturnToMainMenu()
    {
        if (transitionStarted || !CanEnterFrontEnd())
        {
            return;
        }

        transitionStarted = true;
        SaveManager.TrySave();
        RestoreGameplayState();
        SceneTransitionManager.EnterFrontEnd();
    }

    private static bool CanEnterFrontEnd()
    {
        if (
            Application.CanStreamedLevelBeLoaded(
                SceneTransitionManager.FrontEndScenePath
            )
        )
        {
            return true;
        }

        Debug.LogError(
            "暂停菜单：FrontEnd 场景未加入 Build Settings，无法切换。"
        );
        return false;
    }

    private void RestoreGameplayState()
    {
        if (Time.timeScale == 0f)
        {
            Time.timeScale = previousTimeScale <= 0f
                ? 1f
                : previousTimeScale;
        }

        if (ownsPlayerLock && playerInteractor != null)
        {
            playerInteractor.SetPlayerControlLocked(false);
        }

        ownsPlayerLock = false;
    }

    private void ResolvePlayerInteractor()
    {
        if (playerInteractor == null)
        {
            playerInteractor = FindObjectOfType<PlayerInteractor>();
        }
    }

    private static bool AnotherEscapePanelIsOpen()
    {
        foreach (
            StationTerminalInteractable terminal in
            FindObjectsOfType<StationTerminalInteractable>(true)
        )
        {
            if (terminal.IsOpen)
            {
                return true;
            }
        }

        foreach (
            OrbitConsoleInteractable terminal in
            FindObjectsOfType<OrbitConsoleInteractable>(true)
        )
        {
            if (terminal.IsOpen)
            {
                return true;
            }
        }

        StationArchiveController archive =
            FindObjectOfType<StationArchiveController>(true);
        if (archive != null && archive.IsOpen)
        {
            return true;
        }

        NoahRemoteCommunicationController transmission =
            FindObjectOfType<NoahRemoteCommunicationController>(true);
        return transmission != null && transmission.IsOpen;
    }

    private static void SetAnchored(
        RectTransform rect,
        Vector2 position,
        Vector2 size
    )
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
