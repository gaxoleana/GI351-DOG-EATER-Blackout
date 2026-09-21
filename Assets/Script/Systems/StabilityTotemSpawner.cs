using System.Collections;
using UnityEngine;

public class StabilityTotemSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private StabilityTotem totemPrefab;
    [Tooltip("Possible locations for a Totem. Drag scene transforms here.")]
    [SerializeField] private Transform[] spawnPoints;
    [Tooltip("Local offset from the selected Spawn Point.")]
    [SerializeField] private Vector3 spawnOffset;

    [Header("Spawn Timing")]
    [Tooltip("A random cooldown is chosen between these two values before every spawn attempt.")]
    [SerializeField, Min(0f)] private float minimumCooldownSeconds = 60f;
    [SerializeField, Min(0f)] private float maximumCooldownSeconds = 120f;
    [Tooltip("Chance that a Totem appears after a cooldown completes. Lower values make Totems rarer.")]
    [SerializeField, Range(0f, 1f)] private float spawnChancePerAttempt = 0.25f;

    private static StabilityTotem activeTotem;
    private Coroutine spawnRoutine;

    private void OnEnable()
    {
        spawnRoutine = StartCoroutine(SpawnRoutine());
    }

    private void OnDisable()
    {
        if (spawnRoutine != null)
            StopCoroutine(spawnRoutine);

        spawnRoutine = null;
    }

    private IEnumerator SpawnRoutine()
    {
        while (enabled)
        {
            float minimum = Mathf.Min(minimumCooldownSeconds, maximumCooldownSeconds);
            float maximum = Mathf.Max(minimumCooldownSeconds, maximumCooldownSeconds);
            yield return new WaitForSeconds(Random.Range(minimum, maximum));

            if (!PlayerPersistenceManager.IsGameplayActive
                || activeTotem != null
                || totemPrefab == null
                || spawnPoints == null
                || spawnPoints.Length == 0
                || Random.value > spawnChancePerAttempt)
                continue;

            Transform spawnPoint = GetRandomSpawnPoint();
            if (spawnPoint == null)
                continue;

            activeTotem = Instantiate(
                totemPrefab,
                spawnPoint.TransformPoint(spawnOffset),
                spawnPoint.rotation
            );
        }
    }

    private Transform GetRandomSpawnPoint()
    {
        int validPointCount = 0;
        foreach (Transform point in spawnPoints)
        {
            if (point != null)
                validPointCount++;
        }

        if (validPointCount == 0)
            return null;

        int targetIndex = Random.Range(0, validPointCount);
        foreach (Transform point in spawnPoints)
        {
            if (point == null)
                continue;

            if (targetIndex-- == 0)
                return point;
        }

        return null;
    }
}
