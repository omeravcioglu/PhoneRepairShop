using PhoneRepair.Controls;
using PhoneRepair.Interaction;
using TMPro;
using UnityEngine;

namespace PhoneRepair.UI
{
    /// <summary>
    /// Label that follows the pointer while something is hovered: its name, plus the action prompt
    /// from the hover hint ("Hold · Unscrew", "Needs: Screwdriver [2]", "First: unplug ..."),
    /// coloured by how it would go. Lives on the tooltip RectTransform (pivot top-left).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HoverTooltip : MonoBehaviour
    {
        [SerializeField] private SelectionController selection;
        [SerializeField] private GameInput input;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text label;
        [SerializeField] private TMP_Text hintLabel;
        [SerializeField] private Vector2 pointerOffset = new(20f, -20f);
        [Tooltip("Hide while hovering the selected object, unless there is an action prompt to show.")]
        [SerializeField] private bool hideForSelectedWithoutHint = true;
        [SerializeField, Min(0.1f)] private float fadeSpeed = 12f;

        [Header("Prompt colours")]
        [SerializeField] private Color readyColor = new(0.62f, 0.92f, 0.8f);
        [SerializeField] private Color neutralColor = new(0.82f, 0.79f, 0.88f);
        [SerializeField] private Color blockedColor = new(1f, 0.64f, 0.58f);

        private RectTransform rectTransform;
        private RectTransform canvasRect;
        private Canvas canvas;
        private Interactable shownTarget;
        private string shownHint;
        private InteractionTone shownTone;

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            canvas = GetComponentInParent<Canvas>();
            canvasRect = canvas != null ? canvas.rootCanvas.transform as RectTransform : null;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false; // must never block the thing it describes
            }
        }

        private void LateUpdate()
        {
            if (selection == null || input == null || canvasGroup == null || canvasRect == null)
            {
                return;
            }

            Interactable hovered = selection.Hovered;
            HoverHint hint = selection.Hint;
            bool hasHint = !string.IsNullOrEmpty(hint.Text);
            bool visible = hovered != null && (hasHint || !(hideForSelectedWithoutHint && hovered == selection.Selected));

            if (visible)
            {
                if (hovered != shownTarget && label != null)
                {
                    label.text = hovered.GetInfo().Title;
                }

                if (hintLabel != null && (!ReferenceEquals(hint.Text, shownHint) || hint.Tone != shownTone))
                {
                    hintLabel.text = hint.Text;
                    hintLabel.color = hint.Tone switch
                    {
                        InteractionTone.Ready => readyColor,
                        InteractionTone.Blocked => blockedColor,
                        _ => neutralColor,
                    };
                    hintLabel.gameObject.SetActive(hasHint);
                    shownHint = hint.Text;
                    shownTone = hint.Tone;
                }
            }

            shownTarget = visible ? hovered : null;
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, visible ? 1f : 0f, fadeSpeed * Time.unscaledDeltaTime);
            if (canvasGroup.alpha <= 0f)
            {
                return;
            }

            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, input.PointerPosition + pointerOffset, uiCamera, out Vector2 local))
            {
                Rect bounds = canvasRect.rect;
                Vector2 size = rectTransform.rect.size;
                local.x = Mathf.Clamp(local.x, bounds.xMin, bounds.xMax - size.x);
                local.y = Mathf.Clamp(local.y, bounds.yMin + size.y, bounds.yMax);
                rectTransform.anchoredPosition = local;
            }
        }
    }
}
