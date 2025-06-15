using System.Collections.Generic;
using UnityEngine;

public enum TurnPhase
{
    PlayerPlanning,
    PlayerExecuting,
    EnemyTurn
}

public class TurnSystem : MonoBehaviour
{
    // Singleton instance
    public static TurnSystem Instance;

    // Flag to indicate battle phase has started
    public bool battleStarted = false;

    // Current phase of the turn
    public TurnPhase currentPhase = TurnPhase.PlayerPlanning;

    // Current turn number
    public int currentTurn = 1;

    // List of all units in the battle
    private List<BaseUnit> allUnits = new List<BaseUnit>();

    // Index used to track execution order
    private int executingIndex = 0;

    private void Awake()
    {
        // Ensure only one instance of TurnSystem exists
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Add a unit to the list if not already present
    public void RegisterUnit(BaseUnit unit)
    {
        if (!allUnits.Contains(unit))
        {
            allUnits.Add(unit);
        }
    }

    // Move to the next phase in the turn cycle
    public void NextPhase()
    {
        switch (currentPhase)
        {
            case TurnPhase.PlayerPlanning:
                currentPhase = TurnPhase.PlayerExecuting;
                ResetUnitStates(UnitTeam.Player);
                executingIndex = 0;

                // TODO: Execute all planned actions by player units
                break;

            case TurnPhase.PlayerExecuting:
                currentPhase = TurnPhase.EnemyTurn;

                // TODO: Trigger AI logic for enemy turn
                break;

            case TurnPhase.EnemyTurn:
                currentPhase = TurnPhase.PlayerPlanning;
                currentTurn++;

                // TODO: Reset board and allow new planning
                break;
        }

        Debug.Log($"Turn Phase changed to: {currentPhase}");
    }

    // Reset action status of all units on a team
    private void ResetUnitStates(UnitTeam team)
    {
        foreach (BaseUnit unit in allUnits)
        {
            if (unit.teamType == team)
            {
                unit.hasFinishedAction = false;
            }
        }
    }

    // Check if it's currently the player planning phase
    public bool IsPlanningPhase()
    {
        return currentPhase == TurnPhase.PlayerPlanning;
    }
}