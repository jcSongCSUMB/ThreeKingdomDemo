using UnityEngine;

/// <summary>
/// Controls the display and button interactions of the planner action panel.
/// This script should be attached to a persistent, active GameObject in the scene (not the panel itself).
/// The panel reference must be manually assigned in the Inspector.
/// </summary>
public class PlannerActionPanelController : MonoBehaviour
{
    // Reference to the PlannerActionPanel (UI object containing the buttons)
    public GameObject panel;

    /// <summary>
    /// Shows the action panel (e.g. after selecting a unit).
    /// </summary>
    public void Show()
    {
        Debug.Log("[PlannerPanel] Show() called");
        panel.SetActive(true);
    }

    /// <summary>
    /// Hides the action panel (e.g. after deselecting a unit).
    /// </summary>
    public void Hide()
    {
        Debug.Log("[PlannerPanel] Hide() called");
        panel.SetActive(false);
    }

    /// <summary>
    /// Called by the Move button. Sets planner mode to Move.
    /// </summary>
    public void OnMoveClicked()
    {
        Debug.Log("[PlannerPanel] Move button clicked");
        FindObjectOfType<TileClickPathPlanner>().SetPlannerMode(PlannerMode.Move);
    }

    /// <summary>
    /// Called by the Attack button. Sets planner mode to Attack.
    /// </summary>
    public void OnAttackClicked()
    {
        Debug.Log("[PlannerPanel] Attack button clicked");
        FindObjectOfType<TileClickPathPlanner>().SetPlannerMode(PlannerMode.Attack);
    }

    /// <summary>
    /// Called by the Defend button. Sets planner mode to Defend.
    /// </summary>
    public void OnDefendClicked()
    {
        Debug.Log("[PlannerPanel] Defend button clicked");
        FindObjectOfType<TileClickPathPlanner>().SetPlannerMode(PlannerMode.Defend);
    }
}