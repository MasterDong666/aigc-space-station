using System;
using UnityEngine;

[Serializable]
public struct EarthRestorationVisual
{
    public string label;
    public Color tint;
    [Range(0f, 1.25f)] public float saturation;
    [Range(0f, 2f)] public float brightness;
    public Color contaminationColor;
    [Range(0f, 1f)] public float contamination;
    [ColorUsage(false, true)] public Color atmosphereColor;
    [Range(0f, 2f)] public float atmosphereStrength;
    [Range(1f, 8f)] public float fresnelPower;

    public EarthRestorationVisual(
        string label,
        Color tint,
        float saturation,
        float brightness,
        Color contaminationColor,
        float contamination,
        Color atmosphereColor,
        float atmosphereStrength,
        float fresnelPower
    )
    {
        this.label = label;
        this.tint = tint;
        this.saturation = saturation;
        this.brightness = brightness;
        this.contaminationColor = contaminationColor;
        this.contamination = contamination;
        this.atmosphereColor = atmosphereColor;
        this.atmosphereStrength = atmosphereStrength;
        this.fresnelPower = fresnelPower;
    }
}

/// <summary>
/// Drives the MVP Earth's four visual restoration stages without modifying the
/// original FBX, texture, or shared Earth material. Runtime values are applied
/// through a MaterialPropertyBlock and follow the shared 0..50 progression.
/// </summary>
[DisallowMultipleComponent]
public class EarthRestorationController : MonoBehaviour
{
    private static readonly int TintId = Shader.PropertyToID("_Tint");
    private static readonly int SaturationId =
        Shader.PropertyToID("_Saturation");
    private static readonly int BrightnessId =
        Shader.PropertyToID("_Brightness");
    private static readonly int ContaminationColorId =
        Shader.PropertyToID("_ContaminationColor");
    private static readonly int ContaminationId =
        Shader.PropertyToID("_Contamination");
    private static readonly int AtmosphereColorId =
        Shader.PropertyToID("_AtmosphereColor");
    private static readonly int AtmosphereStrengthId =
        Shader.PropertyToID("_AtmosphereStrength");
    private static readonly int FresnelPowerId =
        Shader.PropertyToID("_FresnelPower");

    [Header("场景引用")]
    [SerializeField] private Renderer earthRenderer;
    [SerializeField] private Transform earthTransform;
    [SerializeField] private MVPFlowController flowController;

    [Header("视觉过渡")]
    [SerializeField, Min(0f)] private float transitionDuration = 2.4f;
    [SerializeField] private AnimationCurve transitionCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float rotationSpeed = 0.18f;

    [Header("修复阶段 0～3")]
    [SerializeField] private EarthRestorationVisual[] stages =
    {
        new(
            "0 受损地球",
            new Color(0.82f, 0.78f, 0.70f, 1f),
            0.10f,
            0.66f,
            new Color(0.52f, 0.34f, 0.17f, 1f),
            0.82f,
            new Color(0.30f, 0.11f, 0.025f, 1f),
            0.08f,
            4.4f
        ),
        new(
            "1 轨道恢复",
            new Color(0.90f, 0.91f, 0.91f, 1f),
            0.38f,
            0.80f,
            new Color(0.62f, 0.46f, 0.27f, 1f),
            0.54f,
            new Color(0.16f, 0.48f, 1.35f, 1f),
            0.24f,
            3.9f
        ),
        new(
            "2 生态恢复",
            new Color(0.96f, 0.98f, 1.00f, 1f),
            0.72f,
            0.93f,
            new Color(0.69f, 0.56f, 0.36f, 1f),
            0.23f,
            new Color(0.08f, 0.66f, 1.75f, 1f),
            0.43f,
            3.5f
        ),
        new(
            "3 完全恢复",
            Color.white,
            1.00f,
            1.04f,
            Color.white,
            0.00f,
            new Color(0.10f, 0.80f, 2.10f, 1f),
            0.62f,
            3.1f
        )
    };

    private MaterialPropertyBlock propertyBlock;
    private EarthRestorationVisual startVisual;
    private EarthRestorationVisual currentVisual;
    private EarthRestorationVisual targetVisual;
    private float transitionElapsed;
    private bool isTransitioning;

    public int CurrentStage { get; private set; }
    public int TargetStage { get; private set; }
    public bool IsTransitioning => isTransitioning;

    private void Awake()
    {
        ResolveReferences();
        propertyBlock = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        ResolveReferences();

        MVPGameSession.ProgressChanged += HandleProgressChanged;

        int initialStage = MVPGameSession.GetRestorationStage();
        SetStageImmediate(initialStage);
    }

    private void OnDisable()
    {
        MVPGameSession.ProgressChanged -= HandleProgressChanged;
    }

