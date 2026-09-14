using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerPersistenceManager : MonoBehaviour
{
    private const string HubSceneName = "SampleHub";

    private static PlayerPersistenceManager instance;
    private GameObject persistentPlayer;
    private GameObject playerPrefab;

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
                    spawnPosition,
                    spawnRotation
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
        else
        {
            // Player already exists from a previous scene - move it to this scene's portal instead of leaving it at its old position.
            instance.MovePersistentPlayerTo(spawnPosition, spawnRotation);
        }

        instance.SetGameplayState(SceneManager.GetActiveScene());
    }

    private void MovePersistentPlayerTo(Vector3 position, Quaternion rotation)
    {
        if (persistentPlayer == null)
            return;

        Vector3 before = persistentPlayer.transform.position;

        // Portals are placed at the sprite/pivot height, not the floor height, so only teleport
        // the horizontal position and keep the player's current (already grounded) Y - otherwise
        // the player can land inside a wall/ceiling at the portal's height and look "stuck".
        Vector3 target = new(position.x, before.y, position.z);

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
        IsGameplayActive = scene.name != HubSceneName;

        if (persistentPlayer == null)
            return;

        EnablePersistentPlayerComponents();

        WorldStability stability = persistentPlayer.GetComponent<WorldStability>();
        if (stability != null)
            Destroy(stability);
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

        PlayerAttack attack = persistentPlayer.GetComponent<PlayerAttack>();
        if (attack != null)
        {
            attack.enabled = true;
            attack.EnableInputActions();
        }

        PlayerAbilities abilities = persistentPlayer.GetComponent<PlayerAbilities>();
        if (abilities != null)
        {
            abilities.enabled = true;
            abilities.EnableInputActions();
        }

        EnableComponent<PlayerDamageReceiver>();
        EnableComponent<PlayerHealth>();
        EnableComponent<PlayerPanic>();
    }

    private void EnableComponent<T>() where T : Behaviour
    {
        T component = persistentPlayer.GetComponent<T>();
        if (component != null)
            component.enabled = true;
    }

    private static void DisableComponent<T>(GameObject target) where T : Behaviour
    {
        T component = target.GetComponent<T>();
        if (component != null)
            component.enabled = false;
    }
}
