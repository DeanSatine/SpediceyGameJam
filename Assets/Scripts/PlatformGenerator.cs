using UnityEngine;

public class PlatformGenerator : MonoBehaviour
{
    [Header("Platform Settings")]
    public GameObject platformPrefab;
    public float targetHeight = 100f;
    public float minY = 1.5f;
    public float maxY = 3f;

    [Header("Mountain Reference")]
    public Transform mountainTransform; // Assign your mountain_Snow_000 here
    public float mountainRadius = 15f;   // Adjust based on your mountain size
    public float platformDistance = 8f;  // Distance from mountain edge

    [Header("Spawn Pattern")]
    public int platformsPerLevel = 6;
    public float spreadVariation = 3f;

    private float highestY = 0f;

    void Start()
    {
        if (mountainTransform == null)
        {
            mountainTransform = GameObject.Find("mountain_Snow_000")?.transform;
        }

        highestY = transform.position.y;

        while (highestY < targetHeight)
        {
            SpawnPlatformRing();
        }
    }

    void SpawnPlatformRing()
    {
        float y = highestY + Random.Range(minY, maxY);

        for (int i = 0; i < platformsPerLevel; i++)
        {
            float angle = (360f / platformsPerLevel) * i + Random.Range(-30f, 30f);
            float distance = mountainRadius + platformDistance + Random.Range(-spreadVariation, spreadVariation);

            Vector3 offset = new Vector3(
                Mathf.Sin(angle * Mathf.Deg2Rad) * distance,
                0,
                Mathf.Cos(angle * Mathf.Deg2Rad) * distance
            );

            Vector3 spawnPos;
            if (mountainTransform != null)
            {
                spawnPos = mountainTransform.position + offset + Vector3.up * y;
            }
            else
            {
                spawnPos = offset + Vector3.up * y;
            }

            Instantiate(platformPrefab, spawnPos, Quaternion.identity);
        }

        highestY = y;
    }
}
