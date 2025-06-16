using UnityEngine;

public enum PlannerMode
{
    None,
    Move,
    Attack,
    Defend
}

public class TileClickPathPlanner : MonoBehaviour
{
    public static TileClickPathPlanner Instance;

    public PlannerMode plannerMode = PlannerMode.None;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    // Set the current planner mode based on UI input
    public void SetPlannerMode(PlannerMode mode)
    {
        // Only allow setting during the planning phase
        if (!TurnSystem.Instance.IsPlanningPhase())
        {
            Debug.Log("[Planner] Cannot set mode, not in planning phase");
            return;
        }

        // Only allow setting when a unit is selected
        if (UnitSelector.currentUnit == null)
        {
            Debug.Log("[Planner] No unit selected");
            return;
        }

        plannerMode = mode;
        Debug.Log($"[Planner] Mode set to: {plannerMode}");
    }
}