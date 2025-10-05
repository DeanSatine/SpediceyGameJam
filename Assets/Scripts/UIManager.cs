using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    public Image fadePanel;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void FadeIn() => StartCoroutine(FadeRoutine(1, 1f));
    public void FadeOutAndEnd() => StartCoroutine(FadeRoutine(0, 1f, true));

    private IEnumerator FadeRoutine(float targetAlpha, float duration, bool endGame = false)
    {
        Color c = fadePanel.color;
        float startAlpha = c.a;
        float time = 0f;

        while (time < duration)
        {
            c.a = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            fadePanel.color = c;
            time += Time.deltaTime;
            yield return null;
        }

        c.a = targetAlpha;
        fadePanel.color = c;

        if (endGame)
        {
            // Replace with your main menu scene name
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
    }
}