    private void Update()
    {
        if (earthTransform != null && Mathf.Abs(rotationSpeed) > 0.001f)
        {
            earthTransform.Rotate(
                Vector3.up,
                rotationSpeed * Time.deltaTime,
                Space.Self
            );
        }

        if (!isTransitioning)
        {
            return;
        }

        transitionElapsed += Time.deltaTime;
        float normalizedTime = transitionDuration <= 0f
            ? 1f
            : Mathf.Clamp01(transitionElapsed / transitionDuration);
        float blend = transitionCurve != null
            ? transitionCurve.Evaluate(normalizedTime)
            : normalizedTime;

        currentVisual = LerpVisual(startVisual, targetVisual, blend);
        ApplyVisual(currentVisual);

        if (normalizedTime >= 1f)
        {
            currentVisual = targetVisual;
            CurrentStage = TargetStage;
            isTransitioning = false;
        }
    }

    public void Configure(
        Renderer renderer,
        Transform targetTransform,
        MVPFlowController controller
    )
    {
        earthRenderer = renderer;
        earthTransform = targetTransform;
        flowController = controller;
    }

    public void SetStage(int stage)
    {
        if (!TryGetStage(stage, out EarthRestorationVisual visual))
        {
            return;
        }

        startVisual = currentVisual;
        targetVisual = visual;
        TargetStage = Mathf.Clamp(stage, 0, stages.Length - 1);
        transitionElapsed = 0f;
        isTransitioning = transitionDuration > 0f;

        if (!isTransitioning)
        {
            SetStageImmediate(stage);
        }
    }

    public void SetStageImmediate(int stage)
    {
        if (!TryGetStage(stage, out EarthRestorationVisual visual))
        {
            return;
        }

        int clampedStage = Mathf.Clamp(stage, 0, stages.Length - 1);
        startVisual = visual;
        currentVisual = visual;
        targetVisual = visual;
        CurrentStage = clampedStage;
        TargetStage = clampedStage;
        transitionElapsed = 0f;
        isTransitioning = false;
        ApplyVisual(visual);
    }

    private void HandleProgressChanged(int _)
    {
        SetStage(MVPGameSession.GetRestorationStage());
    }

    private void ResolveReferences()
    {
        if (earthRenderer == null)
        {
            earthRenderer = GetComponentInChildren<Renderer>(true);
        }

        if (earthTransform == null && earthRenderer != null)
        {
            earthTransform = earthRenderer.transform;
        }

        if (flowController == null)
        {
            flowController = FindObjectOfType<MVPFlowController>(true);
        }
    }

    private bool TryGetStage(
        int stage,
        out EarthRestorationVisual visual
    )
    {
        if (stages == null || stages.Length == 0)
        {
            Debug.LogWarning(
                "Earth Restoration：未配置视觉阶段。",
                this
            );
            visual = default;
            return false;
        }

        int clampedStage = Mathf.Clamp(stage, 0, stages.Length - 1);
        visual = stages[clampedStage];
        return true;
    }

    private void ApplyVisual(EarthRestorationVisual visual)
    {
        if (earthRenderer == null)
        {
            return;
        }

        propertyBlock ??= new MaterialPropertyBlock();
        earthRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(TintId, visual.tint);
        propertyBlock.SetFloat(SaturationId, visual.saturation);
        propertyBlock.SetFloat(BrightnessId, visual.brightness);
        propertyBlock.SetColor(
            ContaminationColorId,
            visual.contaminationColor
        );
        propertyBlock.SetFloat(
            ContaminationId,
            visual.contamination
        );
        propertyBlock.SetColor(
            AtmosphereColorId,
            visual.atmosphereColor
        );
        propertyBlock.SetFloat(
            AtmosphereStrengthId,
            visual.atmosphereStrength
        );
        propertyBlock.SetFloat(FresnelPowerId, visual.fresnelPower);
        earthRenderer.SetPropertyBlock(propertyBlock);
    }

    private static EarthRestorationVisual LerpVisual(
        EarthRestorationVisual from,
        EarthRestorationVisual to,
        float blend
    )
    {
        return new EarthRestorationVisual(
            to.label,
            Color.Lerp(from.tint, to.tint, blend),
            Mathf.Lerp(from.saturation, to.saturation, blend),
            Mathf.Lerp(from.brightness, to.brightness, blend),
            Color.Lerp(
                from.contaminationColor,
                to.contaminationColor,
                blend
            ),
            Mathf.Lerp(from.contamination, to.contamination, blend),
            Color.Lerp(
                from.atmosphereColor,
                to.atmosphereColor,
                blend
            ),
            Mathf.Lerp(
                from.atmosphereStrength,
                to.atmosphereStrength,
                blend
            ),
            Mathf.Lerp(from.fresnelPower, to.fresnelPower, blend)
        );
    }
}
