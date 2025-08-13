using UnityEngine;

[CreateAssetMenu(fileName = "BattleBridge", menuName = "Junchen/Battle Bridge")]
public class BattleBridge : ScriptableObject
{
    [Header("Flags")]
    public bool hasPending; // true if there is a result to process

    [Header("Context")]
    public string questId; // quest ID related to this battle result

    [Header("Outcome")]
    public BattleOutcome outcome;   // reuse existing enum
    public BattleStats stats;       // reuse existing stats struct/class

    // Set the pending result
    public void Set(string qid, BattleOutcome oc, in BattleStats st)
    {
        hasPending = true;
        questId = qid;
        outcome = oc;
        stats = st;
    }

    // Clear after processing in RPG scene
    public void Clear()
    {
        hasPending = false;
        questId = null;
        outcome = default;
        stats = default;
    }
}

