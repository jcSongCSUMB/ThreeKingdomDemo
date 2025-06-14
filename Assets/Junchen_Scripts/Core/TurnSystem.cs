using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum TurnPhase
{
    PlayerPlanning,
    PlayerExecuting,
    EnemyTurn
}

public class TurnSystem : MonoBehaviour
{
    public static TurnSystem Instance;

    // Current phase of the battle
    public TurnPhase currentPhase = TurnPhase.PlayerPlanning;

    // Current round number
    public int currentTurn = 1;

    // List of all units on the field
    private List<BaseUnit> allUnits = new List<BaseUnit>();

    // Index for executing units during PlayerExecuting
    private int executingIndex = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Moves to the next battle phase
    public void NextPhase()
    {
        switch (currentPhase)
        {
            case TurnPhase.PlayerPlanning:
                currentPhase = TurnPhase.PlayerExecuting;
                Debug.Log("[TurnSystem] → PlayerExecuting");

                // Collect all player-controlled units
                allUnits = FindObjectsOfType<BaseUnit>()
                    .Where(u => u.teamType == UnitTeam.Player)
                    .ToList();

                executingIndex = 0;

                // TODO: Start executing unit actions
                break;

            case TurnPhase.PlayerExecuting:
                currentPhase = TurnPhase.EnemyTurn;
                Debug.Log("[TurnSystem] → EnemyTurn");

                // TODO: Trigger enemy AI logic
                break;

            case TurnPhase.EnemyTurn:
                currentTurn++;
                currentPhase = TurnPhase.PlayerPlanning;
                Debug.Log($"[TurnSystem] → Round {currentTurn} begins");

                // Reset all units for the new planning phase
                foreach (BaseUnit unit in FindObjectsOfType<BaseUnit>())
                {
                    unit.hasFinishedAction = false;
                    unit.plannedPath.Clear();
                    unit.plannedAction = PlannedAction.None;
                }

                break;
        }
    }

    // Returns true if it's currently the PlayerPlanning phase
    public bool IsPlanningPhase()
    {
        return currentPhase == TurnPhase.PlayerPlanning;
    }
}