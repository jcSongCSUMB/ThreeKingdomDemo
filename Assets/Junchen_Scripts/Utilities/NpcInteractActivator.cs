using UnityEngine;

/// <summary>
/// Shows/hides the NPC's Interact UI based on Manhattan distance to the player.
/// - Show when manhattan <= showThreshold
/// - Hide when manhattan >= hideThreshold (hysteresis to avoid flicker)
/// Uses Grid.WorldToCell when a Grid is provided; otherwise falls back to rounded world positions.
/// Supports XY or XZ plane for distance calculation (isometric scenes often use XZ).
/// </summary>
public class NpcInteractActivator : MonoBehaviour
{
    public enum PlaneMode { XY, XZ } // NEW: choose which two axes form the grid plane

    [Header("References")]
    [Tooltip("Player transform used to measure distance.")]
    public Transform player;

    [Tooltip("Root GameObject of the NPC's Interact UI (e.g., Canvas or the Interact Button).")]
    public GameObject interactRoot;

    [Header("Distance (Manhattan)")]
    [Tooltip("Show Interact when manhattan distance <= this value.")]
    public int showThreshold = 2;

    [Tooltip("Hide Interact when manhattan distance >= this value (should be >= showThreshold).")]
    public int hideThreshold = 3;

    [Header("Grid/Tilemap (Optional)")]
    [Tooltip("If assigned, uses Grid.WorldToCell(player/npc) to compute tile coordinates.")]
    public Grid grid; // prefer true grid coordinates when available

    [Header("Grid Plane")]
    [Tooltip("Choose which two axes define your grid plane. 2D Tilemap usually XY; many isometric layouts use XZ.")]
    public PlaneMode plane = PlaneMode.XY; // NEW: default XY

    [Header("Fallback (No Grid)")]
    [Tooltip("If no Grid is assigned, uses rounded world positions as pseudo-grid.")]
    public bool useRoundedWorldAsGrid = true; // kept for backward compatibility

    private bool _isVisible;

    private void Awake()
    {
        ApplyVisibility(false); // start hidden by default
    }

    private void Update()
    {
        if (player == null || interactRoot == null) return;

        int manhattan = ComputeManhattan();

        if (!_isVisible && manhattan <= showThreshold)
        {
            ApplyVisibility(true);
        }
        else if (_isVisible && manhattan >= hideThreshold)
        {
            ApplyVisibility(false);
        }
    }

    private int ComputeManhattan()
    {
        // Prefer real tile coordinates via Grid.WorldToCell when grid is provided
        if (grid != null)
        {
            Vector3Int npcCell = grid.WorldToCell(transform.position);
            Vector3Int playerCell = grid.WorldToCell(player.position);

            int dx = Mathf.Abs(playerCell.x - npcCell.x);
            int dSecond = (plane == PlaneMode.XY)
                ? Mathf.Abs(playerCell.y - npcCell.y)   // XY: use y
                : Mathf.Abs(playerCell.z - npcCell.z);  // XZ: use z

            return dx + dSecond;
        }

        // Fallback: approximate using world positions (back-compatible)
        Vector3 npcPos3 = transform.position;
        Vector3 playerPos3 = player.position;

        float nx = npcPos3.x;
        float nSecond = (plane == PlaneMode.XY) ? npcPos3.y : npcPos3.z;
        float px = playerPos3.x;
        float pSecond = (plane == PlaneMode.XY) ? playerPos3.y : playerPos3.z;

        int gx_nx, gx_nSecond, gx_px, gx_pSecond;
        if (useRoundedWorldAsGrid)
        {
            gx_nx = Mathf.RoundToInt(nx);
            gx_nSecond = Mathf.RoundToInt(nSecond);
            gx_px = Mathf.RoundToInt(px);
            gx_pSecond = Mathf.RoundToInt(pSecond);
        }
        else
        {
            gx_nx = (int)nx;
            gx_nSecond = (int)nSecond;
            gx_px = (int)px;
            gx_pSecond = (int)pSecond;
        }

        int dxFallback = Mathf.Abs(gx_px - gx_nx);
        int dSecondFallback = Mathf.Abs(gx_pSecond - gx_nSecond);
        return dxFallback + dSecondFallback;
    }

    private void ApplyVisibility(bool visible)
    {
        _isVisible = visible;
        if (interactRoot != null && interactRoot.activeSelf != visible)
        {
            interactRoot.SetActive(visible);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (hideThreshold < showThreshold) hideThreshold = showThreshold;
    }
#endif
}