using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

public class LevelFailureManager : MonoBehaviour
{
    private PlayerHealth playerHealth;
    private WorldStability worldStability;
    private CanvasGroup failureCanvasGroup;
    private bool hasFailed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateInstance()
    {
        if (FindAnyObjectByType<LevelFailureManager>() != null)
            return;

        GameObject managerObject = new("LevelFailureManager");
        managerObject.AddComponent<LevelFailureManager>();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        CreateFailureOverlay();
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
        worldStability = FindAnyObjectByType<WorldStability>();

        if (playerHealth != null)
            playerHealth.Died += HandlePlayerDied;

        if (worldStability != null)
            worldStability.StabilityDepleted += HandleStabilityDepleted;
    }

    private void UnregisterFailureSources()
    {
        if (playerHealth != null)
            playerHealth.Died -= HandlePlayerDied;

        if (worldStability != null)
            worldStability.StabilityDepleted -= HandleStabilityDepleted;

        playerHealth = null;
        worldStability = null;
    }

    private void HandlePlayerDied(PlayerHealth player)
    {
        FailLevel("YOU DIED");
    }

    private void HandleStabilityDepleted()
    {
        FailLevel("WORLD COLLAPSED");
    }

    private void FailLevel(string title)
    {
        if (hasFailed)
            return;

        hasFailed = true;
        SetFailureMessage(title);
        SetFailureVisible(true);
        DisablePlayerActions();
        Time.timeScale = 0f;
    }

    private void RestartLevel()
    {
        playerHealth?.ResetHealth();
        Time.timeScale = 1f;
        PlayerPersistenceManager.RespawnPersistentPlayer();
        hasFailed = false;
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

    private void CreateFailureOverlay()
    {
        GameObject canvasObject = new("LevelFailureCanvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        canvasObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        failureCanvasGroup = canvasObject.AddComponent<CanvasGroup>();

        GameObject messageObject = new("FailureMessage");
        messageObject.transform.SetParent(canvasObject.transform, false);

        RectTransform messageTransform = messageObject.AddComponent<RectTransform>();
        messageTransform.anchorMin = Vector2.zero;
        messageTransform.anchorMax = Vector2.one;
        messageTransform.offsetMin = Vector2.zero;
        messageTransform.offsetMax = Vector2.zero;

        TextMeshProUGUI message = messageObject.AddComponent<TextMeshProUGUI>();
        message.alignment = TextAlignmentOptions.Center;
        message.fontSize = 48f;
        message.color = Color.white;
        message.text = "";

        SetFailureVisible(false);
    }

    private void SetFailureMessage(string title)
    {
        TextMeshProUGUI message = failureCanvasGroup.GetComponentInChildren<TextMeshProUGUI>();
        if (message != null)
            message.text = $"{title}\n\nPRESS R TO RESTART";
    }

    private void SetFailureVisible(bool isVisible)
    {
        failureCanvasGroup.alpha = isVisible ? 1f : 0f;
        failureCanvasGroup.blocksRaycasts = isVisible;
        failureCanvasGroup.interactable = isVisible;
    }
}