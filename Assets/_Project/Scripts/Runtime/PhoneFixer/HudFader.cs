using UnityEngine;

namespace PhoneRepair.PhoneFixer
{
    /// <summary>
    /// Fades the always-on HUD out while a screen (dialogue, drawers, catalog, pause, receipt) is open,
    /// so modals read cleanly, as in the canvas designs. Sits on the HUD canvas.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    [DisallowMultipleComponent]
    public sealed class HudFader : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float fadeSpeed = 8f;

        private CanvasGroup group;

        private void Awake() => group = GetComponent<CanvasGroup>();

        private void Update()
        {
            bool visible = !UiPanel.AnyOpen;
            group.alpha = Mathf.MoveTowards(group.alpha, visible ? 1f : 0f, fadeSpeed * Time.unscaledDeltaTime);
            group.blocksRaycasts = visible;
            group.interactable = visible;
        }
    }
}
