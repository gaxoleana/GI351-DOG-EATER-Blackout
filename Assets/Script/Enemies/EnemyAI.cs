using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : RoamingEnemyBase
{
    private enum State { Roaming, Chasing, Attacking }

    private const string IsWalkingParameter = "isWalk";
    private const string IsAttackParameter = "isAttack";

    private static readonly List<EnemyAI> active = new();

    [Header("Attack")]
    [SerializeField] private float attackRange = 1.2f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackAnimationDuration = 0.5f;
    [SerializeField] private string attackStateName = "rat-attack";

    private Animator animator;
    private IDamageable playerDamageable;
    private State currentState = State.Roaming;

    private float nextAttackTime;
    private float attackAnimationTimer;
    private float attackDamageTimer;
    private bool attackDamagePending;

    // Lets PlayerPanic read nearby enemies without scanning the scene every frame.
    public static IReadOnlyList<EnemyAI> Active => active;

    protected override void Awake()
    {
        base.Awake();
        animator = GetComponent<Animator>();

        if (playerTransform != null)
        {
            playerDamageable = playerTransform.GetComponent<PlayerDamageReceiver>();
            playerDamageable ??= playerTransform.GetComponent<PlayerHealth>();
        }
    }

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
        attackAnimationTimer = Mathf.Max(0f, attackAnimationTimer - Time.deltaTime);

        if (attackDamagePending)
        {
            attackDamageTimer -= Time.deltaTime;

            if (attackDamageTimer <= 0f)
            {
                float finalDamage = attackDamage * GetDamageMultiplier();
                playerDamageable?.TakeDamage(finalDamage);
                attackDamagePending = false;
            }
        }

        if (playerTransform == null)
        {
            currentState = State.Roaming;
        }
        else
        {
            State newState = IsPlayerWithin(attackRange) ? State.Attacking
                          : IsPlayerWithin(detectionRadius) ? State.Chasing
                          : State.Roaming;
            currentState = newState;
        }

        switch (currentState)
        {
            case State.Roaming:
                UpdateRoaming();
                break;
            case State.Chasing:
                UpdateChasing();
                break;
            case State.Attacking:
                UpdateAttacking();
                break;
        }

        UpdateAnimationState();
    }

    private void UpdateAnimationState()
    {
        if (animator == null)
            return;

        bool isAttacking = attackAnimationTimer > 0f;
        bool isMoving = rb != null && rb.linearVelocity.sqrMagnitude > 0.0001f;

        animator.SetBool(IsWalkingParameter, isMoving && !isAttacking);
        animator.SetBool(IsAttackParameter, isAttacking);
    }

    private void UpdateAttacking()
    {
        rb.linearVelocity = Vector3.zero;

        if (Time.time < nextAttackTime || attackDamagePending)
        {
            return;
        }

        attackAnimationTimer = attackAnimationDuration;
        attackDamageTimer = attackAnimationDuration;
        attackDamagePending = true;
        nextAttackTime = Time.time + attackCooldown;

        if (animator != null)
            animator.CrossFadeInFixedTime(attackStateName, 0.05f, 0, 0f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
