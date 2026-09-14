using System;
using UnityEngine;

public class EnemyHealth : HealthBase
{
    private const string IsHurtParameter = "isHurt";
    private const int HurtFrameDuration = 10;

    private Animator animator;
    private int hurtFramesRemaining;

    public static event Action<EnemyHealth> AnyEnemyDied;

    protected override void Awake()
    {
        base.Awake();
        animator = GetComponent<Animator>();
    }

    private void LateUpdate()
    {
        if (hurtFramesRemaining <= 0)
            return;

        hurtFramesRemaining--;

        if (hurtFramesRemaining == 0)
            animator?.SetBool(IsHurtParameter, false);
    }

    public override void TakeDamage(float amount)
    {
        if (amount <= 0f || IsDead)
            return;

        animator?.SetBool(IsHurtParameter, true);
        hurtFramesRemaining = HurtFrameDuration;

        base.TakeDamage(amount);
    }

    protected override void OnDied()
    {
        AnyEnemyDied?.Invoke(this);
        Destroy(gameObject);
    }
}
