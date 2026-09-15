using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ScenePortal : MonoBehaviour
{
    [Header("Destination")]
    [SerializeField] private string targetSceneName;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Vector3 playerSpawnOffset = new(-2f, 0f, 0f);

    [Header("Custom Destination Spawn")]
    [Tooltip("ใช้พิกัดแบบ world-space ของฉากปลายทางแทนตำแหน่ง Portal ในฉากปลายทาง")]
    [SerializeField] private bool useCustomSpawnPoint;
    [SerializeField] private Vector3 destinationSpawnPosition;
    [SerializeField] private Vector3 destinationSpawnEulerAngles;

    private bool hasTriggered;
    private Collider portalCollider;
    private StabilitySystem subscribedStabilitySystem;

    public string TargetSceneName => targetSceneName;

    private void Awake()
    {
        portalCollider = GetComponent<Collider>();
        portalCollider.isTrigger = true;
        PlayerPersistenceManager.EnsurePlayer(
            playerPrefab,
            transform.position + playerSpawnOffset,
            transform.rotation
        );

        RefreshAvailability();
    }

    private void OnEnable()
    {
        SubscribeToStabilitySystem();
    }

    private void Start()
    {
        SubscribeToStabilitySystem();
        RefreshAvailability();
    }

    private void OnDisable()
    {
        UnsubscribeFromStabilitySystem();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered || !other.transform.root.CompareTag("Player"))
            return;

        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogWarning("ScenePortal needs a SceneTransitionManager instance.", this);
            return;
        }

        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogWarning("ScenePortal has no target scene name.", this);
            return;
        }

        if (StabilitySystem.Instance != null && StabilitySystem.Instance.IsWorldBlackedOut(targetSceneName))
            return;

        hasTriggered = true;

        if (useCustomSpawnPoint)
        {
            SceneTransitionManager.Instance.WarpTo(
                targetSceneName,
                destinationSpawnPosition,
                Quaternion.Euler(destinationSpawnEulerAngles)
            );
        }
        else
        {
            SceneTransitionManager.Instance.WarpTo(targetSceneName);
        }
    }

    private void HandleWorldDepleted(string depletedSceneName)
    {
        if (depletedSceneName == targetSceneName)
            RefreshAvailability();
    }

    private void HandleWorldStabilityChanged(string changedSceneName, float current, float max)
    {
        if (changedSceneName == targetSceneName)
            RefreshAvailability();
    }

    private void RefreshAvailability()
    {
        if (portalCollider != null && StabilitySystem.Instance != null)
            portalCollider.enabled = !StabilitySystem.Instance.IsWorldBlackedOut(targetSceneName);
    }

    private void SubscribeToStabilitySystem()
    {
        if (subscribedStabilitySystem != null || StabilitySystem.Instance == null)
            return;

        subscribedStabilitySystem = StabilitySystem.Instance;
        subscribedStabilitySystem.WorldDepleted += HandleWorldDepleted;
        subscribedStabilitySystem.WorldStabilityChanged += HandleWorldStabilityChanged;
    }

    private void UnsubscribeFromStabilitySystem()
    {
        if (subscribedStabilitySystem == null)
            return;

        subscribedStabilitySystem.WorldDepleted -= HandleWorldDepleted;
        subscribedStabilitySystem.WorldStabilityChanged -= HandleWorldStabilityChanged;
        subscribedStabilitySystem = null;
    }
}
