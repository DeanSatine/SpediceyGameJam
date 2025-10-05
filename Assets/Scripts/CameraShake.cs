using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;
    private Vector3 originalPos;

    private void Awake()
    {
        Instance = this;
        originalPos = transform.localPosition;
    }

    public void Shake(float duration, float magnitude)
    {
        StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            transform.localPosition = originalPos + Random.insideUnitSphere * magnitude;
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = originalPos;
    }
}
