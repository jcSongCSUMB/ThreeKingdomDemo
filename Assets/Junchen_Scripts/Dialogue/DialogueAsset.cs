using System;
using System.Collections.Generic;
using UnityEngine;

namespace Junchen_Scripts.Dialogue
{
    /// <summary>
    /// ScriptableObject that stores dialogue lines for a single NPC.
    /// </summary>
    [CreateAssetMenu(menuName = "Dialogue/Dialogue Asset", fileName = "NewDialogueAsset")]
    public class DialogueAsset : ScriptableObject
    {
        [Serializable]
        public struct Line
        {
            public string speakerId;            // e.g., "me", "bao_xin"
            [TextArea] public string text;      // dialogue content
        }

        public string npcId;                    // unique id for this NPC (e.g., "bao_xin")
        public List<Line> lines = new List<Line>(); // ordered lines for this NPC
    }
}