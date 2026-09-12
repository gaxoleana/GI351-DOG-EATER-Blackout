using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyAI : MonoBehaviour
{
    private enum State { Roaming, Chasing, Attacking }

    private const string IsWalkingParameter = "isWalk";
    private const string IsAttackParameter = "isAttack";
    private const string AttackStateName = "rat-attack";

    [Header("References")]
    [SerializeField] private Transform playerTransform;

    [Header("Roaming")]
    [SerializeField] private float roamRadius = 5f;
    [SerializeField] private float roamSpeed = 2f;
    [SerializeField] private float roamWaitTime = 2f;
    [SerializeField] private float roamPointTolerance = 0.2f;

    [Header("Detection")]
    [SerializeField] private float detectionRadius = 6f;
    [SerializeField] private float attackRange = 1.2f;
    [SerializeField] private float chaseSpeed = 3.5f;

    [Header("Attack")]
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackAnimationDuration = 0.5f;

    private Rigidbody rb;
    private SpriteRenderer sr;
    private Animator animator;
    private IDamageable playerDamageable;
    private State currentState = State.Roaming;

    private Vector3 spawnPosition;
    private Vector3 currentRoamTarget;
    private float roamWaitTimer;
    private float nextAttackTime;
    private float attackAnimationTimer;
    private float attackDamageTimer;
    private bool attackDamagePending;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        sr = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponent<Animator>();
        spawnPosition = transform.position;

        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                playerTransform = playerObj.transform;
        }

        if (playerTransform != null)
        {
            bool hasDamageable = playerTransform.TryGetComponent(out playerDamageable);
        }

        PickNewRoamTarget();
    }

    private void Update()
    {
        attackAnimationTimer = Mathf.Max(0f, attackAnimationTimer - Time.deltaTime);

        if (attackDamagePending)
        {
            attackDamageTimer -= Time.deltaTime;

            if (attackDamageTimer <= 0f)
            {
                playerDamageable?.TakeDamage(attackDamage);
                attackDamagePending = false;
            }
        }

        if (playerTransform == null)
        {
            currentState = State.Roaming;
        }
        else
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            State newState = distanceToPlayer <= attackRange ? State.Attacking
                          : distanceToPlayer <= detectionRadius ? State.Chasing
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

    private void UpdateRoaming()
    {
        Vector3 toTarget = currentRoamTarget - transform.position;
        toTarget.y = 0f;

        if (toTarget.magnitude <= roamPointTolerance)
        {
            rb.linearVelocity = Vector3.zero;
            roamWaitTimer -= Time.deltaTime;

            if (roamWaitTimer <= 0f)
                PickNewRoamTarget();

            return;
        }

        MoveTowards(toTarget.normalized, roamSpeed);
    }

    private void UpdateChasing()
    {
        Vector3 toPlayer = playerTransform.position - transform.position;
        toPlayer.y = 0f;

        MoveTowards(toPlayer.normalized, chaseSpeed);
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
            animator.CrossFadeInFixedTime(AttackStateName, 0.05f, 0, 0f);
    }

    private void MoveTowards(Vector3 direction, float speed)
    {
        rb.linearVelocity = direction * speed;
        UpdateFacingDirection(direction.x);
    }

    private void UpdateFacingDirection(float horizontalDirection)
    {
        if (sr == null)
            return;

        if (horizontalDirection < 0f)
            sr.flipX = true;
        else if (horizontalDirection > 0f)
            sr.flipX = false;
    }

    private void PickNewRoamTarget()
    {
        Vector2 randomOffset = Random.insideUnitCircle * roamRadius;
        currentRoamTarget = spawnPosition + new Vector3(randomOffset.x, 0f, randomOffset.y);
        roamWaitTimer = roamWaitTime;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
