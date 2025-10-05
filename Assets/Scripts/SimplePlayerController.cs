using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
public class SimpleCharacterController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float jumpHeight = 2.5f;
    public float gravity = -9.81f;
    public float turnSmoothTime = 0.25f;

    [Header("References")]
    public Animator animator;
    public Transform cameraTransform;
    public Transform chestPoint;        // where VFX spawns
    public Transform carryHoldPoint;    // where corpse is held (front of chest)
    public GameObject corpsePrefab;
    public ParticleSystem stabVFX;
    public TextMeshProUGUI promptText;

    [Header("Respawn Settings")]
    public float respawnDelay = 0.5f;   // slight delay after death anim finishes

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isJumping;
    private bool isStabbing;
    private bool holdingCorpse;
    private GameObject heldCorpse;
    private List<GameObject> corpses = new List<GameObject>();
    private string currentState = "";
    private float turnSmoothVelocity;
    private Transform dynamicSpawnPoint; // last death location

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (animator != null)
            animator.applyRootMotion = false;

        // Default spawn is current position
        GameObject defaultSpawn = new GameObject("DynamicSpawnPoint");
        defaultSpawn.transform.position = transform.position;
        dynamicSpawnPoint = defaultSpawn.transform;

        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }

    void Update()
    {
        HandleMovement();
        HandleJump();
        HandleSelfStab();
        HandleCorpseInteraction();
    }

    // --------------------------
    // MOVEMENT
    // --------------------------
    void HandleMovement()
    {
        if (isStabbing) return;

        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 inputDir = new Vector3(x, 0f, z).normalized;
        bool isMoving = inputDir.magnitude >= 0.1f;

        if (isMoving)
        {
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir.normalized * walkSpeed * Time.deltaTime);

            Crossfade(holdingCorpse ? "Walk w Corpse" : "Walk");
        }
        else
        {
            Crossfade("Idle");
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    // --------------------------
    // JUMP
    // --------------------------
    void HandleJump()
    {
        if (isStabbing) return;

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            isJumping = true;
            Crossfade("Jump");
        }

        if (isGrounded && isJumping && velocity.y < 0)
            isJumping = false;
    }

    // --------------------------
    // SELF STAB
    // --------------------------
    void HandleSelfStab()
    {
        if (!isStabbing && Input.GetKeyDown(KeyCode.E))
        {
            StartCoroutine(SelfStabSequence());
        }
    }

    IEnumerator SelfStabSequence()
    {
        isStabbing = true;
        Crossfade("Stab", 0.05f);

        // Play VFX once at chest
        if (stabVFX != null && chestPoint != null)
        {
            ParticleSystem vfx = Instantiate(stabVFX, chestPoint.position, chestPoint.rotation);
            vfx.Play();
            Destroy(vfx.gameObject, vfx.main.duration + 0.5f);
        }

        // Wait until animation fully plays
        yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).IsName("Stab"));
        yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f);

        // Save death position and spawn corpse
        Vector3 deathPos = transform.position;
        dynamicSpawnPoint.position = deathPos; // set new respawn point
        GameObject corpse = Instantiate(corpsePrefab, deathPos, transform.rotation);
        corpses.Add(corpse);

        yield return new WaitForSeconds(respawnDelay);

        // Respawn at last death position
        controller.enabled = false;
        transform.position = dynamicSpawnPoint.position + Vector3.up * 1.2f; // offset to avoid clipping
        controller.enabled = true;

        isStabbing = false;
    }

    // --------------------------
    // CORPSE INTERACTION
    // --------------------------
    void HandleCorpseInteraction()
    {
        if (isStabbing) return;

        if (holdingCorpse && Input.GetKeyDown(KeyCode.Q))
        {
            // Place corpse in direction of camera
            Vector3 forward = cameraTransform.forward;
            forward.y = 0f;
            forward.Normalize();

            Vector3 placePos = transform.position + forward * 1.8f;
            heldCorpse.transform.SetParent(null);
            heldCorpse.transform.position = placePos;
            heldCorpse.transform.rotation = Quaternion.LookRotation(-forward) * Quaternion.Euler(90f, 0f, 0f);

            holdingCorpse = false;
            heldCorpse = null;

            if (promptText != null)
            {
                promptText.text = "";
                promptText.gameObject.SetActive(false);
            }

            Crossfade("Place");
            return;
        }

        // If not holding, find nearest corpse
        GameObject nearest = GetNearestCorpse();
        if (nearest != null)
        {
            float dist = Vector3.Distance(transform.position, nearest.transform.position);
            bool looking = Vector3.Dot(transform.forward, (nearest.transform.position - transform.position).normalized) > 0.8f;

            if (dist < 3f && looking)
            {
                if (promptText != null)
                {
                    promptText.text = "Press Q to pick up corpse";
                    promptText.gameObject.SetActive(true);
                }

                if (Input.GetKeyDown(KeyCode.Q))
                {
                    holdingCorpse = true;
                    heldCorpse = nearest;
                    heldCorpse.transform.SetParent(carryHoldPoint);
                    heldCorpse.transform.localPosition = Vector3.zero;
                    heldCorpse.transform.localRotation = Quaternion.Euler(90f, -90f, -180f); // rotate in arms
                    Crossfade("Idle");
                    if (promptText != null)
                        promptText.text = "Press Q to place corpse";
                }
            }
            else
            {
                if (promptText != null)
                    promptText.gameObject.SetActive(false);
            }
        }
        else if (promptText != null)
        {
            promptText.gameObject.SetActive(false);
        }
    }

    GameObject GetNearestCorpse()
    {
        GameObject nearest = null;
        float minDist = Mathf.Infinity;
        foreach (var c in corpses)
        {
            if (c == null) continue;
            float d = Vector3.Distance(transform.position, c.transform.position);
            if (d < minDist)
            {
                minDist = d;
                nearest = c;
            }
        }
        return nearest;
    }

    // --------------------------
    // ANIMATION CROSSFADE
    // --------------------------
    void Crossfade(string stateName, float blend = 0.1f)
    {
        if (animator == null) return;
        if (currentState == stateName) return;
        animator.CrossFade(stateName, blend);
        currentState = stateName;
    }
}
