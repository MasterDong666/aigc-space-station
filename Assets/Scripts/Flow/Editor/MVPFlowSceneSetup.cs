using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MVPFlowSceneSetup
{
    private const string MVPScenePath =
        "Assets/Scenes/SpaceStationHub_MVP.unity";

    private const string TwoDGameScenePath =
        "Assets/Scenes/2DGame/EarthRestoration2D.unity";

    [MenuItem(
        "Tools/Earth Reshaping/MVP Flow/Install In Active Scene"
    )]
    public static void InstallInActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (scene.path != MVPScenePath)
        {
            Debug.LogError(
                "MVP Flow：只允许安装到 SpaceStationHub_MVP 场景，" +
                $"当前场景为 {scene.path}。"
            );
            return;
        }

        GameObject flowRoot = FindSceneObject("MVP_GameFlow");

        if (flowRoot == null)
        {
            flowRoot = new GameObject("MVP_GameFlow");
            Undo.RegisterCreatedObjectUndo(
                flowRoot,
                "Create MVP Game Flow"
            );
        }

        MVPFlowController controller =
            GetOrAddComponent<MVPFlowController>(flowRoot);

        ConfigureTerminal(
            "OrbitControlConsole",
            MiniGameId.OrbitInspection,
            controller,
            TwoDGameScenePath
        );
        ConfigureTerminal(
            "EcologyDeploymentTerminal",
            MiniGameId.EcologyDeployment,
            controller,
            TwoDGameScenePath
        );
        ConfigureTerminal(
            "GeneCultivationTerminal",
            MiniGameId.GeneCultivation,
            controller,
            TwoDGameScenePath
        );

        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = flowRoot;

        Debug.Log(
            "MVP Flow：已接入轨道巡检、生态投放、基因培育三个入口。" +
            "流程顺序为 Orbit → Ecology → Gene。",
            flowRoot
        );
    }

    [MenuItem(
        "Tools/Earth Reshaping/MVP Flow/Complete Current Task (Play Mode)"
    )]
    public static void CompleteCurrentTaskInPlayMode()
    {
        MVPFlowController controller =
            Object.FindObjectOfType<MVPFlowController>();

        if (controller == null)
        {
            Debug.LogError("MVP Flow：当前场景没有流程控制器。");
            return;
        }

        controller.CompleteCurrentTaskForDevelopment();
    }

    [MenuItem(
        "Tools/Earth Reshaping/MVP Flow/Complete Current Task (Play Mode)",
        true
    )]
    private static bool ValidateCompleteCurrentTaskInPlayMode()
    {
        return Application.isPlaying;
    }

    private static void ConfigureTerminal(
        string objectName,
        MiniGameId id,
        MVPFlowController controller,
        string scenePath
    )
    {
        GameObject terminal = FindSceneObject(objectName);

        if (terminal == null)
        {
            Debug.LogError(
                $"MVP Flow：没有找到终端对象 {objectName}。"
            );
            return;
        }

        MiniGameTerminalFlowLink flowLink =
            GetOrAddComponent<MiniGameTerminalFlowLink>(terminal);

        GetOrAddComponent<MiniGameCompletionRelay>(terminal);
        Undo.RecordObject(flowLink, "Configure Mini Game Flow Link");
        flowLink.Configure(id, controller);

        if (!string.IsNullOrWhiteSpace(scenePath))
        {
            MiniGameScenePortal portal =
                GetOrAddComponent<MiniGameScenePortal>(terminal);
            Undo.RecordObject(portal, "Configure Mini Game Scene Portal");
            portal.Configure(scenePath);
            EditorUtility.SetDirty(portal);
        }

        EditorUtility.SetDirty(flowLink);
        EditorUtility.SetDirty(terminal);
    }

    private static T GetOrAddComponent<T>(GameObject target)
        where T : Component
    {
        T component = target.GetComponent<T>();

        return component != null
            ? component
            : Undo.AddComponent<T>(target);
    }

    private static GameObject FindSceneObject(string objectName)
    {
        Scene scene = SceneManager.GetActiveScene();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms =
                root.GetComponentsInChildren<Transform>(true);

            foreach (Transform candidate in transforms)
            {
                if (candidate.name == objectName)
                {
                    return candidate.gameObject;
                }
            }
        }

        return null;
    }
}
