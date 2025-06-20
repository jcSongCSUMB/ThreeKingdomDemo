using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class RangeFinder
{
    /// <summary>
    /// Gets all tiles in range from a given grid location and range value.
    /// This is the original method.
    /// </summary>
    public List<OverlayTile> GetTilesInRange(Vector2Int location, int range)
    {
        var startingTile = MapManager.Instance.map[location];
        var inRangeTiles = new List<OverlayTile>();
        int stepCount = 0;

        inRangeTiles.Add(startingTile);

        // Should contain the surroundingTiles of the previous step. 
        var tilesForPreviousStep = new List<OverlayTile>();
        tilesForPreviousStep.Add(startingTile);

        while (stepCount < range)
        {
            var surroundingTiles = new List<OverlayTile>();

            foreach (var item in tilesForPreviousStep)
            {
                surroundingTiles.AddRange(MapManager.Instance.GetSurroundingTiles(new Vector2Int(item.gridLocation.x, item.gridLocation.y)));
            }

            inRangeTiles.AddRange(surroundingTiles);
            tilesForPreviousStep = surroundingTiles.Distinct().ToList();
            stepCount++;
        }

        return inRangeTiles.Distinct().ToList();
    }

    /// <summary>
    /// New method — determines range based on unit's action points and planner mode.
    /// Attack mode uses (actionPoints - 1); otherwise full actionPoints.
    /// </summary>
    public List<OverlayTile> GetTilesInRange(BaseUnit unit, PlannerMode mode)
    {
        int range = (mode == PlannerMode.Attack) ? Mathf.Max(unit.actionPoints - 1, 0) : unit.actionPoints;
        return GetTilesInRange(unit.standOnTile.grid2DLocation, range);
    }
}