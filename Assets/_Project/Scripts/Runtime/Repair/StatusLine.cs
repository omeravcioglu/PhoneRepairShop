using PhoneRepair.Interaction;
using PhoneRepair.Pixel;
using UnityEngine;

namespace PhoneRepair.Repair
{
    /// <summary>
    /// The one-line prompt at the bottom of the bench: what pressing would do ("Hold · Unscrew"), or for
    /// a couple of seconds the last message ("First: unscrew P2 Mochi Screw (×4)").
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StatusLine : MonoBehaviour
    {
        [SerializeField] private PixelText label;
        [SerializeField] private Color readyColor = new(1f, 0.85f, 0.6f, 1f);
        [SerializeField] private Color neutralColor = new(0.96f, 0.91f, 0.83f, 1f);
        [SerializeField] private Color blockedColor = new(1f, 0.62f, 0.56f, 1f);
        [SerializeField, Min(0.2f)] private float messageSeconds = 2.4f;

        private float messageUntil = float.NegativeInfinity;

        public string Text => label != null ? label.Text : string.Empty;
        public bool IsShowingMessage => Time.unscaledTime < messageUntil;

        public void SetHint(HoverHint hint)
        {
            if (!IsShowingMessage)
            {
                Show(hint.Text, hint.Tone);
            }
        }

        public void ShowMessage(string message, InteractionTone tone)
        {
            messageUntil = Time.unscaledTime + messageSeconds;
            Show(message, tone);
        }

        public void ClearMessage() => messageUntil = float.NegativeInfinity;

        private void Show(string text, InteractionTone tone)
        {
            if (label == null)
            {
                return;
            }

            label.Text = text ?? string.Empty;
            label.Color = tone switch
            {
                InteractionTone.Ready => readyColor,
                InteractionTone.Blocked => blockedColor,
                _ => neutralColor,
            };
        }
    }
}
