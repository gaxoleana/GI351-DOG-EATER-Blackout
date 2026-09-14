using UnityEngine;
using System;

public class PlayerHealth : HealthBase
{
    public event Action<PlayerHealth> Died;

    public void Heal(float amount)
    {
        if (amount <= 0f || IsDead)
            return;

        currentHealth = Mathf.Min(currentHealth + amount, MaxHealth);
        NotifyHealthChanged();
    }

    public void ResetHealth()
    {
        currentHealth = MaxHealth;
        NotifyHealthChanged();
    }

    protected override void OnDied()
    {
        Died?.Invoke(this);
    }
}
