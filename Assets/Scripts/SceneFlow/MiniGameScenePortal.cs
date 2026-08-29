using UnityEngine;

/// <summary>
/// Reusable scene destination attached to a physical station terminal.
/// The terminal remains responsible for interaction and task authorization.
/// </summary>
public sealed class MiniGameScenePortal : MonoBehaviour
{
    [SerializeField] private string scenePath;

    public string ScenePath => scenePath;

    public bool TryEnter()
    {
        return SceneTransitionManager.EnterMiniGame(scenePath);
    }

    public void Configure(string targetScenePath)
    {
        scenePath = targetScenePath;
    }
}
