using UnityEngine;

public class PlayerDamageReceiver : MonoBehaviour, IDamageable
{
    private const string IsHurtParameter = "isHurt";

    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerAbilities playerAbilities;
    [SerializeField, Min(1)] private int hurtFlashFrameDuration = 10;

    private Animator animator;
    private HurtFlashTimer hurtFlash;

    private void Awake()
    {
        playerHealth ??= GetComponent<PlayerHealth>();
        playerAbilities ??= GetComponent<PlayerAbilities>();
        animator = GetComponent<Animator>();
        animator ??= GetComponentInChildren<Animator>();
        hurtFlash = new HurtFlashTimer(animator, IsHurtParameter, hurtFlashFrameDuration);
    }

    private void LateUpdate()
    {
        hurtFlash.Tick();
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f || !PlayerPersistenceManager.IsGameplayActive)
            return;

        hurtFlash.Trigger();

        if (playerAbilities != null && playerAbilities.TryConsumeShield())
            return;

        playerHealth?.TakeDamage(amount);
    }
}
