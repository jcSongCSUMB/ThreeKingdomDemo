using System.Collections.Generic;
using UnityEngine;
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

        // Public entry point for Interact buttons or other triggers
        public void StartDialogue()
        {
            if (dialogueAsset == null)
            {
                Debug.LogWarning("[DialogueService] StartDialogue() called but no DialogueAsset assigned.");
                return;
            }

            if (dialoguePanel != null)
            {
                OpenPanelFromAsset();
            }
            else
            {
                // No panel assigned; fall back to console logging for testing
                PlayAllLinesToConsole();
            }
        }

        // Convert SO lines -> panel lines and open the dialogue panel
        private void OpenPanelFromAsset()
        {
            if (dialogueAsset.lines == null || dialogueAsset.lines.Count == 0)
            {
                Debug.LogWarning("[DialogueService] DialogueAsset has no lines.");
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

            dialoguePanel.Open(lines, () =>
            {
                Debug.Log("[DialogueService] Dialogue finished (from DialogueService).");
            });
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
    }
}