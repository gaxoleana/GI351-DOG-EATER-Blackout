using UnityEngine;

public class EnemyHealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private Transform fillTransform;
    [SerializeField] private GameObject barRoot;

    [Header("Display")]
    [SerializeField] private bool hideWhenFull = true;

    private float fullScaleX;

    private void Awake()
    {
        if (fillTransform != null)
            fullScaleX = fillTransform.localScale.x;
    }

    private void OnEnable()
    {
        if (enemyHealth != null)
            enemyHealth.OnHealthChanged += HandleHealthChanged;
    }

    private void OnDisable()
    {
        if (enemyHealth != null)
            enemyHealth.OnHealthChanged -= HandleHealthChanged;
    }

    private void Start()
    {
        if (enemyHealth != null)
            HandleHealthChanged(enemyHealth.CurrentHealth, enemyHealth.MaxHealth);
    }

    private void HandleHealthChanged(float current, float max)
    {
        if (fillTransform == null)
            return;

        float healthPercentage = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        UpdateFillScale(healthPercentage);
        UpdateVisibility(healthPercentage);
    }

    private void UpdateFillScale(float healthPercentage)
    {
        // This is a world-space SpriteRenderer bar, not a Canvas Image, so it uses local scale.
        Vector3 scale = fillTransform.localScale;
        scale.x = fullScaleX * healthPercentage;
        fillTransform.localScale = scale;
    }

    private void UpdateVisibility(float healthPercentage)
    {
        if (hideWhenFull && barRoot != null)
            barRoot.SetActive(healthPercentage < 1f);
    }
}