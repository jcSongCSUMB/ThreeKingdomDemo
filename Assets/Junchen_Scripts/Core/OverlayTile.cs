using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ArrowTranslator;

public class OverlayTile : MonoBehaviour
{
    public int G;
    public int H;
    public int F { get { return G + H; } }

    public bool isBlocked = false;  // Whether this tile is occupied by a unit

    public OverlayTile Previous;
    public Vector3Int gridLocation;
    public Vector2Int grid2DLocation { get { return new Vector2Int(gridLocation.x, gridLocation.y); } }

    public List<Sprite> arrows;

    // Flags indicating which deploy zone this tile belongs to
    public bool isPlayerDeployZone = false;
    public bool isEnemyDeployZone = false;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            HideTile();
        }
    }

    // Hides the tile's main sprite (alpha = 0)
    public void HideTile()
    {
        gameObject.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0);
    }

    // Shows the tile's main sprite (alpha = 1)
    public void ShowTile()
    {
        gameObject.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1);
    }

    // Controls the optional directional arrow sprite display
    public void SetSprite(ArrowDirection d)
    {
        if (d == ArrowDirection.None)
            GetComponentsInChildren<SpriteRenderer>()[1].color = new Color(1, 1, 1, 0);
        else
        {
            GetComponentsInChildren<SpriteRenderer>()[1].color = new Color(1, 1, 1, 1);
            GetComponentsInChildren<SpriteRenderer>()[1].sprite = arrows[(int)d];
            GetComponentsInChildren<SpriteRenderer>()[1].sortingOrder = gameObject.GetComponent<SpriteRenderer>().sortingOrder;
        }
    }

    // Marks this tile as occupied (unit is deployed here)
    public void MarkAsBlocked()
    {
        isBlocked = true;
    }

    // Unmarks this tile (e.g., after unit is removed)
    public void Unblock()
    {
        isBlocked = false;
    }
}