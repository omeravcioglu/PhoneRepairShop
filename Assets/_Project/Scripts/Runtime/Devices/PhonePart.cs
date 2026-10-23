using System;
using PhoneRepair.Interaction;
using UnityEngine;

namespace PhoneRepair.Devices
{
    /// <summary>
    /// One component slot in one phone ("the battery in THIS phone"). Remembers where it sits when
    /// installed, carries the runtime bits of a repair (screw looseness, busy while animating, whether
    /// it is broken) and describes itself to the UI. Assembly state itself lives in the owning PhoneDevice.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PhonePart : MonoBehaviour, IInspectionInfoProvider
    {
        [Tooltip("Unique within the phone. Matches the model node name and the PhoneModelDefinition slot.")]
        [SerializeField] private string slotId;
        [SerializeField] private PartDefinition definition;

        private PhoneDevice device;
        private Interactable interactable;
        private float looseness;
        private bool poseCaptured;
        private Transform installedParent;
        private Vector3 installedLocalPosition;
        private Quaternion installedLocalRotation = Quaternion.identity;

        public string SlotId => string.IsNullOrWhiteSpace(slotId) ? gameObject.name : slotId;
        public PartDefinition Definition => definition;
        public PhoneDevice Device => device != null ? device : device = GetComponentInParent<PhoneDevice>(true);
        public PartState State => Device != null ? Device.GetState(this) : PartState.Secured;

        /// <summary>0 = fully fastened, 1 = ready to come out. Drives the screw's turn and rise.</summary>
        public float Looseness
        {
            get => looseness;
            set
            {
                if (Mathf.Approximately(looseness, value))
                {
                    return;
                }

                looseness = value;
                Changed?.Invoke(this);
            }
        }

        /// <summary>+1 while the screw is being loosened, -1 while being tightened (remembered between grabs).</summary>
        public int ScrewDirection { get; set; } = 1;

        /// <summary>True while an animation owns the transform (flying to the tray, back into the phone...).</summary>
        public bool IsBusy { get; set; }

        /// <summary>Broken parts make the power test fail until they are swapped for new ones.</summary>
        public PartCondition Condition { get; private set; } = PartCondition.Good;

        /// <summary>Raised when the state, condition or screw looseness changed (views redraw).</summary>
        public event Action<PhonePart> Changed;

        public Transform InstalledParent
        {
            get
            {
                CaptureInstalledPose();
                return installedParent;
            }
        }

        public Vector3 InstalledLocalPosition
        {
            get
            {
                CaptureInstalledPose();
                return installedLocalPosition;
            }
        }

        public Quaternion InstalledLocalRotation
        {
            get
            {
                CaptureInstalledPose();
                return installedLocalRotation;
            }
        }

        private void Awake()
        {
            // Cache the owner while still inside the phone: removed parts live on the mat.
            device = GetComponentInParent<PhoneDevice>(true);
            CaptureInstalledPose();
        }

        internal void Bind(PhoneDevice owner) => device = owner;

        public void SetCondition(PartCondition condition)
        {
            if (Condition == condition)
            {
                return;
            }

            Condition = condition;
            NotifyStateChanged();
        }

        /// <summary>Remembers the assembled pose. The prefab is authored assembled, so the first call wins.</summary>
        public void CaptureInstalledPose()
        {
            if (poseCaptured)
            {
                return;
            }

            poseCaptured = true;
            installedParent = transform.parent;
            installedLocalPosition = transform.localPosition;
            installedLocalRotation = transform.localRotation;
        }

        public InspectionInfo GetInspectionInfo()
        {
            if (definition == null)
            {
                return new InspectionInfo(gameObject.name, "Unassigned part");
            }

            string category = definition.Category.ToLabel();
            string modelName = Device != null && Device.Model != null ? Device.Model.ModelName : null;
            string subtitle = string.IsNullOrEmpty(modelName) ? category : $"{category}  ·  {modelName}";
            return new InspectionInfo(definition.DisplayName, subtitle, definition.Description, DescribeState());
        }

        /// <summary>Short status for the UI: "Installed", "Loose", "Unplugged", "In the screw tray"...</summary>
        public string DescribeState()
        {
            if (definition == null)
            {
                return string.Empty;
            }

            if (Condition == PartCondition.Broken)
            {
                return "Broken";
            }

            switch (definition.DetachStyle)
            {
                case DetachStyle.Fixed:
                    return "Part of the phone";
                case DetachStyle.ReleaseInPlace:
                    return State == PartState.Secured ? "Plugged in" : "Unplugged";
            }

            switch (State)
            {
                case PartState.Secured:
                    return definition.Interaction == PartInteraction.Screw ? "Tight" : "Installed";
                case PartState.Loose:
                    return "Loose";
                default:
                    return definition.Category == PartCategory.Fastener ? "In the screw tray" : "On the mat";
            }
        }

        internal void NotifyStateChanged()
        {
            if (interactable == null)
            {
                interactable = GetComponent<Interactable>();
            }

            if (interactable != null)
            {
                interactable.NotifyInfoChanged();
            }

            Changed?.Invoke(this);
        }
    }
}
