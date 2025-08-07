using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Handles fade-in / fade-out and Start / Quit buttons on the Start Menu.
public class StartMenuController : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string battleSceneName = "BattleTestLevel"; // Name in Build Settings

    [Header("Fade Settings")]
    [SerializeField] private CanvasGroup fadeGroup; // Drag FadeOverlay here
    [SerializeField] private float fadeDuration = 0.3f; // Seconds

    private void Start()
    {
        // Fade-in on load
        if (fadeGroup != null) StartCoroutine(Fade(1f, 0f, fadeDuration));
    }

    // Called by StartButton
    public void OnStartPressed()
    {
        if (fadeGroup == null)
        {
            SceneManager.LoadScene(battleSceneName);
            return;
        }
        StartCoroutine(LoadSceneWithFade(battleSceneName));
    }

    // Called by QuitButton
    public void OnQuitPressed()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Stop play mode
#else
        Application.Quit();
#endif
    }

    // ───────────────────── Fade helpers ─────────────────────
    private IEnumerator LoadSceneWithFade(string scene)
    {
        yield return Fade(0f, 1f, fadeDuration); // Fade-out
        SceneManager.LoadScene(scene);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        fadeGroup.alpha = from;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            fadeGroup.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        fadeGroup.alpha = to;
    }
}
