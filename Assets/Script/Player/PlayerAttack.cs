using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    private const string PlayerActionMapName = "Player";
    private const string AttackActionName = "Attack";

    [Header("References")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private PlayerSanity playerSanity;

    [Header("Projectile")]
    [SerializeField] private float projectileSpeed = 18f;
    [SerializeField] private float projectileDamage = 25f;
    [SerializeField] private float projectileLifetime = 2f;
    [SerializeField] private float fireCooldown = 2f;
    [SerializeField, Range(0f, 100f)] private float sanityCostPercent = 15f;

    private InputAction attackAction;
    private PlayerUpgradeSystem upgradeSystem;
    private float nextFireTime;

    private void Awake()
    {
        attackAction = InputActionUtils.Find(inputActions, PlayerActionMapName, AttackActionName);

        if (attackPoint == null)
            attackPoint = transform;

        upgradeSystem = GetComponent<PlayerUpgradeSystem>();
    }

    private void OnEnable()
    {
        if (attackAction == null)
            return;

        attackAction.Enable();
        attackAction.performed += OnAttackPerformed;
    }

    private void OnDisable()
    {
        if (attackAction != null)
        {
            attackAction.performed -= OnAttackPerformed;
            attackAction.Disable();
        }

    }

    public void EnableInputActions()
    {
        attackAction?.Enable();
    }

    private void OnAttackPerformed(InputAction.CallbackContext ctx)
    {
        if (!PlayerPersistenceManager.IsGameplayActive
            || Time.time < nextFireTime
            || projectilePrefab == null
            || attackPoint == null)
            return;

        upgradeSystem ??= GetComponent<PlayerUpgradeSystem>();
        float laserCostPercent = upgradeSystem != null
            ? upgradeSystem.GetLaserCostPercent(sanityCostPercent)
            : sanityCostPercent;

        if (playerSanity == null || !playerSanity.TrySpendPercent(laserCostPercent))
            return;

        GameObject projectileObject = Instantiate(
            projectilePrefab,
            attackPoint.position,
            attackPoint.rotation
        );

        AttackProjectile projectile = projectileObject.GetComponent<AttackProjectile>();
        if (projectile == null)
        {
            Destroy(projectileObject);
            return;
        }

        projectile.Launch(
            attackPoint.right,
            projectileSpeed,
            projectileDamage,
            projectileLifetime,
            gameObject
        );

        float cooldown = upgradeSystem != null
            ? upgradeSystem.GetLaserCooldown(fireCooldown)
            : fireCooldown;
        nextFireTime = Time.time + cooldown;
    }
}
