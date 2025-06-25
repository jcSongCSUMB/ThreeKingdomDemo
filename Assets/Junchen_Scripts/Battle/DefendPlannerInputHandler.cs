using UnityEngine;

public class DefendPlannerInputHandler : MonoBehaviour
{
    private TileClickPathPlanner planner;

    private void Awake()
    {
        planner = FindObjectOfType<TileClickPathPlanner>();
    }

    private void Update()
    {
        // Only proceed if in Defend planner mode during PlayerPlanning
        if (planner == null || planner.plannerMode != PlannerMode.Defend)
            return;

        if (!TurnSystem.Instance.IsPlanningPhase())
            return;

        BaseUnit unit = UnitSelector.currentUnit;
        if (unit == null)
            return;

        if (unit.hasFinishedAction)
        {
            Debug.LogWarning($"[DefendPlanner] Unit {unit.name} has already completed an action.");
            return;
        }

        // Optional: clear previous planned path
        unit.plannedPath.Clear();

        // Mark action type
        unit.plannedAction = PlannedAction.Defend;

        // Mark current tile visually if needed
        if (unit.standOnTile != null)
        {
            unit.standOnTile.MarkAsTempBlocked();
            unit.standOnTile.MarkAsTurnBlocked();

            Debug.Log($"[DefendPlanner] Unit {unit.name} plans to DEFEND on tile {unit.standOnTile.grid2DLocation}.");
        }
        else
        {
            Debug.LogWarning($"[DefendPlanner] Unit {unit.name} has no valid tile.");
        }

        // Hide action panel
        PlannerActionPanelController panel = FindObjectOfType<PlannerActionPanelController>();
        if (panel != null)
        {
            panel.Hide();
        }

        // Reset planner mode to None
        planner.SetPlannerMode(PlannerMode.None);
    }
}
