using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(EnemyHealth))]
public class EnemyRangedAI : RoamingEnemyBase
{
    private enum State
    {
        Roaming,
        Attacking
    }

    private const string IsWalkingParameter = "isWalk";
    private const string IsAttackParameter = "isAttack";

    [Header("Ranged Attack")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private float projectileDamage = 10f;
    [SerializeField] private float projectileLifetime = 3f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackAnimationDuration = 0.5f;
    [SerializeField] private float attackFireDelay = 0.25f;

    private Animator animator;
    private State currentState = State.Roaming;
    private float nextAttackTime;
    private float attackAnimationTimer;
    private float attackFireTimer;
    private bool attackPending;

    protected override void Awake()
    {
        base.Awake();
        animator = GetComponent<Animator>();

        if (attackPoint == null)
            attackPoint = transform;
    }

    private void Update()
    {
        UpdateTimers();

        if (playerTransform != null && IsPlayerWithin(detectionRadius))
        {
            currentState = State.Attacking;
            StopMoving();
            UpdateFacingDirection(GetDirectionToPlayer().x);
            UpdateAttack();
        }
        else
        {
            currentState = State.Roaming;
            attackPending = false;
            UpdateRoaming();
        }

        UpdateAnimationState();
    }

    private void UpdateTimers()
    {
        attackAnimationTimer = Mathf.Max(0f, attackAnimationTimer - Time.deltaTime);

        if (!attackPending)
            return;

        attackFireTimer -= Time.deltaTime;
        if (attackFireTimer <= 0f)
        {
            attackPending = false;
            FireProjectile();
        }
    }

    private void UpdateAttack()
    {
        if (attackPending || Time.time < nextAttackTime)
            return;

        attackAnimationTimer = Mathf.Max(0f, attackAnimationDuration);
        attackFireTimer = Mathf.Clamp(attackFireDelay, 0f, attackAnimationDuration);
        attackPending = true;
        nextAttackTime = Time.time + Mathf.Max(0f, attackCooldown);
    }

    private void FireProjectile()
    {
        if (projectilePrefab == null || attackPoint == null || playerTransform == null)
            return;

        GameObject projectileObject = Instantiate(
            projectilePrefab,
            attackPoint.position,
            attackPoint.rotation
        );

        AttackProjectile projectile = projectileObject.GetComponent<AttackProjectile>();
        if (projectile == null)
        {
            Destroy(projectileObject);
            return;
        }

        Vector3 direction = GetDirectionToPlayer();
        float finalDamage = projectileDamage * GetDamageMultiplier();
        projectile.Launch(
            direction,
            projectileSpeed,
            finalDamage,
            projectileLifetime,
            gameObject
        );
    }

    private Vector3 GetDirectionToPlayer()
    {
        Vector3 direction = playerTransform != null
            ? playerTransform.position - attackPoint.position
            : transform.forward;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
    }

    private void UpdateAnimationState()
    {
        if (animator == null)
            return;

        bool isAttacking = currentState == State.Attacking && attackAnimationTimer > 0f;
        bool isMoving = rb != null && rb.linearVelocity.sqrMagnitude > 0.0001f;

        animator.SetBool(IsWalkingParameter, isMoving && !isAttacking);
        animator.SetBool(IsAttackParameter, isAttacking);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
