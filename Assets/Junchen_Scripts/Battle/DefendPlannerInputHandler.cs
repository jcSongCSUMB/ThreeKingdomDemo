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

        // Optional: clear previous planned path (null-safe)
        if (unit.plannedPath != null) unit.plannedPath.Clear();

        // Mark action type
        unit.plannedAction = PlannedAction.Defend;

        // Planning-phase: TEMP only (Turn will be built at phase end)
        if (unit.standOnTile != null)
        {
            unit.standOnTile.MarkAsTempBlocked();
            // removed: unit.standOnTile.MarkAsTurnBlocked(); // avoid polluting reachable/prep tiles across turns

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

        // Reset planner mode to None (now allowed without a selected unit)
        planner.SetPlannerMode(PlannerMode.None);
    }
}