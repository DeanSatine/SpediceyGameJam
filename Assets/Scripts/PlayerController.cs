using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 5f;
    public float gravity = -20f;
    public float rotationSmoothTime = 0.1f;

    [Header("Animation")]
    public Animator animator;
    public Transform headTransform;

    [Header("References")]
    public Transform cameraTransform;

    private Vector3 velocity;
    private bool isGrounded;
    private bool isJumping;
    private string currentState = "";
    private float turnSmoothVelocity;
    private float groundCheckDistance = 0.2f;
    private float groundYOffset = 0.1f;
    private LayerMask groundMask;

    void Start()
    {
        groundMask = LayerMask.GetMask("Default", "Ground"); // adjust if needed
        if (animator != null)
            animator.applyRootMotion = false;
    }

    void Update()
    {
        HandleMovement();
        HandleJumpAndGravity();
        HandleSelfStab();
    }

    // ------------------------------------------------------------
    // MOVEMENT (Camera-relative, transform-based)
    // ------------------------------------------------------------
    void HandleMovement()
    {
        // --- Ground check ---
        isGrounded = Physics.Raycast(transform.position + Vector3.up * groundYOffset, Vector3.down, groundCheckDistance, groundMask);

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 move = (forward * z + right * x).normalized;

        bool isMoving = move.magnitude > 0.1f;

        // --- Move the player ---
        if (isMoving)
        {
            transform.position += move * moveSpeed * Time.deltaTime;

            float targetAngle = Mathf.Atan2(move.x, move.z) * Mathf.Rad2Deg;
            float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);

            if (!isJumping)
                Crossfade("Walk");
        }
        else if (isGrounded && !isJumping)
        {
            Crossfade("Idle");
        }

        // --- Subtle head look toward camera direction ---
        if (headTransform != null)
        {
            Vector3 lookDir = cameraTransform.forward;
            lookDir.y = 0f;
            Quaternion headRot = Quaternion.LookRotation(lookDir);
            headTransform.rotation = Quaternion.Slerp(headTransform.rotation, headRot, Time.deltaTime * 5f);
        }
    }

    // ------------------------------------------------------------
    // GRAVITY + JUMP (Transform-based)
    // ------------------------------------------------------------
    void HandleJumpAndGravity()
    {
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = jumpForce;
            isJumping = true;
            Crossfade("Jump");
        }

        velocity.y += gravity * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;

        // --- Grounded check and reset ---
        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = 0f;
            if (isJumping)
            {
                isJumping = false;
                Crossfade("Idle");
            }
        }
    }

    // ------------------------------------------------------------
    // SELF STAB (Can always play)
    // ------------------------------------------------------------
    void HandleSelfStab()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Crossfade("Stab", 0.05f);
        }
    }

    // ------------------------------------------------------------
    // CROSSFADE HELPER
    // ------------------------------------------------------------
    void Crossfade(string stateName, float blend = 0.1f)
    {
        if (animator == null) return;
        if (currentState == stateName) return;

        animator.CrossFade(stateName, blend);
        currentState = stateName;
    }

    // Optional: visualize ground ray in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position + Vector3.up * groundYOffset, transform.position + Vector3.up * groundYOffset + Vector3.down * groundCheckDistance);
    }
}
