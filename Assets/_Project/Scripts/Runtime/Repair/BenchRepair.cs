using System;
using PhoneRepair.Audio;
using PhoneRepair.Devices;
using PhoneRepair.Interaction;
using PhoneRepair.Pixel;
using UnityEngine;

namespace PhoneRepair.Repair
{
    /// <summary>
    /// The repair verbs on the 2D bench (the Milestone 2 rules, now drawn in pixels). Decides what a
    /// press on a part means with the tool in hand, checks the phone's assembly rules, then runs the
    /// action while the button is held:
    ///   hold actions (pry, lift, unplug, put back) fill up over the part's hold time;
    ///   screws run the screwdriver minigame (hold; circle the cursor to go faster).
    /// Removed parts fly to the mat or the screw tray; putting them back flies them home.
    /// Driven by the pointer router (Begin / Hold / Release); it has no input code of its own.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BenchRepair : MonoBehaviour
    {
        [Header("Bench")]
        [SerializeField] private ToolBelt toolBelt;
        [SerializeField] private PartsTray2D tray;
        [SerializeField] private PartFlights2D flights;
        [SerializeField] private BenchFeedback feedback;

        [Header("Screwdriver")]
        [Tooltip("Turns per second while just holding the button.")]
        [SerializeField, Min(0f)] private float holdTurnsPerSecond = 0.6f;
        [Tooltip("Extra turns per full circle of the cursor around the point where the button went down.")]
        [SerializeField, Min(0f)] private float turnsPerMouseCircle = 1f;
        [Tooltip("Pixels a screw backs out (in steps) before it pops.")]
        [SerializeField, Min(0)] private int screwRisePixels = 2;

        [Header("Feel")]
        [Tooltip("Pixels a part lifts while you hold to take it off.")]
        [SerializeField, Min(0)] private int strainPixels = 2;
        [SerializeField, Min(0.05f)] private float flightSeconds = 0.42f;
        [Tooltip("Height of the hop when parts fly to the mat or home (pixels).")]
        [SerializeField, Min(0f)] private float flightArc = 18f;

        private ScrewTurnTracker tracker;
        private Active active;
        private Phone2D phone;

        private Interactable hintTarget;
        private int hintTool = int.MinValue;
        private int hintVersion = int.MinValue;
        private bool hintBusy;
        private Phone2D.Side hintSide;
        private PartCondition hintCondition;
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
            public Vector2 PressPointer;
            public float LastTickTurns;
        }

        public Phone2D Phone => phone;
        public PhoneDevice Device => phone != null ? phone.Device : null;
        public PartsTray2D Tray => tray;
        public bool IsBusy => active != null;
        public PhonePart ActivePart => active?.Part;

        /// <summary>True while the progress ring makes sense (holding, or turning a screw).</summary>
        public bool ShowsProgress => active != null && active.Phase != Phase.Flying;

        /// <summary>0..1: hold progress, or how far the screw is towards out/tight.</summary>
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

        private ScrewTurnTracker Tracker => tracker ??= new ScrewTurnTracker(holdTurnsPerSecond, turnsPerMouseCircle);

        public void BeginJob(Phone2D jobPhone)
        {
            Release();
            phone = jobPhone;
            hintTarget = null;
            if (tray != null)
            {
                tray.Clear();
            }
        }

        public void EndJob()
        {
            active = null;
            phone = null;
            hintTarget = null;
            if (tray != null)
            {
                tray.Clear();
            }
        }

        /// <summary>Is this a part of the phone on the bench right now?</summary>
        public bool Owns(Interactable target)
        {
            return target != null && phone != null && target.TryGetComponent(out PhonePart part) && part.Device == phone.Device;
        }

        // ------------------------------------------------------------------ planning

