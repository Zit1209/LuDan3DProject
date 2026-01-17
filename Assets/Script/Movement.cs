using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Ground Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float groundAcceleration = 20f;
    [SerializeField] private float groundDeceleration = 25f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpImpulse = 12f;
    [SerializeField] private float forwardJumpBoost = 1.8f;
    [SerializeField] private float airControlStrength = 0.3f;

    [Header("Gravity Settings")]
    [SerializeField] private float baseGravity = -25f;
    [SerializeField] private float risingGravityMultiplier = 1f;
    [SerializeField] private float fallingGravityMultiplier = 2f;
    [SerializeField] private float maxFallSpeed = -30f;

    [Header("Climbing Settings")]
    [SerializeField] private float climbSpeed = 3f;
    [SerializeField] private float climbRunSpeed = 5f;
    [SerializeField] private float wallDetectionDistance = 0.6f;
    [SerializeField] private float wallJumpForce = 10f;
    [SerializeField] private LayerMask wallLayer = -1;

    [Header("Input Actions")]
    [SerializeField] private InputActionAsset inputAsset;

    private CharacterController controller;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction runAction;
    private Vector3 velocity;
    private Vector2 moveInput;
    private bool isRunning;
    private bool isGrounded;
    private bool isTouchingWall;
    private Vector3 wallNormal;
    private bool jumpRequested;
    private bool climbingDisabled = false;
    private float climbDisableTimer = 0f;

    private bool isInKnockback = false;
    private float knockbackTimer = 0f;

    private enum MovementState { Ground, Air, Climb, Knockback }
    private MovementState currentState = MovementState.Ground;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        
        if (inputAsset != null)
        {
            var playerActionMap = inputAsset.FindActionMap("Player");
            moveAction = playerActionMap.FindAction("Move");
            jumpAction = playerActionMap.FindAction("Jump");
            runAction = playerActionMap.FindAction("Run");
        }
    }

    private void OnEnable()
    {
        if (moveAction != null) moveAction.Enable();
        if (jumpAction != null)
        {
            jumpAction.Enable();
            jumpAction.performed += OnJumpPerformed;
        }
        if (runAction != null) runAction.Enable();
    }

    private void OnDisable()
    {
        if (jumpAction != null)
        {
            jumpAction.performed -= OnJumpPerformed;
            jumpAction.Disable();
        }
        if (moveAction != null) moveAction.Disable();
        if (runAction != null) runAction.Disable();
    }

    private void Update()
    {
        UpdateKnockbackTimer();
        UpdateClimbDisableTimer();
        
        if (!isInKnockback)
        {
            ReadInput();
        }
        
        CheckGrounded();
        DetectWall();
        
        ProcessJump();
        
        UpdateState();
        HandleMovement();
    }

    private void UpdateKnockbackTimer()
    {
        if (isInKnockback)
        {
            knockbackTimer -= Time.deltaTime;
            if (knockbackTimer <= 0f)
            {
                isInKnockback = false;
                currentState = MovementState.Air;
            }
        }
    }

    private void UpdateClimbDisableTimer()
    {
        if (climbingDisabled)
        {
            climbDisableTimer -= Time.deltaTime;
            if (climbDisableTimer <= 0f)
            {
                climbingDisabled = false;
            }
        }
    }

    private void ReadInput()
    {
        if (moveAction != null)
            moveInput = moveAction.ReadValue<Vector2>();
        
        if (runAction != null)
            isRunning = runAction.IsPressed();
    }

    private void CheckGrounded()
    {
        if (isInKnockback)
        {
            isGrounded = false;
            return;
        }
        
        isGrounded = controller.isGrounded;
    }

    private void DetectWall()
    {
        if (climbingDisabled || isInKnockback)
        {
            isTouchingWall = false;
            return;
        }

        isTouchingWall = false;

        Vector3[] directions = {
            transform.forward,
            -transform.forward,
            transform.right,
            -transform.right,
            (transform.forward + transform.right).normalized,
            (transform.forward - transform.right).normalized,
            (-transform.forward + transform.right).normalized,
            (-transform.forward - transform.right).normalized
        };

        foreach (Vector3 dir in directions)
        {
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, dir, out RaycastHit hit, wallDetectionDistance, wallLayer))
            {
                if (Vector3.Dot(hit.normal, Vector3.up) < 0.1f)
                {
                    isTouchingWall = true;
                    wallNormal = hit.normal;
                    break;
                }
            }
        }
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (!isInKnockback)
        {
            jumpRequested = true;
        }
    }

    private void ProcessJump()
    {
        if (!jumpRequested || isInKnockback)
            return;

        jumpRequested = false;

        switch (currentState)
        {
            case MovementState.Ground:
                if (isGrounded)
                {
                    PerformGroundJump();
                }
                break;

            case MovementState.Climb:
                PerformWallJump();
                break;
        }
    }

    private void PerformGroundJump()
    {
        velocity.y = jumpImpulse;

        if (isRunning && moveInput.magnitude > 0.1f)
        {
            Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y);
            Vector3 worldDirection = transform.TransformDirection(inputDirection).normalized;

            velocity.x += worldDirection.x * runSpeed * (forwardJumpBoost - 1f);
            velocity.z += worldDirection.z * runSpeed * (forwardJumpBoost - 1f);
        }

        currentState = MovementState.Air;
    }

    private void PerformWallJump()
    {
        velocity = wallNormal * wallJumpForce;
        velocity.y = jumpImpulse * 0.9f;

        currentState = MovementState.Air;
    }

    private void UpdateState()
    {
        if (isInKnockback)
            return;

        switch (currentState)
        {
            case MovementState.Ground:
                if (!isGrounded && velocity.y < 0f)
                {
                    currentState = MovementState.Air;
                }
                else if (isTouchingWall && !isGrounded && !climbingDisabled)
                {
                    currentState = MovementState.Climb;
                }
                break;

            case MovementState.Air:
                if (isGrounded && velocity.y <= 0f)
                {
                    currentState = MovementState.Ground;
                }
                else if (isTouchingWall && velocity.y <= 0f && !climbingDisabled)
                {
                    currentState = MovementState.Climb;
                    velocity = Vector3.zero;
                }
                break;

            case MovementState.Climb:
                if (isGrounded || climbingDisabled)
                {
                    currentState = MovementState.Ground;
                }
                else if (!isTouchingWall)
                {
                    currentState = MovementState.Air;
                }
                break;
        }
    }

    private void HandleMovement()
    {
        switch (currentState)
        {
            case MovementState.Ground:
                HandleGroundMovement();
                break;

            case MovementState.Air:
                HandleAirMovement();
                break;

            case MovementState.Climb:
                HandleClimbingMovement();
                break;

            case MovementState.Knockback:
                HandleKnockbackMovement();
                break;
        }

        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleGroundMovement()
    {
        float targetSpeed = isRunning ? runSpeed : walkSpeed;
        Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y);
        Vector3 worldDirection = transform.TransformDirection(inputDirection);
        Vector3 targetVelocity = worldDirection * targetSpeed;

        float acceleration = moveInput.magnitude > 0.1f ? groundAcceleration : groundDeceleration;

        velocity.x = Mathf.MoveTowards(velocity.x, targetVelocity.x, acceleration * Time.deltaTime);
        velocity.z = Mathf.MoveTowards(velocity.z, targetVelocity.z, acceleration * Time.deltaTime);

        if (isGrounded && velocity.y <= 0f)
        {
            velocity.y = -2f;
        }
        else
        {
            ApplyGravity();
        }
    }

    private void HandleAirMovement()
    {
        float targetSpeed = isRunning ? runSpeed : walkSpeed;
        Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y);
        Vector3 worldDirection = transform.TransformDirection(inputDirection);
        Vector3 targetVelocity = worldDirection * targetSpeed * airControlStrength;

        velocity.x = Mathf.MoveTowards(velocity.x, velocity.x + targetVelocity.x, groundAcceleration * airControlStrength * Time.deltaTime);
        velocity.z = Mathf.MoveTowards(velocity.z, velocity.z + targetVelocity.z, groundAcceleration * airControlStrength * Time.deltaTime);

        ApplyGravity();
    }

    private void HandleClimbingMovement()
    {
        float speed = isRunning ? climbRunSpeed : climbSpeed;

        Vector3 up = Vector3.up;
        Vector3 right = Vector3.Cross(wallNormal, up).normalized;

        Vector3 climbDirection = (up * moveInput.y) + (right * moveInput.x);

        velocity = climbDirection * speed;
    }

    private void HandleKnockbackMovement()
    {
        ApplyGravity();
    }

    private void ApplyGravity()
    {
        float gravityMultiplier = velocity.y > 0f ? risingGravityMultiplier : fallingGravityMultiplier;
        
        velocity.y += baseGravity * gravityMultiplier * Time.deltaTime;
        
        velocity.y = Mathf.Max(velocity.y, maxFallSpeed);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (isInKnockback)
            return;

        if (currentState == MovementState.Air)
        {
            if (Vector3.Dot(hit.normal, Vector3.up) < 0.1f)
            {
                isTouchingWall = true;
                wallNormal = hit.normal;
            }
        }
    }

    public void ForceExitClimb()
    {
        if (currentState == MovementState.Climb)
        {
            currentState = MovementState.Air;
        }
    }

    public void DisableClimbForSeconds(float duration)
    {
        climbingDisabled = true;
        climbDisableTimer = duration;
        
        if (currentState == MovementState.Climb)
        {
            currentState = MovementState.Air;
        }
    }

    public void ApplyKnockback(Vector3 direction, float force)
    {
        velocity = direction * force;
        velocity.y = Mathf.Max(velocity.y, jumpImpulse * 0.5f);
    }

    public void EnterKnockbackState(Vector3 direction, float force, float duration)
    {
        isInKnockback = true;
        knockbackTimer = duration;
        currentState = MovementState.Knockback;
        
        climbingDisabled = true;
        climbDisableTimer = duration;
        
        velocity = direction * force;
        
        moveInput = Vector2.zero;
    }

    public bool IsClimbing()
    {
        return currentState == MovementState.Climb;
    }
}