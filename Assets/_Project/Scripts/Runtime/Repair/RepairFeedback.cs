using System;
using PhoneRepair.Audio;
using PhoneRepair.Interaction;
using UnityEngine;

namespace PhoneRepair.Repair
{
    public enum FeedbackTone
    {
        Info = 0,
        Warning = 1,
    }

    /// <summary>
    /// One place for repair feedback: sounds, flashes, wiggles and player-facing messages.
    /// UI subscribes to <see cref="MessagePosted"/>; gameplay never talks to UI directly.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RepairFeedback : MonoBehaviour
    {
        [SerializeField] private SfxPlayer sfx;
        [SerializeField] private InteractionHighlighter highlighter;
        [SerializeField] private PartMover mover;
        [SerializeField, ColorUsage(false, true)] private Color refuseFlash = new(1.4f, 0.32f, 0.26f);
        [SerializeField, ColorUsage(false, true)] private Color successFlash = new(0.6f, 1.25f, 0.75f);

        public event Action<string, FeedbackTone> MessagePosted;

        /// <summary>"Not yet": wiggle, red flash, buzz, and say why.</summary>
        public void Refuse(Interactable target, string reason)
        {
            if (target != null)
            {
                if (mover != null)
                {
                    mover.Wiggle(target.transform);
                }

                if (highlighter != null)
                {
                    highlighter.Flash(target, refuseFlash, 0.5f);
                }
            }

            Play(SfxId.Nope);
            Post(reason, FeedbackTone.Warning);
        }

        /// <summary>Short green glow on a part that just did what the player wanted.</summary>
        public void Confirm(Interactable target)
        {
            if (target != null && highlighter != null)
            {
                highlighter.Flash(target, successFlash, 0.35f);
            }
        }

        public void Post(string message, FeedbackTone tone)
        {
            if (!string.IsNullOrEmpty(message))
            {
                MessagePosted?.Invoke(message, tone);
            }
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
