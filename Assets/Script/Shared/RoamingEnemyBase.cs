using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public abstract class RoamingEnemyBase : MonoBehaviour
{
    [Header("References")]
    [SerializeField] protected Transform playerTransform;

    private WorldStability worldStability;

    [Header("Roaming")]
    [SerializeField] protected float roamRadius = 5f;
    [SerializeField] protected float roamSpeed = 2f;
    [SerializeField] protected float roamWaitTime = 2f;
    [SerializeField] protected float roamPointTolerance = 0.2f;

    [Header("Detection")]
    [SerializeField] protected float detectionRadius = 6f;
    [SerializeField] protected float chaseSpeed = 3.5f;

    protected Rigidbody rb;
    protected SpriteRenderer spriteRenderer;
    protected Vector3 spawnPosition;
    protected Vector3 currentRoamTarget;
    protected float roamWaitTimer;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        spawnPosition = transform.position;

        worldStability = FindAnyObjectByType<WorldStability>();

        if (playerTransform == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                playerTransform = playerObject.transform;
        }

        PickNewRoamTarget();
    }

    protected float GetDamageMultiplier()
    {
        if (worldStability == null)
            return 1f;

        float stabilityPercent = Mathf.Clamp(worldStability.StabilityPercent, 0f, 100f);
        return stabilityPercent > 0f && stabilityPercent <= 30f ? 2f : 1f;
    }

    protected void UpdateRoaming()
    {
        Vector3 toTarget = currentRoamTarget - transform.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude <= roamPointTolerance * roamPointTolerance)
        {
            rb.linearVelocity = Vector3.zero;
            roamWaitTimer -= Time.deltaTime;

            if (roamWaitTimer <= 0f)
                PickNewRoamTarget();

            return;
        }

        MoveTowards(toTarget.normalized, roamSpeed);
    }

    protected void UpdateChasing()
    {
        Vector3 toPlayer = playerTransform.position - transform.position;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude <= 0.0001f)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        MoveTowards(toPlayer.normalized, chaseSpeed);
    }

    protected bool IsPlayerWithin(float radius)
    {
        if (playerTransform == null)
            return false;

        return (playerTransform.position - transform.position).sqrMagnitude <= radius * radius;
    }

    protected void MoveTowards(Vector3 direction, float speed)
    {
        rb.linearVelocity = direction * speed;
        UpdateFacingDirection(direction.x);
    }

    protected void StopMoving()
    {
        rb.linearVelocity = Vector3.zero;
    }

    protected void UpdateFacingDirection(float horizontalDirection)
    {
        if (spriteRenderer == null)
            return;

        if (horizontalDirection < 0f)
            spriteRenderer.flipX = true;
        else if (horizontalDirection > 0f)
            spriteRenderer.flipX = false;
    }

    protected void PickNewRoamTarget()
    {
        Vector2 randomOffset = Random.insideUnitCircle * roamRadius;
        currentRoamTarget = spawnPosition + new Vector3(randomOffset.x, 0f, randomOffset.y);
        roamWaitTimer = roamWaitTime;
    }
}
