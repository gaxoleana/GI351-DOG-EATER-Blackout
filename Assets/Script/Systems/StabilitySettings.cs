using System;
using UnityEngine;

[CreateAssetMenu(fileName = "StabilitySettings", menuName = "Blackout/Stability Settings")]
public class StabilitySettings : ScriptableObject
{
    [Serializable]
    public class WorldDefinition
    {
        [SerializeField] private string sceneName;
        [SerializeField, Min(1f)] private float maxStability = 100f;

        public string SceneName => sceneName;
        public float MaxStability => maxStability;
    }

    [Header("Worlds")]
    [SerializeField] private WorldDefinition[] worlds;

    [Header("Passive Decay")]
    [Tooltip("Percent of each world's maximum Stability lost every second. 0.166667 means a world reaches 50% after 5 minutes.")]
    [SerializeField, Min(0f)] private float passiveDecayPercentPerSecond = 0.16666667f;

    public WorldDefinition[] Worlds => worlds;
    public float PassiveDecayPercentPerSecond => passiveDecayPercentPerSecond;
}
