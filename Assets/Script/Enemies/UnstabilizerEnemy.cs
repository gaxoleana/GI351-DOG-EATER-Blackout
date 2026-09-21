using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class UnstabilizerEnemy : RoamingEnemyBase
{
    [Header("Stability Multiplier")]
    [SerializeField, Min(1f)] private float multiplierPerMonster = 1f;
    [SerializeField, Min(1f)] private float maxMultiplier = 3f;

    private static readonly List<UnstabilizerEnemy> active = new();

    public static IReadOnlyList<UnstabilizerEnemy> Active => active;

    public float StabilityMultiplier => Mathf.Min(1f + (active.Count * multiplierPerMonster), maxMultiplier);

    private void OnEnable()
    {
        active.Add(this);
    }

    private void OnDisable()
    {
        active.Remove(this);
    }

    private void Update()
    {
        StopMoving();
    }
}
