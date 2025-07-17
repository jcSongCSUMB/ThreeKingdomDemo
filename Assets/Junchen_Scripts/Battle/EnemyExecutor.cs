using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// UPDATED 2025-07-17: defensive null filtering (player list + enemy list), guard against destroyed targets,
// never assume standOnTile present; fixes MissingReference when player units die.

public static class EnemyExecutor
{
    private const int DEFENSE_BOOST_AMOUNT = 5;
    private const float MOVE_SPEED = 2f;
    private const float TILE_THRESHOLD = 0.01f;

    // Store all target tiles reserved by enemy units this turn
    private static HashSet<OverlayTile> reservedTargetTilesThisTurn = new HashSet<OverlayTile>();

    // Main entry point for EnemyTurn execution phase
    public static IEnumerator Execute(List<BaseUnit> allUnits)
    {
        Debug.Log("[EnemyExecutor] === Enemy Phase Begin ===");

        // clear reserved target tiles at the start of each enemy turn
        reservedTargetTilesThisTurn.Clear();

        // build a safe local list of enemy units (filter null / destroyed / wrong team)
        List<BaseUnit> enemyUnits = new List<BaseUnit>();
        foreach (BaseUnit u in allUnits)
        {
            if (u == null) continue;
            if (u.teamType != UnitTeam.Enemy) continue;
            if (u.gameObject == null) continue;
            enemyUnits.Add(u);
        }

        foreach (BaseUnit enemy in enemyUnits)
        {
            if (enemy == null || enemy.gameObject == null) continue;

            Debug.Log($"[EnemyExecutor] Planning action for {enemy.name}");

            // fetch current player units (DeployManager already prunes, but double-check for safety)
            List<BaseUnit> players = UnitDeployManager.Instance != null
                ? FilterLivePlayers(UnitDeployManager.Instance.GetAllDeployedPlayerUnits())
                : FilterLivePlayers(Object.FindObjectsOfType<BaseUnit>().Where(p => p.teamType == UnitTeam.Player).ToList());

            yield return TurnSystem.Instance.StartCoroutine(PlanEnemyAction(enemy, players));

            // movement
            if (enemy.plannedPath != null && enemy.plannedPath.Count > 0)
            {
                yield return TurnSystem.Instance.StartCoroutine(MoveUnitAlongPath(enemy));
            }

            // execute action
            yield return TurnSystem.Instance.StartCoroutine(ExecutePlannedAction(enemy));

            // mark finished + clear planning fields
            enemy.hasFinishedAction = true;
            enemy.plannedPath.Clear();
            enemy.plannedAction = PlannedAction.None;
            enemy.targetUnit = null;
        }

        Debug.Log("[EnemyExecutor] === Enemy Phase Complete ===");

        // advance to next phase
        TurnSystem.Instance.NextPhase();
    }

    // filter helper: live player units only
    private static List<BaseUnit> FilterLivePlayers(List<BaseUnit> input)
    {
        List<BaseUnit> result = new List<BaseUnit>();
        foreach (var u in input)
        {
            if (u == null) continue;
            if (u.teamType != UnitTeam.Player) continue;
            if (u.gameObject == null) continue;
            result.Add(u);
        }
        return result;
    }

    // filter out any candidate tiles already reserved by other enemy units this turn
    private static List<OverlayTile> FilterReservedTargetTiles(List<OverlayTile> candidateTiles)
    {
        return candidateTiles
            .Where(tile => tile != null && !reservedTargetTilesThisTurn.Contains(tile))
            .ToList();
    }

