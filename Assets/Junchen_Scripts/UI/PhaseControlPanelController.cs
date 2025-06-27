using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the PhaseControlPanel, which contains global battle-phase buttons
/// such as the NextPhaseButton. This panel is activated after the deployment
/// phase and remains visible during the battle loop.
/// </summary>
public class PhaseControlPanelController : MonoBehaviour
{
    [Header("Control Buttons")]
    public Button nextPhaseButton;

    private void Awake()
    {
        // Ensure the NextPhaseButton is assigned
        if (nextPhaseButton != null)
        {
            nextPhaseButton.onClick.AddListener(OnNextPhaseClicked);
        }
        else
        {
            Debug.LogWarning("[PhaseControlPanel] NextPhaseButton is not assigned in Inspector.");
        }
    }

    private void Update()
    {
        // Update button interactivity based on current phase
        if (TurnSystem.Instance == null || nextPhaseButton == null)
            return;

        if (TurnSystem.Instance.currentPhase == TurnPhase.PlayerPlanning)
        {
            if (!nextPhaseButton.interactable)
            {
                nextPhaseButton.interactable = true;
                Debug.Log("[PhaseControlPanel] NextPhaseButton enabled (PlayerPlanning phase).");
            }
        }
        else
        {
            if (nextPhaseButton.interactable)
            {
                nextPhaseButton.interactable = false;
                Debug.Log("[PhaseControlPanel] NextPhaseButton disabled (Not planning phase).");
            }
        }
    }
    
    // Handles NextPhaseButton click event.
    // Calls TurnSystem.Instance.NextPhase() to advance the game phase.
    public void OnNextPhaseClicked()
    {
        Debug.Log("[PhaseControlPanel] NextPhaseButton clicked.");
        TurnSystem.Instance.NextPhase();

        // TODO: Add confirmation popup here in future to prevent accidental phase change
    }

    // TODO: Future expansion
    // You can add more Button references here for global battle commands
    // e.g. public Button surrenderButton;
    // e.g. public Button autoResolveButton;
}

