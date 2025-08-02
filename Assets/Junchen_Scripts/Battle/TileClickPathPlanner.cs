using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum PlannerMode
{
    None,
    Move,
    Attack,
    Defend
}

// UPDATED 2025-07-17: planner no longer auto-unmarks tiles in ClearAllHighlights().
// Use ReleaseCurrentUnitPlanning() when cancelling/switching units to free the tile.
// Planning phase now uses TEMP-only blocking; legacy TurnBlocked clearing retained in release helper for safety.
//
// UPDATED 2025-07-27: Allow resetting to None even without a selected unit.
// Added ForceCancel() to perform a safe global cancel from callers (e.g., UnitSelector).
//
// UPDATED 2025-08-01: unified enemy-neighbor check to coordinate compare, pruned logs, added concise comments.

public class TileClickPathPlanner : MonoBehaviour
{
    // Current planner mode (Move, Attack, Defend, or None)
    public PlannerMode plannerMode = PlannerMode.None;

    // Tiles currently in range and eligible for selection (visuals)
    private List<OverlayTile> currentRangeTiles = new List<OverlayTile>();

    // Path preview shown when hovering over a tile (visuals)
    private List<OverlayTile> currentHoverPath = new List<OverlayTile>();

    // Helpers
    private RangeFinder rangeFinder = new RangeFinder();
    private PathFinder pathFinder = new PathFinder();
    private ArrowTranslator arrowTranslator = new ArrowTranslator();

    // Currently selected unit (can be set manually)
    private BaseUnit currentUnit;

    // Public accessor for highlighted tiles
    public List<OverlayTile> HighlightedTiles => currentRangeTiles;

    // -------- Utility methods --------

    // Clears all tile highlights and path arrows
    public void ClearAllHighlights()
    {
        ClearPreviousRangeTiles();
        ClearPathVisual();
    }

    // Releases the current unit’s reserved tile and optionally clears visuals
    public void ReleaseCurrentUnitPlanning(bool clearHighlights = true)
    {
        BaseUnit unit = currentUnit ?? UnitSelector.currentUnit;
        if (unit != null && unit.plannedPath != null && unit.plannedPath.Count > 0)
        {
            OverlayTile lastTile = unit.plannedPath.Last();
            if (lastTile != null)
            {
                lastTile.UnmarkTempBlocked();
                lastTile.UnmarkTurnBlocked();
            }
        }
        if (clearHighlights) ClearAllHighlights();
    }

    // Sets the planner mode; can always reset to None
    public void SetPlannerMode(PlannerMode mode)
    {
        if (!TurnSystem.Instance.IsPlanningPhase())
        {
            Debug.Log("[Planner] Cannot set mode, not in planning phase");
            return;
        }

        var old = plannerMode;

        // Always allow clearing
        if (mode == PlannerMode.None)
        {
            ClearAllHighlights();
            plannerMode = PlannerMode.None;
            return;
        }

        BaseUnit unit = currentUnit ?? UnitSelector.currentUnit;
        if (unit == null)
        {
            Debug.LogWarning($"[Planner] No unit selected for mode {mode}");
            return;
        }

        ClearPreviousRangeTiles();
        ClearPathVisual();

        plannerMode = mode;

        if (plannerMode == PlannerMode.Move || plannerMode == PlannerMode.Attack)
            ShowMovementRange(unit);
    }

    // Cancels current planning state
    public void ForceCancel(bool releaseCurrentPlanning = true)
    {
        if (releaseCurrentPlanning) ReleaseCurrentUnitPlanning(true);
        else ClearAllHighlights();

        plannerMode = PlannerMode.None;
    }

    // Allows external scripts to assign the current unit
    public void SetCurrentUnit(BaseUnit unit) => currentUnit = unit;

    // -------- Range & visual helpers --------

    // Shows move or attack range tiles based on current mode
    private void ShowMovementRange(BaseUnit unit)
    {
        currentRangeTiles = rangeFinder.GetTilesInRange(unit, plannerMode);

        if (plannerMode == PlannerMode.Attack)
        {
            List<OverlayTile> filtered = new List<OverlayTile>();
            BaseUnit[] allUnits = FindObjectsOfType<BaseUnit>();

            foreach (OverlayTile tile in currentRangeTiles)
            {
                // UPDATED 2025-08-01: use coordinate compare instead of reference equality
                var neighbors = MapManager.Instance.GetSurroundingTilesEightDirections(tile.grid2DLocation);
                bool hasEnemyNearby = neighbors.Any(n =>
                    allUnits.Any(u =>
                        u.standOnTile != null &&
                        u.standOnTile.grid2DLocation == n.grid2DLocation &&
                        u.teamType != unit.teamType)); // enemy check by team

                if (hasEnemyNearby) filtered.Add(tile);
            }

            currentRangeTiles = filtered;
        }

        foreach (var tile in currentRangeTiles) tile.ShowTile();
    }

    // Hides previously highlighted tiles
    private void ClearPreviousRangeTiles()
    {
        foreach (var tile in currentRangeTiles) tile.HideTile();
        currentRangeTiles.Clear();
    }

    // Clears live path preview arrows
    private void ClearPathVisual()
    {
        foreach (var tile in currentHoverPath) tile.SetSprite(ArrowTranslator.ArrowDirection.None);
        currentHoverPath.Clear();
    }

    // -------- Live hover path preview (Move mode only) --------
    private void Update()
    {
        if (plannerMode != PlannerMode.Move || !TurnSystem.Instance.IsPlanningPhase()) return;

        BaseUnit unit = currentUnit ?? UnitSelector.currentUnit;
        if (unit == null) return;

        OverlayTile tileUnderMouse = GetTileUnderMouse();
        if (tileUnderMouse == null) return;

        if (currentRangeTiles.Contains(tileUnderMouse))
        {
            var path = pathFinder.FindPath(unit.standOnTile, tileUnderMouse, currentRangeTiles);

            if (!Enumerable.SequenceEqual(path, currentHoverPath))
            {
                ClearPathVisual();

                for (int i = 0; i < path.Count; i++)
                {
                    var previous = i > 0 ? path[i - 1] : unit.standOnTile;
                    var next = i < path.Count - 1 ? path[i + 1] : null;
                    var dir = arrowTranslator.TranslateDirection(previous, path[i], next);
                    path[i].SetSprite(dir);
                }
                currentHoverPath = path;
            }
        }
    }

    // Raycasts to find the tile under the mouse cursor
    private OverlayTile GetTileUnderMouse()
    {
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 pos2D = new Vector2(worldPos.x, worldPos.y);

        RaycastHit2D hit = Physics2D.Raycast(pos2D, Vector2.zero);
        return hit.collider != null ? hit.collider.GetComponent<OverlayTile>() : null;
    }
}