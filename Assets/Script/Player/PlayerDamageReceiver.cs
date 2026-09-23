using UnityEngine;

public class PlayerDamageReceiver : MonoBehaviour, IDamageable
{
    private const string IsHurtParameter = "isHurt";

    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerAbilities playerAbilities;
    [SerializeField, Min(1)] private int hurtFlashFrameDuration = 10;

    [Header("Invincibility Settings")]
    [Tooltip("ระยะเวลาอมตะชั่วคราวหลังโดนตี (วินาที) เพื่อกันการโดนดาเมจซ้ำถี่เกินไป เช่น ในวงพิษ")]
    [SerializeField] private float invincibilityDuration = 0.2f;

    private Animator animator;
    private HurtFlashTimer hurtFlash;
    private float lastDamageTime = -999f;

    private void Awake()
    {
        // ใช้ GetComponentInParent สำรองไว้ กรณีสคริปต์นี้ติดอยู่บน Hitbox ย่อย
        playerHealth ??= GetComponentInParent<PlayerHealth>();
        playerAbilities ??= GetComponentInParent<PlayerAbilities>();

        animator = GetComponent<Animator>()
                ?? GetComponentInChildren<Animator>()
                ?? GetComponentInParent<Animator>();

        if (animator != null)
        {
            hurtFlash = new HurtFlashTimer(animator, IsHurtParameter, hurtFlashFrameDuration);
        }
    }

    private void LateUpdate()
    {
        hurtFlash?.Tick();
    }

    public void TakeDamage(float amount)
    {
        // 1. เช็คว่าเกมเล่นอยู่ไหม และดาเมจต้องมากกว่า 0
        if (amount <= 0f || !PlayerPersistenceManager.IsGameplayActive)
            return;

        // 2. เช็คเวลาอมตะชั่วคราว (i-frames) ป้องกันการถูกดาเมจซ้ำในเฟรมติดๆ กัน
        if (Time.time < lastDamageTime + invincibilityDuration)
            return;

        // 3. เช็คโล่ป้องกันก่อน! (ถ้ามีโล่รับดาเมจ จะจบฟังก์ชันตรงนี้ ไม่แสดงท่าทางเจ็บ)
        if (playerAbilities != null && playerAbilities.TryConsumeShield())
            return;

        // 4. บันทึกเวลาที่โดนดาเมจล่าสุด
        lastDamageTime = Time.time;

        // 5. แสดงอนิเมชันเจ็บ/กระพริบ
        hurtFlash?.Trigger();

        // 6. ลดเลือดผู้เล่น
        playerHealth?.TakeDamage(amount);
    }
}