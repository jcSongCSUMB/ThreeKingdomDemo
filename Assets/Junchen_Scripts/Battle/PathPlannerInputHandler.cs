using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles input during planner mode (e.g., Move).
/// Interacts with TileClickPathPlanner to confirm tile selection.
/// </summary>
public class PathPlannerInputHandler : MonoBehaviour
{
    private TileClickPathPlanner planner;
    private PathFinder pathFinder;

    private void Awake()
    {
        planner = FindObjectOfType<TileClickPathPlanner>();
        pathFinder = new PathFinder(); // Initialize local instance of PathFinder
    }

    private void Update()
    {
        // Only active in move planning mode
        if (planner == null || planner.plannerMode != PlannerMode.Move) return;

        // On left-click release
        if (Input.GetMouseButtonUp(0))
        {
            OverlayTile clickedTile = GetHoveredTile();
            if (clickedTile != null && planner.HighlightedTiles.Contains(clickedTile))
            {
                ConfirmTileSelection(clickedTile);
            }
        }
    }

    /// <summary>
    /// Raycast to detect hovered OverlayTile under the mouse cursor.
    /// </summary>
    /// <returns>Hovered OverlayTile or null</returns>
    private OverlayTile GetHoveredTile()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100, LayerMask.GetMask("Tile")))
        {
            return hit.collider.GetComponent<OverlayTile>();
        }
        return null;
    }
    
    /// <summary>
    /// Handle logic when player confirms a tile during Move planning.
    /// </summary>
    /// <param name="targetTile">The selected destination tile</param>
    private void ConfirmTileSelection(OverlayTile targetTile)
    {
        BaseUnit unit = UnitSelector.currentUnit;
        if (unit == null || unit.standOnTile == null) return;

        // Use local PathFinder to get final path
        List<OverlayTile> path = pathFinder.FindPath(unit.standOnTile, targetTile, planner.HighlightedTiles);
        if (path == null || path.Count == 0) return;

        // Set the plan into the unit
        unit.plannedPath = path;

        // Hide highlight and arrow
        planner.ClearAllHighlights();

        Debug.Log($"[Planner] Move planned for {unit.name} to tile {targetTile.gridLocation}");
    }
}