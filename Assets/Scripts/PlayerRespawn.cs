using UnityEngine;
using TMPro; // Make sure you have TextMeshPro installed

public class PlayerRespawn : MonoBehaviour
{
    [Header("Respawn Settings")]
    public Transform spawnPoint;          // Where the player respawns
    public GameObject platformPrefab;     // Platform to spawn when the player "dies"

    [Header("UI")]
    public TextMeshProUGUI deathCounterText; // Assign in Inspector

    private CharacterController controller;
    private int deathCount = 0;

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

        UpdateUI();
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

        // Increase death count and update UI
        deathCount++;
        UpdateUI();

        // Move player back to spawn point
        controller.enabled = false;
        transform.position = spawnPoint.position;
        controller.enabled = true;
    }

    void UpdateUI()
    {
        if (deathCounterText != null)
        {
            deathCounterText.text = "Deaths: " + deathCount;
        }
    }
}
