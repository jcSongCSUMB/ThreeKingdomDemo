using System.Collections;
using UnityEngine;

public class BattleUIController : MonoBehaviour
{
    [Header("CanvasGroups for UI transitions")]
    public CanvasGroup prepareUIGroup;         // The central "Prepare for battle!" text
    public CanvasGroup buttonPanelGroup;       // The entire unit deployment button panel

    private void Start()
    {
        StartCoroutine(PlayPrepareSequence());
    }

    // Plays the opening UI animation sequence:
    // Step 1: Show prepare text, then fade out
    // Step 2: Fade in unit deployment panel
    private IEnumerator PlayPrepareSequence()
    {
        // Ensure starting states
        prepareUIGroup.alpha = 1f;
        prepareUIGroup.interactable = false;
        prepareUIGroup.blocksRaycasts = false;

        buttonPanelGroup.alpha = 0f;
        buttonPanelGroup.interactable = false;
        buttonPanelGroup.blocksRaycasts = false;

        // Display prepare text for 2 seconds
        yield return new WaitForSeconds(2f);

        // Fade out prepare text
        while (prepareUIGroup.alpha > 0f)
        {
            prepareUIGroup.alpha -= Time.deltaTime;
            yield return null;
        }
        prepareUIGroup.alpha = 0f;
        prepareUIGroup.gameObject.SetActive(false);

        // Fade in the deployment button panel
        while (buttonPanelGroup.alpha < 1f)
        {
            buttonPanelGroup.alpha += Time.deltaTime;
            yield return null;
        }
        buttonPanelGroup.alpha = 1f;
        buttonPanelGroup.interactable = true;
        buttonPanelGroup.blocksRaycasts = true;
    }
}