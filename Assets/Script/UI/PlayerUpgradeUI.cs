using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerUpgradeUI : MonoBehaviour
{
    private static PlayerUpgradeUI instance;

    public static PlayerUpgradeUI Instance => instance;
    public bool IsMenuOpen => isMenuOpen;

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

    [Header("Upgrade Description Text")]
    [Tooltip("Optional TMP descriptions shown beside each upgrade button. They update to the current total bonus.")]
    [SerializeField] private TMP_Text sanityChargeDescriptionText;
    [SerializeField] private TMP_Text sanityShieldDescriptionText;
    [SerializeField] private TMP_Text laserDescriptionText;
    [SerializeField] private TMP_Text sanityRunDescriptionText;
    [SerializeField, TextArea(2, 4)] private string sanityChargeDescriptionFormat =
        "RECHARGE +{0:0.#}%  |  DRAIN -{1:0.#}%";
    [SerializeField, TextArea(2, 4)] private string sanityShieldDescriptionFormat =
        "SHIELD SPEED +{0:0.#}%  |  HIT COST -{1:0.#}%";
    [SerializeField, TextArea(2, 4)] private string laserDescriptionFormat =
        "COST -{0:0.#}%  |  COOLDOWN -{1:0.##}s";
    [SerializeField, TextArea(2, 4)] private string sanityRunDescriptionFormat =
        "RUN SPEED +{0:0.#}%";

    [Header("Current Stats")]
    [Tooltip("Optional live stat text. {0}=HP %, {1}=Sanity %, {2}=total Stability %, {3}=Fragments.")]
    [SerializeField] private TMP_Text currentStatsText;
    [SerializeField, TextArea(3, 8)] private string currentStatsFormat =
        "HP: {0}/100\nSANITY: {1}/100\nSTABILITY: {2}/100\nFRAGMENT: {3}";

    [Header("Overview Stats")]
    [Tooltip("Optional upgrade overview text, suitable for a HUD such as the lower-left corner. The values are the current total bonuses; see the default format for placeholder order.")]
    [SerializeField] private TMP_Text overviewStatsText;
    [SerializeField, TextArea(3, 8)] private string overviewStatsFormat =
        "SANITY: RECHARGE +{0:0.#}% | DRAIN -{1:0.#}%\nSHIELD: SPEED +{2:0.#}% | HIT -{3:0.#}%\nLASER: COST -{4:0.#}% | CD -{5:0.##}s\nRUN: SPEED +{6:0.#}%";

    private PlayerUpgradeSystem upgradeSystem;
    private FragmentCurrency currency;
    private IResourceStat healthStat;
    private IResourceStat sanityStat;
    private StabilitySystem stabilitySystem;
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
        if (upgradeSystem == null || currency == null || stabilitySystem != StabilitySystem.Instance)
            BindPlayer();
    }

    public void OpenMenu()
    {
        BindPlayer();
        RefreshDisplay();
        SetMenuVisible(true);
    }

    public void CloseMenu()
    {
        SetMenuVisible(false);
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
        IResourceStat nextHealthStat = player?.GetComponent<PlayerHealth>() as IResourceStat;
        IResourceStat nextSanityStat = player?.GetComponent<PlayerSanity>() as IResourceStat;
        StabilitySystem nextStabilitySystem = StabilitySystem.Instance;

        if (nextUpgradeSystem == upgradeSystem && nextCurrency == currency &&
            nextHealthStat == healthStat && nextSanityStat == sanityStat &&
            nextStabilitySystem == stabilitySystem)
            return;

        UnbindPlayer();
        upgradeSystem = nextUpgradeSystem;
        currency = nextCurrency;
        healthStat = nextHealthStat;
        sanityStat = nextSanityStat;
        stabilitySystem = nextStabilitySystem;

        if (upgradeSystem != null)
            upgradeSystem.OnUpgradeChanged += HandleUpgradeChanged;
        if (currency != null)
            currency.OnFragmentsChanged += HandleFragmentsChanged;
        if (healthStat != null)
            healthStat.OnChanged += HandleCurrentStatChanged;
        if (sanityStat != null)
            sanityStat.OnChanged += HandleCurrentStatChanged;
        if (stabilitySystem != null)
            stabilitySystem.TotalStabilityChanged += HandleCurrentStatChanged;
    }

    private void UnbindPlayer()
    {
        if (upgradeSystem != null)
            upgradeSystem.OnUpgradeChanged -= HandleUpgradeChanged;
        if (currency != null)
            currency.OnFragmentsChanged -= HandleFragmentsChanged;
        if (healthStat != null)
            healthStat.OnChanged -= HandleCurrentStatChanged;
        if (sanityStat != null)
            sanityStat.OnChanged -= HandleCurrentStatChanged;
        if (stabilitySystem != null)
            stabilitySystem.TotalStabilityChanged -= HandleCurrentStatChanged;

        upgradeSystem = null;
        currency = null;
        healthStat = null;
        sanityStat = null;
        stabilitySystem = null;
    }

    private void HandleUpgradeChanged(PlayerUpgradeNode node, int level)
    {
        RefreshDisplay();
    }

    private void HandleFragmentsChanged(long amount)
    {
        RefreshButtonState(amount);
        RefreshStatsText();
    }

    private void HandleCurrentStatChanged(float current, float max)
    {
        RefreshStatsText();
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
        RefreshUpgradeDescriptions();
        RefreshButtonState(currency != null ? currency.CurrentFragments : 0);
        RefreshStatsText(sanityChargeLevel, sanityShieldLevel, laserLevel, sanityRunLevel);
    }

    private void SetLevelText(TMP_Text text, int level)
    {
        if (text != null)
            text.text = string.Format(levelFormat, level);
    }

    private void RefreshButtonState(long fragments)
    {
        if (sanityChargeButton != null)
            sanityChargeButton.interactable = CanUpgrade(PlayerUpgradeNode.SanityCharge, fragments);
        if (sanityShieldButton != null)
            sanityShieldButton.interactable = CanUpgrade(PlayerUpgradeNode.SanityShield, fragments);
        if (laserButton != null)
            laserButton.interactable = CanUpgrade(PlayerUpgradeNode.Laser, fragments);
        if (sanityRunButton != null)
            sanityRunButton.interactable = CanUpgrade(PlayerUpgradeNode.SanityRun, fragments);
    }

    private bool CanUpgrade(PlayerUpgradeNode node, long fragments)
    {
        return upgradeSystem != null && upgradeSystem.CanUpgrade(node, fragments);
    }

    private void RefreshUpgradeDescriptions()
    {
        float rechargeBonus = upgradeSystem != null ? upgradeSystem.SanityRechargeBonusPercent : 0f;
        float drainReduction = upgradeSystem != null ? upgradeSystem.SanityDrainReductionPercent : 0f;
        float shieldSpeedBonus = upgradeSystem != null ? upgradeSystem.ShieldSpeedBonusPercent : 0f;
        float shieldHitCostReduction = upgradeSystem != null ? upgradeSystem.ShieldHitCostReductionPercent : 0f;
        float laserCostReduction = upgradeSystem != null ? upgradeSystem.LaserCostReductionPercent : 0f;
        float laserCooldownReduction = upgradeSystem != null ? upgradeSystem.LaserCooldownReductionSeconds : 0f;
        float runSpeedBonus = upgradeSystem != null ? upgradeSystem.SanityRunSpeedBonusPercent : 0f;

        SetText(sanityChargeDescriptionText, sanityChargeDescriptionFormat, rechargeBonus, drainReduction);
        SetText(sanityShieldDescriptionText, sanityShieldDescriptionFormat, shieldSpeedBonus, shieldHitCostReduction);
        SetText(laserDescriptionText, laserDescriptionFormat, laserCostReduction, laserCooldownReduction);
        SetText(sanityRunDescriptionText, sanityRunDescriptionFormat, runSpeedBonus);

        if (overviewStatsText != null)
        {
            overviewStatsText.text = string.Format(
                overviewStatsFormat,
                rechargeBonus,
                drainReduction,
                shieldSpeedBonus,
                shieldHitCostReduction,
                laserCostReduction,
                laserCooldownReduction,
                runSpeedBonus
            );
        }
    }

    private static void SetText(TMP_Text text, string format, params object[] values)
    {
        if (text != null)
            text.text = string.Format(format, values);
    }

    private void RefreshStatsText(
        int sanityChargeLevel = -1,
        int sanityShieldLevel = -1,
        int laserLevel = -1,
        int sanityRunLevel = -1)
    {
        if (sanityChargeLevel < 0)
            sanityChargeLevel = upgradeSystem != null ? upgradeSystem.SanityChargeLevel : 0;
        if (sanityShieldLevel < 0)
            sanityShieldLevel = upgradeSystem != null ? upgradeSystem.SanityShieldLevel : 0;
        if (laserLevel < 0)
            laserLevel = upgradeSystem != null ? upgradeSystem.LaserLevel : 0;
        if (sanityRunLevel < 0)
            sanityRunLevel = upgradeSystem != null ? upgradeSystem.SanityRunLevel : 0;

        if (currentStatsText != null)
        {
            currentStatsText.text = string.Format(
                currentStatsFormat,
                GetPercent(healthStat),
                GetPercent(sanityStat),
                stabilitySystem != null
                    ? GetPercent(stabilitySystem as IResourceStat)
                    : 0,
                currency != null ? currency.CurrentFragments : 0
            );
        }

        RefreshUpgradeDescriptions();
    }

    private static int GetPercent(IResourceStat resourceStat)
    {
        if (resourceStat == null || resourceStat.Max <= 0f)
            return 0;

        return Mathf.RoundToInt(Mathf.Clamp01(resourceStat.Current / resourceStat.Max) * 100f);
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
