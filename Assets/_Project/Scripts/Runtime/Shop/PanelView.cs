using PhoneRepair.Controls;
using PhoneRepair.Interaction;
using PhoneRepair.Pixel;
using UnityEngine;

namespace PhoneRepair.Shop
{
    /// <summary>
    /// A paper panel with text and one button: the title card and the end-of-day summary.
    /// Enter presses the button too. Input is ignored for a moment after opening, so a key or
    /// click mashed through the last line of dialogue can't skip the panel unread.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PanelView : MonoBehaviour
    {
        [SerializeField] private GameInput input;
        [SerializeField] private PixelText body;
        [SerializeField] private PixelButton button;
        [Tooltip("Seconds after opening before Enter or a click counts.")]
        [SerializeField, Min(0f)] private float inputDelay = 0.35f;

        private float readyAt;

        public bool IsOpen => gameObject.activeSelf;
        /// <summary>Open and past the input delay: Enter or a click on the button now confirms.</summary>
        public bool IsReady => IsOpen && Time.unscaledTime >= readyAt;
        public bool Confirmed { get; private set; }
        public string BodyText => body != null ? body.Text : string.Empty;

        public void Open(string text, string buttonLabel)
        {
            Confirmed = false;
            readyAt = Time.unscaledTime + inputDelay;
            gameObject.SetActive(true);
            if (body != null)
            {
                body.Text = text ?? string.Empty;
            }

            if (button != null)
            {
                button.Label = buttonLabel;
            }
        }

        public void Close() => gameObject.SetActive(false);

        private void Awake()
        {
            if (button != null)
            {
                button.Clicked += OnClicked;
            }
        }

        private void OnClicked()
        {
            if (IsReady)
            {
                Confirmed = true;
            }
        }

        private void Update()
        {
            if (IsReady && input != null && input.ConfirmPressedThisFrame && button != null)
            {
                button.Click();
            }
        }
    }
}
