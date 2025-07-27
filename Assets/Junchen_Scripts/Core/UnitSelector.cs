using UnityEngine;
using System.Linq;
using UnityEngine.EventSystems;

public class UnitSelector : MonoBehaviour
{
    // Singleton reference to allow external access
    public static UnitSelector Instance;

    // Public static reference to the currently selected player unit
    public static BaseUnit currentUnit;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    void Update()
    {
        // Only allow selection after battle has started
        if (!TurnSystem.Instance.battleStarted)
            return;

        // Only allow selection during PlayerPlanning phase
        if (!TurnSystem.Instance.IsPlanningPhase())
            return;

        // Prevent interference if mouse is over UI element
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        // Fetch planner once
        TileClickPathPlanner planner = FindObjectOfType<TileClickPathPlanner>();

        // ===== PATCH: allow global cancel even when planner is not None =====
        if (planner != null && planner.plannerMode != PlannerMode.None)
        {
            // ESC or Right Mouse Button => force cancel back to None (release current temp plan safely)
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                planner.ForceCancel(releaseCurrentPlanning: true);
                // Optional: also hide action panel after cancel
                PlannerActionPanelController panelX = FindObjectOfType<PlannerActionPanelController>();
                if (panelX != null) panelX.Hide();
                return; // end this frame; next frame selection will be allowed
            }

            // If not cancelling, do not allow selection while in a planner mode
            return;
        }
        // ===== END PATCH =====

        // Check for left mouse click
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 mousePos2D = new Vector2(mouseWorldPos.x, mouseWorldPos.y);

            // Raycast to find clicked OverlayTile
            RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);
            if (hit.collider != null)
            {
                Debug.Log($"[Selector] Clicked object: {hit.collider.name}");

                OverlayTile tile = hit.collider.GetComponent<OverlayTile>();
                if (tile != null)
                {
                    // Search for a player unit standing on this tile
                    BaseUnit unit = FindObjectsOfType<BaseUnit>()
                        .FirstOrDefault(u => u.standOnTile == tile && u.teamType == UnitTeam.Player && !u.hasFinishedAction);

                    PlannerActionPanelController panel = FindObjectOfType<PlannerActionPanelController>();

                    if (unit != null)
                    {
                        // Reset planner state when switching unit
                        if (planner != null)
                        {
                            planner.SetPlannerMode(PlannerMode.None);
                        }

                        currentUnit = unit;
                        Debug.Log($"[Selector] Unit selected: {unit.name} at tile {tile.grid2DLocation}");

                        // Show action panel
                        if (panel != null)
                        {
                            panel.Show();
                        }

                        // If in Move mode, refresh movement range for new unit
                        if (planner != null && planner.plannerMode == PlannerMode.Move)
                        {
                            planner.SetPlannerMode(PlannerMode.Move);
                        }
                    }
                    else
                    {
                        currentUnit = null;
                        Debug.Log("[Selector] No selectable unit on this tile. Selection cleared.");

                        // Hide panel if no unit selected
                        if (panel != null)
                        {
                            panel.Hide();
                        }

                        // Clear planner state
                        if (planner != null)
                        {
                            planner.SetPlannerMode(PlannerMode.None);
                        }
                    }
                }
                else
                {
                    Debug.Log("[Selector] Clicked object is not an OverlayTile.");
                }
            }
            else
            {
                Debug.Log("[Selector] Nothing hit by Raycast.");
            }
        }
    }
}