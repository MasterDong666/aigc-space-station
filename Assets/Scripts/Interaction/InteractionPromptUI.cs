using UnityEngine;
using UnityEngine.UI;

public class InteractionPromptUI : MonoBehaviour
{
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private Text promptText;

    private static Font runtimeFont;

    public bool IsVisible =>
        promptRoot != null && promptRoot.activeSelf;

    public string CurrentText =>
        promptText == null ? string.Empty : promptText.text;

    private void Awake()
    {
        ApplyRuntimeFont();
        Hide();
    }

    public void Show(string message)
    {
        if (promptText != null)
        {
            promptText.text = message;
        }

        if (promptRoot != null)
        {
            promptRoot.SetActive(true);
        }
    }

    public void Hide()
    {
        if (promptRoot != null)
        {
            promptRoot.SetActive(false);
        }
    }

    private void ApplyRuntimeFont()
    {
        if (runtimeFont == null)
        {
            runtimeFont = Font.CreateDynamicFontFromOSFont(
                new[]
                {
                    "PingFang SC",
                    "Hiragino Sans GB",
                    "Arial"
                },
                32
            );
        }

        if (runtimeFont == null)
        {
            return;
        }

        foreach (Text text in GetComponentsInChildren<Text>(true))
        {
            text.font = runtimeFont;
        }
    }
}
