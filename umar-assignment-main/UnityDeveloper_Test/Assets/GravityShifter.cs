using UnityEngine;

public class GravityShifter : MonoBehaviour
{
    [Header("Setup")]

    public float shiftDuration = 0.5f;
   
    public Transform playerBody;

    [Header("Hologram Prediction ")]
   
    public GameObject forwardHologram;
   
    public GameObject backHologram;
   
    public GameObject rightHologram;
  
    public GameObject leftHologram;

  
    public float hologramPlacementTime = 1.0f; 
  
    public float launchSpeed = 10f;

    private ThirdPersonMovement movementController; 

   
    private Vector3 targetGravity;
    private Quaternion targetRotation;
    private float shiftTimer = 0f;
    private bool isShifting = false;
    private Vector3 shiftStartGravity;
    private Quaternion shiftStartRotation;

   
    private Vector3[] orthogonalCandidates; 
    private int candidateIndex = 0;       
   
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

        
        if (Input.GetKey(KeyCode.RightControl) && !isShifting)
        {
            
            SetAndPlaceActiveHologram();
        }
        else
        {
            
            DeactivateAllHolograms();
        }
    }

    private void HandleInput()
    {
      
        if (Input.GetKeyDown(KeyCode.RightControl) && !isShifting)
        {
           
            GenerateOrthogonalCandidates();

           
            SetTargetGravityFromIndex(candidateIndex);

         
            SetAndPlaceActiveHologram();
        }

        
        if (Input.GetKey(KeyCode.RightControl) && !isShifting && orthogonalCandidates != null)
        {
            Vector3 targetShiftDirection = Vector3.zero;
            bool directionPressed = false;
            int indexChange = 0; 

           
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

               
                SetAndPlaceActiveHologram();
            }
            else if (indexChange != 0)
            {
               
                candidateIndex = (candidateIndex + indexChange) % orthogonalCandidates.Length;
                if (candidateIndex < 0) candidateIndex += orthogonalCandidates.Length;

                SetTargetGravityFromIndex(candidateIndex);

               
                SetAndPlaceActiveHologram();
            }
        }

       
        if (Input.GetKeyUp(KeyCode.RightControl) && !isShifting)
        {
           
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
        Vector3 cameraForwardFlat = Vector3.ProjectOnPlane(-Camera.main.transform.forward, currentDown).normalized;
        candidateIndex = FindBestCandidateIndex(cameraForwardFlat);
    }

  
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

        targetRotation = Quaternion.FromToRotation(playerBody.up, -targetGravity.normalized) * playerBody.rotation;
    }


    private void StartShift()
    {
        isShifting = true;
        shiftTimer = 0f;
        shiftStartGravity = movementController.currentGravity;
        shiftStartRotation = playerBody.rotation;

        
        movementController.currentGravity = targetGravity;

        
        Vector3 oldUpDirection = -shiftStartGravity.normalized;
        movementController.velocity = movementController.velocity + (oldUpDirection * launchSpeed);

      
        DeactivateAllHolograms();
    }

    private void UpdateShift()
    {
        if (!isShifting) return;

        shiftTimer += Time.deltaTime;
        float t = Mathf.Clamp01(shiftTimer / shiftDuration);


        playerBody.rotation = Quaternion.Slerp(shiftStartRotation, targetRotation, t);

        if (t >= 1f)
        {
            isShifting = false;
          
            playerBody.rotation = targetRotation;
        }
    }

 
    private void SetAndPlaceActiveHologram()
    {
        DeactivateAllHolograms();

        if (playerBody == null || Camera.main == null) return;

       
        Vector3 newUpVector = -targetGravity.normalized;

       
        float forwardDot = Vector3.Dot(newUpVector, playerBody.forward);
        float rightDot = Vector3.Dot(newUpVector, playerBody.right);

        GameObject activeHologram = null;

       
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
            
            activeHologram.SetActive(true);
        }
    }

    
}