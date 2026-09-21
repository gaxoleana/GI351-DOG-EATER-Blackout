using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerPersistenceManager : MonoBehaviour
{
    private static PlayerPersistenceManager instance;
    private GameObject persistentPlayer;
    private GameObject playerPrefab;
    private ulong lastEnsuredSceneHandle = ulong.MaxValue;
    private bool hasRespawnPoint;
    private string respawnSceneName;
    private Vector3 respawnPosition;
    private Quaternion respawnRotation;

    public static bool IsGameplayActive { get; private set; }
    public static GameObject PersistentPlayer => instance != null ? instance.persistentPlayer : null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateInstance()
    {
        if (instance != null)
            return;

        GameObject managerObject = new("PlayerPersistenceManager");
        instance = managerObject.AddComponent<PlayerPersistenceManager>();
        DontDestroyOnLoad(managerObject);
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
        HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    public static void EnsurePlayer(GameObject prefab, Vector3 spawnPosition, Quaternion spawnRotation)
    {
        if (instance == null)
            CreateInstance();

        Scene activeScene = SceneManager.GetActiveScene();
        ulong sceneHandle = activeScene.handle.GetRawData();
        bool isFirstEnsureInScene = instance.lastEnsuredSceneHandle != sceneHandle;
        bool isHubScene = HubSceneMarker.IsHubScene(activeScene);

        instance.playerPrefab ??= prefab;

        if (instance.persistentPlayer == null)
        {
            GameObject scenePlayer = GameObject.FindGameObjectWithTag("Player");
            if (scenePlayer != null)
            {
                instance.persistentPlayer = scenePlayer;
            }
            else if (instance.playerPrefab != null)
            {
                instance.persistentPlayer = Instantiate(
                    instance.playerPrefab,
                    isHubScene ? Vector3.zero : spawnPosition,
                    isHubScene ? Quaternion.identity : spawnRotation
                );
            }
            else
            {
                Debug.LogWarning("PlayerPersistenceManager has no Player prefab and could not find a Player in the scene.");
                return;
            }

            instance.persistentPlayer.name = "PersistentPlayer";
            DontDestroyOnLoad(instance.persistentPlayer);
        }
        else if (isFirstEnsureInScene)
        {
            instance.MovePersistentPlayer(
                isHubScene ? Vector3.zero : spawnPosition,
                isHubScene ? Quaternion.identity : spawnRotation,
                !isHubScene
            );
        }

        instance.EnsureFragmentCurrency();
        instance.EnsureUpgradeSystem();
        instance.lastEnsuredSceneHandle = sceneHandle;
        instance.SetGameplayState(activeScene);
    }

    private void EnsureFragmentCurrency()
    {
        if (persistentPlayer != null && persistentPlayer.GetComponent<FragmentCurrency>() == null)
            persistentPlayer.AddComponent<FragmentCurrency>();
    }

    private void EnsureUpgradeSystem()
    {
        if (persistentPlayer != null && persistentPlayer.GetComponent<PlayerUpgradeSystem>() == null)
            persistentPlayer.AddComponent<PlayerUpgradeSystem>();
    }

    public static void SetRespawnPoint(
        string sceneName,
        Vector3 position,
        Quaternion rotation
    )
    {
        if (instance == null)
            CreateInstance();

        instance.hasRespawnPoint = true;
        instance.respawnSceneName = sceneName;
        instance.respawnPosition = position;
        instance.respawnRotation = rotation;
    }

    public static void RespawnPersistentPlayer()
    {
        if (instance == null || instance.persistentPlayer == null)
            return;

        Scene activeScene = SceneManager.GetActiveScene();
        if (instance.hasRespawnPoint && activeScene.name == instance.respawnSceneName)
        {
            instance.MovePersistentPlayer(
                instance.respawnPosition,
                instance.respawnRotation,
                false
            );
        }

        instance.EnablePersistentPlayerComponents();
    }

    public static void MovePersistentPlayerTo(
        Vector3 position,
        Quaternion rotation,
        bool preserveCurrentY = true
    )
    {
        if (instance == null)
            return;

        instance.MovePersistentPlayer(position, rotation, preserveCurrentY);
    }

    private void MovePersistentPlayer(Vector3 position, Quaternion rotation, bool preserveCurrentY)
    {
        if (persistentPlayer == null)
            return;

        Vector3 before = persistentPlayer.transform.position;

        Vector3 target = preserveCurrentY
            ? new(position.x, before.y, position.z)
            : position;

        Rigidbody rb = persistentPlayer.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Briefly go kinematic so PhysX treats this as a teleport instead of a same-step
            // high-speed move, which otherwise gets fought by collision/penetration resolution
            // and leaves the player looking "stuck" (input/animation still work, position doesn't).
            bool wasKinematic = rb.isKinematic;
            rb.isKinematic = true;
            rb.position = target;
            rb.rotation = rotation;
            Physics.SyncTransforms();
            rb.isKinematic = wasKinematic;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        persistentPlayer.transform.SetPositionAndRotation(target, rotation);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (persistentPlayer == null)
        {
            GameObject scenePlayer = GameObject.FindGameObjectWithTag("Player");
            if (scenePlayer != null)
            {
                persistentPlayer = scenePlayer;
                DontDestroyOnLoad(persistentPlayer);
            }
        }

        DisableDuplicatePlayers();
        SetGameplayState(scene);
        StartCoroutine(RefreshPersistentPlayerNextFrame());
    }

    private IEnumerator RefreshPersistentPlayerNextFrame()
    {
        yield return null;

        if (persistentPlayer == null)
            yield break;

        DisableDuplicatePlayers();
        EnablePersistentPlayerComponents();
    }

    private void DisableDuplicatePlayers()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            if (player == persistentPlayer)
                continue;

            player.tag = "Untagged";
            DisableComponent<PlayerController>(player);
            DisableComponent<PlayerAttack>(player);
            DisableComponent<PlayerAbilities>(player);
            DisableComponent<PlayerDamageReceiver>(player);
            DisableComponent<PlayerHealth>(player);
            DisableComponent<PlayerPanic>(player);

            foreach (Collider collider in player.GetComponentsInChildren<Collider>())
                collider.enabled = false;

            foreach (Renderer renderer in player.GetComponentsInChildren<Renderer>())
                renderer.enabled = false;
        }
    }

    private void SetGameplayState(Scene scene)
    {
        IsGameplayActive = !HubSceneMarker.IsHubScene(scene);

        if (persistentPlayer == null)
            return;

        EnablePersistentPlayerComponents();
    }

    private void EnablePersistentPlayerComponents()
    {
        PlayerController controller = persistentPlayer.GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.enabled = true;
            controller.EnableInputActions();
            controller.ResetControlState();
        }

        // A Hub is still navigable, so movement stays available. All combat and
        // survival systems are disabled there and are restored upon entering a world.
        SetComponentEnabled<PlayerAttack>(IsGameplayActive);
        SetComponentEnabled<PlayerAbilities>(IsGameplayActive);
        SetComponentEnabled<PlayerDamageReceiver>(IsGameplayActive);
        SetComponentEnabled<PlayerHealth>(IsGameplayActive);
        SetComponentEnabled<PlayerPanic>(IsGameplayActive);
        SetComponentEnabled<PlayerAim>(IsGameplayActive);
    }

    private void SetComponentEnabled<T>(bool isEnabled) where T : Behaviour
    {
        T component = persistentPlayer.GetComponent<T>();
        if (component != null)
            component.enabled = isEnabled;
    }

    private static void DisableComponent<T>(GameObject target) where T : Behaviour
    {
        T component = target.GetComponent<T>();
        if (component != null)
            component.enabled = false;
    }
}
