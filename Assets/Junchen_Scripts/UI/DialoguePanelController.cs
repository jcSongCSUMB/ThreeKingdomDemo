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

        // ---- Typewriter settings (added) ----
        [Header("Typewriter")]
        [Tooltip("Enable/disable typewriter effect for body text.")]
        public bool useTypewriter = true; // Exposed toggle in Inspector

        [Tooltip("How many characters to reveal per second when typing.")]
        public float charsPerSecond = 30f; // Exposed speed in Inspector

        // Internal state
        private List<DialogueLine> _lines;
        private int _index;
        private Action _onFinished;
        private bool _isOpen;

        // Typewriter runtime state (added)
        private bool _isTyping = false;
        private Coroutine _typingCoroutine = null;

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

            // Body text rendering
            if (bodyText != null)
            {
                // Set full text first
                bodyText.text = line.text ?? string.Empty;

                // Stop any previous typing
                StopTypingIfAny();

                if (useTypewriter)
                {
                    // Begin typewriter for this line
                    _typingCoroutine = StartCoroutine(TypeCurrentBody());
                }
                else
                {
                    // No typing: show complete line immediately
                    _isTyping = false;
                    bodyText.maxVisibleCharacters = int.MaxValue; // ensure fully visible
                }
            }
        }

        // Advances to the next dialogue line or closes the panel if at the end
        public void Next()
        {
            if (!_isOpen || _lines == null)
                return;

            // If typewriter is in progress, first complete current line
            if (_isTyping && bodyText != null)
            {
                CompleteCurrentBodyInstant();
                return;
            }

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

            // stop any typing to avoid running coroutines after close
            StopTypingIfAny();

            if (panelRoot != null)
                panelRoot.SetActive(false);

            _lines = null;
            _index = 0;

            _onFinished?.Invoke();
            _onFinished = null;
        }

        // ---- Typewriter helpers (added) ----

        // Starts revealing body text gradually for the current line
        private System.Collections.IEnumerator TypeCurrentBody()
        {
            if (bodyText == null)
            {
                _isTyping = false;
                yield break;
            }

            _isTyping = true;

            // Force mesh update to get correct character count
            bodyText.ForceMeshUpdate();
            int total = bodyText.textInfo.characterCount;

            // If there are no characters, just finish
            if (total <= 0)
            {
                _isTyping = false;
                bodyText.maxVisibleCharacters = int.MaxValue;
                yield break;
            }

            bodyText.maxVisibleCharacters = 0;

            // Reveal characters over time
            float visible = 0f;
            while (visible < total)
            {
                visible += charsPerSecond * Time.unscaledDeltaTime; // unscaled keeps speed stable
                int show = Mathf.Clamp(Mathf.FloorToInt(visible), 0, total);
                bodyText.maxVisibleCharacters = show;
                yield return null;
            }

            // Ensure fully visible at the end
            bodyText.maxVisibleCharacters = total;
            _isTyping = false;
            _typingCoroutine = null;
        }

        // Instantly completes current line visibility
        private void CompleteCurrentBodyInstant()
        {
            if (bodyText == null) return;

            bodyText.ForceMeshUpdate();
            int total = bodyText.textInfo.characterCount;
            bodyText.maxVisibleCharacters = (total > 0) ? total : int.MaxValue;

            _isTyping = false;

            if (_typingCoroutine != null)
            {
                StopCoroutine(_typingCoroutine);
                _typingCoroutine = null;
            }
        }

        // Stops any running typewriter coroutine
        private void StopTypingIfAny()
        {
            _isTyping = false;
            if (_typingCoroutine != null)
            {
                StopCoroutine(_typingCoroutine);
                _typingCoroutine = null;
            }
            if (bodyText != null)
            {
                // ensure nothing remains hidden if we stop mid-typing
                bodyText.maxVisibleCharacters = int.MaxValue;
            }
        }
    }
}