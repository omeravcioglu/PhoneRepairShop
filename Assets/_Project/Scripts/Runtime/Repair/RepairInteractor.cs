using System;
using PhoneRepair.Audio;
using PhoneRepair.Devices;
using PhoneRepair.Interaction;
using UnityEngine;

namespace PhoneRepair.Repair
{
    public enum RepairDirection
    {
        None = 0,
        Detach = 1,
        Attach = 2,
    }

    public enum RepairBeginResult
    {
        /// <summary>Nothing to do here with this tool (the press is just a click/inspect).</summary>
        NoAction = 0,

        /// <summary>The right tool, but the rules said no; feedback was shown.</summary>
        Refused = 1,

        /// <summary>An action is running; keep calling Hold while the button is down.</summary>
        Started = 2,
    }

    /// <summary>What pressing on a part with the current tool would do.</summary>
    public readonly struct RepairPlan
    {
        public RepairPlan(PhonePart part, RepairDirection direction, bool allowed, bool wrongTool, string prompt)
        {
            Part = part;
            Direction = direction;
            Allowed = allowed;
            WrongTool = wrongTool;
            Prompt = prompt ?? string.Empty;
        }

        public PhonePart Part { get; }
        public RepairDirection Direction { get; }
        public bool Allowed { get; }
        public bool WrongTool { get; }
        public string Prompt { get; }

        public InteractionTone Tone => Direction == RepairDirection.None || WrongTool
            ? InteractionTone.Neutral
            : Allowed ? InteractionTone.Ready : InteractionTone.Blocked;
    }

    /// <summary>
    /// The repair verbs. Decides what a press on a part means with the tool in hand, checks the
    /// phone's assembly rules, then runs the action while the button is held:
    ///   Hold actions (pry, lift, unplug, put back) fill up over the part's hold time.
    ///   Screws run the screwdriver minigame (hold; circle the cursor to go faster).
    /// Removed parts fly to the tray/mat; putting them back flies them home.
    /// Driven by WorkbenchController (Begin / Hold / Release); has no input code of its own.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RepairInteractor : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private PhoneDevice phone;
        [SerializeField] private ToolBelt toolBelt;
        [SerializeField] private PartMover mover;
        [SerializeField] private PartsArea partsArea;
        [SerializeField] private RepairFeedback feedback;
        [Tooltip("Camera the player sees through (screw minigame reads the cursor around the screw on screen).")]
        [SerializeField] private Camera viewCamera;

        [Header("Screwdriver")]
        [Tooltip("Turns per second while just holding the button.")]
        [SerializeField, Min(0f)] private float holdTurnsPerSecond = 0.6f;
        [Tooltip("Extra turns per full circle of the cursor around the screw.")]
        [SerializeField, Min(0f)] private float turnsPerMouseCircle = 1f;
        [Tooltip("How far a screw rises (metres) as it loosens, before it pops out.")]
        [SerializeField, Min(0f)] private float screwRise = 0.0012f;

        [Header("Feel")]
        [Tooltip("How far a part strains outward while you hold to remove it (metres).")]
        [SerializeField, Min(0f)] private float strainDistance = 0.0018f;
        [Tooltip("How far (0-1 of the full flip) a connector lifts while you hold.")]
        [SerializeField, Range(0f, 1f)] private float hingeStrain = 0.3f;
        [SerializeField, Min(0.05f)] private float flightSeconds = 0.5f;
        [SerializeField, Min(0f)] private float flightArc = 0.05f;

        private ScrewTurnTracker tracker;
        private Active active;

        private Interactable hintTarget;
        private int hintTool = int.MinValue;
        private int hintVersion = int.MinValue;
        private bool hintBusy;
        private HoverHint hintCache = HoverHint.None;

        private enum Phase
        {
            Holding,
            Turning,
            Flying,
        }

        private sealed class Active
        {
            public PhonePart Part;
            public RepairDirection Direction;
            public Phase Phase;
            public float Progress;
            public Vector3 StartLocalPosition;
            public Quaternion StartLocalRotation;
            public Vector3 ContactLocal;
            public Vector3 NormalLocal = Vector3.up;
            public float LastTickTurns;
        }

        public PhoneDevice Phone
        {
            get => phone;
            set
            {
                Release();
                phone = value;
            }
        }

