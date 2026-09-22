using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Put this on a trigger collider in MapHub. The player can press E while inside
/// the area to open or close the Player Upgrade panel.
/// </summary>
public class PlayerUpgradeStation : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The interaction area. It is automatically configured as a trigger.")]
    [SerializeField] private Collider interactionCollider;
    [Tooltip("Optional world-space TMP prompt shown while the player is in range.")]
    [SerializeField] private TMP_Text interactionText;

    [Header("Interaction")]
    [SerializeField] private string interactionMessage = "Press E to upgrade";

    private Transform playerRoot;

    private void Awake()
    {
        interactionCollider ??= GetComponent<Collider>();
        if (interactionCollider == null)
        {
            Debug.LogError("PlayerUpgradeStation requires a Collider reference.", this);
            enabled = false;
            return;
        }

        interactionCollider.isTrigger = true;
        SetPromptVisible(false);
    }

    private void Update()
    {
        if (playerRoot == null || Keyboard.current?.eKey.wasPressedThisFrame != true)
            return;

        PlayerUpgradeUI upgradeUI = PlayerUpgradeUI.Instance;
        if (upgradeUI == null)
            return;

        if (upgradeUI.IsMenuOpen)
            upgradeUI.CloseMenu();
        else
            upgradeUI.OpenMenu();
    }

    private void OnTriggerEnter(Collider other)
    {
        Transform root = other.transform.root;
        if (!root.CompareTag("Player"))
            return;

        playerRoot = root;
        if (interactionText != null)
            interactionText.text = interactionMessage;
        SetPromptVisible(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform.root != playerRoot)
            return;

        playerRoot = null;
        SetPromptVisible(false);
    }

    private void OnDisable()
    {
        playerRoot = null;
        SetPromptVisible(false);
    }

    private void SetPromptVisible(bool isVisible)
    {
        if (interactionText != null)
            interactionText.gameObject.SetActive(isVisible);
    }
}
