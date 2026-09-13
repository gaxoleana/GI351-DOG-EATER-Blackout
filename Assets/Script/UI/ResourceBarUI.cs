using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResourceBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MonoBehaviour resourceSource;
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text valueText;

    [Header("Display")]
    [SerializeField] private string label = "RESOURCE";

    private IResourceStat resourceStat;

    private void Awake()
    {
        resourceStat = resourceSource as IResourceStat;

        if (resourceSource != null && resourceStat == null)
            Debug.LogWarning($"{resourceSource.name} must implement IResourceStat.", resourceSource);
    }

    private void OnEnable()
    {
        if (resourceStat != null)
            resourceStat.OnChanged += HandleResourceChanged;
    }

    private void Start()
    {
        RefreshDisplay();
    }

    private void OnDisable()
    {
        if (resourceStat != null)
            resourceStat.OnChanged -= HandleResourceChanged;
    }

    private void HandleResourceChanged(float current, float max)
    {
        UpdateDisplay(current, max);
    }

    private void RefreshDisplay()
    {
        if (resourceStat != null)
            UpdateDisplay(resourceStat.Current, resourceStat.Max);
    }

    private void UpdateDisplay(float current, float max)
    {
        float percentage = max > 0f ? Mathf.Clamp01(current / max) : 0f;

        if (fillImage != null)
            fillImage.fillAmount = percentage;

        if (valueText != null)
            valueText.text = $"{label} {Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
    }
}
