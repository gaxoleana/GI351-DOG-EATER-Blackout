using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BlackoutZoneController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WorldStability worldStability;
    [SerializeField] private Volume globalVolume;
    [SerializeField] private GameObject unstabilizerPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Transform playerTransform;

    [Header("Blackout")]
    [SerializeField, Range(0f, 1f)] private float safeZoneVignette = 0.08f;
    [SerializeField, Range(0f, 1f)] private float warningZoneVignette = 0.48f;
    [SerializeField, Range(0f, 1f)] private float criticalZoneVignette = 0.72f;
    [SerializeField, Range(0f, 1f)] private float dimensionBreakVignette = 1f;
    [SerializeField] private float vignetteResponseSpeed = 8f;

    [Header("Spawning")]
    [SerializeField] private float spawnInterval = 8f;
    [SerializeField] private float spawnRadius = 12f;
    [SerializeField] private int maxActiveUnstabilizers = 3;

    private Vignette vignette;
    private float currentVignette;
    private Coroutine spawnRoutine;

    private void Awake()
    {
        worldStability ??= FindAnyObjectByType<WorldStability>();
        if (globalVolume == null)
            globalVolume = FindAnyObjectByType<Volume>();

        if (playerTransform == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                playerTransform = playerObject.transform;
        }

        if (globalVolume != null)
        {
            VolumeProfile profile = globalVolume.profile;
            if (profile != null && !profile.TryGet(out vignette))
                vignette = profile.Add<Vignette>(true);
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
            CollectSpawnPointsFromChildren();
    }

    private void OnEnable()
    {
        StartSpawnRoutineIfNeeded();
    }

    private void Update()
    {
        float stabilityPercent = worldStability != null ? worldStability.StabilityPercent : 100f;
        float targetVignette = EvaluateVignetteTarget(stabilityPercent);

        currentVignette = Mathf.MoveTowards(currentVignette, targetVignette, vignetteResponseSpeed * Time.deltaTime);

        if (vignette != null)
        {
            vignette.intensity.overrideState = true;
            vignette.intensity.value = currentVignette;
        }

        bool shouldSpawn = stabilityPercent > 30f && stabilityPercent < 71f;
        if (shouldSpawn)
            StartSpawnRoutineIfNeeded();
        else if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    private float EvaluateVignetteTarget(float stabilityPercent)
    {
        if (stabilityPercent <= 0f)
            return dimensionBreakVignette;

        if (stabilityPercent <= 30f)
            return criticalZoneVignette;

        if (stabilityPercent <= 70f)
            return warningZoneVignette;

        return safeZoneVignette;
    }

    private void StartSpawnRoutineIfNeeded()
    {
        if (spawnRoutine != null)
            return;

        if (unstabilizerPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
            return;

        spawnRoutine = StartCoroutine(SpawnUnstabilizerRoutine());
    }

    private IEnumerator SpawnUnstabilizerRoutine()
    {
        while (enabled)
        {
            float stabilityPercent = worldStability != null ? worldStability.StabilityPercent : 100f;
            if (stabilityPercent > 30f && stabilityPercent < 71f)
            {
                if (UnstabilizerEnemy.Active.Count < maxActiveUnstabilizers)
                    SpawnOneUnstabilizer();
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnOneUnstabilizer()
    {
        if (unstabilizerPrefab == null)
            return;

        Transform spawnPoint = GetRandomSpawnPoint();
        if (spawnPoint == null)
            return;

        Vector3 spawnPosition = spawnPoint.position + Random.insideUnitSphere * spawnRadius;
        spawnPosition.y = spawnPoint.position.y;

        if (playerTransform != null)
        {
            Vector3 toPlayer = playerTransform.position - spawnPosition;
            toPlayer.y = 0f;

            if (toPlayer.sqrMagnitude < 3f * 3f)
                return;
        }

        Instantiate(unstabilizerPrefab, spawnPosition, Quaternion.identity);
    }

    private Transform GetRandomSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return null;

        return spawnPoints[Random.Range(0, spawnPoints.Length)];
    }

    private void CollectSpawnPointsFromChildren()
    {
        if (transform.childCount == 0)
            return;

        spawnPoints = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
            spawnPoints[i] = transform.GetChild(i);
    }

    private void OnDisable()
    {
        if (spawnRoutine != null)
            StopCoroutine(spawnRoutine);

        spawnRoutine = null;

        if (vignette != null)
        {
            vignette.intensity.overrideState = true;
            vignette.intensity.value = safeZoneVignette;
        }
    }
}
