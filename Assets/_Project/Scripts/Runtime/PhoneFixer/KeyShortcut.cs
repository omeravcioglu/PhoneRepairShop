using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PhoneRepair.PhoneFixer
{
    /// <summary>
    /// Presses a UI button from the keyboard (the badge printed on the keycap): answers 1-3 in the
    /// dialogue, Enter on the receipt. Active while its panel is open, or, without a panel, while no
    /// panel is open. Ignores the key press that opened the panel.
    /// </summary>
    public sealed class KeyShortcut : MonoBehaviour
    {
        [SerializeField] private Key key = Key.Enter;
        [SerializeField] private Button button;
        [Tooltip("Only while this panel is open. Empty = only while no panel is open.")]
        [SerializeField] private UiPanel whenOpen;

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || button == null || key == Key.None || !keyboard[key].wasPressedThisFrame)
            {
                return;
            }

            bool inContext = whenOpen != null
                ? whenOpen.IsOpen && whenOpen.OpenedFrame != Time.frameCount
                : !UiPanel.AnyOpen && UiPanel.LastClosedFrame != Time.frameCount;
            if (inContext && button.IsActive() && button.IsInteractable())
            {
                button.onClick.Invoke();
            }
        }
    }
}
