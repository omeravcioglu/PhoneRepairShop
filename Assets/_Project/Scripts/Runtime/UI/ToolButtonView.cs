using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhoneRepair.UI
{
    /// <summary>One tool slot button: hotkey number + name.</summary>
    [DisallowMultipleComponent]
    public sealed class ToolButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text hotkeyLabel;
        [SerializeField] private TMP_Text nameLabel;

        public Button Button => button;

        public void Bind(int hotkey, string displayName)
        {
            if (hotkeyLabel != null)
            {
                hotkeyLabel.text = hotkey.ToString();
            }

            if (nameLabel != null)
            {
                nameLabel.text = displayName;
            }
        }

        public void SetLook(Color backgroundColor, Color textColor, float scale)
        {
            if (background != null)
            {
                background.color = backgroundColor;
            }

            if (hotkeyLabel != null)
            {
                hotkeyLabel.color = textColor;
            }

            if (nameLabel != null)
            {
                nameLabel.color = textColor;
            }

            transform.localScale = Vector3.one * scale;
        }
    }
}
