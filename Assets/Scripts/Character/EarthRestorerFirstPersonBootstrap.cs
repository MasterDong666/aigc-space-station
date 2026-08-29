using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Installs the Earth Restorer visual on every PlayerMovement without changing
/// the existing scene or CharacterController setup.
/// </summary>
public sealed class EarthRestorerFirstPersonBootstrap : MonoBehaviour
{
    private const string ModelResourcePath =
        "Characters/EarthRestorer/earth_restorer_character";

    private static bool sceneHookInstalled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallForLoadedScene()
    {
        InstallSceneHook();
        InstallOnPlayers();
    }

    private static void InstallSceneHook()
    {
        if (sceneHookInstalled)
        {
            return;
        }

        sceneHookInstalled = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InstallOnPlayers();
    }

    private static void InstallOnPlayers()
    {
        PlayerMovement[] players =
            FindObjectsOfType<PlayerMovement>(true);

        foreach (PlayerMovement player in players)
        {
            if (player.GetComponent<EarthRestorerFirstPersonBootstrap>() == null)
            {
                player.gameObject.AddComponent<EarthRestorerFirstPersonBootstrap>();
            }
        }
    }

    private void Start()
    {
        if (transform.Find("EarthRestorer_Visual") != null)
        {
            return;
        }

        GameObject modelPrefab =
            Resources.Load<GameObject>(ModelResourcePath);

        if (modelPrefab == null)
        {
            Debug.LogError(
                "EarthRestorer：没有找到角色模型 Resources/" +
                ModelResourcePath,
                this
            );
            return;
        }

        GameObject visual = Instantiate(modelPrefab, transform);
        visual.name = "EarthRestorer_Visual";
        visual.transform.localPosition = new Vector3(0f, -1.0f, 0.07f);
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        Animator animator = visual.GetComponent<Animator>();

        if (animator == null)
        {
            animator = visual.AddComponent<Animator>();
        }

        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.updateMode = AnimatorUpdateMode.Normal;

        ConvertImportedMaterialsToUrp(visual);
        ConfigureFirstPersonRenderers(visual);

        EarthRestorerFirstPersonDriver driver =
            visual.AddComponent<EarthRestorerFirstPersonDriver>();
        driver.Initialize(
            transform,
            GetComponent<CharacterController>(),
            GetComponent<PlayerMovement>(),
            animator,
            ModelResourcePath
        );

        Camera playerCamera = GetComponentInChildren<Camera>(true);

        if (playerCamera != null)
        {
            playerCamera.nearClipPlane = Mathf.Min(
                playerCamera.nearClipPlane,
                0.05f
            );

            EarthRestorerViewBob viewBob =
                playerCamera.GetComponent<EarthRestorerViewBob>();

            if (viewBob == null)
            {
                viewBob = playerCamera.gameObject.AddComponent<EarthRestorerViewBob>();
            }

            viewBob.Initialize(
                GetComponent<CharacterController>(),
                GetComponent<PlayerMovement>()
            );
        }
    }

    private static void ConfigureFirstPersonRenderers(GameObject visual)
    {
        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            string objectName = renderer.gameObject.name;

            if (
                objectName.Equals("FP_HeadMesh", StringComparison.Ordinal) ||
                objectName.Equals("FP_TorsoMesh", StringComparison.Ordinal) ||
                objectName.StartsWith("HeadVisual_", StringComparison.Ordinal) ||
                objectName.StartsWith("HairVisual_", StringComparison.Ordinal)
            )
            {
                renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
        }
    }

    private static void ConvertImportedMaterialsToUrp(GameObject visual)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");

        if (urpLit == null)
        {
            Debug.LogWarning("EarthRestorer：没有找到 URP/Lit Shader，保留导入材质。");
            return;
        }

        Dictionary<string, Material> converted =
            new Dictionary<string, Material>(StringComparer.Ordinal);

        foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
        {
            Material[] sourceMaterials = renderer.sharedMaterials;
            Material[] targetMaterials = new Material[sourceMaterials.Length];

            for (int index = 0; index < sourceMaterials.Length; index++)
            {
                Material source = sourceMaterials[index];
                string sourceName = source == null
                    ? "EarthRestorer_Default"
                    : source.name.Replace(" (Instance)", string.Empty);

                if (!converted.TryGetValue(sourceName, out Material target))
                {
                    target = BuildUrpMaterial(urpLit, sourceName, source);
                    converted[sourceName] = target;
                }

                targetMaterials[index] = target;
            }

            renderer.materials = targetMaterials;
        }
    }

    private static Material BuildUrpMaterial(
        Shader shader,
        string materialName,
        Material source
    )
    {
        Material material = new Material(shader)
        {
            name = materialName + "_URP_Runtime"
        };

        Color color = source != null && source.HasProperty("_Color")
            ? source.color
            : new Color(0.35f, 0.40f, 0.50f, 1f);
        float metallic = 0.05f;
        float smoothness = 0.45f;

        if (
            materialName.Contains("SilverGrey") ||
            materialName.Contains("PearlSilver")
        )
        {
            color = new Color(0.48f, 0.53f, 0.62f, 1f);
            metallic = 0.12f;
            smoothness = 0.52f;
        }
        else if (
            materialName.Contains("DarkPanels") ||
            materialName.Contains("SidePanel")
        )
        {
            color = new Color(0.20f, 0.25f, 0.34f, 1f);
            metallic = 0.08f;
        }
        else if (
            materialName.Contains("GoldTrim") ||
            materialName.Contains("WarmGold")
        )
        {
            color = new Color(0.67f, 0.48f, 0.14f, 1f);
            metallic = 0.70f;
            smoothness = 0.70f;
        }
        else if (materialName.Contains("DeepNavy"))
        {
            color = new Color(0.025f, 0.040f, 0.075f, 1f);
            metallic = 0.18f;
            smoothness = 0.62f;
        }
        else if (materialName.Contains("Skin"))
        {
            color = new Color(0.72f, 0.39f, 0.24f, 1f);
            smoothness = 0.38f;
        }
        else if (materialName.Contains("Hair"))
        {
            color = new Color(0.025f, 0.017f, 0.015f, 1f);
            smoothness = 0.55f;
        }
        else if (materialName.Contains("Boot"))
        {
            color = new Color(0.025f, 0.035f, 0.060f, 1f);
            metallic = 0.15f;
            smoothness = 0.62f;
        }
        else if (
            materialName.Contains("EyeWhite") ||
            materialName.Contains("Eye_White")
        )
        {
            color = new Color(0.88f, 0.90f, 0.86f, 1f);
            smoothness = 0.72f;
        }
        else if (
            materialName.Contains("Eyes") ||
            materialName.Contains("Eye_Brown") ||
            materialName.Contains("Eye_Pupil")
        )
        {
            color = new Color(0.012f, 0.008f, 0.006f, 1f);
            smoothness = 0.82f;
        }
        else if (materialName.Contains("BadgeGlow"))
        {
            color = new Color(0.03f, 0.32f, 0.52f, 1f);
            metallic = 0.35f;
            smoothness = 0.75f;
            material.EnableKeyword("_EMISSION");
            material.SetColor(
                "_EmissionColor",
                new Color(0.04f, 0.65f, 1.0f, 1f) * 1.5f
            );
        }

        material.SetColor("_BaseColor", color);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smoothness);
        return material;
    }
}


[DefaultExecutionOrder(100)]
internal sealed class EarthRestorerFirstPersonDriver : MonoBehaviour
{
    private sealed class ClipState
    {
        public string Key;
        public AnimationClip Clip;
        public AnimationClipPlayable Playable;
        public int MixerIndex;
        public bool Loop;
    }

    private Transform playerRoot;
    private CharacterController controller;
    private PlayerMovement movement;
    private Animator animator;
    private PlayableGraph graph;
    private AnimationMixerPlayable mixer;
    private readonly List<ClipState> states = new List<ClipState>();
    private ClipState currentState;
    private bool initialized;

    public void Initialize(
        Transform root,
        CharacterController characterController,
        PlayerMovement playerMovement,
        Animator targetAnimator,
        string resourcePath
    )
    {
        playerRoot = root;
        controller = characterController;
        movement = playerMovement;
        animator = targetAnimator;

        AnimationClip[] importedClips =
            Resources.LoadAll<AnimationClip>(resourcePath);

        foreach (AnimationClip clip in importedClips)
        {
            string key = NormalizeClipKey(clip.name);

            if (string.IsNullOrEmpty(key) || HasState(key))
            {
                continue;
            }

            states.Add(new ClipState
            {
                Key = key,
                Clip = clip,
                Loop = key != "jump"
            });
        }

        if (states.Count == 0)
        {
            Debug.LogError(
                "EarthRestorer：FBX 中没有可播放的动作片段。",
                this
            );
            return;
        }

        graph = PlayableGraph.Create("EarthRestorer_FirstPersonGraph");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        mixer = AnimationMixerPlayable.Create(graph, states.Count);

        for (int index = 0; index < states.Count; index++)
        {
            ClipState state = states[index];
            state.MixerIndex = index;
            state.Playable = AnimationClipPlayable.Create(graph, state.Clip);
            state.Playable.SetApplyFootIK(true);
            graph.Connect(state.Playable, 0, mixer, index);
            mixer.SetInputWeight(index, 0f);
        }

        AnimationPlayableOutput output = AnimationPlayableOutput.Create(
            graph,
            "EarthRestorer_Animation",
            animator
        );
        output.SetSourcePlayable(mixer);
        graph.Play();

        currentState = FindState("idle") ?? states[0];
        mixer.SetInputWeight(currentState.MixerIndex, 1f);
        currentState.Playable.SetTime(0);
        initialized = true;
    }

