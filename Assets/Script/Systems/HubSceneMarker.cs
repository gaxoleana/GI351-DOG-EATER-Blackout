using UnityEngine;
using UnityEngine.SceneManagement;

public class HubSceneMarker : MonoBehaviour
{
    [Tooltip("When enabled, this scene is treated as a Hub: gameplay actions are disabled and the player returns to the hub spawn.")]
    [SerializeField] private bool isHubScene = true;

    public static bool IsHubScene(Scene scene)
    {
        HubSceneMarker[] markers = FindObjectsByType<HubSceneMarker>(FindObjectsInactive.Include);

        foreach (HubSceneMarker marker in markers)
        {
            if (marker.isHubScene && marker.gameObject.scene == scene)
                return true;
        }

        return false;
    }
}