        public RepairPlan Evaluate(Interactable target)
        {
            if (target == null || phone == null)
            {
                return default;
            }

            PhonePart part = target.GetComponent<PhonePart>();
            if (part == null || part.Device != phone.Device || part.Definition == null)
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
            if (direction == RepairDirection.Attach && part.State == PartState.Removed && !phone.IsOnVisibleSide(part))
            {
                string side = phone.SideOf(part) == Phone2D.Side.Front ? "front" : "back";
                return new RepairPlan(part, direction, false, false, $"{verb}: turn the phone to its {side} first [Space]");
            }

            if (toolBelt == null || toolBelt.CurrentKind != needed)
            {
                string tool = toolBelt != null ? toolBelt.Describe(needed) : needed.ToString();
                return new RepairPlan(part, direction, false, true, $"{verb}: needs {tool}");
            }

            RuleCheck rule = direction == RepairDirection.Detach ? Device.CanDetach(part) : Device.CanAttach(part);
            if (!rule.Allowed)
            {
                return new RepairPlan(part, direction, false, false, rule.Reason);
            }

            if (direction == RepairDirection.Detach && definition.DetachStyle == DetachStyle.Remove &&
                tray != null && !tray.HasRoomFor(part))
            {
                return new RepairPlan(part, direction, false, false, "No room left on the mat");
            }

            string prompt = $"Hold · {verb}";
            if (definition.Interaction == PartInteraction.Screw)
            {
                prompt += direction == RepairDirection.Detach ? "  (circle anticlockwise = faster)" : "  (circle clockwise = faster)";
            }
            else if (direction == RepairDirection.Attach && part.Condition == PartCondition.Broken)
            {
                prompt += "  (it's still broken!)";
            }

            return new RepairPlan(part, direction, true, false, prompt);
        }

        /// <summary>Hover prompt, cached until the tool, the phone state, the side or the part changes.</summary>
        public HoverHint DescribeHover(Interactable target)
        {
            if (target == null || phone == null)
            {
                hintTarget = null;
                return HoverHint.None;
            }

            int tool = toolBelt != null ? toolBelt.CurrentIndex : -1;
            int version = Device.StateVersion;
            PhonePart part = target.GetComponent<PhonePart>();
            bool busy = part != null && part.IsBusy;
            PartCondition condition = part != null ? part.Condition : PartCondition.Good;
            if (target == hintTarget && tool == hintTool && version == hintVersion && busy == hintBusy &&
                phone.Showing == hintSide && condition == hintCondition)
            {
                return hintCache;
            }

            RepairPlan plan = Evaluate(target);
            hintTarget = target;
            hintTool = tool;
            hintVersion = version;
            hintBusy = busy;
            hintSide = phone.Showing;
            hintCondition = condition;
            hintCache = new HoverHint(plan.Prompt, plan.Tone);
            return hintCache;
        }

