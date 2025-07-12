using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class EnemyExecutor
{
    private const int DEFENSE_BOOST_AMOUNT = 5;
    private const float MOVE_SPEED = 2f;
    private const float TILE_THRESHOLD = 0.01f;

    // === NEW ===
    // Store all target tiles reserved by enemy units this turn
    private static HashSet<OverlayTile> reservedTargetTilesThisTurn = new HashSet<OverlayTile>();

    /// <summary>
    /// Main entry point for EnemyTurn execution phase.
    /// </summary>
    public static IEnumerator Execute(List<BaseUnit> allUnits)
    {
        Debug.Log("[EnemyExecutor] === Enemy Phase Begin ===");
        
        // Clear the reserved target tiles at the start of each enemy turn
        reservedTargetTilesThisTurn.Clear();

        foreach (BaseUnit unit in allUnits)
        {
            if (unit.teamType != UnitTeam.Enemy)
                continue;

            Debug.Log($"[EnemyExecutor] Planning action for {unit.name}");

            yield return TurnSystem.Instance.StartCoroutine(
                PlanEnemyAction(unit, UnitDeployManager.Instance.GetAllDeployedPlayerUnits())
            );

            // Movement phase
            if (unit.plannedPath != null && unit.plannedPath.Count > 0)
            {
                yield return TurnSystem.Instance.StartCoroutine(MoveUnitAlongPath(unit));
            }

            // Execute the planned action
            yield return TurnSystem.Instance.StartCoroutine(ExecutePlannedAction(unit));

            // Mark as finished
            unit.hasFinishedAction = true;

            // Clear planning fields
            unit.plannedPath.Clear();
            unit.plannedAction = PlannedAction.None;
            unit.targetUnit = null;
        }

        Debug.Log("[EnemyExecutor] === Enemy Phase Complete ===");

        // Advance to next phase
        TurnSystem.Instance.NextPhase();
    }
    
    // Filter out any candidate tiles already reserved by other enemy units this turn
    private static List<OverlayTile> FilterReservedTargetTiles(List<OverlayTile> candidateTiles)
    {
        return candidateTiles
            .Where(tile => !reservedTargetTilesThisTurn.Contains(tile))
            .ToList();
    }

    // Decide plannedAction and plannedPath for an enemy unit
    private static IEnumerator PlanEnemyAction(BaseUnit enemy, List<BaseUnit> allUnits)
    {
        Debug.Log($"[EnemyExecutor] - AI Planning for {enemy.name}");

        BaseUnit target = FindClosestPlayerUnit(enemy, allUnits);
        if (target == null)
        {
            Debug.Log($"[EnemyExecutor] No player units found. Defaulting to DEFEND.");
            enemy.plannedAction = PlannedAction.Defend;
            yield break;
        }

        RangeFinder rf = new RangeFinder();
        List<OverlayTile> attackRange = rf.GetTilesInRange(enemy, PlannerMode.Attack);
        
        // Filter out tiles already reserved this turn
        List<OverlayTile> availableAttackTiles = FilterReservedTargetTiles(attackRange);

        OverlayTile prepTile = FindValidAttackPrepTile(enemy, target, availableAttackTiles);
        if (prepTile != null)
        {
            // === NEW ===
            // Reserve this tile for this turn
            reservedTargetTilesThisTurn.Add(prepTile);

            PathFinder pf = new PathFinder();
            enemy.plannedPath = pf.FindPath(enemy.standOnTile, prepTile, availableAttackTiles);
            enemy.targetUnit = target;
            enemy.plannedAction = PlannedAction.Attack;
            Debug.Log($"[EnemyExecutor] Decided to ATTACK {target.name} via {prepTile.grid2DLocation}");
            yield break;
        }
        
        // Try to Move closer while avoiding reserved tiles
        List<OverlayTile> moveRange = rf.GetTilesInRange(enemy, PlannerMode.Move);
        List<OverlayTile> availableMoveTiles = FilterReservedTargetTiles(moveRange);

        OverlayTile moveTile = FindClosestTileTowardsTarget(enemy, target, availableMoveTiles);
        if (moveTile != null)
        {
            // === NEW ===
            // Reserve this tile for this turn
            reservedTargetTilesThisTurn.Add(moveTile);

            PathFinder pf = new PathFinder();
            enemy.plannedPath = pf.FindPath(enemy.standOnTile, moveTile, availableMoveTiles);
            enemy.plannedAction = PlannedAction.None;
            Debug.Log($"[EnemyExecutor] Decided to MOVE to {moveTile.grid2DLocation}");
            yield break;
        }

        enemy.plannedAction = PlannedAction.Defend;
        Debug.Log($"[EnemyExecutor] Decided to DEFEND in place.");
    }

    // Move the unit along its planned path
    private static IEnumerator MoveUnitAlongPath(BaseUnit unit)
    {
        foreach (OverlayTile tile in unit.plannedPath)
        {
            Vector3 targetPos = tile.transform.position;

            while (Vector2.Distance(unit.transform.position, targetPos) > TILE_THRESHOLD)
            {
                unit.transform.position = Vector3.MoveTowards(unit.transform.position, targetPos, MOVE_SPEED * Time.deltaTime);
                yield return null;
            }

            unit.transform.position = targetPos;
            unit.standOnTile = tile;

            Debug.Log($"[EnemyExecutor] {unit.name} moved to tile {tile.grid2DLocation}");
        }
    }

    // Actually execute the planned action (Attack / Defend)
    private static IEnumerator ExecutePlannedAction(BaseUnit unit)
    {
        switch (unit.plannedAction)
        {
            case PlannedAction.None:
                Debug.Log($"[EnemyExecutor] {unit.name} has no action.");
                break;

            case PlannedAction.Defend:
                unit.defensePower += DEFENSE_BOOST_AMOUNT;
                unit.tempDefenseBonus = DEFENSE_BOOST_AMOUNT;
                Debug.Log($"[EnemyExecutor] {unit.name} is DEFENDING. Defense +{DEFENSE_BOOST_AMOUNT}.");
                break;

            case PlannedAction.Attack:
                if (unit.targetUnit != null)
                {
                    if (unit.visual != null)
                    {
                        var animator = unit.visual.GetComponent<AttackAnimator>();
                        if (animator != null)
                        {
                            Debug.Log($"[EnemyExecutor] Playing AttackAnimator on {unit.name}");
                            yield return TurnSystem.Instance.StartCoroutine(animator.PlayAttackAnimation());
                        }
                    }

                    int damage = Mathf.Max(unit.attackPower - unit.targetUnit.defensePower, 1);
                    unit.targetUnit.health -= damage;
                    Debug.Log($"[EnemyExecutor] {unit.name} attacks {unit.targetUnit.name} for {damage} damage. Target HP now {unit.targetUnit.health}.");

                    unit.targetUnit.UpdateHealthBar();
                    CameraShake.Instance.Shake();

                    if (unit.targetUnit.health <= 0)
                    {
                        Debug.Log($"[EnemyExecutor] {unit.targetUnit.name} has died.");
                        yield return TurnSystem.Instance.StartCoroutine(unit.targetUnit.DieAndRemove());
                    }
                }
                else
                {
                    Debug.Log($"[EnemyExecutor] {unit.name} planned attack but has no target.");
                }
                break;
        }
    }

    // Find the closest player unit on the map
    private static BaseUnit FindClosestPlayerUnit(BaseUnit enemy, List<BaseUnit> allUnits)
    {
        BaseUnit closest = null;
        int minDistance = int.MaxValue;

        foreach (var unit in allUnits)
        {
            if (unit.teamType != UnitTeam.Player || unit.standOnTile == null)
                continue;

            int dist = Mathf.Abs(enemy.standOnTile.grid2DLocation.x - unit.standOnTile.grid2DLocation.x)
                     + Mathf.Abs(enemy.standOnTile.grid2DLocation.y - unit.standOnTile.grid2DLocation.y);

            if (dist < minDistance)
            {
                minDistance = dist;
                closest = unit;
            }
        }

        return closest;
    }

    // Find a valid attack prep tile adjacent to the target
    private static OverlayTile FindValidAttackPrepTile(BaseUnit enemy, BaseUnit target, List<OverlayTile> candidateTiles)
    {
        foreach (OverlayTile tile in candidateTiles)
        {
            var neighbors = MapManager.Instance.GetSurroundingTilesEightDirections(tile.grid2DLocation);
            if (neighbors.Contains(target.standOnTile))
            {
                return tile;
            }
        }
        return null;
    }

    // Find the reachable tile that brings the enemy closest to the target
    private static OverlayTile FindClosestTileTowardsTarget(BaseUnit enemy, BaseUnit target, List<OverlayTile> candidateTiles)
    {
        OverlayTile bestTile = null;
        int bestDist = int.MaxValue;

        foreach (var tile in candidateTiles)
        {
            if (tile.isBlocked || tile.isTempBlocked)
                continue;

            int dist = Mathf.Abs(tile.grid2DLocation.x - target.standOnTile.grid2DLocation.x)
                     + Mathf.Abs(tile.grid2DLocation.y - target.standOnTile.grid2DLocation.y);

            if (dist < bestDist)
            {
                bestDist = dist;
                bestTile = tile;
            }
        }

        return bestTile;
    }
}