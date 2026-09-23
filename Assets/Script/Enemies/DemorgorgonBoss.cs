using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(EnemyHealth))]
public class DemorgorgonBoss : MonoBehaviour
{
    private enum AttackType { Scratch, Dash, StraightBeam, SweepingBeam, Jump, Poison, Roar }

    private static readonly int IsWalkParameter = Animator.StringToHash("isWalk");
    private static readonly int IsAttackParameter = Animator.StringToHash("isAttack");
    private static readonly int RoarTriggerParameter = Animator.StringToHash("Roar");

    private const string IdleStateName = "idle";
    private const string WalkStateName = "walk";
    private const string AttackStateName = "attack";

    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform mouthPoint;
    [SerializeField] private GameObject poisonPrefab;
    [SerializeField] private Animator animator;
    [SerializeField] private bool spriteFacesRight = true;

    [Header("Phase Settings")]
    [SerializeField] private bool enablePhaseTwo = true;
    [SerializeField, Range(0.05f, 0.95f)] private float phaseTwoHealthPercent = 0.5f;
    [SerializeField] private float attackCooldown = 2f;
    [Tooltip("เปอร์เซ็นต์ของ Warning Fill ที่จะหยุดติดตามผู้เล่น")]
    [SerializeField, Range(0.1f, 1f)] private float stopTrackingPercent = 0.7f;

    [Header("Movement & Detection")]
    [SerializeField] private float movementSpeed = 2.5f;
    [SerializeField] private float movementStopDistance = 1.5f;
    [Tooltip("ระยะที่บอสเริ่มมองเห็นและตัดสินใจใช้ทักษะ (ตั้งไว้ไกลๆ เพื่อให้ใช้ Dash พุ่งหาได้)")]
    [SerializeField] private float detectionRange = 15f;

    [Header("Phase 1 - Scratch")]
    [SerializeField] private float scratchRange = 2.5f;
    [SerializeField] private float scratchDamage = 20f;
    [SerializeField] private float scratchTelegraphDuration = 0.7f;

    [Header("Phase 1 - Dash")]
    [SerializeField] private float dashCooldown = 3f;
    [SerializeField] private float dashDistance = 8f;
    [SerializeField] private float dashSpeed = 14f;
    [SerializeField] private float dashAcceleration = 35f;
    [SerializeField] private float dashHitRadius = 1.3f;
    [SerializeField] private float dashDamage = 30f;
    [SerializeField] private float dashTelegraphDuration = 0.9f;

    [Header("Phase Transition - Roar")]
    [SerializeField] private float roarDuration = 1.5f;
    [SerializeField, Range(0f, 100f)] private float targetStabilityPercentOnRoar = 30f;

    [Header("Phase 2 - Straight Beam")]
    [SerializeField] private float straightBeamDamage = 40f;
    [SerializeField] private float straightBeamLength = 12f;
    [SerializeField] private float straightBeamWidth = 1.3f;
    [SerializeField] private float straightBeamTelegraphDuration = 1.2f;

    [Header("Phase 2 - Sweeping Beam")]
    [SerializeField] private float sweepingBeamDamage = 35f;
    [SerializeField] private float sweepingBeamRadius = 6f;
    [SerializeField, Range(30f, 180f)] private float sweepingBeamAngle = 100f;
    [SerializeField] private float sweepingBeamTelegraphDuration = 1.2f;

    [Header("Phase 2 - Jump")]
    [SerializeField] private float jumpDamage = 50f;
    [SerializeField] private float jumpRadius = 2.2f;
    [SerializeField] private float jumpTelegraphDuration = 1.2f;
    [SerializeField] private float jumpVanishDuration = 0.6f;

    [Header("Phase 2 - Poison")]
    [SerializeField] private float poisonDamage = 15f;
    [SerializeField] private float poisonRadius = 1.5f;
    [SerializeField] private float poisonLifetime = 6f;
    [SerializeField] private float poisonTelegraphDuration = 1.2f;

    [Header("Telegraph Color")]
    [SerializeField] private Color telegraphColor = new(1f, 0f, 0f, 0.8f);

    [Header("Telegraph Positioning")]
    [Tooltip("ระยะปรับระดับ Y สำหรับวงเตือนทั่วไป (วงกลม/พัด)")]
    [SerializeField] private float telegraphYOffset = 0.55f;

