using System.Collections.Generic;
using UnityEngine;

public class PlayerPanic : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float panicStartDistance = 6f;
    [SerializeField] private float maximumPanicDistance = 1.5f;
    [SerializeField] private float scanInterval = 0.1f;
    [SerializeField] private int maxNearbyEnemies = 3;
    [SerializeField, Range(0f, 0.5f)] private float crowdPanicBonus = 0.12f;

    [Header("Response")]
    [SerializeField] private float panicSmoothing = 2.5f;
    [SerializeField] private PanicCameraShake cameraShake;

    public float CurrentPanic { get; private set; }

    private float targetPanic;
    private float scanTimer;

    private void Awake()
    {
        ResolveCameraShake();
    }

    private void Update()
    {
        if (cameraShake == null)
            ResolveCameraShake();

        scanTimer -= Time.deltaTime;

        if (scanTimer <= 0f)
        {
            scanTimer = scanInterval;
            targetPanic = CalculateTargetPanic();
        }

        CurrentPanic = Mathf.MoveTowards(
            CurrentPanic,
            targetPanic,
            panicSmoothing * Time.deltaTime
        );

        cameraShake?.SetIntensity(CurrentPanic);
    }

    private float CalculateTargetPanic()
    {
        float nearestDistanceSquared = float.MaxValue;
        int nearbyEnemyCount = 0;
        IReadOnlyList<EnemyAI> enemies = EnemyAI.Active;

        foreach (EnemyAI enemy in enemies)
        {
            if (enemy == null)
                continue;

            float distanceSquared = (enemy.transform.position - transform.position).sqrMagnitude;
            nearestDistanceSquared = Mathf.Min(nearestDistanceSquared, distanceSquared);

            if (distanceSquared < panicStartDistance * panicStartDistance)
                nearbyEnemyCount++;
        }

        if (nearestDistanceSquared == float.MaxValue || nearestDistanceSquared >= panicStartDistance * panicStartDistance)
            return 0f;

        float nearestDistance = Mathf.Sqrt(nearestDistanceSquared);

        float panic = Mathf.InverseLerp(
            panicStartDistance,
            maximumPanicDistance,
            nearestDistance
        );

        int additionalEnemies = Mathf.Clamp(nearbyEnemyCount - 1, 0, Mathf.Max(0, maxNearbyEnemies - 1));
        panic += additionalEnemies * crowdPanicBonus;

        return Mathf.Clamp01(panic);
    }

    private void OnDisable()
    {
        cameraShake?.SetIntensity(0f);
    }

    private void ResolveCameraShake()
    {
        if (Camera.main == null)
            return;

        cameraShake = Camera.main.GetComponent<PanicCameraShake>();

        if (cameraShake == null)
            cameraShake = Camera.main.gameObject.AddComponent<PanicCameraShake>();
    }
}
