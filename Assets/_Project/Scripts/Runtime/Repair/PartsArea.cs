using System;
using System.Collections.Generic;
using PhoneRepair.Core;
using PhoneRepair.Devices;
using UnityEngine;

namespace PhoneRepair.Repair
{
    /// <summary>
    /// Where removed parts go: compartments of the screw tray for fasteners, marked spots on the
    /// mat for everything else. Picks the smallest free spot a part fits (turning it 90° if that
    /// helps) and lays it down the way it sits in a face-up phone; screws lie on their side.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PartsArea : MonoBehaviour
    {
        [Serializable]
        public sealed class Slot
        {
            [Tooltip("On the surface, +Y up. Parts are centred on it, resting on it.")]
            public Transform anchor;
            [Tooltip("Usable footprint (x, z) in metres.")]
            public Vector2 size = new(0.08f, 0.08f);
            public bool fastenersOnly;
            [NonSerialized] public PhonePart occupant;

            public Transform Anchor => anchor;
            public bool IsFree => occupant == null;
        }

        [SerializeField] private List<Slot> slots = new();
        [Tooltip("Space kept around a part inside its spot.")]
        [SerializeField, Min(0f)] private float clearance = 0.003f;

        public IReadOnlyList<Slot> Slots => slots;

        public void AddSlot(Transform anchor, Vector2 size, bool fastenersOnly) =>
            slots.Add(new Slot { anchor = anchor, size = size, fastenersOnly = fastenersOnly });

        public bool Holds(PhonePart part) => IndexOf(part) >= 0;

        public bool HasRoomFor(PhonePart part) => Holds(part) || TryPlan(part, out _, out _);

        /// <summary>Claims the best free spot for the part and returns its resting pose in the spot's space.</summary>
        public bool TryReserve(PhonePart part, out Slot slot, out Pose localPose)
        {
            Release(part);
            if (!TryPlan(part, out slot, out localPose))
            {
                return false;
            }

            slot.occupant = part;
            return true;
        }

        public void Release(PhonePart part)
        {
            int index = IndexOf(part);
            if (index >= 0)
            {
                slots[index].occupant = null;
            }
        }

        private int IndexOf(PhonePart part)
        {
            if (part == null)
            {
                return -1;
            }

            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].occupant == part)
                {
                    return i;
                }
            }

            return -1;
        }

        private bool TryPlan(PhonePart part, out Slot best, out Pose bestPose)
        {
            best = null;
            bestPose = default;
            if (part == null)
            {
                return false;
            }

            bool fastener = part.Definition != null && part.Definition.Category == PartCategory.Fastener;
            Bounds local = BoundsUtility.LocalBounds(part.transform);
            float bestArea = float.MaxValue;
            for (int i = 0; i < slots.Count; i++)
            {
                Slot slot = slots[i];
                if (slot.anchor == null || !slot.IsFree || slot.fastenersOnly != fastener)
                {
                    continue;
                }

                if (!TryFit(part, local, slot, i, fastener, out Pose pose))
                {
                    continue;
                }

                float area = slot.size.x * slot.size.y;
                if (area < bestArea)
                {
                    bestArea = area;
                    best = slot;
                    bestPose = pose;
                }
            }

            return best != null;
        }

        private bool TryFit(PhonePart part, Bounds partLocalBounds, Slot slot, int slotIndex, bool fastener, out Pose pose)
        {
            Quaternion rest = fastener
                ? Quaternion.Euler(0f, 35f + 47f * slotIndex, 90f) // on its side, each one at its own angle
                : part.InstalledLocalRotation;                     // as it lies in a face-up phone

            for (int attempt = 0; attempt < 2; attempt++)
            {
                Quaternion rotation = attempt == 0 ? rest : Quaternion.Euler(0f, 90f, 0f) * rest;
                Bounds b = BoundsUtility.Rotate(partLocalBounds, rotation);
                bool fits = fastener ||
                            (b.size.x + clearance * 2f <= slot.size.x && b.size.z + clearance * 2f <= slot.size.y);
                if (!fits)
                {
                    continue;
                }

                // centre the footprint on the anchor, bottom resting on it
                pose = new Pose(new Vector3(-b.center.x, b.extents.y - b.center.y, -b.center.z), rotation);
                return true;
            }

            pose = default;
            return false;
        }
    }
}
