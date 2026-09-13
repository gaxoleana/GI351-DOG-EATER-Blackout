using System;
using UnityEngine;

public class EnemyHealth : HealthBase
{
    private const string HitParameter = "getHit";

    [Header("Hit Animation")]
    [SerializeField] private float flickDuration = 0.1f;

    private Animator animator;
    private float hitAnimationTimer;

    public static event Action<EnemyHealth> AnyEnemyDied;

    protected override void Awake()
    {
        base.Awake();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (hitAnimationTimer <= 0f)
            return;

        hitAnimationTimer -= Time.deltaTime;

        if (hitAnimationTimer <= 0f)
            animator?.SetBool(HitParameter, false);
    }

    public override void TakeDamage(float amount)
    {
        if (amount <= 0f || IsDead)
            return;

        hitAnimationTimer = flickDuration;
        animator?.SetBool(HitParameter, true);

        base.TakeDamage(amount);
    }

    protected override void OnDied()
    {
        AnyEnemyDied?.Invoke(this);
        Destroy(gameObject);
    }
}
