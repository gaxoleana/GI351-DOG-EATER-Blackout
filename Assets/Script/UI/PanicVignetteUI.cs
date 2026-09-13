using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PanicVignetteUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerPanic playerPanic;
    [SerializeField] private Volume globalVolume;

    [Header("Vignette")]
    [SerializeField, Range(0f, 1f)] private float baseVignetteIntensity = 0.2f;
    [SerializeField, Range(0f, 1f)] private float maximumVignetteIntensity = 0.65f;
    [SerializeField, Range(0f, 1f)] private float baseVignetteSmoothness = 0.2f;
    [SerializeField, Range(0f, 1f)] private float maximumVignetteSmoothness = 0.55f;
    [SerializeField] private float responseSpeed = 8f;

    private Vignette vignette;
    private float visualPanic;

    private void Awake()
    {
        playerPanic ??= FindAnyObjectByType<PlayerPanic>();

        if (globalVolume == null)
            globalVolume = FindAnyObjectByType<Volume>();

        if (globalVolume != null && globalVolume.profile != null)
            globalVolume.profile.TryGet(out vignette);
    }

    private void Update()
    {
        float targetPanic = playerPanic != null ? playerPanic.CurrentPanic : 0f;
        visualPanic = Mathf.MoveTowards(visualPanic, targetPanic, responseSpeed * Time.deltaTime);

        if (vignette != null)
        {
            vignette.intensity.overrideState = true;
            vignette.intensity.value = Mathf.Lerp(baseVignetteIntensity, maximumVignetteIntensity, visualPanic);
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = Mathf.Lerp(baseVignetteSmoothness, maximumVignetteSmoothness, visualPanic);
        }
    }

    private void OnDisable()
    {
        visualPanic = 0f;

        if (vignette != null)
        {
            vignette.intensity.value = baseVignetteIntensity;
            vignette.smoothness.value = baseVignetteSmoothness;
        }
    }
}
