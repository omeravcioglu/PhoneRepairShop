using UnityEngine;
using UnityEngine.EventSystems;

namespace PhoneRepair.PhoneFixer
{
    /// <summary>
    /// Keycap feel for Paper &amp; Keys buttons: the cap lifts a little on hover and sinks onto its
    /// navy drop when pressed. Moves the cap's content, not the button, so layouts stay put.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KeycapPress : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private float hoverLift = 2f;
        [SerializeField] private float pressDepth = 5f;
        [SerializeField, Min(1f)] private float speed = 40f;

        private Vector2 rest;
        private bool hovered;
        private bool pressed;
        private float offset;

        private void Awake()
        {
            if (content != null)
            {
                rest = content.anchoredPosition;
            }
        }

        private void OnDisable()
        {
            hovered = pressed = false;
            offset = 0f;
            if (content != null)
            {
                content.anchoredPosition = rest;
            }
        }

        public void OnPointerEnter(PointerEventData eventData) => hovered = true;

        public void OnPointerExit(PointerEventData eventData) => hovered = pressed = false;

        public void OnPointerDown(PointerEventData eventData) => pressed = true;

        public void OnPointerUp(PointerEventData eventData) => pressed = false;

        private void Update()
        {
            if (content == null)
            {
                return;
            }

            float target = pressed ? -pressDepth : hovered ? hoverLift : 0f;
            offset = Mathf.MoveTowards(offset, target, speed * Time.unscaledDeltaTime);
            content.anchoredPosition = rest + new Vector2(0f, offset);
        }
    }
}
