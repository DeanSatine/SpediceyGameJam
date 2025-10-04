using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Camera Settings")]
    public Transform player;
    public float mouseSensitivity = 100f;
    public float distanceFromPlayer = 5f;
    public float minVerticalAngle = -20f;
    public float maxVerticalAngle = 80f;

    private float xRotation = 0f;
    private float yRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        // Initial camera rotation based on player's rotation
        Vector3 startRotation = player.eulerAngles;
        yRotation = startRotation.y;
        xRotation = startRotation.x;
    }

    void LateUpdate()
    {
        if (player == null) return;

        // Mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Rotate camera around player
        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minVerticalAngle, maxVerticalAngle);

        // Calculate camera position and rotation
        Quaternion rotation = Quaternion.Euler(xRotation, yRotation, 0f);
        Vector3 negDistance = new Vector3(0f, 0f, -distanceFromPlayer);
        Vector3 position = rotation * negDistance + player.position;

        // Apply to camera
        transform.rotation = rotation;
        transform.position = position;

        // Make player rotate with horizontal camera movement
        player.rotation = Quaternion.Euler(0f, yRotation, 0f);
    }
}