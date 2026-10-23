using System;
using PhoneRepair.Audio;
using PhoneRepair.Interaction;
using UnityEngine;

namespace PhoneRepair.Repair
{
    /// <summary>
    /// One place for "nope" and "nice": wiggle, colour flash, sound and a status-line message.
    /// The customer's face at the top of the bench reacts too (through <see cref="Reacted"/>).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BenchFeedback : MonoBehaviour
    {
        public enum Reaction
        {
            Worried = 0,
            Pleased = 1,
        }

        [SerializeField] private SfxPlayer sfx;
        [SerializeField] private SpriteHighlighter highlighter;
        [SerializeField] private PartFlights2D flights;
        [SerializeField] private StatusLine status;
        [SerializeField] private Color refuseFlash = new(0.87f, 0.4f, 0.36f, 1f);
        [SerializeField] private Color successFlash = new(0.55f, 0.85f, 0.45f, 1f);

        /// <summary>Every message shown to the player (tests and the customer face listen).</summary>
        public event Action<string, InteractionTone> MessagePosted;

        public event Action<Reaction> Reacted;

        public string LastMessage { get; private set; } = string.Empty;

        public void Refuse(Interactable target, string reason)
        {
            if (target != null)
            {
                if (flights != null)
                {
                    flights.Wiggle(target.transform);
                }

                if (highlighter != null)
                {
                    highlighter.Flash(target, refuseFlash, 0.35f);
                }
            }

            Play(SfxId.Nope);
            Post(reason, InteractionTone.Blocked);
            Reacted?.Invoke(Reaction.Worried);
        }

        public void Confirm(Interactable target)
        {
            if (target != null && highlighter != null)
            {
                highlighter.Flash(target, successFlash, 0.3f);
            }

            Reacted?.Invoke(Reaction.Pleased);
        }

        public void Post(string message, InteractionTone tone)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            LastMessage = message;
            if (status != null)
            {
                status.ShowMessage(message, tone);
            }

            MessagePosted?.Invoke(message, tone);
        }

        public void Play(SfxId id, float pitch = 1f, float volume = 1f)
        {
            if (sfx != null)
            {
                sfx.Play(id, pitch, volume);
            }
        }
    }
}
