using PhoneRepair.Controls;
using PhoneRepair.Repair;
using UnityEngine;
using UnityEngine.UI;

namespace PhoneRepair.UI
{
    /// <summary>Radial progress ring next to the cursor while a repair action is held.</summary>
    [DisallowMultipleComponent]
    public sealed class ActionProgressView : MonoBehaviour
    {
        [SerializeField] private RepairInteractor repair;
        [SerializeField] private GameInput input;
        [SerializeField] private CanvasGroup canvasGroup;
        [Tooltip("Image set to Filled / Radial 360.")]
        [SerializeField] private Image ring;
        [SerializeField] private Vector2 pointerOffset = new(-34f, 30f);
        [SerializeField, Min(0.1f)] private float fadeSpeed = 10f;

        private RectTransform rectTransform;
        private RectTransform canvasRect;
        private Canvas canvas;

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            canvas = GetComponentInParent<Canvas>();
            canvasRect = canvas != null ? canvas.rootCanvas.transform as RectTransform : null;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
            }
        }

        private void LateUpdate()
        {
            if (repair == null || input == null || canvasGroup == null || ring == null || canvasRect == null)
            {
                return;
            }

            bool visible = repair.ShowsProgress;
            if (visible)
            {
                ring.fillAmount = repair.ActiveProgress;
            }

            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, visible ? 1f : 0f, fadeSpeed * Time.unscaledDeltaTime);
            if (canvasGroup.alpha <= 0f)
            {
                return;
            }

            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, input.PointerPosition + pointerOffset, uiCamera, out Vector2 local))
            {
                rectTransform.anchoredPosition = local;
            }
        }
    }
}
