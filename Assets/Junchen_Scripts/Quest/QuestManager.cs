using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // NEW: HUD text

namespace Junchen_Scripts.Quest
{
    /// <summary>
    /// Scene-level quest driver for the RPG scene.
    /// Minimal loop tonight: activate -> highlight target tile -> detect player enter -> load battle.
    /// Holds no design-time references to OverlayTile instances; resolves target at runtime by grid coords.
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        [Header("Refs")]
        public RpgMapManager map;          // optional but recommended (not strictly needed tonight)
        public CharacterInfo player;       // player character that updates standOnTile
        public QuestData currentQuest;     // data-only SO

        [Header("HUD")] // NEW
        [SerializeField] private TMP_Text questHudText;                 // drag HUD_Canvas/QuestText (optional)
        [SerializeField] private string questHudTemplate = "Quest: {0}"; // optional format

        [Header("State (runtime)")]
        [SerializeField] private bool questActive = false;
        [SerializeField] private OverlayTile targetTile; // resolved at activation
        [SerializeField] private Sprite originalSprite;  // to restore when deactivating
        private OverlayTile lastSeenTile;               // for change detection
        private bool loadingBattle = false;
        
        // Called by DialogueService.OnDialogueFinished (Inspector binding) to start the quest.
        public void ActivateCurrentQuest()
        {
            if (questActive || loadingBattle)
            {
                Debug.Log("[QuestManager] Activate ignored (already active or loading).");
                return;
            }
            if (currentQuest == null)
            {
                Debug.LogWarning("[QuestManager] No QuestData assigned.");
                return;
            }

            // Resolve target tile from grid coordinates at runtime.
            targetTile = FindOverlayTileByGrid(currentQuest.targetCell);
            if (targetTile == null)
            {
                Debug.LogError($"[QuestManager] Target tile not found at {currentQuest.targetCell}.");
                return;
            }

            // Optional highlight via sprite swap (kept as-is; may be visually overridden by OverlayTile.Update()).
            if (currentQuest.showHighlightOnStart && currentQuest.highlightSprite != null)
            {
                var sr = targetTile.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    originalSprite = sr.sprite;
                    sr.sprite = currentQuest.highlightSprite;
                }
            }

            // Visual lock for quest highlight (display-only, no mechanics impact)
            // Use OverlayTile's "temp blocked" visual channel so OverlayTile.Update() will keep showing tempBlockedSprite.
            // Also ensure alpha=1 (in case the tile was hidden).
            targetTile.isTempBlocked = true;            // drives ShowAsTempBlocked() each frame → tempBlockedSprite
            targetTile.tempBlockedByPlanning = true;    // prevents immediate default reversion path
            targetTile.ShowTile();                      // make sure it's visible (alpha = 1)

            questActive = true;
            lastSeenTile = (player != null) ? player.standOnTile : null;
            Debug.Log($"[QuestManager] Quest activated → target {currentQuest.targetCell}, scene '{currentQuest.battleSceneName}'.");

            // show HUD text on activation (use title if available, fallback literal)
            if (questHudText != null)
            {
                var title = (currentQuest != null && !string.IsNullOrEmpty(currentQuest.title))
                    ? currentQuest.title
                    : "Reach the target";
                questHudText.SetText(string.Format(questHudTemplate, title));
            }
        }
        
        // Minimal change-detection loop: only reacts when player's standOnTile reference changes.
        private void Update()
        {
            if (!questActive || loadingBattle || player == null || targetTile == null) return;

            var now = player.standOnTile;
            if (now == lastSeenTile) return;     // no change since last frame
            lastSeenTile = now;

            if (now == targetTile)
            {
                // one-shot trigger
                questActive = false;
                loadingBattle = true;

                // clear visual lock before leaving scene (symmetry with activation)
                targetTile.isTempBlocked = false;
                targetTile.tempBlockedByPlanning = false;
                // Optionally restore default look immediately (next frame Update() will also revert):
                // targetTile.SetToDefaultSprite();

                // (optional) restore sprite before leaving scene
                if (currentQuest.highlightSprite != null && targetTile != null)
                {
                    var sr = targetTile.GetComponent<SpriteRenderer>();
                    if (sr != null && originalSprite != null) sr.sprite = originalSprite;
                }

                Debug.Log("[QuestManager] Target reached → loading battle scene...");
                LoadBattleScene();
            }
        }

        private void LoadBattleScene()
        {
            // Minimal path tonight; tomorrow we can swap to a SceneLoader/fade.
            var sceneName = string.IsNullOrWhiteSpace(currentQuest.battleSceneName)
                ? "BattleTestLevel"
                : currentQuest.battleSceneName;

            SceneManager.LoadSceneAsync(sceneName);
        }
        
        // Finds an OverlayTile by its (x,y) grid coordinate. Works even if overlay tiles are spawned at runtime.
        private OverlayTile FindOverlayTileByGrid(Vector2Int cell)
        {
            // If RpgMapManager exposes a dictionary accessor later, prefer that.
            // For tonight: single pass over existing instances at activation time.
            var all = FindObjectsOfType<OverlayTile>(includeInactive: false);
            for (int i = 0; i < all.Length; i++)
            {
                var t = all[i];
                // Inspector shows "Grid Location X,Y,Z" (Vector3Int). Match on X/Y only.
                // field name 'gridLocation' must exist on OverlayTile (as in your project).
                if (t.gridLocation.x == cell.x && t.gridLocation.y == cell.y)
                    return t;
            }
            return null;
        }
        
        // Optional utility if you ever need to cancel the quest without loading the battle.
        public void DeactivateCurrentQuest()
        {
            if (!questActive && targetTile == null) return;

            // symmetric cleanup for visual lock
            if (targetTile != null)
            {
                targetTile.isTempBlocked = false;
                targetTile.tempBlockedByPlanning = false;
                // targetTile.SetToDefaultSprite(); // optional immediate revert
            }

            // restore sprite if we changed it
            if (currentQuest != null && currentQuest.highlightSprite != null && targetTile != null)
            {
                var sr = targetTile.GetComponent<SpriteRenderer>();
                if (sr != null && originalSprite != null) sr.sprite = originalSprite;
            }

            questActive = false;
            targetTile = null;
            originalSprite = null;
            lastSeenTile = null;
        }
    }
}