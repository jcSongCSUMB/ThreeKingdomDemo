using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// UPDATED 2025-07-20: add TurnBlocked handling in MoveUnitAlongPath
// All other logic unchanged
// UPDATED 2025-08-01: Added safe attack logic and simplified comments
// UPDATED 2025-08-02: log Manhattan distance in ExecutePlannedAction

public static class EnemyExecutor
{
    private const int DEFENSE_BOOST_AMOUNT = 5;
    private const float MOVE_SPEED = 2f;
    private const float TILE_THRESHOLD = 0.01f;

    // Tracks tiles reserved by enemies this turn
    private static HashSet<OverlayTile> reservedTargetTilesThisTurn = new HashSet<OverlayTile>();

    // Runs the entire enemy phase
    public static IEnumerator Execute(List<BaseUnit> allUnits)
    {
        Debug.Log("[EnemyExecutor] Enemy phase begin");

        reservedTargetTilesThisTurn.Clear();

        // Collect live enemy units
        List<BaseUnit> enemyUnits = new List<BaseUnit>();
        foreach (BaseUnit u in allUnits)
        {
            if (u == null || u.gameObject == null) continue;
            if (u.teamType != UnitTeam.Enemy) continue;
            enemyUnits.Add(u);
        }

        foreach (BaseUnit enemy in enemyUnits)
        {
            if (enemy == null || enemy.gameObject == null) continue;

            // Plan action
            List<BaseUnit> players = UnitDeployManager.Instance != null
                ? FilterLivePlayers(UnitDeployManager.Instance.GetAllDeployedPlayerUnits())
                : FilterLivePlayers(Object.FindObjectsOfType<BaseUnit>().Where(p => p.teamType == UnitTeam.Player).ToList());

            yield return TurnSystem.Instance.StartCoroutine(PlanEnemyAction(enemy, players));

            // Move first
            if (enemy.plannedPath != null && enemy.plannedPath.Count > 0)
                yield return TurnSystem.Instance.StartCoroutine(MoveUnitAlongPath(enemy));

            // Execute planned action
            yield return TurnSystem.Instance.StartCoroutine(ExecutePlannedAction(enemy));

            // Reset planning fields
            enemy.hasFinishedAction = true;
            enemy.plannedPath.Clear();
            enemy.plannedAction = PlannedAction.None;
            enemy.targetUnit = null;
        }

        Debug.Log("[EnemyExecutor] Enemy phase complete");
        TurnSystem.Instance.NextPhase();
    }

    // Filters live player units
    private static List<BaseUnit> FilterLivePlayers(List<BaseUnit> input)
    {
        var result = new List<BaseUnit>();
        foreach (var u in input)
            if (u != null && u.gameObject != null && u.teamType == UnitTeam.Player)
                result.Add(u);
        return result;
    }

    // Removes tiles already reserved by others
    private static List<OverlayTile> FilterReservedTargetTiles(List<OverlayTile> candidateTiles) =>
        candidateTiles.Where(t => t != null && !reservedTargetTilesThisTurn.Contains(t)).ToList();

    // Determines enemy plan
    private static IEnumerator PlanEnemyAction(BaseUnit enemy, List<BaseUnit> players)
    {
        BaseUnit target = FindClosestPlayerUnit(enemy, players);
        if (target == null)
        {
            enemy.plannedAction = PlannedAction.Defend;
            yield break;
        }

        RangeFinder rf = new RangeFinder();
        List<OverlayTile> attackRange = rf.GetTilesInRange(enemy, PlannerMode.Attack);
        List<OverlayTile> availableAttackTiles = FilterReservedTargetTiles(attackRange);

        OverlayTile prepTile = FindValidAttackPrepTile(enemy, target, availableAttackTiles);
        if (prepTile != null)
        {
            reservedTargetTilesThisTurn.Add(prepTile);
            PathFinder pf = new PathFinder();
            enemy.plannedPath = pf.FindPath(enemy.standOnTile, prepTile, availableAttackTiles);
            enemy.targetUnit = target;
            enemy.plannedAction = PlannedAction.Attack;
            yield break;
        }

        // Try to move closer
        List<OverlayTile> moveRange = rf.GetTilesInRange(enemy, PlannerMode.Move);
        List<OverlayTile> availableMoveTiles = FilterReservedTargetTiles(moveRange);

        OverlayTile moveTile = FindClosestTileTowardsTarget(enemy, target, availableMoveTiles);
        if (moveTile != null)
        {
            reservedTargetTilesThisTurn.Add(moveTile);
            PathFinder pf = new PathFinder();
            enemy.plannedPath = pf.FindPath(enemy.standOnTile, moveTile, availableMoveTiles);
            enemy.plannedAction = PlannedAction.None;
            yield break;
        }

        enemy.plannedAction = PlannedAction.Defend;
    }

