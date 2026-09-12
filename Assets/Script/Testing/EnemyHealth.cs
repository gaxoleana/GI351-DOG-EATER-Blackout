using UnityEngine;
using System;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 30f;

    [Header("Hit Animation")]
    [SerializeField] private float flickDuration = 0.1f;

    private Animator animator;
    private float currentHealth;
    private bool isDead;
    private float hitAnimationTimer;

    public event Action<float, float> OnHealthChanged;
    public static event Action<EnemyHealth> AnyEnemyDied;

    private void Awake()
    {
        currentHealth = Mathf.Max(maxHealth, 0f);
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
        if (isDead)
            return;

        ApplyDamage(amount);
        NotifyHealthChanged();
        hitAnimationTimer = flickDuration;
        animator?.SetBool("getHit", true);

        if (currentHealth <= 0f)
            Die();
    }

    private void ApplyDamage(float amount)
    {
        currentHealth = Mathf.Max(currentHealth - amount, 0f);
    }

    private void NotifyHealthChanged()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        isDead = true;
        AnyEnemyDied?.Invoke(this);
        Destroy(gameObject);
    }
}
