using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    [Header("Target & Offsets")]
    public Transform target;
    public Vector3 offset = new Vector3(0f, 2f, -4f);

    [Header("Rotation")]
    public float mouseSensitivity = 100f;
    public float minPitch = -30f;
    public float maxPitch = 60f;

    [Header("Zoom")]
    public float zoomSpeed = 1.5f; // reduced sensitivity
    public float zoomSmoothTime = 0.2f;
    public float minDistance = 2f;
    public float maxDistance = 8f;

    [Header("Smoothing")]
    public float followSmoothTime = 0.1f;

    private float yaw;
    private float pitch = 20f;
    private float targetDistance;
    private float currentDistance;
    private Vector3 currentVelocity;

    void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("OrbitCamera: No target assigned!");
            enabled = false;
            return;
        }

        targetDistance = offset.magnitude;
        currentDistance = targetDistance;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (target == null) return;

        HandleRotation();
        HandleZoom();
        FollowTarget();
    }

    void HandleRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            targetDistance -= scroll * zoomSpeed;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }

        currentDistance = Mathf.Lerp(currentDistance, targetDistance, zoomSmoothTime);
    }

    void FollowTarget()
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPos = target.position + rotation * new Vector3(0, offset.y, -currentDistance);

        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref currentVelocity, followSmoothTime);
        transform.LookAt(target.position + Vector3.up * offset.y);
    }
}
