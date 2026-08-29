using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class StationArchiveSceneSetup
{
    private const string MVPScenePath =
        "Assets/Scenes/SpaceStationHub_MVP.unity";

    private static readonly Color CanvasBackground =
        new(0.008f, 0.025f, 0.035f, 0.97f);
    private static readonly Color PanelBackground =
        new(0.018f, 0.065f, 0.078f, 0.97f);
    private static readonly Color Cyan =
        new(0.18f, 0.92f, 1f, 1f);
    private static readonly Color PaleText =
        new(0.77f, 0.94f, 0.97f, 1f);
    private static readonly Color MutedText =
        new(0.42f, 0.65f, 0.69f, 1f);

    private static Font editorFont;

    [MenuItem(
        "Tools/Earth Reshaping/Archive/Install In Active Scene"
    )]
    public static void InstallInActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (scene.path != MVPScenePath)
        {
            Debug.LogError(
                "Archive：只允许安装到 SpaceStationHub_MVP 场景，" +
                $"当前场景为 {scene.path}。"
            );
            return;
        }

        GameObject existing = FindSceneObject("MVP_ArchiveSystem");

        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
        }

        GameObject root = new("MVP_ArchiveSystem");
        Undo.RegisterCreatedObjectUndo(root, "Create Station Archive");

        StationArchiveController controller =
            root.AddComponent<StationArchiveController>();

        GameObject canvasObject = new(
            "StationArchiveCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );
        canvasObject.transform.SetParent(root.transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 220;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode =
            CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect =
            canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        Button shortcutButton = CreateButton(
            "ArchiveShortcut",
            canvasRect,
            "[TAB]  空间站资料库",
            21,
            new Color(0.02f, 0.13f, 0.16f, 0.92f),
            PaleText
        );
        SetFixedRect(
            shortcutButton.GetComponent<RectTransform>(),
            Vector2.one,
            Vector2.one,
            new Vector2(276f, 48f),
            new Vector2(-34f, -30f)
        );

        GameObject panel = CreatePanel(
            "ArchivePanel",
            canvasRect,
            CanvasBackground
        );
        SetStretchRect(
            panel.GetComponent<RectTransform>(),
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero
        );

        RectTransform panelRect = panel.GetComponent<RectTransform>();

        Image topLine = CreateImage(
            "TopSignalLine",
            panelRect,
            Cyan
        );
        SetStretchRect(
            topLine.rectTransform,
            new Vector2(0f, 1f),
            Vector2.one,
            new Vector2(42f, -106f),
            new Vector2(-42f, -102f)
        );

        Text headerTitle = CreateText(
            "HeaderTitle",
            panelRect,
            "空间站中央资料库",
            38,
            FontStyle.Bold,
            PaleText,
            TextAnchor.MiddleLeft
        );
        SetFixedRect(
            headerTitle.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(500f, 48f),
            new Vector2(48f, -34f)
        );

        Text headerSubtitle = CreateText(
            "HeaderSubtitle",
            panelRect,
            "EARTH RESTORATION ARCHIVE  //  LOCAL NODE 07",
            15,
            FontStyle.Normal,
            MutedText,
            TextAnchor.MiddleLeft
        );
        SetFixedRect(
            headerSubtitle.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(610f, 26f),
            new Vector2(50f, -66f)
        );

        Button closeButton = CreateButton(
            "CloseButton",
            panelRect,
            "ESC  关闭",
            20,
            new Color(0.12f, 0.24f, 0.27f, 0.96f),
            PaleText
        );
        SetFixedRect(
            closeButton.GetComponent<RectTransform>(),
            Vector2.one,
            Vector2.one,
            new Vector2(164f, 46f),
            new Vector2(-46f, -36f)
        );

        Button civilizationTab = CreateButton(
            "CivilizationTab",
            panelRect,
            "地球文明图鉴",
            22,
            new Color(0.02f, 0.48f, 0.58f, 0.98f),
            Color.white
        );
        SetFixedRect(
            civilizationTab.GetComponent<RectTransform>(),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(250f, 54f),
            new Vector2(48f, -120f)
        );

        Button officerTab = CreateButton(
            "OfficerTab",
            panelRect,
            "历代修复官档案",
            22,
            new Color(0.025f, 0.1f, 0.13f, 0.96f),
            MutedText
        );
        SetFixedRect(
            officerTab.GetComponent<RectTransform>(),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(270f, 54f),
            new Vector2(316f, -120f)
        );

        GameObject listPanel = CreatePanel(
            "ArchiveListPanel",
            panelRect,
            PanelBackground
        );
        SetStretchRect(
            listPanel.GetComponent<RectTransform>(),
            new Vector2(0f, 0f),
            new Vector2(0.34f, 1f),
            new Vector2(48f, 56f),
            new Vector2(-12f, -176f)
        );

        RectTransform listPanelRect =
            listPanel.GetComponent<RectTransform>();
        Text sectionTitle = CreateText(
            "SectionTitle",
            listPanelRect,
            "地球文明图鉴",
            26,
            FontStyle.Bold,
            PaleText,
            TextAnchor.MiddleLeft
        );
        SetStretchRect(
            sectionTitle.rectTransform,
            new Vector2(0f, 1f),
            Vector2.one,
            new Vector2(24f, -62f),
            new Vector2(-24f, -18f)
        );

        Text collectionStatus = CreateText(
            "CollectionStatus",
            listPanelRect,
            "已收录 0/6",
            16,
            FontStyle.Normal,
            MutedText,
            TextAnchor.MiddleLeft
        );
        SetStretchRect(
            collectionStatus.rectTransform,
            new Vector2(0f, 1f),
            Vector2.one,
            new Vector2(24f, -96f),
            new Vector2(-24f, -64f)
        );

        GameObject rowsObject = new(
            "RowsRoot",
            typeof(RectTransform),
            typeof(VerticalLayoutGroup)
        );
        rowsObject.transform.SetParent(listPanelRect, false);
        RectTransform rowsRoot =
            rowsObject.GetComponent<RectTransform>();
        SetStretchRect(
            rowsRoot,
            Vector2.zero,
            Vector2.one,
            new Vector2(22f, 24f),
            new Vector2(-22f, -112f)
        );

        VerticalLayoutGroup layout =
            rowsObject.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 9f;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        Button rowTemplate = CreateButton(
            "ArchiveRowTemplate",
            rowsRoot,
            "ARCHIVE ROW",
            18,
            new Color(0.035f, 0.16f, 0.19f, 0.96f),
            PaleText
        );
        LayoutElement rowLayout =
            rowTemplate.gameObject.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 66f;
        rowLayout.minHeight = 58f;

        Text rowText = rowTemplate.GetComponentInChildren<Text>();
        rowText.alignment = TextAnchor.MiddleLeft;
        rowText.rectTransform.offsetMin = new Vector2(18f, 0f);
        rowText.rectTransform.offsetMax = new Vector2(-12f, 0f);

        GameObject detailPanel = CreatePanel(
            "ArchiveDetailPanel",
            panelRect,
            new Color(0.012f, 0.047f, 0.06f, 0.98f)
        );
        SetStretchRect(
            detailPanel.GetComponent<RectTransform>(),
            new Vector2(0.34f, 0f),
            Vector2.one,
            new Vector2(8f, 56f),
            new Vector2(-48f, -176f)
        );

        RectTransform detailRect =
            detailPanel.GetComponent<RectTransform>();
        Image accentBar = CreateImage("AccentBar", detailRect, Cyan);
        SetStretchRect(
            accentBar.rectTransform,
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(0f, 0f),
            new Vector2(5f, 0f)
        );

        Text entryId = CreateText(
            "EntryId",
            detailRect,
            "CIV-001",
            21,
            FontStyle.Bold,
            Cyan,
            TextAnchor.MiddleLeft
        );
        SetStretchRect(
            entryId.rectTransform,
            new Vector2(0f, 1f),
            Vector2.one,
            new Vector2(42f, -68f),
            new Vector2(-42f, -28f)
        );

        Text entryTitle = CreateText(
            "EntryTitle",
            detailRect,
            "深海蓝鲸",
            38,
            FontStyle.Bold,
            PaleText,
            TextAnchor.MiddleLeft
        );
        SetStretchRect(
            entryTitle.rectTransform,
            new Vector2(0f, 1f),
            Vector2.one,
            new Vector2(42f, -126f),
            new Vector2(-42f, -76f)
        );

        Text entrySubtitle = CreateText(
            "EntrySubtitle",
            detailRect,
            "海洋生态 / 旗舰物种",
            20,
            FontStyle.Normal,
            new Color(0.52f, 0.8f, 0.84f, 1f),
            TextAnchor.MiddleLeft
        );
        SetStretchRect(
            entrySubtitle.rectTransform,
            new Vector2(0f, 1f),
            Vector2.one,
            new Vector2(42f, -165f),
            new Vector2(-42f, -132f)
        );

        Text classification = CreateText(
            "Classification",
            detailRect,
            "CLASSIFICATION // BIOSPHERE",
            15,
            FontStyle.Normal,
            MutedText,
            TextAnchor.MiddleLeft
        );
        SetStretchRect(
            classification.rectTransform,
            new Vector2(0f, 1f),
            Vector2.one,
            new Vector2(42f, -206f),
            new Vector2(-42f, -177f)
        );

        Image divider = CreateImage(
            "DetailDivider",
            detailRect,
            new Color(0.12f, 0.48f, 0.53f, 0.55f)
        );
        SetStretchRect(
            divider.rectTransform,
            new Vector2(0f, 1f),
            Vector2.one,
            new Vector2(42f, -220f),
            new Vector2(-42f, -218f)
        );

        Text body = CreateText(
            "Body",
            detailRect,
            "资料正文",
            22,
            FontStyle.Normal,
            new Color(0.72f, 0.86f, 0.88f, 1f),
            TextAnchor.UpperLeft
        );
        body.horizontalOverflow = HorizontalWrapMode.Wrap;
        body.verticalOverflow = VerticalWrapMode.Overflow;
        body.lineSpacing = 1.22f;
        SetStretchRect(
            body.rectTransform,
            Vector2.zero,
            Vector2.one,
            new Vector2(42f, 86f),
            new Vector2(-42f, -244f)
        );

        Text lockHint = CreateText(
            "LockHint",
            detailRect,
            "ARCHIVE VERIFIED",
            16,
            FontStyle.Bold,
            new Color(0.35f, 1f, 0.78f, 1f),
            TextAnchor.MiddleLeft
        );
        SetStretchRect(
            lockHint.rectTransform,
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(42f, 30f),
            new Vector2(-42f, 70f)
        );

        PlayerInteractor interactor =
            Object.FindObjectOfType<PlayerInteractor>();
        MVPFlowController flow =
            Object.FindObjectOfType<MVPFlowController>();

        controller.Configure(
            shortcutButton.gameObject,
            shortcutButton,
            panel,
            closeButton,
            civilizationTab,
            officerTab,
            civilizationTab.GetComponentInChildren<Text>(),
            officerTab.GetComponentInChildren<Text>(),
            rowsRoot,
            rowTemplate,
            sectionTitle,
            collectionStatus,
            entryId,
            entryTitle,
            entrySubtitle,
            classification,
            body,
            lockHint,
            interactor,
            flow,
            CreateDefaultEntries()
        );

        rowTemplate.gameObject.SetActive(false);
        panel.SetActive(false);

        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = root;

        Debug.Log(
            "Archive：已安装中央资料库。TAB 打开，包含 6 个文明图鉴" +
            "条目与 3 份重点修复官档案，并接入 0～3 修复阶段。",
            root
        );
    }

    [MenuItem(
        "Tools/Earth Reshaping/Archive/Open Archive (Play Mode)"
    )]
    public static void OpenArchiveInPlayMode()
    {
        StationArchiveController controller =
            Object.FindObjectOfType<StationArchiveController>();

        if (controller == null)
        {
            Debug.LogError("Archive：当前场景没有资料库控制器。");
            return;
        }

        controller.OpenArchive();
    }

    [MenuItem(
        "Tools/Earth Reshaping/Archive/Show Officer Files (Play Mode)"
    )]
    public static void ShowOfficerFilesInPlayMode()
    {
        StationArchiveController controller =
            Object.FindObjectOfType<StationArchiveController>();

        if (controller == null)
        {
            Debug.LogError("Archive：当前场景没有资料库控制器。");
            return;
        }

        controller.OpenArchive();
        controller.ShowOfficerCategory();
    }

    [MenuItem(
        "Tools/Earth Reshaping/Archive/Close Archive (Play Mode)"
    )]
    public static void CloseArchiveInPlayMode()
    {
        StationArchiveController controller =
            Object.FindObjectOfType<StationArchiveController>();

        if (controller != null)
        {
            controller.CloseArchive();
        }
    }

    [MenuItem(
        "Tools/Earth Reshaping/Archive/Open Archive (Play Mode)",
        true
    )]
    [MenuItem(
        "Tools/Earth Reshaping/Archive/Show Officer Files (Play Mode)",
        true
    )]
    [MenuItem(
        "Tools/Earth Reshaping/Archive/Close Archive (Play Mode)",
        true
    )]
    private static bool ValidatePlayModeMenu()
    {
        return Application.isPlaying;
    }

    private static List<ArchiveEntryData> CreateDefaultEntries()
    {
        return new List<ArchiveEntryData>
        {
            new(
                ArchiveCategory.Civilization,
                "CIV-001",
                "深海蓝鲸",
                "海洋生态 / 旗舰物种",
                "BIOSPHERE // OCEAN",
                "地球上体型最大的生命，也是深海生态网络的重要指标。" +
                "它们的迁徙路线记录着洋流、温度与食物链的变化。\n\n" +
                "重塑计划保留了蓝鲸基因组、声纹档案及迁徙模型，" +
                "等待海洋环境重新具备承载大型鲸类的条件。",
                0
            ),
            new(
                ArchiveCategory.Civilization,
                "CIV-014",
                "银杏种源",
                "植物文明 / 活化石",
                "BIOSPHERE // FLORA",
                "银杏跨越漫长地质年代存续至今，拥有出色的环境适应力。" +
                "它既是城市记忆的一部分，也是植物演化史的活档案。\n\n" +
                "种质库保存了来自不同纬度的样本，作为地表植被恢复的" +
                "长期候选物种。",
                0
            ),
            new(
                ArchiveCategory.Civilization,
                "CIV-027",
                "珊瑚礁生态",
                "海洋生态 / 生物城市",
                "BIOSPHERE // REEF",
                "珊瑚礁占据的海域有限，却庇护着极高比例的海洋物种。" +
                "温度、酸碱度与微生物群落必须同时稳定，珊瑚才可能重建。\n\n" +
                "轨道巡检恢复后，空间站重新取得了适合珊瑚生态投放的" +
                "海域候选数据。",
                1
            ),
            new(
                ArchiveCategory.Civilization,
                "CIV-036",
                "授粉蜂群",
                "陆地生态 / 关键协作者",
                "BIOSPHERE // POLLINATOR",
                "蜂群并非孤立物种，而是开花植物与农业系统之间的连接者。" +
                "恢复授粉网络，意味着让植物群落重新拥有自我繁衍能力。\n\n" +
                "生态投放完成后，蜂群行为模型与抗逆种群资料已解除封存。",
                1
            ),
            new(
                ArchiveCategory.Civilization,
                "CIV-052",
                "水稻种质",
                "农业文明 / 主粮基因库",
                "CIVILIZATION // AGRICULTURE",
                "水稻承载着人类定居、灌溉与季节协作的共同记忆。" +
                "档案同时保存耐盐、耐旱及高海拔品系，用于适配修复后的" +
                "多样地表环境。\n\n" +
                "基因培育链路恢复后，该种质包可进入下一轮活性评估。",
                2
            ),
            new(
                ArchiveCategory.Civilization,
                "CIV-078",
                "地衣先锋群落",
                "极端生态 / 第一批定居者",
                "BIOSPHERE // PIONEER",
                "地衣能够在贫瘠表面缓慢分解岩石、积累有机质，为后续" +
                "植物群落创造最初的土壤条件。\n\n" +
                "三条修复链路全部通过后，先锋群落方案被列为地表复苏的" +
                "第一批长期投放候选。",
                3
            ),
            new(
                ArchiveCategory.RepairOfficer,
                "OFF-001",
                "第001任 · 首任守望者",
                "地球重塑计划初始记录",
                "PERSONNEL // FOUNDING ARCHIVE",
                "首任修复官建立了跨世纪交接制度，将地球环境监测、" +
                "种质保存与轨道设施维护写入同一份长期协议。\n\n" +
                "其身份资料已随早期迁移记录受损，但留下的第一条原则仍被" +
                "每一任修复官沿用：修复不是一次行动，而是一代代人的接力。",
                0
            ),
            new(
                ArchiveCategory.RepairOfficer,
                "OFF-077",
                "第077任 · 祖父档案",
                "星际迁徙见证者 / 家族交接记录",
                "PERSONNEL // LEGACY ARCHIVE",
                "第七十七任修复官亲历了迁徙年代的关键阶段。档案中既有" +
                "诺亚星生活片段，也有他对地球旧城市、学校与家庭记忆的口述。\n\n" +
                "这些资料并不要求我们立刻建造另一座城市；它们首先作为" +
                "理解修复官为何坚持返回地球的情感证据被保存。",
                2
            ),
            new(
                ArchiveCategory.RepairOfficer,
                "OFF-078",
                "第078任 · 父亲档案",
                "上一任修复官 / 最后任务记录",
                "PERSONNEL // RESTRICTED",
                "上一任修复官在三年前的高辐射观测任务中失联。空间站" +
                "保留了他的任务清单、环境读数与未完成的交接留言。\n\n" +
                "当三条核心修复链路重新连通后，最后一段记录被解密：" +
                "他没有要求继任者复制自己的道路，只希望地球再次成为" +
                "人类可以选择回去的家。",
                3
            )
        };
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
        Color color
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
        Color textColor
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
        colors.highlightedColor = new Color(0.76f, 1f, 1f, 1f);
        colors.pressedColor = new Color(0.45f, 0.82f, 0.87f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        Text text = CreateText(
            "Label",
            buttonObject.transform,
            label,
            fontSize,
            FontStyle.Bold,
            textColor,
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
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static Font GetEditorFont()
    {
        if (editorFont == null)
        {
            editorFont = Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf"
            );
        }

        return editorFont;
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
