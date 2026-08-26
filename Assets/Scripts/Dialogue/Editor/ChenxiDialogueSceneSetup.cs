using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ChenxiDialogueSceneSetup
{
    private const string MVPScenePath =
        "Assets/Scenes/SpaceStationHub_MVP.unity";

    private const string PortraitPath =
        "Assets/Arts/UI/Characters/Chenxi_Avatar.png";

    private const string DialogueFolder =
        "Assets/Settings/Dialogue";

    private const string VoiceFolder =
        "Assets/Arts/Audio/Chenxi";

    [MenuItem(
        "Tools/Earth Reshaping/Dialogue/Install Chenxi Dialogue"
    )]
    public static void InstallChenxiDialogue()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (scene.path != MVPScenePath)
        {
            Debug.LogError(
                "晨曦对话：只允许安装到 SpaceStationHub_MVP 场景。"
            );
            return;
        }

        ConfigurePortraitImporter();
        EnsureDialogueFolder();

        DialogueSequence intro = CreateOrUpdateSequence(
            "Chenxi_Intro",
            new[]
            {
                Line(
                    "Chenxi_Intro_01",
                    "第七十九任修复官，你好。欢迎抵达归墟同步轨道空间站。"
                ),
                Line(
                    "Chenxi_Intro_02",
                    "我是空间站 AI 管家「晨曦」。三项今日修复任务已经就绪。"
                ),
                Line(
                    "Chenxi_Intro_03",
                    "请先前往右侧星际轨道控制台，完成轨道巡检。"
                )
            }
        );

        DialogueSequence orbitComplete = CreateOrUpdateSequence(
            "Chenxi_OrbitComplete",
            new[]
            {
                Line(
                    "Chenxi_OrbitComplete_01",
                    "轨道巡检数据已确认。近地轨道能量传输恢复稳定。"
                ),
                Line(
                    "Chenxi_OrbitComplete_02",
                    "Earth 修复进度已经推进，生态投放终端现已获得授权。"
                )
            }
        );

        DialogueSequence ecologyComplete = CreateOrUpdateSequence(
            "Chenxi_EcologyComplete",
            new[]
            {
                Line(
                    "Chenxi_EcologyComplete_01",
                    "生态营养投放完成。地表修复区域开始出现生命信号。"
                ),
                Line(
                    "Chenxi_EcologyComplete_02",
                    "请前往基因培育终端，执行今日最后一项修复任务。"
                )
            }
        );

        DialogueSequence geneComplete = CreateOrUpdateSequence(
            "Chenxi_GeneComplete",
            new[]
            {
                Line(
                    "Chenxi_GeneComplete_01",
                    "基因培育任务完成，无人机群已经进入投放序列。"
                )
            }
        );

        DialogueSequence allComplete = CreateOrUpdateSequence(
            "Chenxi_AllTasksComplete",
            new[]
            {
                Line(
                    "Chenxi_AllTasksComplete_01",
                    "今日三项修复任务已经全部完成。"
                ),
                Line(
                    "Chenxi_AllTasksComplete_02",
                    "修复官，请看向舷窗——Earth 正在回应我们的努力。"
                )
            }
        );

        GameObject interactionUI = FindSceneObject("InteractionUI");

        if (interactionUI == null)
        {
            Debug.LogError("晨曦对话：没有找到 InteractionUI Canvas。");
            return;
        }

        GameObject systemRoot = FindSceneObject("ChenxiDialogueSystem");

        if (systemRoot == null)
        {
            systemRoot = new GameObject("ChenxiDialogueSystem");
            Undo.RegisterCreatedObjectUndo(
                systemRoot,
                "Create Chenxi Dialogue System"
            );
        }

        ChenxiDialogueController controller =
            GetOrAddComponent<ChenxiDialogueController>(systemRoot);
        AudioSource voiceSource = GetOrAddComponent<AudioSource>(systemRoot);
        ConfigureVoiceSource(voiceSource);

        GameObject panel = FindChild(interactionUI, "ChenxiDialoguePanel");

        if (
            panel != null &&
            (
                FindChild(panel, "Portrait") == null ||
                FindChild(panel, "SpeakerText") == null ||
                FindChild(panel, "BodyText") == null ||
                FindChild(panel, "ContinueHint") == null
            )
        )
        {
            panel.SetActive(false);
            panel.name = "ChenxiDialoguePanel_Incomplete";
            panel = null;
        }

        if (panel == null)
        {
            panel = CreateDialoguePanel(interactionUI.transform);
        }

        Image portrait =
            FindChild(panel, "Portrait").GetComponent<Image>();
        Text speaker =
            FindChild(panel, "SpeakerText").GetComponent<Text>();
        Text body =
            FindChild(panel, "BodyText").GetComponent<Text>();
        Text hint =
            FindChild(panel, "ContinueHint").GetComponent<Text>();
        Button button = panel.GetComponent<Button>();
        Sprite portraitSprite =
            AssetDatabase.LoadAssetAtPath<Sprite>(PortraitPath);

        portrait.sprite = portraitSprite;
        portrait.preserveAspect = true;

        controller.ConfigureUI(
            panel,
            portrait,
            speaker,
            body,
            hint,
            button,
            portraitSprite
        );
        controller.ConfigureSceneReferences(
            Object.FindObjectOfType<PlayerInteractor>(),
            Object.FindObjectOfType<MVPFlowController>()
        );
        controller.ConfigureVoice(voiceSource);
        controller.ConfigureSequences(
            intro,
            orbitComplete,
            ecologyComplete,
            geneComplete,
            allComplete
        );

        panel.SetActive(false);
        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(panel);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = systemRoot;

        Debug.Log(
            "晨曦对话：已接入开场、三项任务完成和全部完成对话。",
            systemRoot
        );
    }

    [MenuItem(
        "Tools/Earth Reshaping/Dialogue/Log Voice Status (Play Mode)",
        false,
        30
    )]
    private static void LogVoiceStatus()
    {
        ChenxiDialogueController controller =
            Object.FindObjectOfType<ChenxiDialogueController>(true);

        if (controller == null)
        {
            Debug.LogWarning("晨曦语音：没有找到对话控制器。");
            return;
        }

        Debug.Log(
            $"晨曦语音：clip={controller.CurrentVoiceClipName}, " +
            $"playing={controller.IsVoicePlaying}, " +
            $"time={controller.CurrentVoicePlaybackTime:F2}s",
            controller
        );
    }

    [MenuItem(
        "Tools/Earth Reshaping/Dialogue/Log Voice Status (Play Mode)",
        true
    )]
    private static bool ValidateLogVoiceStatus() => Application.isPlaying;

    [MenuItem(
        "Tools/Earth Reshaping/Dialogue/Replay Current Voice (Play Mode)",
        false,
        31
    )]
    private static void ReplayCurrentVoice()
    {
        ChenxiDialogueController controller =
            Object.FindObjectOfType<ChenxiDialogueController>(true);

        if (controller == null)
        {
            Debug.LogWarning("晨曦语音：没有找到对话控制器。");
            return;
        }

        bool started = controller.ReplayCurrentVoiceForDevelopment();
        Debug.Log(
            $"晨曦语音：重新播放 {controller.CurrentVoiceClipName}, " +
            $"started={started}",
            controller
        );
    }

    [MenuItem(
        "Tools/Earth Reshaping/Dialogue/Replay Current Voice (Play Mode)",
        true
    )]
    private static bool ValidateReplayCurrentVoice() => Application.isPlaying;

    private static GameObject CreateDialoguePanel(Transform canvasRoot)
    {
        GameObject panel = CreateUIObject(
            "ChenxiDialoguePanel",
            canvasRoot,
            typeof(Image),
            typeof(Outline),
            typeof(Button)
        );

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 34f);
        panelRect.sizeDelta = new Vector2(1060f, 190f);

        Image background = panel.GetComponent<Image>();
        background.color = new Color(0.008f, 0.025f, 0.045f, 0.94f);

        Outline outline = panel.GetComponent<Outline>();
        outline.effectColor = new Color(0.1f, 0.85f, 0.95f, 0.72f);
        outline.effectDistance = new Vector2(2f, -2f);

        Button button = panel.GetComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.93f, 1f, 1f, 1f);
        colors.pressedColor = new Color(0.8f, 0.95f, 1f, 1f);
        button.colors = colors;

        GameObject topLine = CreateUIObject(
            "TopGlowLine",
            panel.transform,
            typeof(Image)
        );
        SetRect(
            topLine.GetComponent<RectTransform>(),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            Vector2.zero,
            new Vector2(0f, 3f)
        );
        topLine.GetComponent<Image>().color =
            new Color(0.12f, 0.95f, 1f, 0.9f);

        GameObject portraitFrame = CreateUIObject(
            "PortraitFrame",
            panel.transform,
            typeof(Image),
            typeof(Outline)
        );
        SetRect(
            portraitFrame.GetComponent<RectTransform>(),
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(22f, 0f),
            new Vector2(146f, 146f)
        );
        portraitFrame.GetComponent<Image>().color =
            new Color(0.015f, 0.11f, 0.16f, 0.96f);
        portraitFrame.GetComponent<Outline>().effectColor =
            new Color(0.15f, 0.95f, 1f, 0.9f);

        GameObject portrait = CreateUIObject(
            "Portrait",
            portraitFrame.transform,
            typeof(Image)
        );
        SetRect(
            portrait.GetComponent<RectTransform>(),
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(-10f, -10f)
        );
        portrait.GetComponent<Image>().raycastTarget = false;

        GameObject speaker = CreateText(
            "SpeakerText",
            panel.transform,
            "晨曦  ·  CHENXI",
            24,
            FontStyle.Bold,
            new Color(0.38f, 0.95f, 1f, 1f),
            TextAnchor.MiddleLeft
        );
        SetRect(
            speaker.GetComponent<RectTransform>(),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(190f, -24f),
            new Vector2(720f, 38f)
        );

        GameObject body = CreateText(
            "BodyText",
            panel.transform,
            string.Empty,
            27,
            FontStyle.Normal,
            new Color(0.9f, 0.97f, 1f, 1f),
            TextAnchor.UpperLeft
        );
        SetRect(
            body.GetComponent<RectTransform>(),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(190f, -67f),
            new Vector2(830f, 76f)
        );
        body.GetComponent<Text>().horizontalOverflow =
            HorizontalWrapMode.Wrap;
        body.GetComponent<Text>().verticalOverflow =
            VerticalWrapMode.Truncate;

        GameObject hint = CreateText(
            "ContinueHint",
            panel.transform,
            "点击或空格继续  ›",
            17,
            FontStyle.Normal,
            new Color(0.5f, 0.78f, 0.84f, 1f),
            TextAnchor.MiddleRight
        );
        SetRect(
            hint.GetComponent<RectTransform>(),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(-28f, 13f),
            new Vector2(390f, 30f)
        );

        return panel;
    }

    private static DialogueSequence CreateOrUpdateSequence(
        string assetName,
        DialogueLine[] lines
    )
    {
        string path = $"{DialogueFolder}/{assetName}.asset";
        DialogueSequence sequence =
            AssetDatabase.LoadAssetAtPath<DialogueSequence>(path);

        if (sequence == null)
        {
            sequence = ScriptableObject.CreateInstance<DialogueSequence>();
            AssetDatabase.CreateAsset(sequence, path);
        }

        sequence.Configure(assetName, true, lines);
        EditorUtility.SetDirty(sequence);
        return sequence;
    }

    private static DialogueLine Line(string clipName, string text)
    {
        AudioClip clip = LoadVoiceClip(clipName);
        return new DialogueLine("晨曦", text, clip);
    }

    private static AudioClip LoadVoiceClip(string clipName)
    {
        string path = $"{VoiceFolder}/{clipName}.wav";
        AssetDatabase.ImportAsset(
            path,
            ImportAssetOptions.ForceSynchronousImport
        );

        AudioImporter importer =
            AssetImporter.GetAtPath(path) as AudioImporter;

        if (importer == null)
        {
            Debug.LogWarning($"晨曦语音：没有找到 {path}。");
            return null;
        }

        AudioImporterSampleSettings settings =
            importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.CompressedInMemory;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = 0.72f;
        settings.sampleRateSetting =
            AudioSampleRateSetting.OptimizeSampleRate;
        settings.preloadAudioData = true;

        importer.forceToMono = true;
        importer.loadInBackground = false;
        importer.defaultSampleSettings = settings;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }

    private static void ConfigureVoiceSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = false;
        source.volume = 0.86f;
        source.pitch = 1f;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.priority = 64;
    }

    private static void ConfigurePortraitImporter()
    {
        AssetDatabase.ImportAsset(
            PortraitPath,
            ImportAssetOptions.ForceSynchronousImport
        );

        TextureImporter importer =
            AssetImporter.GetAtPath(PortraitPath) as TextureImporter;

        if (importer == null)
        {
            Debug.LogError($"晨曦对话：没有找到头像 {PortraitPath}。");
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 1024;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.SaveAndReimport();
    }

    private static void EnsureDialogueFolder()
    {
        if (!AssetDatabase.IsValidFolder(DialogueFolder))
        {
            AssetDatabase.CreateFolder("Assets/Settings", "Dialogue");
        }
    }

    private static GameObject CreateText(
        string name,
        Transform parent,
        string value,
        int fontSize,
        FontStyle style,
        Color color,
        TextAnchor alignment
    )
    {
        GameObject textObject = CreateUIObject(
            name,
            parent,
            typeof(Text)
        );
        Text text = textObject.GetComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>(
            "LegacyRuntime.ttf"
        );
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        return textObject;
    }

    private static GameObject CreateUIObject(
        string name,
        Transform parent,
        params System.Type[] components
    )
    {
        GameObject result = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer)
        );
        Undo.RegisterCreatedObjectUndo(result, $"Create {name}");
        result.transform.SetParent(parent, false);

        foreach (System.Type componentType in components)
        {
            if (result.GetComponent(componentType) == null)
            {
                Undo.AddComponent(result, componentType);
            }
        }

        return result;
    }

    private static void SetRect(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 position,
        Vector2 size
    )
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
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
            GameObject found = FindChild(root, objectName);

            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static GameObject FindChild(
        GameObject root,
        string objectName
    )
    {
        if (root == null)
        {
            return null;
        }

        foreach (Transform candidate in
            root.GetComponentsInChildren<Transform>(true))
        {
            if (
                candidate != null &&
                candidate.name == objectName
            )
            {
                return candidate.gameObject;
            }
        }

        return null;
    }
}
