using UnityEngine;
using System;

public class PlayerSanity : MonoBehaviour, IResourceStat
{
    [Header("Sanity")]
    [SerializeField] private float maxSanity = 100f;
    [SerializeField] private float rechargePerSecond = 15f;

    private float currentSanity;

    public event Action<float, float> OnSanityChanged;

    public float CurrentSanity => currentSanity;
    public float MaxSanity => maxSanity;
    public bool HasSanity => currentSanity > 0f;

    event Action<float, float> IResourceStat.OnChanged
    {
        add => OnSanityChanged += value;
        remove => OnSanityChanged -= value;
    }

    float IResourceStat.Current => CurrentSanity;
    float IResourceStat.Max => MaxSanity;

    private void Awake()
    {
        currentSanity = maxSanity;
    }

    private void Start()
    {
        NotifySanityChanged();
    }

    public void Reduce(float amount)
    {
        if (amount <= 0f || currentSanity <= 0f)
            return;

        currentSanity = Mathf.Max(currentSanity - amount, 0f);
        NotifySanityChanged();
    }

    public void ReducePercent(float percent, float deltaTime)
    {
        if (percent <= 0f || deltaTime <= 0f)
            return;

        Reduce(maxSanity * percent * 0.01f * deltaTime);
    }

    public void Recharge(float deltaTime)
    {
        if (currentSanity >= maxSanity)
            return;

        currentSanity = Mathf.Min(currentSanity + rechargePerSecond * deltaTime, maxSanity);
        NotifySanityChanged();
    }

    public void Charge(float deltaTime)
    {
        if (deltaTime <= 0f || currentSanity >= maxSanity)
            return;

        currentSanity = Mathf.Min(currentSanity + rechargePerSecond * deltaTime, maxSanity);
        NotifySanityChanged();
    }

    public void Refill(float amount)
    {
        if (amount <= 0f)
            return;

        currentSanity = Mathf.Min(currentSanity + amount, maxSanity);
        NotifySanityChanged();
    }

    public bool TrySpend(float amount)
    {
        if (amount <= 0f || currentSanity < amount)
            return false;

        currentSanity -= amount;
        NotifySanityChanged();
        return true;
    }

    public bool TrySpendPercent(float percent)
    {
        if (percent <= 0f)
            return false;

        return TrySpend(maxSanity * percent * 0.01f);
    }

    private void NotifySanityChanged()
    {
        OnSanityChanged?.Invoke(currentSanity, maxSanity);
    }

}
