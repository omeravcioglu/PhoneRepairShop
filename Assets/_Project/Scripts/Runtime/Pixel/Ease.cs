using UnityEngine;

namespace PhoneRepair.Pixel
{
    /// <summary>Easing curves for tweens. All take and return 0..1.</summary>
    public static class Ease
    {
        public static float InOutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
        }

        public static float OutCubic(float t)
        {
            t = 1f - Mathf.Clamp01(t);
            return 1f - t * t * t;
        }

        /// <summary>0 → 1 → 0: the height of a hop or an arcing flight.</summary>
        public static float Arc(float t) => Mathf.Sin(Mathf.PI * Mathf.Clamp01(t));
    }
}
