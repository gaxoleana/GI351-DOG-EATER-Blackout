using System;
using System.Collections;
using UnityEngine;

public class WorldStability : MonoBehaviour, IResourceStat
{
    [Header("World Combat Decay")]
    [SerializeField] private float decayPerEnemyPerSecond = 0.1f;
    [SerializeField] private float stabilityGainOnKill = 5f;
    [SerializeField] private float scanInterval = 0.25f;
    [SerializeField, Min(1f)] private float unstabilizerMultiplierPerMonster = 1f;
    [SerializeField, Min(1f)] private float maxUnstabilizerMultiplier = 3f;

    [Header("World Completion")]
    [Tooltip("The world's waves. Leave empty to find the WaveManager in this scene automatically.")]
    [SerializeField] private WaveManager waveManager;

    private float scanTimer;
    private int aliveEnemyCount;
    private int aliveUnstabilizerCount;
    private string sceneName;
    private StabilitySystem stabilitySystem;
    private bool isSubscribed;
    private bool isSubscribedToWaves;

    public event Action<float, float> OnStabilityChanged;
    public event Action StabilityDepleted;

    public float CurrentStability => TryGetStability(out float current, out _) ? current : 0f;
    public float MaxStability => TryGetStability(out _, out float max) ? max : 0f;
    public float Instability => MaxStability > 0f
        ? 1f - Mathf.Clamp01(CurrentStability / MaxStability)
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
        sceneName = gameObject.scene.name;
        stabilitySystem = StabilitySystem.Instance;
    }

    private void OnEnable()
    {
        EnemyHealth.AnyEnemyDied += HandleEnemyDied;
        SubscribeToStabilitySystem();
    }

    private void OnDisable()
    {
        EnemyHealth.AnyEnemyDied -= HandleEnemyDied;
        UnsubscribeFromWaveManager();
        UnsubscribeFromStabilitySystem();
    }

    private IEnumerator Start()
    {
        if (stabilitySystem == null)
        {
            yield return null;
            stabilitySystem = StabilitySystem.Instance;
            SubscribeToStabilitySystem();
        }

        if (stabilitySystem == null)
        {
            Debug.LogError($"WorldStability in '{sceneName}' could not find a StabilitySystem.", this);
            enabled = false;
            yield break;
        }

        NotifyStabilityChanged();
        SubscribeToWaveManager();

        if (stabilitySystem.IsWorldBlackedOut(sceneName))
            StabilityDepleted?.Invoke();
    }

    private void Update()
    {
        if (stabilitySystem == null || stabilitySystem.IsWorldCleared(sceneName))
            return;

        scanTimer -= Time.deltaTime;
        if (scanTimer <= 0f)
        {
            aliveEnemyCount = EnemyHealth.Active.Count;
            aliveUnstabilizerCount = UnstabilizerEnemy.Active.Count;
            scanTimer = Mathf.Max(0.05f, scanInterval);
        }

        if (aliveEnemyCount <= 0 || stabilitySystem.IsWorldBlackedOut(sceneName))
            return;

        float multiplier = Mathf.Min(
            1f + (aliveUnstabilizerCount * unstabilizerMultiplierPerMonster),
            maxUnstabilizerMultiplier
        );

        stabilitySystem.ChangeWorldStability(
            sceneName,
            -decayPerEnemyPerSecond * aliveEnemyCount * multiplier * Time.deltaTime
        );
    }

    public void Restore(float amount)
    {
        if (amount <= 0f || stabilitySystem == null)
            return;

        stabilitySystem.ChangeWorldStability(sceneName, amount);
    }

    public void SetStability(float value)
    {
        if (stabilitySystem == null || !TryGetStability(out float current, out _))
            return;

        stabilitySystem.ChangeWorldStability(sceneName, value - current);
    }

    private void NotifyStabilityChanged()
    {
        if (TryGetStability(out float current, out float max))
            OnStabilityChanged?.Invoke(current, max);
    }

    private bool TryGetStability(out float current, out float max)
    {
        current = 0f;
        max = 0f;

        return stabilitySystem != null && stabilitySystem.TryGetWorldStability(sceneName, out current, out max);
    }

    private void HandleEnemyDied(EnemyHealth enemy)
    {
        Restore(stabilityGainOnKill);
    }

    private void SubscribeToWaveManager()
    {
        if (isSubscribedToWaves)
            return;

        waveManager ??= FindAnyObjectByType<WaveManager>();
        if (waveManager == null)
            return;

        waveManager.AllWavesCleared += HandleAllWavesCleared;
        isSubscribedToWaves = true;

        if (waveManager.IsComplete)
            HandleAllWavesCleared();
    }

    private void UnsubscribeFromWaveManager()
    {
        if (!isSubscribedToWaves || waveManager == null)
            return;

        waveManager.AllWavesCleared -= HandleAllWavesCleared;
        isSubscribedToWaves = false;
    }

    private void HandleAllWavesCleared()
    {
        stabilitySystem?.MarkWorldCleared(sceneName);
    }

    private void SubscribeToStabilitySystem()
    {
        if (isSubscribed)
            return;

        stabilitySystem ??= StabilitySystem.Instance;
        if (stabilitySystem == null)
            return;

        stabilitySystem.WorldStabilityChanged += HandleWorldStabilityChanged;
        stabilitySystem.WorldDepleted += HandleWorldDepleted;
        isSubscribed = true;
    }

    private void UnsubscribeFromStabilitySystem()
    {
        if (!isSubscribed || stabilitySystem == null)
            return;

        stabilitySystem.WorldStabilityChanged -= HandleWorldStabilityChanged;
        stabilitySystem.WorldDepleted -= HandleWorldDepleted;
        isSubscribed = false;
    }

    private void HandleWorldStabilityChanged(string changedSceneName, float current, float max)
    {
        if (changedSceneName == sceneName)
            OnStabilityChanged?.Invoke(current, max);
    }

    private void HandleWorldDepleted(string depletedSceneName)
    {
        if (depletedSceneName == sceneName)
            StabilityDepleted?.Invoke();
    }
}
