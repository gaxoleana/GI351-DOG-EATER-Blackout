using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(EnemyHealth))]
public class DemorgorgonBoss : MonoBehaviour
{
    private enum AttackType { Scratch, Dash, StraightBeam, SweepingBeam, Jump, Poison }

    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform mouthPoint;
    [SerializeField] private GameObject poisonPrefab;
    [SerializeField] private Animator animator;

    [Header("Phase")]
    [SerializeField] private bool enablePhaseTwo;
    [SerializeField, Range(0.05f, 0.95f)] private float phaseTwoHealthPercent = 0.5f;
    [SerializeField] private float attackCooldown = 2f;

    [Header("Movement")]
    [SerializeField] private float movementSpeed = 2.5f;
    [SerializeField] private float movementStopDistance = 1.5f;

    [Header("Phase 1")]
    [SerializeField] private float dashCooldown = 3f;
    [SerializeField] private float scratchRange = 2f;
    [SerializeField] private float scratchDamage = 20f;
    [SerializeField] private float dashDistance = 5f;
    [SerializeField] private float dashDamage = 30f;

    [Header("Phase 2")]
    [SerializeField] private float straightBeamDamage = 40f;
    [SerializeField] private float straightBeamLength = 9f;
    [SerializeField] private float straightBeamWidth = 1.3f;
    [SerializeField] private float sweepingBeamDamage = 35f;
    [SerializeField] private float sweepingBeamRadius = 6f;
    [SerializeField, Range(30f, 180f)] private float sweepingBeamAngle = 100f;
    [SerializeField] private float jumpDamage = 50f;
    [SerializeField] private float jumpRadius = 2.2f;
    [SerializeField] private float poisonDamage = 15f;
    [SerializeField] private float poisonRadius = 1.5f;
    [SerializeField] private float poisonLifetime = 6f;

    [Header("Telegraph")]
    [SerializeField] private float scratchTelegraphDuration = 0.7f;
    [SerializeField] private float dashTelegraphDuration = 0.9f;
    [SerializeField] private float phaseTwoTelegraphDuration = 1.2f;
    [SerializeField] private Color telegraphColor = new(1f, 0f, 0f, 0.8f);

    private Rigidbody rb;
    private EnemyHealth health;
    private IDamageable playerDamageable;
    private SpriteRenderer spriteRenderer;
    private Renderer[] bossRenderers;
    private Collider[] bossColliders;
    private Coroutine attackRoutine;
    private float nextAttackTime;
    private float nextDashTime;

