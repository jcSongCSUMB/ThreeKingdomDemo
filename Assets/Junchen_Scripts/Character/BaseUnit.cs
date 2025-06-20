using System.Collections.Generic;
using UnityEngine;

public enum PlannedAction
{
    None,
    Attack,
    Defend
}

public enum UnitTeam
{
    Player,
    Enemy
}

public class BaseUnit : MonoBehaviour
{
    // The tile this unit is currently standing on
    public OverlayTile standOnTile;

    // The planned movement path for this unit
    public List<OverlayTile> plannedPath = new List<OverlayTile>();

    // The intended action this unit will perform after moving
    public PlannedAction plannedAction = PlannedAction.None;

    // Whether this unit has finished its action this round
    public bool hasFinishedAction = false;

    // The team this unit belongs to (Player or Enemy)
    public UnitTeam teamType;

    // === [New fields for combat and planning] ===

    // Current HP of the unit
    public int health = 100;

    // Max HP of the unit
    public int maxHealth = 100;

    // Unit's base attack power
    public int attackPower = 30;

    // Unit's base defense power
    public int defensePower = 10;

    // Unit's action points (used for movement and attacks)
    public int actionPoints = 3;

    // Unit type string (e.g., "Infantry", "Archer")
    public string unitType = "Infantry";

    // Attack target (set in attack planning)
    public BaseUnit targetUnit;
}