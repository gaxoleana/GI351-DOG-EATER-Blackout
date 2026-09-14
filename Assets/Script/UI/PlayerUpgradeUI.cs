using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerUpgradeUI : MonoBehaviour
{
    private static PlayerUpgradeUI instance;

    [Header("Menu")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private bool pauseGameplay = true;

    [Header("Upgrade Buttons")]
    [SerializeField] private Button sanityChargeButton;
    [SerializeField] private Button sanityShieldButton;
    [SerializeField] private Button laserButton;
    [SerializeField] private Button sanityRunButton;

    [Header("Level Text")]
    [SerializeField] private TMP_Text sanityChargeLevelText;
    [SerializeField] private TMP_Text sanityShieldLevelText;
    [SerializeField] private TMP_Text laserLevelText;
    [SerializeField] private TMP_Text sanityRunLevelText;
    [SerializeField] private string levelFormat = "LEVEL {0}";

    private PlayerUpgradeSystem upgradeSystem;
    private FragmentCurrency currency;
    private bool isMenuOpen;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        BindButtons();
        SetMenuVisible(false);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnbindPlayer();
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;

        UnbindButtons();
        if (isMenuOpen && pauseGameplay)
            Time.timeScale = 1f;
        instance = null;
    }

    private void Start()
    {
        BindPlayer();
        RefreshDisplay();
    }

    private void Update()
    {
        if (Keyboard.current?.iKey.wasPressedThisFrame == true)
            SetMenuVisible(!isMenuOpen);

        if (upgradeSystem == null || currency == null)
            BindPlayer();
    }

    public void UpgradeSanityCharge()
    {
        TryUpgrade(PlayerUpgradeNode.SanityCharge);
    }

    public void UpgradeSanityShield()
    {
        TryUpgrade(PlayerUpgradeNode.SanityShield);
    }

    public void UpgradeLaser()
    {
        TryUpgrade(PlayerUpgradeNode.Laser);
    }

    public void UpgradeSanityRun()
    {
        TryUpgrade(PlayerUpgradeNode.SanityRun);
    }

    private void TryUpgrade(PlayerUpgradeNode node)
    {
        BindPlayer();
        upgradeSystem?.TryUpgrade(node);
        RefreshDisplay();
    }

    private void BindButtons()
    {
        sanityChargeButton?.onClick.AddListener(UpgradeSanityCharge);
        sanityShieldButton?.onClick.AddListener(UpgradeSanityShield);
        laserButton?.onClick.AddListener(UpgradeLaser);
        sanityRunButton?.onClick.AddListener(UpgradeSanityRun);
    }

    private void UnbindButtons()
    {
        sanityChargeButton?.onClick.RemoveListener(UpgradeSanityCharge);
        sanityShieldButton?.onClick.RemoveListener(UpgradeSanityShield);
        laserButton?.onClick.RemoveListener(UpgradeLaser);
        sanityRunButton?.onClick.RemoveListener(UpgradeSanityRun);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SetMenuVisible(false);
        BindPlayer();
        RefreshDisplay();
    }

    private void BindPlayer()
    {
        GameObject player = PlayerPersistenceManager.PersistentPlayer;
        PlayerUpgradeSystem nextUpgradeSystem = player?.GetComponent<PlayerUpgradeSystem>();
        FragmentCurrency nextCurrency = player?.GetComponent<FragmentCurrency>();

        if (nextUpgradeSystem == upgradeSystem && nextCurrency == currency)
            return;

        UnbindPlayer();
        upgradeSystem = nextUpgradeSystem;
        currency = nextCurrency;

        if (upgradeSystem != null)
            upgradeSystem.OnUpgradeChanged += HandleUpgradeChanged;
        if (currency != null)
            currency.OnFragmentsChanged += HandleFragmentsChanged;
    }

    private void UnbindPlayer()
    {
        if (upgradeSystem != null)
            upgradeSystem.OnUpgradeChanged -= HandleUpgradeChanged;
        if (currency != null)
            currency.OnFragmentsChanged -= HandleFragmentsChanged;

        upgradeSystem = null;
        currency = null;
    }

    private void HandleUpgradeChanged(PlayerUpgradeNode node, int level)
    {
        RefreshDisplay();
    }

    private void HandleFragmentsChanged(long amount)
    {
        RefreshButtonState(amount);
    }

    private void RefreshDisplay()
    {
        int sanityChargeLevel = upgradeSystem != null
            ? upgradeSystem.SanityChargeLevel
            : 0;
        int sanityShieldLevel = upgradeSystem != null
            ? upgradeSystem.SanityShieldLevel
            : 0;
        int laserLevel = upgradeSystem != null
            ? upgradeSystem.LaserLevel
            : 0;
        int sanityRunLevel = upgradeSystem != null
            ? upgradeSystem.SanityRunLevel
            : 0;

        SetLevelText(sanityChargeLevelText, sanityChargeLevel);
        SetLevelText(sanityShieldLevelText, sanityShieldLevel);
        SetLevelText(laserLevelText, laserLevel);
        SetLevelText(sanityRunLevelText, sanityRunLevel);
        RefreshButtonState(currency != null ? currency.CurrentFragments : 0);
    }

    private void SetLevelText(TMP_Text text, int level)
    {
        if (text != null)
            text.text = string.Format(levelFormat, level);
    }

    private void RefreshButtonState(long fragments)
    {
        bool canUpgrade = fragments >= 1 && upgradeSystem != null;
        if (sanityChargeButton != null)
            sanityChargeButton.interactable = canUpgrade;
        if (sanityShieldButton != null)
            sanityShieldButton.interactable = canUpgrade;
        if (laserButton != null)
            laserButton.interactable = canUpgrade;
        if (sanityRunButton != null)
            sanityRunButton.interactable = canUpgrade;
    }

    private void SetMenuVisible(bool isVisible)
    {
        isMenuOpen = isVisible;
        if (menuPanel != null)
            menuPanel.SetActive(isVisible);

        if (pauseGameplay)
            Time.timeScale = isVisible ? 0f : 1f;
    }
}
