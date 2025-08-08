using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RPGPathFinder
{
    // Dictionary of searchable tiles for pathfinding
    private Dictionary<Vector2Int, OverlayTile> searchableTiles;

    // Reference to the RpgMapManager in the current scene
    private RpgMapManager rpgMap;

    // Constructor: store reference to the RpgMapManager
    public RPGPathFinder(RpgMapManager map)
    {
        rpgMap = map;
    }

    // Find path from start to end using A* search
    public List<OverlayTile> FindPath(OverlayTile start, OverlayTile end, List<OverlayTile> inRangeTiles)
    {
        searchableTiles = new Dictionary<Vector2Int, OverlayTile>();

        List<OverlayTile> openList = new List<OverlayTile>();
        HashSet<OverlayTile> closedList = new HashSet<OverlayTile>();

        // If in-range tiles are provided, limit search space to them
        if (inRangeTiles != null && inRangeTiles.Count > 0)
        {
            foreach (var item in inRangeTiles)
            {
                if (!searchableTiles.ContainsKey(item.grid2DLocation) && rpgMap.map.ContainsKey(item.grid2DLocation))
                {
                    searchableTiles.Add(item.grid2DLocation, rpgMap.map[item.grid2DLocation]);
                }
            }
        }
        else
        {
            searchableTiles = rpgMap.map;
        }

        openList.Add(start);

        // Main A* loop
        while (openList.Count > 0)
        {
            OverlayTile currentOverlayTile = openList.OrderBy(x => x.F).First();

            openList.Remove(currentOverlayTile);
            closedList.Add(currentOverlayTile);

            // Path found
            if (currentOverlayTile == end)
            {
                return GetFinishedList(start, end);
            }

            // Check each neighbour tile
            foreach (var tile in GetNeighbourTiles(currentOverlayTile))
            {
                if (tile.isBlocked || tile.isBlockedThisTurn || closedList.Contains(tile) ||
                    Mathf.Abs(currentOverlayTile.transform.position.z - tile.transform.position.z) > 1)
                {
                    continue;
                }

                tile.G = GetManhattanDistance(start, tile);
                tile.H = GetManhattanDistance(end, tile);
                tile.Previous = currentOverlayTile;

                if (!openList.Contains(tile))
                {
                    openList.Add(tile);
                }
            }
        }

        // Return empty path if no route is found
        return new List<OverlayTile>();
    }

    // Build the finished path by following Previous pointers from end to start
    private List<OverlayTile> GetFinishedList(OverlayTile start, OverlayTile end)
    {
        List<OverlayTile> finishedList = new List<OverlayTile>();
        OverlayTile currentTile = end;

        while (currentTile != start)
        {
            finishedList.Add(currentTile);
            currentTile = currentTile.Previous;
        }

        finishedList.Reverse();
        return finishedList;
    }

    // Manhattan distance heuristic for A* (grid-based)
    private int GetManhattanDistance(OverlayTile start, OverlayTile tile)
    {
        return Mathf.Abs(start.gridLocation.x - tile.gridLocation.x) +
               Mathf.Abs(start.gridLocation.y - tile.gridLocation.y);
    }

    // Get 4-direction neighbours of a tile from searchableTiles
    private List<OverlayTile> GetNeighbourTiles(OverlayTile currentOverlayTile)
    {
        List<OverlayTile> neighbours = new List<OverlayTile>();

        Vector2Int check;

        check = new Vector2Int(currentOverlayTile.gridLocation.x + 1, currentOverlayTile.gridLocation.y);
        if (searchableTiles.ContainsKey(check)) neighbours.Add(searchableTiles[check]);

        check = new Vector2Int(currentOverlayTile.gridLocation.x - 1, currentOverlayTile.gridLocation.y);
        if (searchableTiles.ContainsKey(check)) neighbours.Add(searchableTiles[check]);

        check = new Vector2Int(currentOverlayTile.gridLocation.x, currentOverlayTile.gridLocation.y + 1);
        if (searchableTiles.ContainsKey(check)) neighbours.Add(searchableTiles[check]);

        check = new Vector2Int(currentOverlayTile.gridLocation.x, currentOverlayTile.gridLocation.y - 1);
        if (searchableTiles.ContainsKey(check)) neighbours.Add(searchableTiles[check]);

        return neighbours;
    }
}   
