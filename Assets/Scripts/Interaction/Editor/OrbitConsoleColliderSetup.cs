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
    private const string ConsoleRendererName = "Object_22";
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

        Renderer consoleRenderer = FindConsoleRenderer(console.transform);

        if (consoleRenderer == null)
        {
            Debug.LogError(
                "轨道控制台：没有找到对应的 GLB Renderer " +
                ConsoleRendererName + "。"
            );
            return;
        }

        Undo.RecordObject(
            console.transform,
            "Align orbit console interaction anchor"
        );
        console.transform.position = consoleRenderer.bounds.center;

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
        interactionCollider.center = new Vector3(0f, 0.05f, 0f);
        interactionCollider.size = new Vector3(0.58f, 1.42f, 0.78f);

        BoxCollider baseCollider = ConfigurePhysicalCollider(
            console.transform,
            BaseColliderName,
            new Vector3(0f, -0.33f, 0f),
            new Vector3(0.44f, 0.58f, 0.5f)
        );
        BoxCollider screenCollider = ConfigurePhysicalCollider(
            console.transform,
            ScreenColliderName,
            new Vector3(0f, 0.25f, 0f),
            new Vector3(0.48f, 0.65f, 0.64f)
        );

        EditorUtility.SetDirty(interactionCollider);
        EditorUtility.SetDirty(baseCollider);
        EditorUtility.SetDirty(screenCollider);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Selection.activeGameObject = console;
        Debug.Log(
            "轨道控制台：已对齐 Object_22，并更新交互区与实体碰撞。",
            console
        );
    }

    [MenuItem(
        "Tools/Earth Reshaping/Interaction/Validate Orbit Console (Play Mode)",
        false,
        21
    )]
    private static void ValidateInPlayMode()
    {
        GameObject console = FindSceneObject(ConsoleName);
        PlayerInteractor interactor =
            Object.FindObjectOfType<PlayerInteractor>();
        CharacterController characterController =
            Object.FindObjectOfType<CharacterController>();
        Camera playerCamera =
            interactor == null
                ? null
                : interactor.GetComponentInChildren<Camera>();

        if (
            console == null ||
            interactor == null ||
            characterController == null ||
            playerCamera == null
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
        Renderer consoleRenderer = FindConsoleRenderer(console.transform);
        BoxCollider[] physicalColliders = console
            .GetComponentsInChildren<BoxCollider>(true)
            .Where(collider => !collider.isTrigger)
            .ToArray();

        bool interactionReady =
            interactionCollider != null &&
            interactionCollider.isTrigger &&
            interactionCollider.size.y >= 1.4f &&
            interactionCollider.size.z >= 0.75f;
        bool anchorAligned =
            consoleRenderer != null &&
            Vector3.Distance(
                console.transform.position,
                consoleRenderer.bounds.center
            ) <= 0.02f;
        bool physicalReady = physicalColliders.Length >= 2;
        bool sweepBlocked = physicalReady &&
            TestCharacterControllerSweep(
                characterController,
                physicalColliders[0]
            );
        Vector3 screenTarget =
            consoleRenderer == null
                ? Vector3.zero
                : GetScreenTarget(consoleRenderer);
        bool screenRayHits =
            consoleRenderer != null &&
            RayHitsConsole(playerCamera, screenTarget);
        bool rightSideRayMisses =
            consoleRenderer != null &&
            !RayHitsConsole(
                playerCamera,
                screenTarget + playerCamera.transform.right * 0.9f
            );

        Debug.Log(
            "轨道控制台验证：" +
            $"interactionReady={interactionReady}, " +
            $"anchorAligned={anchorAligned}, " +
            $"physicalColliders={physicalColliders.Length}, " +
            $"characterSweepBlocked={sweepBlocked}, " +
            $"screenRayHits={screenRayHits}, " +
            $"rightSideRayMisses={rightSideRayMisses}, " +
            $"currentPrompt={interactor.CurrentPrompt}",
            console
        );
    }

    [MenuItem(
        "Tools/Earth Reshaping/Interaction/Focus Orbit Console (Play Mode)",
        false,
        20
    )]
    private static void FocusOrbitConsole()
    {
        FocusOrbitConsole(0f);
    }

    [MenuItem(
        "Tools/Earth Reshaping/Interaction/Focus Right of Orbit Console (Play Mode)",
        false,
        22
    )]
    private static void FocusRightOfOrbitConsole()
    {
        FocusOrbitConsole(0.9f);
    }

    private static void FocusOrbitConsole(float horizontalScreenOffset)
    {
        GameObject console = FindSceneObject(ConsoleName);
        PlayerInteractor interactor =
            Object.FindObjectOfType<PlayerInteractor>();
        CharacterController controller =
            Object.FindObjectOfType<CharacterController>();

        if (console == null || interactor == null || controller == null)
        {
            Debug.LogError("轨道控制台取景：缺少 Console 或 Player。");
            return;
        }

        Renderer consoleRenderer = FindConsoleRenderer(console.transform);
        Camera playerCamera = interactor.GetComponentInChildren<Camera>();
        PlayerMovement movement =
            controller.GetComponent<PlayerMovement>();

        if (consoleRenderer == null || playerCamera == null)
        {
            Debug.LogError("轨道控制台取景：缺少 Renderer 或 Camera。");
            return;
        }

        Vector3 centerTarget = GetScreenTarget(consoleRenderer);
        Vector3 playerPosition = new Vector3(
            centerTarget.x - 1.9f,
            1.15f,
            centerTarget.z + 0.35f
        );
        Vector3 centerDirection = centerTarget - playerPosition;
        centerDirection.y = 0f;
        Vector3 screenRight =
            Quaternion.LookRotation(centerDirection.normalized) *
            Vector3.right;
        Vector3 target =
            centerTarget + screenRight * horizontalScreenOffset;

        if (movement != null)
        {
            movement.enabled = false;
        }

        controller.enabled = false;
        controller.transform.position = playerPosition;
        Vector3 horizontalDirection = target - playerPosition;
        horizontalDirection.y = 0f;
        controller.transform.rotation = Quaternion.LookRotation(
            horizontalDirection.normalized,
            Vector3.up
        );
        controller.enabled = true;
        Physics.SyncTransforms();

        Vector3 lookDirection =
            (target - playerCamera.transform.position).normalized;
        float pitch = -Mathf.Asin(lookDirection.y) * Mathf.Rad2Deg;
        playerCamera.transform.localRotation = Quaternion.Euler(
            pitch,
            0f,
            0f
        );

        Debug.Log("轨道控制台取景：准星已对准真实屏幕。", console);
    }

    [MenuItem(
        "Tools/Earth Reshaping/Interaction/Validate Orbit Console (Play Mode)",
        true
    )]
    private static bool ValidateMenu() => Application.isPlaying;

    [MenuItem(
        "Tools/Earth Reshaping/Interaction/Focus Orbit Console (Play Mode)",
        true
    )]
    private static bool ValidateFocusMenu() => Application.isPlaying;

    [MenuItem(
        "Tools/Earth Reshaping/Interaction/Focus Right of Orbit Console (Play Mode)",
        true
    )]
    private static bool ValidateFocusRightMenu() => Application.isPlaying;

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

    private static Renderer FindConsoleRenderer(Transform anchor)
    {
        return Object.FindObjectsOfType<Renderer>(true)
            .Where(renderer =>
                renderer.enabled &&
                renderer.name == ConsoleRendererName
            )
            .OrderBy(renderer =>
                Vector3.Distance(
                    renderer.bounds.center,
                    anchor.position
                )
            )
            .FirstOrDefault();
    }

    private static Vector3 GetScreenTarget(Renderer consoleRenderer)
    {
        return consoleRenderer.bounds.center +
            Vector3.up * consoleRenderer.bounds.extents.y * 0.45f;
    }

    private static bool RayHitsConsole(Camera camera, Vector3 target)
    {
        Vector3 direction = target - camera.transform.position;

        if (
            !Physics.Raycast(
                camera.transform.position,
                direction.normalized,
                out RaycastHit hit,
                2.8f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Collide
            )
        )
        {
            return false;
        }

        return hit.collider.GetComponentInParent<IInteractable>() != null;
    }
}
