using System;
using UnityEngine;

namespace PhoneRepair.Interaction
{
    /// <summary>
    /// Single source of truth for what is hovered and what is selected. Input handlers decide WHEN
    /// it changes; highlight, UI and audio just listen to the events.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SelectionController : MonoBehaviour
    {
        public Interactable Hovered { get; private set; }
        public Interactable Selected { get; private set; }

        /// <summary>What pressing on the hovered thing would do. Set by the active mode controller each frame.</summary>
        public HoverHint Hint { get; private set; } = HoverHint.None;

        public void SetHint(HoverHint hint) => Hint = hint;

        /// <summary>Raised with the new hovered Interactable (null when nothing is hovered).</summary>
        public event Action<Interactable> HoveredChanged;

        /// <summary>Raised with the new selection (null when cleared).</summary>
        public event Action<Interactable> SelectionChanged;

        public void SetHovered(Interactable target)
        {
            if (target == null || !target.IsInteractable)
            {
                target = null; // also turns destroyed objects into a real null
            }

            // ReferenceEquals: a destroyed Hovered compares "== null" in Unity and would never be cleared.
            if (ReferenceEquals(target, Hovered))
            {
                return;
            }

            Interactable previous = Hovered;
            Hovered = target;
            if (previous != null)
            {
                previous.SetHovered(false);
            }

            if (target != null)
            {
                target.SetHovered(true);
            }

            HoveredChanged?.Invoke(target);
        }

        public void Select(Interactable target)
        {
            if (target == null || !target.IsInteractable)
            {
                target = null;
            }

            if (ReferenceEquals(target, Selected))
            {
                return;
            }

            Interactable previous = Selected;
            Selected = target;
            if (previous != null)
            {
                previous.SetSelected(false);
            }

            if (target != null)
            {
                target.SetSelected(true);
            }

            SelectionChanged?.Invoke(target);
        }

        public void ClearSelection() => Select(null);

        private void LateUpdate()
        {
            // Drop references to things that were destroyed or switched off this frame.
            if (IsStale(Hovered, requireInteractable: true))
            {
                SetHovered(null);
            }

            if (IsStale(Selected, requireInteractable: false))
            {
                Select(null);
            }
        }

        private static bool IsStale(Interactable target, bool requireInteractable)
        {
            if (ReferenceEquals(target, null))
            {
                return false;
            }

            // Unity's == reports destroyed objects as null.
            return target == null || !target.isActiveAndEnabled || (requireInteractable && !target.IsInteractable);
        }
    }
}
