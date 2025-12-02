using UnityEngine;

/// <summary>
/// Third Person Movement adapted to work with dynamic, non-Y-axis gravity.
/// All movement, rotation, and gravity checks are relative to the player's transform.up.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class ThirdPersonMovement : MonoBehaviour
{
    [Header("Movement Stats")]
    public float moveSpeed = 6f;
    public float turnSmoothTime = 0.1f;

    // EXPOSED: This variable will be read and modified by GravityShifter.cs
    [HideInInspector] public Vector3 velocity;

    // EXPOSED: The current gravity vector is managed by GravityShifter.cs
    [HideInInspector] public Vector3 currentGravity = Vector3.down * 9.81f;

    [Header("Ground Check Settings")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    [Header("References")]
    public Transform cam;

    // Private variables
    private CharacterController controller;
    private Animator animator;
    private float turnSmoothVelocity;
    private bool isGrounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        if (cam == null) cam = Camera.main.transform;
    }

    void Update()
    {
        // 1. GROUND CHECK (Relative to the current 'Down' direction)
        Vector3 downDirection = -transform.up;

        // Checks a sphere below the groundCheck point for ground layer collision
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        // Update Animator Bool
        animator.SetBool("IsGrounded", isGrounded);

        // Reset gravity velocity when grounded and if we are moving against the current up vector
        if (isGrounded && Vector3.Dot(velocity, downDirection) > -0.1f)
        {
            // Apply a small force down the new gravity vector to stick to the floor
            velocity = downDirection * 2f;
        }

        // 2. INPUT
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;

        // 3. MOVEMENT & ROTATION
        if (inputDirection.magnitude >= 0.1f)
        {
            // Calculate movement plane (perpendicular to the player's UP direction)
            Vector3 playerRight = Vector3.Cross(transform.up, cam.forward).normalized;
            Vector3 playerForward = Vector3.Cross(playerRight, transform.up).normalized;

            // Project camera forward and right onto the player's movement plane
            Vector3 cameraForwardOnPlane = Vector3.ProjectOnPlane(cam.forward, transform.up).normalized;
            Vector3 cameraRightOnPlane = Vector3.ProjectOnPlane(cam.right, transform.up).normalized;

            // Determine the move direction using the camera's orientation on the new plane
            Vector3 moveDir = cameraForwardOnPlane * vertical + cameraRightOnPlane * horizontal;
            moveDir.Normalize();

            // Calculate the target rotation angle based on the movement direction
            float targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;

            // Smoothly rotate the character's Y-axis relative to the current UP vector
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir, transform.up), turnSmoothTime);

            // Move the character
            controller.Move(moveDir * moveSpeed * Time.deltaTime);
        }

        // 4. ANIMATOR SPEED
        float inputMagnitude = inputDirection.magnitude;
        animator.SetFloat("Speed", inputMagnitude, 0.1f, Time.deltaTime);

        // 5. GRAVITY
        // Gravity is applied in the direction of the current gravity vector
        velocity += currentGravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    // Helper to visualize the Ground Check sphere in the Editor
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }
}