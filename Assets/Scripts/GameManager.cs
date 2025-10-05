using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public int deathCount = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AddDeath()
    {
        deathCount++;
    }

    public void TriggerEndSequence()
    {
        StartCoroutine(EndSequence());
    }

    private IEnumerator EndSequence()
    {
        UIManager.Instance.FadeIn();
        yield return new WaitForSeconds(1f);

        // Fixed deprecated method
        EvilManController evilMan = FindFirstObjectByType<EvilManController>();
    }
}
