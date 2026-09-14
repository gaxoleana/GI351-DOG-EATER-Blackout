using UnityEngine;

public class PlayerDamageReceiver : MonoBehaviour, IDamageable
{
    private const string IsHurtParameter = "isHurt";
    private const int HurtFrameDuration = 10;

    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerAbilities playerAbilities;

    private Animator animator;
    private int hurtFramesRemaining;

    private void Awake()
    {
        playerHealth ??= GetComponent<PlayerHealth>();
        playerAbilities ??= GetComponent<PlayerAbilities>();
        animator = GetComponent<Animator>();
        animator ??= GetComponentInChildren<Animator>();
    }

    private void LateUpdate()
    {
        if (hurtFramesRemaining <= 0)
            return;

        hurtFramesRemaining--;

        if (hurtFramesRemaining == 0)
            animator?.SetBool(IsHurtParameter, false);
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f)
            return;

        SetHurtAnimation(true);
        hurtFramesRemaining = HurtFrameDuration;

        if (playerAbilities != null && playerAbilities.TryConsumeShield())
            return;

        playerHealth?.TakeDamage(amount);
    }

    private void SetHurtAnimation(bool isHurt)
    {
        animator?.SetBool(IsHurtParameter, isHurt);
    }
}
