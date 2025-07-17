using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// UPDATED 2025-07-17: planning-temp overhaul, batch Temp->Turn, full temp clear, deploy prune hooks.

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

    public void RegisterUnit(BaseUnit unit)
    {
        if (!allUnits.Contains(unit))
        {
            allUnits.Add(unit);
            Debug.Log($"[TurnSystem] Registered unit: {unit.name}");
        }
    }

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
        Debug.Log($"[TurnSystem] === NextPhase called. CurrentPhase={currentPhase} ===");

        switch (currentPhase)
        {
            case TurnPhase.PlayerPlanning:
                // finalize player planning: convert temp blocks to turn-blocks and clean up
                PlayerPlanning_EndTransformTempToTurn();
                currentPhase = TurnPhase.PlayerExecuting;
                Debug.Log("[TurnSystem] Switching to PlayerExecuting phase.");
                allUnits = UnitDeployManager.Instance.GetAllDeployedPlayerUnits();
                StartCoroutine(PlayerExecutor.Execute(allUnits));
                break;

            case TurnPhase.PlayerExecuting:
                currentPhase = TurnPhase.EnemyTurn;
                Debug.Log("[TurnSystem] Switching to EnemyTurn phase.");
                allUnits = UnitDeployManager.Instance.GetAllDeployedEnemyUnits();
                StartCoroutine(EnemyExecutor.Execute(allUnits));
                break;

            case TurnPhase.EnemyTurn:
                currentPhase = TurnPhase.PlayerPlanning;
                currentTurn++;
                Debug.Log("[TurnSystem] Switching to PlayerPlanning phase.");

                // --- Full‑turn housekeeping (enemy list still in allUnits) ---
                ClearAllTempBlocked_FULL();   // ensure no temp carry-over from both sides
                ClearAllTurnBlockedTiles();
                MarkEnemyTilesAsTurnBlocked();

                // --- Switch allUnits to player list for the new planning phase ---
                allUnits = UnitDeployManager.Instance.GetAllDeployedPlayerUnits();
                Debug.Log($"[TurnSystem] Refreshed allUnits for PlayerPlanning. Count: {allUnits.Count}");

                // Debug: log flag before reset
                foreach (var unit in allUnits)
                    Debug.Log($"[TurnSystem] Before Reset - {unit.name}: hasFinishedAction = {unit.hasFinishedAction}");

                ResetUnitStates(UnitTeam.Player);

                // Debug: log flag after reset
                foreach (var unit in allUnits)
                    Debug.Log($"[TurnSystem] After Reset  - {unit.name}: hasFinishedAction = {unit.hasFinishedAction}");

                // Clear previous plans and bonuses for player units
                ClearAllPlayerPlans();
                RemoveAllTemporaryDefenseBonuses();

                // Clear temporary path‑planning blocks (legacy light clear; full clear already run above)
                ClearAllTempBlockedTiles();

                // Rebind standOnTile references to MapManager overlay tiles
                RebindAllUnitsToCurrentTiles();
                break;
        }
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

    // Convert all player planning TEMP blocks into TURN blocks at end of planning phase.
    // Also clear any stray TEMP blocks that are not final destinations.
    private void PlayerPlanning_EndTransformTempToTurn()
    {
        // get current player units (allUnits should already be player units in planning, but re-fetch to be safe)
        List<BaseUnit> players = UnitDeployManager.Instance.GetAllDeployedPlayerUnits();

        // track which tiles become final turn-blocked
        HashSet<OverlayTile> finalTiles = new HashSet<OverlayTile>();

        foreach (var u in players)
        {
            OverlayTile dest = null;

            // plannedPath final tile if exists
            if (u.plannedPath != null && u.plannedPath.Count > 0)
            {
                dest = u.plannedPath[u.plannedPath.Count - 1];
            }
            else
            {
                // no planned move/attack/defend path -> use current standOnTile
                dest = u.standOnTile;
            }

            if (dest == null) continue;

            // clear temp then mark turn block
            dest.UnmarkTempBlocked();
            dest.MarkAsTurnBlocked();
            finalTiles.Add(dest);
        }

        // clear any remaining temp blocks globally (excluding finalTiles already handled above but safe to call)
        OverlayTile[] allTiles = FindObjectsOfType<OverlayTile>();
        foreach (var tile in allTiles)
        {
            if (!finalTiles.Contains(tile) && tile.isTempBlocked)
            {
                tile.UnmarkTempBlocked();
            }
        }
    }

    // Aggressively clear ALL temp-block flags on every tile (used at new planning phase).
    private void ClearAllTempBlocked_FULL()
    {
        OverlayTile[] allTiles = FindObjectsOfType<OverlayTile>();
        foreach (OverlayTile tile in allTiles)
        {
            if (tile.isTempBlocked || tile.tempBlockedByPlanning)
            {
                tile.UnmarkTempBlocked();
            }
        }
    }

    // Reset all temporarily blocked tiles and restore their default appearance (legacy light clear)
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

    // Mark all tiles occupied by enemy units as temporarily blocked (legacy visual helper)
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

    // Rebind all units' standOnTile to MapManager's current overlay tiles
    private void RebindAllUnitsToCurrentTiles()
    {
        foreach (var unit in allUnits)
        {
            if (unit.standOnTile == null) continue;

            Vector2Int gridPos = unit.standOnTile.grid2DLocation;

            if (MapManager.Instance.map.TryGetValue(gridPos, out OverlayTile newTile))
            {
                unit.standOnTile = newTile;
            }
        }
    }
}