using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    [Header("Respawn Settings")]
    public Transform spawnPoint;          // Where the player respawns
    public GameObject platformPrefab;     // Platform to spawn when the player "dies"

    private CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // If no spawn point is set, default to current position
        if (spawnPoint == null)
        {
            GameObject defaultSpawn = new GameObject("DefaultSpawnPoint");
            defaultSpawn.transform.position = transform.position;
            spawnPoint = defaultSpawn.transform;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            DieAndRespawn();
        }
    }

    void DieAndRespawn()
    {
        Vector3 deathPosition = transform.position;

        // Spawn platform at death position
        if (platformPrefab != null)
        {
            Instantiate(platformPrefab, deathPosition, Quaternion.identity);
        }

        // Move player back to spawn point
        controller.enabled = false;  // Temporarily disable to teleport cleanly
        transform.position = spawnPoint.position;
        controller.enabled = true;
    }
}
