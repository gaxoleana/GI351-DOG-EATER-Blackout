using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class StabilitySystem : MonoBehaviour, IResourceStat
{
    [Serializable]
    private class WorldDefinition
    {
        [SerializeField] private string sceneName;
        [SerializeField, Min(1f)] private float maxStability = 100f;

        public string SceneName => sceneName;
        public float MaxStability => maxStability;

        public WorldDefinition(string sceneName, float maxStability)
        {
            this.sceneName = sceneName;
            this.maxStability = maxStability;
        }
    }

    private sealed class WorldState
    {
        public readonly float Max;
        public float Current;
        public bool IsBlackedOut;

        public WorldState(float max)
        {
            Max = max;
            Current = max;
        }
    }

    [Header("World Settings")]
    [Tooltip("Each world managed by this Stability System and its maximum Stability.")]
    [FormerlySerializedAs("fallbackWorlds")]
    [SerializeField] private WorldDefinition[] worlds =
    {
        new WorldDefinition("SampleWorld1", 200f),
        new WorldDefinition("SampleWorld2", 600f),
        new WorldDefinition("SampleWorld3", 300f)
    };

    [Header("Passive Decay")]
    [Tooltip("Percent of each world's maximum Stability lost every second. 0.166667 means a world reaches 50% after 5 minutes.")]
    [FormerlySerializedAs("fallbackPassiveDecayPercentPerSecond")]
    [SerializeField, Min(0f)] private float passiveDecayPercentPerSecond = 0.16666667f;

    private readonly Dictionary<string, WorldState> worldStates = new();
    private readonly HashSet<UnityEngine.Object> passiveDecayPauseRequesters = new();
    private bool totalStabilityDepleted;

    public static StabilitySystem Instance { get; private set; }

    public event Action<string, float, float> WorldStabilityChanged;
    public event Action<string> WorldDepleted;
    public event Action<float, float> TotalStabilityChanged;
    public event Action TotalStabilityDepleted;

    public float CurrentStability { get; private set; }
    public float MaxStability { get; private set; }
    public bool IsPassiveDecayPaused => passiveDecayPauseRequesters.Count > 0;

    event Action<float, float> IResourceStat.OnChanged
    {
        add => TotalStabilityChanged += value;
        remove => TotalStabilityChanged -= value;
    }

    float IResourceStat.Current => CurrentStability;
    float IResourceStat.Max => MaxStability;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateRuntimeInstance()
    {
        if (Instance != null)
            return;

        GameObject systemObject = new("StabilitySystem");
        systemObject.AddComponent<StabilitySystem>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeWorlds();
    }

    private void Update()
    {
        if (IsPassiveDecayPaused || passiveDecayPercentPerSecond <= 0f || worldStates.Count == 0)
            return;

        foreach (KeyValuePair<string, WorldState> entry in worldStates)
        {
            WorldState state = entry.Value;
            if (state.IsBlackedOut)
                continue;

            float passiveLoss = state.Max * passiveDecayPercentPerSecond * 0.01f * Time.deltaTime;
            ChangeWorldStability(entry.Key, -passiveLoss);
        }
    }

    public bool TryGetWorldStability(string sceneName, out float current, out float max)
    {
        if (worldStates.TryGetValue(sceneName, out WorldState state))
        {
            current = state.Current;
            max = state.Max;
            return true;
        }

        current = 0f;
        max = 0f;
        return false;
    }

    public void EnsureWorld(string sceneName, float maxStability)
    {
        if (string.IsNullOrWhiteSpace(sceneName) || worldStates.ContainsKey(sceneName))
            return;

        AddWorldDefinition(sceneName, maxStability);
        RefreshTotalStability();
    }

    public bool IsWorldBlackedOut(string sceneName)
    {
        return worldStates.TryGetValue(sceneName, out WorldState state) && state.IsBlackedOut;
    }

    public void ChangeWorldStability(string sceneName, float amount)
    {
        if (Mathf.Approximately(amount, 0f) || !worldStates.TryGetValue(sceneName, out WorldState state) || state.IsBlackedOut)
            return;

        float previous = state.Current;
        state.Current = Mathf.Clamp(state.Current + amount, 0f, state.Max);

        if (Mathf.Approximately(previous, state.Current))
            return;

        RefreshTotalStability();
        WorldStabilityChanged?.Invoke(sceneName, state.Current, state.Max);

        if (state.Current <= 0f)
        {
            state.IsBlackedOut = true;
            WorldDepleted?.Invoke(sceneName);
            CheckTotalDepleted();
        }
    }

    public void SetPassiveDecayPaused(UnityEngine.Object requester, bool isPaused)
    {
        if (requester == null)
            return;

        if (isPaused)
            passiveDecayPauseRequesters.Add(requester);
        else
            passiveDecayPauseRequesters.Remove(requester);
    }

    public void RestoreActiveWorldsPercent(float percent)
    {
        if (percent <= 0f)
            return;

        List<KeyValuePair<string, WorldState>> restoredWorlds = new();

        foreach (KeyValuePair<string, WorldState> entry in worldStates)
        {
            WorldState state = entry.Value;
            if (state.IsBlackedOut)
                continue;

            float previous = state.Current;
            state.Current = Mathf.Clamp(state.Current + state.Max * percent * 0.01f, 0f, state.Max);

            if (!Mathf.Approximately(previous, state.Current))
                restoredWorlds.Add(entry);
        }

        if (restoredWorlds.Count == 0)
            return;

        RefreshTotalStability();

        foreach (KeyValuePair<string, WorldState> entry in restoredWorlds)
            WorldStabilityChanged?.Invoke(entry.Key, entry.Value.Current, entry.Value.Max);
    }

    public void ResetAllWorlds()
    {
        foreach (WorldState state in worldStates.Values)
        {
            state.Current = state.Max;
            state.IsBlackedOut = false;
        }

        totalStabilityDepleted = false;
        RefreshTotalStability();

        foreach (KeyValuePair<string, WorldState> entry in worldStates)
            WorldStabilityChanged?.Invoke(entry.Key, entry.Value.Current, entry.Value.Max);
    }

    private void InitializeWorlds()
    {
        worldStates.Clear();
        MaxStability = 0f;

        if (worlds != null)
        {
            foreach (WorldDefinition definition in worlds)
            {
                if (definition != null)
                    AddWorldDefinition(definition.SceneName, definition.MaxStability);
            }
        }

        CurrentStability = MaxStability;
        TotalStabilityChanged?.Invoke(CurrentStability, MaxStability);
    }

    private void AddWorldDefinition(string sceneName, float maxStability)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return;

        if (worldStates.ContainsKey(sceneName))
        {
            Debug.LogWarning($"StabilitySystem has a duplicate world entry for '{sceneName}'.", this);
            return;
        }

        float max = Mathf.Max(1f, maxStability);
        worldStates.Add(sceneName, new WorldState(max));
        MaxStability += max;
    }

    private void RefreshTotalStability()
    {
        float total = 0f;
        foreach (WorldState state in worldStates.Values)
            total += state.Current;

        CurrentStability = total;
        TotalStabilityChanged?.Invoke(CurrentStability, MaxStability);
    }

    private void CheckTotalDepleted()
    {
        if (CurrentStability > 0f || totalStabilityDepleted)
            return;

        totalStabilityDepleted = true;
        TotalStabilityDepleted?.Invoke();
    }
}
