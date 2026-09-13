using UnityEngine;

public class PlayerDamageReceiver : MonoBehaviour, IDamageable
{
    private const string HitParameter = "getHit";

    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerAbilities playerAbilities;

    [Header("Hit Effect")]
    [SerializeField] private float flickDuration = 0.1f;

    private Animator animator;
    private float hitAnimationTimer;

    private void Awake()
    {
        playerHealth ??= GetComponent<PlayerHealth>();
        playerAbilities ??= GetComponent<PlayerAbilities>();
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

    public void TakeDamage(float amount)
    {
        hitAnimationTimer = flickDuration;
        animator?.SetBool(HitParameter, true);

        if (playerAbilities != null && playerAbilities.TryConsumeShield())
            return;

        playerHealth?.TakeDamage(amount);
    }
}
