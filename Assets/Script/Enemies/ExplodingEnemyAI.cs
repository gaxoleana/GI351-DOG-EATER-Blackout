using UnityEngine;

[RequireComponent(typeof(Animator), typeof(EnemyHealth))]
public class ExplodingEnemyAI : RoamingEnemyBase
{
    private enum State { Roaming, Chasing, Exploding }

    private const string IsWalkingParameter = "isWalk";
    private const string IsExplodingParameter = "isExplode";

    [Header("Explosion Settings")]
    [SerializeField] private float explosionDamage = 75f;
    [SerializeField] private float explosionAnimationDuration = 0.5f;
    [SerializeField] private float playerStunDuration = 3f;

    [Header("Explosion Audio")]
    [SerializeField] private AudioClip explosionSFX;
    [SerializeField, Range(0f, 1f)] private float explosionVolume = 1f;

    private Animator animator;
    private State currentState = State.Roaming;
    private IDamageable playerDamageable;
    private PlayerController playerController;

    private float explosionTimer;

    protected override void Awake()
    {
        base.Awake();
        animator = GetComponent<Animator>();

        if (playerTransform != null)
        {
            playerDamageable = playerTransform.GetComponent<PlayerDamageReceiver>();
            playerDamageable ??= playerTransform.GetComponent<PlayerHealth>();
            playerController = playerTransform.GetComponent<PlayerController>();
        }
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
            if (IsPlayerWithin(detectionRadius))
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

    private void BeginExplosion()
    {
        if (currentState == State.Exploding)
            return;

        currentState = State.Exploding;
        explosionTimer = Mathf.Max(0f, explosionAnimationDuration);
        rb.linearVelocity = Vector3.zero;

        // เล่นเสียงระเบิดแบบ 3D ณ ตำแหน่งระเบิด
        if (explosionSFX != null)
        {
            AudioSource.PlayClipAtPoint(explosionSFX, transform.position, explosionVolume);
        }

        animator?.SetBool(IsExplodingParameter, true);
        playerController?.Stun(playerStunDuration);
        playerDamageable?.TakeDamage(explosionDamage * BlackoutZoneController.GetEnemyDamageMultiplier(this));
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}