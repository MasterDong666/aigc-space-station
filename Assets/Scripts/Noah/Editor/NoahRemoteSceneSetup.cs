using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class NoahRemoteSceneSetup
{
    private const string MVPScenePath =
        "Assets/Scenes/SpaceStationHub_MVP.unity";
    private const string PlanetPath =
        "Assets/Art/UI/NoahRemote/NoahPlanet.png";
    private const string SchoolPath =
        "Assets/Art/UI/NoahRemote/NoahSchoolCity.png";
    private const string PortraitPath =
        "Assets/Art/UI/NoahRemote/YoungerSisterPortrait.png";

    private static readonly Color DeepNavy =
        new(0.006f, 0.018f, 0.035f, 0.98f);
    private static readonly Color GlassNavy =
        new(0.012f, 0.065f, 0.09f, 0.92f);
    private static readonly Color Cyan =
        new(0.15f, 0.92f, 1f, 1f);
    private static readonly Color Pale =
        new(0.82f, 0.97f, 1f, 1f);
    private static readonly Color Muted =
        new(0.48f, 0.72f, 0.76f, 1f);
    private static readonly Color Green =
        new(0.38f, 1f, 0.72f, 1f);

    private static Font editorFont;

    [MenuItem(
        "Tools/Earth Reshaping/Noah Remote/Install In Active Scene"
    )]
    public static void InstallInActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (scene.path != MVPScenePath)
        {
            Debug.LogError(
                "Noah Remote：只允许安装到 SpaceStationHub_MVP，" +
                $"当前场景为 {scene.path}。"
            );
            return;
        }

        Sprite planetSprite = PrepareSprite(PlanetPath);
        Sprite schoolSprite = PrepareSprite(SchoolPath);
        Sprite portraitSprite = PrepareSprite(PortraitPath);

        if (
            planetSprite == null ||
            schoolSprite == null ||
            portraitSprite == null
        )
        {
            Debug.LogError("Noah Remote：叙事美术资源导入失败。");
            return;
        }

        DestroyExisting("MVP_NoahRemoteSystem");
        DestroyExisting("NoahRemoteTerminal");

        GameObject root = new("MVP_NoahRemoteSystem");
        Undo.RegisterCreatedObjectUndo(root, "Create Noah Remote System");

        NoahRemoteCommunicationController controller =
            root.AddComponent<NoahRemoteCommunicationController>();

        Canvas canvas = CreateOverlayCanvas(root.transform);
        RectTransform canvasRect =
            canvas.GetComponent<RectTransform>();

        GameObject panel = CreatePanel(
            "NoahRemotePanel",
            canvasRect,
            DeepNavy
        );
        CanvasGroup canvasGroup = panel.AddComponent<CanvasGroup>();
        SetStretchRect(
            panel.GetComponent<RectTransform>(),
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero
        );

        RectTransform panelRect = panel.GetComponent<RectTransform>();

        Image background = CreateImage(
            "TransmissionVisual",
            panelRect,
            Color.white,
            planetSprite
        );
        background.preserveAspect = false;
        SetStretchRect(
            background.rectTransform,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero
        );

        Image visualShade = CreateImage(
            "VisualShade",
            panelRect,
            new Color(0.002f, 0.012f, 0.025f, 0.34f)
        );
        SetStretchRect(
            visualShade.rectTransform,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero
        );

        CreateCornerDecorations(panelRect);

        GameObject topBar = CreatePanel(
            "TransmissionHeader",
            panelRect,
            new Color(0.006f, 0.028f, 0.05f, 0.94f)
        );
        SetStretchRect(
            topBar.GetComponent<RectTransform>(),
            new Vector2(0f, 1f),
            Vector2.one,
            new Vector2(36f, -108f),
            new Vector2(-36f, -24f)
        );

        RectTransform topRect = topBar.GetComponent<RectTransform>();
        Image topAccent = CreateImage(
            "HeaderAccent",
            topRect,
            Cyan
        );
        SetStretchRect(
            topAccent.rectTransform,
            Vector2.zero,
            new Vector2(0.004f, 1f),
            Vector2.zero,
            Vector2.zero
        );

        Text title = CreateText(
            "Title",
            topRect,
            "诺亚远程通讯",
            34,
            FontStyle.Bold,
            Pale,
            TextAnchor.MiddleLeft
        );
        SetStretchRect(
            title.rectTransform,
            new Vector2(0f, 0.48f),
            new Vector2(0.45f, 1f),
            new Vector2(28f, 0f),
            Vector2.zero
        );

        Text subtitle = CreateText(
            "Subtitle",
            topRect,
            "NOAH QUANTUM RELAY  //  N-03",
            15,
            FontStyle.Normal,
            Muted,
            TextAnchor.MiddleLeft
        );
        SetStretchRect(
            subtitle.rectTransform,
            Vector2.zero,
            new Vector2(0.45f, 0.48f),
            new Vector2(30f, 0f),
            Vector2.zero
        );

        Text connectionStatus = CreateText(
            "ConnectionStatus",
            topRect,
            "信号同步中  ·  延迟 3.2 光年",
            18,
            FontStyle.Normal,
            Green,
            TextAnchor.MiddleRight
        );
        SetStretchRect(
            connectionStatus.rectTransform,
            new Vector2(0.52f, 0f),
            new Vector2(0.83f, 1f),
            Vector2.zero,
            Vector2.zero
        );

        Button closeButton = CreateButton(
            "CloseButton",
            topRect,
            "ESC  断开",
            19,
            new Color(0.08f, 0.19f, 0.23f, 0.96f),
            Pale
        );
        SetStretchRect(
            closeButton.GetComponent<RectTransform>(),
            new Vector2(0.85f, 0.16f),
            new Vector2(0.985f, 0.84f),
            Vector2.zero,
            Vector2.zero
        );

        Text location = CreateText(
            "Location",
            panelRect,
            "NOAH // 近地轨道观测",
            19,
            FontStyle.Bold,
            Pale,
            TextAnchor.MiddleLeft
        );
        SetFixedRect(
            location.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(530f, 42f),
            new Vector2(56f, -132f)
        );

        Text telemetry = CreateText(
            "Telemetry",
            panelRect,
            "LATENCY  03.20 ly    SIGNAL  98.7%    ENCRYPTION  Q-LINK",
            14,
            FontStyle.Normal,
            new Color(0.55f, 0.86f, 0.9f, 0.82f),
            TextAnchor.MiddleLeft
        );
        SetFixedRect(
            telemetry.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(650f, 30f),
            new Vector2(58f, -170f)
        );

        GameObject portraitFrame = CreatePanel(
            "SisterVideoFeed",
            panelRect,
            new Color(0.005f, 0.035f, 0.055f, 0.97f)
        );
        SetStretchRect(
            portraitFrame.GetComponent<RectTransform>(),
            new Vector2(0.705f, 0.31f),
            new Vector2(0.95f, 0.84f),
            Vector2.zero,
            Vector2.zero
        );
        RectTransform portraitRect =
            portraitFrame.GetComponent<RectTransform>();

        CreateOutline(portraitRect, Cyan, 3f);
        Image portrait = CreateImage(
            "YoungerSisterPortrait",
            portraitRect,
            Color.white,
            portraitSprite
        );
        portrait.preserveAspect = true;
        SetStretchRect(
            portrait.rectTransform,
            Vector2.zero,
            Vector2.one,
            new Vector2(10f, 10f),
            new Vector2(-10f, -46f)
        );

        Text feedLabel = CreateText(
            "FeedLabel",
            portraitRect,
            "LIVE  //  NOAH SCHOOL NODE",
            14,
            FontStyle.Bold,
            Green,
            TextAnchor.MiddleCenter
        );
        SetStretchRect(
            feedLabel.rectTransform,
            Vector2.zero,
            new Vector2(1f, 0.075f),
            new Vector2(8f, 4f),
            new Vector2(-8f, 0f)
        );

        GameObject dialoguePanel = CreatePanel(
            "DialoguePanel",
            panelRect,
            new Color(0.005f, 0.035f, 0.055f, 0.94f)
        );
        SetStretchRect(
            dialoguePanel.GetComponent<RectTransform>(),
            new Vector2(0.05f, 0.055f),
            new Vector2(0.95f, 0.285f),
            Vector2.zero,
            Vector2.zero
        );
        RectTransform dialogueRect =
            dialoguePanel.GetComponent<RectTransform>();
        CreateOutline(dialogueRect, new Color(0.13f, 0.72f, 0.8f, 0.9f), 2f);

        Image dialogueAccent = CreateImage(
            "DialogueAccent",
            dialogueRect,
            Cyan
        );
        SetStretchRect(
            dialogueAccent.rectTransform,
            Vector2.zero,
            new Vector2(0.006f, 1f),
            Vector2.zero,
            Vector2.zero
        );

        Text speaker = CreateText(
            "Speaker",
            dialogueRect,
            "系统",
            22,
            FontStyle.Bold,
            Cyan,
            TextAnchor.MiddleLeft
        );
        SetStretchRect(
            speaker.rectTransform,
            new Vector2(0.035f, 0.67f),
            new Vector2(0.22f, 0.93f),
            Vector2.zero,
            Vector2.zero
        );

        Text progress = CreateText(
            "Progress",
            dialogueRect,
            "TRANSMISSION  01/06",
            13,
            FontStyle.Normal,
            Muted,
            TextAnchor.MiddleRight
        );
        SetStretchRect(
            progress.rectTransform,
            new Vector2(0.67f, 0.73f),
            new Vector2(0.955f, 0.92f),
            Vector2.zero,
            Vector2.zero
        );

        Text dialogue = CreateText(
            "Dialogue",
            dialogueRect,
            "正在建立通讯……",
            25,
            FontStyle.Normal,
            Pale,
            TextAnchor.UpperLeft
        );
        dialogue.horizontalOverflow = HorizontalWrapMode.Wrap;
        dialogue.verticalOverflow = VerticalWrapMode.Truncate;
        SetStretchRect(
            dialogue.rectTransform,
            new Vector2(0.035f, 0.17f),
            new Vector2(0.74f, 0.68f),
            Vector2.zero,
            Vector2.zero
        );

        Text puzzleStatus = CreateText(
            "PuzzleStatus",
            dialogueRect,
            "外部模块插槽：NOAH_BIO_PUZZLE",
            14,
            FontStyle.Normal,
            Muted,
            TextAnchor.MiddleLeft
        );
        SetStretchRect(
            puzzleStatus.rectTransform,
            new Vector2(0.035f, 0.02f),
            new Vector2(0.48f, 0.17f),
            Vector2.zero,
            Vector2.zero
        );

        Button puzzleButton = CreateButton(
            "PuzzleHookButton",
            dialogueRect,
            "接入生物拼图",
            18,
            new Color(0.04f, 0.34f, 0.38f, 0.98f),
            Pale
        );
        SetStretchRect(
            puzzleButton.GetComponent<RectTransform>(),
            new Vector2(0.67f, 0.16f),
            new Vector2(0.81f, 0.48f),
            Vector2.zero,
            Vector2.zero
        );

        Button nextButton = CreateButton(
            "NextButton",
            dialogueRect,
            "继续  ›",
            20,
            new Color(0.02f, 0.48f, 0.56f, 0.98f),
            Color.white
        );
        SetStretchRect(
            nextButton.GetComponent<RectTransform>(),
            new Vector2(0.825f, 0.16f),
            new Vector2(0.955f, 0.48f),
            Vector2.zero,
            Vector2.zero
        );

        Text nextLabel = nextButton.GetComponentInChildren<Text>(true);

        PlayerInteractor playerInteractor =
            Object.FindObjectOfType<PlayerInteractor>();
        StationArchiveController archiveController =
            Object.FindObjectOfType<StationArchiveController>();

        List<NoahRemoteDialogueBeat> beats = CreateDialogueBeats();

        controller.Configure(
            panel,
            canvasGroup,
            background,
            portrait,
            portraitFrame,
            connectionStatus,
            location,
            speaker,
            dialogue,
            progress,
            puzzleStatus,
            nextLabel,
            nextButton,
            closeButton,
            puzzleButton,
            planetSprite,
            schoolSprite,
            portraitSprite,
            playerInteractor,
            archiveController,
            beats
        );

        CreateWorldTerminal(controller);
        panel.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = root;

        Debug.Log(
            "Noah Remote：P5-2 已安装。入口位于后墙通讯节点，" +
            "全屏叙事、拼图接口和家族档案解锁已连接。"
        );
    }

    private static List<NoahRemoteDialogueBeat> CreateDialogueBeats()
    {
        return new List<NoahRemoteDialogueBeat>
        {
            new(
                "系统",
                "量子中继校正完成。诺亚星地表通讯已接入。",
                NoahRemoteVisual.PlanetOverview
            ),
            new(
                "妹妹",
                "姐！学校刚下课。今天从教室望出去，诺亚的天空特别亮。",
                NoahRemoteVisual.SchoolConnection
            ),
            new(
                "妹妹",
                "老师讲到地球曾经有真正的森林和成群的动物。轨道库里还保存着它们的数据吗？",
                NoahRemoteVisual.SchoolConnection,
                true
            ),
            new(
                "第079任修复官",
                "还在。基因、生态和文明记录都在等我们重新接通。等修复完成，我带你亲眼去看。",
                NoahRemoteVisual.SchoolConnection
            ),
            new(
                "妹妹",
                "说好了。对了，我把爸爸和爷爷留下的修复官档案发给你了。别总一个人扛着。",
                NoahRemoteVisual.SchoolConnection
            ),
            new(
                "系统",
                "家族加密包接收完成：第077任与第078任修复官档案已解锁。",
                NoahRemoteVisual.PlanetOverview
            )
        };
    }

    private static void CreateWorldTerminal(
        NoahRemoteCommunicationController controller
    )
    {
        GameObject interactionRoot =
            GameObject.Find("InteractionPoints");

        if (interactionRoot == null)
        {
            interactionRoot = new GameObject("InteractionPoints");
        }

        GameObject terminal = new("NoahRemoteTerminal");
        terminal.transform.SetParent(interactionRoot.transform, false);
        terminal.transform.position = new Vector3(
            -5.65f,
            1.70f,
            -6.20f
        );

        BoxCollider detector = terminal.AddComponent<BoxCollider>();
        detector.size = new Vector3(0.76f, 0.92f, 0.18f);
        detector.center = Vector3.zero;
        detector.isTrigger = true;

        NoahRemoteTerminalInteractable interactable =
            terminal.AddComponent<NoahRemoteTerminalInteractable>();
        interactable.Configure(controller, "连接 诺亚远程通讯");

        GameObject worldCanvasObject = new(
            "NoahLinkDisplay",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );
        worldCanvasObject.transform.SetParent(terminal.transform, false);
        worldCanvasObject.transform.localPosition =
            new Vector3(0f, 0f, 0.101f);
        worldCanvasObject.transform.localRotation =
            Quaternion.Euler(0f, 180f, 0f);
        worldCanvasObject.transform.localScale =
            Vector3.one * 0.00078f;

        Canvas worldCanvas = worldCanvasObject.GetComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.sortingOrder = 12;

        RectTransform canvasRect =
            worldCanvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(720f, 900f);

        GameObject screen = CreatePanel(
            "Screen",
            canvasRect,
            new Color(0.004f, 0.035f, 0.055f, 0.94f)
        );
        SetStretchRect(
            screen.GetComponent<RectTransform>(),
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero
        );

        RectTransform screenRect = screen.GetComponent<RectTransform>();
        CreateOutline(screenRect, Cyan, 10f);

        Image signalLine = CreateImage(
            "SignalLine",
            screenRect,
            Green
        );
        SetStretchRect(
            signalLine.rectTransform,
            new Vector2(0.08f, 0.73f),
            new Vector2(0.92f, 0.75f),
            Vector2.zero,
            Vector2.zero
        );

        Text icon = CreateText(
            "RelayIcon",
            screenRect,
            "◈",
            138,
            FontStyle.Normal,
            Cyan,
            TextAnchor.MiddleCenter
        );
        SetStretchRect(
            icon.rectTransform,
            new Vector2(0.08f, 0.43f),
            new Vector2(0.92f, 0.76f),
            Vector2.zero,
            Vector2.zero
        );

        Text title = CreateText(
            "RelayTitle",
            screenRect,
            "NOAH LINK",
            54,
            FontStyle.Bold,
            Pale,
            TextAnchor.MiddleCenter
        );
        SetStretchRect(
            title.rectTransform,
            new Vector2(0.08f, 0.27f),
            new Vector2(0.92f, 0.43f),
            Vector2.zero,
            Vector2.zero
        );

        Text status = CreateText(
            "RelayStatus",
            screenRect,
            "远程通讯  //  READY",
            27,
            FontStyle.Normal,
            Green,
            TextAnchor.MiddleCenter
        );
        SetStretchRect(
            status.rectTransform,
            new Vector2(0.08f, 0.12f),
            new Vector2(0.92f, 0.27f),
            Vector2.zero,
            Vector2.zero
        );
    }

    private static Canvas CreateOverlayCanvas(Transform parent)
    {
        GameObject canvasObject = new(
            "NoahRemoteCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );
        canvasObject.transform.SetParent(parent, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 260;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode =
            CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform rect = canvasObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return canvas;
    }

    private static Sprite PrepareSprite(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer =
            AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = false;
            importer.textureCompression =
                TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void CreateCornerDecorations(RectTransform parent)
    {
        float thickness = 3f;
        float length = 92f;

        CreateCorner(parent, "TopLeft", Vector2.up, Vector2.up,
            new Vector2(length, thickness), new Vector2(42f, -22f));
        CreateCorner(parent, "TopLeftV", Vector2.up, Vector2.up,
            new Vector2(thickness, length), new Vector2(42f, -22f));
        CreateCorner(parent, "BottomRight", Vector2.right, Vector2.right,
            new Vector2(length, thickness), new Vector2(-42f, 22f));
        CreateCorner(parent, "BottomRightV", Vector2.right, Vector2.right,
            new Vector2(thickness, length), new Vector2(-42f, 22f));
    }

    private static void CreateCorner(
        RectTransform parent,
        string name,
        Vector2 anchor,
        Vector2 pivot,
        Vector2 size,
        Vector2 position
    )
    {
        Image image = CreateImage(name, parent, Cyan);
        SetFixedRect(
            image.rectTransform,
            anchor,
            pivot,
            size,
            position
        );
    }

    private static void CreateOutline(
        RectTransform parent,
        Color color,
        float thickness
    )
    {
        Image top = CreateImage("OutlineTop", parent, color);
        SetStretchRect(top.rectTransform, new Vector2(0f, 1f), Vector2.one,
            Vector2.zero, new Vector2(0f, -thickness));

        Image bottom = CreateImage("OutlineBottom", parent, color);
        SetStretchRect(bottom.rectTransform, Vector2.zero,
            new Vector2(1f, 0f), new Vector2(0f, thickness), Vector2.zero);

        Image left = CreateImage("OutlineLeft", parent, color);
        SetStretchRect(left.rectTransform, Vector2.zero,
            new Vector2(0f, 1f), Vector2.zero, new Vector2(thickness, 0f));

        Image right = CreateImage("OutlineRight", parent, color);
        SetStretchRect(right.rectTransform, new Vector2(1f, 0f), Vector2.one,
            new Vector2(-thickness, 0f), Vector2.zero);
    }

    private static GameObject CreatePanel(
        string name,
        Transform parent,
        Color color
    )
    {
        GameObject panel = new(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        panel.transform.SetParent(parent, false);
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private static Image CreateImage(
        string name,
        Transform parent,
        Color color,
        Sprite sprite = null
    )
    {
        GameObject imageObject = new(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.sprite = sprite;
        image.raycastTarget = false;
        return image;
    }

    private static Text CreateText(
        string name,
        Transform parent,
        string value,
        int fontSize,
        FontStyle style,
        Color color,
        TextAnchor alignment
    )
    {
        GameObject textObject = new(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Text)
        );
        textObject.transform.SetParent(parent, false);

        Text text = textObject.GetComponent<Text>();
        text.text = value;
        text.font = GetEditorFont();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(
        string name,
        Transform parent,
        string label,
        int fontSize,
        Color background,
        Color foreground
    )
    {
        GameObject buttonObject = new(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button)
        );
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = background;

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.78f, 1f, 1f, 1f);
        colors.pressedColor = new Color(0.56f, 0.88f, 0.92f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        Text text = CreateText(
            "Label",
            buttonObject.transform,
            label,
            fontSize,
            FontStyle.Bold,
            foreground,
            TextAnchor.MiddleCenter
        );
        SetStretchRect(
            text.rectTransform,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero
        );
        return button;
    }

    private static Font GetEditorFont()
    {
        if (editorFont == null)
        {
            editorFont = Font.CreateDynamicFontFromOSFont(
                new[]
                {
                    "PingFang SC",
                    "Hiragino Sans GB",
                    "Arial"
                },
                36
            );
        }

        return editorFont ?? Resources.GetBuiltinResource<Font>(
            "Arial.ttf"
        );
    }

    private static void SetStretchRect(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax
    )
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static void SetFixedRect(
        RectTransform rect,
        Vector2 anchor,
        Vector2 pivot,
        Vector2 size,
        Vector2 position
    )
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void DestroyExisting(string objectName)
    {
        GameObject existing = FindSceneObject(objectName);

        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
        }
    }

    private static GameObject FindSceneObject(string objectName)
    {
        foreach (GameObject root in SceneManager.GetActiveScene()
                     .GetRootGameObjects())
        {
            Transform[] transforms =
                root.GetComponentsInChildren<Transform>(true);

            foreach (Transform transform in transforms)
            {
                if (transform.name == objectName)
                {
                    return transform.gameObject;
                }
            }
        }

        return null;
    }
}
