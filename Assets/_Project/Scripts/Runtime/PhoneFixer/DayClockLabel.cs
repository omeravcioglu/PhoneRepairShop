using TMPro;
using UnityEngine;

namespace PhoneRepair.PhoneFixer
{
    /// <summary>
    /// The HUD shop clock: game minutes advance in real time, shown as HH:MM, and a sun marker
    /// travels along the opening-hours bar. Placeholder for the day cycle of Milestone 4.
    /// </summary>
    public sealed class DayClockLabel : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [Tooltip("Optional: marker moved along its parent between opening and closing time.")]
        [SerializeField] private RectTransform sunMarker;
        [Tooltip("Optional: bar whose right edge follows the sun marker (anchor max x).")]
        [SerializeField] private RectTransform dayFill;
        [SerializeField, Range(0f, 24f)] private float startHour = 16.5f;
        [SerializeField, Range(0f, 24f)] private float openHour = 9f;
        [SerializeField, Range(0f, 24f)] private float closeHour = 19f;
        [SerializeField, Min(0f)] private float gameMinutesPerSecond = 1f;

        private float hour;

        public float Hour => hour;

        private void OnEnable()
        {
            hour = startHour;
            Refresh();
        }

        private void Update()
        {
            hour += gameMinutesPerSecond / 60f * Time.deltaTime;
            if (hour >= closeHour)
            {
                hour = openHour;
            }

            Refresh();
        }

        private void Refresh()
        {
            int h = Mathf.FloorToInt(hour);
            int m = Mathf.FloorToInt((hour - h) * 60f);
            if (label != null)
            {
                label.text = $"{h:00}:{m:00}";
            }

            float t = Mathf.InverseLerp(openHour, closeHour, hour);
            if (sunMarker != null)
            {
                sunMarker.anchorMin = sunMarker.anchorMax = new Vector2(t, 0.5f);
                sunMarker.anchoredPosition = Vector2.zero;
            }

            if (dayFill != null)
            {
                dayFill.anchorMax = new Vector2(t, dayFill.anchorMax.y);
            }
        }
    }
}
