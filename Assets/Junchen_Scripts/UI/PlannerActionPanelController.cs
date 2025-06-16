using UnityEngine;

public class PlannerActionPanelController : MonoBehaviour
{
    public GameObject panel; // Reference to the planner action panel GameObject

    // Show the action button panel
    public void Show()
    {
        panel.SetActive(true);
    }

    // Hide the panel (e.g., when unit is deselected)
    public void Hide()
    {
        panel.SetActive(false);
    }

    // Button click events
    public void OnMoveClicked()
    {
        TileClickPathPlanner.Instance.SetPlannerMode(PlannerMode.Move);
    }

    public void OnAttackClicked()
    {
        TileClickPathPlanner.Instance.SetPlannerMode(PlannerMode.Attack);
    }

    public void OnDefendClicked()
    {
        TileClickPathPlanner.Instance.SetPlannerMode(PlannerMode.Defend);
    }
}
