using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

public class LevelFailureManager : MonoBehaviour
{
    private PlayerHealth playerHealth;
    private StabilitySystem stabilitySystem;
    private bool hasFailed;
    private bool restartAsNewRun;

    [Header("Custom Failure UI")]
    [Tooltip("Required overlay CanvasGroup. Its root GameObject is kept across scene loads.")]
    [SerializeField] private CanvasGroup failureCanvasGroup;
    [Tooltip("Required handmade failure-message text element.")]
    [SerializeField] private TextMeshProUGUI failureMessage;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        if (failureCanvasGroup == null || failureMessage == null)
        {
            Debug.LogError(
                "LevelFailureManager requires both a Failure Canvas Group and Failure Message reference.",
                this
            );
            enabled = false;
            return;
        }

        DontDestroyOnLoad(failureCanvasGroup.transform.root.gameObject);
        SetFailureVisible(false);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        RegisterFailureSources();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnregisterFailureSources();
    }

    private void Update()
    {
        if (hasFailed && Keyboard.current?.rKey.wasPressedThisFrame == true)
            RestartLevel();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Time.timeScale = 1f;
        hasFailed = false;
        SetFailureVisible(false);
        RegisterFailureSources();
    }

    private void RegisterFailureSources()
    {
        UnregisterFailureSources();

        playerHealth = FindAnyObjectByType<PlayerHealth>();
        stabilitySystem = StabilitySystem.Instance;

        if (playerHealth != null)
            playerHealth.Died += HandlePlayerDied;

        if (stabilitySystem != null)
            stabilitySystem.TotalStabilityDepleted += HandleStabilityDepleted;
    }

    private void UnregisterFailureSources()
    {
        if (playerHealth != null)
            playerHealth.Died -= HandlePlayerDied;

        if (stabilitySystem != null)
            stabilitySystem.TotalStabilityDepleted -= HandleStabilityDepleted;

        playerHealth = null;
        stabilitySystem = null;
    }

    private void HandlePlayerDied(PlayerHealth player)
    {
        restartAsNewRun = false;
        FailLevel();
    }

    private void HandleStabilityDepleted()
    {
        restartAsNewRun = true;
        FailLevel();
    }

    private void FailLevel()
    {
        if (hasFailed)
            return;

        hasFailed = true;
        SetFailureVisible(true);
        DisablePlayerActions();
        Time.timeScale = 0f;
    }

    private void RestartLevel()
    {
        playerHealth?.ResetHealth();
        if (restartAsNewRun)
            stabilitySystem?.ResetAllWorlds();

        Time.timeScale = 1f;
        PlayerPersistenceManager.RespawnPersistentPlayer();
        hasFailed = false;
        restartAsNewRun = false;
        SetFailureVisible(false);
    }

    private void DisablePlayerActions()
    {
        foreach (PlayerController controller in FindObjectsByType<PlayerController>())
            controller.enabled = false;

        foreach (PlayerAbilities abilities in FindObjectsByType<PlayerAbilities>())
            abilities.enabled = false;

        foreach (PlayerAttack attack in FindObjectsByType<PlayerAttack>())
            attack.enabled = false;
    }

    private void SetFailureVisible(bool isVisible)
    {
        if (failureCanvasGroup == null)
            return;

        failureCanvasGroup.alpha = isVisible ? 1f : 0f;
        failureCanvasGroup.blocksRaycasts = isVisible;
        failureCanvasGroup.interactable = isVisible;
    }
}
