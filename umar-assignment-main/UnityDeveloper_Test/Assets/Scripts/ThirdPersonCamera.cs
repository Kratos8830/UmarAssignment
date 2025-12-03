using UnityEngine;

public class ThirdPersonGravityCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;           
    public Vector3 pivotOffset = new Vector3(0f, 1.7f, 0f); 

    [Header("Distance")]
    public float distance = 5f;
    public float minDistance = 2f;
    public float maxDistance = 8f;
    public float zoomSpeed = 2f;

    [Header("Rotation")]
    public float mouseSensitivityX = 120f;
    public float mouseSensitivityY = 120f;
    public float minPitch = -30f;
    public float maxPitch = 70f;

    [Header("Gravity / Up")]
    public bool usePlayerUp = true;     
    public Vector3 customUp = Vector3.up;

    private float yaw;   
    private float pitch; 

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (target == null)
        {
            
            return;
        }

        
        Vector3 up = GetUpDirection();
        Vector3 forward = target.forward;

        
        forward = Vector3.ProjectOnPlane(forward, up).normalized;
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;

        yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        pitch = 10f; 
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 up = GetUpDirection();

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        yaw += mouseX * mouseSensitivityX * Time.deltaTime;
        pitch -= mouseY * mouseSensitivityY * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

       // Zoom with scrollwheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        distance -= scroll * zoomSpeed;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        Quaternion upAlign = Quaternion.FromToRotation(Vector3.up, up);

        Quaternion yawRot = Quaternion.AngleAxis(yaw, up);
        Vector3 right = yawRot * Vector3.right;
        Quaternion pitchRot = Quaternion.AngleAxis(pitch, right);

        Quaternion finalRot = upAlign * (pitchRot * yawRot);

       
        Vector3 pivot = target.position + target.TransformVector(pivotOffset);
        Vector3 camPos = pivot - finalRot * Vector3.forward * distance;

       
        transform.position = camPos;
        transform.rotation = Quaternion.LookRotation((pivot - camPos).normalized, up);
    }

    private Vector3 GetUpDirection()
    {
        if (usePlayerUp && target != null)
        {
            
            return target.up;
        }

        return customUp.normalized;
    }
}
