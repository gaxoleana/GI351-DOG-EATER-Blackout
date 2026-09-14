using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// วาง Component นี้ไว้บน GameObject ใน Persistent Scene (scene ที่ไม่ถูก unload)
// พร้อม CanvasGroup ของ FadeImage (สีดำเต็มจอ, Screen Space - Overlay, sort order สูงสุด)
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("References")]
    [Tooltip("CanvasGroup ของ Image สีดำเต็มจอ")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [Header("Timing")]
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float fadeInDuration = 0.5f;
    [Tooltip("หน่วงเวลาสั้น ๆ ตอนจอดำสนิท ก่อนเริ่มโหลดฉากใหม่ (กันความรู้สึกโหลดกระตุกทันที)")]
    [SerializeField] private float holdBlackDuration = 0.15f;

    private bool isTransitioning;

    private void Awake()
    {
        // Singleton แบบ persistent — กันซ้ำถ้าเผลอมีสอง instance ข้าม scene
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (fadeCanvasGroup != null)
            DontDestroyOnLoad(fadeCanvasGroup.transform.root.gameObject);

        if (fadeCanvasGroup == null)
            CreateFadeOverlay();

        SetFadeAlpha(0f);
        SetFadeBlocking(false);
    }

    // เรียกจากที่อื่น เช่น: SceneTransitionManager.Instance.WarpTo("MirrorWorld_A");
    public void WarpTo(string sceneName)
    {
        if (isTransitioning)
            return;

        StartCoroutine(WarpRoutine(sceneName, false, Vector3.zero, Quaternion.identity));
    }

    public void WarpTo(string sceneName, Vector3 spawnPosition, Quaternion spawnRotation)
    {
        if (isTransitioning)
            return;

        PlayerPersistenceManager.SetRespawnPoint(sceneName, spawnPosition, spawnRotation);
        StartCoroutine(WarpRoutine(sceneName, true, spawnPosition, spawnRotation));
    }

    private IEnumerator WarpRoutine(
        string sceneName,
        bool hasCustomSpawnPoint,
        Vector3 spawnPosition,
        Quaternion spawnRotation
    )
    {
        isTransitioning = true;
        SetFadeBlocking(true);

        yield return Fade(0f, 1f, fadeOutDuration);

        if (holdBlackDuration > 0f)
            yield return new WaitForSeconds(holdBlackDuration);

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);
        loadOperation.allowSceneActivation = true;

        while (!loadOperation.isDone)
            yield return null;

        if (hasCustomSpawnPoint)
        {
            yield return null;
            PlayerPersistenceManager.MovePersistentPlayerTo(
                spawnPosition,
                spawnRotation,
                false
            );
        }

        yield return Fade(1f, 0f, fadeInDuration);

        SetFadeBlocking(false);
        isTransitioning = false;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (fadeCanvasGroup == null || duration <= 0f)
        {
            SetFadeAlpha(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetFadeAlpha(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }

        SetFadeAlpha(to);
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeCanvasGroup != null)
            fadeCanvasGroup.alpha = alpha;
    }

    private void SetFadeBlocking(bool isBlocking)
    {
        if (fadeCanvasGroup == null)
            return;

        fadeCanvasGroup.blocksRaycasts = isBlocking;
        fadeCanvasGroup.interactable = isBlocking;
    }

    private void CreateFadeOverlay()
    {
        GameObject canvasObject = new("SceneTransitionCanvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        canvasObject.AddComponent<CanvasScaler>();

        GameObject fadeObject = new("FadeImage");
        fadeObject.transform.SetParent(canvasObject.transform, false);

        RectTransform fadeTransform = fadeObject.AddComponent<RectTransform>();
        fadeTransform.anchorMin = Vector2.zero;
        fadeTransform.anchorMax = Vector2.one;
        fadeTransform.offsetMin = Vector2.zero;
        fadeTransform.offsetMax = Vector2.zero;

        Image fadeImage = fadeObject.AddComponent<Image>();
        fadeImage.color = Color.black;
        fadeImage.raycastTarget = false;
        fadeCanvasGroup = fadeObject.AddComponent<CanvasGroup>();
    }
}