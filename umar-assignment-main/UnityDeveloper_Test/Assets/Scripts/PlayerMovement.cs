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
    public float jumpHeight = 2f; // New: Jump height property

    // EXPOSED: This variable will be read and modified by GravityShifter.cs
    [HideInInspector] public Vector3 velocity;

    // EXPOSED: The current gravity vector is managed by GravityShifter.cs
    [HideInInspector] public Vector3 currentGravity = Vector3.down * 9.81f;

    [Header("Ground Check Settings")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    [Header("Game Over Settings")] // New: Game Over specific settings
    public float maxAirTime = 7f;
    private float airTimeCounter = 0f;
    private bool isGameOver = false;

    [Header("References")]
    public Transform cam;
    private GameManager gameManager;

    // Private variables
    private CharacterController controller;
    [SerializeField] private Animator animator;
    private float turnSmoothVelocity;
    private bool isGrounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        gameManager = FindObjectOfType<GameManager>();
        if (cam == null) cam = Camera.main.transform;
    }

    void Update()
    {
      
        if (isGameOver) return;

       
        Vector3 downDirection = -transform.up;

       
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

       
        animator.SetBool("IsGrounded", isGrounded);

        // Reset gravity velocity when grounded and if we are moving against the current up vector
        if (isGrounded)
        {
           
            airTimeCounter = 0f;

            if (Vector3.Dot(velocity, downDirection) > -0.1f)
            {
               
                velocity = downDirection * 2f;
            }
        }
        else
        {
            
            airTimeCounter += Time.deltaTime;

         
            if (airTimeCounter >= maxAirTime)
            {
                HandleGameOver();
                return; // Stop processing further logic this frame
            }
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;

         //MOVEMENT & ROTATION
        if (inputDirection.magnitude >= 0.1f)
        {
            // Calculate movement plane (perpendicular to the player's UP direction)
            // The rest of the movement and rotation logic remains the same

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

      
        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            
            float jumpVelocityMagnitude = Mathf.Sqrt(jumpHeight * 2f * currentGravity.magnitude);
            velocity = transform.up * jumpVelocityMagnitude;

           
        }

        
        float inputMagnitude = inputDirection.magnitude;
        animator.SetFloat("Speed", inputMagnitude, 0.1f, Time.deltaTime);

        // 6. GRAVITY
        // Gravity is applied in the direction of the current gravity vector
        velocity += currentGravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }


    private void HandleGameOver()
    {
        isGameOver = true;
        Debug.Log("GAME OVER: Air Time Exceeded " + maxAirTime + " seconds!");
        gameManager.GameOver();

       
        controller.enabled = false;
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