    [Tooltip("ระยะยกสูงเพิ่มเติมเฉพาะกรอบสี่เหลี่ยม/สายพุ่ง (Dash / StraightBeam) เพื่อกันเมชจมพื้น")]
    [SerializeField] private float lineTelegraphExtraY = 0.05f;

    private Vector3 GetGroundPosition()
    {
        Vector3 pos = transform.position;
        pos.y += telegraphYOffset;
        return pos;
    }

    private Vector3 GetLineGroundPosition()
    {
        Vector3 pos = transform.position;
        pos.y += telegraphYOffset + lineTelegraphExtraY;
        return pos;
    }

    private Vector3 GetGroundPositionOf(Vector3 targetPos)
    {
        targetPos.y = transform.position.y + telegraphYOffset;
        return targetPos;
    }

    private Rigidbody rb;
    private EnemyHealth health;
    private IDamageable playerDamageable;
    private SpriteRenderer spriteRenderer;
    private Renderer[] bossRenderers;
    private Collider[] bossColliders;

    private Coroutine attackRoutine;
    private float nextAttackTime;
    private float nextDashTime;
    private bool isMoving;
    private bool isAttacking;
    private bool isDashing;
    private bool hasRoared;
    private string activeAnimationState;

    public bool IsPhaseTwo => health != null && health.MaxHealth > 0f && (health.CurrentHealth / health.MaxHealth) <= phaseTwoHealthPercent;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        health = GetComponent<EnemyHealth>();
        animator ??= GetComponent<Animator>();
        if (animator != null && animator.layerCount > 0)
            animator.SetLayerWeight(0, 1f);

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        bossRenderers = GetComponentsInChildren<Renderer>(true);
        bossColliders = GetComponentsInChildren<Collider>(true);
        ResolvePlayer();
    }

    private void Update()
    {
        ResolvePlayer();

        if (attackRoutine == null && Time.time >= nextAttackTime && IsPlayerTarget())
        {
            float distanceToPlayer = GetDistanceToPlayer();
            if (distanceToPlayer <= detectionRange)
            {
                AttackType? selectedAttack = SelectAttack(distanceToPlayer);
                if (selectedAttack.HasValue)
                {
                    attackRoutine = StartCoroutine(PerformAttack(selectedAttack.Value));
                }
            }
        }

        UpdateAnimationState();
    }

    private void FixedUpdate()
    {
        if (rb == null || playerTransform == null || (attackRoutine != null && !isDashing))
        {
            StopMoving();
            return;
        }

        if (isDashing)
            return;

        Vector3 offset = playerTransform.position - rb.position;
        offset.y = 0f;
        if (offset.sqrMagnitude <= movementStopDistance * movementStopDistance)
        {
            StopMoving();
            FacePlayer(offset);
            return;
        }

        Vector3 direction = offset.normalized;
        rb.MovePosition(rb.position + direction * movementSpeed * Time.fixedDeltaTime);
        isMoving = true;
        FacePlayer(direction);
    }

    private AttackType? SelectAttack(float distanceToPlayer)
    {
        if (!enablePhaseTwo || !IsPhaseTwo)
        {
            bool isDashReady = Time.time >= nextDashTime;

            if (distanceToPlayer > scratchRange)
            {
                if (isDashReady)
                    return AttackType.Dash;

                return null;
            }

            if (isDashReady)
                return Random.value < 0.5f ? AttackType.Scratch : AttackType.Dash;

            return AttackType.Scratch;
        }

        return (AttackType)Random.Range((int)AttackType.StraightBeam, (int)AttackType.Poison + 1);
    }

    private IEnumerator PerformAttack(AttackType attack)
    {
        nextAttackTime = Time.time + attackCooldown;

        if (enablePhaseTwo && IsPhaseTwo && !hasRoared)
        {
            yield return Roar();
            attackRoutine = null;
            yield break;
        }

        isAttacking = true;
        UpdateAnimationState();

        switch (attack)
        {
            case AttackType.Scratch: yield return Scratch(); break;
            case AttackType.Dash: yield return Dash(); break;
            case AttackType.StraightBeam: yield return StraightBeam(); break;
            case AttackType.SweepingBeam: yield return SweepingBeam(); break;
            case AttackType.Jump: yield return Jump(); break;
            case AttackType.Poison: yield return Poison(); break;
        }

        isAttacking = false;
        UpdateAnimationState();
        attackRoutine = null;
    }

    private IEnumerator Roar()
    {
        hasRoared = true;
        isAttacking = true;
        UpdateAnimationState();

        if (animator != null)
        {
            animator.SetTrigger(RoarTriggerParameter);
        }

        ReduceStabilityToTarget(targetStabilityPercentOnRoar);

        yield return new WaitForSeconds(roarDuration);

        isAttacking = false;
        UpdateAnimationState();
    }

    private IEnumerator Scratch()
    {
        BossAttackTelegraph telegraph = BossAttackTelegraph.CreateCircle(GetGroundPosition(), scratchRange, scratchTelegraphDuration, telegraphColor);

        yield return ChargeTelegraph(
            telegraph,
            scratchTelegraphDuration,
            getTargetPosition: () => GetGroundPosition(),
            getTargetDirection: () => GetDirectionToPlayer(),
            updatePosition: true,
            updateDirection: true
        );

        if (IsPlayerWithin(scratchRange))
            playerDamageable?.TakeDamage(scratchDamage * BlackoutZoneController.GetEnemyDamageMultiplier(this));
    }

    private IEnumerator Dash()
    {
        nextDashTime = Time.time + Mathf.Max(0f, dashCooldown);
        Vector3 dashDirection = GetDirectionToPlayer();

        // 🟢 เปลี่ยนใช้ GetLineGroundPosition() ป้องกันสี่เหลี่ยมจมพื้น
        BossAttackTelegraph telegraph = BossAttackTelegraph.CreateLine(
            GetLineGroundPosition(), dashDirection, dashDistance, dashHitRadius, dashTelegraphDuration, telegraphColor
        );

        yield return ChargeTelegraph(
            telegraph,
            dashTelegraphDuration,
            getTargetPosition: () => GetLineGroundPosition(),
            getTargetDirection: () => GetDirectionToPlayer(),
            updatePosition: true,
            updateDirection: true,
            onLockDir: (lockedDir) => dashDirection = lockedDir
        );

        FacePlayer(dashDirection);

        float travelledDistance = 0f;
        float currentSpeed = movementSpeed;
        bool hasHitPlayer = false;
        isDashing = true;

        while (travelledDistance < dashDistance)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, dashSpeed, dashAcceleration * Time.deltaTime);
            float movement = Mathf.Min(currentSpeed * Time.deltaTime, dashDistance - travelledDistance);
            rb.MovePosition(rb.position + dashDirection * movement);
            travelledDistance += movement;
            isMoving = movement > 0f;

            if (!hasHitPlayer && IsPlayerTarget() && IsPlayerWithin(dashHitRadius))
            {
                playerDamageable?.TakeDamage(dashDamage * BlackoutZoneController.GetEnemyDamageMultiplier(this));
                hasHitPlayer = true;
            }

            yield return null;
        }

        isDashing = false;
        StopMoving();
    }

    private IEnumerator StraightBeam()
    {
        Vector3 beamDirection = GetDirectionToPlayer();
        Vector3 origin = GetLineGroundPosition(); // 🟢 เปลี่ยนใช้ GetLineGroundPosition()

        BossAttackTelegraph telegraph = BossAttackTelegraph.CreateLine(
            origin, beamDirection, straightBeamLength, straightBeamWidth, straightBeamTelegraphDuration, telegraphColor
        );

        yield return ChargeTelegraph(
            telegraph,
            straightBeamTelegraphDuration,
            getTargetPosition: () => GetLineGroundPosition(),
            getTargetDirection: () => GetDirectionToPlayer(),
            updatePosition: true,
            updateDirection: true,
            onLockDir: (lockedDir) => beamDirection = lockedDir
        );

        if (IsPlayerInsideLine(origin, beamDirection, straightBeamLength, straightBeamWidth))
            playerDamageable?.TakeDamage(straightBeamDamage * BlackoutZoneController.GetEnemyDamageMultiplier(this));
    }

    private IEnumerator SweepingBeam()
    {
        Vector3 sweepDirection = GetDirectionToPlayer();
        BossAttackTelegraph telegraph = BossAttackTelegraph.CreateFan(
            GetGroundPosition(), sweepDirection, sweepingBeamRadius, sweepingBeamAngle, sweepingBeamTelegraphDuration, telegraphColor
        );

        yield return ChargeTelegraph(
            telegraph,
            sweepingBeamTelegraphDuration,
            getTargetPosition: () => GetGroundPosition(),
            getTargetDirection: () => GetDirectionToPlayer(),
            updatePosition: true,
            updateDirection: true,
            onLockDir: (lockedDir) => sweepDirection = lockedDir
        );

        if (IsPlayerInsideFan(sweepDirection, sweepingBeamRadius, sweepingBeamAngle))
            playerDamageable?.TakeDamage(sweepingBeamDamage * BlackoutZoneController.GetEnemyDamageMultiplier(this));
    }

    private IEnumerator Jump()
    {
        Vector3 landingPosition = playerTransform != null ? GetGroundPositionOf(playerTransform.position) : GetGroundPosition();

        BossAttackTelegraph telegraph = BossAttackTelegraph.CreateCircle(
            landingPosition, jumpRadius, jumpTelegraphDuration, telegraphColor
        );

        yield return ChargeTelegraph(
            telegraph,
            jumpTelegraphDuration,
            getTargetPosition: () => playerTransform != null ? GetGroundPositionOf(playerTransform.position) : GetGroundPosition(),
            getTargetDirection: () => GetDirectionToPlayer(),
            updatePosition: true,
            updateDirection: false,
            onLockPos: (lockedPos) => landingPosition = lockedPos
        );

        SetBossVisible(false);
        yield return new WaitForSeconds(jumpVanishDuration);

        transform.position = landingPosition - Vector3.up * telegraphYOffset;
        SetBossVisible(true);

        if (IsPlayerWithin(jumpRadius))
            playerDamageable?.TakeDamage(jumpDamage * BlackoutZoneController.GetEnemyDamageMultiplier(this));
    }

    private IEnumerator Poison()
    {
        Vector3 poisonPosition = playerTransform != null ? GetGroundPositionOf(playerTransform.position) : GetGroundPosition();

        BossAttackTelegraph telegraph = BossAttackTelegraph.CreateCircle(
            poisonPosition, poisonRadius, poisonTelegraphDuration, telegraphColor
        );

        yield return ChargeTelegraph(
            telegraph,
            poisonTelegraphDuration,
            getTargetPosition: () => playerTransform != null ? GetGroundPositionOf(playerTransform.position) : GetGroundPosition(),
            getTargetDirection: () => GetDirectionToPlayer(),
            updatePosition: true,
            updateDirection: false,
            onLockPos: (lockedPos) => poisonPosition = lockedPos
        );

        GameObject poison = poisonPrefab != null
            ? Instantiate(poisonPrefab, poisonPosition, Quaternion.identity)
            : CreatePoisonHazard(poisonPosition);

        BossPoisonHazard hazard = poison.GetComponent<BossPoisonHazard>();
        hazard?.Initialize(poisonDamage, poisonLifetime, poisonRadius);
    }

    private IEnumerator ChargeTelegraph(
        BossAttackTelegraph telegraph,
        float duration,
        System.Func<Vector3> getTargetPosition,
        System.Func<Vector3> getTargetDirection,
        bool updatePosition,
        bool updateDirection,
        System.Action<Vector3> onLockDir = null,
        System.Action<Vector3> onLockPos = null)
    {
        float timer = 0f;
        bool isLocked = false;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / duration);

            if (!isLocked && progress >= stopTrackingPercent)
            {
                isLocked = true;
                if (getTargetDirection != null) onLockDir?.Invoke(getTargetDirection());
                if (getTargetPosition != null) onLockPos?.Invoke(getTargetPosition());
                telegraph?.FreezeRotation();
            }

            if (!isLocked)
            {
                Vector3 currentPos = updatePosition && getTargetPosition != null ? getTargetPosition() : GetGroundPosition();
                Vector3 currentDir = updateDirection && getTargetDirection != null ? getTargetDirection() : GetDirectionToPlayer();

                telegraph?.UpdatePositionAndDirection(currentPos, currentDir);
                if (updateDirection)
                {
                    FacePlayer(currentDir);
                }
            }

            telegraph?.SetProgress(progress);
            yield return null;
        }
    }

    private void ReduceStabilityToTarget(float targetPercent)
    {
        float ratio = Mathf.Clamp01(targetPercent * 0.01f);

        WorldStability worldStability = FindAnyObjectByType<WorldStability>();
        if (worldStability != null)
        {
            float max = worldStability.MaxStability;
            if (worldStability.CurrentStability > max * ratio)
            {
                worldStability.SetStability(max * ratio);
            }
        }
        else if (StabilitySystem.Instance != null)
        {
            string sceneName = gameObject.scene.name;
            if (StabilitySystem.Instance.TryGetWorldStability(sceneName, out float current, out float max))
            {
                if (current > max * ratio)
                {
                    StabilitySystem.Instance.ChangeWorldStability(sceneName, -(current - max * ratio));
                }
            }
        }
    }

    private float GetDistanceToPlayer()
    {
        if (playerTransform == null) return float.MaxValue;
        Vector3 offset = playerTransform.position - transform.position;
        offset.y = 0f;
        return offset.magnitude;
    }

    private bool IsPlayerWithin(float radius)
    {
        if (playerTransform == null)
            return false;

        Vector3 offset = playerTransform.position - transform.position;
        offset.y = 0f;
        return offset.sqrMagnitude <= radius * radius;
    }

    private bool IsPlayerTarget()
    {
        return playerTransform != null && playerTransform.CompareTag("Player");
    }

    private bool IsPlayerInsideLine(Vector3 origin, Vector3 direction, float length, float width)
    {
        if (playerTransform == null) return false;
        Vector3 offset = playerTransform.position - origin;
        offset.y = 0f;
        float forwardDistance = Vector3.Dot(offset, direction);
        Vector3 lateral = offset - direction * forwardDistance;
        return forwardDistance >= 0f && forwardDistance <= length && lateral.magnitude <= width * 0.5f;
    }

    private bool IsPlayerInsideFan(Vector3 direction, float radius, float angle)
    {
        if (playerTransform == null) return false;
        Vector3 offset = playerTransform.position - transform.position;
        offset.y = 0f;
        return offset.magnitude <= radius && Vector3.Angle(direction, offset) <= angle * 0.5f;
    }

    private Vector3 GetDirectionToPlayer()
    {
        Vector3 direction = playerTransform != null ? playerTransform.position - transform.position : transform.right;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.right;
    }

    private void FacePlayer(Vector3 direction)
    {
        if (spriteRenderer != null && Mathf.Abs(direction.x) > 0.001f)
        {
            bool playerIsToTheLeft = direction.x < 0f;
            spriteRenderer.flipX = spriteFacesRight ? playerIsToTheLeft : !playerIsToTheLeft;
        }
    }

    private void StopMoving()
    {
        isMoving = false;
        if (rb != null)
            rb.linearVelocity = Vector3.zero;
    }

    private void UpdateAnimationState()
    {
        if (animator == null)
            return;

        animator.SetBool(IsWalkParameter, isMoving && !isAttacking);
        animator.SetBool(IsAttackParameter, isAttacking);

        string desiredState = isAttacking
            ? AttackStateName
            : isMoving
                ? WalkStateName
                : IdleStateName;

        if (desiredState == activeAnimationState)
            return;

        animator.Play(desiredState, 0, 0f);
        activeAnimationState = desiredState;
    }

    private void ResolvePlayer()
    {
        if (playerTransform == null || !playerTransform.CompareTag("Player"))
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                playerTransform = playerObject.transform;
        }

        if (playerDamageable == null && playerTransform != null)
        {
            playerDamageable = playerTransform.GetComponent<PlayerDamageReceiver>();
            playerDamageable ??= playerTransform.GetComponent<PlayerHealth>();
        }
    }

    private GameObject CreatePoisonHazard(Vector3 position)
    {
        GameObject poison = new("Demorgorgon Poison");
        poison.transform.position = position;
        poison.AddComponent<SphereCollider>();
        poison.AddComponent<BossPoisonHazard>();
        return poison;
    }

    private void SetBossVisible(bool isVisible)
    {
        foreach (Renderer bossRenderer in bossRenderers)
            if (bossRenderer != null) bossRenderer.enabled = isVisible;
        foreach (Collider bossCollider in bossColliders)
            if (bossCollider != null) bossCollider.enabled = isVisible;
    }
}