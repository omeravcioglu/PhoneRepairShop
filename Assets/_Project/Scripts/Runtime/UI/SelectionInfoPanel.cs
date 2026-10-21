using PhoneRepair.Interaction;
using TMPro;
using UnityEngine;

namespace PhoneRepair.UI
{
    /// <summary>
    /// Card describing the selected Interactable: name, category, description and live status.
    /// Fades and pops in on change and refreshes when the selected thing reports new info
    /// (e.g. a screw comes loose). Works for any selectable thing, not only phone parts.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SelectionInfoPanel : MonoBehaviour
    {
        [SerializeField] private SelectionController selection;
        [SerializeField] private CanvasGroup canvasGroup;
        [Tooltip("Scaled for the pop animation. Usually the card itself.")]
        [SerializeField] private RectTransform card;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private TMP_Text statusText;

        [Header("Animation")]
        [SerializeField, Min(0.1f)] private float fadeSpeed = 8f;
        [SerializeField, Range(0f, 0.2f)] private float popScale = 0.05f;
        [SerializeField, Min(0.1f)] private float popSpeed = 7f;

        private Interactable shown;
        private float targetAlpha;
        private float pop;

        private void OnEnable()
        {
            if (selection != null)
            {
                selection.SelectionChanged += Show;
                Show(selection.Selected);
            }
            else
            {
                Show(null);
            }

            pop = 0f;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = targetAlpha;
            }
        }

        private void OnDisable()
        {
            if (selection != null)
            {
                selection.SelectionChanged -= Show;
            }

            Watch(null);
        }

        private void Update()
        {
            if (canvasGroup == null)
            {
                return;
            }

            float deltaTime = Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * deltaTime);
            canvasGroup.blocksRaycasts = targetAlpha > 0.5f;

            pop = Mathf.MoveTowards(pop, 0f, popSpeed * deltaTime);
            if (card != null)
            {
                card.localScale = Vector3.one * (1f + popScale * pop * pop);
            }
        }

        private void Show(Interactable target)
        {
            Watch(target);
            if (target == null)
            {
                targetAlpha = 0f;
                return;
            }

            Refresh(target);
            targetAlpha = 1f;
            pop = 1f;
        }

        private void Refresh(Interactable target)
        {
            InspectionInfo info = target.GetInfo();
            SetText(titleText, info.Title);
            SetText(subtitleText, info.Subtitle);
            SetText(bodyText, info.Body);
            SetText(statusText, info.Status);
        }

        private void Watch(Interactable target)
        {
            if (shown != null)
            {
                shown.InfoChanged -= Refresh;
            }

            shown = target;
            if (shown != null)
            {
                shown.InfoChanged += Refresh;
            }
        }

        private static void SetText(TMP_Text label, string value)
        {
            if (label == null)
            {
                return;
            }

            label.text = value;
            label.gameObject.SetActive(!string.IsNullOrEmpty(value));
        }
    }
}
