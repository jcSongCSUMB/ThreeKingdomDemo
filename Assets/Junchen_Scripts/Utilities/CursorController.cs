using System.Linq;
using UnityEngine;

public class CursorController : MonoBehaviour
{
    public GameObject cursor; // Visual cursor GameObject

    void Update()
    {
        RaycastHit2D? hit = GetTileUnderCursor();

        if (hit.HasValue)
        {
            OverlayTile tile = hit.Value.collider.GetComponent<OverlayTile>();
            if (tile != null)
            {
                // Move the cursor to the tile's position
                cursor.transform.position = tile.transform.position;

                // Adjust sorting order to match the tile
                SpriteRenderer cursorRenderer = cursor.GetComponent<SpriteRenderer>();
                if (cursorRenderer != null)
                {
                    cursorRenderer.sortingOrder = tile.GetComponent<SpriteRenderer>().sortingOrder;
                }
            }
        }
    }

    // Returns the topmost OverlayTile under the mouse
    private RaycastHit2D? GetTileUnderCursor()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mousePos2D = new Vector2(mouseWorldPos.x, mouseWorldPos.y);

        RaycastHit2D[] hits = Physics2D.RaycastAll(mousePos2D, Vector2.zero);
        if (hits.Length > 0)
        {
            return hits.OrderByDescending(i => i.collider.transform.position.z).First();
        }

        return null;
    }
}
