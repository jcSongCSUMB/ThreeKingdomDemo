using System.Collections.Generic;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;

// UPDATED 2025-07-17: planning phase marks TEMP only (no TurnBlocked here).
// Previous prep tile still unmarked (temp + turn) for safety when re-selecting.
// UPDATED 2025-07-26: support adjacent direct attack (click enemy first).

public class AttackPlannerInputHandler : MonoBehaviour
{
    private TileClickPathPlanner planner;
    private PathFinder pathFinder;
    private RangeFinder rangeFinder;

    // The tile chosen as attack preparation position
    private OverlayTile selectedPrepTile;

    private List<OverlayTile> validPrepTiles;

    private void Awake()
    {
        planner = FindObjectOfType<TileClickPathPlanner>();
        pathFinder = new PathFinder();
        rangeFinder = new RangeFinder();
    }

    private void Update()
    {
        // Only handle player input in Attack mode
        if (planner == null || planner.plannerMode != PlannerMode.Attack)
            return;

        if (Input.GetMouseButtonUp(0))
        {
            // Ignore clicks on UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                Debug.Log("[AttackPlanner] Click ignored — pointer is over UI");
                return;
            }

            OverlayTile clickedTile = GetHoveredTile();
            if (clickedTile == null)
            {
                Debug.Log("[AttackPlanner] No tile under mouse");
                return;
            }

            // adjacent direct attack (no prep-tile needed)
            if (selectedPrepTile == null && TryDirectAttackIfAdjacent(clickedTile))
            {
                return; // planned and exited
            }

            if (selectedPrepTile == null)
            {
                HandlePreparationTileSelection(clickedTile);
            }
            else
            {
                HandleTargetUnitSelection(clickedTile);
            }
        }
    }

    // Get the tile currently under the mouse cursor
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

    // Adjacent direct attack branch
    private bool TryDirectAttackIfAdjacent(OverlayTile clickedTile)
    {
        BaseUnit unit = UnitSelector.currentUnit;
        if (unit == null || unit.standOnTile == null) return false;

        // Find enemy on the clicked tile
        BaseUnit enemy = FindObjectsOfType<BaseUnit>()
            .FirstOrDefault(u => u.standOnTile == clickedTile && u.teamType != unit.teamType);
        if (enemy == null) return false;

        // Check adjacency (Chebyshev = 1)
        if (!IsAdjacent(unit.standOnTile.grid2DLocation, clickedTile.grid2DLocation)) return false;

        // AP check: direct attack requires at least 1 AP
        if (unit.actionPoints < 1)
        {
            Debug.Log("[AttackPlanner] Not enough AP for direct attack.");
            return false;
        }

        // Clear previous temp block (if any previous plan existed)
        if (unit.plannedPath != null && unit.plannedPath.Count > 0)
        {
            OverlayTile previous = unit.plannedPath.Last();
            if (previous != null)
            {
                previous.UnmarkTempBlocked();
                previous.UnmarkTurnBlocked(); // safety clear (legacy)
            }
        }

        // Plan: stay on current tile and attack
        unit.plannedPath = new List<OverlayTile>() { unit.standOnTile }; // for consistent release/transform
        unit.standOnTile.MarkAsTempBlocked(); // planning uses TEMP only
        unit.targetUnit = enemy;
        unit.plannedAction = PlannedAction.Attack;

        Debug.Log($"[AttackPlanner] Direct attack planned: {unit.name} -> {enemy.name} (no move)");

        planner.ClearAllHighlights();
        ResetPlanner();
        return true;
    }

    // Handle the first click to select a valid tile for preparation
    private void HandlePreparationTileSelection(OverlayTile clickedTile)
    {
        BaseUnit unit = UnitSelector.currentUnit;
        if (unit == null || unit.standOnTile == null)
            return;

        // Get reachable tiles and filter to only valid attack prep tiles
        List<OverlayTile> reachableTiles = rangeFinder.GetTilesInRange(unit, PlannerMode.Attack);
        validPrepTiles = FindValidPrepTilesWithEnemyNearby(reachableTiles);

        if (!validPrepTiles.Contains(clickedTile))
        {
            Debug.Log($"[AttackPlanner] Clicked tile {clickedTile.grid2DLocation} is not a valid attack prep tile.");
            return;
        }

        // Confirm valid path exists
        List<OverlayTile> path = pathFinder.FindPath(unit.standOnTile, clickedTile, reachableTiles);
        if (path == null || path.Count == 0)
        {
            Debug.Log("[AttackPlanner] No valid path to tile. Cancelling.");
            return;
        }

        // Clear previous temp block
        if (unit.plannedPath != null && unit.plannedPath.Count > 0)
        {
            OverlayTile previous = unit.plannedPath.Last();
            previous.UnmarkTempBlocked();
            previous.UnmarkTurnBlocked(); // safety clear (legacy)
        }

        unit.plannedPath = path;

        // planning: temp only (no TurnBlocked until planning phase ends)
        clickedTile.MarkAsTempBlocked();

        selectedPrepTile = clickedTile;
        Debug.Log($"[AttackPlanner] Prep tile selected at {clickedTile.grid2DLocation}. Now select an adjacent enemy.");
    }

    // Handle second click: confirm enemy unit selection
    private void HandleTargetUnitSelection(OverlayTile clickedTile)
    {
        BaseUnit unit = UnitSelector.currentUnit;
        if (unit == null || selectedPrepTile == null)
            return;

        if (!IsAdjacent(clickedTile.grid2DLocation, selectedPrepTile.grid2DLocation))
        {
            Debug.Log("[AttackPlanner] Selected tile is not adjacent to prep tile.");
            return;
        }

        BaseUnit[] allUnits = FindObjectsOfType<BaseUnit>();
        BaseUnit enemy = allUnits.FirstOrDefault(u => u.standOnTile == clickedTile && u.teamType != unit.teamType);

        if (enemy == null)
        {
            Debug.Log("[AttackPlanner] No enemy unit on selected tile.");
            return;
        }

        unit.targetUnit = enemy;
        unit.plannedAction = PlannedAction.Attack;

        Debug.Log($"[AttackPlanner] Attack planned: {unit.name} -> {enemy.name}");

        planner.ClearAllHighlights();
        ResetPlanner();
    }

    // Filter reachable tiles to only those near enemies (8-direction check)
    private List<OverlayTile> FindValidPrepTilesWithEnemyNearby(List<OverlayTile> tiles)
    {
        List<OverlayTile> valid = new List<OverlayTile>();
        BaseUnit[] allUnits = FindObjectsOfType<BaseUnit>();

        foreach (OverlayTile tile in tiles)
        {
            // Use 8-directional neighbor check
            List<OverlayTile> neighbors = MapManager.Instance.GetSurroundingTilesEightDirections(tile.grid2DLocation);

            if (neighbors.Any(n => allUnits.Any(u => u.standOnTile == n && u.teamType != UnitSelector.currentUnit.teamType)))
            {
                valid.Add(tile);
            }
        }

        return valid;
    }

    // Check if two grid positions are adjacent
    private bool IsAdjacent(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return (dx <= 1 && dy <= 1 && (dx + dy) > 0);
    }

    // Reset internal planner state and UI
    private void ResetPlanner()
    {
        selectedPrepTile = null;
        planner.SetPlannerMode(PlannerMode.None);

        PlannerActionPanelController panel = FindObjectOfType<PlannerActionPanelController>();
        if (panel != null)
        {
            panel.Hide();
        }
    }
}