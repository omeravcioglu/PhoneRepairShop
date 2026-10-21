using UnityEngine;

namespace PhoneRepair.Devices
{
    /// <summary>
    /// A TYPE of component ("Mochi One battery", "P2 screw"), shared by every phone that uses it
    /// and later by the spare-parts shop. Says what the part is and how it is worked on.
    /// Per-phone state (secured, loose, removed) lives on PhonePart / PhoneDevice.
    /// </summary>
    [CreateAssetMenu(fileName = "PRT_NewPart", menuName = "Phone Repair/Part Definition", order = 10)]
    public sealed class PartDefinition : ScriptableObject
    {
        [Tooltip("Stable id for save data and inventory. Never change it after release.")]
        [SerializeField] private string partId;
        [SerializeField] private string displayName;
        [SerializeField] private PartCategory category = PartCategory.Other;
        [SerializeField, TextArea(2, 6)] private string description;

        [Header("Repair")]
        [SerializeField] private DetachStyle detachStyle = DetachStyle.Remove;
        [SerializeField] private PartInteraction interaction = PartInteraction.Hold;
        [SerializeField] private ToolKind detachTool = ToolKind.Hands;
        [SerializeField] private ToolKind attachTool = ToolKind.Hands;
        [Tooltip("Player-facing verb for taking it off, e.g. 'Unscrew', 'Pry off', 'Unplug', 'Lift out'.")]
        [SerializeField] private string detachVerb;
        [Tooltip("Player-facing verb for putting it back, e.g. 'Screw in', 'Snap on', 'Plug in', 'Seat'.")]
        [SerializeField] private string attachVerb;
        [Tooltip("Hold time (seconds, before tool speed) for Hold interactions.")]
        [SerializeField, Min(0f)] private float detachSeconds = 0.5f;
        [SerializeField, Min(0f)] private float attachSeconds = 0.3f;
        [Tooltip("Full turns to loosen or tighten (Screw interaction).")]
        [SerializeField, Min(0.25f)] private float screwTurns = 3f;
        [Tooltip("Direction the part leaves the phone, in the part's own local space (e.g. (0,-1,0) = out of the back).")]
        [SerializeField] private Vector3 removalDirection = Vector3.down;
        [Tooltip("ReleaseInPlace parts: hinge axis in local space, and how far they flip up when unplugged.")]
        [SerializeField] private Vector3 releaseAxis = Vector3.right;
        [SerializeField, Range(0f, 120f)] private float releaseAngle = 70f;

        public string PartId => string.IsNullOrWhiteSpace(partId) ? name : partId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public PartCategory Category => category;
        public string Description => description ?? string.Empty;

        public DetachStyle DetachStyle => detachStyle;
        public PartInteraction Interaction => interaction;
        public ToolKind DetachTool => detachTool;
        public ToolKind AttachTool => attachTool;
        public string DetachVerb => string.IsNullOrWhiteSpace(detachVerb) ? "Remove" : detachVerb;
        public string AttachVerb => string.IsNullOrWhiteSpace(attachVerb) ? "Put back" : attachVerb;
        public float DetachSeconds => detachSeconds;
        public float AttachSeconds => attachSeconds;
        public float ScrewTurns => screwTurns;
        public Vector3 RemovalDirection => removalDirection.sqrMagnitude > 1e-6f ? removalDirection.normalized : Vector3.down;
        public Vector3 ReleaseAxis => releaseAxis.sqrMagnitude > 1e-6f ? releaseAxis.normalized : Vector3.right;
        public float ReleaseAngle => releaseAngle;

        /// <summary>True once repair data has been authored (used by the setup to migrate old assets).</summary>
        public bool HasRepairData => !string.IsNullOrWhiteSpace(detachVerb);
    }
}
