using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Lightweight map manager for RPG scene (decoupled from battle).
/// - Builds overlay tiles (no deploy-zone / turn blocking).
/// - Keeps a (x,y) -> OverlayTile dictionary (highest visible layer only).
/// - Provides GetTileFromWorld() and neighbours lookup for pathfinding.
/// - Snaps all CharacterInfo units to the nearest tile center after build.
/// </summary>
public class RpgMapManager : MonoBehaviour
{
    // Overlay prefab used to create overlay tiles
    public GameObject overlayPrefab;

    // Parent transform for overlay tiles
    public GameObject overlayContainer;

    // If true, skip z == 0 cells when building overlays
    public bool ignoreBottomTiles = false;

    // (x,y) -> OverlayTile (topmost only)
    public Dictionary<Vector2Int, OverlayTile> map;

    // Build state
    public bool IsBuilt { get; private set; } = false;

    // Optional event for other systems
    public event Action OnBuilt;

    private void Awake()
    {
        map = new Dictionary<Vector2Int, OverlayTile>();
    }

    private void Start()
    {
        BuildOverlayMap();
        SnapAllCharactersToNearestTiles();
        IsBuilt = true;
        OnBuilt?.Invoke();
    }

    // Build overlays from child tilemaps ordered by sortingOrder (desc) and z (desc)
    private void BuildOverlayMap()
    {
        if (overlayPrefab == null || overlayContainer == null)
        {
            Debug.LogError("[RpgMapManager] Missing overlayPrefab or overlayContainer.");
            return;
        }

        map.Clear();

        var tilemaps = GetComponentsInChildren<Tilemap>(false)
            .OrderByDescending(tm =>
            {
                var r = tm.GetComponent<TilemapRenderer>();
                return r != null ? r.sortingOrder : 0;
            });

        foreach (var tm in tilemaps)
        {
            var r = tm.GetComponent<TilemapRenderer>();
            int order = r != null ? r.sortingOrder : 0;

            BoundsInt b = tm.cellBounds;

            for (int z = b.max.z; z >= b.min.z; z--)
            {
                for (int y = b.min.y; y < b.max.y; y++)
                {
                    for (int x = b.min.x; x < b.max.x; x++)
                    {
                        if (ignoreBottomTiles && z == 0) continue;

                        var cell = new Vector3Int(x, y, z);
                        if (!tm.HasTile(cell)) continue;

                        var key = new Vector2Int(x, y);
                        if (map.ContainsKey(key)) continue;

                        var world = tm.GetCellCenterWorld(cell);
                        var go = Instantiate(overlayPrefab, overlayContainer.transform);
                        go.name = $"Overlay ({x},{y})";
                        go.transform.position = new Vector3(world.x, world.y, world.z + 1f);

                        var sr = go.GetComponent<SpriteRenderer>();
                        if (sr != null) sr.sortingOrder = order;

                        var ot = go.GetComponent<OverlayTile>();
                        if (ot != null) ot.gridLocation = new Vector3Int(x, y, z);

                        map.Add(key, ot);
                    }
                }
            }
        }

        Debug.Log($"[RpgMapManager] Overlays: {map.Count}");

        if (map.Count > 0)
        {
            int minX = map.Keys.Min(k => k.x);
            int maxX = map.Keys.Max(k => k.x);
            int minY = map.Keys.Min(k => k.y);
            int maxY = map.Keys.Max(k => k.y);
            Debug.Log($"[RpgMapManager] X: {minX}..{maxX}, Y: {minY}..{maxY}");
        }
    }

    // World position -> OverlayTile (try exact cell via nearest overlay fallback)
    public OverlayTile GetTileFromWorld(Vector3 worldPosition)
    {
        if (map == null || map.Count == 0) return null;

        float best = float.MaxValue;
        OverlayTile bestTile = null;

        foreach (var kv in map)
        {
            var t = kv.Value;
            if (t == null) continue;

            float d = (t.transform.position - worldPosition).sqrMagnitude;
            if (d < best)
            {
                best = d;
                bestTile = t;
            }
        }

        return bestTile;
    }

    // 4-direction neighbours (same-height tolerance)
    public List<OverlayTile> GetFourDirectionNeighbours(OverlayTile currentTile)
    {
        var result = new List<OverlayTile>();
        if (currentTile == null || map == null || map.Count == 0) return result;

        var origin = new Vector2Int(currentTile.gridLocation.x, currentTile.gridLocation.y);

        TryAdd(origin + Vector2Int.right);
        TryAdd(origin + Vector2Int.left);
        TryAdd(origin + Vector2Int.up);
        TryAdd(origin + Vector2Int.down);

        return result;

        void TryAdd(Vector2Int key)
        {
            if (!map.TryGetValue(key, out var cand) || cand == null) return;
            float dz = Mathf.Abs(cand.transform.position.z - currentTile.transform.position.z);
            if (dz < 1f) result.Add(cand);
        }
    }

    // same name as battle MapManager
    public List<OverlayTile> GetSurroundingTiles(Vector2Int originTile)
    {
        var list = new List<OverlayTile>();
        if (!map.TryGetValue(originTile, out var origin) || origin == null) return list;
        return GetFourDirectionNeighbours(origin);
    }

    // eight directions with same-height tolerance
    public List<OverlayTile> GetSurroundingTilesEightDirections(Vector2Int originTile)
    {
        var list = new List<OverlayTile>();
        if (!map.TryGetValue(originTile, out var origin) || origin == null) return list;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                var key = new Vector2Int(originTile.x + dx, originTile.y + dy);
                if (!map.TryGetValue(key, out var cand) || cand == null) continue;

                float dz = Mathf.Abs(cand.transform.position.z - origin.transform.position.z);
                if (dz < 1f) list.Add(cand);
            }
        }

        return list;
    }

    // Snap all CharacterInfo to nearest overlay center and set standOnTile
    private void SnapAllCharactersToNearestTiles()
    {
        var characters = FindObjectsOfType<CharacterInfo>();
        foreach (var ch in characters)
        {
            var tile = GetTileFromWorld(ch.transform.position);
            if (tile == null) continue;

            var p = tile.transform.position;
            ch.transform.position = new Vector3(p.x, p.y, ch.transform.position.z);
            ch.standOnTile = tile;
        }
    }
}