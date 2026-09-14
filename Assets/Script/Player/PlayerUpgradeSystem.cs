using System;
using UnityEngine;

public enum PlayerUpgradeNode
{
    SanityCharge,
    SanityShield,
    Laser,
    SanityRun
}

public class PlayerUpgradeSystem : MonoBehaviour
{
    private const long UpgradeCost = 1;

    [Header("Upgrade Levels")]
    [SerializeField, Min(0)] private int sanityChargeLevel;
    [SerializeField, Min(0)] private int sanityShieldLevel;
    [SerializeField, Min(0)] private int laserLevel;
    [SerializeField, Min(0)] private int sanityRunLevel;

    private FragmentCurrency currency;

    public event Action<PlayerUpgradeNode, int> OnUpgradeChanged;

    public int SanityChargeLevel => sanityChargeLevel;
    public int SanityShieldLevel => sanityShieldLevel;
    public int LaserLevel => laserLevel;
    public int SanityRunLevel => sanityRunLevel;

    private void Awake()
    {
        currency = GetComponent<FragmentCurrency>();
    }

    public int GetLevel(PlayerUpgradeNode node)
    {
        return node switch
        {
            PlayerUpgradeNode.SanityCharge => sanityChargeLevel,
            PlayerUpgradeNode.SanityShield => sanityShieldLevel,
            PlayerUpgradeNode.Laser => laserLevel,
            PlayerUpgradeNode.SanityRun => sanityRunLevel,
            _ => 0
        };
    }

    public bool TryUpgrade(PlayerUpgradeNode node)
    {
        currency ??= GetComponent<FragmentCurrency>();
        if (currency == null || !currency.TrySpend(UpgradeCost))
            return false;

        int level = GetLevel(node) + 1;
        SetLevel(node, level);
        OnUpgradeChanged?.Invoke(node, level);
        return true;
    }

    public float GetSanityRechargeMultiplier()
    {
        return 1f + sanityChargeLevel * 0.05f;
    }

    public float GetSanityDrainPercent(float basePercent)
    {
        return Mathf.Max(0f, basePercent - sanityChargeLevel * 0.1f);
    }

    public float GetShieldSpeedMultiplier(float baseMultiplier)
    {
        return Mathf.Clamp01(baseMultiplier + sanityShieldLevel * 0.025f);
    }

    public float GetShieldHitCostPercent(float basePercent)
    {
        return Mathf.Max(0f, basePercent - sanityShieldLevel * 1f);
    }

    public float GetLaserCostPercent(float basePercent)
    {
        return Mathf.Max(0f, basePercent - laserLevel * 0.5f);
    }

    public float GetLaserCooldown(float baseCooldown)
    {
        return Mathf.Max(0.05f, baseCooldown - laserLevel * 0.05f);
    }

    public float GetSanityRunSpeedMultiplier(float baseMultiplier)
    {
        return baseMultiplier * (1f + sanityRunLevel * 0.015f);
    }

    private void SetLevel(PlayerUpgradeNode node, int level)
    {
        switch (node)
        {
            case PlayerUpgradeNode.SanityCharge:
                sanityChargeLevel = level;
                break;
            case PlayerUpgradeNode.SanityShield:
                sanityShieldLevel = level;
                break;
            case PlayerUpgradeNode.Laser:
                laserLevel = level;
                break;
            case PlayerUpgradeNode.SanityRun:
                sanityRunLevel = level;
                break;
        }
    }
}