        /// <summary>Prompt while an action runs: what to keep doing.</summary>
        public HoverHint DescribeActive()
        {
            if (active == null || active.Part == null)
            {
                return HoverHint.None;
            }

            if (active.Phase == Phase.Turning)
            {
                return new HoverHint(active.Part.ScrewDirection >= 0
                    ? "Circle anticlockwise to loosen faster"
                    : "Circle clockwise to tighten faster", InteractionTone.Ready);
            }

            PartDefinition definition = active.Part.Definition;
            string verb = active.Direction == RepairDirection.Detach ? definition.DetachVerb : definition.AttachVerb;
            return new HoverHint($"Hold · {verb}", InteractionTone.Ready);
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

                    return part.ScrewDirection >= 0 ? RepairDirection.Detach : RepairDirection.Attach;
            }
        }

        // ------------------------------------------------------------------ running

        /// <summary>
        /// Start working the part under the cursor. <paramref name="pointer"/> (screen pixels) is the
        /// centre the screwdriver circles around.
        /// </summary>
        public RepairBeginResult Begin(Interactable target, Vector2 pointer)
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
            active = new Active
            {
                Part = part,
                Direction = plan.Direction,
                StartLocalPosition = part.transform.localPosition,
                PressPointer = pointer,
            };

            if (part.Definition.Interaction == PartInteraction.Screw)
            {
                Tracker.Reset();
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
                _ => true, // a screw flying home; turning starts when it lands
            };
        }

        /// <summary>Button let go. Holds settle back; a screw stays as loose as it got.</summary>
        public void Release()
        {
            if (active == null)
            {
                return;
            }

            if (active.Phase == Phase.Holding && active.Part != null)
            {
                active.Part.transform.localPosition = active.StartLocalPosition;
            }

            active = null;
        }

        private bool TickHold(PhonePart part, float deltaTime)
        {
            PartDefinition definition = part.Definition;
            float seconds = active.Direction == RepairDirection.Detach ? definition.DetachSeconds : definition.AttachSeconds;
            active.Progress = Mathf.Min(1f, active.Progress + deltaTime * Speed / Mathf.Max(0.05f, seconds));

            float eased = 1f - (1f - active.Progress) * (1f - active.Progress);
            part.transform.localPosition = active.StartLocalPosition + new Vector3(0f, Mathf.Round(eased * strainPixels), 0f);
            if (active.Progress < 1f)
            {
                return true;
            }

            RepairDirection direction = active.Direction;
            Vector3 start = active.StartLocalPosition;
            active = null;
            CompleteHold(part, direction, start);
            return false;
        }

        private void CompleteHold(PhonePart part, RepairDirection direction, Vector3 startLocalPosition)
        {
            PartDefinition definition = part.Definition;
            if (definition.DetachStyle == DetachStyle.ReleaseInPlace)
            {
                part.transform.localPosition = startLocalPosition;
                bool unplug = direction == RepairDirection.Detach;
                Device.SetState(part, unplug ? PartState.Loose : PartState.Secured);
                Play(unplug ? SfxId.Unplug : SfxId.Click);
                Confirm(part);
                return;
            }

            if (direction == RepairDirection.Detach)
            {
                SendToTray(part);
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
            PartDefinition definition = part.Definition;
            float turns = Tracker.Step(pointer, active.PressPointer, deltaTime, part.ScrewDirection) * Speed;
            float totalTurns = Mathf.Max(0.25f, definition.ScrewTurns);
            part.Looseness = Mathf.Clamp01(part.Looseness + part.ScrewDirection * turns / totalTurns);
            if (part.State == PartState.Secured && part.Looseness > 0f)
            {
                Device.SetState(part, PartState.Loose);
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
                SendToTray(part);
                return false;
            }

            if (part.ScrewDirection < 0 && part.Looseness <= 0f)
            {
                active = null;
                part.ScrewDirection = 1;
                Device.SetState(part, PartState.Secured);
                ApplyScrewPose(part);
                Play(SfxId.Click, 1.1f);
                Confirm(part);
                return false;
            }

            return true;
        }

        private void ApplyScrewPose(PhonePart part)
        {
            float rise = Mathf.Round(part.Looseness * screwRisePixels);
            part.transform.localPosition = part.InstalledLocalPosition + new Vector3(0f, rise, 0f);
        }

        private void FlyScrewHome(PhonePart part)
        {
            active.Phase = Phase.Flying;
            part.ScrewDirection = -1;
            part.Looseness = 1f;
            FlyHome(part, PartState.Loose, new Vector3(0f, screwRisePixels, 0f), () =>
            {
                ApplyScrewPose(part);
                if (active != null && active.Part == part && active.Phase == Phase.Flying)
                {
                    active.Phase = Phase.Turning;
                    active.LastTickTurns = part.Looseness * part.Definition.ScrewTurns;
                    Tracker.Reset();
                }

                Play(SfxId.Tick, 0.8f);
            });
        }

        private void SendToTray(PhonePart part)
        {
            if (tray == null || flights == null || !tray.TryReserve(part, out PartsTray2D.Spot spot, out Vector3 local))
            {
                Refuse(part, "No room left on the mat");
                part.transform.localPosition = part.InstalledLocalPosition;
                return;
            }

            Device.SetState(part, PartState.Removed);
            PartView2D view = part.GetComponent<PartView2D>();
            if (view != null)
            {
                view.SetOrderBase(SortOrder.PartsFlying);
            }

            int restingOrder = spot.sortingOrder;
            flights.Fly(part, spot.Anchor, local, flightArc, flightSeconds, () =>
            {
                if (view != null)
                {
                    view.SetOrderBase(restingOrder);
                }

                Play(SfxId.Thunk, UnityEngine.Random.Range(0.92f, 1.08f), 0.7f);
            });
            Play(SfxId.Pop, UnityEngine.Random.Range(0.95f, 1.05f));
            Play(SfxId.Whoosh, 1f, 0.4f);
        }

        private void FlyHome(PhonePart part, PartState newState, Vector3 localOffset, Action onArrived)
        {
            Transform parent = part.InstalledParent;
            if (flights == null || parent == null)
            {
                return;
            }

            if (tray != null)
            {
                tray.Release(part);
            }

            Device.SetState(part, newState);
            PartView2D view = part.GetComponent<PartView2D>();
            if (view != null)
            {
                view.SetOrderBase(SortOrder.PartsFlying);
            }

            flights.Fly(part, parent, part.InstalledLocalPosition + localOffset, flightArc, flightSeconds, () =>
            {
                if (view != null)
                {
                    view.RestoreInstalledOrder();
                }

                onArrived?.Invoke();
            });
            Play(SfxId.Whoosh, 1.15f, 0.35f);
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
