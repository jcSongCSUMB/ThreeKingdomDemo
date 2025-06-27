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

    [Header("Battle Phase Panels")]
    public GameObject phaseControlPanel;       // The right-side PhaseControlPanel shown after deployment

    private void Start()
    {
        // Ensure deploy zone is hidden at the very beginning
        MapManager.Instance.HideDeployZones();

        // Start the opening animation
        StartCoroutine(PlayPrepareSequence());

        // Attach button listeners for Reset and Start
        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetClicked);

        if (startButton != null)
            startButton.onClick.AddListener(OnStartClicked);

        // Ensure the PhaseControlPanel starts hidden
        if (phaseControlPanel != null)
            phaseControlPanel.SetActive(false);
    }

    // Plays the opening UI animation sequence:
    // Step 1: Show prepare text, then fade out
    // Step 2: Fade in unit deployment panel
    private IEnumerator PlayPrepareSequence()
    {
        // Set initial visibility states
        prepareUIGroup.alpha = 1f;
        prepareUIGroup.interactable = false;
        prepareUIGroup.blocksRaycasts = false;

        buttonPanelGroup.alpha = 0f;
        buttonPanelGroup.interactable = false;
        buttonPanelGroup.blocksRaycasts = false;

        // Display prepare text for 2 seconds
        yield return new WaitForSeconds(2f);

        // Gradually fade out the prepare text
        while (prepareUIGroup.alpha > 0f)
        {
            prepareUIGroup.alpha -= Time.deltaTime;
            yield return null;
        }
        prepareUIGroup.alpha = 0f;
        prepareUIGroup.gameObject.SetActive(false);

        // Gradually fade in the deployment button panel
        while (buttonPanelGroup.alpha < 1f)
        {
            buttonPanelGroup.alpha += Time.deltaTime;
            yield return null;
        }
        buttonPanelGroup.alpha = 1f;
        buttonPanelGroup.interactable = true;
        buttonPanelGroup.blocksRaycasts = true;

        // After UI finishes transitioning, trigger enemy deployment
        EnemyDeploymentManager.Instance.AutoDeployEnemies();
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

        // Hide deploy zone after reset
        MapManager.Instance.HideDeployZones();

        Debug.Log("[BattleUI] Reset completed.");
    }

    // Handles Start button click
    private void OnStartClicked()
    {
        // Hide all deployment UI (fade out)
        buttonPanelGroup.alpha = 0f;
        buttonPanelGroup.interactable = false;
        buttonPanelGroup.blocksRaycasts = false;

        // Ensure deploy zone is hidden after transition
        MapManager.Instance.HideDeployZones();

        // Show the battle phase control panel (e.g. NextPhaseButton)
        if (phaseControlPanel != null)
        {
            phaseControlPanel.SetActive(true);
            Debug.Log("[BattleUI] PhaseControlPanel activated.");
        }

        // Mark the battle as started in TurnSystem
        TurnSystem.Instance.battleStarted = true;

        Debug.Log("[BattleUI] Start clicked. Transition to battle phase.");

        // TODO: Add any additional setup for entering the battle phase here
    }
}