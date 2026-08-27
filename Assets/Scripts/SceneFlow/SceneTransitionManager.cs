using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Centralizes scene changes between the 3D station and UI-driven mini-games.
/// Scene destinations are configured by portal components; the station return
/// path lives here so mini-games do not duplicate it.
/// </summary>
public static class SceneTransitionManager
{
    public const string StationHubScenePath =
        "Assets/Scenes/SpaceStationHub_MVP.unity";

    public static bool EnterMiniGame(string scenePath)
    {
        return LoadScene(scenePath, true);
    }

    public static bool ReturnToStationHub()
    {
        return LoadScene(StationHubScenePath, false);
    }

    private static bool LoadScene(string scenePath, bool showCursor)
    {
        if (string.IsNullOrWhiteSpace(scenePath))
        {
            Debug.LogError("Scene Transition：没有配置目标 Scene。");
            return false;
        }

        if (!Application.CanStreamedLevelBeLoaded(scenePath))
        {
            Debug.LogError(
                $"Scene Transition：目标 Scene 未加入 Build Settings：{scenePath}"
            );
            return false;
        }

        Cursor.lockState = showCursor
            ? CursorLockMode.None
            : CursorLockMode.Locked;
        Cursor.visible = showCursor;

        SceneManager.LoadScene(scenePath, LoadSceneMode.Single);
        return true;
    }
}
