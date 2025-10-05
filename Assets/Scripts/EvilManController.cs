using System.Collections;
using UnityEngine;
using TMPro;

public class EvilManController : MonoBehaviour
{
    [Header("Components")]
    public Animator animator;
    public Transform player;
    public AudioSource punchSound;
    public ParticleSystem soulAbsorbFX;
    public TextMeshProUGUI dialogueText;

    [Header("Movement")]
    public float jumpSpeed = 5f;
    public float jumpHeight = 2f;

    [Header("VFX")]
    public ParticleSystem punchVFX;
    public ParticleSystem[] additionalPunchVFX;

    [Header("Audio")]
    public AudioSource jumpSound;
    public AudioSource soulAbsorbSound;

    [Header("Combat Settings")]
    public float punchDistance = 2.5f; // Distance in front of player
    public float knockbackForce = 25f; // horizontal
    public float knockbackUpForce = 35f; // vertical
    public float rapidPunchDelay = 0.3f;

    private bool isPerformingSequence = false;
    private AnimationTester playerMovement;
    private Animator playerAnimator;
    private Rigidbody playerRigidbody;

    void Start()
    {
        // Cache player components
        if (player != null)
        {
            playerMovement = player.GetComponent<AnimationTester>();
            playerAnimator = player.GetComponent<Animator>();
            playerRigidbody = player.GetComponent<Rigidbody>();
        }
    }

    public void StartDialogue()
    {
        if (isPerformingSequence) return;

        Debug.Log("EvilMan: Starting dialogue sequence");
        StartCoroutine(FullCombatSequence());
    }

    private IEnumerator FullCombatSequence()
    {
        isPerformingSequence = true;

        // Disable player movement and set to idle
        DisablePlayerMovement();
        SetPlayerToIdle();

        // Prevent physics drifting while we run the cinematic sequence
        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = true;
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        // Step 1: Jump and land DIRECTLY in front of player
        Debug.Log("EvilMan: Jumping to player");
        yield return StartCoroutine(JumpInFrontOfPlayer());

        // Step 2: Dialogue
        Debug.Log("EvilMan: Starting dialogue");
        yield return StartCoroutine(SafeDialogue());

        // Step 3: Rapid combat sequence
        Debug.Log("EvilMan: Starting rapid combat sequence");
        yield return StartCoroutine(RapidCombatSequence());

        // Step 4: Cartoon-style final uppercut
        Debug.Log("EvilMan: Final uppercut launch!");
        yield return StartCoroutine(CartoonUppercut());

        // End sequence
        SafeSetDialogueText("Pathetic.");
        yield return new WaitForSeconds(2f);
        SafeEndGame();

        // Safety reset velocities / re-enable movement
        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = false; // ensure physics back to normal
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        EnablePlayerMovement();
        isPerformingSequence = false;
    }

