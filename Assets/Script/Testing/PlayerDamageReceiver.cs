using UnityEngine;
using System.Collections;

public class PlayerDamageReceiver : MonoBehaviour, IDamageable
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerAbilities playerAbilities;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Hit Effect")]
    [SerializeField] private float flickDuration = 0.1f;

    private Animator animator;
    private float hitAnimationTimer;

    private void Awake()
    {
        playerHealth ??= GetComponent<PlayerHealth>();
        playerAbilities ??= GetComponent<PlayerAbilities>();
        spriteRenderer ??= GetComponentInChildren<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (hitAnimationTimer <= 0f)
            return;

        hitAnimationTimer -= Time.deltaTime;

        if (hitAnimationTimer <= 0f)
            animator?.SetBool("getHit", false);
    }

    public void TakeDamage(float amount)
    {
        hitAnimationTimer = flickDuration;
        animator?.SetBool("getHit", true);
        StartCoroutine(FlickWhite());

        if (playerAbilities != null && playerAbilities.TryConsumeShield())
            return;

        playerHealth?.TakeDamage(amount);
    }

    private IEnumerator FlickWhite()
    {
        if (spriteRenderer == null)
            yield break;

        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(flickDuration);
        spriteRenderer.color = originalColor;
    }
}
