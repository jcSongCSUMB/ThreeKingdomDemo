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

public class TileClickPathPlanner : MonoBehaviour
{
    // Current planner mode (Move, Attack, Defend, or None)
    public PlannerMode plannerMode = PlannerMode.None;

    // Tiles currently in range and eligible for selection
    private List<OverlayTile> currentRangeTiles = new List<OverlayTile>();

    // Path preview shown when hovering over a tile
    private List<OverlayTile> currentHoverPath = new List<OverlayTile>();

    // Helper classes for tile range, path, and arrow direction
    private RangeFinder rangeFinder = new RangeFinder();
    private PathFinder pathFinder = new PathFinder();
    private ArrowTranslator arrowTranslator = new ArrowTranslator();

    // Currently selected unit (can be set manually)
    private BaseUnit currentUnit;

    // Public accessor for highlighted tiles
    public List<OverlayTile> HighlightedTiles => currentRangeTiles;

    // Clears both the highlighted tiles and hover path arrows (VISUALS ONLY)
    public void ClearAllHighlights()
    {
        ClearPreviousRangeTiles();
        ClearPathVisual();
        // NOTE: No longer unmarking any tile blocking flags here.
        // Use ReleaseCurrentUnitPlanning() explicitly when cancelling a plan.
    }

    // Explicitly release the current unit's planned destination tile (Temp + Turn safety)
    // Optionally clear highlights + arrows.
    public void ReleaseCurrentUnitPlanning(bool clearHighlights = true)
    {
        BaseUnit unit = currentUnit ?? UnitSelector.currentUnit;
        if (unit != null && unit.plannedPath != null && unit.plannedPath.Count > 0)
        {
            OverlayTile lastTile = unit.plannedPath.Last();
            lastTile.UnmarkTempBlocked();   // free temp claim
            lastTile.UnmarkTurnBlocked();   // safety: clear any legacy turn block
            Debug.Log($"[Planner] Released planning tile {lastTile.grid2DLocation} for {unit.name}.");
        }

        if (clearHighlights)
        {
            ClearAllHighlights();
        }
    }

    // Sets the current planner mode and highlights tiles accordingly
    public void SetPlannerMode(PlannerMode mode)
    {
        // Only allow planner mode during the player's planning phase
        if (!TurnSystem.Instance.IsPlanningPhase())
        {
            Debug.Log("[Planner] Cannot set mode, not in planning phase");
            return;
        }

        BaseUnit unit = currentUnit ?? UnitSelector.currentUnit;
        if (unit == null)
        {
            Debug.Log("[Planner] No unit selected");
            return;
        }

        // Clear previous highlights and arrows
        ClearPreviousRangeTiles();
        ClearPathVisual();

        plannerMode = mode;
        Debug.Log($"[Planner] Mode set to: {plannerMode}");

        // Show range for Move and Attack modes
        if (plannerMode == PlannerMode.Move || plannerMode == PlannerMode.Attack)
        {
            ShowMovementRange(unit);
        }
    }

    // Allows external systems to assign the current unit
    public void SetCurrentUnit(BaseUnit unit)
    {
        currentUnit = unit;
    }

    // Highlights all tiles reachable based on unit's action points and current planner mode
    private void ShowMovementRange(BaseUnit unit)
    {
        currentRangeTiles = rangeFinder.GetTilesInRange(unit, plannerMode);

        // Special filtering logic for Attack mode
        if (plannerMode == PlannerMode.Attack)
        {
            List<OverlayTile> filtered = new List<OverlayTile>();
            BaseUnit[] allUnits = FindObjectsOfType<BaseUnit>();

            foreach (OverlayTile tile in currentRangeTiles)
            {
                // Check if this tile is adjacent to any enemy unit
                List<OverlayTile> neighbors = MapManager.Instance.GetSurroundingTilesEightDirections(tile.grid2DLocation);
                bool hasEnemyNearby = neighbors.Any(n =>
                    allUnits.Any(u => u.standOnTile == n && u.teamType != unit.teamType));

                if (hasEnemyNearby)
                {
                    filtered.Add(tile);
                }
            }

            currentRangeTiles = filtered;

            // Highlight only filtered legal tiles
            foreach (var tile in currentRangeTiles)
            {
                tile.ShowTile();  // customize tint if needed
            }

            Debug.Log($"[Planner] Showing legal attack prep tiles: {currentRangeTiles.Count}");
            return;
        }

        // Default logic for Move and other modes
        foreach (var tile in currentRangeTiles)
        {
            tile.ShowTile();
        }

        Debug.Log($"[Planner] Showing movement range tiles: {currentRangeTiles.Count}");
    }

    // Hides all previously highlighted tiles
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

    // Handles live path preview updates while in Move mode
    private void Update()
    {
        // Only respond to mouse hover in Move mode during PlayerPlanning phase
        if (plannerMode != PlannerMode.Move || !TurnSystem.Instance.IsPlanningPhase())
            return;

        BaseUnit unit = currentUnit ?? UnitSelector.currentUnit;
        if (unit == null) return;

        OverlayTile tileUnderMouse = GetTileUnderMouse();
        if (tileUnderMouse == null) return;

        // Check if hovered tile is in movement range
        if (currentRangeTiles.Contains(tileUnderMouse))
        {
            var path = pathFinder.FindPath(unit.standOnTile, tileUnderMouse, currentRangeTiles);

            // Only update visuals if path is different from last
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

    // Performs a raycast to get the tile currently under the mouse
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