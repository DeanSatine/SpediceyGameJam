using UnityEngine;

public class PlatformGenerator : MonoBehaviour
{
    [Header("Platform Settings")]
    public GameObject platformPrefab;   // Your platform prefab
    public float targetHeight = 100f;   // How tall you want the level to go
    public float minY = 1.5f;           // Minimum vertical spacing
    public float maxY = 3f;             // Maximum vertical spacing
    public float spreadX = 5f;          // Horizontal spread range (X axis)
    public float spreadZ = 5f;          // Horizontal spread range (Z axis)

    private float highestY = 0f;

    void Start()
    {
        highestY = transform.position.y;

        while (highestY < targetHeight)
        {
            SpawnPlatform();
        }
    }

    void SpawnPlatform()
    {
        float y = highestY + Random.Range(minY, maxY);
        float x = Random.Range(-spreadX, spreadX);
        float z = Random.Range(-spreadZ, spreadZ);

        Vector3 spawnPos = new Vector3(x, y, z);
        Instantiate(platformPrefab, spawnPos, Quaternion.identity);

        highestY = y;
    }
}