    private void Update()
    {
        if (!initialized || controller == null || states.Count == 0)
        {
            return;
        }

        Vector3 velocity = controller.velocity;
        Vector3 planarVelocity = new Vector3(velocity.x, 0f, velocity.z);
        float speed = planarVelocity.magnitude;
        Vector3 localVelocity = playerRoot.InverseTransformDirection(planarVelocity);
        string targetKey = SelectTargetKey(speed, localVelocity);
        ClipState target = FindState(targetKey) ?? FindState("walkforward");

        if (target == null)
        {
            target = states[0];
        }

        if (!ReferenceEquals(target, currentState))
        {
            currentState = target;
            currentState.Playable.SetTime(0);
        }

        float blendStep = Time.deltaTime * 8f;

        foreach (ClipState state in states)
        {
            float desired = ReferenceEquals(state, currentState) ? 1f : 0f;
            float weight = mixer.GetInputWeight(state.MixerIndex);
            mixer.SetInputWeight(
                state.MixerIndex,
                Mathf.MoveTowards(weight, desired, blendStep)
            );

            if (state.Loop && state.Clip.length > 0.001f)
            {
                double time = state.Playable.GetTime();

                if (time >= state.Clip.length)
                {
                    state.Playable.SetTime(time % state.Clip.length);
                }
            }
        }

        currentState.Playable.SetSpeed(GetPlaybackSpeed(currentState.Key, speed));
    }

    private string SelectTargetKey(float speed, Vector3 localVelocity)
    {
        bool grounded = movement != null
            ? movement.IsGrounded
            : controller.isGrounded;

        if (!grounded)
        {
            return "jump";
        }

        if (speed < 0.08f)
        {
            return "idle";
        }

        if (movement != null && movement.IsSprinting)
        {
            return "run";
        }

        if (Mathf.Abs(localVelocity.x) > Mathf.Abs(localVelocity.z) * 0.65f)
        {
            return localVelocity.x < 0f
                ? "strafeleft"
                : "straferight";
        }

        return localVelocity.z < 0f
            ? "walkbackward"
            : "walkforward";
    }

    private static float GetPlaybackSpeed(string key, float movementSpeed)
    {
        if (key == "idle")
        {
            return 1f;
        }

        if (key == "run")
        {
            return Mathf.Clamp(movementSpeed / 4.5f, 0.78f, 1.35f);
        }

        if (key == "jump")
        {
            return 1f;
        }

        return Mathf.Clamp(movementSpeed / 2.8f, 0.72f, 1.35f);
    }

    private bool HasState(string key)
    {
        return FindState(key) != null;
    }

    private ClipState FindState(string key)
    {
        return states.Find(state => state.Key == key);
    }

    private static string NormalizeClipKey(string clipName)
    {
        string lower = clipName.ToLowerInvariant();

        if (lower.Contains("walkforward")) return "walkforward";
        if (lower.Contains("walkbackward")) return "walkbackward";
        if (lower.Contains("strafeleft")) return "strafeleft";
        if (lower.Contains("straferight")) return "straferight";
        if (lower.Contains("run")) return "run";
        if (lower.Contains("jump")) return "jump";
        if (lower.Contains("idle")) return "idle";
        return string.Empty;
    }

    private void OnDestroy()
    {
        if (graph.IsValid())
        {
            graph.Destroy();
        }
    }
}


[DefaultExecutionOrder(200)]
internal sealed class EarthRestorerViewBob : MonoBehaviour
{
    private CharacterController controller;
    private PlayerMovement movement;
    private Vector3 baseLocalPosition;
    private float phase;
    private bool initialized;

    public void Initialize(
        CharacterController characterController,
        PlayerMovement playerMovement
    )
    {
        controller = characterController;
        movement = playerMovement;
        baseLocalPosition = transform.localPosition;
        initialized = true;
    }

    private void LateUpdate()
    {
        if (!initialized || controller == null)
        {
            return;
        }

        Vector3 planarVelocity = controller.velocity;
        planarVelocity.y = 0f;
        float speed = planarVelocity.magnitude;
        bool grounded = movement != null
            ? movement.IsGrounded
            : controller.isGrounded;

        Vector3 targetOffset = Vector3.zero;

        if (grounded && speed > 0.08f)
        {
            bool sprinting = movement != null && movement.IsSprinting;
            float frequency = sprinting ? 11.5f : 7.8f;
            float horizontalAmplitude = sprinting ? 0.020f : 0.010f;
            float verticalAmplitude = sprinting ? 0.028f : 0.015f;
            phase += Time.deltaTime * frequency;

            targetOffset.x = Mathf.Cos(phase * 0.5f) * horizontalAmplitude;
            targetOffset.y = Mathf.Abs(Mathf.Sin(phase)) * verticalAmplitude;
        }

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            baseLocalPosition + targetOffset,
            1f - Mathf.Exp(-14f * Time.deltaTime)
        );
    }
}
