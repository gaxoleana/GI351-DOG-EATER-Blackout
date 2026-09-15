using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAim : MonoBehaviour
{
    private const float MinimumAimDistance = 0.0001f;

    [Header("References")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Camera cam;

    [Header("Settings")]
    [SerializeField] private float rotationSpeed = 15f;
    [SerializeField] private float attackPointRotationOffset = -90f;

    private Plane groundPlane;

    private void Awake()
    {
        if (cam == null)
            cam = Camera.main;
    }

    private void Update()
    {
        if (cam == null)
            cam = Camera.main;

        AimAttackPointAtMouse();
    }

    private void AimAttackPointAtMouse()
    {
        if (attackPoint == null || cam == null)
            return;

        if (Mouse.current == null)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mousePosition);
        groundPlane = new Plane(Vector3.up, attackPoint.position);

        if (!groundPlane.Raycast(ray, out float distance))
            return;

        Vector3 directionToMouse = ray.GetPoint(distance) - attackPoint.position;
        directionToMouse.y = 0f;

        if (directionToMouse.sqrMagnitude <= MinimumAimDistance)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(directionToMouse, Vector3.up)
                                   * Quaternion.Euler(0f, attackPointRotationOffset, 0f);

        attackPoint.rotation = Quaternion.Slerp(
            attackPoint.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }
}
