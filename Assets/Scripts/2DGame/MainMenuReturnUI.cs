using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 2D 主流程共用的安全退出入口。
/// 始终显示在任务 UI 顶层；二次确认后保存既有进度并返回 FrontEnd 主菜单。
/// </summary>
public sealed class MainMenuReturnUI : MonoBehaviour
{
    private GameObject confirmationRoot;
    private bool transitionStarted;

    public void BuildUI()
    {
        Image transparentRoot = gameObject.AddComponent<Image>();
        transparentRoot.color = Color.clear;
        transparentRoot.raycastTarget = false;

        Button menuButton = UIFactory.CreateButton(
            "BtnReturnMainMenu",
            transform,
            "主菜单  ›",
            new Vector2(190f, 58f),
            new Color(0.05f, 0.15f, 0.22f, 0.96f),
            23
        );
        SetAnchored(
            menuButton.GetComponent<RectTransform>(),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-135f, -62f),
            new Vector2(190f, 58f)
        );
        MiniGameVisuals.Round(menuButton.targetGraphic as Image);
        CinematicUIVisuals.AddShadow(menuButton.gameObject, new Vector2(0f, -5f), 0.24f);
        menuButton.onClick.AddListener(OpenConfirmation);

        BuildConfirmation();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape) || transitionStarted)
        {
            return;
        }

        if (confirmationRoot != null && confirmationRoot.activeSelf)
        {
            CloseConfirmation();
        }
        else
        {
            OpenConfirmation();
        }
    }

    private void BuildConfirmation()
    {
        RectTransform root = UIFactory.CreateRect("ReturnMenuConfirmation", transform);
        UIFactory.Stretch(root);
        confirmationRoot = root.gameObject;

        Image veil = UIFactory.CreatePanel(
            "Veil",
            root,
            new Color(0.015f, 0.03f, 0.07f, 0.76f)
        );
        UIFactory.Stretch(veil.rectTransform);

        Image card = CinematicUIVisuals.CreateCard(
            "ConfirmationCard",
            root,
            new Color(0.055f, 0.14f, 0.20f, 0.99f),
            new Vector2(760f, 470f)
        );
        SetAnchored(
            card.rectTransform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(760f, 470f)
        );
        MiniGameVisuals.Round(card);
        CinematicUIVisuals.AddShadow(card.gameObject, new Vector2(0f, -12f), 0.34f);
        CinematicUIVisuals.AddEntrance(card.gameObject);

        Image accent = UIFactory.CreatePanel(
            "WarmAccent",
            card.transform,
            new Color(1f, 0.69f, 0.31f, 1f)
        );
        SetAnchored(
            accent.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -2f),
            new Vector2(620f, 10f)
        );
        MiniGameVisuals.Round(accent);

        Image icon = UIFactory.CreatePanel(
            "HomeIcon",
            card.transform,
            new Color(0.22f, 0.68f, 0.78f, 0.22f)
        );
        SetAnchored(
            icon.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -52f),
            new Vector2(76f, 76f)
        );
        MiniGameVisuals.MakeCircle(icon);

        Text iconText = UIFactory.CreateText(
            "IconText",
            icon.transform,
            "⌂",
            42,
            new Color(0.78f, 0.95f, 0.95f, 1f)
        );
        UIFactory.Stretch(iconText.rectTransform);

        Text eyebrow = UIFactory.CreateText(
            "Eyebrow",
            card.transform,
            "RESTORATION SESSION",
            17,
            new Color(0.43f, 0.83f, 0.88f, 1f)
        );
        SetAnchored(
            eyebrow.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -138f),
            new Vector2(500f, 28f)
        );

        Text title = UIFactory.CreateText(
            "Title",
            card.transform,
            "现在返回主菜单吗？",
            40,
            CinematicUIVisuals.Cream
        );
        title.fontStyle = FontStyle.Bold;
        SetAnchored(
            title.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -174f),
            new Vector2(650f, 60f)
        );

        Text body = UIFactory.CreateText(
            "Body",
            card.transform,
            "已经获得的修复进度会保存。\n当前尚未完成的任务操作不会结算。",
            23,
            new Color(0.80f, 0.91f, 0.93f, 1f)
        );
        body.lineSpacing = 1.18f;
        SetAnchored(
            body.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -248f),
            new Vector2(650f, 86f)
        );

        Button continueButton = UIFactory.CreateButton(
            "BtnContinueTask",
            card.transform,
            "继续当前任务",
            new Vector2(260f, 72f),
            new Color(0.13f, 0.31f, 0.38f, 1f),
            25
        );
        SetAnchored(
            continueButton.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(-150f, 46f),
            new Vector2(260f, 72f)
        );
        MiniGameVisuals.Round(continueButton.targetGraphic as Image);
        continueButton.onClick.AddListener(CloseConfirmation);

        Button confirmButton = UIFactory.CreateButton(
            "BtnConfirmReturnMenu",
            card.transform,
            "保存并返回  ›",
            new Vector2(260f, 72f),
            new Color(0.94f, 0.46f, 0.24f, 1f),
            25
        );
        SetAnchored(
            confirmButton.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(150f, 46f),
            new Vector2(260f, 72f)
        );
        MiniGameVisuals.Round(confirmButton.targetGraphic as Image);
        CinematicUIVisuals.AddShadow(confirmButton.gameObject, new Vector2(0f, -5f), 0.22f);
        confirmButton.onClick.AddListener(ReturnToMainMenu);

        confirmationRoot.SetActive(false);
    }

    private void OpenConfirmation()
    {
        if (confirmationRoot == null || transitionStarted)
        {
            return;
        }

        confirmationRoot.SetActive(true);
        confirmationRoot.transform.SetAsLastSibling();
    }

    private void CloseConfirmation()
    {
        if (confirmationRoot != null && !transitionStarted)
        {
            confirmationRoot.SetActive(false);
        }
    }

    private void ReturnToMainMenu()
    {
        if (transitionStarted)
        {
            return;
        }

        transitionStarted = true;
        SaveManager.TrySave();

        if (!SceneTransitionManager.EnterFrontEnd())
        {
            transitionStarted = false;
        }
    }

    private static void SetAnchored(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 size
    )
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }
}
