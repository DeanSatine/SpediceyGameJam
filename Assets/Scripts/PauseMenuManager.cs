using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class PauseMenuManager : MonoBehaviour
{
    [Header("UI")]
    public CanvasGroup pauseMenuCanvasGroup;
    public float fadeDuration = 0.3f;

    [Header("Buttons")]
    public Button resumeButton;
    public Button mainMenuButton;
    public Button quitButton;

    [Header("Audio (Optional)")]
    public AudioSource uiAudioSource;
    public AudioClip clickSound;
    public AudioClip hoverSound;

    [Header("Hover Animation Settings")]
    public float hoverScale = 1.15f;
    public float hoverSpeed = 6f;
    public Color hoverColor = Color.yellow;
    public bool useHoverColor = true;

    private bool isPaused;
    private bool isFading;

    private Dictionary<Button, Vector3> originalScales = new Dictionary<Button, Vector3>();
    private Dictionary<Button, Color> originalColors = new Dictionary<Button, Color>();

    void Start()
    {
        if (pauseMenuCanvasGroup != null)
        {
            pauseMenuCanvasGroup.alpha = 0f;
            pauseMenuCanvasGroup.interactable = false;
            pauseMenuCanvasGroup.blocksRaycasts = false;
        }

        SetupButton(resumeButton, ResumeGame);
        SetupButton(mainMenuButton, GoToMainMenu);
        SetupButton(quitButton, QuitGame);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    // -------------------------------------------------
    // 🔹 GAME PAUSE / RESUME
    // -------------------------------------------------
    public void PauseGame()
    {
        if (isFading) return;

        isPaused = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        StartCoroutine(FadeMenu(1f, true));
    }

    public void ResumeGame()
    {
        if (isFading) return;

        isPaused = false;
        StartCoroutine(FadeMenu(0f, false, () =>
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }));
    }

    public void GoToMainMenu()
    {
        PlayClickSound();
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
        PlayClickSound();
        Application.Quit();
    }

    // -------------------------------------------------
    // 🔹 MENU FADE LOGIC
    // -------------------------------------------------
    private IEnumerator FadeMenu(float targetAlpha, bool show, System.Action onComplete = null)
    {
        isFading = true;
        if (pauseMenuCanvasGroup == null)
        {
            isFading = false;
            onComplete?.Invoke();
            yield break;
        }

        float startAlpha = pauseMenuCanvasGroup.alpha;
        float elapsed = 0f;

        if (show)
        {
            pauseMenuCanvasGroup.interactable = true;
            pauseMenuCanvasGroup.blocksRaycasts = true;
        }

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            pauseMenuCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        pauseMenuCanvasGroup.alpha = targetAlpha;

        if (!show)
        {
            pauseMenuCanvasGroup.interactable = false;
            pauseMenuCanvasGroup.blocksRaycasts = false;
        }

        isFading = false;
        onComplete?.Invoke();
    }

    // -------------------------------------------------
    // 🔹 BUTTON SETUP
    // -------------------------------------------------
    private void SetupButton(Button button, UnityEngine.Events.UnityAction onClickAction)
    {
        if (button == null) return;

        originalScales[button] = button.GetComponent<RectTransform>().localScale;
        originalColors[button] = button.image.color;

        button.onClick.AddListener(() =>
        {
            PlayClickSound();
            onClickAction.Invoke();
        });

        AddHoverEvents(button);
    }

    private void AddHoverEvents(Button button)
    {
        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = button.gameObject.AddComponent<EventTrigger>();

        // Pointer Enter
        EventTrigger.Entry entryEnter = new EventTrigger.Entry();
        entryEnter.eventID = EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((_) => StartCoroutine(AnimateButtonHover(button, true)));
        trigger.triggers.Add(entryEnter);

        // Pointer Exit
        EventTrigger.Entry entryExit = new EventTrigger.Entry();
        entryExit.eventID = EventTriggerType.PointerExit;
        entryExit.callback.AddListener((_) => StartCoroutine(AnimateButtonHover(button, false)));
        trigger.triggers.Add(entryExit);
    }

    private IEnumerator AnimateButtonHover(Button button, bool isHovering)
    {
        RectTransform rect = button.GetComponent<RectTransform>();
        Vector3 startScale = rect.localScale;
        Vector3 baseScale = originalScales[button];
        Vector3 targetScale = isHovering ? baseScale * hoverScale : baseScale;

        Color startColor = button.image.color;
        Color targetColor = useHoverColor && isHovering
            ? hoverColor
            : originalColors[button];

        if (isHovering)
            PlayHoverSound();

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * hoverSpeed;
            rect.localScale = Vector3.Lerp(startScale, targetScale, t);
            button.image.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        rect.localScale = targetScale;
        button.image.color = targetColor;
    }

    // -------------------------------------------------
    // 🔹 AUDIO HELPERS
    // -------------------------------------------------
    private void PlayClickSound()
    {
        if (uiAudioSource && clickSound)
            uiAudioSource.PlayOneShot(clickSound);
    }

    private void PlayHoverSound()
    {
        if (uiAudioSource && hoverSound)
            uiAudioSource.PlayOneShot(hoverSound);
    }
}
