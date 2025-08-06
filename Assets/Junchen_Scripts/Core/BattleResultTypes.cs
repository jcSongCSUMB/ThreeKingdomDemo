// Shared types used by TurnSystem and BattleResult UI.
public enum BattleOutcome
{
    Victory,
    Defeat
}

public struct BattleStats
{
    public int playerUnitsLost;
    public int enemyUnitsLost;
}