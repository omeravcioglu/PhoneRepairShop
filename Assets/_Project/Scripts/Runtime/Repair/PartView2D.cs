using System;
using System.Collections.Generic;
using PhoneRepair.Devices;
using UnityEngine;

namespace PhoneRepair.Repair
{
    /// <summary>
    /// How one phone part looks right now: its normal sprite, the broken variant, the unplugged variant
    /// (connectors) or one of the turning frames (screws). It redraws whenever the part changes, gives
    /// the part a collider shaped like its pixels, and knows where the visible pixels are, so flights
    /// and tray spots centre what the player sees rather than the whole phone-sized canvas.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PartView2D : MonoBehaviour
    {
        [SerializeField] private PhonePart part;
        [SerializeField] private Sprite normal;
        [SerializeField] private Sprite broken;
        [Tooltip("Connectors: the unplugged look.")]
        [SerializeField] private Sprite loose;
        [Tooltip("Screws: frames for one turn (4 = one per quarter turn).")]
        [SerializeField] private Sprite[] turnFrames = Array.Empty<Sprite>();
        [Tooltip("Sorting order while installed: the phone's base order plus this part's layer.")]
        [SerializeField] private int installedOrder;
        [Tooltip("Shape a PolygonCollider2D like the sprite's pixels at start (re-reads new art automatically).")]
        [SerializeField] private bool colliderFromSprite = true;

        private readonly List<Vector2> points = new();
        private SpriteRenderer spriteRenderer;
        private bool hasVisualBounds;
        private Rect visualBounds;

        public PhonePart Part => part;
        public int InstalledOrder => installedOrder;
        public SpriteRenderer Renderer => spriteRenderer != null ? spriteRenderer : spriteRenderer = GetComponent<SpriteRenderer>();

        /// <summary>Rectangle of the visible pixels in local space (1 unit = 1 pixel).</summary>
        public Rect VisualBounds
        {
            get
            {
                if (!hasVisualBounds)
                {
                    visualBounds = MeasureVisualBounds();
                    hasVisualBounds = true;
                }

                return visualBounds;
            }
        }

        public Vector2 VisualCenterWorld => transform.TransformPoint(VisualBounds.center);

        public void Refresh()
        {
            if (part == null || part.Definition == null)
            {
                return;
            }

            Sprite sprite = normal;
            if (part.Condition == PartCondition.Broken && broken != null)
            {
                sprite = broken;
            }
            else if (loose != null && part.Definition.DetachStyle == DetachStyle.ReleaseInPlace && part.State == PartState.Loose)
            {
                sprite = loose;
            }

            if (turnFrames.Length > 0)
            {
                float turnsDone = part.Looseness * part.Definition.ScrewTurns;
                int frame = Mathf.FloorToInt(turnsDone * turnFrames.Length) % turnFrames.Length;
                sprite = turnFrames[Mathf.Clamp(frame, 0, turnFrames.Length - 1)] ?? sprite;
            }

            if (sprite != null)
            {
                Renderer.sprite = sprite;
            }
        }

        /// <summary>Moves this part and the parts riding on it (the battery's connector) to a sorting base.</summary>
        public void SetOrderBase(int baseOrder)
        {
            foreach (PartView2D view in GetComponentsInChildren<PartView2D>(true))
            {
                view.Renderer.sortingOrder = baseOrder + (view.installedOrder - installedOrder);
            }
        }

        public void RestoreInstalledOrder()
        {
            foreach (PartView2D view in GetComponentsInChildren<PartView2D>(true))
            {
                view.Renderer.sortingOrder = view.installedOrder;
            }
        }

        private void Awake()
        {
            if (colliderFromSprite && normal != null && TryGetComponent(out PolygonCollider2D polygon))
            {
                int count = normal.GetPhysicsShapeCount();
                polygon.pathCount = count;
                for (int i = 0; i < count; i++)
                {
                    normal.GetPhysicsShape(i, points);
                    polygon.SetPath(i, points);
                }
            }
        }

        private void OnEnable()
        {
            if (part != null)
            {
                part.Changed += OnPartChanged;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (part != null)
            {
                part.Changed -= OnPartChanged;
            }
        }

        private void OnPartChanged(PhonePart changed) => Refresh();

        private Rect MeasureVisualBounds()
        {
            Sprite sprite = normal != null ? normal : Renderer.sprite;
            if (sprite == null)
            {
                return new Rect(-4f, -4f, 8f, 8f);
            }

            int shapes = sprite.GetPhysicsShapeCount();
            if (shapes == 0 || turnFrames.Length > 0)
            {
                Bounds b = sprite.bounds; // tiny sprites (screws) are all visible anyway
                return new Rect(b.min.x, b.min.y, b.size.x, b.size.y);
            }

            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i < shapes; i++)
            {
                sprite.GetPhysicsShape(i, points);
                foreach (Vector2 p in points)
                {
                    minX = Mathf.Min(minX, p.x);
                    minY = Mathf.Min(minY, p.y);
                    maxX = Mathf.Max(maxX, p.x);
                    maxY = Mathf.Max(maxY, p.y);
                }
            }

            return Rect.MinMaxRect(Mathf.Floor(minX), Mathf.Floor(minY), Mathf.Ceil(maxX), Mathf.Ceil(maxY));
        }
    }
}
