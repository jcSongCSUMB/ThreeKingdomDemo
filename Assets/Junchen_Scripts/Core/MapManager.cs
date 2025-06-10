using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

public class MapManager : MonoBehaviour
{
    private static MapManager _instance;
    public static MapManager Instance { get { return _instance; } }

    public GameObject overlayPrefab;
    public GameObject overlayContainer;

    public Dictionary<Vector2Int, OverlayTile> map;
    public bool ignoreBottomTiles;

    // Reference to the red/green deploy zone tilemap
    public Tilemap deployZoneTilemap;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    void Start()
    {
        Debug.Log("[MapManager] Start() entered.");

        var tileMaps = gameObject.transform.GetComponentsInChildren<Tilemap>().OrderByDescending(x => x.GetComponent<TilemapRenderer>().sortingOrder);
        map = new Dictionary<Vector2Int, OverlayTile>();

        foreach (var tm in tileMaps)
        {
            BoundsInt bounds = tm.cellBounds;

            for (int z = bounds.max.z; z >= bounds.min.z; z--)
            {
                for (int y = bounds.min.y; y < bounds.max.y; y++)
                {
                    for (int x = bounds.min.x; x < bounds.max.x; x++)
                    {
                        if (z == 0 && ignoreBottomTiles)
                            return;

                        if (tm.HasTile(new Vector3Int(x, y, z)))
                        {
                            Vector2Int grid2DPos = new Vector2Int(x, y);

                            if (!map.ContainsKey(grid2DPos))
                            {
                                var overlayTile = Instantiate(overlayPrefab, overlayContainer.transform);
                                var cellWorldPosition = tm.GetCellCenterWorld(new Vector3Int(x, y, z));
                                overlayTile.transform.position = new Vector3(cellWorldPosition.x, cellWorldPosition.y, cellWorldPosition.z + 1);
                                overlayTile.GetComponent<SpriteRenderer>().sortingOrder = tm.GetComponent<TilemapRenderer>().sortingOrder;
                                overlayTile.gameObject.GetComponent<OverlayTile>().gridLocation = new Vector3Int(x, y, z);

                                map.Add(grid2DPos, overlayTile.gameObject.GetComponent<OverlayTile>());
                            }
                        }
                    }
                }
            }
        }

        // Debug: Print overall map coverage
        Debug.Log($"Total overlay tiles generated: {map.Count}");

        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        foreach (var kvp in map)
        {
            var pos = kvp.Key;
            if (pos.x < minX) minX = pos.x;
            if (pos.x > maxX) maxX = pos.x;
            if (pos.y < minY) minY = pos.y;
            if (pos.y > maxY) maxY = pos.y;
        }

        Debug.Log($"Overlay tile X range: {minX} to {maxX}");
        Debug.Log($"Overlay tile Y range: {minY} to {maxY}");

        // Assign deploy zone flags based on the tile content at each location
        foreach (var kvp in map)
        {
            OverlayTile tile = kvp.Value;
            Vector3Int pos = tile.gridLocation;

            TileBase deployTile = deployZoneTilemap.GetTile(pos);
            if (deployTile != null)
            {
                string tileName = deployTile.name;

                if (tileName == "Sprite_Overlays_3")  // Green tile -> Player deploy zone
                {
                    tile.isPlayerDeployZone = true;
                }
                else if (tileName == "Sprite_Overlays_4")  // Red tile -> Enemy deploy zone
                {
                    tile.isEnemyDeployZone = true;
                }
            }
        }
    }

    // Return the 4 cardinal neighbors of a tile
    public List<OverlayTile> GetSurroundingTiles(Vector2Int originTile)
    {
        var surroundingTiles = new List<OverlayTile>();

        Vector2Int TileToCheck = new Vector2Int(originTile.x + 1, originTile.y);
        if (map.ContainsKey(TileToCheck))
        {
            if (Mathf.Abs(map[TileToCheck].transform.position.z - map[originTile].transform.position.z) < 1)
                surroundingTiles.Add(map[TileToCheck]);
        }

        TileToCheck = new Vector2Int(originTile.x - 1, originTile.y);
        if (map.ContainsKey(TileToCheck))
        {
            if (Mathf.Abs(map[TileToCheck].transform.position.z - map[originTile].transform.position.z) < 1)
                surroundingTiles.Add(map[TileToCheck]);
        }

        TileToCheck = new Vector2Int(originTile.x, originTile.y + 1);
        if (map.ContainsKey(TileToCheck))
        {
            if (Mathf.Abs(map[TileToCheck].transform.position.z - map[originTile].transform.position.z) < 1)
                surroundingTiles.Add(map[TileToCheck]);
        }

        TileToCheck = new Vector2Int(originTile.x, originTile.y - 1);
        if (map.ContainsKey(TileToCheck))
        {
            if (Mathf.Abs(map[TileToCheck].transform.position.z - map[originTile].transform.position.z) < 1)
                surroundingTiles.Add(map[TileToCheck]);
        }

        return surroundingTiles;
    }

    // Toggle deploy zone tilemap visibility
    public void ShowDeployZones()
    {
        if (deployZoneTilemap != null)
            deployZoneTilemap.gameObject.SetActive(true);
    }

    public void HideDeployZones()
    {
        if (deployZoneTilemap != null)
            deployZoneTilemap.gameObject.SetActive(false);
    }
}