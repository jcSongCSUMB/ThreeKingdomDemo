using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Junchen_Scripts.UI
{
    /// <summary>
    /// DialoguePanelController
    /// Controls opening, progressing, and closing dialogue in the RPG scene.
    /// This is the minimal skeleton with fields and empty methods.
    /// </summary>
    public class DialoguePanelController : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject panelRoot; // Root container for the dialogue panel
        public Image portrait; // Speaker portrait image
        public TMP_Text nameText; // Speaker name text
        public TMP_Text bodyText; // Dialogue body text
        public Button clickCatcher; // Button covering panel area for advancing dialogue
        public Sprite defaultPortrait; // Fallback sprite for missing speaker portraits

        [Serializable]
        public class SpeakerSprite
        {
            public string speakerId; // Unique key for speaker
            public Sprite sprite; // Portrait image for speaker
            public string displayName; // Optional display name override
        }

        [Header("Speaker Mapping")]
        public List<SpeakerSprite> speakerSprites; // List of speaker portrait/name mappings

        // Data structure for a single dialogue line
        [Serializable]
        public struct DialogueLine
        {
            public string speakerId;
            public string text;
        }

        // Internal state
        private List<DialogueLine> _lines;
        private int _index;
        private Action _onFinished;
        private bool _isOpen;

        private void Awake()
        {
            if (clickCatcher != null)
            {
                clickCatcher.onClick.AddListener(Next);
            }
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        // Opens the dialogue panel with the provided lines
        public void Open(List<DialogueLine> lines, Action onFinished)
        {
            if (lines == null || lines.Count == 0 || panelRoot == null)
                return;

            _lines = lines;
            _index = 0;
            _onFinished = onFinished;
            _isOpen = true;

            panelRoot.SetActive(true);
            ShowCurrentLine();
        }

        // Shows the current dialogue line based on _index
        private void ShowCurrentLine()
        {
            if (_lines == null || _lines.Count == 0 || _index < 0 || _index >= _lines.Count)
                return;

            var line = _lines[_index];

            // Resolve speaker name and portrait
            string displayName = line.speakerId;
            Sprite portraitSprite = defaultPortrait;
            if (speakerSprites != null)
            {
                for (int i = 0; i < speakerSprites.Count; i++)
                {
                    var s = speakerSprites[i];
                    if (s != null && s.speakerId == line.speakerId)
                    {
                        if (!string.IsNullOrEmpty(s.displayName))
                            displayName = s.displayName;
                        if (s.sprite != null)
                            portraitSprite = s.sprite;
                        break;
                    }
                }
            }

            if (nameText != null) nameText.text = displayName;
            if (portrait != null)
            {
                portrait.sprite = portraitSprite;
                portrait.enabled = (portrait.sprite != null);
            }
            if (bodyText != null) bodyText.text = line.text ?? string.Empty;
        }

        // Advances to the next dialogue line or closes the panel if at the end
        public void Next()
        {
            if (!_isOpen || _lines == null)
                return;

            _index++;
            if (_index >= _lines.Count)
            {
                Close();
            }
            else
            {
                ShowCurrentLine();
            }
        }

        // Closes the dialogue panel
        public void Close()
        {
            _isOpen = false;

            if (panelRoot != null)
                panelRoot.SetActive(false);

            _lines = null;
            _index = 0;

            _onFinished?.Invoke();
            _onFinished = null;
        }
    }
}