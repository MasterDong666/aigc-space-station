#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 仅用于编辑器视觉验收；快捷切换运行中的 FrontEnd 页面，不写入场景。
/// </summary>
public static class FrontEndVisualPreviewMenu
{
    private const BindingFlags PrivateInstance =
        BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem("Tools/Earth Reshaping/Front End Preview/Profile Setup %2")]
    private static void PreviewProfile()
    {
        Invoke("ShowProfile");
    }

    [MenuItem("Tools/Earth Reshaping/Front End Preview/Opening Sequence %3")]
    private static void PreviewOpening()
    {
        if (!MVPGameSession.HasPlayerProfile)
        {
            MVPGameSession.SetPlayerProfile("预览修复官", "RESTORER_A");
        }

        Invoke("StartOpeningNarrative");
    }

    [MenuItem("Tools/Earth Reshaping/Front End Preview/Noah Holiday %4")]
    private static void PreviewNoah()
    {
        Invoke("ShowHolidayFlow");
    }

    [MenuItem("Tools/Earth Reshaping/Front End Preview/Ending Choice %5")]
    private static void PreviewEnding()
    {
        Invoke("ShowEndingFlow");
    }

    [MenuItem("Tools/Earth Reshaping/Front End Preview/Profile Setup %2", true)]
    [MenuItem("Tools/Earth Reshaping/Front End Preview/Opening Sequence %3", true)]
    [MenuItem("Tools/Earth Reshaping/Front End Preview/Noah Holiday %4", true)]
    [MenuItem("Tools/Earth Reshaping/Front End Preview/Ending Choice %5", true)]
    private static bool ValidatePreview()
    {
        return EditorApplication.isPlaying &&
            Object.FindObjectOfType<FrontEndBootstrap>() != null;
    }

    private static void Invoke(string methodName)
    {
        FrontEndBootstrap bootstrap =
            Object.FindObjectOfType<FrontEndBootstrap>();
        MethodInfo method = typeof(FrontEndBootstrap).GetMethod(
            methodName,
            PrivateInstance
        );
        method?.Invoke(bootstrap, null);
    }
}
#endif
