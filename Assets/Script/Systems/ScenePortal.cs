using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ScenePortal : MonoBehaviour
{
    [Header("Destination")]
    [SerializeField] private string targetSceneName;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Vector3 playerSpawnOffset = new(-2f, 0f, 0f);

    private bool hasTriggered;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        PlayerPersistenceManager.EnsurePlayer(
            playerPrefab,
            transform.position + playerSpawnOffset,
            transform.rotation
        );
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

        hasTriggered = true;
        SceneTransitionManager.Instance.WarpTo(targetSceneName);
    }
}
