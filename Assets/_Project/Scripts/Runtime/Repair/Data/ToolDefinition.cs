using PhoneRepair.Devices;
using UnityEngine;

namespace PhoneRepair.Repair
{
    /// <summary>
    /// One concrete tool the player can own ("Basic Screwdriver"). Parts ask for a ToolKind; any
    /// tool of that kind works, so upgrades are just new assets with a better speed.
    /// </summary>
    [CreateAssetMenu(fileName = "TOOL_New", menuName = "Phone Repair/Tool Definition", order = 20)]
    public sealed class ToolDefinition : ScriptableObject
    {
        [Tooltip("Stable id for save data and the shop. Never change it after release.")]
        [SerializeField] private string toolId;
        [SerializeField] private string displayName;
        [SerializeField] private ToolKind kind = ToolKind.Hands;
        [SerializeField, TextArea(2, 4)] private string description;
        [Tooltip("Picture on the bench's tool rack.")]
        [SerializeField] private Sprite icon;
        [Tooltip("Mouse cursor while this tool is in hand; its pivot is the working tip. Empty = the hand.")]
        [SerializeField] private Sprite cursor;
        [Tooltip("1 = normal. Upgrades make holds and screw turns faster.")]
        [SerializeField, Min(0.1f)] private float speedMultiplier = 1f;

        public string ToolId => string.IsNullOrWhiteSpace(toolId) ? name : toolId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public ToolKind Kind => kind;
        public string Description => description ?? string.Empty;
        public Sprite Icon => icon;
        public Sprite Cursor => cursor;
        public float SpeedMultiplier => speedMultiplier;
    }
}
