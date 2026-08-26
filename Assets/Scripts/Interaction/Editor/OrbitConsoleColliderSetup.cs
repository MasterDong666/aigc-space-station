using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OrbitConsoleColliderSetup
{
    private const string MvpScenePath =
        "Assets/Scenes/SpaceStationHub_MVP.unity";
    private const string ConsoleName = "OrbitControlConsole";
    private const string BaseColliderName =
        "OrbitConsolePhysical_Base";
    private const string ScreenColliderName =
        "OrbitConsolePhysical_Screen";

    [MenuItem(
        "Tools/Earth Reshaping/Interaction/Fix Orbit Console Collision"
    )]
    public static void Install()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (scene.path != MvpScenePath)
        {
            Debug.LogError(
                "轨道控制台：请先打开 " + MvpScenePath
            );
            return;
        }

        GameObject console = FindSceneObject(ConsoleName);

        if (console == null)
        {
            Debug.LogError("轨道控制台：没有找到交互对象。");
            return;
        }

        BoxCollider interactionCollider =
            console.GetComponent<BoxCollider>();

        if (interactionCollider == null)
        {
            interactionCollider = Undo.AddComponent<BoxCollider>(console);
        }

        Undo.RecordObject(
            interactionCollider,
            "Configure orbit console interaction volume"
        );
        interactionCollider.isTrigger = true;
        interactionCollider.center = new Vector3(0f, 0.18f, 0f);
        interactionCollider.size = new Vector3(0.34f, 1.95f, 1.4f);

        BoxCollider baseCollider = ConfigurePhysicalCollider(
            console.transform,
            BaseColliderName,
            new Vector3(0f, -0.42f, 0f),
            new Vector3(0.5f, 1.55f, 0.5f)
        );
        BoxCollider screenCollider = ConfigurePhysicalCollider(
            console.transform,
            ScreenColliderName,
            new Vector3(0f, 0.48f, 0f),
            new Vector3(0.42f, 0.72f, 0.86f)
        );

        EditorUtility.SetDirty(interactionCollider);
        EditorUtility.SetDirty(baseCollider);
        EditorUtility.SetDirty(screenCollider);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Selection.activeGameObject = console;
        Debug.Log(
            "轨道控制台：已扩大正面交互区，并添加底座与屏幕实体碰撞。",
            console
        );
    }

    [MenuItem(
        "Tools/Earth Reshaping/Interaction/Validate Orbit Console (Play Mode)",
        false,
        20
    )]
    private static void ValidateInPlayMode()
    {
        GameObject console = FindSceneObject(ConsoleName);
        PlayerInteractor interactor =
            Object.FindObjectOfType<PlayerInteractor>();
        CharacterController characterController =
            Object.FindObjectOfType<CharacterController>();

        if (
            console == null ||
            interactor == null ||
            characterController == null
        )
        {
            Debug.LogError(
                "轨道控制台验证：缺少 Console、PlayerInteractor " +
                "或 CharacterController。"
            );
            return;
        }

        BoxCollider interactionCollider =
            console.GetComponent<BoxCollider>();
        BoxCollider[] physicalColliders = console
            .GetComponentsInChildren<BoxCollider>(true)
            .Where(collider => !collider.isTrigger)
            .ToArray();

        bool interactionReady =
            interactionCollider != null &&
            interactionCollider.isTrigger &&
            interactionCollider.size.y >= 1.9f &&
            interactionCollider.size.z >= 1.35f;
        bool physicalReady = physicalColliders.Length >= 2;
        bool sweepBlocked = physicalReady &&
            TestCharacterControllerSweep(
                characterController,
                physicalColliders[0]
            );

        Debug.Log(
            "轨道控制台验证：" +
            $"interactionReady={interactionReady}, " +
            $"physicalColliders={physicalColliders.Length}, " +
            $"characterSweepBlocked={sweepBlocked}, " +
            $"currentPrompt={interactor.CurrentPrompt}",
            console
        );
    }

    [MenuItem(
        "Tools/Earth Reshaping/Interaction/Validate Orbit Console (Play Mode)",
        true
    )]
    private static bool ValidateMenu() => Application.isPlaying;

    private static BoxCollider ConfigurePhysicalCollider(
        Transform parent,
        string objectName,
        Vector3 center,
        Vector3 size
    )
    {
        Transform child = parent.Find(objectName);

        if (child == null)
        {
            GameObject childObject = new GameObject(objectName);
            Undo.RegisterCreatedObjectUndo(
                childObject,
                "Create orbit console physical collider"
            );
            childObject.transform.SetParent(parent, false);
            child = childObject.transform;
        }

        Undo.RecordObject(
            child,
            "Reset orbit console collider transform"
        );
        child.localPosition = Vector3.zero;
        child.localRotation = Quaternion.identity;
        child.localScale = Vector3.one;
        child.gameObject.layer = parent.gameObject.layer;

        BoxCollider collider = child.GetComponent<BoxCollider>();

        if (collider == null)
        {
            collider = Undo.AddComponent<BoxCollider>(child.gameObject);
        }

        Undo.RecordObject(
            collider,
            "Configure orbit console physical collider"
        );
        collider.isTrigger = false;
        collider.center = center;
        collider.size = size;
        return collider;
    }

    private static bool TestCharacterControllerSweep(
        CharacterController controller,
        Collider targetCollider
    )
    {
        Transform player = controller.transform;
        Vector3 originalPosition = player.position;
        Quaternion originalRotation = player.rotation;
        bool originalEnabled = controller.enabled;
        Bounds bounds = targetCollider.bounds;

        try
        {
            controller.enabled = false;
            player.position = new Vector3(
                bounds.min.x - controller.radius - 0.55f,
                originalPosition.y,
                bounds.center.z
            );
            player.rotation = Quaternion.LookRotation(Vector3.right);
            controller.enabled = true;
            Physics.SyncTransforms();

            CollisionFlags flags = controller.Move(Vector3.right * 2.5f);
            float expectedStopX = bounds.min.x - controller.radius;
            bool stoppedBeforeConsole =
                player.position.x <= expectedStopX + 0.12f;
            bool reportedSideCollision =
                (flags & CollisionFlags.Sides) != 0;

            return stoppedBeforeConsole || reportedSideCollision;
        }
        finally
        {
            controller.enabled = false;
            player.position = originalPosition;
            player.rotation = originalRotation;
            controller.enabled = originalEnabled;
            Physics.SyncTransforms();
        }
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
