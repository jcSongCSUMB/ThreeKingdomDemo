using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Junchen_Scripts.Movement
{
    /// <summary>
    /// RPGClickController
    /// Handles click-to-move in the RPG scene.
    /// UPDATED 2025-08-08: Adds hover-based path preview (arrows) and keeps click-to-move flow unchanged.
    /// </summary>
    public class RPGClickController : MonoBehaviour
    {
        public RpgMapManager map;
        public CharacterInfo player;
        public int maxSteps = 5;
        public float moveSpeed = 6f;

        private Camera cam;
        private RPGPathFinder rpgPathFinder;

        // Arrow rendering support for RPG path preview
        private ArrowTranslator arrowTranslator;                   // plain class; instantiate directly
        private readonly List<OverlayTile> lastArrowTiles = new List<OverlayTile>(); // rendered tiles (preview or confirmed path)
        private OverlayTile lastHoverTile;                         // cache last hovered tile to avoid redundant recompute

        private void Awake()
        {
            cam = Camera.main;

            // prefer new API; if not supported in your Unity version, we can revert
            if (map == null)
            {
                map = Object.FindFirstObjectByType<RpgMapManager>();
            }

            if (map != null)
            {
                rpgPathFinder = new RPGPathFinder(map);
            }

            // ArrowTranslator is not a MonoBehaviour
            if (arrowTranslator == null)
            {
                arrowTranslator = new ArrowTranslator();
            }
        }

        private void Update()
        {
            // Do not interact through UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            // Hover path preview (recompute only when hovered tile changes)
            HandleHoverPreview(); // keeps cost low by recomputing only on tile change

            // Left click to start movement along current target
            if (Input.GetMouseButtonDown(0))
            {
                TryHandleClick();
            }
        }

        // Preview path while cursor hovers over a new tile
        private void HandleHoverPreview()
        {
            if (map == null || player == null || rpgPathFinder == null) return;

            if (player.standOnTile == null)
            {
                player.standOnTile = map.GetTileFromWorld(player.transform.position);
                if (player.standOnTile == null) return;
            }

            var mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0f;
            var hover = map.GetTileFromWorld(mouseWorld);

            // Only when the hovered tile changed, recompute preview
            if (hover == lastHoverTile) return;
            lastHoverTile = hover;

            // Clear previous preview first
            ClearPathArrows(); // UPDATED 2025-08-08

            if (hover == null) return;

            // Only preview if within step bound
            if (!IsWithinStepsBoundedBfs(player.standOnTile, hover, maxSteps))
            {
                return;
            }

            // Compute a temporary path and render arrows as preview
            List<OverlayTile> previewPath = rpgPathFinder.FindPath(player.standOnTile, hover, new List<OverlayTile>());
            if (previewPath == null || previewPath.Count == 0) return;

            RenderPathArrows(previewPath); // UPDATED 2025-08-08
        }

        // Handle click and initiate movement if within range
        private void TryHandleClick()
        {
            if (map == null || player == null || rpgPathFinder == null) return;

            if (player.standOnTile == null)
            {
                player.standOnTile = map.GetTileFromWorld(player.transform.position);
                if (player.standOnTile == null) return;
            }

            var mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0f;
            var target = map.GetTileFromWorld(mouseWorld);
            if (target == null) return;

            var start = player.standOnTile;

            // Step bound check
            if (!IsWithinStepsBoundedBfs(start, target, maxSteps))
            {
                return;
            }

            // Full path for execution
            List<OverlayTile> path = rpgPathFinder.FindPath(start, target, new List<OverlayTile>());
            if (path == null || path.Count == 0) return;

            // Optional: re-render to ensure arrows match final path (harmless even if identical to preview)
            ClearPathArrows();
            RenderPathArrows(path);

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

                player.standOnTile = path[i];
            }

            // Clean up arrows after movement completes
            ClearPathArrows();
            lastHoverTile = null; // reset hover so next mouse move will recompute preview
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

        // Arrow rendering helpers (RPG only)
        private void RenderPathArrows(List<OverlayTile> path)
        {
            if (arrowTranslator == null || path == null || path.Count == 0) return;

            for (int i = 0; i < path.Count; i++)
            {
                OverlayTile previous = (i > 0) ? path[i - 1] : null;
                OverlayTile current  = path[i];
                OverlayTile next     = (i < path.Count - 1) ? path[i + 1] : null;

                var dir = arrowTranslator.TranslateDirection(previous, current, next);
                current.SetSprite(dir);

                lastArrowTiles.Add(current);
            }
        }

        private void ClearPathArrows()
        {
            if (lastArrowTiles.Count == 0) return;

            for (int i = 0; i < lastArrowTiles.Count; i++)
            {
                var t = lastArrowTiles[i];
                if (t != null)
                {
                    t.SetSprite(ArrowTranslator.ArrowDirection.None);
                }
            }
            lastArrowTiles.Clear();
        }
    }
}