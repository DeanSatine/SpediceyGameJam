using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Makes humanoid bones randomly wiggle when the object is moved.
/// </summary>
[RequireComponent(typeof(Animator))]
public class RagdollonMove : MonoBehaviour
{
    [Header("Movement Detection")]
    [Tooltip("How far the root must move to trigger wobbly motion.")]
    public float movementThreshold = 0.05f;

    [Header("Wobble Settings")]
    [Tooltip("How strong the bone rotations are.")]
    public float wobbleStrength = 15f;

    [Tooltip("How quickly bones return to normal.")]
    public float smoothSpeed = 5f;

    [Tooltip("List of bones that will wiggle. Leave empty to auto-detect all human bones.")]
    public List<Transform> bonesToWobble = new List<Transform>();

    private Animator animator;
    private Vector3 lastPosition;
    private Dictionary<Transform, Quaternion> originalRotations = new Dictionary<Transform, Quaternion>();
    private float wobbleTimer;

    void Start()
    {
        animator = GetComponent<Animator>();
        lastPosition = transform.position;

        // Auto-detect bones if not set
        if (bonesToWobble.Count == 0 && animator.isHuman)
        {
            foreach (HumanBodyBones bone in System.Enum.GetValues(typeof(HumanBodyBones)))
            {
                if (bone == HumanBodyBones.LastBone) continue;
                Transform t = animator.GetBoneTransform(bone);
                if (t != null) bonesToWobble.Add(t);
            }
        }

        // Save starting rotations
        foreach (var bone in bonesToWobble)
        {
            if (bone != null && !originalRotations.ContainsKey(bone))
                originalRotations[bone] = bone.localRotation;
        }
    }

    void Update()
    {
        float moved = Vector3.Distance(transform.position, lastPosition);
        lastPosition = transform.position;

        if (moved > movementThreshold)
        {
            wobbleTimer = 0.5f; // seconds of wobble after movement
        }

        if (wobbleTimer > 0f)
        {
            wobbleTimer -= Time.deltaTime;

            foreach (var bone in bonesToWobble)
            {
                if (bone == null) continue;

                Quaternion targetRot = originalRotations[bone];
                Quaternion randomOffset = Quaternion.Euler(
                    Mathf.Sin(Time.time * 10f + bone.GetInstanceID()) * wobbleStrength,
                    Mathf.Cos(Time.time * 8f + bone.GetInstanceID()) * wobbleStrength,
                    Mathf.Sin(Time.time * 6f + bone.GetInstanceID()) * wobbleStrength
                );

                bone.localRotation = Quaternion.Slerp(bone.localRotation, targetRot * randomOffset, Time.deltaTime * smoothSpeed);
            }
        }
        else
        {
            // return bones to normal when idle
            foreach (var bone in bonesToWobble)
            {
                if (bone == null) continue;
                bone.localRotation = Quaternion.Slerp(bone.localRotation, originalRotations[bone], Time.deltaTime * smoothSpeed);
            }
        }
    }
}
