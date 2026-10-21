using System;
using System.Collections.Generic;
using UnityEngine;

namespace PhoneRepair.Devices
{
    /// <summary>
    /// Root of one physical phone in the world. Indexes its parts by slot id and owns the
    /// assembly state: which parts are secured, loose or removed, and what may happen next.
    /// The rules come from the PhoneModelDefinition, so every phone model uses this same script.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PhoneDevice : MonoBehaviour
    {
        [SerializeField] private PhoneModelDefinition model;

        private PhonePart[] parts;
        private Dictionary<string, PhonePart> partsBySlot;
        private AssemblyGraph graph;

        /// <summary>Raised after any part changes state (for UI, audio, the future repair checker).</summary>
        public event Action<PhonePart> PartStateChanged;

        public PhoneModelDefinition Model => model;

        /// <summary>Increments on every state change; cheap way for views to know they must refresh.</summary>
        public int StateVersion { get; private set; }

        public IReadOnlyList<PhonePart> Parts
        {
            get
            {
                EnsureIndexed();
                return parts;
            }
        }

        public bool TryGetPart(string slotId, out PhonePart part)
        {
            EnsureIndexed();
            return partsBySlot.TryGetValue(slotId, out part);
        }

        public PartState GetState(PhonePart part)
        {
            EnsureIndexed();
            return graph.TryGet(part.SlotId, out AssemblyGraph.Node node) ? node.State : PartState.Secured;
        }

        public RuleCheck CanDetach(PhonePart part)
        {
            EnsureIndexed();
            return graph.TryGet(part.SlotId, out _) ? graph.CanDetach(part.SlotId) : RuleCheck.No($"{part.name} has no assembly data.");
        }

        public RuleCheck CanAttach(PhonePart part)
        {
            EnsureIndexed();
            return graph.TryGet(part.SlotId, out _) ? graph.CanAttach(part.SlotId) : RuleCheck.No($"{part.name} has no assembly data.");
        }

        /// <summary>Records a state change. Callers validate with CanDetach/CanAttach first.</summary>
        public void SetState(PhonePart part, PartState state)
        {
            EnsureIndexed();
            if (!graph.TryGet(part.SlotId, out AssemblyGraph.Node node) || node.State == state)
            {
                return;
            }

            graph.SetState(part.SlotId, state);
            StateVersion++;
            part.NotifyStateChanged();
            PartStateChanged?.Invoke(part);
        }

        /// <summary>True when every removable part is back and fastened.</summary>
        public bool IsFullyAssembled()
        {
            EnsureIndexed();
            foreach (AssemblyGraph.Node node in graph.Nodes)
            {
                if (node.State != PartState.Secured)
                {
                    return false;
                }
            }

            return true;
        }

        private void Awake() => EnsureIndexed();

        private void EnsureIndexed()
        {
            if (parts != null)
            {
                return;
            }

            parts = GetComponentsInChildren<PhonePart>(true);
            partsBySlot = new Dictionary<string, PhonePart>(parts.Length);
            foreach (PhonePart part in parts)
            {
                part.Bind(this);
                part.CaptureInstalledPose();
                if (!partsBySlot.TryAdd(part.SlotId, part))
                {
                    Debug.LogError($"{name}: duplicate part slot id '{part.SlotId}'.", part);
                }
            }

            graph = AssemblyGraph.FromDefinition(model);
            if (model == null)
            {
                Debug.LogError($"{name}: PhoneDevice has no PhoneModelDefinition; parts cannot be worked on.", this);
            }

            foreach (string problem in graph.FindMissingReferences())
            {
                Debug.LogError($"{name}: assembly rule points at a missing slot: {problem}", this);
            }
        }
    }
}
