using UnityEngine;
using UnityEngine.InputSystem;

namespace PhoneRepair.Controls
{
    /// <summary>
    /// Turns raw device input into game-level queries ("primary pressed", "rotate axis").
    /// Gameplay code never asks for a specific mouse button, so rebinding, gamepad support or
    /// input recording only ever touch this class.
    /// Actions are created in code to keep the prototype self-contained; move them into an
    /// .inputactions asset once there is a rebinding menu.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class GameInput : MonoBehaviour
    {
        [Tooltip("Scroll values larger than this are raw platform deltas (e.g. 120 per notch) and get normalised.")]
        [SerializeField, Min(1f)] private float rawScrollThreshold = 3f;
        [SerializeField, Min(1f)] private float rawScrollUnitsPerNotch = 120f;

        private InputAction point;
        private InputAction primary;
        private InputAction secondary;
        private InputAction tertiary;
        private InputAction scroll;
        private InputAction rotate;
        private InputAction flip;
        private InputAction resetView;
        private InputAction focus;
        private InputAction cancel;
        private InputAction[] toolSlots;
        private InputAction[] actions;

        private Vector2 lastPointerPosition;
        private bool hasLastPointerPosition;

        /// <summary>Pointer position in screen pixels (origin bottom-left).</summary>
        public Vector2 PointerPosition => point.ReadValue<Vector2>();

        /// <summary>
        /// How far the on-screen cursor moved this frame, in pixels. Derived from positions rather than
        /// the raw device delta, so drags follow the visible cursor (OS acceleration included) and
        /// behave the same for mouse, pen, touch and simulated input.
        /// </summary>
        public Vector2 PointerDelta { get; private set; }

        /// <summary>Main "hands / tool" button (left mouse).</summary>
        public bool PrimaryPressedThisFrame => primary.WasPressedThisFrame();
        public bool PrimaryReleasedThisFrame => primary.WasReleasedThisFrame();
        public bool PrimaryHeld => primary.IsPressed();

        /// <summary>Rotate / cancel button (right mouse).</summary>
        public bool SecondaryPressedThisFrame => secondary.WasPressedThisFrame();
        public bool SecondaryHeld => secondary.IsPressed();

        /// <summary>Pan button (middle mouse).</summary>
        public bool TertiaryPressedThisFrame => tertiary.WasPressedThisFrame();
        public bool TertiaryHeld => tertiary.IsPressed();

        /// <summary>Scroll this frame in wheel notches. Positive = wheel pushed away (zoom in).</summary>
        public float ScrollNotches
        {
            get
            {
                float raw = scroll.ReadValue<float>();
                return Mathf.Abs(raw) > rawScrollThreshold ? raw / rawScrollUnitsPerNotch : raw;
            }
        }

        /// <summary>Keyboard rotation (WASD / arrows), each axis in [-1, 1].</summary>
        public Vector2 RotateAxis => rotate.ReadValue<Vector2>();

        public bool FlipPressedThisFrame => flip.WasPressedThisFrame();
        public bool ResetViewPressedThisFrame => resetView.WasPressedThisFrame();
        public bool FocusPressedThisFrame => focus.WasPressedThisFrame();
        public bool CancelPressedThisFrame => cancel.WasPressedThisFrame();

        /// <summary>Tool hotkey pressed this frame (0 = key 1), or -1 for none.</summary>
        public int ToolSlotPressedThisFrame
        {
            get
            {
                for (int i = 0; i < toolSlots.Length; i++)
                {
                    if (toolSlots[i].WasPressedThisFrame())
                    {
                        return i;
                    }
                }

                return -1;
            }
        }

        private void Awake()
        {
            point = new InputAction("Point", InputActionType.PassThrough, "<Pointer>/position");

            primary = new InputAction("Primary", InputActionType.Button, "<Mouse>/leftButton");
            primary.AddBinding("<Pen>/tip");
            primary.AddBinding("<Touchscreen>/primaryTouch/press");
            secondary = new InputAction("Secondary", InputActionType.Button, "<Mouse>/rightButton");
            tertiary = new InputAction("Tertiary", InputActionType.Button, "<Mouse>/middleButton");
            scroll = new InputAction("Scroll", InputActionType.PassThrough, "<Mouse>/scroll/y");

            rotate = new InputAction("Rotate", InputActionType.Value, expectedControlType: "Vector2");
            rotate.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            rotate.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");

            flip = new InputAction("Flip", InputActionType.Button, "<Keyboard>/space");
            resetView = new InputAction("ResetView", InputActionType.Button, "<Keyboard>/r");
            focus = new InputAction("Focus", InputActionType.Button, "<Keyboard>/f");
            cancel = new InputAction("Cancel", InputActionType.Button, "<Keyboard>/escape");

            toolSlots = new InputAction[6];
            for (int i = 0; i < toolSlots.Length; i++)
            {
                toolSlots[i] = new InputAction($"ToolSlot{i + 1}", InputActionType.Button, $"<Keyboard>/{i + 1}");
                toolSlots[i].AddBinding($"<Keyboard>/numpad{i + 1}");
            }

            var all = new System.Collections.Generic.List<InputAction>
            {
                point, primary, secondary, tertiary, scroll,
                rotate, flip, resetView, focus, cancel
            };
            all.AddRange(toolSlots);
            actions = all.ToArray();
        }

        private void OnEnable()
        {
            foreach (InputAction action in actions)
            {
                action.Enable();
            }

            hasLastPointerPosition = false;
            PointerDelta = Vector2.zero;
        }

        private void Update()
        {
            // Runs before gameplay scripts (execution order -100), so everyone sees this frame's delta.
            Vector2 position = PointerPosition;
            PointerDelta = hasLastPointerPosition ? position - lastPointerPosition : Vector2.zero;
            lastPointerPosition = position;
            hasLastPointerPosition = true;
        }

        private void OnDisable()
        {
            foreach (InputAction action in actions)
            {
                action.Disable();
            }
        }

        private void OnDestroy()
        {
            foreach (InputAction action in actions)
            {
                action.Dispose();
            }
        }
    }
}
