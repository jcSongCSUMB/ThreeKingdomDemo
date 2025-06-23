using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;

public class AttackPlanner : MonoBehaviour
{
    private TileClickPathPlanner planner;
    private PathFinder pathFinder;
    private OverlayTile selectedPrepTile; // The tile chosen as attack position

    private void Awake()
    {
        planner = FindObjectOfType<TileClickPathPlanner>();
        pathFinder = new PathFinder();
    }

    private void Update()
    {
        // Only active in attack planning mode
        if (planner == null || planner.plannerMode != PlannerMode.Attack) return;

        if (Input.GetMouseButtonUp(0))
        {
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

    // Get the tile currently under the mouse
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

    // Step 1: Choose the tile to move to before attacking
    private void HandlePreparationTileSelection(OverlayTile tile)
    {
        BaseUnit unit = UnitSelector.currentUnit;
        if (unit == null || unit.standOnTile == null) return;

        int moveRange = unit.actionPoint - 1;
        List<OverlayTile> reachable = pathFinder.GetReachableTiles(unit.standOnTile, moveRange);

        List<OverlayTile> validPrepTiles = FindValidAttackPrepTiles(reachable);

        if (!validPrepTiles.Contains(tile))
        {
            Debug.Log("[AttackPlanner] Invalid attack prep tile selected. Cancelling.");
            planner.SetPlannerMode(PlannerMode.None);
            return;
        }

        selectedPrepTile = tile;

        // Unmark previous plan
        if (unit.plannedPath != null && unit.plannedPath.Count > 0)
        {
            unit.plannedPath.Last().UnmarkTempBlocked();
            unit.plannedPath.Last().UnmarkTurnBlocked();
        }

        List<OverlayTile> path = pathFinder.FindPath(unit.standOnTile, tile, reachable);
        unit.plannedPath = path;
        tile.MarkAsTempBlocked();
        tile.MarkAsTurnBlocked();
        unit.attackTargetTileCandidate = tile;

        Debug.Log("[AttackPlanner] Prep tile selected. Please click target enemy.");
    }

    // Step 2: Click on an enemy unit to confirm attack
    private void HandleTargetUnitSelection(OverlayTile tile)
    {
        BaseUnit unit = UnitSelector.currentUnit;
        if (unit == null || selectedPrepTile == null) return;

        if (!IsAdjacent(tile.grid2DLocation, selectedPrepTile.grid2DLocation))
        {
            Debug.Log("[AttackPlanner] Target not adjacent to prep tile. Cancelling.");
            planner.SetPlannerMode(PlannerMode.None);
            return;
        }

        BaseUnit target = tile.unitOnTile;
        if (target == null || target.faction == unit.faction)
        {
            Debug.Log("[AttackPlanner] No valid enemy on target tile. Cancelling.");
            planner.SetPlannerMode(PlannerMode.None);
            return;
        }

        unit.targetUnit = target;
        unit.plannedAction = PlannedAction.Attack;

        Debug.Log($"[AttackPlanner] Attack planned: {unit.name} -> {target.name}");

        planner.ClearAllHighlights();
        planner.SetPlannerMode(PlannerMode.None);

        selectedPrepTile = null;

        PlannerActionPanelController panel = FindObjectOfType<PlannerActionPanelController>();
        if (panel != null) panel.Hide();
    }

    // Utility: Check if two tiles are adjacent (in 8 directions)
    private bool IsAdjacent(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return (dx <= 1 && dy <= 1 && (dx + dy) > 0);
    }

    // Utility: Find reachable tiles that are also adjacent to an enemy
    private List<OverlayTile> FindValidAttackPrepTiles(List<OverlayTile> reachable)
    {
        List<OverlayTile> valid = new List<OverlayTile>();
        foreach (OverlayTile tile in reachable)
        {
            List<OverlayTile> neighbors = MapManager.Instance.GetNeighbors(tile);
            foreach (OverlayTile n in neighbors)
            {
                if (n.unitOnTile != null && n.unitOnTile.faction != UnitSelector.currentUnit.faction)
                {
                    valid.Add(tile);
                    break;
                }
            }
        }
        return valid;
    }
}