    private IEnumerator JumpInFrontOfPlayer()
    {
        SafeTriggerAnimation("Jump");
        SafePlayAudio(jumpSound);

        if (player != null)
        {
            Vector3 startPos = transform.position;

            // Ensure we land directly in front of where the player is *facing*
            Vector3 targetPos = player.position + (player.forward.normalized * punchDistance);

            // Make Evil Man look toward the player from the target position
            Quaternion targetRotation = Quaternion.LookRotation(player.position - targetPos);

            // Smooth parabolic arc
            float moveTime = 1f;
            float elapsed = 0f;
            Vector3 midPoint = (startPos + targetPos) * 0.5f;
            midPoint.y += jumpHeight;

            while (elapsed < moveTime)
            {
                float t = elapsed / moveTime;
                // Parabolic / bezier-like interpolation
                Vector3 a = Vector3.Lerp(startPos, midPoint, t);
                Vector3 b = Vector3.Lerp(midPoint, targetPos, t);
                transform.position = Vector3.Lerp(a, b, t);

                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Final snap to ensure exact placement/rotation
            transform.position = targetPos;
            transform.rotation = targetRotation;

            // Make player face evil man without changing player's transform position (rotate only)
            if (player != null)
            {
                Vector3 lookDir = (transform.position - player.position).normalized;
                if (lookDir.sqrMagnitude > 0.001f)
                    player.rotation = Quaternion.LookRotation(lookDir);
            }

            Debug.Log($"FINAL: Evil man at {transform.position}, Player at {player.position}, Distance: {Vector3.Distance(transform.position, player.position)}");
        }

        // Landing impact
        SafeCameraShake(0.4f, 0.3f);
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator SafeDialogue()
    {
        SafeSetDialogueText("So... you made it.");
        yield return new WaitForSeconds(2f);

        int corpseCount = CountCorpsesSafe();

        if (corpseCount == 1)
        {
            SafeSetDialogueText("I see you left 1 corpse behind...");
        }
        else if (corpseCount > 1)
        {
            SafeSetDialogueText($"I see you left {corpseCount} corpses behind...");
        }
        else
        {
            SafeSetDialogueText("You somehow made it without dying...");
        }
        yield return new WaitForSeconds(3f);

        SafeSetDialogueText("Those souls belong to me now.");
        yield return new WaitForSeconds(2f);

        // Soul absorption
        SafePlayParticleSystem(soulAbsorbFX);
        SafePlayAudio(soulAbsorbSound);
        yield return new WaitForSeconds(2f);
    }

    private IEnumerator RapidCombatSequence()
    {
        int punchCount = Mathf.Max(CountCorpsesSafe(), 4); // at least 4 rapid hits
        SafeSetDialogueText("Now face your punishment!");
        yield return new WaitForSeconds(0.6f);

        // Move slightly closer for combat (in front of player)
        Vector3 originalPos = transform.position;
        Vector3 combatPos = player.position + (transform.position - player.position).normalized * (punchDistance * 0.7f);

        float moveTime = 0.15f;
        float elapsed = 0f;
        while (elapsed < moveTime)
        {
            float t = elapsed / moveTime;
            transform.position = Vector3.Lerp(originalPos, combatPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Rapid alternating punches (ensures alternation)
        for (int i = 0; i < punchCount; i++)
        {
            // small idle reset to ensure take-damage animations trigger
            SetPlayerToIdle();
            yield return new WaitForSeconds(0.04f);

            yield return StartCoroutine(RapidSyncedPunch(i));
        }

        // Move back to original position
        elapsed = 0f;
        while (elapsed < moveTime)
        {
            float t = elapsed / moveTime;
            transform.position = Vector3.Lerp(transform.position, originalPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPos;
    }

    private IEnumerator RapidSyncedPunch(int index)
    {
        // Alternate between Punch1/Punch2 and Take Damage 1/2
        string punchTrigger = (index % 2 == 0) ? "Punch1" : "Punch2";
        string playerDamage = (index % 2 == 0) ? "Take Damage 1" : "Take Damage 2";

        // Start evil man punch
        SafeTriggerAnimation(punchTrigger);

        // Small timing so the hit lines up with the animation (tweak if needed)
        yield return new WaitForSeconds(0.06f);

        // Trigger the player's damage animation and effects
        SafeTriggerPlayerAnimation(playerDamage);
        SafePlayAudio(punchSound);
        SafePlayParticleSystem(punchVFX);
        SafePlayRandomAdditionalVFX();

        // Camera shake for impact
        float shakeMag = Mathf.Lerp(0.18f, 0.42f, (float)index / 6f);
        SafeCameraShake(0.22f, shakeMag);

        // Pause before next rapid hit
        yield return new WaitForSeconds(rapidPunchDelay);
    }

    private IEnumerator CartoonUppercut()
    {
        SafeSetDialogueText("THIS IS THE END!");
        yield return new WaitForSeconds(0.5f);

        // Wind up animation
        SafeTriggerAnimation("UppercutPrep");
        yield return new WaitForSeconds(0.35f);

        // Re-enable physics for the player just before the final hit
        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = false;
            playerRigidbody.useGravity = true;
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        // Final strike visuals & audio
        SafeTriggerAnimation("Punch2");
        SafeTriggerPlayerAnimation("Take Damage 2");
        SafePlayAudio(punchSound);
        SafePlayParticleSystem(punchVFX);
        SafePlayRandomAdditionalVFX();

        SafeCameraShake(1.2f, 0.8f);

        // Apply a strong upward + backward force to launch the player
        if (playerRigidbody != null)
        {
            // Build a launch vector: mostly up + some back relative to evil man->player
            Vector3 away = (player.position - transform.position).normalized;
            Vector3 launch = Vector3.up * knockbackUpForce - away * knockbackForce;

            // Apply using impulse; also a small extra velocity change to guarantee launch
            playerRigidbody.AddForce(launch, ForceMode.Impulse);
            playerRigidbody.AddForce(Vector3.up * (knockbackUpForce * 0.5f), ForceMode.VelocityChange);

            // Add a little spin for cartoon effect (unfreeze rotation must be enabled in editor)
            playerRigidbody.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);

            Debug.Log($"Applied uppercut launch: {launch}");
        }

        // Give a tiny moment for physics to react then stop player animations on the animated controller
        yield return new WaitForSeconds(0.12f);
        StopPlayerAnimations();

        // Evil man steps back dramatically
        Vector3 originalPos = transform.position;
        Vector3 backPos = originalPos - transform.forward * 2f;
        float moveTime = 0.4f;
        float elapsed = 0f;
        while (elapsed < moveTime)
        {
            float t = elapsed / moveTime;
            transform.position = Vector3.Lerp(originalPos, backPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Let the player fly for dramatic effect
        yield return new WaitForSeconds(2f);

        // Safety: stop drifting velocities if still present
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }
    }

    // Helper methods
    private void DisablePlayerMovement()
    {
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
            Debug.Log("Player movement disabled");
        }
    }

    private void EnablePlayerMovement()
    {
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
            Debug.Log("Player movement re-enabled");
        }

        if (playerAnimator != null)
        {
            // Re-enable animator if we disabled it before
            if (!playerAnimator.enabled)
            {
                playerAnimator.enabled = true;
            }
        }
    }

    private void SetPlayerToIdle()
    {
        if (playerAnimator != null && playerAnimator.enabled)
        {
            playerAnimator.CrossFade("Idle", 0.1f);
            Debug.Log("Player set to Idle animation");
        }
    }

    private void StopPlayerAnimations()
    {
        if (playerAnimator != null)
        {
            playerAnimator.enabled = false;
            Debug.Log("Player animations stopped completely");
        }
    }

    private void SafeTriggerPlayerAnimation(string animationName)
    {
        if (playerAnimator != null && playerAnimator.enabled && !string.IsNullOrEmpty(animationName))
        {
            playerAnimator.CrossFade(animationName, 0.05f);
            Debug.Log($"Player animation triggered: {animationName}");
        }
    }

    private void SafeTriggerAnimation(string triggerName)
    {
        if (animator != null && !string.IsNullOrEmpty(triggerName))
        {
            animator.SetTrigger(triggerName);
            Debug.Log($"Evil man animation triggered: {triggerName}");
        }
    }

    private void SafePlayAudio(AudioSource audioSource)
    {
        if (audioSource != null)
        {
            audioSource.Play();
        }
    }

    private void SafePlayParticleSystem(ParticleSystem particles)
    {
        if (particles != null)
        {
            particles.Play();
        }
    }

    private void SafeSetDialogueText(string text)
    {
        if (dialogueText != null && !string.IsNullOrEmpty(text))
        {
            dialogueText.text = text;
        }
    }

    private void SafeCameraShake(float duration, float magnitude)
    {
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(duration, magnitude);
        }
    }

    private void SafePlayRandomAdditionalVFX()
    {
        if (additionalPunchVFX != null && additionalPunchVFX.Length > 0)
        {
            int randomIndex = Random.Range(0, additionalPunchVFX.Length);
            SafePlayParticleSystem(additionalPunchVFX[randomIndex]);
        }
    }

    private void SafeEndGame()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.FadeOutAndEnd();
        }
    }

    private int CountCorpsesSafe()
    {
        int corpseCount = 3;

        GameObject[] corpses = GameObject.FindGameObjectsWithTag("Corpse");
        if (corpses != null && corpses.Length > 0)
        {
            return corpses.Length;
        }

        if (GameManager.Instance != null)
        {
            return GameManager.Instance.deathCount;
        }

        return corpseCount;
    }
}
