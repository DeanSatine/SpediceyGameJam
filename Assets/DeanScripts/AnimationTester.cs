using UnityEngine;

public class AnimationTester : MonoBehaviour
{
    [Header("Animation")]
    public Animator animator;

    [Header("Movement")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float rotationSpeed = 10f;

    private string currentState = "";
    private bool stabTriggered = false;
    private bool deathTriggered = false;
    private Vector3 movement;
    private bool isMoving = false;

    void Start()
    {
        if (animator != null)
            animator.applyRootMotion = false; // Disable root motion for manual movement
    }

    void Update()
    {
        HandleMovementInput();
        HandleAnimationInput();
        UpdateMovement();
    }

    void HandleMovementInput()
    {
        // Get input for movement
        float horizontal = Input.GetAxisRaw("Horizontal"); // A/D keys
        float vertical = Input.GetAxisRaw("Vertical");     // W/S keys

        // Create movement vector
        movement = new Vector3(horizontal, 0f, vertical).normalized;
        isMoving = movement.magnitude > 0.1f;

        // Handle rotation when moving
        if (isMoving)
        {
            Vector3 lookDirection = movement;
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    void UpdateMovement()
    {
        if (stabTriggered || deathTriggered) return; // Don't move during death sequence

        if (isMoving)
        {
            // Determine speed based on input
            bool isRunning = Input.GetKey(KeyCode.LeftShift);
            float currentSpeed = isRunning ? runSpeed : walkSpeed;

            // Move the player
            Vector3 moveVector = movement * currentSpeed * Time.deltaTime;
            transform.Translate(moveVector, Space.World);
        }
    }

    void HandleAnimationInput()
    {
        if (animator == null) return;

        // If currently doing stab/death sequence, check progress
        if (stabTriggered)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

            // Once stab finishes
            if (state.IsName("Stab") && state.normalizedTime >= 1f && !deathTriggered)
            {
                animator.CrossFade("Death", 0.1f);
                deathTriggered = true;
            }
            // Once death finishes
            else if (state.IsName("Death") && state.normalizedTime >= 1f)
            {
                animator.CrossFade("Idle", 0.1f);
                stabTriggered = false;
                deathTriggered = false;
                currentState = "Idle";
            }
            return; // Skip rest of input during sequence
        }

        string newState = "";

        // --- Movement-based animations (overrides manual input) ---
        if (isMoving && !Input.GetKey(KeyCode.Alpha3)) // Normal walking
        {
            bool isRunning = Input.GetKey(KeyCode.LeftShift);
            newState = isRunning ? "Run" : "Walk"; // You can add "Run" animation or use "Walk"
        }

        // --- Manual animation testing (number keys) ---
        else if (Input.GetKey(KeyCode.Alpha2))
            newState = "Walk";
        else if (Input.GetKey(KeyCode.Alpha3))
            newState = "Walk w Corpse";
        else if (Input.GetKey(KeyCode.Alpha7))
            newState = "Jump";

        // --- One-shot triggers ---
        else if (Input.GetKeyDown(KeyCode.Alpha4))
            newState = "Place";
        else if (Input.GetKeyDown(KeyCode.Alpha5))
            newState = "Take Damage 1";
        else if (Input.GetKeyDown(KeyCode.Alpha6))
            newState = "Take Damage 2";

        // --- Stab → Death sequence (E key like in game description) ---
        else if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Alpha8))
        {
            animator.CrossFade("Stab", 0.1f);
            stabTriggered = true;
            deathTriggered = false;
            currentState = "Stab";
            return;
        }

        // --- Default idle ---
        if (string.IsNullOrEmpty(newState) && !isMoving)
            newState = "Idle";

        // --- Switch only when needed ---
        if (newState != currentState && !string.IsNullOrEmpty(newState))
        {
            animator.CrossFade(newState, 0.1f);
            currentState = newState;
        }
    }
}
