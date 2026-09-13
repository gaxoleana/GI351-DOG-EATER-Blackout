using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ScenePortal : MonoBehaviour
{
    [Header("Destination")]
    [SerializeField] private string targetSceneName;

    private bool hasTriggered;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
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
