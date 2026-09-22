using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PanicVignetteUI : MonoBehaviour
{
    private static readonly System.Collections.Generic.List<PanicVignetteUI> activeInstances = new();

    [Header("References")]
    [SerializeField] private PlayerPanic playerPanic;
    [SerializeField] private Volume globalVolume;

    [Header("Stability Blackout")]
    [SerializeField] private bool enableStabilityBlackout;
    [SerializeField] private WorldStability worldStability;
    [SerializeField, Range(0f, 100f)] private float warningThreshold = 70f;
    [SerializeField, Range(0f, 100f)] private float criticalThreshold = 30f;
    [SerializeField, Range(0f, 1f)] private float warningVignetteIntensity = 0.48f;
    [SerializeField, Range(0f, 1f)] private float criticalVignetteIntensity = 0.72f;
    [SerializeField, Range(0f, 1f)] private float depletedVignetteIntensity = 1f;
    [SerializeField, Range(0f, 1f)] private float blackoutSmoothness = 0.45f;

    [Header("Vignette")]
    [SerializeField, Range(0f, 1f)] private float baseVignetteIntensity = 0.2f;
    [SerializeField, Range(0f, 1f)] private float maximumVignetteIntensity = 0.65f;
    [SerializeField, Range(0f, 1f)] private float baseVignetteSmoothness = 0.2f;
    [SerializeField, Range(0f, 1f)] private float maximumVignetteSmoothness = 0.55f;
    [SerializeField] private float responseSpeed = 8f;

    private Vignette vignette;
    private float visualPanic;

    public static bool HasInstanceInScene(UnityEngine.SceneManagement.Scene scene)
    {
        foreach (PanicVignetteUI instance in activeInstances)
        {
            if (instance != null && instance.gameObject.scene == scene)
                return true;
        }

        return false;
    }

    private void OnEnable()
    {
        activeInstances.Add(this);
    }

    private void Awake()
    {
        playerPanic ??= FindAnyObjectByType<PlayerPanic>();

        if (globalVolume == null)
            globalVolume = FindAnyObjectByType<Volume>();

        if (globalVolume != null && globalVolume.profile != null)
            globalVolume.profile.TryGet(out vignette);

        if (enableStabilityBlackout)
            worldStability ??= FindAnyObjectByType<WorldStability>();
    }

    private void Update()
    {
        if (playerPanic == null)
            playerPanic = FindAnyObjectByType<PlayerPanic>();

        if (enableStabilityBlackout && worldStability == null)
            worldStability = FindAnyObjectByType<WorldStability>();

        float targetPanic = playerPanic != null ? playerPanic.CurrentPanic : 0f;
        visualPanic = Mathf.MoveTowards(visualPanic, targetPanic, responseSpeed * Time.deltaTime);

        float panicIntensity = Mathf.Lerp(baseVignetteIntensity, maximumVignetteIntensity, visualPanic);
        float panicSmoothness = Mathf.Lerp(baseVignetteSmoothness, maximumVignetteSmoothness, visualPanic);
        float blackoutIntensity = GetBlackoutIntensity();
        float blackoutSmoothness = this.blackoutSmoothness;
        if (BlackoutZoneController.TryGetForScene(gameObject.scene, out BlackoutZoneController blackoutController))
        {
            blackoutIntensity = blackoutController.CurrentVignetteIntensity;
            blackoutSmoothness = blackoutController.CurrentVignetteSmoothness;
        }

        if (vignette != null)
        {
            vignette.intensity.overrideState = true;
            vignette.intensity.value = Mathf.Max(panicIntensity, blackoutIntensity);
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = Mathf.Max(panicSmoothness, blackoutIntensity > 0f ? blackoutSmoothness : 0f);
        }
    }

    private void OnDisable()
    {
        activeInstances.Remove(this);
        visualPanic = 0f;

        if (vignette != null)
        {
            vignette.intensity.value = baseVignetteIntensity;
            vignette.smoothness.value = baseVignetteSmoothness;
        }
    }

    private float GetBlackoutIntensity()
    {
        if (!enableStabilityBlackout || worldStability == null || worldStability.MaxStability <= 0f)
            return 0f;

        float stabilityPercent = Mathf.Clamp01(worldStability.CurrentStability / worldStability.MaxStability) * 100f;

        if (stabilityPercent <= 0f)
            return depletedVignetteIntensity;

        if (stabilityPercent <= criticalThreshold)
            return criticalVignetteIntensity;

        if (stabilityPercent <= warningThreshold)
            return warningVignetteIntensity;

        return 0f;
    }
}
