using PhoneRepair.Controls;
using PhoneRepair.Interaction;
using UnityEngine;

namespace PhoneRepair.PhoneFixer
{
    /// <summary>
    /// Shop-view input owner: hover a <see cref="ShopStation"/> to make it glow (orange = you can pick it)
    /// and show its label; click to use it. Mirrors WorkbenchController's role for the bench.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShopController : MonoBehaviour
    {
        [SerializeField] private GameInput input;
        [SerializeField] private InteractionRaycaster raycaster;
        [SerializeField] private SelectionController selection;

        public ShopStation HoveredStation { get; private set; }

        private void Update()
        {
            if (input == null || raycaster == null || selection == null)
            {
                return;
            }

            // While a screen (dialogue, inventory, pause...) is open the world waits.
            Interactable target = !UiPanel.AnyOpen && raycaster.TryPick(input.PointerPosition, out PickResult pick) ? pick.Interactable : null;
            HoveredStation = target != null ? target.GetComponent<ShopStation>() : null;
            if (HoveredStation == null)
            {
                target = null;
            }

            selection.SetHint(HoveredStation != null ? new HoverHint(HoveredStation.Verb, InteractionTone.Ready) : HoverHint.None);
            selection.SetHovered(target);

            if (HoveredStation != null && input.PrimaryPressedThisFrame)
            {
                HoveredStation.Use();
            }
        }
    }
}
