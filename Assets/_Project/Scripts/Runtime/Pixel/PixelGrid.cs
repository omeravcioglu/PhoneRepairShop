using UnityEngine;

namespace PhoneRepair.Pixel
{
    /// <summary>
    /// The game's pixel space. 1 world unit = 1 art pixel, the screen is 640×360, and the shop view
    /// starts at the world origin (bottom-left). The bench view sits directly below it (y -360..0).
    /// Anything that moves is snapped to whole pixels so the art never shimmers.
    /// </summary>
    public static class PixelGrid
    {
        public const int ScreenWidth = 640;
        public const int ScreenHeight = 360;
        public const int PixelsPerUnit = 1;

        /// <summary>Rounds half up (Mathf.Round rounds .5 to even, which makes slow motion jitter).</summary>
        public static float Snap(float value) => Mathf.Floor(value + 0.5f);

        public static Vector3 Snap(Vector3 position) => new(Snap(position.x), Snap(position.y), position.z);

        public static Vector2 Snap(Vector2 position) => new(Snap(position.x), Snap(position.y));
    }
}
