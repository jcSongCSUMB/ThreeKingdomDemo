using System.Collections;
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

    private void Start()
    {
        // Ensure enemy tiles are marked as turn-blocked at the very first PlayerPlanning phase
        MarkEnemyTilesAsTurnBlocked();
    }

    // Register a unit if not already in the unit list
    public void RegisterUnit(BaseUnit unit)
    {
        if (!allUnits.Contains(unit))
        {
            allUnits.Add(unit);
        }
    }

    // === NEW ===
    // Remove a unit from the master unit list
    public void RemoveUnit(BaseUnit unit)
    {
        if (allUnits.Contains(unit))
        {
            allUnits.Remove(unit);
            Debug.Log($"[TurnSystem] Removed unit: {unit.name}");
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

                // Refresh the list of all player units from UnitDeployManager
                allUnits = UnitDeployManager.Instance.GetAllDeployedPlayerUnits();
                Debug.Log($"[TurnSystem] Updated allUnits list. Count: {allUnits.Count}");

                // Start player execution as Coroutine
                StartPlayerExecutionPhase();
                break;

            case TurnPhase.PlayerExecuting:
                currentPhase = TurnPhase.EnemyTurn;
                Debug.Log("[TurnSystem] === EnemyTurn started ===");

                // Refresh the list of all enemy units from UnitDeployManager
                allUnits = UnitDeployManager.Instance.GetAllDeployedEnemyUnits();
                Debug.Log($"[TurnSystem] Updated allUnits list for EnemyTurn. Count: {allUnits.Count}");

                StartEnemyExecutionPhase();
                break;

            case TurnPhase.EnemyTurn:
                currentPhase = TurnPhase.PlayerPlanning;
                currentTurn++;

                // Clear path and action plans for all player units
                ClearAllPlayerPlans();

                // Remove any temporary defense bonuses applied during previous turn
                RemoveAllTemporaryDefenseBonuses();

                // Clear all tiles temporarily blocked by previous plans
                ClearAllTempBlockedTiles();

                // Clear all tiles marked as turn-blocked (end of full turn)
                ClearAllTurnBlockedTiles();

                // Start of new full turn: mark enemy tiles as turn-blocked
                MarkEnemyTilesAsTurnBlocked();

                // TODO: Reset planning environment for next turn
                break;
        }

        Debug.Log($"[TurnSystem] Turn Phase changed to: {currentPhase}");
    }

    // Start the Coroutine to execute player unit actions
    private void StartPlayerExecutionPhase()
    {
        Debug.Log("[TurnSystem] Starting PlayerExecutionPhase Coroutine");
        StartCoroutine(PlayerExecutor.Execute(allUnits));
    }

    // Start the Coroutine to execute enemy unit actions
    private void StartEnemyExecutionPhase()
    {
        Debug.Log("[TurnSystem] Starting EnemyExecutionPhase Coroutine");
        StartCoroutine(EnemyExecutor.Execute(allUnits));
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

    // Remove temporary defense bonuses at the end of the enemy turn
    private void RemoveAllTemporaryDefenseBonuses()
    {
        foreach (BaseUnit unit in allUnits)
        {
            if (unit.teamType == UnitTeam.Player && unit.tempDefenseBonus > 0)
            {
                unit.defensePower -= unit.tempDefenseBonus;
                Debug.Log($"[TurnSystem] Removed defense bonus for {unit.name}. New defensePower: {unit.defensePower}");
                unit.tempDefenseBonus = 0;
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

    // Mark all tiles occupied by enemy units as temporarily blocked
    private void MarkEnemyTilesAsBlocked()
    {
        foreach (BaseUnit unit in allUnits)
        {
            if (unit.teamType == UnitTeam.Enemy && unit.standOnTile != null)
            {
                unit.standOnTile.isTempBlocked = true;
                unit.standOnTile.tempBlockedByPlanning = true;
                unit.standOnTile.ShowAsTempBlocked();
            }
        }
    }

    // Mark all tiles occupied by enemy units as turn-blocked
    private void MarkEnemyTilesAsTurnBlocked()
    {
        foreach (BaseUnit unit in allUnits)
        {
            if (unit.teamType == UnitTeam.Enemy && unit.standOnTile != null)
            {
                unit.standOnTile.MarkAsTurnBlocked();
            }
        }
    }

    // Clear all turn-blocked flags on all tiles at the start of each full turn
    private void ClearAllTurnBlockedTiles()
    {
        OverlayTile[] allTiles = FindObjectsOfType<OverlayTile>();
        foreach (OverlayTile tile in allTiles)
        {
            tile.UnmarkTurnBlocked();
        }
    }
}