    // decide plannedAction and plannedPath for an enemy unit
    private static IEnumerator PlanEnemyAction(BaseUnit enemy, List<BaseUnit> players)
    {
        Debug.Log($"[EnemyExecutor] - AI Planning for {enemy.name}");

        BaseUnit target = FindClosestPlayerUnit(enemy, players);
        if (target == null)
        {
            Debug.Log("[EnemyExecutor] No live player units found. DEFEND.");
            enemy.plannedAction = PlannedAction.Defend;
            yield break;
        }

        RangeFinder rf = new RangeFinder();
        List<OverlayTile> attackRange = rf.GetTilesInRange(enemy, PlannerMode.Attack);

        // filter out tiles already reserved this turn
        List<OverlayTile> availableAttackTiles = FilterReservedTargetTiles(attackRange);

        OverlayTile prepTile = FindValidAttackPrepTile(enemy, target, availableAttackTiles);
        if (prepTile != null)
        {
            // reserve this tile for this turn
            reservedTargetTilesThisTurn.Add(prepTile);

            PathFinder pf = new PathFinder();
            enemy.plannedPath = pf.FindPath(enemy.standOnTile, prepTile, availableAttackTiles);
            enemy.targetUnit = target;
            enemy.plannedAction = PlannedAction.Attack;
            Debug.Log($"[EnemyExecutor] ATTACK {target.name} via {prepTile.grid2DLocation}");
            yield break;
        }

        // try to Move closer while avoiding reserved tiles
        List<OverlayTile> moveRange = rf.GetTilesInRange(enemy, PlannerMode.Move);
        List<OverlayTile> availableMoveTiles = FilterReservedTargetTiles(moveRange);

        OverlayTile moveTile = FindClosestTileTowardsTarget(enemy, target, availableMoveTiles);
        if (moveTile != null)
        {
            // reserve this tile
            reservedTargetTilesThisTurn.Add(moveTile);

            PathFinder pf = new PathFinder();
            enemy.plannedPath = pf.FindPath(enemy.standOnTile, moveTile, availableMoveTiles);
            enemy.plannedAction = PlannedAction.None;
            Debug.Log($"[EnemyExecutor] MOVE to {moveTile.grid2DLocation}");
            yield break;
        }

        // fallback
        enemy.plannedAction = PlannedAction.Defend;
        Debug.Log("[EnemyExecutor] DEFEND in place.");
    }

    // move the unit along its planned path
    private static IEnumerator MoveUnitAlongPath(BaseUnit unit)
    {
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

            Debug.Log($"[EnemyExecutor] {unit.name} moved to tile {tile.grid2DLocation}");
        }
    }

    // actually execute the planned action (Attack / Defend)
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
                Debug.Log($"[EnemyExecutor] {unit.name} DEFENDING +{DEFENSE_BOOST_AMOUNT}");
                break;

            case PlannedAction.Attack:
                if (unit.targetUnit != null && unit.targetUnit.gameObject != null)
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
                    Debug.Log($"[EnemyExecutor] {unit.name} attacks {unit.targetUnit.name} for {damage} dmg. Target HP {unit.targetUnit.health}");

                    unit.targetUnit.UpdateHealthBar();
                    CameraShake.Instance.Shake();

                    if (unit.targetUnit.health <= 0)
                    {
                        Debug.Log($"[EnemyExecutor] {unit.targetUnit.name} died.");
                        yield return TurnSystem.Instance.StartCoroutine(unit.targetUnit.DieAndRemove());
                    }
                }
                else
                {
                    Debug.Log($"[EnemyExecutor] {unit.name} planned attack but target missing.");
                }
                break;
        }
    }

    // find the closest player unit on the map
    private static BaseUnit FindClosestPlayerUnit(BaseUnit enemy, List<BaseUnit> players)
    {
        if (enemy == null || enemy.standOnTile == null) return null;
        BaseUnit closest = null;
        int minDistance = int.MaxValue;

        foreach (var unit in players)
        {
            if (unit == null) continue;
            if (unit.teamType != UnitTeam.Player) continue;
            if (unit.standOnTile == null) continue;

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

    // find a valid attack prep tile adjacent to the target
    private static OverlayTile FindValidAttackPrepTile(BaseUnit enemy, BaseUnit target, List<OverlayTile> candidateTiles)
    {
        if (target == null || target.standOnTile == null) return null;

        foreach (OverlayTile tile in candidateTiles)
        {
            if (tile == null) continue;
            var neighbors = MapManager.Instance.GetSurroundingTilesEightDirections(tile.grid2DLocation);
            if (neighbors.Contains(target.standOnTile))
            {
                return tile;
            }
        }
        return null;
    }

    // find the reachable tile that brings the enemy closest to the target
    private static OverlayTile FindClosestTileTowardsTarget(BaseUnit enemy, BaseUnit target, List<OverlayTile> candidateTiles)
    {
        if (target == null || target.standOnTile == null) return null;

        OverlayTile bestTile = null;
        int bestDist = int.MaxValue;

        foreach (var tile in candidateTiles)
        {
            if (tile == null) continue;
            if (tile.isBlocked || tile.isTempBlocked) continue;

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