using System.Collections;
using PhoneRepair.Pixel;
using UnityEngine;

namespace PhoneRepair.Shop
{
    /// <summary>
    /// A customer in the shop. Appears in the doorway (small, far away), hops up to the counter
    /// (full size, close), talks with a two-frame mouth, blinks, breathes, and reacts. On the way
    /// out it hops back to the door. Very little motion on purpose: a few frames and a hop.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class CustomerActor : MonoBehaviour
    {
        public enum Pose
        {
            Idle = 0,
            Talk = 1,
            Happy = 2,
            Sad = 3,
        }

        [SerializeField] private Transform doorSpot;
        [SerializeField] private Transform counterSpot;
        [Tooltip("Size in the doorway (far away). 0.5 keeps the pixels whole.")]
        [SerializeField, Range(0.25f, 1f)] private float doorScale = 0.5f;
        [SerializeField, Min(0.1f)] private float hopSeconds = 0.32f;
        [SerializeField, Min(0)] private int hopsToCounter = 2;
        [SerializeField, Min(0f)] private float hopHeight = 10f;
        [SerializeField] private int doorOrder = SortOrder.CustomerAtDoor;
        [SerializeField] private int counterOrder = SortOrder.Customer;
        [SerializeField, Min(1f)] private float talkFramesPerSecond = 8f;
        [SerializeField, Min(0.5f)] private float blinkEvery = 2.8f;

        private SpriteRenderer spriteRenderer;
        private CustomerDefinition customer;
        private Pose pose;
        private float animationTime;
        private float bob;
        private bool atCounter;

        public CustomerDefinition Customer => customer;
        public Pose CurrentPose => pose;
        public bool IsMoving { get; private set; }
        public bool IsAtCounter => atCounter && !IsMoving;
        public bool IsVisible => Renderer.enabled;

        private SpriteRenderer Renderer => spriteRenderer != null ? spriteRenderer : spriteRenderer = GetComponent<SpriteRenderer>();

        /// <summary>Stands the customer in the open doorway, small.</summary>
        public void AppearAtDoor(CustomerDefinition who)
        {
            customer = who;
            pose = Pose.Idle;
            atCounter = false;
            IsMoving = false;
            Renderer.enabled = true;
            Renderer.sortingOrder = doorOrder;
            transform.position = PixelGrid.Snap(doorSpot != null ? doorSpot.position : transform.position);
            transform.localScale = Vector3.one * doorScale;
            ApplyFrame();
        }

        public IEnumerator WalkToCounter()
        {
            yield return Hop(doorSpot, counterSpot, doorScale, 1f, Mathf.Max(1, hopsToCounter));
            Renderer.sortingOrder = counterOrder;
            atCounter = true;
        }

        public IEnumerator WalkOut()
        {
            atCounter = false;
            pose = Pose.Idle;
            yield return Hop(counterSpot, doorSpot, 1f, doorScale, Mathf.Max(1, hopsToCounter));
            Hide();
        }

        public void Hide()
        {
            Renderer.enabled = false;
            atCounter = false;
            customer = null;
        }

        public void SetPose(Pose newPose)
        {
            pose = newPose;
            animationTime = 0f;
            ApplyFrame();
        }

        private IEnumerator Hop(Transform from, Transform to, float fromScale, float toScale, int hops)
        {
            IsMoving = true;
            Vector3 start = from != null ? from.position : transform.position;
            Vector3 end = to != null ? to.position : transform.position;
            for (int h = 0; h < hops; h++)
            {
                float a = h / (float)hops;
                float b = (h + 1) / (float)hops;
                if (h == hops / 2)
                {
                    Renderer.sortingOrder = toScale > fromScale ? counterOrder : doorOrder; // pass the door frame halfway
                }

                for (float t = 0f; t < hopSeconds; t += Time.deltaTime)
                {
                    float u = Mathf.Lerp(a, b, t / hopSeconds);
                    Vector3 p = Vector3.Lerp(start, end, Ease.InOutCubic(u));
                    p.y += Ease.Arc(t / hopSeconds) * hopHeight;
                    transform.position = PixelGrid.Snap(p);
                    transform.localScale = Vector3.one * Mathf.Lerp(fromScale, toScale, u);
                    yield return null;
                }
            }

            transform.position = PixelGrid.Snap(end);
            transform.localScale = Vector3.one * toScale;
            IsMoving = false;
        }

        private void Update()
        {
            if (customer == null || !Renderer.enabled)
            {
                return;
            }

            animationTime += Time.deltaTime;
            ApplyFrame();

            if (IsAtCounter && counterSpot != null)
            {
                // breathe: one pixel up and down
                bob = pose == Pose.Idle && Mathf.FloorToInt(Time.time * 1.6f) % 2 == 0 ? 1f : 0f;
                transform.position = PixelGrid.Snap(counterSpot.position + new Vector3(0f, bob, 0f));
            }
        }

        private void ApplyFrame()
        {
            if (customer == null)
            {
                return;
            }

            Sprite sprite = pose switch
            {
                Pose.Talk => Pick(customer.Talk, Mathf.FloorToInt(animationTime * talkFramesPerSecond)),
                Pose.Happy => customer.Happy,
                Pose.Sad => customer.Sad,
                _ => Blinking() ? Pick(customer.Idle, 1) : Pick(customer.Idle, 0),
            };

            if (sprite == null)
            {
                sprite = Pick(customer.Idle, 0);
            }

            if (sprite != null)
            {
                Renderer.sprite = sprite;
            }
        }

        private bool Blinking()
        {
            float phase = (Time.time + customer.CustomerId.Length * 0.37f) % blinkEvery;
            return phase > blinkEvery - 0.14f;
        }

        private static Sprite Pick(Sprite[] frames, int index)
        {
            if (frames == null || frames.Length == 0)
            {
                return null;
            }

            return frames[Mathf.Abs(index) % frames.Length];
        }
    }
}
