using System.Collections;
using PhoneRepair.Audio;
using PhoneRepair.Devices;
using PhoneRepair.Interaction;
using UnityEngine;

namespace PhoneRepair.Repair
{
    /// <summary>
    /// The box of spare parts on the bench. Click it while a broken part lies on the mat: the broken
    /// one hops into the box and a fresh one hops back out to the same spot. (Milestone 5 turns this
    /// into a real inventory with prices; for now spares are free and endless.)
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Interactable))]
    public sealed class SparesBox : MonoBehaviour, IClickable
    {
        [SerializeField] private PartsTray2D tray;
        [SerializeField] private PartFlights2D flights;
        [SerializeField] private BenchFeedback feedback;
        [Tooltip("Where parts dive in and pop out (the box's opening).")]
        [SerializeField] private Transform mouth;
        [SerializeField, Min(0.05f)] private float flightSeconds = 0.32f;

        public bool IsSwapping { get; private set; }
        public int Swaps { get; private set; }

        public HoverHint Hint => FindBroken() != null
            ? new HoverHint("Click · Swap the broken part for a new one", InteractionTone.Ready)
            : new HoverHint("Parts box: new parts for broken ones", InteractionTone.Neutral);

        public void Click()
        {
            if (IsSwapping)
            {
                return;
            }

            PhonePart broken = FindBroken();
            if (broken == null)
            {
                if (feedback != null)
                {
                    feedback.Refuse(GetComponent<Interactable>(), "Nothing broken on the mat to swap. Take the broken part out first!");
                }

                return;
            }

            StartCoroutine(Swap(broken));
        }

        /// <summary>A broken part lying on the mat, if any.</summary>
        public PhonePart FindBroken()
        {
            if (tray == null)
            {
                return null;
            }

            foreach (PhonePart part in tray.Occupants)
            {
                if (part.Condition == PartCondition.Broken && !part.IsBusy)
                {
                    return part;
                }
            }

            return null;
        }

        private IEnumerator Swap(PhonePart part)
        {
            IsSwapping = true;
            PartsTray2D.Spot spot = tray.SpotOf(part);
            Transform target = mouth != null ? mouth : transform;
            bool arrived = false;

            Play(SfxId.Whoosh, 0.9f, 0.4f);
            flights.Fly(part, target, PartsTray2D.RestingPosition(part), 14f, flightSeconds, () => arrived = true);
            while (!arrived)
            {
                yield return null;
            }

            SetVisible(part, false); // into the box...
            part.SetCondition(PartCondition.Good);
            Play(SfxId.Swap);
            yield return new WaitForSeconds(0.15f);
            SetVisible(part, true); // ...and a new one pops out

            arrived = false;
            if (spot != null && spot.Anchor != null)
            {
                flights.Fly(part, spot.Anchor, PartsTray2D.RestingPosition(part), 14f, flightSeconds, () => arrived = true);
                while (!arrived)
                {
                    yield return null;
                }
            }

            Swaps++;
            if (feedback != null)
            {
                string name = part.Definition != null ? part.Definition.DisplayName : part.name;
                feedback.Post($"Fresh {name} from the box!", InteractionTone.Ready);
                feedback.Confirm(part.GetComponent<Interactable>());
            }

            Play(SfxId.Thunk, 1.1f, 0.7f);
            IsSwapping = false;
        }

        private static void SetVisible(PhonePart part, bool visible)
        {
            foreach (SpriteRenderer sr in part.GetComponentsInChildren<SpriteRenderer>(true))
            {
                sr.enabled = visible;
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
