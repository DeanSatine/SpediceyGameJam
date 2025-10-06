using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
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
    public Transform chestPoint;
    public Transform carryHoldPoint;
    public Transform handPoint; // 👈 assign in inspector (for stab weapon)
    public GameObject corpsePrefab;
    public GameObject stabWeaponPrefab;
    public ParticleSystem stabVFX;
    public TextMeshProUGUI promptText;

    [Header("Respawn Settings")]
    public float respawnDelay = 0.5f;

    [Header("Audio Settings")]
    public AudioClip jumpSound;
    public AudioClip landSound;
    public AudioClip stabSound; // 👈 new
    public AudioClip[] footstepSounds;
    public float footstepInterval = 0.4f;
    public float footstepVolume = 0.7f;
    public float stabVolume = 1f; // 👈 new

    [Header("Animation Settings")]
    [Tooltip("How long (seconds) to protect the Jump animation from being overridden.")]
    public float jumpAnimProtectDuration = 0.7f;

    private CharacterController controller;
    private AudioSource audioSource;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isJumping;
    private bool isStabbing;
    private bool holdingCorpse;
    private bool wasGroundedLastFrame;
    private float stepTimer;
    private GameObject heldCorpse;
    private List<GameObject> corpses = new List<GameObject>();
    private string currentState = "";
    private float turnSmoothVelocity;
    private Transform dynamicSpawnPoint;

    // new protection field
    private float jumpAnimProtectUntil = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        if (animator != null)
            animator.applyRootMotion = false;

        GameObject defaultSpawn = new GameObject("DynamicSpawnPoint");
        defaultSpawn.transform.position = transform.position;
        dynamicSpawnPoint = defaultSpawn.transform;

        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (Time.timeScale == 0f) return; // Skip all logic when paused

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

        wasGroundedLastFrame = isGrounded;
        isGrounded = controller.isGrounded;

        if (!wasGroundedLastFrame && isGrounded && landSound != null)
            audioSource.PlayOneShot(landSound, footstepVolume);

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

            HandleFootsteps(isMoving);
        }
        else
        {
            Crossfade("Idle");
            stepTimer = 0f;
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleFootsteps(bool isMoving)
    {
        if (!isGrounded || !isMoving || footstepSounds.Length == 0)
            return;

        stepTimer += Time.deltaTime;
        if (stepTimer >= footstepInterval)
        {
            stepTimer = 0f;
            AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
            audioSource.PlayOneShot(clip, footstepVolume);
        }
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
            Crossfade("Jump", 0.05f);

            // protect the Jump animation for a short window so movement doesn't stomp it
            jumpAnimProtectUntil = Time.time + jumpAnimProtectDuration;

            if (jumpSound != null)
                audioSource.PlayOneShot(jumpSound, 0.9f);
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

        // 🔹 Play stab sound
        if (stabSound != null)
            audioSource.PlayOneShot(stabSound, stabVolume);

        // 🔹 Spawn temporary weapon
        GameObject tempWeapon = null;
        if (stabWeaponPrefab != null && handPoint != null)
        {
            tempWeapon = Instantiate(stabWeaponPrefab, handPoint.position, handPoint.rotation, handPoint);
            tempWeapon.transform.localRotation *= Quaternion.Euler(0f, 180f, 0f);
        }

        // 🔹 Spawn VFX
        if (stabVFX != null && chestPoint != null)
        {
            ParticleSystem vfx = Instantiate(stabVFX, chestPoint.position, chestPoint.rotation);
            vfx.Play();
            Destroy(vfx.gameObject, vfx.main.duration + 0.5f);
        }

        // ✅ Wait for stab animation to start, but fail-safe after 1 second
        float waitTimer = 0f;
        bool started = false;
        while (waitTimer < 1f)
        {
            var info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName("Stab")) { started = true; break; }
            waitTimer += Time.deltaTime;
            yield return null;
        }

        if (!started)
        {
            Debug.LogWarning("⚠️ Stab animation not found — check state name!");
        }

        // ✅ Wait until the stab animation finishes, with a 3-second timeout
        float stabTimer = 0f;
        while (stabTimer < 3f)
        {
            var info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName("Stab") && info.normalizedTime >= 1f)
                break;

            stabTimer += Time.deltaTime;
            yield return null;
        }

        if (tempWeapon != null)
            Destroy(tempWeapon);

        // ✅ Ensure corpse spawns on ground
        Vector3 deathPos = transform.position;
        if (Physics.Raycast(deathPos + Vector3.up, Vector3.down, out RaycastHit hit, 5f))
            deathPos = hit.point;

        dynamicSpawnPoint.position = deathPos;
        GameObject corpse = Instantiate(corpsePrefab, deathPos, transform.rotation);
        corpses.Add(corpse);

        yield return new WaitForSeconds(respawnDelay);

        controller.enabled = false;
        transform.position = dynamicSpawnPoint.position + Vector3.up * 1.2f;
        controller.enabled = true;

        isStabbing = false;
    }

    // --------------------------
    // CORPSE INTERACTION
    // --------------------------
    void HandleCorpseInteraction()
    {
        if (isStabbing) return;

        if (holdingCorpse)
        {
            if (promptText != null)
            {
                promptText.text = "Press Q to place it down";
                promptText.gameObject.SetActive(true);
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                Vector3 forward = cameraTransform.forward;
                forward.y = 0f;
                forward.Normalize();

                Vector3 placePos = transform.position + forward * 1.8f;
                heldCorpse.transform.SetParent(null);
                heldCorpse.transform.position = placePos;
                heldCorpse.transform.rotation = Quaternion.LookRotation(-forward) * Quaternion.Euler(90f, 0f, 0f);

                holdingCorpse = false;
                heldCorpse = null;

                Crossfade("Place", 0.1f);

                if (promptText != null)
                {
                    promptText.text = "";
                    promptText.gameObject.SetActive(false);
                }
                return;
            }
            return;
        }

        GameObject nearest = GetNearestCorpse();
        if (nearest != null)
        {
            float dist = Vector3.Distance(transform.position, nearest.transform.position);

            if (dist < 3f) // 👈 only distance matters now
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

                    // Center corpse between arms
                    heldCorpse.transform.localPosition = new Vector3(0f, 0f, 0.2f);
                    heldCorpse.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

                    Crossfade("Idle");

                    if (promptText != null)
                        promptText.text = "Press Q to place it down";
                }
            }
            else if (promptText != null)
            {
                promptText.gameObject.SetActive(false);
            }
        }
        else if (promptText != null)
            promptText.gameObject.SetActive(false);
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

        // Prevent overriding Jump while jump is active (except for Stab)
        if (currentState == "Jump" && stateName != "Stab")
        {
            bool animStillPlaying = false;
            if (animator != null)
            {
                var s = animator.GetCurrentAnimatorStateInfo(0);
                animStillPlaying = s.IsName("Jump") && s.normalizedTime < 1f;
            }

            if (isJumping || animStillPlaying || Time.time < jumpAnimProtectUntil)
            {
                // keep playing jump until it finishes OR protection window expires
                return;
            }
        }

        animator.CrossFade(stateName, blend);
        currentState = stateName;
    }
}
