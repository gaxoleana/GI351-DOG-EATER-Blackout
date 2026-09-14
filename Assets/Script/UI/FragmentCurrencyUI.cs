using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FragmentCurrencyUI : MonoBehaviour
{
    private static FragmentCurrencyUI instance;
    private static bool isCreatingPersistentInstance;

    [Header("References")]
    [SerializeField] private TMP_Text countText;

    [Header("Display")]
    [SerializeField] private string displayFormat = "FRAGMENT {0}";

    private FragmentCurrency currency;
    private bool isInitialized;

    private void Awake()
    {
        if (isCreatingPersistentInstance)
            return;

        if (instance != null && instance != this)
        {
            countText?.gameObject.SetActive(false);
            Destroy(this);
            return;
        }

        if (countText == null)
            return;

        CreatePersistentInstance();
        countText.gameObject.SetActive(false);
        Destroy(this);
    }

    private void OnEnable()
    {
        if (!isInitialized)
            return;

        SceneManager.sceneLoaded += HandleSceneLoaded;
        BindCurrency();
    }

    private void OnDisable()
    {
        if (!isInitialized)
            return;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnbindCurrency();
    }

    private void Start()
    {
        if (!isInitialized)
            return;

        BindCurrency();
    }

    private void CreatePersistentInstance()
    {
        GameObject canvasObject = new("FragmentCurrencyCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        Canvas sourceCanvas = countText.GetComponentInParent<Canvas>();

        canvas.renderMode = sourceCanvas != null
            ? sourceCanvas.renderMode
            : RenderMode.ScreenSpaceOverlay;
        canvas.worldCamera = sourceCanvas != null ? sourceCanvas.worldCamera : null;
        canvas.sortingOrder = sourceCanvas != null ? sourceCanvas.sortingOrder + 1 : 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        CanvasScaler sourceScaler = sourceCanvas != null
            ? sourceCanvas.GetComponent<CanvasScaler>()
            : null;

        if (sourceScaler != null)
        {
            scaler.uiScaleMode = sourceScaler.uiScaleMode;
            scaler.referencePixelsPerUnit = sourceScaler.referencePixelsPerUnit;
            scaler.referenceResolution = sourceScaler.referenceResolution;
            scaler.screenMatchMode = sourceScaler.screenMatchMode;
            scaler.matchWidthOrHeight = sourceScaler.matchWidthOrHeight;
            scaler.physicalUnit = sourceScaler.physicalUnit;
            scaler.fallbackScreenDPI = sourceScaler.fallbackScreenDPI;
            scaler.defaultSpriteDPI = sourceScaler.defaultSpriteDPI;
        }

        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject textObject = Instantiate(countText.gameObject, canvasObject.transform, false);
        textObject.name = countText.gameObject.name;
        TMP_Text persistentText = textObject.GetComponent<TMP_Text>();

        isCreatingPersistentInstance = true;
        FragmentCurrencyUI persistentInstance = canvasObject.AddComponent<FragmentCurrencyUI>();
        isCreatingPersistentInstance = false;

        persistentInstance.Initialize(persistentText, displayFormat);
        instance = persistentInstance;
        DontDestroyOnLoad(canvasObject);
    }

    private void Initialize(TMP_Text text, string format)
    {
        countText = text;
        displayFormat = format;
        isInitialized = true;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindCurrency();
    }

    private void BindCurrency()
    {
        FragmentCurrency persistentCurrency = PlayerPersistenceManager.PersistentPlayer?
            .GetComponent<FragmentCurrency>();

        if (persistentCurrency == currency)
        {
            RefreshDisplay();
            return;
        }

        UnbindCurrency();
        currency = persistentCurrency;

        if (currency != null)
            currency.OnFragmentsChanged += HandleFragmentsChanged;

        RefreshDisplay();
    }

    private void UnbindCurrency()
    {
        if (currency != null)
            currency.OnFragmentsChanged -= HandleFragmentsChanged;

        currency = null;
    }

    private void HandleFragmentsChanged(long amount)
    {
        UpdateDisplay(amount);
    }

    private void RefreshDisplay()
    {
        UpdateDisplay(currency != null ? currency.CurrentFragments : 0);
    }

    private void UpdateDisplay(long amount)
    {
        if (countText != null)
            countText.text = string.Format(displayFormat, amount);
    }
}
