using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// --------------------------------------------------------------
// Turn system phases
// --------------------------------------------------------------
public enum TurnPhase
{
    PlayerPlanning,
    PlayerExecuting,
    EnemyTurn
}

public class TurnSystem : MonoBehaviour
{
    public static TurnSystem Instance;

    public bool battleStarted = false;
    public TurnPhase currentPhase = TurnPhase.PlayerPlanning;
    public int currentTurn = 1;

    private List<BaseUnit> allUnits = new List<BaseUnit>();
    private int executingIndex = 0;

    // battle-end bookkeeping
    [SerializeField] private BattleResultUIController battleResultUI;
    private bool resultShown = false;       // ensure single win/lose trigger
    private int initialPlayerCount;
    private int initialEnemyCount;

    // capture initial counts once, at the real start of battle
    private bool initialCaptured = false;

    // Bridge (added)
    [Header("Bridge")]
    [SerializeField] private BattleBridge battleBridge;             // assign the shared SO asset
    [SerializeField] private string currentQuestId = "quest_001";   // temp: single-quest demo
    [SerializeField] private bool logBridgeWrite = false;           // optional debug toggle

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // keep enemy tiles blocked at first planning phase
        MarkEnemyTilesAsTurnBlocked();
    }

    public void RegisterUnit(BaseUnit unit)
    {
        if (!allUnits.Contains(unit))
            allUnits.Add(unit);
    }

    public void RemoveUnit(BaseUnit unit)
    {
        if (allUnits.Contains(unit))
            allUnits.Remove(unit);
    }

    // capture initial counts once, using deploy manager as source of truth
    private void EnsureInitialCaptured()
    {
        if (initialCaptured) return;
        initialPlayerCount = UnitDeployManager.Instance.GetAllDeployedPlayerUnits().Count;
        initialEnemyCount  = UnitDeployManager.Instance.GetAllDeployedEnemyUnits().Count;
        initialCaptured = true;
    }

    // Transition to the next phase of the turn cycle
    public void NextPhase()
    {
        Debug.Log($"[TurnSystem] === NextPhase called. CurrentPhase={currentPhase} ===");

        switch (currentPhase)
        {
            case TurnPhase.PlayerPlanning:
                // first time we leave Planning, capture initial counts once
                EnsureInitialCaptured();

                PlayerPlanning_EndTransformTempToTurn();
                currentPhase = TurnPhase.PlayerExecuting;
                Debug.Log("[TurnSystem] Switching to PlayerExecuting phase.");
                allUnits = UnitDeployManager.Instance.GetAllDeployedPlayerUnits();
                StartCoroutine(PlayerExecutor.Execute(allUnits));
                break;

            case TurnPhase.PlayerExecuting:
                if (TryEndBattleIfOneSideEliminated()) return;

                currentPhase = TurnPhase.EnemyTurn;
                Debug.Log("[TurnSystem] Switching to EnemyTurn phase.");
                allUnits = UnitDeployManager.Instance.GetAllDeployedEnemyUnits();
                StartCoroutine(EnemyExecutor.Execute(allUnits));
                break;

            case TurnPhase.EnemyTurn:
                if (TryEndBattleIfOneSideEliminated()) return;

                currentPhase = TurnPhase.PlayerPlanning;
                currentTurn++;
                Debug.Log("[TurnSystem] Switching to PlayerPlanning phase.");

                ClearAllTempBlocked_FULL();
                ClearAllTurnBlockedTiles();
                MarkAllLiveUnitsTilesAsTurnBlocked();
                MarkEnemyTilesAsTurnBlocked();

                allUnits = UnitDeployManager.Instance.GetAllDeployedPlayerUnits();
                ResetUnitStates(UnitTeam.Player);
                ClearAllPlayerPlans();
                RemoveAllTemporaryDefenseBonuses();
                ClearAllTempBlockedTiles();
                RebindAllUnitsToCurrentTiles();

                // extra safety: check once more right after enemy turn finishes
                if (TryEndBattleIfOneSideEliminated()) return;

                break;
        }
    }

    // single entry point for win/lose detection
    private bool TryEndBattleIfOneSideEliminated()
    {
        if (resultShown) return true;   // already resolved

        // if for any reason we haven't captured initial counts yet, do it now (once)
        EnsureInitialCaptured();

        // use deploy manager lists as the single source of truth (alive units only)
        int players = UnitDeployManager.Instance.GetAllDeployedPlayerUnits().Count;
        int enemies = UnitDeployManager.Instance.GetAllDeployedEnemyUnits().Count;

        // TEMP DEBUG
        Debug.Log($"[ENDCHK] aliveP={players}, aliveE={enemies}, initP={initialPlayerCount}, initE={initialEnemyCount}, phase={currentPhase}");

        if (players == 0)
        {
            resultShown = true;

            BattleStats stats;
            stats.playerUnitsLost = initialPlayerCount;                        // all lost
            stats.enemyUnitsLost  = Mathf.Max(0, initialEnemyCount - enemies); // clamp >= 0

            if (battleResultUI != null)
                battleResultUI.Show(BattleOutcome.Defeat, in stats);

            // Bridge write (added)
            if (battleBridge != null)
            {
                battleBridge.Set(currentQuestId, BattleOutcome.Defeat, in stats);
                if (logBridgeWrite) Debug.Log($"[TurnSystem] Bridge <- Defeat ({currentQuestId})");
            }

            battleStarted = false;
            return true;
        }

        if (enemies == 0)
        {
            resultShown = true;

            BattleStats stats;
            stats.playerUnitsLost = Mathf.Max(0, initialPlayerCount - players); // clamp >= 0
            stats.enemyUnitsLost  = initialEnemyCount;                          // all lost

            if (battleResultUI != null)
                battleResultUI.Show(BattleOutcome.Victory, in stats);

            // Bridge write (added)
            if (battleBridge != null)
            {
                battleBridge.Set(currentQuestId, BattleOutcome.Victory, in stats);
                if (logBridgeWrite) Debug.Log($"[TurnSystem] Bridge <- Victory ({currentQuestId})");
            }

            battleStarted = false;
            return true;
        }

        return false;
    }

    private void StartPlayerExecutionPhase() =>
        StartCoroutine(PlayerExecutor.Execute(allUnits));

    private void StartEnemyExecutionPhase() =>
        StartCoroutine(EnemyExecutor.Execute(allUnits));

    private void ResetUnitStates(UnitTeam team)
    {
        foreach (BaseUnit unit in allUnits)
            if (unit.teamType == team) unit.hasFinishedAction = false;
    }

    public bool IsPlanningPhase() => currentPhase == TurnPhase.PlayerPlanning;

    private void ClearAllPlayerPlans()
    {
        foreach (BaseUnit unit in allUnits)
        {
            if (unit.teamType != UnitTeam.Player) continue;
            unit.plannedPath.Clear();
            unit.plannedAction = PlannedAction.None;
            unit.targetUnit = null;
        }
    }

    private void RemoveAllTemporaryDefenseBonuses()
    {
        foreach (BaseUnit unit in allUnits)
        {
            if (unit.teamType != UnitTeam.Player || unit.tempDefenseBonus <= 0) continue;
            unit.defensePower -= unit.tempDefenseBonus;
            unit.tempDefenseBonus = 0;
        }
    }

    private void PlayerPlanning_EndTransformTempToTurn()
    {
        List<BaseUnit> players = UnitDeployManager.Instance.GetAllDeployedPlayerUnits();
        HashSet<OverlayTile> finalTiles = new HashSet<OverlayTile>();

        foreach (var u in players)
        {
            OverlayTile dest = (u.plannedPath != null && u.plannedPath.Count > 0)
                               ? u.plannedPath[^1]
                               : u.standOnTile;

            if (dest == null) continue;

            dest.UnmarkTempBlocked();
            dest.MarkAsTurnBlocked();
            finalTiles.Add(dest);
        }

        foreach (var tile in FindObjectsOfType<OverlayTile>())
            if (!finalTiles.Contains(tile) && tile.isTempBlocked) tile.UnmarkTempBlocked();
    }

    private void ClearAllTempBlocked_FULL()
    {
        foreach (OverlayTile tile in FindObjectsOfType<OverlayTile>())
            if (tile.isTempBlocked || tile.tempBlockedByPlanning) tile.UnmarkTempBlocked();
    }

    private void ClearAllTempBlockedTiles()
    {
        foreach (OverlayTile tile in FindObjectsOfType<OverlayTile>())
            if (tile.tempBlockedByPlanning) { tile.tempBlockedByPlanning = false; tile.SetToDefaultSprite(); }
    }

    private void MarkEnemyTilesAsBlocked()
    {
        foreach (BaseUnit unit in allUnits)
            if (unit.teamType == UnitTeam.Enemy && unit.standOnTile != null)
            {
                unit.standOnTile.isTempBlocked = true;
                unit.standOnTile.tempBlockedByPlanning = true;
                unit.standOnTile.ShowAsTempBlocked();
            }
    }

    private void MarkEnemyTilesAsTurnBlocked()
    {
        foreach (BaseUnit unit in allUnits)
            if (unit != null && unit.gameObject != null
                && unit.teamType == UnitTeam.Enemy
                && unit.standOnTile != null)
                unit.standOnTile.MarkAsTurnBlocked();
    }

    private void ClearAllTurnBlockedTiles()
    {
        foreach (OverlayTile tile in FindObjectsOfType<OverlayTile>())
            tile.UnmarkTurnBlocked();
    }

    private void RebindAllUnitsToCurrentTiles()
    {
        foreach (var unit in allUnits)
        {
            if (unit.standOnTile == null) continue;
            Vector2Int pos = unit.standOnTile.grid2DLocation;
            if (MapManager.Instance.map.TryGetValue(pos, out var newTile))
                unit.standOnTile = newTile;
        }
    }

    private void MarkAllLiveUnitsTilesAsTurnBlocked()
    {
        foreach (BaseUnit u in FindObjectsOfType<BaseUnit>())
            if (u != null && u.gameObject != null && u.standOnTile != null)
                u.standOnTile.MarkAsTurnBlocked();
    }
}