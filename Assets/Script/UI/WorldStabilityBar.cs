using UnityEngine;

public class WorldStabilityBar : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Portal whose destination world this bar represents. Leave empty when this component is on the Portal or one of its children.")]
    [SerializeField] private ScenePortal portal;
    [SerializeField] private Transform fillTransform;
    [SerializeField] private GameObject barRoot;

    [Header("Display")]
    [SerializeField] private bool hideWhenFull;

    private float fullScaleX;
    private StabilitySystem stabilitySystem;

    private void Awake()
    {
        portal ??= GetComponentInParent<ScenePortal>();

        if (fillTransform != null)
            fullScaleX = fillTransform.localScale.x;
    }

    private void OnEnable()
    {
        SubscribeToStabilitySystem();
    }

    private void OnDisable()
    {
        UnsubscribeFromStabilitySystem();
    }

    private void Start()
    {
        portal ??= GetComponentInParent<ScenePortal>();
        SubscribeToStabilitySystem();
        RefreshDisplay();
    }

    private void Update()
    {
        if (stabilitySystem == null)
        {
            SubscribeToStabilitySystem();
            RefreshDisplay();
        }
    }

    private void SubscribeToStabilitySystem()
    {
        if (stabilitySystem != null)
            return;

        stabilitySystem = StabilitySystem.Instance;
        if (stabilitySystem != null)
        {
            stabilitySystem.WorldStabilityChanged += HandleStabilityChanged;
            RefreshDisplay();
        }
    }

    private void UnsubscribeFromStabilitySystem()
    {
        if (stabilitySystem == null)
            return;

        stabilitySystem.WorldStabilityChanged -= HandleStabilityChanged;
        stabilitySystem = null;
    }

    private void RefreshDisplay()
    {
        if (portal == null || stabilitySystem == null)
            return;

        if (stabilitySystem.TryGetWorldStability(portal.TargetSceneName, out float current, out float max))
            UpdateDisplay(current, max);
    }

    private void HandleStabilityChanged(string sceneName, float current, float max)
    {
        if (portal != null && sceneName == portal.TargetSceneName)
            UpdateDisplay(current, max);
    }

    private void UpdateDisplay(float current, float max)
    {
        float percentage = max > 0f ? Mathf.Clamp01(current / max) : 0f;

        if (fillTransform != null)
        {
            Vector3 scale = fillTransform.localScale;
            scale.x = fullScaleX * percentage;
            fillTransform.localScale = scale;
        }

        if (hideWhenFull && barRoot != null)
            barRoot.SetActive(percentage < 1f);
    }
}
