using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>美术验收辅助：Play Mode 下直接重载指定小游戏，不写入正式流程状态。</summary>
public static class MiniGameVisualPreviewMenu
{
    private const string SceneName = "EarthRestoration2D";

    [MenuItem("Tools/Earth Reshaping/Minigame Preview/Orbit Inspection")]
    private static void PreviewOrbit()
    {
        Preview(MiniGameId.OrbitInspection);
    }

    [MenuItem("Tools/Earth Reshaping/Minigame Preview/Ecology Deployment")]
    private static void PreviewEcology()
    {
        Preview(MiniGameId.EcologyDeployment);
    }

    [MenuItem("Tools/Earth Reshaping/Minigame Preview/Gene Cultivation")]
    private static void PreviewGene()
    {
        Preview(MiniGameId.GeneCultivation);
    }

    [MenuItem("Tools/Earth Reshaping/Minigame Preview/Orbit Inspection", true)]
    [MenuItem("Tools/Earth Reshaping/Minigame Preview/Ecology Deployment", true)]
    [MenuItem("Tools/Earth Reshaping/Minigame Preview/Gene Cultivation", true)]
    private static bool ValidatePreview()
    {
        return Application.isPlaying;
    }

    private static void Preview(MiniGameId id)
    {
        MVPGameSession.MarkTaskTutorialSeen(id);
        MVPGameSession.RequestMiniGame(id);
        SceneManager.LoadScene(SceneName);
    }
}
