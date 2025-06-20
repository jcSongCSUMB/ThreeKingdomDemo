using System.Collections.Generic;
using UnityEngine;

public enum TurnPhase
{
    PlayerPlanning,
    PlayerExecuting,
    EnemyTurn
}

public class TurnSystem : MonoBehaviour
{
    public static TurnSystem Instance;

    public bool battleStarted = false;
    public TurnPhase currentPhase = TurnPhase.PlayerPlanning;
    public int currentTurn = 1;

    private List<BaseUnit> allUnits = new List<BaseUnit>();
    private int executingIndex = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Register a unit if not already in the unit list
    public void RegisterUnit(BaseUnit unit)
    {
        if (!allUnits.Contains(unit))
        {
            allUnits.Add(unit);
        }
    }

    // Transition to the next phase of the turn cycle
    public void NextPhase()
    {
        switch (currentPhase)
        {
            case TurnPhase.PlayerPlanning:
                currentPhase = TurnPhase.PlayerExecuting;
                ResetUnitStates(UnitTeam.Player);

                // TODO: Execute all planned actions by player units
                executingIndex = 0;
                break;

            case TurnPhase.PlayerExecuting:
                currentPhase = TurnPhase.EnemyTurn;

                // TODO: Trigger enemy AI logic
                break;

            case TurnPhase.EnemyTurn:
                currentPhase = TurnPhase.PlayerPlanning;
                currentTurn++;

                // Clear path and action plans for all player units
                ClearAllPlayerPlans();

                // Clear all tiles temporarily blocked by previous plans
                ClearAllTempBlockedTiles();

                // TODO: Reset planning environment for next turn
                break;
        }

        Debug.Log($"Turn Phase changed to: {currentPhase}");
    }

    // Reset action status for all units belonging to the given team
    private void ResetUnitStates(UnitTeam team)
    {
        foreach (BaseUnit unit in allUnits)
        {
            if (unit.teamType == team)
            {
                unit.hasFinishedAction = false;
            }
        }
    }

    // Returns true if the current phase is PlayerPlanning
    public bool IsPlanningPhase()
    {
        return currentPhase == TurnPhase.PlayerPlanning;
    }

    // Clear all saved plans (movement, action, target) for player units
    private void ClearAllPlayerPlans()
    {
        foreach (BaseUnit unit in allUnits)
        {
            if (unit.teamType == UnitTeam.Player)
            {
                unit.plannedPath.Clear();
                unit.plannedAction = PlannedAction.None;
                unit.targetUnit = null;
            }
        }
    }

    // Reset all temporarily blocked tiles and restore their default appearance
    private void ClearAllTempBlockedTiles()
    {
        OverlayTile[] allTiles = FindObjectsOfType<OverlayTile>();
        foreach (OverlayTile tile in allTiles)
        {
            if (tile.tempBlockedByPlanning)
            {
                tile.tempBlockedByPlanning = false;
                tile.SetToDefaultSprite();
            }
        }
    }
}