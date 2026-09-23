using UnityEngine;

public class PlayerFootstepAudio : MonoBehaviour
{
    [Header("Audio References")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] footstepClips;

    [Header("Step Timing")]
    [Tooltip("ระยะเวลาระหว่างก้าวขณะเดินปกติ (ตั้งไว้ที่ 1.5 วินาทีต่อก้าว)")]
    [SerializeField] private float baseStepInterval = 1.5f;
    [Tooltip("ระยะห่างขั้นต่ำสุดระหว่างก้าว")]
    [SerializeField] private float minStepInterval = 0.5f;
    [Tooltip("ความเร็วอ้างอิงขณะเดินปกติ")]
    [SerializeField] private float baseWalkSpeed = 2.5f;
    [SerializeField] private float minSpeedThreshold = 0.2f;

    private Vector3 lastPosition;
    private float stepTimer;
    private float currentSpeed;
    private bool isWalking;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        lastPosition = transform.position;
    }

    private void Update()
    {
        // คำนวณความเร็วและกรองแรงกระตุก
        float rawSpeed = Vector3.Distance(transform.position, lastPosition) / Time.deltaTime;
        lastPosition = transform.position;

        currentSpeed = Mathf.Lerp(currentSpeed, rawSpeed, Time.deltaTime * 10f);

        if (currentSpeed > minSpeedThreshold)
        {
            // เล่นเสียงก้าวแรกทันทีเมื่อเริ่มออกตัวเดิน
            if (!isWalking)
            {
                isWalking = true;
                PlayFootstepSound();
                stepTimer = 0f;
                return;
            }

            float speedRatio = Mathf.Max(0.5f, currentSpeed / baseWalkSpeed);
            float currentInterval = Mathf.Max(minStepInterval, baseStepInterval / speedRatio);

            stepTimer += Time.deltaTime;

            if (stepTimer >= currentInterval)
            {
                PlayFootstepSound();
                stepTimer = 0f;
            }
        }
        else
        {
            isWalking = false;
            stepTimer = 0f;
        }
    }

    private void PlayFootstepSound()
    {
        if (footstepClips == null || footstepClips.Length == 0 || audioSource == null) return;

        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
        if (clip == null) return;

        audioSource.pitch = Random.Range(0.95f, 1.05f);
        audioSource.PlayOneShot(clip);
    }
}