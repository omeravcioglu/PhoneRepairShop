using System;
using TMPro;
using UnityEngine;

namespace PhoneRepair.PhoneFixer
{
    /// <summary>
    /// The speech bubble of the counter dialogue: shows one of its lines with a typewriter reveal.
    /// Placeholder for the dialogue system (Milestone 4): lines are authored on the component and
    /// picked by the answer buttons.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DialogueBox : MonoBehaviour
    {
        [SerializeField] private TMP_Text speech;
        [SerializeField, TextArea(2, 5)] private string[] lines = Array.Empty<string>();
        [SerializeField, Min(1f)] private float charactersPerSecond = 55f;

        private float revealed;
        private int total;

        public int CurrentLine { get; private set; } = -1;

        public void Say(int index)
        {
            if (speech == null || lines.Length == 0)
            {
                return;
            }

            CurrentLine = Mathf.Clamp(index, 0, lines.Length - 1);
            speech.text = lines[CurrentLine];
            speech.ForceMeshUpdate();
            total = speech.textInfo.characterCount;
            revealed = 0f;
            speech.maxVisibleCharacters = 0;
        }

        private void Update()
        {
            if (speech == null || revealed >= total)
            {
                return;
            }

            revealed += charactersPerSecond * Time.unscaledDeltaTime;
            speech.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(revealed));
        }
    }
}
