using System.Collections;
using PhoneRepair.Pixel;
using UnityEngine;

namespace PhoneRepair.Shop
{
    /// <summary>
    /// The camera has two places to be: the shop (looking at the counter and the customer) and the
    /// bench (looking down at the repair mat, just below). Switching is one short slide, the only
    /// camera move in the game.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CameraRig2D : MonoBehaviour
    {
        public enum View
        {
            Shop = 0,
            Bench = 1,
        }

        [SerializeField] private Vector2 shopCentre = new(320f, 180f);
        [SerializeField] private Vector2 benchCentre = new(320f, -180f);
        [SerializeField, Min(0.05f)] private float slideSeconds = 0.45f;

        public View Current { get; private set; } = View.Shop;
        public bool IsSliding { get; private set; }

        public Vector2 CentreOf(View view) => view == View.Bench ? benchCentre : shopCentre;

        public void SnapTo(View view)
        {
            Current = view;
            IsSliding = false;
            Vector2 c = CentreOf(view);
            transform.position = new Vector3(c.x, c.y, transform.position.z);
        }

        public IEnumerator SlideTo(View view)
        {
            Vector3 from = transform.position;
            Vector2 c = CentreOf(view);
            Vector3 to = new(c.x, c.y, from.z);
            IsSliding = true;
            for (float t = 0f; t < slideSeconds; t += Time.deltaTime)
            {
                transform.position = PixelGrid.Snap(Vector3.Lerp(from, to, Ease.InOutCubic(t / slideSeconds)));
                yield return null;
            }

            transform.position = to;
            Current = view;
            IsSliding = false;
        }
    }
}
