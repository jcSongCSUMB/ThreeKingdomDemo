using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleResultUIController : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite victorySprite;
    [SerializeField] private Sprite defeatSprite;

    [Header("UI Refs")]
    [SerializeField] private Image cardImage;
    [SerializeField] private TMP_Text lossText;
    [SerializeField] private CanvasGroup cg;
    [SerializeField] private Button restartBtn;
    [SerializeField] private Button quitBtn;

    [Header("Scenes")]
    [SerializeField] private string startMenuSceneName = "StartMenu";        // kept for backward compat (unused now)
    [SerializeField] private string returnRpgSceneName = "TestSimpleRPG";     // target RPG scene name

    private void Awake()
    {
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        restartBtn.onClick.AddListener(OnRestart);
        quitBtn.onClick.AddListener(OnQuit);
    }

    public void Show(BattleOutcome outcome, in BattleStats stats)
    {
        // TEMP DEBUG
        Debug.Log($"[UI/SHOW] outcome={outcome}, lossTextPreview=Lost {stats.playerUnitsLost} / {stats.enemyUnitsLost}");

        cardImage.sprite = outcome == BattleOutcome.Victory ? victorySprite : defeatSprite;
        lossText.SetText($"Lost {stats.playerUnitsLost} / {stats.enemyUnitsLost}");
        StartCoroutine(FadeIn());
    }

    private System.Collections.IEnumerator FadeIn()
    {
        cg.blocksRaycasts = true;
        cg.interactable   = true;

        float t = 0f, dur = 0.5f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(0f, 1f, t / dur);
            yield return null;
        }
        cg.alpha = 1f;
    }

    private void OnRestart() =>
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);

    // load configurable RPG scene instead of StartMenu
    private void OnQuit() =>
        UnityEngine.SceneManagement.SceneManager.LoadScene(returnRpgSceneName);
}