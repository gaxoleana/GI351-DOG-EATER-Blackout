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
    [SerializeField, Range(0f, 1f)] private float rechargeSlowdownMultiplier = 0.85f;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    private InputAction moveAction;
    private InputAction sprintAction;
    private Rigidbody rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Vector3 moveDir;
    private bool isSprinting;
    private bool isSprintLocked;
    private bool isRechargeSlowing;
    private float stunTimer;
    private float speedModifier = 1f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponent<Animator>();

        ResolveInputActions();
    }

    private void OnEnable()
    {
        EnableInputActions();
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

        Vector2 movementInput = moveAction != null
            ? moveAction.ReadValue<Vector2>()
            : Vector2.zero;

        if (movementInput.sqrMagnitude <= 0.0001f)
            movementInput = ReadKeyboardMovement();

        movementInput = Vector2.ClampMagnitude(movementInput, 1f);
        moveDir = new Vector3(movementInput.x, 0.0f, movementInput.y);
        isSprinting = !isSprintLocked && sprintAction?.IsPressed() == true;

        UpdateAnimationState();
        UpdateFacingDirection(movementInput.x);
    }

    private static Vector2 ReadKeyboardMovement()
    {
        if (Keyboard.current == null)
            return Vector2.zero;

        float horizontal = 0f;
        float vertical = 0f;

        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            horizontal -= 1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            horizontal += 1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            vertical -= 1f;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            vertical += 1f;

        return new Vector2(horizontal, vertical);
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

    public void ResetControlState()
    {
        rb ??= GetComponent<Rigidbody>();

        stunTimer = 0f;
        moveDir = Vector3.zero;
        isSprinting = false;
        isSprintLocked = false;
        isRechargeSlowing = false;
        speedModifier = 1f;
        if (rb != null)
            rb.linearVelocity = Vector3.zero;

        animator?.SetBool(IsStunnedParameter, false);
    }

    public void SetSprintLocked(bool isLocked)
    {
        isSprintLocked = isLocked;
        if (isSprintLocked)
            isSprinting = false;
    }

    public void SetRechargeSlowdown(bool isSlowing)
    {
        isRechargeSlowing = isSlowing;
    }

    public void EnableInputActions()
    {
        ResolveInputActions();
        inputActions?.FindActionMap(PlayerActionMapName)?.Enable();
        moveAction?.Enable();
        sprintAction?.Enable();
    }

    private void ResolveInputActions()
    {
        moveAction = InputActionUtils.Find(inputActions, PlayerActionMapName, MoveActionName);
        sprintAction = InputActionUtils.Find(inputActions, PlayerActionMapName, SprintActionName);
    }

    private void UpdateFacingDirection(float horizontalInput)
    {
        if (spriteRenderer == null)
            return;

        if (horizontalInput < 0f)
        {
            spriteRenderer.flipX = true;
        }
        else if (horizontalInput > 0f)
        {
            spriteRenderer.flipX = false;
        }
    }

    private void FixedUpdate()
    {
        if (rb == null)
            return;

        if (stunTimer > 0f)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        float activeSpeed = isSprinting ? sprintSpeed : speed;
        float rechargePenaltyMultiplier = isRechargeSlowing ? rechargeSlowdownMultiplier : 1f;
        float currentSpeed = activeSpeed * rechargePenaltyMultiplier * speedModifier;

        Vector3 movement = moveDir * currentSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);
        rb.linearVelocity = Vector3.zero;
    }

    // Lets other systems (e.g. PlayerAbilities' Shield) apply a temporary move-speed penalty/bonus.
    // 1f = normal speed, 0.5f = 50% speed, etc.
    public void SetSpeedModifier(float multiplier)
    {
        speedModifier = Mathf.Max(0f, multiplier);
    }
}
