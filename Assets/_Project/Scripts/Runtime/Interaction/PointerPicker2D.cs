using System.Collections.Generic;
using UnityEngine;

namespace PhoneRepair.Interaction
{
    /// <summary>
    /// Finds the <see cref="Interactable"/> under a screen point in the 2D world. When colliders overlap
    /// (a screw on the cover, a connector on the battery) the one drawn in front wins, by sorting order.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PointerPicker2D : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private LayerMask layers = Physics2D.DefaultRaycastLayers;

        private readonly List<Collider2D> hits = new();

        public Camera Camera
        {
            get => targetCamera;
            set => targetCamera = value;
        }

        public Vector2 ScreenToWorld(Vector2 screenPoint)
        {
            if (targetCamera == null)
            {
                return screenPoint;
            }

            Vector3 world = targetCamera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, 0f));
            return new Vector2(world.x, world.y);
        }

        public Interactable Pick(Vector2 screenPoint) => PickWorld(ScreenToWorld(screenPoint));

        public Interactable PickWorld(Vector2 worldPoint)
        {
            Physics2D.SyncTransforms(); // things moved this frame (flights, the flip) must be found where they are now

            var filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(layers);
            hits.Clear();
            Physics2D.OverlapPoint(worldPoint, filter, hits);

            Interactable best = null;
            int bestOrder = int.MinValue;
            foreach (Collider2D hit in hits)
            {
                Interactable candidate = hit.GetComponentInParent<Interactable>();
                if (candidate == null || !candidate.IsInteractable)
                {
                    continue;
                }

                int order = OrderOf(hit, candidate);
                if (best == null || order > bestOrder)
                {
                    best = candidate;
                    bestOrder = order;
                }
            }

            return best;
        }

        private static int OrderOf(Collider2D hit, Interactable owner)
        {
            if (hit.TryGetComponent(out Renderer renderer))
            {
                return renderer.sortingOrder;
            }

            int order = int.MinValue;
            foreach (Renderer r in owner.HighlightRenderers)
            {
                if (r != null)
                {
                    order = Mathf.Max(order, r.sortingOrder);
                }
            }

            return order;
        }
    }
}
