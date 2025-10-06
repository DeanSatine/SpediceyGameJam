using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class CutsceneManager : MonoBehaviour
{
    [Header("Cutscene Settings")]
    [Tooltip("How long the cutscene lasts before fading out (seconds).")]
    public float cutsceneDuration = 32f;

    [Header("Fade Settings")]
    public Image fadeImage;               // Assign a full-screen UI Image
    public float fadeDuration = 2f;       // How long the fade lasts (seconds)

    [Header("Scene Settings")]
    public string menuSceneName = "MainMenu";

    private void Start()
    {
        // Ensure fadeImage is visible and transparent at start
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }

        // Start cutscene sequence
        StartCoroutine(CutsceneSequence());
    }

    private IEnumerator CutsceneSequence()
    {
        // Wait for cutscene duration
        yield return new WaitForSeconds(cutsceneDuration);

        // Fade to black
        if (fadeImage != null)
            yield return StartCoroutine(FadeToBlack());

        // Re-enable mouse before switching scenes
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Transition back to menu
        SceneManager.LoadScene(menuSceneName);
    }

    private IEnumerator FadeToBlack()
    {
        float elapsed = 0f;
        Color c = fadeImage.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Clamp01(elapsed / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }

        c.a = 1f;
        fadeImage.color = c;
    }
}
