using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 2D 主界面游戏入口：运行时构建全部 UI 并串联界面流程。
/// 场景中只需挂载本组件即可直接 Play。
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    private MainHubUI mainHub;
    private OrbitTaskController orbitTask;
    private GeneCultivationTaskController geneTask;
    private EcologyNutrientTaskController ecologyTask;
    private GreenhouseHarvestUI greenhouse;
    private CompletionPopupUI completionPopup;
    private TaskTutorialUI tutorialUI;
    private bool launchedFromStation;

    private void Awake()
    {
        // 编辑器窗口失焦/最小化时保持游戏循环运行；
        // 否则 Play Mode 帧循环会暂停，协程动画与计时全部停滞。
        Application.runInBackground = true;

        // 全局进度（同一运行内防重复发放）
        GameObject progressGo = new GameObject("GameProgress");
        progressGo.transform.SetParent(transform);
        progressGo.AddComponent<GameProgressManager>();

        EnsureCamera();
        EnsureEventSystem();

        Canvas canvas = UIFactory.CreateCanvas("Canvas");

        mainHub = CreatePanel<MainHubUI>("MainPanel", canvas.transform);
        mainHub.BuildUI();

        orbitTask = CreatePanel<OrbitTaskController>("OrbitPanel", canvas.transform);
        orbitTask.BuildUI();

        geneTask = CreatePanel<GeneCultivationTaskController>("GenePanel", canvas.transform);
        geneTask.BuildUI();

        greenhouse = CreatePanel<GreenhouseHarvestUI>("GreenhousePanel", canvas.transform);
        greenhouse.BuildUI();

        ecologyTask = CreatePanel<EcologyNutrientTaskController>("EcologyPanel", canvas.transform);
        ecologyTask.BuildUI();

        completionPopup = CreatePanel<CompletionPopupUI>("CompletionPopup", canvas.transform);
        completionPopup.BuildUI();

        tutorialUI = CreatePanel<TaskTutorialUI>("TaskTutorial", canvas.transform);
        tutorialUI.BuildUI();

        // 流程串联
        mainHub.OrbitTaskClicked += () => OpenTaskWithTutorial(
            MiniGameId.OrbitInspection,
            OpenOrbitTask
        );
        orbitTask.ReturnRequested += ReturnFromMiniGame;
        orbitTask.ReportSubmitted += reward => ShowCompletionPopup(
            "今日轨道巡检完成",
            reward
        );

        mainHub.GeneCultivationClicked += () => OpenTaskWithTutorial(
            MiniGameId.GeneCultivation,
            OpenGeneTask
        );
        geneTask.ExitRequested += ReturnFromMiniGame;
        geneTask.EnterGreenhouse += OpenGreenhouse;
        greenhouse.ReturnRequested += ReturnFromMiniGame;

        mainHub.EcologyNutrientClicked += () => OpenTaskWithTutorial(
            MiniGameId.EcologyDeployment,
            OpenEcologyTask
        );
        ecologyTask.ExitRequested += ReturnFromMiniGame;
        ecologyTask.ReportSubmitted += reward => ShowCompletionPopup(
            "营养液投放完成",
            reward
        );

        OpenRequestedTaskOrMainHub();
    }

    private void OpenOrbitTask()
    {
        mainHub.Hide();
        orbitTask.Show();
    }

    private void OpenGeneTask()
    {
        mainHub.Hide();
        geneTask.OpenTask();
    }

    private void OpenRequestedTaskOrMainHub()
    {
        if (!MVPGameSession.TryConsumeRequestedMiniGame(out MiniGameId id))
        {
            launchedFromStation = false;
            mainHub.Show();
            return;
        }

        launchedFromStation = true;

        switch (id)
        {
            case MiniGameId.OrbitInspection:
                OpenTaskWithTutorial(id, OpenOrbitTask);
                break;
            case MiniGameId.EcologyDeployment:
                OpenTaskWithTutorial(id, OpenEcologyTask);
                break;
            case MiniGameId.GeneCultivation:
                OpenTaskWithTutorial(id, OpenGeneTask);
                break;
            default:
                launchedFromStation = false;
                mainHub.Show();
                break;
        }
    }

    private void OpenGreenhouse()
    {
        geneTask.Hide();
        greenhouse.Show();
    }

    private void OpenTaskWithTutorial(
        MiniGameId id,
        System.Action openTask
    )
    {
        if (MVPGameSession.HasSeenTaskTutorial(id))
        {
            openTask?.Invoke();
            return;
        }

        tutorialUI.Show(id, openTask);
    }

    private void OpenEcologyTask()
    {
        mainHub.Hide();
        ecologyTask.OpenTask();
    }

    private void ReturnToMain()
    {
        orbitTask.Hide();
        geneTask.Hide();
        ecologyTask.Hide();
        greenhouse.Hide();
        tutorialUI.gameObject.SetActive(false);
        mainHub.Show();
        mainHub.Refresh();
    }

    private void ShowCompletionPopup(string title, int reward)
    {
        completionPopup.Show(
            title + "\n地球修复进度 +" + reward + " 点",
            launchedFromStation ? ReturnToStation : ReturnToMain
        );
    }

    private void ReturnFromMiniGame()
    {
        if (launchedFromStation)
        {
            ReturnToStation();
            return;
        }

        ReturnToMain();
    }

    private static void ReturnToStation()
    {
        SceneTransitionManager.ReturnToStationHub();
    }

    private static T CreatePanel<T>(string name, Transform parent) where T : Component
    {
        RectTransform rect = UIFactory.CreateRect(name, parent);
        UIFactory.Stretch(rect);
        return rect.gameObject.AddComponent<T>();
    }

    private static void EnsureCamera()
    {
        if (Camera.main != null)
        {
            return;
        }

        GameObject cameraGo = new GameObject("Main Camera");
        cameraGo.tag = "MainCamera";

        Camera camera = cameraGo.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = UIPalette.Background;
        camera.transform.position = new Vector3(0f, 0f, -10f);

        cameraGo.AddComponent<AudioListener>();
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject eventGo = new GameObject("EventSystem");
        eventGo.AddComponent<EventSystem>();
        eventGo.AddComponent<StandaloneInputModule>();
    }
}
