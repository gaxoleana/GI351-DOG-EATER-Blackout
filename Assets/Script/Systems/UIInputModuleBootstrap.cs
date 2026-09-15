using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

public class UIInputModuleBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateInstance()
    {
        GameObject bootstrapObject = new("UIInputModuleBootstrap");
        bootstrapObject.AddComponent<UIInputModuleBootstrap>();
        DontDestroyOnLoad(bootstrapObject);
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
        ConfigureInputModules();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ConfigureInputModules();
    }

    private static void ConfigureInputModules()
    {
        InputSystemUIInputModule[] modules = FindObjectsByType<InputSystemUIInputModule>(
            FindObjectsInactive.Exclude
        );

        foreach (InputSystemUIInputModule module in modules)
        {
            if (module.point?.action != null && module.leftClick?.action != null)
                continue;

            bool wasEnabled = module.enabled;
            module.enabled = false;
            module.UnassignActions();
            module.AssignDefaultActions();
            module.enabled = wasEnabled;
        }
    }
}