        public bool IsBusy => active != null;
        public PhonePart ActivePart => active != null ? active.Part : null;

        /// <summary>True while a progress ring makes sense (holding, or turning a screw).</summary>
        public bool ShowsProgress => active != null && active.Phase != Phase.Flying;

        /// <summary>0..1: hold progress, or how far along the screw is towards out/tight.</summary>
        public float ActiveProgress
        {
            get
            {
                if (active == null || active.Part == null)
                {
                    return 0f;
                }

                return active.Phase switch
                {
                    Phase.Holding => active.Progress,
                    Phase.Turning => active.Part.ScrewDirection >= 0 ? active.Part.Looseness : 1f - active.Part.Looseness,
                    _ => 0f,
                };
            }
        }

        private float Speed => toolBelt != null && toolBelt.Current != null ? toolBelt.Current.SpeedMultiplier : 1f;

        private void Awake()
        {
            tracker = new ScrewTurnTracker(holdTurnsPerSecond, turnsPerMouseCircle);
        }

        // ------------------------------------------------------------------ planning

        public RepairPlan Evaluate(Interactable target)
        {
            if (target == null || phone == null)
            {
                return default;
            }

            PhonePart part = target.GetComponent<PhonePart>();
            if (part == null || part.Device != phone || part.Definition == null)
            {
                return default;
            }

            PartDefinition definition = part.Definition;
            if (definition.DetachStyle == DetachStyle.Fixed)
            {
                return new RepairPlan(part, RepairDirection.None, false, false, "Holds everything together");
            }

            if (part.IsBusy)
            {
                return new RepairPlan(part, RepairDirection.None, false, false, string.Empty);
            }

            RepairDirection direction = ChooseDirection(part);
            ToolKind needed = direction == RepairDirection.Detach ? definition.DetachTool : definition.AttachTool;
            string verb = direction == RepairDirection.Detach ? definition.DetachVerb : definition.AttachVerb;
            if (toolBelt == null || toolBelt.CurrentKind != needed)
            {
                string tool = toolBelt != null ? toolBelt.Describe(needed) : needed.ToString();
                return new RepairPlan(part, direction, false, true, $"{verb}: needs {tool}");
            }

            RuleCheck rule = direction == RepairDirection.Detach ? phone.CanDetach(part) : phone.CanAttach(part);
            if (!rule.Allowed)
            {
                return new RepairPlan(part, direction, false, false, rule.Reason);
            }

            if (direction == RepairDirection.Detach && definition.DetachStyle == DetachStyle.Remove &&
                partsArea != null && !partsArea.HasRoomFor(part))
            {
                return new RepairPlan(part, direction, false, false, "No room left on the mat");
            }

            return new RepairPlan(part, direction, true, false, $"Hold · {verb}");
        }

        /// <summary>Hover prompt for a target, cached until the tool, the phone state or the part's busy flag changes.</summary>
        public HoverHint DescribeHover(Interactable target)
        {
            if (target == null)
            {
                hintTarget = null;
                return HoverHint.None;
            }

            int tool = toolBelt != null ? toolBelt.CurrentIndex : -1;
            int version = phone != null ? phone.StateVersion : 0;
            PhonePart part = target.GetComponent<PhonePart>();
            bool busy = part != null && part.IsBusy;
            if (target == hintTarget && tool == hintTool && version == hintVersion && busy == hintBusy)
            {
                return hintCache;
            }

            RepairPlan plan = Evaluate(target);
            hintTarget = target;
            hintTool = tool;
            hintVersion = version;
            hintBusy = busy;
            hintCache = new HoverHint(plan.Prompt, plan.Tone);
            return hintCache;
        }

        private static RepairDirection ChooseDirection(PhonePart part)
        {
            switch (part.State)
            {
                case PartState.Secured:
                    return RepairDirection.Detach;
                case PartState.Removed:
                    return RepairDirection.Attach;
                default:
                    if (part.Definition.DetachStyle == DetachStyle.ReleaseInPlace)
                    {
                        return RepairDirection.Attach; // plug it back in
                    }

                    // a loose screw keeps going the way it was going
                    return part.ScrewDirection >= 0 ? RepairDirection.Detach : RepairDirection.Attach;
            }
        }

