using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 面板注册表（静态）。
/// 场景内组件把自己构建的面板注册进来，供调试、验证脚本与后续编辑器工具按 ID 查找。
/// LoadSceneMode.Single 场景切换时旧面板已销毁，注册表随 sceneLoaded 清空，防止悬挂引用。
/// </summary>
public static class UIManager
{
    private static readonly Dictionary<string, GameObject> Panels = new();
    private static bool sceneHookInstalled;

    public static string[] RegisteredIds
    {
        get
        {
            string[] ids = new string[Panels.Count];
            Panels.Keys.CopyTo(ids, 0);
            return ids;
        }
    }

    public static void Register(string panelId, GameObject panel)
    {
        if (panel == null)
        {
            Debug.LogWarning("[UI] Register 收到空面板：" + panelId);
            return;
        }

        Panels[panelId] = panel;
    }

    public static void Unregister(string panelId)
    {
        Panels.Remove(panelId);
    }

    public static GameObject Find(string panelId)
    {
        return Panels.TryGetValue(panelId, out GameObject panel) ? panel : null;
    }

    public static T FindComponent<T>(string panelId) where T : Component
    {
        GameObject panel = Find(panelId);
        return panel != null ? panel.GetComponent<T>() : null;
    }

    public static bool SetActive(string panelId, bool active)
    {
        GameObject panel = Find(panelId);
        if (panel == null)
        {
            return false;
        }

        panel.SetActive(active);
        return true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InstallSceneHook()
    {
        if (sceneHookInstalled)
        {
            return;
        }

        sceneHookInstalled = true;
        SceneManager.sceneUnloaded += HandleSceneUnloaded;
    }

    // 注意：不能用 sceneLoaded 清空——sceneLoaded 在新场景 Awake（Register）之后触发，
    // 会误伤刚注册的面板。sceneUnloaded 在旧场景销毁时触发，此时新场景尚未注册。
    private static void HandleSceneUnloaded(Scene scene)
    {
        Panels.Clear();
    }
}
