using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : HealthBase
{
    private const string IsHurtParameter = "isHurt";

    private static readonly List<EnemyHealth> active = new();

    [Header("Fragment Drop")]
    [SerializeField] private GameObject fragmentPrefab;
    [SerializeField, Range(0f, 1f)] private float fragmentDropChance = 0.6f;
    [SerializeField, Min(1)] private int fragmentAmount = 1;
    [SerializeField, Min(1)] private int hurtFlashFrameDuration = 10;

    private Animator animator;
    private HurtFlashTimer hurtFlash;

    public static event Action<EnemyHealth> AnyEnemyDied;

    // Lets WorldStability read enemy count without scanning the scene every frame.
    public static IReadOnlyList<EnemyHealth> Active => active;

    protected override void Awake()
    {
        base.Awake();
        animator = GetComponent<Animator>();
        hurtFlash = new HurtFlashTimer(animator, IsHurtParameter, hurtFlashFrameDuration);
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
        TryDropFragment();
        AnyEnemyDied?.Invoke(this);
        Destroy(gameObject);
    }

    private void TryDropFragment()
    {
        if (fragmentPrefab == null || UnityEngine.Random.value >= fragmentDropChance)
            return;

        GameObject fragmentObject = Instantiate(
            fragmentPrefab,
            transform.position + new Vector3(0, 0.5f, 0),
            Quaternion.identity
        );

        FragmentPickup pickup = fragmentObject.GetComponent<FragmentPickup>();
        if (pickup != null)
            pickup.SetAmount(fragmentAmount);
    }
}
