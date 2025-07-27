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

    // ===== DEBUG TAGS =====
    private const string LOG_MODE  = "[MODE]";
    private const string LOG_CANCEL = "[CANCEL]";

    // VISUAL-ONLY: clear highlights/arrows (does NOT touch any tile flags)
    public void ClearAllHighlights()
    {
        ClearPreviousRangeTiles();
        ClearPathVisual();
        // NOTE: No unmark here. Use ReleaseCurrentUnitPlanning() when you need to free a reserved tile.
    }

    // Release current unit's planned destination (Temp + Turn safety) and optionally clear visuals
    public void ReleaseCurrentUnitPlanning(bool clearHighlights = true)
    {
        BaseUnit unit = currentUnit ?? UnitSelector.currentUnit;
        if (unit != null && unit.plannedPath != null && unit.plannedPath.Count > 0)
        {
            OverlayTile lastTile = unit.plannedPath.Last();
            if (lastTile != null)
            {
                lastTile.UnmarkTempBlocked();   // free temp claim
                lastTile.UnmarkTurnBlocked();   // safety: clear any legacy turn block
                Debug.Log($"[Planner] Released planning tile {lastTile.grid2DLocation} for {unit.name}.");
            }
        }

        if (clearHighlights)
        {
            ClearAllHighlights();
        }
    }

    // ===== PATCHED: SetPlannerMode =====
    // Allow resetting to None even without a selected unit.
    public void SetPlannerMode(PlannerMode mode)
    {
        // Planner mode is meaningful only in PlayerPlanning phase
        if (!TurnSystem.Instance.IsPlanningPhase())
        {
            Debug.Log("[Planner] Cannot set mode, not in planning phase");
            return;
        }

        var old = plannerMode;

        // Always allow falling back to None (even if no current unit)
        if (mode == PlannerMode.None)
        {
            try { ClearAllHighlights(); } catch { /* safety */ }
            plannerMode = PlannerMode.None;
            Debug.Log($"{LOG_MODE} {old} -> None (allowed without currentUnit)");
            return;
        }

        // For non-None modes, require a selected unit
        BaseUnit unit = currentUnit ?? UnitSelector.currentUnit;
        if (unit == null)
        {
            Debug.LogWarning($"{LOG_MODE} early-return: currentUnit==null for {mode}, keep={plannerMode}");
            return;
        }

        // Reset visuals before showing new range
        ClearPreviousRangeTiles();
        ClearPathVisual();

        plannerMode = mode;
        Debug.Log($"{LOG_MODE} {old} -> {plannerMode}");

        if (plannerMode == PlannerMode.Move || plannerMode == PlannerMode.Attack)
        {
            ShowMovementRange(unit);
        }
    }

    // Public helper for callers that need a robust global cancel (no new script required).
    // Optionally releases current unit's temporary plan (Temp/Turn safety) and clears visuals.
    public void ForceCancel(bool releaseCurrentPlanning = true)
    {
        if (releaseCurrentPlanning)
        {
            try { ReleaseCurrentUnitPlanning(true); } catch { /* safety */ }
        }
        else
        {
            try { ClearAllHighlights(); } catch { /* safety */ }
        }

        plannerMode = PlannerMode.None;
        Debug.Log($"{LOG_CANCEL} plannerMode => None");
    }

    // Allow external systems to assign the current unit
    public void SetCurrentUnit(BaseUnit unit)
    {
        currentUnit = unit;
    }

    // Highlights all tiles reachable based on unit's AP and current planner mode
    private void ShowMovementRange(BaseUnit unit)
    {
        currentRangeTiles = rangeFinder.GetTilesInRange(unit, plannerMode);

        if (plannerMode == PlannerMode.Attack)
        {
            List<OverlayTile> filtered = new List<OverlayTile>();
            BaseUnit[] allUnits = FindObjectsOfType<BaseUnit>();

            foreach (OverlayTile tile in currentRangeTiles)
            {
                // 8-direction neighbor check against enemies
                var neighbors = MapManager.Instance.GetSurroundingTilesEightDirections(tile.grid2DLocation);
                bool hasEnemyNearby = neighbors.Any(n =>
                    allUnits.Any(u => u.standOnTile == n && u.teamType != unit.teamType));

                if (hasEnemyNearby)
                {
                    filtered.Add(tile);
                }
            }

            currentRangeTiles = filtered;

            foreach (var tile in currentRangeTiles)
            {
                tile.ShowTile();
            }

            Debug.Log($"[Planner] Showing legal attack prep tiles: {currentRangeTiles.Count}");
            return;
        }

        // Default for Move/others
        foreach (var tile in currentRangeTiles)
        {
            tile.ShowTile();
        }

        Debug.Log($"[Planner] Showing movement range tiles: {currentRangeTiles.Count}");
    }

    // Hides previously highlighted tiles
    private void ClearPreviousRangeTiles()
    {
        foreach (var tile in currentRangeTiles)
        {
            tile.HideTile();
        }
        currentRangeTiles.Clear();
    }

    // Clears any path arrow sprites shown for hover preview
    private void ClearPathVisual()
    {
        foreach (var tile in currentHoverPath)
        {
            tile.SetSprite(ArrowTranslator.ArrowDirection.None);
        }
        currentHoverPath.Clear();
    }

    // Live path preview (Move mode only)
    private void Update()
    {
        if (plannerMode != PlannerMode.Move || !TurnSystem.Instance.IsPlanningPhase())
            return;

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
                    var direction = arrowTranslator.TranslateDirection(previous, path[i], next);
                    path[i].SetSprite(direction);
                }

                currentHoverPath = path;
            }
        }
    }

    // Raycast to the tile under the mouse cursor
    private OverlayTile GetTileUnderMouse()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mousePos2D = new Vector2(mouseWorldPos.x, mouseWorldPos.y);

        RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);
        if (hit.collider != null)
        {
            return hit.collider.GetComponent<OverlayTile>();
        }
        return null;
    }
}