using PhoneRepair.Controls;
using PhoneRepair.Interaction;
using UnityEngine;

namespace PhoneRepair.Shop
{
    /// <summary>Two answer buttons on the counter ("[1] Leave it with me!" / "[2] Sorry, can't"). Keys 1 and 2 work too.</summary>
    [DisallowMultipleComponent]
    public sealed class ChoicePrompt : MonoBehaviour
    {
        [SerializeField] private GameInput input;
        [SerializeField] private PixelButton first;
        [SerializeField] private PixelButton second;

        public bool IsOpen { get; private set; }

        /// <summary>-1 until answered, then 0 (first) or 1 (second).</summary>
        public int Choice { get; private set; } = -1;

        public void Open(string firstLabel, string secondLabel)
        {
            Choice = -1;
            IsOpen = true;
            first.gameObject.SetActive(true);
            second.gameObject.SetActive(true);
            first.Label = firstLabel;
            second.Label = secondLabel;
        }

        public void Close()
        {
            IsOpen = false;
            first.gameObject.SetActive(false);
            second.gameObject.SetActive(false);
        }

        public void Choose(int index)
        {
            if (IsOpen && Choice < 0)
            {
                Choice = index;
            }
        }

        private void Awake()
        {
            first.Clicked += () => Choose(0);
            second.Clicked += () => Choose(1);
            Close();
        }

        private void Update()
        {
            if (!IsOpen || input == null)
            {
                return;
            }

            int key = input.ToolSlotPressedThisFrame;
            if (key == 0 || key == 1)
            {
                (key == 0 ? first : second).Click();
            }
        }
    }
}
