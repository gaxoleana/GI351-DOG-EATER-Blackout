using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SanityUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerSanity playerSanity;
    [SerializeField] private Image sanityFillImage;
    [SerializeField] private TMP_Text sanityText;

    private void OnEnable()
    {
        if (playerSanity != null)
            playerSanity.OnSanityChanged += HandleSanityChanged;
    }

    private void OnDisable()
    {
        if (playerSanity != null)
            playerSanity.OnSanityChanged -= HandleSanityChanged;
    }

    private void Start()
    {
        if (playerSanity != null)
            HandleSanityChanged(playerSanity.CurrentSanity, playerSanity.MaxSanity);
    }

    private void HandleSanityChanged(float current, float max)
    {
        float percentage = max > 0f ? Mathf.Clamp01(current / max) : 0f;

        if (sanityFillImage != null)
            sanityFillImage.fillAmount = percentage;

        if (sanityText != null)
            sanityText.text = $"SANITY {Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
    }
}