        // ------------------------------------------------------------------ running

        /// <summary>Start working the part under the cursor. <paramref name="hit"/> is where the press landed.</summary>
        public RepairBeginResult Begin(Interactable target, RaycastHit hit)
        {
            if (active != null)
            {
                return RepairBeginResult.NoAction;
            }

            RepairPlan plan = Evaluate(target);
            if (plan.Direction == RepairDirection.None || plan.WrongTool)
            {
                return RepairBeginResult.NoAction;
            }

            if (!plan.Allowed)
            {
                if (feedback != null)
                {
                    feedback.Refuse(target, plan.Prompt);
                }

                return RepairBeginResult.Refused;
            }

            PhonePart part = plan.Part;
            Transform t = part.transform;
            active = new Active
            {
                Part = part,
                Direction = plan.Direction,
                StartLocalPosition = t.localPosition,
                StartLocalRotation = t.localRotation,
                ContactLocal = hit.collider != null ? t.InverseTransformPoint(hit.point) : Vector3.zero,
                NormalLocal = hit.collider != null ? t.InverseTransformDirection(hit.normal) : Vector3.up,
            };

            if (part.Definition.Interaction == PartInteraction.Screw)
            {
                tracker.Reset();
                if (plan.Direction == RepairDirection.Attach && part.State == PartState.Removed)
                {
                    FlyScrewHome(part);
                }
                else
                {
                    part.ScrewDirection = plan.Direction == RepairDirection.Detach ? 1 : -1;
                    active.Phase = Phase.Turning;
                    active.LastTickTurns = part.Looseness * part.Definition.ScrewTurns;
                }
            }
            else
            {
                active.Phase = Phase.Holding;
            }

            Play(SfxId.Select, 1f, 0.5f);
            return RepairBeginResult.Started;
        }

        /// <summary>Advance while the button is held. Returns false once the action is finished.</summary>
        public bool Hold(Vector2 pointer, float deltaTime)
        {
            if (active == null)
            {
                return false;
            }

            if (active.Part == null)
            {
                active = null;
                return false;
            }

            return active.Phase switch
            {
                Phase.Holding => TickHold(active.Part, deltaTime),
                Phase.Turning => TickScrew(active.Part, pointer, deltaTime),
                _ => true, // screw flying home; turning starts when it lands
            };
        }

        /// <summary>Button let go. Holds snap back; a screw stays as loose as it got.</summary>
        public void Release()
        {
            if (active == null)
            {
                return;
            }

            if (active.Phase == Phase.Holding && active.Part != null)
            {
                RestoreAfterCancel(active);
            }

            active = null;
        }

        public void Cancel() => Release();

        /// <summary>Where a tool model should sit while working: tip, axis (pointing away from the part), spin.</summary>
        public bool TryGetToolPose(out Vector3 tip, out Vector3 axis, out float spinDegrees)
        {
            tip = default;
            axis = Vector3.up;
            spinDegrees = 0f;
            if (active == null || active.Part == null || active.Phase == Phase.Flying)
            {
                return false;
            }

            Transform t = active.Part.transform;
            PartDefinition definition = active.Part.Definition;
            if (definition.Interaction == PartInteraction.Screw)
            {
                float headTop = HeadHeight(active.Part);
                tip = t.TransformPoint(new Vector3(0f, headTop, 0f));
                axis = t.up;
                spinDegrees = -360f * active.Part.Looseness * definition.ScrewTurns;
                return true;
            }

            tip = t.TransformPoint(active.ContactLocal);
            axis = t.TransformDirection(active.NormalLocal).normalized;
            return true;
        }

        private bool TickHold(PhonePart part, float deltaTime)
        {
            PartDefinition definition = part.Definition;
            float seconds = active.Direction == RepairDirection.Detach ? definition.DetachSeconds : definition.AttachSeconds;
            active.Progress = Mathf.Min(1f, active.Progress + deltaTime * Speed / Mathf.Max(0.05f, seconds));
            ApplyStrain(part, active.Progress);
            if (active.Progress < 1f)
            {
                return true;
            }

            RepairDirection direction = active.Direction;
            active = null;
            CompleteHold(part, direction);
            return false;
        }

