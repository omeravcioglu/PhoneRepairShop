using System;
using System.Collections;
using System.Collections.Generic;
using PhoneRepair.Audio;
using PhoneRepair.Controls;
using PhoneRepair.Pixel;
using UnityEngine;

namespace PhoneRepair.Shop
{
    public enum ShopPhase
    {
        Title = 0,
        DayStart = 1,
        Entering = 2,
        Talking = 3,
        Choosing = 4,
        Repairing = 5,
        Returning = 6,
        Leaving = 7,
        DayEnd = 8,
    }

    /// <summary>
    /// Runs the shop. Title card, then each day: customer after customer comes through the door,
    /// hops up to the counter and tells you their (goofy) problem; you take the job or not; the
    /// repair happens at the bench; the phone goes back, gets switched on, and they pay or grumble;
    /// they leave. After the day's last customer comes the summary. The loop reads top to bottom
    /// in <see cref="Visit"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShopDirector : MonoBehaviour
    {
        [Header("Content")]
        [SerializeField] private ShopCalendar calendar;
        [SerializeField, Min(1)] private int startDay = 1;
        [SerializeField] private bool showTitle = true;

        [Header("Shop")]
        [SerializeField] private GameInput input;
        [SerializeField] private CameraRig2D rig;
        [SerializeField] private CustomerActor customer;
        [SerializeField] private SpeechBubble bubble;
        [SerializeField] private ChoicePrompt choices;
        [SerializeField] private SpriteToggle door;
        [SerializeField] private SpriteToggle sign;
        [Tooltip("The customer's phone lying on the counter while it changes hands.")]
        [SerializeField] private SpriteRenderer counterPhone;
        [SerializeField] private Wallet wallet;
        [SerializeField] private PixelText dayLabel;
        [SerializeField] private PanelView titleCard;
        [SerializeField] private PanelView daySummary;
        [SerializeField] private SfxPlayer sfx;

        [Header("Bench")]
        [SerializeField] private BenchController bench;

        [Header("Pacing")]
        [SerializeField, Min(0f)] private float doorPause = 0.3f;
        [SerializeField, Min(0f)] private float handOverPause = 0.35f;

        private readonly List<string> tipsToday = new();

        public ShopPhase Phase { get; private set; } = ShopPhase.Title;
        public int Day { get; private set; }
        public CustomerDefinition CurrentCustomer { get; private set; }
        public JobTicket CurrentJob => bench != null ? bench.Job : null;
        public JobResult LastResult { get; private set; }
        public int CustomersToday { get; private set; }
        public int FixedToday { get; private set; }
        public int EarnedToday { get; private set; }
        public IReadOnlyList<string> TipsToday => tipsToday;

        public event Action<ShopPhase> PhaseChanged;

        private IEnumerator Start()
        {
            rig.SnapTo(CameraRig2D.View.Shop);
            customer.Hide();
            bubble.Hide();
            choices.Close();
            door.Set(false);
            sign.Set(false);
            counterPhone.enabled = false;
            daySummary.Close();

            if (showTitle)
            {
                SetPhase(ShopPhase.Title);
                titleCard.Open("A tiny phone repair shop.\nFix what they bring, get paid,\nand pet the cat.", "Open the shop  [Enter]");
                while (!titleCard.Confirmed)
                {
                    yield return null;
                }

                Play(SfxId.Paper);
            }

            titleCard.Close();
            for (int day = startDay; ; day++)
            {
                yield return RunDay(day);
            }
        }

        private IEnumerator RunDay(int day)
        {
            Day = day;
            CustomersToday = 0;
            FixedToday = 0;
            EarnedToday = 0;
            tipsToday.Clear();
            dayLabel.Text = $"DAY {day}";
            sign.Set(true);
            SetPhase(ShopPhase.DayStart);
            yield return new WaitForSeconds(0.4f);

            IReadOnlyList<CustomerDefinition> customers = calendar != null ? calendar.CustomersFor(day) : Array.Empty<CustomerDefinition>();
            foreach (CustomerDefinition c in customers)
            {
                if (c != null)
                {
                    yield return Visit(c);
                }
            }

            SetPhase(ShopPhase.DayEnd);
            sign.Set(false);
            daySummary.Open(SummaryText(), "Next day  [Enter]");
            while (!daySummary.Confirmed)
            {
                yield return null;
            }

            daySummary.Close();
            Play(SfxId.Paper);
        }

        /// <summary>One customer, from the door chime to the door chime.</summary>
        private IEnumerator Visit(CustomerDefinition c)
        {
            CurrentCustomer = c;
            CustomersToday++;
            LastResult = JobResult.Open;

            SetPhase(ShopPhase.Entering);
            door.Set(true);
            Play(SfxId.DoorChime);
            customer.AppearAtDoor(c);
            yield return new WaitForSeconds(doorPause);
            yield return customer.WalkToCounter();
            door.Set(false);

            SetPhase(ShopPhase.Talking);
            yield return Say(c.Greeting);
            yield return Say(c.Problem);

            SetPhase(ShopPhase.Choosing);
            choices.Open("[1] Leave it with me!", "[2] Sorry, can't");
            while (choices.Choice < 0)
            {
                yield return null;
            }

            int choice = choices.Choice;
            choices.Close();
            Play(SfxId.Paper);
            if (choice == 1)
            {
                LastResult = JobResult.Declined;
                SetPhase(ShopPhase.Talking);
                yield return Say(c.Declined, CustomerActor.Pose.Sad, CustomerActor.Pose.Sad);
                yield return Leave();
                yield break;
            }

            yield return Say(c.Accepted);

            // the phone changes hands, then the camera looks down at the bench
            counterPhone.enabled = true;
            Play(SfxId.Thunk);
            yield return new WaitForSeconds(handOverPause);
            bubble.Hide();
            counterPhone.enabled = false;
            if (bench.BeginJob(c) == null)
            {
                yield return Leave();
                yield break;
            }

            SetPhase(ShopPhase.Repairing);
            yield return rig.SlideTo(CameraRig2D.View.Bench);
            while (!bench.HandedBack)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.25f);
            bool fixedIt = bench.Job.PowerTest();
            bench.Job.Result = fixedIt ? JobResult.Fixed : JobResult.StillBroken;
            LastResult = bench.Job.Result;
            yield return rig.SlideTo(CameraRig2D.View.Shop);
            bench.EndJob();

            SetPhase(ShopPhase.Returning);
            counterPhone.enabled = true;
            if (fixedIt)
            {
                FixedToday++;
                Play(SfxId.Happy);
                yield return Say(c.FixedLine, CustomerActor.Pose.Happy, CustomerActor.Pose.Happy);
                Pay(c);
                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                Play(SfxId.Sad);
                yield return Say(c.StillBrokenLine, CustomerActor.Pose.Sad, CustomerActor.Pose.Sad);
            }

            counterPhone.enabled = false;
            yield return Leave();
        }

