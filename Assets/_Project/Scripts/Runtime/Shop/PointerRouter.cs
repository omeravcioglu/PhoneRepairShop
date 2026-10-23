using PhoneRepair.Controls;
using PhoneRepair.Interaction;
using PhoneRepair.Repair;
using UnityEngine;

namespace PhoneRepair.Shop
{
    /// <summary>
    /// Turns the mouse into actions in both views: finds what's under the cursor (outline colour and
    /// prompt), clicks buttons and props, and runs press-and-hold repairs on phone parts at the bench.
    /// Input pauses while the camera slides or the phone flips.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-10)]
    public sealed class PointerRouter : MonoBehaviour
    {
        [SerializeField] private GameInput input;
        [SerializeField] private PointerPicker2D picker;
        [SerializeField] private SelectionController selection;
        [SerializeField] private SpriteHighlighter highlighter;
        [SerializeField] private BenchRepair repair;
        [SerializeField] private StatusLine status;
        [SerializeField] private CameraRig2D rig;

        private bool holding;

        /// <summary>Set by the director for moments with nothing to click (none yet; kept for cutscenes).</summary>
        public bool Locked { get; set; }

        public bool IsHolding => holding;
        public Interactable Hovered => selection != null ? selection.Hovered : null;

        private void Update()
        {
            if (input == null || picker == null)
            {
                return;
            }

            bool paused = Locked || (rig != null && rig.IsSliding) || (repair != null && repair.Phone != null && repair.Phone.IsFlipping);
            if (paused)
            {
                if (holding)
                {
                    repair.Release();
                    holding = false;
                }

                SetHover(null, HoverHint.None);
                return;
            }

            Vector2 pointer = input.PointerPosition;
            if (holding)
            {
                if (!input.PrimaryHeld)
                {
                    repair.Release();
                    holding = false;
                }
                else if (!repair.Hold(pointer, Time.deltaTime))
                {
                    holding = false; // done; the next action needs a new press
                }

                if (holding)
                {
                    if (highlighter != null)
                    {
                        highlighter.SetHover(Hovered, InteractionTone.Ready);
                    }

                    if (status != null)
                    {
                        status.SetHint(repair.DescribeActive());
                    }

                    return;
                }
            }

            Interactable hovered = picker.Pick(pointer);
            SetHover(hovered, Describe(hovered));

            if (input.PrimaryPressedThisFrame && hovered != null)
            {
                IClickable clickable = hovered.GetComponent<IClickable>();
                if (clickable != null)
                {
                    clickable.Click();
                }
                else if (repair != null && repair.Owns(hovered) &&
                         repair.Begin(hovered, pointer) == RepairBeginResult.Started)
                {
                    holding = true;
                }
            }
        }

        private HoverHint Describe(Interactable hovered)
        {
            if (hovered == null)
            {
                return HoverHint.None;
            }

            IClickable clickable = hovered.GetComponent<IClickable>();
            if (clickable != null)
            {
                return clickable.Hint;
            }

            if (repair != null && repair.Owns(hovered))
            {
                return repair.DescribeHover(hovered);
            }

            return new HoverHint(hovered.DisplayName, InteractionTone.Neutral);
        }

        private void SetHover(Interactable target, HoverHint hint)
        {
            if (selection != null)
            {
                selection.SetHovered(target);
            }

            if (highlighter != null)
            {
                highlighter.SetHover(target, hint.Tone);
            }

            if (status != null && rig != null && rig.Current == CameraRig2D.View.Bench)
            {
                status.SetHint(hint);
            }
        }
    }
}
