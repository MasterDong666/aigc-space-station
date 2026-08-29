using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EarthRestorationSceneSetup
{
    private const string MvpScenePath =
        "Assets/Scenes/SpaceStationHub_MVP.unity";
    private const string OriginalMaterialPath =
        "Assets/Arts/Materials/地球.mat";
    private const string MvpMaterialPath =
        "Assets/Arts/Materials/MVP_EarthRestoration.mat";
    private const string ShaderName =
        "Earth Reshaping/MVP Earth Restoration";

    [MenuItem(
        "Tools/Earth Reshaping/Earth/Install Restoration Visual"
    )]
    public static void Install()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (scene.path != MvpScenePath)
        {
            Debug.LogError(
                "Earth Restoration：请先打开 " + MvpScenePath
            );
            return;
        }

        GameObject earthSystem = GameObject.Find("EarthSystem");
        MVPFlowController flowController =
            Object.FindObjectOfType<MVPFlowController>(true);

        if (earthSystem == null || flowController == null)
        {
            Debug.LogError(
                "Earth Restoration：未找到 EarthSystem 或 MVPFlowController。"
            );
            return;
        }

        Renderer earthRenderer =
            earthSystem.GetComponentInChildren<Renderer>(true);

        if (earthRenderer == null)
        {
            Debug.LogError("Earth Restoration：Earth 没有 Renderer。");
            return;
        }

        Shader shader = Shader.Find(ShaderName);

        if (shader == null)
        {
            Debug.LogError(
                "Earth Restoration：未找到 Shader：" + ShaderName
            );
            return;
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(
            MvpMaterialPath
        );

        if (material == null)
        {
            material = new Material(shader)
            {
                name = "MVP_EarthRestoration"
            };
            AssetDatabase.CreateAsset(material, MvpMaterialPath);
        }
        else
        {
            material.shader = shader;
        }

        Material originalMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(OriginalMaterialPath);

        if (originalMaterial != null)
        {
            Texture earthTexture = originalMaterial.GetTexture("_BaseMap");
            material.SetTexture("_BaseMap", earthTexture);
            material.SetTextureScale(
                "_BaseMap",
                originalMaterial.GetTextureScale("_BaseMap")
            );
            material.SetTextureOffset(
                "_BaseMap",
                originalMaterial.GetTextureOffset("_BaseMap")
            );
        }

        SetDamagedMaterialDefaults(material);
        EditorUtility.SetDirty(material);

        Undo.RecordObject(
            earthRenderer,
            "Assign MVP Earth restoration material"
        );
        earthRenderer.sharedMaterial = material;

        EarthRestorationController restorationController =
            earthSystem.GetComponent<EarthRestorationController>();

        if (restorationController == null)
        {
            restorationController = Undo.AddComponent<
                EarthRestorationController
            >(earthSystem);
        }

        Undo.RecordObject(
            restorationController,
            "Configure MVP Earth restoration"
        );
        restorationController.Configure(
            earthRenderer,
            earthRenderer.transform,
            flowController
        );
        EditorUtility.SetDirty(restorationController);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Selection.activeGameObject = earthSystem;
        Debug.Log(
            "Earth Restoration：已安装四阶段视觉控制，" +
            "原始 Earth FBX、贴图和地球.mat 均未修改。",
            earthSystem
        );
    }

    [MenuItem(
        "Tools/Earth Reshaping/Earth/Preview Stage 0 - Damaged",
        false,
        20
    )]
    private static void PreviewStage0() => PreviewStage(0);

    [MenuItem(
        "Tools/Earth Reshaping/Earth/Preview Stage 1 - Orbit",
        false,
        21
    )]
    private static void PreviewStage1() => PreviewStage(1);

    [MenuItem(
        "Tools/Earth Reshaping/Earth/Preview Stage 2 - Ecology",
        false,
        22
    )]
    private static void PreviewStage2() => PreviewStage(2);

    [MenuItem(
        "Tools/Earth Reshaping/Earth/Preview Stage 3 - Restored",
        false,
        23
    )]
    private static void PreviewStage3() => PreviewStage(3);

    [MenuItem(
        "Tools/Earth Reshaping/Earth/Preview Stage 0 - Damaged",
        true
    )]
    [MenuItem(
        "Tools/Earth Reshaping/Earth/Preview Stage 1 - Orbit",
        true
    )]
    [MenuItem(
        "Tools/Earth Reshaping/Earth/Preview Stage 2 - Ecology",
        true
    )]
    [MenuItem(
        "Tools/Earth Reshaping/Earth/Preview Stage 3 - Restored",
        true
    )]
    private static bool ValidatePreviewStage() => Application.isPlaying;

    private static void PreviewStage(int stage)
    {
        EarthRestorationController controller =
            Object.FindObjectOfType<EarthRestorationController>(true);

        if (controller == null)
        {
            Debug.LogWarning(
                "Earth Restoration：当前 Scene 尚未安装视觉控制器。"
            );
            return;
        }

        controller.SetStage(stage);
        Debug.Log($"Earth Restoration：预览阶段 {stage}。", controller);
    }

    private static void SetDamagedMaterialDefaults(Material material)
    {
        material.SetColor(
            "_Tint",
            new Color(0.82f, 0.78f, 0.70f, 1f)
        );
        material.SetFloat("_Saturation", 0.10f);
        material.SetFloat("_Brightness", 0.66f);
        material.SetColor(
            "_ContaminationColor",
            new Color(0.52f, 0.34f, 0.17f, 1f)
        );
        material.SetFloat("_Contamination", 0.82f);
        material.SetColor(
            "_AtmosphereColor",
            new Color(0.30f, 0.11f, 0.025f, 1f)
        );
        material.SetFloat("_AtmosphereStrength", 0.08f);
        material.SetFloat("_FresnelPower", 4.4f);
    }
}
