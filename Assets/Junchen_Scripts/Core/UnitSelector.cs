using UnityEngine;
using System.Linq;

public class UnitSelector : MonoBehaviour
{
    // Public static reference to the currently selected player unit
    public static BaseUnit currentUnit;

    void Update()
    {
        // Only allow selection after battle has started
        if (!TurnSystem.Instance.battleStarted)
            return;

        // Only allow selection during PlayerPlanning phase
        if (!TurnSystem.Instance.IsPlanningPhase())
            return;

        // Check for left mouse click
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 mousePos2D = new Vector2(mouseWorldPos.x, mouseWorldPos.y);

            // Raycast to find clicked OverlayTile
            RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);
            if (hit.collider != null)
            {
                OverlayTile tile = hit.collider.GetComponent<OverlayTile>();
                if (tile != null)
                {
                    // Search for a player unit standing on this tile
                    BaseUnit unit = FindObjectsOfType<BaseUnit>()
                        .FirstOrDefault(u => u.standOnTile == tile && u.teamType == UnitTeam.Player && !u.hasFinishedAction);

                    if (unit != null)
                    {
                        currentUnit = unit;
                        Debug.Log($"Unit selected: {unit.name} at tile {tile.grid2DLocation}");

                        // Show action panel if in PlayerPlanning phase
                        if (TurnSystem.Instance.IsPlanningPhase())
                        {
                            PlannerActionPanelController panel = FindObjectOfType<PlannerActionPanelController>();
                            if (panel != null)
                            {
                                panel.Show();
                            }
                        }
                    }
                    else
                    {
                        currentUnit = null;
                        Debug.Log("No selectable unit on this tile, selection cleared.");

                        // Hide action panel when deselecting
                        PlannerActionPanelController panel = FindObjectOfType<PlannerActionPanelController>();
                        if (panel != null)
                        {
                            panel.Hide();
                        }
                    }
                }
            }
        }
    }
}