    // Moves unit along its planned path
    private static IEnumerator MoveUnitAlongPath(BaseUnit unit)
    {
        OverlayTile prevTile = unit.standOnTile;
        OverlayTile destTile = prevTile;

        foreach (OverlayTile tile in unit.plannedPath)
        {
            if (tile == null) yield break;

            Vector3 targetPos = tile.transform.position;
            while (Vector2.Distance(unit.transform.position, targetPos) > TILE_THRESHOLD)
            {
                unit.transform.position = Vector3.MoveTowards(unit.transform.position, targetPos, MOVE_SPEED * Time.deltaTime);
                yield return null;
            }

            unit.transform.position = targetPos;
            unit.standOnTile = tile;
            destTile = tile;
        }

        prevTile?.UnmarkTurnBlocked();
        destTile?.MarkAsTurnBlocked();
    }

    // Executes the chosen action
    private static IEnumerator ExecutePlannedAction(BaseUnit unit)
    {
        switch (unit.plannedAction)
        {
            case PlannedAction.None:
                Debug.Log($"[EnemyExecutor] {unit.name} does nothing");
                break;

            case PlannedAction.Defend:
                unit.defensePower += DEFENSE_BOOST_AMOUNT;
                unit.tempDefenseBonus = DEFENSE_BOOST_AMOUNT;
                break;

            case PlannedAction.Attack:
                BaseUnit targetSnap = unit.targetUnit; // local reference
                if (targetSnap == null || targetSnap.gameObject == null)
                {
                    Debug.Log($"[EnemyExecutor] {unit.name} target missing");
                    break;
                }

                // UPDATED 2025-08-02: log Manhattan distance between attacker and target
                int dx = Mathf.Abs(unit.standOnTile.grid2DLocation.x - targetSnap.standOnTile.grid2DLocation.x);
                int dy = Mathf.Abs(unit.standOnTile.grid2DLocation.y - targetSnap.standOnTile.grid2DLocation.y);
                int manhattan = dx + dy;
                Debug.Log($"[EnemyAtkDist] {unit.name}->{targetSnap.name} dist={manhattan} (dx={dx}, dy={dy})");

                // Play attack animation
                if (unit.visual != null)
                {
                    var animator = unit.visual.GetComponent<AttackAnimator>();
                    if (animator != null)
                        yield return TurnSystem.Instance.StartCoroutine(animator.PlayAttackAnimation());
                }

                // Verify target after animation
                if (targetSnap == null || targetSnap.gameObject == null)
                    break;

                int dmg = Mathf.Max(unit.attackPower - targetSnap.defensePower, 1);
                targetSnap.health -= dmg;
                targetSnap.UpdateHealthBar();
                CameraShake.Instance.Shake();

                if (targetSnap.health <= 0)
                    yield return TurnSystem.Instance.StartCoroutine(targetSnap.DieAndRemove());
                break;
        }
    }

    // Finds the closest player unit
    private static BaseUnit FindClosestPlayerUnit(BaseUnit enemy, List<BaseUnit> players)
    {
        if (enemy == null || enemy.standOnTile == null) return null;
        BaseUnit closest = null;
        int minDist = int.MaxValue;

        foreach (var p in players)
        {
            if (p == null || p.standOnTile == null) continue;
            int dist = Mathf.Abs(enemy.standOnTile.grid2DLocation.x - p.standOnTile.grid2DLocation.x)
                     + Mathf.Abs(enemy.standOnTile.grid2DLocation.y - p.standOnTile.grid2DLocation.y);

            if (dist < minDist)
            {
                minDist = dist;
                closest = p;
            }
        }
        return closest;
    }

    // Finds a tile adjacent to the target
    private static OverlayTile FindValidAttackPrepTile(BaseUnit enemy, BaseUnit target, List<OverlayTile> candidates)
    {
        if (target == null || target.standOnTile == null) return null;
        foreach (var t in candidates)
        {
            if (t == null) continue;
            var neigh = MapManager.Instance.GetSurroundingTilesEightDirections(t.grid2DLocation);
            if (neigh.Contains(target.standOnTile))
                return t;
        }
        return null;
    }

    // Finds a move tile closest to the target
    private static OverlayTile FindClosestTileTowardsTarget(BaseUnit enemy, BaseUnit target, List<OverlayTile> candidates)
    {
        if (target == null || target.standOnTile == null) return null;
        OverlayTile best = null;
        int bestDist = int.MaxValue;

        foreach (var t in candidates)
        {
            if (t == null || t.isBlocked || t.isTempBlocked) continue;
            int dist = Mathf.Abs(t.grid2DLocation.x - target.standOnTile.grid2DLocation.x)
                     + Mathf.Abs(t.grid2DLocation.y - target.standOnTile.grid2DLocation.y);

            if (dist < bestDist)
            {
                bestDist = dist;
                best = t;
            }
        }
        return best;
    }
}