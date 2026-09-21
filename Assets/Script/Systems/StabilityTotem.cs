using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class StabilityTotem : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The supplied Area Collider. It is automatically set to Is Trigger.")]
    [SerializeField] private Collider areaCollider;
    [Tooltip("World-space TMP text that shows the charging and interaction prompt.")]
    [SerializeField] private TMP_Text interactionText;

    [Header("Interaction")]
    [SerializeField, Min(0f)] private float chargeDuration = 10f;
    [SerializeField, Range(0f, 100f)] private float minimumRestorePercent = 20f;
    [SerializeField, Range(0f, 100f)] private float maximumRestorePercent = 30f;
    [SerializeField] private string chargingFormat = "Stabilizing {0:0}/{1:0}";
    [SerializeField] private string readyMessage = "Press E to restore Stability";

    private StabilitySystem stabilitySystem;
    private Transform playerRoot;
    private float chargeElapsed;
    private bool isPlayerInside;
    private bool isReady;
    private bool hasBeenClaimed;

    private void Awake()
    {
        areaCollider ??= GetComponent<Collider>();
        if (areaCollider == null)
        {
            Debug.LogError("StabilityTotem requires an Area Collider reference.", this);
            enabled = false;
            return;
        }

        areaCollider.isTrigger = true;
        SetPromptVisible(false);
    }

    private void Update()
    {
        if (hasBeenClaimed || !isPlayerInside)
            return;

        stabilitySystem ??= StabilitySystem.Instance;
        if (stabilitySystem == null)
            return;

        if (!isReady)
        {
            stabilitySystem.SetPassiveDecayPaused(this, true);
            chargeElapsed = Mathf.Min(chargeElapsed + Time.deltaTime, chargeDuration);
            isReady = chargeElapsed >= chargeDuration;

            if (isReady)
                stabilitySystem.SetPassiveDecayPaused(this, false);

            UpdatePrompt();
            return;
        }

        if (Keyboard.current?.eKey.wasPressedThisFrame == true)
            ClaimTotem();
    }

    private void OnTriggerEnter(Collider other)
    {
        Transform root = other.transform.root;
        if (hasBeenClaimed || !root.CompareTag("Player"))
            return;

        playerRoot = root;
        isPlayerInside = true;
        stabilitySystem ??= StabilitySystem.Instance;
        if (!isReady)
            stabilitySystem?.SetPassiveDecayPaused(this, true);

        SetPromptVisible(true);
        UpdatePrompt();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform.root != playerRoot)
            return;

        LeaveArea();
    }

    private void OnDisable()
    {
        stabilitySystem?.SetPassiveDecayPaused(this, false);
    }

    private void LeaveArea()
    {
        isPlayerInside = false;
        playerRoot = null;
        stabilitySystem?.SetPassiveDecayPaused(this, false);

        // Charge progress remains stored until the player explicitly claims the Totem.
        SetPromptVisible(false);
    }

    private void ClaimTotem()
    {
        float minimum = Mathf.Min(minimumRestorePercent, maximumRestorePercent);
        float maximum = Mathf.Max(minimumRestorePercent, maximumRestorePercent);
        float restorePercent = Random.Range(minimum, maximum);

        stabilitySystem.RestoreActiveWorldsPercent(restorePercent);
        hasBeenClaimed = true;
        stabilitySystem.SetPassiveDecayPaused(this, false);
        SetPromptVisible(false);
        Destroy(gameObject);
    }

    private void UpdatePrompt()
    {
        if (interactionText == null)
            return;

        interactionText.text = isReady
            ? readyMessage
            : string.Format(chargingFormat, chargeElapsed, chargeDuration);
    }

    private void SetPromptVisible(bool isVisible)
    {
        if (interactionText != null)
            interactionText.gameObject.SetActive(isVisible);
    }
}
