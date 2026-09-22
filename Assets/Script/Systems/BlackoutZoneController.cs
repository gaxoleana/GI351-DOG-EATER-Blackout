using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Scene-local stability rules for a single world. Add this beside that world's
/// WorldStability component; it never reads or modifies another world's value.
/// </summary>
public class BlackoutZoneController : MonoBehaviour
{
    private static readonly List<BlackoutZoneController> activeControllers = new();

    [Header("References")]
    [SerializeField] private WorldStability worldStability;
    [Tooltip("Used only when this scene has no PanicVignetteUI component.")]
    [SerializeField] private Volume globalVolume;

    [Header("Stability Zones")]
    [SerializeField, Range(0f, 100f)] private float blackoutStartPercent = 70f;
    [SerializeField, Range(0f, 100f)] private float criticalStartPercent = 30f;
    [SerializeField, Min(1f)] private float criticalEnemyDamageMultiplier = 2f;

    [Header("Blackout Vignette")]
    [SerializeField, Range(0f, 1f)] private float blackoutVignetteIntensity = 0.48f;
    [SerializeField, Range(0f, 1f)] private float criticalVignetteIntensity = 0.72f;
    [SerializeField, Range(0f, 1f)] private float vignetteSmoothness = 0.45f;

    [Header("Critical Unstabilizer Spawning (1-30%)")]
    [SerializeField] private GameObject unstabilizerPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField, Min(1)] private int maximumActiveUnstabilizers = 3;
    [SerializeField, Min(0f)] private float spawnPointRandomOffset = 2f;
    [SerializeField, Min(0f)] private float minimumSpawnDelay = 5f;
    [SerializeField, Min(0f)] private float maximumSpawnDelay = 12f;

    private Vignette vignette;
    private float originalVignetteIntensity;
    private float originalVignetteSmoothness;
    private bool hasOriginalVignetteValues;
    private float nextSpawnTime = float.PositiveInfinity;
    private bool wasCritical;

    public float StabilityPercent { get; private set; } = 100f;
    public bool IsBlackout { get; private set; }
    public bool IsCritical { get; private set; }
    public float CurrentVignetteIntensity { get; private set; }
    public float CurrentVignetteSmoothness => IsBlackout ? vignetteSmoothness : 0f;

    private void Awake()
    {
        worldStability ??= GetComponent<WorldStability>();

        if (globalVolume != null && globalVolume.profile != null && globalVolume.profile.TryGet(out vignette))
        {
            originalVignetteIntensity = vignette.intensity.value;
            originalVignetteSmoothness = vignette.smoothness.value;
            hasOriginalVignetteValues = true;
        }
    }

    private void OnEnable()
    {
        activeControllers.Add(this);
    }

    private void OnDisable()
    {
        activeControllers.Remove(this);
        RestoreDirectVignette();
    }

    private void Update()
    {
        UpdateZoneState();
        UpdateUnstabilizerSpawning();

        // PanicVignetteUI combines panic and blackout safely. If a scene has no
        // panic UI, this controller still drives the assigned Global Volume.
        if (!PanicVignetteUI.HasInstanceInScene(gameObject.scene))
            ApplyDirectVignette();
    }

    public float GetEnemyDamageMultiplier()
    {
        return IsCritical ? criticalEnemyDamageMultiplier : 1f;
    }

    public static float GetEnemyDamageMultiplier(Component damageSource)
    {
        if (damageSource == null)
            return 1f;

        foreach (BlackoutZoneController controller in activeControllers)
        {
            if (controller != null && controller.gameObject.scene == damageSource.gameObject.scene)
                return controller.GetEnemyDamageMultiplier();
        }

        return 1f;
    }

    public static bool TryGetForScene(UnityEngine.SceneManagement.Scene scene, out BlackoutZoneController controller)
    {
        foreach (BlackoutZoneController activeController in activeControllers)
        {
            if (activeController != null && activeController.gameObject.scene == scene)
            {
                controller = activeController;
                return true;
            }
        }

        controller = null;
        return false;
    }

    private void UpdateZoneState()
    {
        if (worldStability == null || worldStability.MaxStability <= 0f)
        {
            StabilityPercent = 100f;
            IsBlackout = false;
            IsCritical = false;
            CurrentVignetteIntensity = 0f;
            return;
        }

        StabilityPercent = Mathf.Clamp01(worldStability.CurrentStability / worldStability.MaxStability) * 100f;
        IsCritical = StabilityPercent > 0f && StabilityPercent <= criticalStartPercent;
        IsBlackout = StabilityPercent > 0f && StabilityPercent <= blackoutStartPercent;

        if (IsCritical)
        {
            float progress = Mathf.InverseLerp(criticalStartPercent, 0f, StabilityPercent);
            CurrentVignetteIntensity = Mathf.Lerp(blackoutVignetteIntensity, criticalVignetteIntensity, progress);
        }
        else if (IsBlackout)
        {
            float progress = Mathf.InverseLerp(blackoutStartPercent, criticalStartPercent, StabilityPercent);
            CurrentVignetteIntensity = Mathf.Lerp(0f, blackoutVignetteIntensity, progress);
        }
        else
        {
            CurrentVignetteIntensity = 0f;
        }
    }

    private void UpdateUnstabilizerSpawning()
    {
        if (!IsCritical)
        {
            wasCritical = false;
            nextSpawnTime = float.PositiveInfinity;
            return;
        }

        if (!wasCritical)
        {
            wasCritical = true;
            ScheduleNextSpawn();
        }

        if (Time.time < nextSpawnTime || CountActiveUnstabilizers() >= maximumActiveUnstabilizers)
            return;

        SpawnUnstabilizer();
        ScheduleNextSpawn();
    }

    private void SpawnUnstabilizer()
    {
        if (unstabilizerPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
            return;

        List<Transform> validSpawnPoints = new();
        foreach (Transform candidateSpawnPoint in spawnPoints)
        {
            if (candidateSpawnPoint != null)
                validSpawnPoints.Add(candidateSpawnPoint);
        }

        if (validSpawnPoints.Count == 0)
            return;

        Transform selectedSpawnPoint = validSpawnPoints[Random.Range(0, validSpawnPoints.Count)];
        Vector2 offset = Random.insideUnitCircle * spawnPointRandomOffset;
        Vector3 position = selectedSpawnPoint.position + new Vector3(offset.x, 0f, offset.y);
        Instantiate(unstabilizerPrefab, position, selectedSpawnPoint.rotation);
    }

    private int CountActiveUnstabilizers()
    {
        int count = 0;
        foreach (UnstabilizerEnemy enemy in UnstabilizerEnemy.Active)
        {
            if (enemy != null && enemy.gameObject.scene == gameObject.scene)
                count++;
        }

        return count;
    }

    private void ScheduleNextSpawn()
    {
        float minimum = Mathf.Min(minimumSpawnDelay, maximumSpawnDelay);
        float maximum = Mathf.Max(minimumSpawnDelay, maximumSpawnDelay);
        nextSpawnTime = Time.time + Random.Range(minimum, maximum);
    }

    private void ApplyDirectVignette()
    {
        if (vignette == null)
            return;

        vignette.intensity.overrideState = true;
        vignette.intensity.value = Mathf.Max(originalVignetteIntensity, CurrentVignetteIntensity);
        vignette.smoothness.overrideState = true;
        vignette.smoothness.value = Mathf.Max(originalVignetteSmoothness, CurrentVignetteSmoothness);
    }

    private void RestoreDirectVignette()
    {
        if (vignette == null || !hasOriginalVignetteValues)
            return;

        vignette.intensity.value = originalVignetteIntensity;
        vignette.smoothness.value = originalVignetteSmoothness;
    }
}
