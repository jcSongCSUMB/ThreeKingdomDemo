using UnityEngine;
using TMPro;

namespace Junchen_Scripts.Quest
{
    /// <summary>
    /// Consume one-shot battle result from BattleBridge when the RPG scene loads,
    /// then update quest state and clean visuals. Designed to run once in Start().
    /// </summary>
    public class BattleReturnHandler : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private BattleBridge battleBridge;   // same asset used by TurnSystem in Battle scene
        [SerializeField] private QuestManager questManager;   // scene QuestManager
        [SerializeField] private TMP_Text hudText;            // optional HUD text (can be null)

        private void Start()
        {
            // No bridge or nothing to process → bail out
            if (battleBridge == null || !battleBridge.hasPending) return;

            // Only handle when the returned questId matches the current quest
            string expectedId =
                (questManager != null && questManager.currentQuest != null)
                    ? questManager.currentQuest.questId
                    : null;

            if (!string.IsNullOrEmpty(expectedId) && battleBridge.questId == expectedId)
            {
                // Minimal behavior for tonight: symmetric cleanup
                if (questManager != null)
                    questManager.DeactivateCurrentQuest();

                // Optional HUD feedback
                if (hudText != null)
                {
                    hudText.SetText(
                        battleBridge.outcome == BattleOutcome.Victory
                            ? "Quest Complete."
                            : "Quest Failed."
                    );
                }
            }

            // Always clear the bridge to avoid re-processing
            battleBridge.Clear();
        }
    }
}