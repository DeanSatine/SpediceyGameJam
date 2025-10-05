using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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
    public float zoomSpeed = 1.5f;
    public float zoomSmoothTime = 0.2f;
    public float minDistance = 2f;
    public float maxDistance = 8f;

    [Header("Smoothing")]
    public float followSmoothTime = 0.1f;

    [Header("Fade Settings")]
    public Image fadeImage;
    public float fadeDuration = 2f;

    [Header("Cinematic Settings")]
    public float cinematicZoomDistance = 2.5f;   // how close camera gets during hit
    public float cinematicFollowTightness = 0.03f;
    public float cinematicDuration = 2.0f;       // how long before it returns to normal

    private float yaw;
    private float pitch = 20f;
    private float targetDistance;
    private float currentDistance;
    private Vector3 currentVelocity;

    private bool canControl = false;
    private bool inCinematicMode = false;

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

        if (fadeImage != null)
            StartCoroutine(FadeInRoutine());
        else
            canControl = true;
    }

    IEnumerator FadeInRoutine()
    {
        Color color = fadeImage.color;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            fadeImage.color = color;
            yield return null;
        }

        color.a = 0f;
        fadeImage.color = color;
        fadeImage.gameObject.SetActive(false);
        canControl = true;
    }

    void LateUpdate()
    {
        if (target == null || !canControl) return;

        HandleRotation();
        HandleZoom();
        FollowTarget();
    }

    void HandleRotation()
    {
        if (inCinematicMode) return; // disable manual input during cinematic

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    void HandleZoom()
    {
        if (inCinematicMode) return; // no zoom control during cinematic

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

        transform.position = Vector3.SmoothDamp(
            transform.position, desiredPos, ref currentVelocity,
            inCinematicMode ? cinematicFollowTightness : followSmoothTime
        );

        transform.LookAt(target.position + Vector3.up * offset.y);
    }

    // 🔥 Public method to trigger the cinematic follow
    public void TriggerCinematicFollow(float duration = -1f)
    {
        if (inCinematicMode) return;
        StartCoroutine(CinematicFollowRoutine(duration > 0 ? duration : cinematicDuration));
    }

    private IEnumerator CinematicFollowRoutine(float duration)
    {
        inCinematicMode = true;

        float startDistance = currentDistance;
        float elapsed = 0f;

        // Zoom in quickly
        while (elapsed < 0.4f)
        {
            elapsed += Time.deltaTime;
            currentDistance = Mathf.Lerp(startDistance, cinematicZoomDistance, elapsed / 0.4f);
            yield return null;
        }

        // Hold close-up for duration
        yield return new WaitForSeconds(duration);

        // Smoothly return to normal
        elapsed = 0f;
        while (elapsed < 0.6f)
        {
            elapsed += Time.deltaTime;
            currentDistance = Mathf.Lerp(cinematicZoomDistance, startDistance, elapsed / 0.6f);
            yield return null;
        }

        inCinematicMode = false;
    }
}
