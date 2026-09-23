using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    private const string PlayerActionMapName = "Player";
    private const string AttackActionName = "Attack";
    private const string SecondaryAttackActionName = "SecondaryAttack";

    [Header("References")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private PlayerSanity playerSanity;
    [SerializeField] private Camera mainCamera;

    [Header("Primary Attack (Left Click)")]
    [SerializeField] private float projectileSpeed = 18f;
    [SerializeField] private float projectileDamage = 25f;
    [SerializeField] private float projectileLifetime = 2f;
    [SerializeField] private float fireCooldown = 2f;
    [SerializeField, Range(0f, 100f)] private float sanityCostPercent = 15f;

    [Header("Snap Attack (Right Click)")]
    [Tooltip("เปอร์เซ็นต์ Sanity ที่ใช้ในการกด Snap คลิกขวา")]
    [SerializeField, Range(0f, 100f)] private float snapSanityCostPercent = 25f;
    [Tooltip("ตัวคูณดาเมจคลิกขวา (1.5 = ดาเมจแรงกว่าคลิกซ้าย 1.5 เท่า)")]
    [SerializeField] private float snapDamageMultiplier = 1.5f;
    [Tooltip("ความสูงเหนือหัวศัตรูที่จะเสก Projectile ตกลงมา")]
    [SerializeField] private float snapSpawnHeightOffset = 3f;
    [Tooltip("รัศมีช่วยเหลือค้นหาศัตรูรอบๆ จุดเมาส์")]
    [SerializeField] private float snapSearchRadius = 3f;

    private InputAction attackAction;
    private InputAction secondaryAttackAction;
    private PlayerUpgradeSystem upgradeSystem;
    private float nextFireTime;

    private void Awake()
    {
        attackAction = InputActionUtils.Find(inputActions, PlayerActionMapName, AttackActionName);
        secondaryAttackAction = InputActionUtils.Find(inputActions, PlayerActionMapName, SecondaryAttackActionName);

        if (attackPoint == null)
            attackPoint = transform;

        if (mainCamera == null)
            mainCamera = Camera.main;

        upgradeSystem = GetComponent<PlayerUpgradeSystem>();
    }

    private void OnEnable()
    {
        if (attackAction != null)
        {
            attackAction.Enable();
            attackAction.performed += OnAttackPerformed;
        }

        if (secondaryAttackAction != null)
        {
            secondaryAttackAction.Enable();
            secondaryAttackAction.performed += OnSecondaryAttackPerformed;
        }
    }

    private void OnDisable()
    {
        if (attackAction != null)
        {
            attackAction.performed -= OnAttackPerformed;
            attackAction.Disable();
        }

        if (secondaryAttackAction != null)
        {
            secondaryAttackAction.performed -= OnSecondaryAttackPerformed;
            secondaryAttackAction.Disable();
        }
    }

    public void EnableInputActions()
    {
        attackAction?.Enable();
        secondaryAttackAction?.Enable();
    }

    private void Update()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        // Fallback: รองรับการกดคลิกขวาโดยตรง
        if (secondaryAttackAction == null && Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            PerformSnapAttack();
        }
    }

    private void OnAttackPerformed(InputAction.CallbackContext ctx)
    {
        PerformPrimaryAttack();
    }

    private void OnSecondaryAttackPerformed(InputAction.CallbackContext ctx)
    {
        PerformSnapAttack();
    }

    private void PerformPrimaryAttack()
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

    private void PerformSnapAttack()
    {
        if (!PlayerPersistenceManager.IsGameplayActive
            || Time.time < nextFireTime
            || projectilePrefab == null)
            return;

        if (!TryGetEnemyUnderMouse(out Transform enemyTransform, out Vector3 spawnPosition))
            return;

        upgradeSystem ??= GetComponent<PlayerUpgradeSystem>();
        float snapCostPercent = upgradeSystem != null
            ? upgradeSystem.GetLaserCostPercent(snapSanityCostPercent)
            : snapSanityCostPercent;

        if (playerSanity == null || !playerSanity.TrySpendPercent(snapCostPercent))
            return;

        // 1. คำนวณเวกเตอร์ทิศทางจากจุดเสกพุ่งตรงลงหาตัวศัตรู
        Vector3 enemyCenter = enemyTransform.position + Vector3.up * 0.5f;
        Vector3 launchDirection = (enemyCenter - spawnPosition).normalized;
        if (launchDirection == Vector3.zero) launchDirection = Vector3.down;

        // 2. หมุนแกน transform.right ของตัวเลเซอร์ให้ชี้ไปตามทิศทางพุ่งลงหาศัตรู
        Quaternion spawnRotation = Quaternion.FromToRotation(Vector3.right, launchDirection);

        // 3. เสกกระสุนเลเซอร์
        GameObject projectileObject = Instantiate(
            projectilePrefab,
            spawnPosition,
            spawnRotation
        );

        AttackProjectile projectile = projectileObject.GetComponent<AttackProjectile>();
        if (projectile == null)
        {
            Destroy(projectileObject);
            return;
        }

        // 4. ส่งเวกเตอร์ทิศทางพุ่งลง และเพิ่มดาเมจ 1.5 เท่า
        float finalDamage = projectileDamage * snapDamageMultiplier;
        projectile.Launch(
            launchDirection,
            projectileSpeed,
            finalDamage,
            projectileLifetime,
            gameObject
        );

        float cooldown = upgradeSystem != null
            ? upgradeSystem.GetLaserCooldown(fireCooldown)
            : fireCooldown;
        nextFireTime = Time.time + cooldown;
    }

    private bool TryGetEnemyUnderMouse(out Transform enemyTransform, out Vector3 spawnPosition)
    {
        enemyTransform = null;
        spawnPosition = Vector3.zero;

        if (mainCamera == null || Mouse.current == null)
            return false;

        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
        float closestDist = float.MaxValue;
        Transform bestEnemy = null;
        Collider enemyCollider = null;

        foreach (var hit in hits)
        {
            if (hit.collider.isTrigger) continue;

            if (IsEnemy(hit.collider.gameObject))
            {
                if (hit.distance < closestDist)
                {
                    closestDist = hit.distance;
                    bestEnemy = hit.collider.transform;
                    enemyCollider = hit.collider;
                }
            }
        }

        if (bestEnemy == null)
        {
            Plane groundPlane = new Plane(Vector3.up, transform.position);
            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 mouseGroundPos = ray.GetPoint(enter);
                Collider[] nearby = Physics.OverlapSphere(mouseGroundPos, snapSearchRadius);
                float minRadiusDist = float.MaxValue;

                foreach (var col in nearby)
                {
                    if (IsEnemy(col.gameObject))
                    {
                        float dist = Vector3.Distance(mouseGroundPos, col.transform.position);
                        if (dist < minRadiusDist)
                        {
                            minRadiusDist = dist;
                            bestEnemy = col.transform;
                            enemyCollider = col;
                        }
                    }
                }
            }
        }

        if (bestEnemy != null)
        {
            enemyTransform = bestEnemy;
            float headY = enemyCollider != null ? enemyCollider.bounds.max.y : bestEnemy.position.y + 1.5f;
            spawnPosition = new Vector3(bestEnemy.position.x, headY + snapSpawnHeightOffset, bestEnemy.position.z);
            return true;
        }

        return false;
    }

    private bool IsEnemy(GameObject go)
    {
        if (go.CompareTag("Enemy")) return true;
        if (go.GetComponentInParent<EnemyHealth>() != null) return true;
        if (go.GetComponentInParent<IDamageable>() != null && !go.CompareTag("Player")) return true;
        return false;
    }
}