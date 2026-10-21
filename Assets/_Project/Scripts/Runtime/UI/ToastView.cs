using PhoneRepair.Repair;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhoneRepair.UI
{
    /// <summary>Short message pill at the top of the screen ("First: unscrew P2 Mochi Screw (×4)").</summary>
    [DisallowMultipleComponent]
    public sealed class ToastView : MonoBehaviour
    {
        [SerializeField] private RepairFeedback feedback;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform pill;
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Color infoColor = new(0.17f, 0.14f, 0.2f, 0.9f);
        [SerializeField] private Color warningColor = new(0.72f, 0.28f, 0.3f, 0.95f);
        [SerializeField, Min(0.2f)] private float visibleSeconds = 2.4f;
        [SerializeField, Min(0.1f)] private float fadeSpeed = 6f;

        private float hideAt = float.NegativeInfinity;
        private float pop;

        /// <summary>Last message shown (handy for tests and debugging).</summary>
        public string CurrentMessage { get; private set; } = string.Empty;

        private void OnEnable()
        {
            if (feedback != null)
            {
                feedback.MessagePosted += Show;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
            }
        }

        private void OnDisable()
        {
            if (feedback != null)
            {
                feedback.MessagePosted -= Show;
            }
        }

        public void Show(string message, FeedbackTone tone)
        {
            CurrentMessage = message ?? string.Empty;
            if (label != null)
            {
                label.text = CurrentMessage;
            }

            if (background != null)
            {
                background.color = tone == FeedbackTone.Warning ? warningColor : infoColor;
            }

            hideAt = Time.unscaledTime + visibleSeconds;
            pop = 1f;
        }

        private void Update()
        {
            if (canvasGroup == null)
            {
                return;
            }

            float deltaTime = Time.unscaledDeltaTime;
            bool visible = Time.unscaledTime < hideAt;
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, visible ? 1f : 0f, fadeSpeed * deltaTime);
            pop = Mathf.MoveTowards(pop, 0f, 6f * deltaTime);
            if (pill != null)
            {
                pill.localScale = Vector3.one * (1f + 0.08f * pop * pop);
            }
        }
    }
}
