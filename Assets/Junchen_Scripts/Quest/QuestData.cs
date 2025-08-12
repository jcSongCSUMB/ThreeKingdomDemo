using UnityEngine;

namespace Junchen_Scripts.Quest
{
    /// <summary>
    /// Data-only quest configuration. No flow logic here.
    /// </summary>
    [CreateAssetMenu(fileName = "QuestData", menuName = "Junchen/Quest Data")]
    public class QuestData : ScriptableObject
    {
        [Header("Identity")]
        public string questId = "quest_001";
        public string title = "First Battle";
        [TextArea] public string description = "Reach the target tile to start the battle.";

        [Header("Target (single cell for now)")]
        // Grid coordinate (x,y) in the same space used by RpgMapManager.map keys
        public Vector2Int targetCell = Vector2Int.zero;

        [Header("Visuals")]
        // The highlight sprite to apply on the target OverlayTile while the quest is active
        public Sprite highlightSprite;

        [Header("Scene")]
        // Battle scene name to load when the player reaches the target
        public string battleSceneName = "BattleTestLevel";

        [Header("Behavior")]
        // If true, the highlight appears immediately when the quest is activated
        public bool showHighlightOnStart = true;
    }
}