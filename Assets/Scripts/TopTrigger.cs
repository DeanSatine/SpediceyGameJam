using UnityEngine;

public class TopTrigger : MonoBehaviour
{
    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Trigger entered by: {other.name}, Tag: {other.tag}");

        if (hasTriggered) return;
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player detected! Starting evil man sequence...");
            hasTriggered = true;

            // Fixed deprecated method
            EvilManController evilMan = FindFirstObjectByType<EvilManController>();
            if (evilMan != null)
            {
                Debug.Log("EvilMan found, calling StartDialogue()");
                evilMan.StartDialogue();
            }
            else
            {
                Debug.LogError("EvilManController not found!");
            }
        }
    }
}
