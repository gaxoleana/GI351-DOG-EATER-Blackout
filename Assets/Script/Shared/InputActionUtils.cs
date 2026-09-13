using UnityEngine;
using UnityEngine.InputSystem;

public static class InputActionUtils
{
    public static InputAction Find(InputActionAsset asset, string mapName, string actionName)
    {
        InputAction action = asset?.FindActionMap(mapName)?.FindAction(actionName);

        if (action == null)
            Debug.LogWarning($"Input action '{mapName}/{actionName}' not found.");

        return action;
    }
}
