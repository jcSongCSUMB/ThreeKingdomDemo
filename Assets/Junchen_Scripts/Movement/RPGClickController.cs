using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class RPGClickController : MonoBehaviour
{
    // Reference to the RpgMapManager in the scene
    public RpgMapManager map;

    // Reference to the player unit's CharacterInfo
    public CharacterInfo player;

    // Maximum step distance allowed
    public int maxSteps = 5;

    // Movement speed in units per second
    public float moveSpeed = 6f;

    // Cached main camera
    private Camera cam;

    // Instance of RPGPathFinder for this scene
    private RPGPathFinder rpgPathFinder;

    private void Awake()
    {
        cam = Camera.main;

        // Auto-find map reference if not assigned
        if (map == null)
        {
            map = FindObjectOfType<RpgMapManager>();
        }

        // Initialize pathfinder
        if (map != null)
        {
            rpgPathFinder = new RPGPathFinder(map);
        }
    }

    private void Update()
    {
        // Ignore if mouse is over UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // Left click to attempt movement
        if (Input.GetMouseButtonDown(0))
        {
            TryHandleClick();
        }
    }

    // Handle click and initiate movement if within range
    private void TryHandleClick()
    {
        if (map == null || player == null || rpgPathFinder == null) return;

        // Ensure player has a valid standOnTile
        if (player.standOnTile == null)
        {
            player.standOnTile = map.GetTileFromWorld(player.transform.position);
            if (player.standOnTile == null) return;
        }

        // Get clicked world position and corresponding tile
        var mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        var target = map.GetTileFromWorld(mouseWorld);
        if (target == null) return;

        var start = player.standOnTile;

        // Check if target tile is within maxSteps using BFS
        if (!IsWithinStepsBoundedBfs(start, target, maxSteps))
        {
            return;
        }

        // Get path (pass empty list for inRangeTiles to search full map)
        List<OverlayTile> path = rpgPathFinder.FindPath(start, target, new List<OverlayTile>());
        if (path == null || path.Count == 0) return;

        // Stop any existing movement and start new path coroutine
        StopAllCoroutines();
        StartCoroutine(MoveAlongPath(path));
    }

    // Coroutine to move player along path one tile at a time
    private IEnumerator MoveAlongPath(List<OverlayTile> path)
    {
        for (int i = 0; i < path.Count; i++)
        {
            var p = path[i].transform.position;
            var dst = new Vector3(p.x, p.y, player.transform.position.z);

            while ((player.transform.position - dst).sqrMagnitude > 0.0001f)
            {
                player.transform.position = Vector3.MoveTowards(player.transform.position, dst, moveSpeed * Time.deltaTime);
                yield return null;
            }

            // Update player's current tile
            player.standOnTile = path[i];
        }
    }

    // Check if target is within max steps using BFS (4-directional)
    private bool IsWithinStepsBoundedBfs(OverlayTile start, OverlayTile target, int max)
    {
        if (start == null || target == null) return false;
        if (start == target) return true;

        var q = new Queue<OverlayTile>();
        var dist = new Dictionary<OverlayTile, int>();

        q.Enqueue(start);
        dist[start] = 0;

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            int d = dist[cur];
            if (d >= max) continue;

            var neighbours = map.GetFourDirectionNeighbours(cur);
            for (int i = 0; i < neighbours.Count; i++)
            {
                var nb = neighbours[i];
                if (dist.ContainsKey(nb)) continue;

                dist[nb] = d + 1;
                if (nb == target) return true;
                q.Enqueue(nb);
            }
        }

        return false;
    }
}