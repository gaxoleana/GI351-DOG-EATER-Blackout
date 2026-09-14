using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAbilities : MonoBehaviour
{
    private const string PlayerActionMapName = "Player";
    private const string RechargeActionName = "Recharge";
    private const string ShieldActionName = "Shield";

    [Header("References")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private PlayerSanity playerSanity;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private GameObject shieldVisual;

    [Header("Shield Settings")]
    [Tooltip("Sanity cost paid each time the player is hit while the shield is raised")]
    [SerializeField, Range(0f, 100f)] private float hitSanityCostPercent = 25f;
    [Tooltip("Movement speed multiplier while the shield is raised (0.5 = 50% slower)")]
    [SerializeField, Range(0f, 1f)] private float shieldSpeedMultiplier = 0.5f;

    [Header("Sanity Run")]
    [SerializeField, Range(1f, 2f)] private float sanityRunSpeedMultiplier = 1.1f;

    [Header("Sanity Drain")]
    [SerializeField, Range(0f, 100f)] private float sanityDrainPercentPerSecond = 3f;

    private InputAction rechargeAction;
    private InputAction shieldAction;
    private PlayerUpgradeSystem upgradeSystem;
    private SpriteRenderer shieldRenderer;
    private bool isRecharging;
    private bool isShielding;

    public bool IsShielding => isShielding;

    private void Awake()
    {
        rechargeAction = InputActionUtils.Find(inputActions, PlayerActionMapName, RechargeActionName);
        shieldAction = InputActionUtils.Find(inputActions, PlayerActionMapName, ShieldActionName);

        playerSanity ??= GetComponent<PlayerSanity>();
        playerController ??= GetComponent<PlayerController>();
        upgradeSystem = GetComponent<PlayerUpgradeSystem>();

        if (shieldVisual != null)
            shieldRenderer = shieldVisual.GetComponentInChildren<SpriteRenderer>();

        SetShieldVisible(false);
    }

    private void OnEnable()
    {
        if (rechargeAction != null)
        {
            rechargeAction.Enable();
            rechargeAction.started += OnRechargeStarted;
            rechargeAction.canceled += OnRechargeCanceled;
        }

        if (shieldAction != null)
        {
            shieldAction.Enable();
            // กด F แต่ละครั้งจะสลับสถานะโล่
            shieldAction.started += OnShieldStarted;
        }
    }

    private void OnDisable()
    {
        if (rechargeAction != null)
        {
            rechargeAction.started -= OnRechargeStarted;
            rechargeAction.canceled -= OnRechargeCanceled;
            rechargeAction.Disable();
        }

        if (shieldAction != null)
        {
            shieldAction.started -= OnShieldStarted;
            shieldAction.Disable();
        }

        isRecharging = false;
        EndShield();
    }

    public void EnableInputActions()
    {
        rechargeAction?.Enable();
        shieldAction?.Enable();
    }

    private void Update()
    {
        if (!PlayerPersistenceManager.IsGameplayActive)
            return;

        upgradeSystem ??= GetComponent<PlayerUpgradeSystem>();

        if (isRecharging && playerSanity != null)
            playerSanity.Charge(Time.deltaTime);
        else if (playerSanity != null)
        {
            float drainPercent = upgradeSystem != null
                ? upgradeSystem.GetSanityDrainPercent(sanityDrainPercentPerSecond)
                : sanityDrainPercentPerSecond;
            playerSanity.ReducePercent(drainPercent, Time.deltaTime);
        }

        if (isShielding && (playerSanity == null || !playerSanity.HasSanity))
            EndShield();

        UpdateMovementSpeed();
    }

    private void OnRechargeStarted(InputAction.CallbackContext ctx)
    {
        isRecharging = true;
    }

    private void OnRechargeCanceled(InputAction.CallbackContext ctx)
    {
        isRecharging = false;
    }

    private void OnShieldStarted(InputAction.CallbackContext ctx)
    {
        if (isShielding)
            EndShield();
        else
            BeginShield();
    }

    private void BeginShield()
    {
        if (isShielding || playerSanity == null || !playerSanity.HasSanity)
            return;

        isShielding = true;
        float speedMultiplier = upgradeSystem != null
            ? upgradeSystem.GetShieldSpeedMultiplier(shieldSpeedMultiplier)
            : shieldSpeedMultiplier;
        playerController?.SetSpeedModifier(speedMultiplier);
        SetShieldVisible(true);
    }

    private void EndShield()
    {
        if (!isShielding)
            return;

        isShielding = false;
        playerController?.SetSpeedModifier(1f);
        SetShieldVisible(false);
    }

    // เรียกจาก PlayerDamageReceiver ตอนโดนตี — โล่จะหัก Sanity แทน HP
    public bool TryConsumeShield()
    {
        if (!IsShielding || playerSanity == null || !playerSanity.HasSanity)
            return false;

        upgradeSystem ??= GetComponent<PlayerUpgradeSystem>();
        float hitCostPercent = upgradeSystem != null
            ? upgradeSystem.GetShieldHitCostPercent(hitSanityCostPercent)
            : hitSanityCostPercent;
        playerSanity.ReducePercent(hitCostPercent, 1f);

        if (!playerSanity.HasSanity)
            EndShield();

        return true;
    }

    private void UpdateMovementSpeed()
    {
        if (playerController == null)
            return;

        float multiplier;
        if (isShielding)
        {
            multiplier = upgradeSystem != null
                ? upgradeSystem.GetShieldSpeedMultiplier(shieldSpeedMultiplier)
                : shieldSpeedMultiplier;
        }
        else if (playerSanity != null && playerSanity.HasSanity)
        {
            multiplier = upgradeSystem != null
                ? upgradeSystem.GetSanityRunSpeedMultiplier(sanityRunSpeedMultiplier)
                : sanityRunSpeedMultiplier;
        }
        else
        {
            multiplier = 1f;
        }

        playerController.SetSpeedModifier(multiplier);
    }

    private void SetShieldVisible(bool isVisible)
    {
        if (shieldVisual != null)
            shieldVisual.SetActive(isVisible);

        if (shieldRenderer != null)
            shieldRenderer.enabled = isVisible;
    }
}