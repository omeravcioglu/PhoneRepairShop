using PhoneRepair.Pixel;
using PhoneRepair.Repair;
using UnityEngine;

namespace PhoneRepair.Shop
{
    /// <summary>
    /// The customer's face peeking in at the top of the bench while you work on their phone. Winces
    /// at a "nope", beams when something comes off or clicks home, then settles back.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CustomerPortrait : MonoBehaviour
    {
        public enum Mood
        {
            Neutral = 0,
            Happy = 1,
            Worried = 2,
        }

        [SerializeField] private SpriteRenderer face;
        [SerializeField] private PixelText nameLabel;
        [SerializeField] private BenchFeedback feedback;
        [SerializeField, Min(0.1f)] private float reactSeconds = 1.1f;

        private CustomerDefinition customer;
        private float moodUntil;

        public Mood CurrentMood { get; private set; }

        public void Show(CustomerDefinition who)
        {
            customer = who;
            gameObject.SetActive(who != null);
            if (nameLabel != null && who != null)
            {
                nameLabel.Text = $"{who.DisplayName} is watching";
            }

            SetMood(Mood.Neutral);
        }

        public void React(Mood mood)
        {
            SetMood(mood);
            moodUntil = Time.time + reactSeconds;
        }

        private void OnEnable()
        {
            if (feedback != null)
            {
                feedback.Reacted += OnReacted;
            }
        }

        private void OnDisable()
        {
            if (feedback != null)
            {
                feedback.Reacted -= OnReacted;
            }
        }

        private void OnReacted(BenchFeedback.Reaction reaction) =>
            React(reaction == BenchFeedback.Reaction.Worried ? Mood.Worried : Mood.Happy);

        private void Update()
        {
            if (CurrentMood != Mood.Neutral && Time.time >= moodUntil)
            {
                SetMood(Mood.Neutral);
            }
        }

        private void SetMood(Mood mood)
        {
            CurrentMood = mood;
            if (customer == null || face == null)
            {
                return;
            }

            Sprite sprite = mood switch
            {
                Mood.Happy => customer.FaceHappy,
                Mood.Worried => customer.FaceWorried,
                _ => customer.FaceNeutral,
            };

            face.sprite = sprite != null ? sprite : customer.FaceNeutral;
        }
    }
}
