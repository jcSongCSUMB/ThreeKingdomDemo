using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

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

        // Only handle left click release
        if (Input.GetMouseButtonUp(0))
        {
            // Prevent raycast if clicking on UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                Debug.Log("[InputHandler] Click ignored — pointer is over UI");
                return;
            }

            Debug.Log("[InputHandler] Click detected");

            OverlayTile clickedTile = GetHoveredTile();
            if (clickedTile == null)
            {
                Debug.Log("[InputHandler] No tile detected under mouse");
                return;
            }

            Debug.Log($"[InputHandler] Clicked tile: {clickedTile.grid2DLocation}");

            if (!planner.HighlightedTiles.Contains(clickedTile))
            {
                Debug.Log("[InputHandler] Clicked tile is not in highlighted move range");
                return;
            }

            ConfirmTileSelection(clickedTile);
        }
    }

    /// <summary>
    /// Raycast to detect hovered OverlayTile under the mouse cursor.
    /// Uses Physics2D to match 2D Collider setup.
    /// </summary>
    /// <returns>Hovered OverlayTile or null</returns>
    private OverlayTile GetHoveredTile()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mousePos2D = new Vector2(mouseWorldPos.x, mouseWorldPos.y);

        RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero, 0f, LayerMask.GetMask("Tile"));
        if (hit.collider != null)
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

        // Hide action panel after planning
        PlannerActionPanelController panel = FindObjectOfType<PlannerActionPanelController>();
        if (panel != null)
        {
            panel.Hide();
        }

        // Exit planner mode
        planner.SetPlannerMode(PlannerMode.None);
    }
}