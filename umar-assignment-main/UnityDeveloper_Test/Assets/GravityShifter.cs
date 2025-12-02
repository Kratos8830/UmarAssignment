using UnityEngine;

/// <summary>
/// Handles gravity shifting, player reorientation, and holographic path prediction.
/// Attach this script to your Player GameObject (which should also have CharacterController and ThirdPersonMovement).
/// </summary>
[RequireComponent(typeof(ThirdPersonMovement))] // Require the movement script
public class GravityShifter : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("The time it takes for the gravity shift and player rotation to complete.")]
    public float shiftDuration = 0.5f;
    [Tooltip("Reference to the 3rd person character's main transform to rotate.")]
    public Transform playerBody;

    [Header("Hologram Prediction Settings")]
    [Tooltip("Hologram object for the forward shift direction (relative to player body).")]
    public GameObject forwardHologram;
    [Tooltip("Hologram object for the backward shift direction (relative to player body).")]
    public GameObject backHologram;
    [Tooltip("Hologram object for the right shift direction (relative to player body).")]
    public GameObject rightHologram;
    [Tooltip("Hologram object for the left shift direction (relative to player body).")]
    public GameObject leftHologram;

    [Tooltip("How far along the predicted path (in seconds) the hologram should be placed.")]
    public float hologramPlacementTime = 1.0f; // Simplified placement time for a single object
    [Tooltip("The speed at which the player is 'launched' into the new gravity field upon shift.")]
    public float launchSpeed = 10f;

    private ThirdPersonMovement movementController; // Reference to the movement script

    // Gravity management variables
    private Vector3 targetGravity;
    private Quaternion targetRotation;
    private float shiftTimer = 0f;
    private bool isShifting = false;
    private Vector3 shiftStartGravity;
    private Quaternion shiftStartRotation;

    // --- CYCLING VARIABLES ---
    private Vector3[] orthogonalCandidates; // List of 4 available shift directions (perpendicular to current gravity)
    private int candidateIndex = 0;        // Current index into orthogonalCandidates
    // -----------------------------

    // Gravity directions relative to the world (Magnitude is 9.81f for force)
    private readonly Vector3[] gravityDirections = {
        Vector3.down * 9.81f,
        Vector3.up * 9.81f,
        Vector3.left * 9.81f,
        Vector3.right * 9.81f,
        Vector3.forward * 9.81f,
        Vector3.back * 9.81f
    };

    void Awake()
    {
        movementController = GetComponent<ThirdPersonMovement>();

        // Initialize the movement controller's gravity
        movementController.currentGravity = Vector3.down * 9.81f;

        // Hide all directional holograms on start
        DeactivateAllHolograms();
    }

    private void DeactivateAllHolograms()
    {
        if (forwardHologram != null) forwardHologram.SetActive(false);
        if (backHologram != null) backHologram.SetActive(false);
        if (rightHologram != null) rightHologram.SetActive(false);
        if (leftHologram != null) leftHologram.SetActive(false);
    }

    void Update()
    {
        HandleInput();
        UpdateShift();

        // Only show prediction when holding the shift key (e.g., Spacebar)
        if (Input.GetKey(KeyCode.Space) && !isShifting)
        {
            // The gravity direction is calculated in HandleInput when the key is first pressed
            SetAndPlaceActiveHologram();
        }
        else
        {
            // Hide all holograms when not previewing
            DeactivateAllHolograms();
        }
    }

    private void HandleInput()
    {
        // 1. START PREVIEW (Spacebar Down)
        if (Input.GetKeyDown(KeyCode.Space) && !isShifting)
        {
            // Calculate all 4 possible orthogonal gravity directions and find the best starting one
            GenerateOrthogonalCandidates();

            // Set initial target based on camera and candidates
            SetTargetGravityFromIndex(candidateIndex);

            // Show the initial hologram based on selection
            SetAndPlaceActiveHologram();
        }

        // 2. DIRECTIONAL SELECTION (While Spacebar is Held) - ARROW KEYS or Mouse Scroll
        if (Input.GetKey(KeyCode.Space) && !isShifting && orthogonalCandidates != null)
        {
            Vector3 targetShiftDirection = Vector3.zero;
            bool directionPressed = false;
            int indexChange = 0; // For mouse scroll cycling

            // Check for directional input (Arrow Keys)
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                targetShiftDirection = -Camera.main.transform.forward;
                directionPressed = true;
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                targetShiftDirection = Camera.main.transform.forward;
                directionPressed = true;
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                targetShiftDirection = -Camera.main.transform.right;
                directionPressed = true;
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                targetShiftDirection = Camera.main.transform.right;
                directionPressed = true;
            }

            // Mouse scroll wheel for fine control (optional, but good for cycling)
            float scroll = Input.mouseScrollDelta.y;
            if (scroll > 0.1f) indexChange = 1;
            else if (scroll < -0.1f) indexChange = -1;

            if (directionPressed)
            {
                // The target shift direction must be projected onto the current shift plane
                Vector3 currentDown = movementController.currentGravity.normalized;
                Vector3 targetProjection = Vector3.ProjectOnPlane(targetShiftDirection, currentDown).normalized;

                // Find the best candidate based on this direction
                candidateIndex = FindBestCandidateIndex(targetProjection);
                SetTargetGravityFromIndex(candidateIndex);

                // Update the visible hologram
                SetAndPlaceActiveHologram();
            }
            else if (indexChange != 0)
            {
                // Retain mouse scroll cycling functionality
                candidateIndex = (candidateIndex + indexChange) % orthogonalCandidates.Length;
                if (candidateIndex < 0) candidateIndex += orthogonalCandidates.Length;

                SetTargetGravityFromIndex(candidateIndex);

                // Update the visible hologram
                SetAndPlaceActiveHologram();
            }
        }

        // 3. TRIGGER SHIFT (Spacebar Up)
        if (Input.GetKeyUp(KeyCode.Space) && !isShifting)
        {
            // Start the gravity shift sequence using the currently selected targetGravity
            StartShift();
        }
    }

    // Generates a list of all 4 possible orthogonal gravity directions and finds the best starting one.
    private void GenerateOrthogonalCandidates()
    {
        Vector3 currentGravity = movementController.currentGravity;
        Vector3 currentDown = currentGravity.normalized;

        // Find two arbitrary vectors perpendicular to the current down vector
        Vector3 axis1 = Vector3.Cross(currentDown, (Mathf.Abs(currentDown.x) < 0.9f) ? Vector3.right : Vector3.forward).normalized;
        Vector3 axis2 = Vector3.Cross(currentDown, axis1).normalized;

        // The 4 candidates are: +axis1, -axis1, +axis2, -axis2 (all scaled by gravity magnitude)
        float gMag = currentGravity.magnitude;
        orthogonalCandidates = new Vector3[]
        {
            axis1 * gMag,
            -axis1 * gMag,
            axis2 * gMag,
            -axis2 * gMag
        };

        // Determine the initial best index based on camera forward direction
        Vector3 cameraForwardFlat = Vector3.ProjectOnPlane(Camera.main.transform.forward, currentDown).normalized;
        candidateIndex = FindBestCandidateIndex(cameraForwardFlat);
    }

    /// <summary>
    /// Determines which of the 4 orthogonal candidates best aligns with the given direction vector.
    /// </summary>
    /// <param name="direction">The desired new 'up' vector (e.g., Camera Forward).</param>
    /// <returns>The index of the best matching candidate.</returns>
    private int FindBestCandidateIndex(Vector3 direction)
    {
        float maxDot = -Mathf.Infinity;
        int bestIndex = 0;

        for (int i = 0; i < orthogonalCandidates.Length; i++)
        {
            // We compare the desired direction (e.g., Camera Forward) with the
            // NEW UP vector, which is the negative normalized candidate gravity vector.
            Vector3 newUpVector = -orthogonalCandidates[i].normalized;
            float dot = Vector3.Dot(newUpVector, direction.normalized);

            if (dot > maxDot)
            {
                maxDot = dot;
                bestIndex = i;
            }
        }
        return bestIndex;
    }

    // Updates targetGravity and targetRotation based on the current candidateIndex
    private void SetTargetGravityFromIndex(int index)
    {
        if (orthogonalCandidates == null || index < 0 || index >= orthogonalCandidates.Length) return;

        targetGravity = orthogonalCandidates[index];

        // Calculate the required rotation for the player's UP vector to align with the new UP vector (-targetGravity.normalized)
        targetRotation = Quaternion.FromToRotation(playerBody.up, -targetGravity.normalized) * playerBody.rotation;
    }


    private void StartShift()
    {
        isShifting = true;
        shiftTimer = 0f;
        shiftStartGravity = movementController.currentGravity;
        shiftStartRotation = playerBody.rotation;

        // The actual gravity applied: Update the movement controller's gravity state
        movementController.currentGravity = targetGravity;

        // Add an immediate 'launch' impulse to the movement controller's internal velocity
        Vector3 oldUpDirection = -shiftStartGravity.normalized;
        movementController.velocity = movementController.velocity + (oldUpDirection * launchSpeed);

        // Hide all holograms once the shift starts
        DeactivateAllHolograms();
    }

    private void UpdateShift()
    {
        if (!isShifting) return;

        shiftTimer += Time.deltaTime;
        float t = Mathf.Clamp01(shiftTimer / shiftDuration);

        // 1. Smoothly rotate the player body to the new 'up' orientation
        playerBody.rotation = Quaternion.Slerp(shiftStartRotation, targetRotation, t);

        if (t >= 1f)
        {
            isShifting = false;
            // Ensure final rotation is exact
            playerBody.rotation = targetRotation;
        }
    }

    /// <summary>
    /// Deactivates all holograms, determines the correct directional hologram based on targetRotation, 
    /// and activates it. It assumes the hologram's transform is already correctly set up locally.
    /// </summary>
    private void SetAndPlaceActiveHologram()
    {
        DeactivateAllHolograms();

        if (playerBody == null || Camera.main == null) return;

        // The vector pointing from the player to the target wall (the new UP vector)
        Vector3 newUpVector = -targetGravity.normalized;

        // Determine which of the player's axes the new up vector aligns with most closely.
        float forwardDot = Vector3.Dot(newUpVector, playerBody.forward);
        float rightDot = Vector3.Dot(newUpVector, playerBody.right);

        GameObject activeHologram = null;

        // Check if the jump is primarily along the forward/back axis
        if (Mathf.Abs(forwardDot) > Mathf.Abs(rightDot) && Mathf.Abs(forwardDot) > 0.01f)
        {
            if (forwardDot > 0)
            {
                activeHologram = backHologram;
            }
            else
            {
                activeHologram = forwardHologram;
            }
        }
        // Check if the jump is primarily along the right/left axis
        else if (Mathf.Abs(rightDot) > 0.01f)
        {
            if (rightDot > 0)
            {
                activeHologram = leftHologram;
            }
            else
            {
                activeHologram = rightHologram;
            }
        }

        if (activeHologram != null)
        {
            // Activate the pre-configured hologram only.
            // We trust its local transform is correctly set for the target direction.
            activeHologram.SetActive(true);
        }
    }

    // NOTE: Removed PlaceHologram method as per user request to use pre-set local transforms.
}