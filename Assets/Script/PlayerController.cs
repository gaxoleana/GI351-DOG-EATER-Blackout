using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    private const string PlayerActionMapName = "Player";
    private const string MoveActionName = "Move";
    private const string SprintActionName = "Sprint";
    private const string IsWalkingParameter = "isWalk";
    private const string IsSprintingParameter = "isSprint";
    private const string IsStunnedParameter = "isStun";

    [Header("Movement")]
    [SerializeField] private float speed = 5.0f;
    [SerializeField] private float sprintSpeed = 10.0f;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    private InputAction moveAction;
    private InputAction sprintAction;
    private Rigidbody rb;
    private SpriteRenderer sr;
    private Animator animator;
    private Vector3 moveDir;
    private bool isSprinting;
    private float stunTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        sr = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponent<Animator>();

        InputActionMap playerActionMap = inputActions?.FindActionMap(PlayerActionMapName);
        moveAction = playerActionMap?.FindAction(MoveActionName);
        sprintAction = playerActionMap?.FindAction(SprintActionName);
    }

    private void OnEnable()
    {
        moveAction?.Enable();
        sprintAction?.Enable();
    }

    private void OnDisable()
    {
        moveAction?.Disable();
        sprintAction?.Disable();
    }

    private void Update()
    {
        if (stunTimer > 0f)
        {
            stunTimer -= Time.deltaTime;
            moveDir = Vector3.zero;
            isSprinting = false;
            animator?.SetBool(IsStunnedParameter, true);
            UpdateAnimationState();
            return;
        }

        animator?.SetBool(IsStunnedParameter, false);

        if (moveAction == null)
            return;

        Vector2 movementInput = moveAction.ReadValue<Vector2>();
        movementInput = Vector2.ClampMagnitude(movementInput, 1f);
        moveDir = new Vector3(movementInput.x, 0.0f, movementInput.y);
        isSprinting = sprintAction?.IsPressed() == true;

        UpdateAnimationState();
        UpdateFacingDirection(movementInput.x);
    }

    private void UpdateAnimationState()
    {
        bool isMoving = stunTimer <= 0f && moveDir.sqrMagnitude > 0f;
        animator?.SetBool(IsWalkingParameter, isMoving);
        animator?.SetBool(IsSprintingParameter, isMoving && isSprinting);
    }

    public void Stun(float duration)
    {
        stunTimer = Mathf.Max(stunTimer, duration);
        moveDir = Vector3.zero;
        isSprinting = false;
        animator?.SetBool(IsStunnedParameter, true);
    }

    private void UpdateFacingDirection(float horizontalInput)
    {
        if (sr == null)
            return;

        if (horizontalInput < 0f)
        {
            sr.flipX = true;
        }
        else if (horizontalInput > 0f)
        {
            sr.flipX = false;
        }
    }

    private void FixedUpdate()
    {
        if (rb != null)
        {
            if (stunTimer > 0f)
            {
                rb.linearVelocity = Vector3.zero;
                return;
            }

            float currentSpeed = isSprinting ? sprintSpeed : speed;
            rb.linearVelocity = moveDir * currentSpeed;
        }
    }
}
