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
    [Header("Upgrade Cost")]
    [SerializeField, Min(1)] private long upgradeCost = 1;

    [Header("Upgrade Levels")]
    [SerializeField, Min(1)] private int maxUpgradeLevel = 20;
    [SerializeField, Min(0)] private int sanityChargeLevel;
    [SerializeField, Min(0)] private int sanityShieldLevel;
    [SerializeField, Min(0)] private int laserLevel;
    [SerializeField, Min(0)] private int sanityRunLevel;

    [Header("Per-Level Effects")]
    [SerializeField, Min(0f)] private float sanityRechargeBonusPerLevel = 0.05f;
    [SerializeField, Min(0f)] private float sanityDrainReductionPerLevel = 0.1f;
    [SerializeField, Min(0f)] private float shieldSpeedBonusPerLevel = 0.025f;
    [SerializeField, Min(0f)] private float shieldHitCostReductionPerLevel = 1f;
    [SerializeField, Min(0f)] private float laserCostReductionPerLevel = 0.5f;
    [SerializeField, Min(0f)] private float laserCooldownReductionPerLevel = 0.05f;
    [SerializeField, Min(0.01f)] private float minimumLaserCooldown = 0.05f;
    [SerializeField, Min(0f)] private float sanityRunSpeedBonusPerLevel = 0.015f;

    private FragmentCurrency currency;

    public event Action<PlayerUpgradeNode, int> OnUpgradeChanged;

    public int SanityChargeLevel => sanityChargeLevel;
    public int SanityShieldLevel => sanityShieldLevel;
    public int LaserLevel => laserLevel;
    public int SanityRunLevel => sanityRunLevel;
    public long UpgradeCost => upgradeCost;
    public int MaxUpgradeLevel => maxUpgradeLevel;

    // Values below are the total benefit from the player's current upgrade levels.
    // They are exposed for UI only, so the overview always reflects the same values
    // that the ability scripts use in-game.
    public float SanityRechargeBonusPercent => sanityChargeLevel * sanityRechargeBonusPerLevel * 100f;
    public float SanityDrainReductionPercent => sanityChargeLevel * sanityDrainReductionPerLevel;
    public float ShieldSpeedBonusPercent => sanityShieldLevel * shieldSpeedBonusPerLevel * 100f;
    public float ShieldHitCostReductionPercent => sanityShieldLevel * shieldHitCostReductionPerLevel;
    public float LaserCostReductionPercent => laserLevel * laserCostReductionPerLevel;
    public float LaserCooldownReductionSeconds => laserLevel * laserCooldownReductionPerLevel;
    public float SanityRunSpeedBonusPercent => sanityRunLevel * sanityRunSpeedBonusPerLevel * 100f;

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
        if (currency == null || IsMaxLevel(node) || !currency.TrySpend(upgradeCost))
            return false;

        int level = GetLevel(node) + 1;
        SetLevel(node, level);
        OnUpgradeChanged?.Invoke(node, level);
        return true;
    }

    public bool IsMaxLevel(PlayerUpgradeNode node)
    {
        return GetLevel(node) >= maxUpgradeLevel;
    }

    public bool CanUpgrade(PlayerUpgradeNode node, long availableFragments)
    {
        return !IsMaxLevel(node) && availableFragments >= upgradeCost;
    }

    public float GetSanityRechargeMultiplier()
    {
        return 1f + sanityChargeLevel * sanityRechargeBonusPerLevel;
    }

    public float GetSanityDrainPercent(float basePercent)
    {
        return Mathf.Max(0f, basePercent - sanityChargeLevel * sanityDrainReductionPerLevel);
    }

    public float GetShieldSpeedMultiplier(float baseMultiplier)
    {
        return Mathf.Clamp01(baseMultiplier + sanityShieldLevel * shieldSpeedBonusPerLevel);
    }

    public float GetShieldHitCostPercent(float basePercent)
    {
        return Mathf.Max(0f, basePercent - sanityShieldLevel * shieldHitCostReductionPerLevel);
    }

    public float GetLaserCostPercent(float basePercent)
    {
        return Mathf.Max(0f, basePercent - laserLevel * laserCostReductionPerLevel);
    }

    public float GetLaserCooldown(float baseCooldown)
    {
        return Mathf.Max(minimumLaserCooldown, baseCooldown - laserLevel * laserCooldownReductionPerLevel);
    }

    public float GetSanityRunSpeedMultiplier(float baseMultiplier)
    {
        return baseMultiplier * (1f + sanityRunLevel * sanityRunSpeedBonusPerLevel);
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
