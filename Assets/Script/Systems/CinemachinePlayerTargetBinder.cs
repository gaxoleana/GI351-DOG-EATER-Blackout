using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CinemachinePlayerTargetBinder : MonoBehaviour
{
    private static CinemachinePlayerTargetBinder instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateInstance()
    {
        if (instance != null)
            return;

        GameObject binderObject = new("CinemachinePlayerTargetBinder");
        instance = binderObject.AddComponent<CinemachinePlayerTargetBinder>();
        DontDestroyOnLoad(binderObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Start()
    {
        BindPlayerTarget();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindPlayerTarget();
        StartCoroutine(BindPlayerTargetNextFrame());
    }

    private IEnumerator BindPlayerTargetNextFrame()
    {
        yield return null;
        BindPlayerTarget();
    }

    private static void BindPlayerTarget()
    {
        GameObject player = PlayerPersistenceManager.PersistentPlayer;
        player ??= GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogWarning("CinemachinePlayerTargetBinder could not find a Player object.");
            return;
        }

        CinemachineCamera[] cameras = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
        foreach (CinemachineCamera camera in cameras)
        {
            CameraTarget target = camera.Target;
            target.TrackingTarget = player.transform;
            camera.Target = target;
        }
    }
}
