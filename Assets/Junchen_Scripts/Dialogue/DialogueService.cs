using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // added for Button
using Junchen_Scripts.UI; // DialoguePanelController

namespace Junchen_Scripts.Dialogue
{
    /// <summary>
    /// Loads dialogue lines from a DialogueAsset and, if a DialoguePanelController is assigned,
    /// opens the panel to display them. Intended to be triggered by external events (e.g., an Interact button).
    /// Falls back to Console logging via PlayAllLinesToConsole() if needed.
    /// </summary>
    public class DialogueService : MonoBehaviour
    {
        [Header("Dialogue Data")]
        public DialogueAsset dialogueAsset; // The DialogueAsset to load from

        [Header("UI (Optional)")]
        public DialoguePanelController dialoguePanel; // If set, StartDialogue() will open the panel with lines

        private int currentLineIndex = 0; // Reserved for future use (e.g., resume)

        // interaction gating during dialogue
        [Header("Interaction Lock (Optional)")]
        [Tooltip("The Interact button under the NPC (set non-interactable while dialogue is active).")]
        [SerializeField] private Button interactButton; // optional

        [Tooltip("Any components to disable while dialogue is active, e.g., RPGClickController.")]
        [SerializeField] private MonoBehaviour[] disableWhileDialogue; // optional

        private bool isDialogueActive = false; // added

        // Public entry point for Interact buttons or other triggers
        public void StartDialogue()
        {
            if (dialogueAsset == null)
            {
                Debug.LogWarning("[DialogueService] StartDialogue() called but no DialogueAsset assigned.");
                return;
            }

            if (isDialogueActive)
            {
                Debug.Log("[DialogueService] Dialogue already active. Ignored Start.");
                return;
            }

            if (dialoguePanel != null)
            {
                Debug.Log("[DialogueService] Start");
                isDialogueActive = true;
                SetInteractionEnabled(false);
                OpenPanelFromAsset();
            }
            else
            {
                // No panel assigned; fall back to console logging for testing
                Debug.Log("[DialogueService] Start (console mode)");
                isDialogueActive = true; // keep consistent, even in console mode
                SetInteractionEnabled(false);
                PlayAllLinesToConsole();
                OnDialogueFinished();
            }
        }

        // Convert SO lines -> panel lines and open the dialogue panel
        private void OpenPanelFromAsset()
        {
            if (dialogueAsset.lines == null || dialogueAsset.lines.Count == 0)
            {
                Debug.LogWarning("[DialogueService] DialogueAsset has no lines.");
                // nothing to show; finish immediately
                OnDialogueFinished();
                return;
            }

            var lines = new List<DialoguePanelController.DialogueLine>(dialogueAsset.lines.Count);
            for (int i = 0; i < dialogueAsset.lines.Count; i++)
            {
                var src = dialogueAsset.lines[i];
                lines.Add(new DialoguePanelController.DialogueLine
                {
                    speakerId = src.speakerId,
                    text = src.text
                });
            }

            // pass a single, centralized finish callback
            dialoguePanel.Open(lines, OnDialogueFinished);
        }

        // Called by DialoguePanelController when dialogue ends
        private void OnDialogueFinished()
        {
            Debug.Log("[DialogueService] End");
            SetInteractionEnabled(true);
            isDialogueActive = false;
        }

        // Old test path: print all lines to the Console
        private void PlayAllLinesToConsole()
        {
            if (dialogueAsset == null || dialogueAsset.lines == null) return;

            foreach (var line in dialogueAsset.lines)
            {
                Debug.Log($"{line.speakerId}: {line.text}");
            }
        }

        // unified interaction toggling
        private void SetInteractionEnabled(bool enabled)
        {
            // Do not hide the button here; only toggle clickability.
            if (interactButton != null)
                interactButton.interactable = enabled;

            if (disableWhileDialogue != null)
            {
                for (int i = 0; i < disableWhileDialogue.Length; i++)
                {
                    var comp = disableWhileDialogue[i];
                    if (comp != null) comp.enabled = enabled;
                }
            }
        }
    }
}