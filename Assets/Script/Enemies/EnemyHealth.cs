using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : HealthBase
{
    private const string IsHurtParameter = "isHurt";
    private const int HurtFrameDuration = 10;

    private static readonly List<EnemyHealth> active = new();

    private Animator animator;
    private HurtFlashTimer hurtFlash;

    public static event Action<EnemyHealth> AnyEnemyDied;

    // Lets WorldStability read enemy count without scanning the scene every frame.
    public static IReadOnlyList<EnemyHealth> Active => active;

    protected override void Awake()
    {
        base.Awake();
        animator = GetComponent<Animator>();
        hurtFlash = new HurtFlashTimer(animator, IsHurtParameter, HurtFrameDuration);
    }

    private void OnEnable()
    {
        active.Add(this);
    }

    private void OnDisable()
    {
        active.Remove(this);
    }

    private void LateUpdate()
    {
        hurtFlash.Tick();
    }

    public override void TakeDamage(float amount)
    {
        if (amount <= 0f || IsDead)
            return;

        hurtFlash.Trigger();

        base.TakeDamage(amount);
    }

    protected override void OnDied()
    {
        AnyEnemyDied?.Invoke(this);
        Destroy(gameObject);
    }
}
