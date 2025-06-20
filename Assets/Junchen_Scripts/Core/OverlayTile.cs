using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ArrowTranslator;

public class OverlayTile : MonoBehaviour
{
    public int G;
    public int H;
    public int F { get { return G + H; } }

    public bool isBlocked = false;      // Permanent block (deployment)
    public bool isTempBlocked = false;  // Temporary block during planning
    public bool tempBlockedByPlanning = false; // Used by TurnSystem to clean up

    public OverlayTile Previous;
    public Vector3Int gridLocation;
    public Vector2Int grid2DLocation { get { return new Vector2Int(gridLocation.x, gridLocation.y); } }

    public List<Sprite> arrows;

    public bool isPlayerDeployZone = false;
    public bool isEnemyDeployZone = false;

    public Sprite tempBlockedSprite; // Assigned in inspector
    private Sprite defaultSprite;    // Backed-up default sprite

    private void Start()
    {
        defaultSprite = GetComponent<SpriteRenderer>().sprite;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            HideTile();
        }
    }

    // Make tile invisible
    public void HideTile()
    {
        GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0);
    }

    // Make tile visible with full alpha
    public void ShowTile()
    {
        GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1);
    }

    // Show red tile by switching sprite (for planning block)
    public void ShowAsTempBlocked()
    {
        if (tempBlockedSprite != null)
        {
            GetComponent<SpriteRenderer>().sprite = tempBlockedSprite;
            tempBlockedByPlanning = true;
        }
    }

    // Reset sprite to original and clear visual overlay
    public void UnmarkTempBlocked()
    {
        isTempBlocked = false;
        tempBlockedByPlanning = false;
        GetComponent<SpriteRenderer>().sprite = defaultSprite;
        HideTile(); // Optional: you can replace with ShowTile() if needed
    }

    // Restore default appearance (used by TurnSystem)
    public void SetToDefaultSprite()
    {
        GetComponent<SpriteRenderer>().sprite = defaultSprite;
    }

    // Clear arrows (for hover preview)
    public void SetSprite(ArrowDirection d)
    {
        var arrowRenderer = GetComponentsInChildren<SpriteRenderer>()[1];

        if (d == ArrowDirection.None)
        {
            arrowRenderer.color = new Color(1, 1, 1, 0);
        }
        else
        {
            arrowRenderer.color = new Color(1, 1, 1, 1);
            arrowRenderer.sprite = arrows[(int)d];
            arrowRenderer.sortingOrder = GetComponent<SpriteRenderer>().sortingOrder;
        }
    }

    public void MarkAsBlocked()
    {
        isBlocked = true;
    }

    public void Unblock()
    {
        isBlocked = false;
    }

    public void MarkAsTempBlocked()
    {
        isTempBlocked = true;
        ShowAsTempBlocked();
    }

    public void ClearVisualState()
    {
        HideTile();
        SetSprite(ArrowDirection.None);
    }
}