using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// -----------------------------------------------------------------------------
// RangeFinder
//   • Calculates reachable tiles for Move / Attack planners.
//   • Filters out tiles that are permanently blocked (isBlocked)
//     or blocked for the entire turn (isBlockedThisTurn) so UI high‑light
//     always matches actual pathfinding feasibility.
// -----------------------------------------------------------------------------
public class RangeFinder
{
    // -------------------------------------------------------------------------
    // GetTilesInRange(location, range)
    //   Breadth‑first expansion up to <range> steps in 4‑dir grid.
    //   Returns DISTINCT tiles that are NOT blocked / turn‑blocked.
    // -------------------------------------------------------------------------
    public List<OverlayTile> GetTilesInRange(Vector2Int location, int range)
    {
        OverlayTile startTile = MapManager.Instance.map[location];
        var inRangeTiles       = new List<OverlayTile> { startTile };
        var frontierLastStep   = new List<OverlayTile> { startTile };
        int stepsTaken         = 0;

        while (stepsTaken < range)
        {
            var frontierNext = new List<OverlayTile>();

            foreach (OverlayTile tile in frontierLastStep)
            {
                frontierNext.AddRange(
                    MapManager.Instance.GetSurroundingTiles(tile.grid2DLocation)
                );
            }

            inRangeTiles.AddRange(frontierNext);
            frontierLastStep = frontierNext.Distinct().ToList();
            stepsTaken++;
        }

        // Keep only distinct, non‑blocked tiles
        return inRangeTiles
            .Distinct()
            .Where(tile => !tile.isBlocked && !tile.isBlockedThisTurn)
            .ToList();
    }

    // -------------------------------------------------------------------------
    // GetTilesInRange(unit, mode)
    //   Wrapper that derives <range> from the unit's action points.
    //   • Move   : full actionPoints
    //   • Attack : actionPoints - 1  (must reserve 1 point for the attack itself)
    // -------------------------------------------------------------------------
    public List<OverlayTile> GetTilesInRange(BaseUnit unit, PlannerMode mode)
    {
        int range = (mode == PlannerMode.Attack)
                    ? Mathf.Max(unit.actionPoints - 1, 0)
                    : unit.actionPoints;

        return GetTilesInRange(unit.standOnTile.grid2DLocation, range);
    }
}