using System;
using System.Collections.Generic;
using PhoneRepair.Devices;
using PhoneRepair.Pixel;
using UnityEngine;

namespace PhoneRepair.Repair
{
    /// <summary>
    /// Where removed parts wait on the repair mat: the screw tray's compartments for fasteners and
    /// marked spots for everything else. Picks the smallest free spot the part's visible pixels fit in
    /// and centres them on the spot, on whole pixels.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PartsTray2D : MonoBehaviour
    {
        [Serializable]
        public sealed class Spot
        {
            [Tooltip("Centre of the spot.")]
            public Transform anchor;
            [Tooltip("Room in pixels (width, height).")]
            public Vector2Int size = new(64, 64);
            public bool fastenersOnly;
            [Tooltip("Sorting order of a part resting here (later spots draw on top).")]
            public int sortingOrder = SortOrder.PartsOnMat;
            [NonSerialized] public PhonePart occupant;

            public Transform Anchor => anchor;
            public bool IsFree => occupant == null;
        }

        [SerializeField] private List<Spot> spots = new();

        public IReadOnlyList<Spot> Spots => spots;

        public void AddSpot(Transform anchor, Vector2Int size, bool fastenersOnly, int sortingOrder) =>
            spots.Add(new Spot { anchor = anchor, size = size, fastenersOnly = fastenersOnly, sortingOrder = sortingOrder });

        public bool Holds(PhonePart part) => IndexOf(part) >= 0;

        public bool HasRoomFor(PhonePart part) => Holds(part) || TryPlan(part, out _, out _);

        /// <summary>Parts currently resting on the mat or in the tray.</summary>
        public IEnumerable<PhonePart> Occupants
        {
            get
            {
                foreach (Spot spot in spots)
                {
                    if (spot.occupant != null)
                    {
                        yield return spot.occupant;
                    }
                }
            }
        }

        public Spot SpotOf(PhonePart part)
        {
            int index = IndexOf(part);
            return index >= 0 ? spots[index] : null;
        }

        /// <summary>Claims the best free spot and returns the part's resting position in the spot's space.</summary>
        public bool TryReserve(PhonePart part, out Spot spot, out Vector3 localPosition)
        {
            Release(part);
            if (!TryPlan(part, out spot, out localPosition))
            {
                return false;
            }

            spot.occupant = part;
            return true;
        }

        public void Release(PhonePart part)
        {
            int index = IndexOf(part);
            if (index >= 0)
            {
                spots[index].occupant = null;
            }
        }

        public void Clear()
        {
            foreach (Spot spot in spots)
            {
                spot.occupant = null;
            }
        }

        /// <summary>Where a part rests on a spot: its visible pixels centred on the anchor.</summary>
        public static Vector3 RestingPosition(PhonePart part)
        {
            Rect visual = VisualBoundsOf(part);
            return PixelGrid.Snap(new Vector3(-visual.center.x, -visual.center.y, 0f));
        }

        private int IndexOf(PhonePart part)
        {
            if (part == null)
            {
                return -1;
            }

            for (int i = 0; i < spots.Count; i++)
            {
                if (spots[i].occupant == part)
                {
                    return i;
                }
            }

            return -1;
        }

        private bool TryPlan(PhonePart part, out Spot best, out Vector3 localPosition)
        {
            best = null;
            localPosition = Vector3.zero;
            if (part == null)
            {
                return false;
            }

            bool fastener = part.Definition != null && part.Definition.Category == PartCategory.Fastener;
            Rect visual = VisualBoundsOf(part);
            int bestArea = int.MaxValue;
            foreach (Spot spot in spots)
            {
                if (spot.anchor == null || !spot.IsFree || spot.fastenersOnly != fastener)
                {
                    continue;
                }

                if (!fastener && (visual.width > spot.size.x || visual.height > spot.size.y))
                {
                    continue;
                }

                int area = spot.size.x * spot.size.y;
                if (area < bestArea)
                {
                    bestArea = area;
                    best = spot;
                }
            }

            if (best == null)
            {
                return false;
            }

            localPosition = RestingPosition(part);
            return true;
        }

        private static Rect VisualBoundsOf(PhonePart part)
        {
            return part.TryGetComponent(out PartView2D view)
                ? view.VisualBounds
                : new Rect(-4f, -4f, 8f, 8f);
        }
    }
}
