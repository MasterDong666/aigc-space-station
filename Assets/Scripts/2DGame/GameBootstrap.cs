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
    private GreenhouseHarvestUI greenhouse;
    private EndingBridgeUI endingBridge;
    private CompletionPopupUI completionPopup;

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

        endingBridge = CreatePanel<EndingBridgeUI>("EndingBridgePanel", canvas.transform);
        endingBridge.BuildUI();

        completionPopup = CreatePanel<CompletionPopupUI>("CompletionPopup", canvas.transform);
        completionPopup.BuildUI();

        // 流程串联
        mainHub.OrbitTaskClicked += OpenOrbitTask;
        orbitTask.ReturnRequested += ReturnToMain;
        orbitTask.ReportSubmitted += ShowCompletionPopup;

        mainHub.GeneCultivationClicked += OpenGeneTask;
        geneTask.ExitRequested += ReturnToMain;
        geneTask.EnterGreenhouse += OpenGreenhouse;
        greenhouse.ReturnRequested += ReturnToMain;
        greenhouse.ContinueToEnding += OpenEndingBridge;
        endingBridge.ReturnRequested += ReturnToMain;

        mainHub.Show();
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

    private void OpenGreenhouse()
    {
        geneTask.Hide();
        greenhouse.Show();
    }

    private void OpenEndingBridge()
    {
        greenhouse.Hide();
        endingBridge.Show();
    }

    private void ReturnToMain()
    {
        orbitTask.Hide();
        geneTask.Hide();
        greenhouse.Hide();
        endingBridge.Hide();
        mainHub.Show();
        mainHub.Refresh();
    }

    private void ShowCompletionPopup(int reward)
    {
        completionPopup.Show(
            "今日轨道巡检完成\n地球修复进度 +" + reward + "%",
            ReturnToStation
        );
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
