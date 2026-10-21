using PhoneRepair.Interaction;
using TMPro;
using UnityEngine;

namespace PhoneRepair.PhoneFixer
{
    /// <summary>
    /// The navy pill that floats above whatever station is hovered in the shop ("Workbench / Repair").
    /// Lives on a Screen Space canvas; positions itself from the station's world bounds every frame.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StationLabel : MonoBehaviour
    {
        [SerializeField] private SelectionController selection;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private RectTransform pill;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text verbLabel;
        [SerializeField] private Vector2 screenOffset = new(0f, 36f);
        [SerializeField, Min(0.1f)] private float fadeSpeed = 10f;

        private void LateUpdate()
        {
            if (pill == null || group == null)
            {
                return;
            }

            Interactable target = selection != null ? selection.Hovered : null;
            float targetAlpha = 0f;
            if (target != null && worldCamera != null)
            {
                Bounds b = target.GetWorldBounds();
                Vector3 screen = worldCamera.WorldToScreenPoint(b.center + Vector3.up * b.extents.y);
                Canvas canvas = pill.GetComponentInParent<Canvas>();
                var area = (RectTransform)pill.parent;
                Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                if (screen.z > 0f && RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screen, uiCamera, out Vector2 local))
                {
                    pill.anchoredPosition = local + screenOffset;
                    if (nameLabel != null)
                    {
                        nameLabel.text = target.DisplayName;
                    }

                    if (verbLabel != null)
                    {
                        verbLabel.text = selection.Hint.Text;
                    }

                    targetAlpha = 1f;
                }
            }

            group.alpha = Mathf.MoveTowards(group.alpha, targetAlpha, fadeSpeed * Time.unscaledDeltaTime);
        }
    }
}
