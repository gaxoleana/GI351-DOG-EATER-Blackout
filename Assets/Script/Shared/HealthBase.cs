using System;
using UnityEngine;

public abstract class HealthBase : MonoBehaviour, IDamageable, IResourceStat
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    protected float currentHealth;

    public event Action<float, float> OnHealthChanged;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0f;

    event Action<float, float> IResourceStat.OnChanged
    {
        add => OnHealthChanged += value;
        remove => OnHealthChanged -= value;
    }

    float IResourceStat.Current => CurrentHealth;
    float IResourceStat.Max => MaxHealth;

    protected virtual void Awake()
    {
        maxHealth = Mathf.Max(maxHealth, 0f);
        currentHealth = maxHealth;
    }

    protected virtual void Start()
    {
        NotifyHealthChanged();
    }

    public virtual void TakeDamage(float amount)
    {
        if (amount <= 0f || IsDead)
            return;

        currentHealth = Mathf.Max(currentHealth - amount, 0f);
        NotifyHealthChanged();

        if (IsDead)
            OnDied();
    }

    protected virtual void OnDied()
    {
    }

    protected void NotifyHealthChanged()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
