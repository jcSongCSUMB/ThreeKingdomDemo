using UnityEngine;

public class TileClickSpawner : MonoBehaviour
{
    void Update()
    {
        // Left-click detection
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
                    // Check if tile is already occupied
                    if (tile.isBlocked)
                    {
                        Debug.Log("This tile is already occupied. Cannot deploy unit here.");
                        return;
                    }

                    // Instantiate unit on the tile
                    GameObject newUnit = Instantiate(UnitDeployManager.Instance.selectedUnitPrefab);
                    newUnit.transform.position = tile.transform.position;

                    // Optional: set as child of Grid
                    newUnit.transform.SetParent(GameObject.Find("Grid").transform);

                    // Link character info to the tile
                    CharacterInfo info = newUnit.GetComponent<CharacterInfo>();
                    if (info != null)
                    {
                        info.standOnTile = tile;
                    }

                    // Mark tile as blocked
                    tile.MarkAsBlocked();

                    // Clear current selection after deployment
                    UnitDeployManager.Instance.ClearSelection();

                    // Disable the last clicked button (if any)
                    GameObject lastButton = UnitDeployManager.Instance.lastSelectedButton;
                    if (lastButton != null)
                    {
                        UnitButtonController controller = lastButton.GetComponent<UnitButtonController>();
                        if (controller != null)
                        {
                            controller.DisableButton();
                        }
                    }

                    Debug.Log($"Unit spawned on tile: {tile.name}");
                }
            }
        }
    }
}