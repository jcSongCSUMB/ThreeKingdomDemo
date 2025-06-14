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
}