        private IEnumerator Leave()
        {
            SetPhase(ShopPhase.Leaving);
            bubble.Hide();
            door.Set(true);
            yield return customer.WalkOut();
            Play(SfxId.DoorChime, 0.9f);
            door.Set(false);
            CurrentCustomer = null;
            yield return new WaitForSeconds(0.3f);
        }

        /// <summary>Types a line; a click (or Enter/Space) finishes it, another click continues.</summary>
        private IEnumerator Say(string line, CustomerActor.Pose whileTalking = CustomerActor.Pose.Talk,
            CustomerActor.Pose after = CustomerActor.Pose.Idle)
        {
            customer.SetPose(whileTalking);
            bubble.Show(line, CurrentCustomer != null ? CurrentCustomer.VoicePitch : 1f);
            yield return null; // the click that started this line must not skip it

            while (bubble.IsTyping)
            {
                if (AdvancePressed())
                {
                    bubble.FinishTyping();
                    yield return null;
                    break;
                }

                yield return null;
            }

            customer.SetPose(after);
            while (!AdvancePressed())
            {
                yield return null;
            }

            yield return null;
        }

        private bool AdvancePressed() =>
            input != null && (input.PrimaryPressedThisFrame || input.ConfirmPressedThisFrame || input.FlipPressedThisFrame);

        private void Pay(CustomerDefinition c)
        {
            wallet.Add(c.Payment);
            EarnedToday += c.Payment;
            Play(SfxId.Coin);
            if (!string.IsNullOrWhiteSpace(c.Tip))
            {
                tipsToday.Add(c.Tip);
            }
        }

        private string SummaryText()
        {
            string tips = tipsToday.Count > 0 ? string.Join(", ", tipsToday) : "none";
            return $"DAY {Day} - CLOSED\n\nRepairs: {FixedToday} of {CustomersToday}\nEarned: {Wallet.Format(EarnedToday)}\nTips: {tips}";
        }

        private void SetPhase(ShopPhase phase)
        {
            Phase = phase;
            PhaseChanged?.Invoke(phase);
        }

        private void Play(SfxId id, float pitch = 1f, float volume = 1f)
        {
            if (sfx != null)
            {
                sfx.Play(id, pitch, volume);
            }
        }
    }
}
