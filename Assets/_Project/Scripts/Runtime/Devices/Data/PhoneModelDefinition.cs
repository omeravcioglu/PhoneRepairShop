using System;
using System.Collections.Generic;
using UnityEngine;

namespace PhoneRepair.Devices
{
    /// <summary>One node of the phone model, the kind of part it is, and how it is held in place.</summary>
    [Serializable]
    public struct PartSlot
    {
        [Tooltip("Object name in the model (exported from Blender). Unique within the phone.")]
        public string slotId;
        public PartDefinition part;

        [Tooltip("Slots that must be off (or unplugged) before this one can come off, and while putting it back. E.g. the back cover is blocked by its screws.")]
        public string[] blockedBy;

        [Tooltip("Slots that must be in place and fastened before this one can go back on: what it attaches to. E.g. screws need the back cover.")]
        public string[] mountedOn;
    }

    /// <summary>
    /// A phone model: identity, the assembled interactive prefab, which model node is which part,
    /// and the assembly rules between them. Adding a phone = model it in Blender with slot names,
    /// create one of these, run the setup. No phone-specific code.
    /// </summary>
    [CreateAssetMenu(fileName = "PHN_NewPhone", menuName = "Phone Repair/Phone Model Definition", order = 0)]
    public sealed class PhoneModelDefinition : ScriptableObject
    {
        [Tooltip("Stable id for save data. Never change it after release.")]
        [SerializeField] private string modelId;
        [SerializeField] private string brand;
        [SerializeField] private string modelName;
        [SerializeField, TextArea(2, 5)] private string description;
        [Tooltip("Assembled, interactive phone. Built by Tools > Phone Repair > Run Setup.")]
        [SerializeField] private GameObject prefab;
        [SerializeField] private List<PartSlot> slots = new();

        public string ModelId => string.IsNullOrWhiteSpace(modelId) ? name : modelId;
        public string Brand => brand ?? string.Empty;
        public string ModelName => string.IsNullOrWhiteSpace(modelName) ? name : modelName;
        public string Description => description ?? string.Empty;
        public GameObject Prefab => prefab;
        public IReadOnlyList<PartSlot> Slots => slots;

        public bool TryGetSlot(string slotId, out PartSlot slot)
        {
            foreach (PartSlot candidate in slots)
            {
                if (candidate.slotId == slotId)
                {
                    slot = candidate;
                    return true;
                }
            }

            slot = default;
            return false;
        }
    }
}
