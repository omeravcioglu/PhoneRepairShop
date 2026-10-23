using PhoneRepair.Interaction;
using PhoneRepair.Pixel;
using UnityEngine;

namespace PhoneRepair.Repair
{
    /// <summary>One tool on the bench's tool rack: click it (or press its number) to pick it up.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Interactable))]
    public sealed class ToolSlot : MonoBehaviour, IClickable, ICustomHover
    {
        [SerializeField] private ToolBelt toolBelt;
        [SerializeField, Min(0)] private int index;
        [SerializeField] private SpriteRenderer frame;
        [SerializeField] private SpriteRenderer icon;
        [SerializeField] private PixelText keyLabel;
        [SerializeField] private Sprite frameNormal;
        [SerializeField] private Sprite frameSelected;
        [SerializeField] private Color hoverTint = new(1f, 0.86f, 0.62f, 1f);

        private Interactable target;
        private Vector3 iconRest;
        private bool hasRest;

        public int Index => index;

        public HoverHint Hint
        {
            get
            {
                if (toolBelt == null || index >= toolBelt.Tools.Count || toolBelt.Tools[index] == null)
                {
                    return HoverHint.None;
                }

                return new HoverHint($"Pick up the {toolBelt.Tools[index].DisplayName}  [{index + 1}]", InteractionTone.Ready);
            }
        }

        public void Click()
        {
            if (toolBelt != null)
            {
                toolBelt.Equip(index);
            }
        }

        private void Update()
        {
            if (toolBelt == null)
            {
                return;
            }

            target ??= GetComponent<Interactable>();
            bool selected = toolBelt.CurrentIndex == index;
            if (frame != null)
            {
                frame.sprite = selected && frameSelected != null ? frameSelected : frameNormal;
                frame.color = target.IsHovered && !selected ? hoverTint : Color.white;
            }

            if (icon != null)
            {
                if (!hasRest)
                {
                    iconRest = icon.transform.localPosition;
                    hasRest = true;
                }

                icon.transform.localPosition = iconRest + (selected ? Vector3.up : Vector3.zero);
            }
        }
    }
}
