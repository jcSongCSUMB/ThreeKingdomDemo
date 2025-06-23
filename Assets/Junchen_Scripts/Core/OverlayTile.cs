using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ArrowTranslator;

public class OverlayTile : MonoBehaviour
{
    // Pathfinding cost
    public int G;
    public int H;
    public int F { get { return G + H; } }

    // Tile status flags
    public bool isBlocked = false;             // Permanent block (e.g., terrain)
    public bool isTempBlocked = false;         // Temporary block during planning
    public bool tempBlockedByPlanning = false; // Used to track tiles for temp cleanup
    public bool isBlockedThisTurn = false;     // Blocked for the full turn (e.g., enemy tile or planned target)

    // Grid location
    public OverlayTile Previous;
    public Vector3Int gridLocation;
    public Vector2Int grid2DLocation { get { return new Vector2Int(gridLocation.x, gridLocation.y); } }

    // Deployment info
    public bool isPlayerDeployZone = false;
    public bool isEnemyDeployZone = false;

    // Sprite visuals
    public List<Sprite> arrows;
    public Sprite tempBlockedSprite;   // Assigned in inspector
    private Sprite defaultSprite;      // Sprite to restore

    private void Start()
    {
        defaultSprite = GetComponent<SpriteRenderer>().sprite;
    }

    private void Update()
    {
        // Update tile visuals each frame based on blocked state
        if (isBlockedThisTurn || isTempBlocked)
        {
            ShowAsTempBlocked();
        }
        else if (!tempBlockedByPlanning)
        {
            SetToDefaultSprite();
        }
    }

    // === Visual Helpers ===

    public void ShowAsTempBlocked()
    {
        if (tempBlockedSprite != null)
        {
            GetComponent<SpriteRenderer>().sprite = tempBlockedSprite;
        }
    }

    public void SetToDefaultSprite()
    {
        GetComponent<SpriteRenderer>().sprite = defaultSprite;
    }

    public void HideTile()
    {
        GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0);
    }

    public void ShowTile()
    {
        GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1);
    }

    public void ClearVisualState()
    {
        HideTile();
        SetSprite(ArrowDirection.None);
    }

    // === Block Status Markers ===

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
        tempBlockedByPlanning = true;
        ShowAsTempBlocked();
    }

    public void UnmarkTempBlocked()
    {
        isTempBlocked = false;
        tempBlockedByPlanning = false;
        SetToDefaultSprite();
        HideTile(); // Optional based on game state
    }

    public void MarkAsTurnBlocked()
    {
        isBlockedThisTurn = true;
        ShowAsTempBlocked();
    }

    public void UnmarkTurnBlocked()
    {
        isBlockedThisTurn = false;
        if (!tempBlockedByPlanning)
        {
            SetToDefaultSprite();
        }
    }

    // === Arrow Drawing ===

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
}