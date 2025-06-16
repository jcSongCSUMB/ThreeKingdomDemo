using UnityEngine;

public class TileClickSpawner : MonoBehaviour
{
    void Update()
    {
        // Detect left mouse button click
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 mousePos2D = new Vector2(mouseWorldPos.x, mouseWorldPos.y);

            // Raycast to detect clicked tile
            RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);
            if (hit.collider != null)
            {
                OverlayTile tile = hit.collider.GetComponent<OverlayTile>();
                if (tile != null && UnitDeployManager.Instance.selectedUnitPrefab != null)
                {
                    // Check if the tile is a valid player deploy zone
                    if (!tile.isPlayerDeployZone)
                    {
                        // Show reason based on zone type
                        if (tile.isEnemyDeployZone)
                        {
                            Debug.Log("This is an enemy deploy zone. You cannot deploy here.");
                        }
                        else
                        {
                            Debug.Log("This tile is not a valid deployment zone.");
                        }
                        return;
                    }

                    // Check if tile is already occupied
                    if (tile.isBlocked)
                    {
                        Debug.Log("This tile is already occupied. Cannot deploy unit here.");
                        return;
                    }

                    // Instantiate the selected unit prefab
                    GameObject newUnit = Instantiate(UnitDeployManager.Instance.selectedUnitPrefab);
                    newUnit.transform.position = tile.transform.position;

                    // Optional: set as child of Grid
                    newUnit.transform.SetParent(GameObject.Find("Grid").transform);

                    // Register for cleanup tracking
                    UnitDeployManager.Instance.RegisterDeployedUnit(newUnit);

                    // Attach character logic to tile
                    CharacterInfo info = newUnit.GetComponent<CharacterInfo>();
                    if (info != null)
                    {
                        info.standOnTile = tile;
                    }

                    // Set standOnTile in BaseUnit for selection logic
                    BaseUnit unit = newUnit.GetComponent<BaseUnit>();
                    if (unit != null)
                    {
                        unit.standOnTile = tile;
                    }

                    // Mark this tile as blocked
                    tile.MarkAsBlocked();

                    // Clear current unit selection
                    UnitDeployManager.Instance.ClearSelection();

                    // Disable the last selected button
                    GameObject lastButton = UnitDeployManager.Instance.lastSelectedButton;
                    if (lastButton != null)
                    {
                        UnitButtonController controller = lastButton.GetComponent<UnitButtonController>();
                        if (controller != null)
                        {
                            controller.DisableButton();
                        }
                    }

                    Debug.Log($"Unit successfully deployed on tile: {tile.gridLocation}");
                }
            }
        }
    }
}