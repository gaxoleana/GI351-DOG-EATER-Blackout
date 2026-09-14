using System;
using UnityEngine;

public class WorldStability : MonoBehaviour, IResourceStat
{
    [Header("Stability")]
    [SerializeField] private float maxStability = 100f;
    [SerializeField] private float decayPerEnemyPerSecond = 0.1f;
    [SerializeField] private float stabilityGainOnKill = 5f;
    [SerializeField] private float scanInterval = 0.25f;

    private float currentStability;
    private float scanTimer;
    private int aliveEnemyCount;
    private bool isDepleted;

    public event Action<float, float> OnStabilityChanged;
    public event Action StabilityDepleted;

    public float CurrentStability => currentStability;
    public float MaxStability => maxStability;
    public float Instability => maxStability > 0f
        ? 1f - Mathf.Clamp01(currentStability / maxStability)
        : 0f;
    public int AliveEnemyCount => aliveEnemyCount;

    event Action<float, float> IResourceStat.OnChanged
    {
        add => OnStabilityChanged += value;
        remove => OnStabilityChanged -= value;
    }

    float IResourceStat.Current => CurrentStability;
    float IResourceStat.Max => MaxStability;

    private void Awake()
    {
        currentStability = Mathf.Max(maxStability, 0f);
    }

    private void OnEnable()
    {
        EnemyHealth.AnyEnemyDied += HandleEnemyDied;
    }

    private void OnDisable()
    {
        EnemyHealth.AnyEnemyDied -= HandleEnemyDied;
    }

    private void Start()
    {
        NotifyStabilityChanged();
    }

    private void Update()
    {
        scanTimer -= Time.deltaTime;
        if (scanTimer <= 0f)
        {
            aliveEnemyCount = EnemyHealth.Active.Count;
            scanTimer = Mathf.Max(0.05f, scanInterval);
        }

        if (aliveEnemyCount <= 0 || currentStability <= 0f)
            return;

        currentStability = Mathf.Max(
            currentStability - decayPerEnemyPerSecond * aliveEnemyCount * Time.deltaTime,
            0f
        );
        NotifyStabilityChanged();

        if (currentStability <= 0f && !isDepleted)
        {
            isDepleted = true;
            StabilityDepleted?.Invoke();
        }
    }

    public void Restore(float amount)
    {
        if (amount <= 0f || currentStability >= maxStability)
            return;

        currentStability = Mathf.Min(currentStability + amount, maxStability);
        NotifyStabilityChanged();
    }

    public void SetStability(float value)
    {
        currentStability = Mathf.Clamp(value, 0f, maxStability);
        isDepleted = currentStability <= 0f;
        NotifyStabilityChanged();
    }

    private void NotifyStabilityChanged()
    {
        OnStabilityChanged?.Invoke(currentStability, maxStability);
    }

    private void HandleEnemyDied(EnemyHealth enemy)
    {
        Restore(stabilityGainOnKill);
    }
}
