using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class MenuManager : MonoBehaviour
{
    [Header("Buttons")]
    public Button playButton;
    public Button quitButton;

    [Header("Audio (Optional)")]
    public AudioSource uiAudioSource;
    public AudioClip clickSound;
    public AudioClip hoverSound;

    [Header("Scene Settings")]
    public string gameSceneName = "GameScene";

    [Header("Hover Animation Settings")]
    public float hoverScale = 1.15f;      // how big the button grows when hovered
    public float hoverSpeed = 6f;         // how fast the scale animates
    public Color hoverColor = Color.yellow;
    public bool useHoverColor = true;

    private Dictionary<Button, Vector3> originalScales = new Dictionary<Button, Vector3>();
    private Dictionary<Button, Color> originalColors = new Dictionary<Button, Color>();

    void Start()
    {
        // Initialize and assign listeners
        SetupButton(playButton);
        SetupButton(quitButton);
    }

    private void SetupButton(Button button)
    {
        if (button == null) return;

        // Store original scale and color
        originalScales[button] = button.GetComponent<RectTransform>().localScale;
        originalColors[button] = button.image.color;

        // Add click functionality
        if (button == playButton)
            button.onClick.AddListener(PlayGame);
        else if (button == quitButton)
            button.onClick.AddListener(QuitGame);

        // Add hover events
        AddHoverEvents(button);
    }

    public void PlayGame()
    {
        PlayClickSound();
        SceneManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
        PlayClickSound();
        Debug.Log("Quit requested");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

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

    // -------------------------------------------------
    // 🔹 HOVER EFFECT HANDLER
    // -------------------------------------------------
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
            t += Time.deltaTime * hoverSpeed;
            rect.localScale = Vector3.Lerp(startScale, targetScale, t);
            button.image.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        rect.localScale = targetScale;
        button.image.color = targetColor;
    }
}
