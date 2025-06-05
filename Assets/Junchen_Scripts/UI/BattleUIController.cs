using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleUIController : MonoBehaviour
{
    [Header("CanvasGroups for UI transitions")]
    public CanvasGroup prepareUIGroup;         // The central "Prepare for battle!" text
    public CanvasGroup buttonPanelGroup;       // The entire unit deployment button panel

    [Header("Control buttons")]
    public Button resetButton;                 // Resets all deployed units and buttons
    public Button startButton;                 // Starts battle phase (UI hide only)

    private void Start()
    {
        StartCoroutine(PlayPrepareSequence());

        // Attach button listeners
        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetClicked);

        if (startButton != null)
            startButton.onClick.AddListener(OnStartClicked);
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

    // Handles Reset button click
    private void OnResetClicked()
    {
        // Destroy all deployed units
        UnitDeployManager.Instance.ClearAllDeployedUnits();

        // Unblock all overlay tiles
        foreach (var tile in MapManager.Instance.map.Values)
        {
            tile.Unblock();
        }

        // Reset all unit deployment buttons
        UnitButtonController[] allButtons = FindObjectsOfType<UnitButtonController>();
        foreach (var btn in allButtons)
        {
            btn.ResetButton();  // You must implement this method in UnitButtonController
        }

        Debug.Log("[BattleUI] Reset completed.");
    }

    // Handles Start button click
    private void OnStartClicked()
    {
        // Hide all deployment UI (fade out)
        buttonPanelGroup.alpha = 0f;
        buttonPanelGroup.interactable = false;
        buttonPanelGroup.blocksRaycasts = false;

        // Placeholder for actual battle logic
        Debug.Log("[BattleUI] Start clicked. Transition to battle phase.");
    }
}
