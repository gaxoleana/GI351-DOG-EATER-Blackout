using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAbilities : MonoBehaviour
{
    private const string PlayerActionMapName = "Player";
    private const string RechargeActionName = "Recharge"; // ตั้งค่าปุ่ม G ใน Input Action
    private const string ShieldActionName = "Shield";     // ตั้งค่าปุ่ม F ใน Input Action

    [Header("References")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private PlayerSanity playerSanity;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private GameObject shieldVisual;

    [Header("Sanity Charge Audio")]
    [SerializeField] private AudioSource chargeAudioSource;
    [SerializeField] private AudioClip chargeAudioClip;
    [Tooltip("ความแหลมเสียงเริ่มต้นตอนเริ่มกดชาร์จ")]
    [SerializeField] private float minChargePitch = 0.8f;
    [Tooltip("ความแหลมเสียงสูงสุดขณะชาร์จ")]
    [SerializeField] private float maxChargePitch = 1.75f;
    [Tooltip("ความเร็วในการไต่ระดับ Pitch สูงขึ้น")]
    [SerializeField] private float pitchRiseSpeed = 0.6f;

    [Header("Shield Settings")]
    [SerializeField, Range(0f, 100f)] private float hitSanityCostPercent = 25f;
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
            shieldAction.started += OnShieldStarted;
            shieldAction.performed += OnShieldStarted;
            shieldAction.canceled += OnShieldCanceled;
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
            shieldAction.performed -= OnShieldStarted;
            shieldAction.canceled -= OnShieldCanceled;
            shieldAction.Disable();
        }

        StopRechargeAudio();
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
        {
            if (isRecharging) StopRechargeAudio();
            return;
        }

        upgradeSystem ??= GetComponent<PlayerUpgradeSystem>();

        if (isRecharging && playerSanity != null)
        {
            playerSanity.Charge(Time.deltaTime);
            UpdateChargeAudio();
        }
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

    // --- ชาร์จ Sanity (ปุ่ม G) ---
    private void OnRechargeStarted(InputAction.CallbackContext ctx)
    {
        isRecharging = true;
        playerController?.SetSprintLocked(true);
        playerController?.SetRechargeSlowdown(true);

        if (chargeAudioSource != null)
        {
            if (chargeAudioClip != null)
            {
                chargeAudioSource.clip = chargeAudioClip;
                chargeAudioSource.loop = true;
            }

            chargeAudioSource.pitch = minChargePitch;
            if (!chargeAudioSource.isPlaying)
                chargeAudioSource.Play();
        }
    }

    private void OnRechargeCanceled(InputAction.CallbackContext ctx)
    {
        isRecharging = false;
        playerController?.SetSprintLocked(false);
        playerController?.SetRechargeSlowdown(false);

        StopRechargeAudio();
    }

    private void UpdateChargeAudio()
    {
        if (chargeAudioSource == null || !chargeAudioSource.isPlaying) return;

        chargeAudioSource.pitch = Mathf.MoveTowards(chargeAudioSource.pitch, maxChargePitch, pitchRiseSpeed * Time.deltaTime);
    }

    private void StopRechargeAudio()
    {
        if (chargeAudioSource != null && chargeAudioSource.isPlaying)
        {
            chargeAudioSource.Stop();
        }
    }

    // --- กางโล่ (ปุ่ม F) ---
    private void OnShieldStarted(InputAction.CallbackContext ctx) => BeginShield();
    private void OnShieldCanceled(InputAction.CallbackContext ctx) => EndShield();

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