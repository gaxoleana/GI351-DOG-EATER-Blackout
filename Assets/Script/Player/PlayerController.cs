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
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Vector3 moveDir;
    private bool isSprinting;
    private float stunTimer;
    private float speedModifier = 1f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponent<Animator>();

        moveAction = InputActionUtils.Find(inputActions, PlayerActionMapName, MoveActionName);
        sprintAction = InputActionUtils.Find(inputActions, PlayerActionMapName, SprintActionName);
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
        if (rb != null)
        {
            if (stunTimer > 0f)
            {
                rb.linearVelocity = Vector3.zero;
                return;
            }

            float currentSpeed = (isSprinting ? sprintSpeed : speed) * speedModifier;
            rb.linearVelocity = moveDir * currentSpeed;
        }
    }

    // Lets other systems (e.g. PlayerAbilities' Shield) apply a temporary move-speed penalty/bonus.
    // 1f = normal speed, 0.5f = 50% speed, etc.
    public void SetSpeedModifier(float multiplier)
    {
        speedModifier = Mathf.Max(0f, multiplier);
    }
}