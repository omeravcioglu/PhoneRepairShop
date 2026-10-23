using System;
using PhoneRepair.Pixel;
using UnityEngine;
using UnityEngine.Events;

namespace PhoneRepair.Interaction
{
    /// <summary>
    /// A pixel-art button in the world: a 9-sliced sprite sized around a <see cref="PixelText"/> label,
    /// a box collider for picking, a hover tint and a 1 px press. Clicks arrive through
    /// <see cref="IClickable"/> (the pointer router), or call <see cref="Click"/> for hotkeys.
    /// The transform is the button's centre.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Interactable), typeof(BoxCollider2D))]
    public sealed class PixelButton : MonoBehaviour, IClickable, ICustomHover
    {
        [SerializeField] private SpriteRenderer background;
        [SerializeField] private PixelText label;
        [Tooltip("Status-line prompt while hovered.")]
        [SerializeField] private string hint = "Click";
        [SerializeField] private Vector2Int padding = new(7, 5);
        [SerializeField, Min(0)] private int minWidth = 40;
        [SerializeField] private Color normalTint = Color.white;
        [SerializeField] private Color hoverTint = new(1f, 0.86f, 0.62f, 1f);
        [SerializeField] private Color disabledTint = new(0.7f, 0.7f, 0.7f, 0.8f);
        [SerializeField] private bool interactable = true;
        [SerializeField] private UnityEvent onClick = new();

        private Interactable target;
        private float pressedUntil;
        private Vector3 labelRest;

        public event Action Clicked;

        public string Label
        {
            get => label != null ? label.Text : string.Empty;
            set
            {
                if (label != null)
                {
                    label.Text = value;
                    Resize();
                }
            }
        }

        public bool IsInteractable => interactable;

        public HoverHint Hint => new(hint, interactable ? InteractionTone.Ready : InteractionTone.Neutral);

        public void SetHint(string text) => hint = text ?? string.Empty;

        public void SetInteractable(bool value)
        {
            interactable = value;
            Target.SetInteractable(value);
        }

        public void Click()
        {
            if (!interactable || !isActiveAndEnabled)
            {
                return;
            }

            pressedUntil = Time.unscaledTime + 0.09f;
            Clicked?.Invoke();
            onClick.Invoke();
        }

        /// <summary>Fits the background and collider around the label (even sizes keep the centre on a pixel).</summary>
        public void Resize()
        {
            if (label == null || background == null)
            {
                return;
            }

            label.Alignment = PixelText.Align.Center;
            label.WrapWidth = 0;
            label.Rebuild(); // its own OnEnable may not have run yet when this Awake does
            Vector2Int text = label.Size;
            int width = Mathf.Max(minWidth, text.x + padding.x * 2);
            int height = text.y + padding.y * 2;
            width += width % 2;
            height += height % 2;

            background.drawMode = SpriteDrawMode.Sliced;
            background.size = new Vector2(width, height);
            GetComponent<BoxCollider2D>().size = new Vector2(width, height);
            labelRest = new Vector3(0f, Mathf.Floor(text.y / 2f), 0f);
            label.transform.localPosition = labelRest;
        }

        private Interactable Target => target != null ? target : target = GetComponent<Interactable>();

        private void Awake()
        {
            Resize();
            Target.SetInteractable(interactable);
        }

        private void Update()
        {
            if (background != null)
            {
                background.color = !interactable ? disabledTint : Target.IsHovered ? hoverTint : normalTint;
            }

            if (label != null)
            {
                bool pressed = Time.unscaledTime < pressedUntil;
                label.transform.localPosition = labelRest + (pressed ? Vector3.down : Vector3.zero);
            }
        }
    }
}
