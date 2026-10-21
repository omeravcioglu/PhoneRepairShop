using PhoneRepair.Cameras;
using PhoneRepair.Controls;
using PhoneRepair.Interaction;
using PhoneRepair.Repair;
using UnityEngine;

namespace PhoneRepair.Workbench
{
    /// <summary>
    /// The player's hands at the repair bench. Reads <see cref="GameInput"/> and routes it:
    ///   press a part        = select it and use the tool in hand on it (hold to keep working)
    ///   click empty space   = deselect             double-click / F = frame the part
    ///   right drag / WASD   = rotate the phone     left drag on empty space = rotate too
    ///   wheel = zoom to cursor   middle drag = pan   Space = flip   R = reset   1-6 = tools   Esc = deselect
    /// Rotation is locked while a repair action runs, so the part stays put under the tool.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorkbenchController : MonoBehaviour
    {
        private enum Gesture
        {
            None,
            LeftPending,   // pressed, not yet a click or a drag
            LeftRotate,    // left drag that started on empty space
            LeftOnPart,    // left drag that started on a part / finished action: wait for release
            Repairing,     // a repair action is running while the button is held
            RightRotate,
            Pan,
        }

        [Header("Systems")]
        [SerializeField] private GameInput input;
        [SerializeField] private InteractionRaycaster raycaster;
        [SerializeField] private SelectionController selection;

        [Header("Bench")]
        [SerializeField] private InspectionTurntable turntable;
        [SerializeField] private InspectionCameraView inspectionView;
        [SerializeField] private CameraDirector cameraDirector;

        [Header("Repair (optional)")]
        [SerializeField] private RepairInteractor repair;
        [SerializeField] private ToolBelt toolBelt;

        [Header("Feel")]
        [Tooltip("Pointer travel (pixels) that turns a click into a drag.")]
        [SerializeField, Min(0f)] private float dragThreshold = 6f;
        [SerializeField, Min(0.05f)] private float doubleClickTime = 0.3f;
        [Tooltip("Dragging with the left button on empty space also rotates the phone (friendly for trackpads).")]
        [SerializeField] private bool leftDragOnEmptySpaceRotates = true;

        private Gesture gesture;
        private Vector2 pressPosition;
        private float gestureTravel;
        private Interactable pressTarget;
        private Interactable lastClickTarget;
        private float lastClickTime = float.NegativeInfinity;
        private PickResult hoverPick;

        public bool IsDragging => gesture is Gesture.LeftRotate or Gesture.RightRotate or Gesture.Pan;
        public bool IsRepairing => gesture == Gesture.Repairing;

        private void Update()
        {
            if (input == null || raycaster == null || selection == null)
            {
                return;
            }

            if (cameraDirector != null && inspectionView != null && cameraDirector.ActiveView != inspectionView)
            {
                return; // another mode owns the camera (e.g. the future shop view)
            }

            // Colliders moved by the turntable / flights this frame must be visible to raycasts.
            Physics.SyncTransforms();

            Vector2 pointer = input.PointerPosition;
            bool overUI = raycaster.IsOverUI(pointer);
            Transform viewer = cameraDirector != null ? cameraDirector.transform : null;

            HandleKeyboard(viewer);
            if (!overUI)
            {
                HandleZoom(pointer);
            }

            UpdateHover(pointer, overUI);
            HandleGestures(pointer, overUI, viewer);
        }

        private void HandleKeyboard(Transform viewer)
        {
            int toolSlot = input.ToolSlotPressedThisFrame;
            if (toolSlot >= 0 && toolBelt != null && toolSlot < toolBelt.Tools.Count)
            {
                if (gesture == Gesture.Repairing)
                {
                    repair.Cancel();
                    gesture = Gesture.LeftOnPart;
                }

                toolBelt.Equip(toolSlot);
            }

            bool locked = gesture == Gesture.Repairing;
            if (turntable != null && !locked)
            {
                Vector2 axis = input.RotateAxis;
                if (axis.sqrMagnitude > 0.0001f)
                {
                    turntable.RotateByAxis(axis, Time.unscaledDeltaTime, viewer);
                }

                if (input.FlipPressedThisFrame)
                {
                    turntable.Flip(viewer);
                }
            }

            if (input.ResetViewPressedThisFrame && !locked)
            {
                if (turntable != null)
                {
                    turntable.ResetOrientation();
                }

                if (inspectionView != null)
                {
                    inspectionView.ResetView();
                }
            }

            if (input.FocusPressedThisFrame)
            {
                if (selection.Selected != null)
                {
                    FocusOn(selection.Selected);
                }
                else if (inspectionView != null)
                {
                    inspectionView.ResetView();
                }
            }

            if (input.CancelPressedThisFrame)
            {
                selection.ClearSelection();
            }
        }

        private void HandleZoom(Vector2 pointer)
        {
            float notches = input.ScrollNotches;
            if (inspectionView != null && Mathf.Abs(notches) > 0.001f)
            {
                inspectionView.Zoom(notches, pointer);
            }
        }

        private void UpdateHover(Vector2 pointer, bool overUI)
        {
            if (gesture != Gesture.Repairing)
            {
                Interactable hovered = null;
                hoverPick = default;
                if (!overUI && !IsDragging && raycaster.TryPick(pointer, out PickResult pick))
                {
                    hovered = pick.Interactable;
                    hoverPick = pick;
                }

                selection.SetHovered(hovered);
            }

            selection.SetHint(repair != null ? repair.DescribeHover(selection.Hovered) : HoverHint.None);
        }

        private void HandleGestures(Vector2 pointer, bool overUI, Transform viewer)
        {
            if (gesture == Gesture.None)
            {
                if (overUI)
                {
                    return;
                }

                if (input.PrimaryPressedThisFrame)
                {
                    Begin(Gesture.LeftPending, pointer);
                    if (pressTarget != null)
                    {
                        selection.Select(pressTarget);
                        if (repair != null && repair.Begin(pressTarget, hoverPick.Hit) == RepairBeginResult.Started)
                        {
                            gesture = Gesture.Repairing;
                        }
                    }
                }
                else if (input.SecondaryPressedThisFrame)
                {
                    Begin(Gesture.RightRotate, pointer);
                }
                else if (input.TertiaryPressedThisFrame)
                {
                    Begin(Gesture.Pan, pointer);
                }

                return; // this frame's delta happened before the press
            }

            Vector2 delta = input.PointerDelta;
            switch (gesture)
            {
                case Gesture.Repairing:
                    if (input.PrimaryHeld)
                    {
                        if (!repair.Hold(pointer, Time.deltaTime))
                        {
                            gesture = Gesture.LeftOnPart; // done; wait for the button to come up
                        }
                    }
                    else
                    {
                        repair.Release();
                        End();
                    }

                    break;

                case Gesture.LeftPending:
                    if (!input.PrimaryHeld)
                    {
                        Interactable clicked = pressTarget; // End() clears it
                        End();
                        HandleClick(clicked);
                    }
                    else if ((pointer - pressPosition).sqrMagnitude > dragThreshold * dragThreshold)
                    {
                        if (pressTarget == null && leftDragOnEmptySpaceRotates && turntable != null)
                        {
                            gesture = Gesture.LeftRotate;
                            turntable.RotateByScreenDelta(pointer - pressPosition, viewer);
                        }
                        else
                        {
                            gesture = Gesture.LeftOnPart;
                        }
                    }

                    break;

                case Gesture.LeftRotate:
                    if (input.PrimaryHeld)
                    {
                        turntable.RotateByScreenDelta(delta, viewer);
                    }
                    else
                    {
                        End();
                        turntable.SettleIfNearlyFlat();
                    }

                    break;

                case Gesture.LeftOnPart:
                    if (!input.PrimaryHeld)
                    {
                        End();
                    }

                    break;

                case Gesture.RightRotate:
                    if (input.SecondaryHeld)
                    {
                        gestureTravel += delta.magnitude;
                        if (turntable != null)
                        {
                            turntable.RotateByScreenDelta(delta, viewer);
                        }
                    }
                    else
                    {
                        bool wasClick = gestureTravel <= dragThreshold;
                        End();
                        if (wasClick)
                        {
                            selection.ClearSelection();
                        }
                        else if (turntable != null)
                        {
                            turntable.SettleIfNearlyFlat();
                        }
                    }

                    break;

                case Gesture.Pan:
                    if (input.TertiaryHeld)
                    {
                        if (inspectionView != null)
                        {
                            inspectionView.Pan(delta);
                        }
                    }
                    else
                    {
                        End();
                    }

                    break;
            }
        }

        private void Begin(Gesture newGesture, Vector2 pointer)
        {
            gesture = newGesture;
            pressPosition = pointer;
            gestureTravel = 0f;
            pressTarget = selection.Hovered;
        }

        private void End()
        {
            gesture = Gesture.None;
            pressTarget = null;
        }

        private void HandleClick(Interactable target)
        {
            if (target == null || !target.IsInteractable)
            {
                selection.ClearSelection();
                lastClickTarget = null;
                return;
            }

            float now = Time.unscaledTime;
            bool isDoubleClick = target == lastClickTarget && now - lastClickTime <= doubleClickTime;
            selection.Select(target);
            if (isDoubleClick)
            {
                FocusOn(target);
                lastClickTarget = null;
            }
            else
            {
                lastClickTarget = target;
                lastClickTime = now;
            }
        }

        private void FocusOn(Interactable target)
        {
            if (inspectionView != null)
            {
                inspectionView.FocusOn(target.GetWorldBounds(), target.transform);
            }
        }
    }
}
