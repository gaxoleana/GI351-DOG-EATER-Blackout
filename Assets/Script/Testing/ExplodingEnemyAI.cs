using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Animator), typeof(EnemyHealth))]
public class ExplodingEnemyAI : MonoBehaviour
{
    private enum State { Roaming, Chasing, Exploding }

    private const string IsWalkingParameter = "isWalk";
    private const string IsExplodingParameter = "isExplode";

    [Header("References")]
    [SerializeField] private Transform playerTransform;

    [Header("Roaming")]
    [SerializeField] private float roamRadius = 5f;
    [SerializeField] private float roamSpeed = 2f;
    [SerializeField] private float roamWaitTime = 2f;
    [SerializeField] private float roamPointTolerance = 0.2f;

    [Header("Detection")]
    [SerializeField] private float detectionRadius = 12f;
    [SerializeField] private float chaseSpeed = 7f;

    [Header("Explosion")]
    [SerializeField] private float explosionDamage = 75f;
    [SerializeField] private float explosionAnimationDuration = 0.5f;
    [SerializeField] private float playerStunDuration = 3f;

    private Rigidbody rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private State currentState = State.Roaming;
    private IDamageable playerDamageable;
    private PlayerController playerController;

    private Vector3 spawnPosition;
    private Vector3 currentRoamTarget;
    private float roamWaitTimer;
    private float explosionTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        spawnPosition = transform.position;

        if (playerTransform == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                playerTransform = playerObject.transform;
        }

        if (playerTransform != null)
        {
            playerTransform.TryGetComponent(out playerDamageable);
            playerController = playerTransform.GetComponent<PlayerController>();
        }

        PickNewRoamTarget();
    }

    private void Update()
    {
        if (currentState == State.Exploding)
        {
            UpdateExplosion();
            UpdateAnimationState();
            return;
        }

        if (playerTransform == null)
        {
            currentState = State.Roaming;
        }
        else
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            if (distanceToPlayer <= detectionRadius)
                currentState = State.Chasing;
            else
                currentState = State.Roaming;
        }

        if (currentState == State.Roaming)
            UpdateRoaming();
        else if (currentState == State.Chasing)
            UpdateChasing();

        UpdateAnimationState();
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

        if (toPlayer.sqrMagnitude > 0.0001f)
            MoveTowards(toPlayer.normalized, chaseSpeed);
        else
            rb.linearVelocity = Vector3.zero;
    }

    private void BeginExplosion()
    {
        if (currentState == State.Exploding)
            return;

        currentState = State.Exploding;
        explosionTimer = Mathf.Max(0f, explosionAnimationDuration);
        rb.linearVelocity = Vector3.zero;
        animator?.SetBool(IsExplodingParameter, true);
        playerController?.Stun(playerStunDuration);
        playerDamageable?.TakeDamage(explosionDamage);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryExplodeOnPlayerContact(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryExplodeOnPlayerContact(other);
    }

    private void TryExplodeOnPlayerContact(Collider other)
    {
        if (currentState != State.Chasing || playerTransform == null)
            return;

        if (other.transform.root != playerTransform.root)
            return;

        BeginExplosion();
    }

    private void UpdateExplosion()
    {
        rb.linearVelocity = Vector3.zero;
        explosionTimer -= Time.deltaTime;

        if (explosionTimer > 0f)
            return;

        Destroy(gameObject);
    }

    private void UpdateAnimationState()
    {
        if (animator == null)
            return;

        animator.SetBool(IsWalkingParameter, currentState != State.Exploding && rb.linearVelocity.sqrMagnitude > 0.0001f);
        animator.SetBool(IsExplodingParameter, currentState == State.Exploding);
    }

    private void MoveTowards(Vector3 direction, float speed)
    {
        rb.linearVelocity = direction * speed;
        UpdateFacingDirection(direction.x);
    }

    private void UpdateFacingDirection(float horizontalDirection)
    {
        if (spriteRenderer == null)
            return;

        if (horizontalDirection < 0f)
            spriteRenderer.flipX = true;
        else if (horizontalDirection > 0f)
            spriteRenderer.flipX = false;
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

    }
}