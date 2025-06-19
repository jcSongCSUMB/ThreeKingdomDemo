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

public class TileClickPathPlanner : MonoBehaviour
{
    public PlannerMode plannerMode = PlannerMode.None;

    private List<OverlayTile> currentRangeTiles = new List<OverlayTile>();       // to highlight the tiles within unit's range
    private List<OverlayTile> currentHoverPath = new List<OverlayTile>();        // to show the path after calculation

    private RangeFinder rangeFinder = new RangeFinder();
    private PathFinder pathFinder = new PathFinder();
    private ArrowTranslator arrowTranslator = new ArrowTranslator();

    private BaseUnit currentUnit;    // Reference passed from PathPlannerInputHandler

    // Public getter for external access to range tiles
    public List<OverlayTile> HighlightedTiles => currentRangeTiles;

    // Clears both tile highlights and path arrows
    public void ClearAllHighlights()
    {
        ClearPreviousRangeTiles();
        ClearPathVisual();
    }

    public void SetPlannerMode(PlannerMode mode)
    {
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

        ClearPreviousRangeTiles();
        ClearPathVisual();

        plannerMode = mode;
        Debug.Log($"[Planner] Mode set to: {plannerMode}");

        if (plannerMode == PlannerMode.Move)
        {
            ShowMovementRange(unit);
        }
    }

    public void SetCurrentUnit(BaseUnit unit)
    {
        currentUnit = unit;
    }

    private void ShowMovementRange(BaseUnit unit)
    {
        Vector2Int unitPos = unit.standOnTile.grid2DLocation;
        currentRangeTiles = rangeFinder.GetTilesInRange(unitPos, 3);

        foreach (var tile in currentRangeTiles)
        {
            tile.ShowTile();
        }

        Debug.Log($"[Planner] Showing movement range tiles: {currentRangeTiles.Count}");
    }

    private void ClearPreviousRangeTiles()
    {
        foreach (var tile in currentRangeTiles)
        {
            tile.HideTile();
        }
        currentRangeTiles.Clear();
    }

    private void ClearPathVisual()
    {
        foreach (var tile in currentHoverPath)
        {
            tile.SetSprite(ArrowTranslator.ArrowDirection.None);
        }
        currentHoverPath.Clear();
    }

    private void Update()
    {
        // Only operate in Move planning mode
        if (plannerMode != PlannerMode.Move || !TurnSystem.Instance.IsPlanningPhase())
            return;

        BaseUnit unit = currentUnit ?? UnitSelector.currentUnit;
        if (unit == null) return;

        OverlayTile tileUnderMouse = GetTileUnderMouse();
        if (tileUnderMouse == null) return;

        // Check tile is within reachable range
        if (currentRangeTiles.Contains(tileUnderMouse))
        {
            var path = pathFinder.FindPath(unit.standOnTile, tileUnderMouse, currentRangeTiles);

            // Refresh path arrows if the path has changed
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

            // the mechanics of mouse click to confirm path is transferred to path planner
        }
    }

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