using System.Collections.Generic;
using UnityEngine;

// Persistent (static) registry that lets each mirror world's Stability keep "aging" while the
// player isn't inside it. WorldStability writes its current value here when its scene unloads,
// and reads it back on load to apply passive decay for however much real time passed away.
//
// Pure data, no MonoBehaviour needed — a static Dictionary already survives scene loads for the
// lifetime of the application.
public static class MirrorWorldRegistry
{
    private readonly struct Snapshot
    {
        public readonly float stability;
        public readonly float timestamp;

        public Snapshot(float stability, float timestamp)
        {
            this.stability = stability;
            this.timestamp = timestamp;
        }
    }

    private static readonly Dictionary<string, Snapshot> snapshots = new();

    // Called from WorldStability.OnDestroy() when its scene unloads.
    public static void Save(string sceneName, float currentStability)
    {
        if (string.IsNullOrEmpty(sceneName))
            return;

        snapshots[sceneName] = new Snapshot(currentStability, Time.realtimeSinceStartup);
    }

    // Called from WorldStability.Awake() when its scene loads.
    // Returns false if this world has no saved snapshot yet (first time entering it).
    // outStability is already adjusted for elapsed real time using passiveDecayPerSecond,
    // clamped to [0, maxStability].
    public static bool TryResolve(
        string sceneName,
        float passiveDecayPerSecond,
        float maxStability,
        out float outStability
    )
    {
        outStability = maxStability;

        if (string.IsNullOrEmpty(sceneName) || !snapshots.TryGetValue(sceneName, out Snapshot snapshot))
            return false;

        float elapsedSeconds = Mathf.Max(0f, Time.realtimeSinceStartup - snapshot.timestamp);
        outStability = Mathf.Clamp(
            snapshot.stability - passiveDecayPerSecond * elapsedSeconds,
            0f,
            maxStability
        );
        return true;
    }
}