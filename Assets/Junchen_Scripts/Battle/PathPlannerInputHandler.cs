using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;

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

            // Prevent selecting a tile that has already been claimed by another unit
            if (clickedTile.isTempBlocked)
            {
                Debug.Log($"[InputHandler] Tile {clickedTile.grid2DLocation} is already claimed by another unit.");
                return;
            }

            ConfirmTileSelection(clickedTile);
        }
    }

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

    private void ConfirmTileSelection(OverlayTile clickedTile)
    {
        BaseUnit unit = UnitSelector.currentUnit;
        if (unit == null || unit.standOnTile == null) return;

        List<OverlayTile> path = pathFinder.FindPath(unit.standOnTile, clickedTile, planner.HighlightedTiles);
        if (path == null || path.Count == 0) return;

        // 🔁 New: unmark previously blocked tile if any
        if (unit.plannedPath != null && unit.plannedPath.Count > 0)
        {
            OverlayTile previousTile = unit.plannedPath.Last();
            previousTile.UnmarkTempBlocked();
        }

        unit.plannedPath = path;

        OverlayTile destinationTile = path[path.Count - 1];
        destinationTile.MarkAsTempBlocked();
        Debug.Log($"[Planner] Tile {destinationTile.grid2DLocation} temporarily blocked for this unit.");

        planner.ClearAllHighlights();

        Debug.Log($"[Planner] Move planned for {unit.name} to tile {destinationTile.gridLocation}");

        PlannerActionPanelController panel = FindObjectOfType<PlannerActionPanelController>();
        if (panel != null)
        {
            panel.Hide();
        }

        planner.SetPlannerMode(PlannerMode.None);
    }
}