    public bool IsPhaseTwo => health != null && health.MaxHealth > 0f && health.CurrentHealth / health.MaxHealth <= phaseTwoHealthPercent;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        health = GetComponent<EnemyHealth>();
        animator ??= GetComponent<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        bossRenderers = GetComponentsInChildren<Renderer>(true);
        bossColliders = GetComponentsInChildren<Collider>(true);
        ResolvePlayer();
    }

    private void Update()
    {
        ResolvePlayer();
        if (attackRoutine == null && Time.time >= nextAttackTime && playerTransform != null)
            attackRoutine = StartCoroutine(ChooseAndPerformAttack());
    }

    private void FixedUpdate()
    {
        if (rb == null || playerTransform == null || attackRoutine != null)
        {
            StopMoving();
            return;
        }

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
        FacePlayer(direction);
    }

    private IEnumerator ChooseAndPerformAttack()
    {
        nextAttackTime = Time.time + attackCooldown;
        switch (SelectAttack())
        {
            case AttackType.Scratch: yield return Scratch(); break;
            case AttackType.Dash: yield return Dash(); break;
            case AttackType.StraightBeam: yield return StraightBeam(); break;
            case AttackType.SweepingBeam: yield return SweepingBeam(); break;
            case AttackType.Jump: yield return Jump(); break;
            case AttackType.Poison: yield return Poison(); break;
        }
        attackRoutine = null;
    }

    private AttackType SelectAttack()
    {
        if (!enablePhaseTwo || !IsPhaseTwo)
        {
            if (Time.time < nextDashTime)
                return AttackType.Scratch;
            return Random.value < 0.5f ? AttackType.Scratch : AttackType.Dash;
        }

        return (AttackType)Random.Range((int)AttackType.StraightBeam, (int)AttackType.Poison + 1);
    }

    private IEnumerator Scratch()
    {
        FacePlayer(GetDirectionToPlayer());
        BossAttackTelegraph telegraph = BossAttackTelegraph.CreateVerticalCircle(transform.position, GetDirectionToPlayer(), scratchRange, scratchTelegraphDuration, telegraphColor);
        yield return Charge(telegraph, scratchTelegraphDuration);
        if (IsPlayerWithin(scratchRange))
            playerDamageable?.TakeDamage(scratchDamage);
    }

    private IEnumerator Dash()
    {
        nextDashTime = Time.time + Mathf.Max(0f, dashCooldown);
        Vector3 direction = GetDirectionToPlayer();
        BossAttackTelegraph telegraph = BossAttackTelegraph.CreateLine(transform.position, direction, dashDistance, 0.9f, dashTelegraphDuration, telegraphColor);
        yield return Charge(telegraph, dashTelegraphDuration);

        Vector3 start = transform.position;
        Vector3 destination = start + direction * dashDistance;
        float timer = 0f;
        while (timer < 0.25f)
        {
            timer += Time.deltaTime;
            rb.MovePosition(Vector3.Lerp(start, destination, timer / 0.25f));
            yield return null;
        }

        if (IsPlayerWithin(1.3f))
            playerDamageable?.TakeDamage(dashDamage);
    }

    private IEnumerator StraightBeam()
    {
        Vector3 direction = GetDirectionToPlayer();
        Vector3 origin = mouthPoint != null ? mouthPoint.position : transform.position;
        BossAttackTelegraph telegraph = BossAttackTelegraph.CreateLine(origin, direction, straightBeamLength, straightBeamWidth, phaseTwoTelegraphDuration, telegraphColor);
        yield return Charge(telegraph, phaseTwoTelegraphDuration);
        if (IsPlayerInsideLine(origin, direction, straightBeamLength, straightBeamWidth))
            playerDamageable?.TakeDamage(straightBeamDamage);
    }

    private IEnumerator SweepingBeam()
    {
        Vector3 direction = GetDirectionToPlayer();
        BossAttackTelegraph telegraph = BossAttackTelegraph.CreateFan(transform.position, direction, sweepingBeamRadius, sweepingBeamAngle, phaseTwoTelegraphDuration, telegraphColor);
        yield return Charge(telegraph, phaseTwoTelegraphDuration);
        if (IsPlayerInsideFan(direction, sweepingBeamRadius, sweepingBeamAngle))
            playerDamageable?.TakeDamage(sweepingBeamDamage);
    }

    private IEnumerator Jump()
    {
        Vector3 landingPosition = playerTransform.position;
        BossAttackTelegraph telegraph = BossAttackTelegraph.CreateCircle(landingPosition, jumpRadius, phaseTwoTelegraphDuration, telegraphColor);
        yield return Charge(telegraph, phaseTwoTelegraphDuration);
        SetBossVisible(false);
        yield return new WaitForSeconds(0.6f);
        transform.position = landingPosition;
        SetBossVisible(true);
        if (IsPlayerWithin(jumpRadius))
            playerDamageable?.TakeDamage(jumpDamage);
    }

    private IEnumerator Poison()
    {
        Vector3 poisonPosition = playerTransform.position;
        BossAttackTelegraph telegraph = BossAttackTelegraph.CreateCircle(poisonPosition, poisonRadius, phaseTwoTelegraphDuration, telegraphColor);
        yield return Charge(telegraph, phaseTwoTelegraphDuration);
        GameObject poison = poisonPrefab != null ? Instantiate(poisonPrefab, poisonPosition, Quaternion.identity) : CreatePoisonHazard(poisonPosition);
        BossPoisonHazard hazard = poison.GetComponent<BossPoisonHazard>();
        hazard?.Initialize(poisonDamage, poisonLifetime, poisonRadius);
    }

    private IEnumerator Charge(BossAttackTelegraph telegraph, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            telegraph?.SetProgress(timer / duration);
            yield return null;
        }
    }

    private bool IsPlayerWithin(float radius) => playerTransform != null && (playerTransform.position - transform.position).sqrMagnitude <= radius * radius;

    private bool IsPlayerInsideLine(Vector3 origin, Vector3 direction, float length, float width)
    {
        Vector3 offset = playerTransform.position - origin;
        offset.y = 0f;
        float forwardDistance = Vector3.Dot(offset, direction);
        Vector3 lateral = offset - direction * forwardDistance;
        return forwardDistance >= 0f && forwardDistance <= length && lateral.magnitude <= width * 0.5f;
    }

    private bool IsPlayerInsideFan(Vector3 direction, float radius, float angle)
    {
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
            spriteRenderer.flipX = direction.x < 0f;
    }

    private void StopMoving()
    {
        if (rb != null)
            rb.linearVelocity = Vector3.zero;
    }

    private void ResolvePlayer()
    {
        if (playerTransform == null)
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
