using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float runJumpMomentumMultiplier = 1.5f;
    [SerializeField] private float gravity = -20f;

    [Header("Climbing Settings")]
    [SerializeField] private float climbSpeed = 3f;
    [SerializeField] private float climbRunSpeed = 5f;
    [SerializeField] private float wallDetectionDistance = 0.6f;
    [SerializeField] private LayerMask wallLayer = -1;

    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference runAction;

    private CharacterController controller;
    private Vector3 velocity;
    private Vector2 moveInput;
    private bool isRunning;
    private bool isGrounded;
    private bool isTouchingWall;
    private Vector3 wallNormal;
    
    private enum MovementState { Ground, Air, Climb }
    private MovementState currentState = MovementState.Ground;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        moveAction.action.Enable();
        jumpAction.action.Enable();
        runAction.action.Enable();

        jumpAction.action.performed += OnJump;
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
        jumpAction.action.Disable();
        runAction.action.Disable();

        jumpAction.action.performed -= OnJump;
    }

    private void Update()
    {
        ReadInput();
        CheckGrounded();
        DetectWall();
        UpdateState();
        HandleMovement();
    }

    private void ReadInput()
    {
        moveInput = moveAction.action.ReadValue<Vector2>();
        isRunning = runAction.action.IsPressed();
    }

    private void CheckGrounded()
    {
        isGrounded = controller.isGrounded;
    }

    private void DetectWall()
    {
        isTouchingWall = false;

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        Vector3[] directions = { forward, -forward, right, -right };

        foreach (Vector3 dir in directions)
        {
            if (Physics.Raycast(transform.position, dir, out RaycastHit hit, wallDetectionDistance, wallLayer))
            {
                isTouchingWall = true;
                wallNormal = hit.normal;
                break;
            }
        }
    }

    private void UpdateState()
    {
        switch (currentState)
        {
            case MovementState.Ground:
                if (!isGrounded)
                {
                    currentState = MovementState.Air;
                }
                else if (isTouchingWall && !isGrounded)
                {
                    currentState = MovementState.Climb;
                }
                break;

            case MovementState.Air:
                if (isGrounded)
                {
                    currentState = MovementState.Ground;
                }
                else if (isTouchingWall)
                {
                    currentState = MovementState.Climb;
                    velocity = Vector3.zero;
                }
                break;

            case MovementState.Climb:
                if (isGrounded || !isTouchingWall)
                {
                    currentState = isGrounded ? MovementState.Ground : MovementState.Air;
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
        }

        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleGroundMovement()
    {
        float speed = isRunning ? runSpeed : walkSpeed;
        Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);
        moveDirection = transform.TransformDirection(moveDirection);

        velocity.x = moveDirection.x * speed;
        velocity.z = moveDirection.z * speed;

        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        velocity.y += gravity * Time.deltaTime;
    }

    private void HandleAirMovement()
    {
        velocity.y += gravity * Time.deltaTime;
    }

    private void HandleClimbingMovement()
    {
        float speed = isRunning ? climbRunSpeed : climbSpeed;

        Vector3 up = Vector3.up;
        Vector3 right = Vector3.Cross(wallNormal, up).normalized;

        Vector3 climbDirection = (up * moveInput.y) + (right * moveInput.x);

        velocity = climbDirection * speed;
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        switch (currentState)
        {
            case MovementState.Ground:
                if (isGrounded)
                {
                    velocity.y = jumpForce;

                    if (isRunning && moveInput.magnitude > 0.1f)
                    {
                        Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
                        horizontalVelocity *= runJumpMomentumMultiplier;
                        velocity.x = horizontalVelocity.x;
                        velocity.z = horizontalVelocity.z;
                    }

                    currentState = MovementState.Air;
                }
                break;

            case MovementState.Climb:
                velocity = wallNormal * jumpForce * 0.7f;
                velocity.y = jumpForce;
                currentState = MovementState.Air;
                break;
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (currentState == MovementState.Air && Vector3.Dot(hit.normal, Vector3.up) < 0.1f)
        {
            isTouchingWall = true;
            wallNormal = hit.normal;
        }
    }
}