        private void ApplyStrain(PhonePart part, float progress)
        {
            float eased = 1f - (1f - progress) * (1f - progress);
            PartDefinition definition = part.Definition;
            if (definition.DetachStyle == DetachStyle.ReleaseInPlace)
            {
                float from = active.Direction == RepairDirection.Detach ? 0f : 1f;
                float to = active.Direction == RepairDirection.Detach ? hingeStrain : 1f - hingeStrain;
                part.transform.localRotation = HingeRotation(part, Mathf.Lerp(from, to, eased));
                return;
            }

            // on the phone: strain outwards; on the mat: lift a little
            Vector3 direction = active.Direction == RepairDirection.Detach
                ? part.InstalledLocalRotation * definition.RemovalDirection
                : Vector3.up;
            float jitter = (Mathf.PerlinNoise(Time.time * 45f, 0.37f) - 0.5f) * 0.0005f * progress;
            part.transform.localPosition = active.StartLocalPosition + direction * (strainDistance * eased + jitter);
        }

        private void CompleteHold(PhonePart part, RepairDirection direction)
        {
            PartDefinition definition = part.Definition;
            if (definition.DetachStyle == DetachStyle.ReleaseInPlace)
            {
                bool unplug = direction == RepairDirection.Detach;
                phone.SetState(part, unplug ? PartState.Loose : PartState.Secured);
                if (mover != null)
                {
                    mover.RotateLocal(part.transform, HingeRotation(part, unplug ? 1f : 0f), 0.12f);
                }

                Play(unplug ? SfxId.Unplug : SfxId.Click);
                Confirm(part);
                return;
            }

            if (direction == RepairDirection.Detach)
            {
                SendToPartsArea(part, 0);
            }
            else
            {
                FlyHome(part, PartState.Secured, Vector3.zero, () =>
                {
                    Play(SfxId.Click, UnityEngine.Random.Range(0.95f, 1.05f));
                    Confirm(part);
                });
            }
        }

        private bool TickScrew(PhonePart part, Vector2 pointer, float deltaTime)
        {
            if (viewCamera == null)
            {
                return false;
            }

            PartDefinition definition = part.Definition;
            Vector3 screen = viewCamera.WorldToScreenPoint(part.transform.position);
            if (screen.z <= 0f)
            {
                return true;
            }

            float turns = tracker.Step(pointer, new Vector2(screen.x, screen.y), deltaTime, part.ScrewDirection) * Speed;
            float totalTurns = Mathf.Max(0.25f, definition.ScrewTurns);
            part.Looseness = Mathf.Clamp01(part.Looseness + part.ScrewDirection * turns / totalTurns);
            if (part.State == PartState.Secured && part.Looseness > 0f)
            {
                phone.SetState(part, PartState.Loose);
            }

            ApplyScrewPose(part);

            float turnsDone = part.Looseness * totalTurns;
            if (Mathf.Abs(turnsDone - active.LastTickTurns) >= 0.25f)
            {
                active.LastTickTurns = turnsDone;
                Play(SfxId.Tick, UnityEngine.Random.Range(0.9f, 1.1f), 0.6f);
            }

            if (part.ScrewDirection > 0 && part.Looseness >= 1f)
            {
                active = null;
                part.ScrewDirection = -1; // next time it comes back, it gets tightened
                SendToPartsArea(part, 2);
                return false;
            }

            if (part.ScrewDirection < 0 && part.Looseness <= 0f)
            {
                active = null;
                part.ScrewDirection = 1;
                phone.SetState(part, PartState.Secured);
                ApplyScrewPose(part);
                if (mover != null)
                {
                    mover.Squash(part.transform, 0.12f);
                }

                Play(SfxId.Click, 1.1f);
                Confirm(part);
                return false;
            }

            return true;
        }

        private void ApplyScrewPose(PhonePart part)
        {
            float turnsDone = part.Looseness * part.Definition.ScrewTurns;
            Vector3 axis = part.InstalledLocalRotation * Vector3.up;
            part.transform.localPosition = part.InstalledLocalPosition + axis * (screwRise * part.Looseness);
            // loosening turns counter-clockwise as seen looking at the head (negative about local up)
            part.transform.localRotation = part.InstalledLocalRotation * Quaternion.AngleAxis(-360f * turnsDone, Vector3.up);
        }

