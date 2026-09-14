using System;
using UnityEngine;

public class WorldStability : MonoBehaviour, IResourceStat
{
    [Header("Stability")]
    [SerializeField] private float maxStability = 100f;
    [SerializeField] private float decayPerEnemyPerSecond = 0.1f;
    [SerializeField] private float stabilityGainOnKill = 5f;
    [SerializeField] private float scanInterval = 0.25f;

    [Header("Passive Decay")]
    [Tooltip("Stability lost per real second that passes while this world's scene is unloaded " +
             "(e.g. player is in the Hub or inside another mirror world).")]
    [SerializeField] private float passiveDecayPerSecond = 0.5f;

    private float currentStability;
    private float scanTimer;
    private int aliveEnemyCount;
    private bool isDepleted;
    private string sceneName;

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
        sceneName = gameObject.scene.name;
        maxStability = Mathf.Max(maxStability, 0f);

        currentStability = MirrorWorldRegistry.TryResolve(
            sceneName,
            passiveDecayPerSecond,
            maxStability,
            out float resolvedStability
        )
            ? resolvedStability
            : maxStability;
    }

    private void OnEnable()
    {
        EnemyHealth.AnyEnemyDied += HandleEnemyDied;
    }

    private void OnDisable()
    {
        EnemyHealth.AnyEnemyDied -= HandleEnemyDied;
    }

    private void OnDestroy()
    {
        // Persist wherever we ended up so the next time this scene loads (or is checked
        // passively) picks up decay from here, not from a fresh maxStability.
        MirrorWorldRegistry.Save(sceneName, currentStability);
    }

    private void Start()
    {
        NotifyStabilityChanged();

        // Covers the case where passive decay already brought this world to 0 before we
        // even loaded it — without this, a world that "died" offline would never fire
        // StabilityDepleted, since Update() below exits early once currentStability <= 0.
        CheckDepleted();
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

        CheckDepleted();
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

    private void CheckDepleted()
    {
        if (currentStability <= 0f && !isDepleted)
        {
            isDepleted = true;
            StabilityDepleted?.Invoke();
        }
    }

    private void HandleEnemyDied(EnemyHealth enemy)
    {
        Restore(stabilityGainOnKill);
    }
}