        private void FlyScrewHome(PhonePart part)
        {
            active.Phase = Phase.Flying;
            part.ScrewDirection = -1;
            part.Looseness = 1f;
            Vector3 raised = part.InstalledLocalRotation * Vector3.up * screwRise;
            FlyHome(part, PartState.Loose, raised, () =>
            {
                ApplyScrewPose(part);
                if (active != null && active.Part == part && active.Phase == Phase.Flying)
                {
                    active.Phase = Phase.Turning;
                    active.LastTickTurns = part.Looseness * part.Definition.ScrewTurns;
                    tracker.Reset();
                }

                Play(SfxId.Tick, 0.8f);
            });
        }

        private void SendToPartsArea(PhonePart part, int spinTurns)
        {
            if (partsArea == null || mover == null || !partsArea.TryReserve(part, out PartsArea.Slot slot, out Pose pose))
            {
                Refuse(part, "No room left on the mat");
                part.transform.SetLocalPositionAndRotation(part.InstalledLocalPosition, part.InstalledLocalRotation);
                return;
            }

            Transform parent = part.transform.parent;
            Vector3 exit = parent != null
                ? parent.TransformDirection(part.InstalledLocalRotation * part.Definition.RemovalDirection)
                : Vector3.up;
            phone.SetState(part, PartState.Removed);
            mover.Fly(part, slot.Anchor, pose, exit, Vector3.up, flightArc, spinTurns, flightSeconds,
                () => Play(SfxId.Thunk, UnityEngine.Random.Range(0.92f, 1.08f), 0.7f));
            Play(SfxId.Pop, UnityEngine.Random.Range(0.95f, 1.05f));
            Play(SfxId.Whoosh, 1f, 0.4f);
        }

        private void FlyHome(PhonePart part, PartState newState, Vector3 localOffset, Action onArrived)
        {
            Transform parent = part.InstalledParent;
            if (mover == null || parent == null)
            {
                return;
            }

            if (partsArea != null)
            {
                partsArea.Release(part);
            }

            Vector3 arriveFrom = parent.TransformDirection(part.InstalledLocalRotation * part.Definition.RemovalDirection);
            phone.SetState(part, newState);
            var pose = new Pose(part.InstalledLocalPosition + localOffset, part.InstalledLocalRotation);
            mover.Fly(part, parent, pose, Vector3.up, arriveFrom, flightArc, 0, flightSeconds, onArrived);
            Play(SfxId.Whoosh, 1.15f, 0.35f);
        }

        private void RestoreAfterCancel(Active cancelled)
        {
            PhonePart part = cancelled.Part;
            if (part.Definition.DetachStyle == DetachStyle.ReleaseInPlace)
            {
                Quaternion rest = HingeRotation(part, part.State == PartState.Secured ? 0f : 1f);
                if (mover != null)
                {
                    mover.RotateLocal(part.transform, rest, 0.1f);
                }
                else
                {
                    part.transform.localRotation = rest;
                }

                return;
            }

            part.transform.SetLocalPositionAndRotation(cancelled.StartLocalPosition, cancelled.StartLocalRotation);
        }

        private static Quaternion HingeRotation(PhonePart part, float amount)
        {
            PartDefinition definition = part.Definition;
            return part.InstalledLocalRotation * Quaternion.AngleAxis(definition.ReleaseAngle * amount, definition.ReleaseAxis);
        }

        private static float HeadHeight(PhonePart part)
        {
            return part.TryGetComponent(out MeshFilter filter) && filter.sharedMesh != null
                ? Mathf.Max(0f, filter.sharedMesh.bounds.max.y)
                : 0f;
        }

        private void Refuse(PhonePart part, string reason)
        {
            if (feedback != null)
            {
                feedback.Refuse(part.GetComponent<Interactable>(), reason);
            }
        }

        private void Confirm(PhonePart part)
        {
            if (feedback != null)
            {
                feedback.Confirm(part.GetComponent<Interactable>());
            }
        }

        private void Play(SfxId id, float pitch = 1f, float volume = 1f)
        {
            if (feedback != null)
            {
                feedback.Play(id, pitch, volume);
            }
        }
    }
}
