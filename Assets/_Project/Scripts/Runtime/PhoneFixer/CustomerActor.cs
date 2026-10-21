using UnityEngine;

namespace PhoneRepair.PhoneFixer
{
    /// <summary>
    /// Keeps a customer alive in the shop view: a looping mood clip (Idle, Talk, Impatient...) from a
    /// random start, plus an occasional one-shot gesture (Celebrate, Confused...). States are the clip
    /// names in AC_PF_Customer, shared by every CHR_PF_* prefab.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class CustomerActor : MonoBehaviour
    {
        [SerializeField] private string loopState = "Idle";
        [SerializeField] private string[] gestures = System.Array.Empty<string>();
        [SerializeField] private Vector2 gestureEverySeconds = new(6f, 11f);
        [SerializeField, Min(0.1f)] private float gestureSeconds = 1.4f;
        [Tooltip("Normalized start time of the loop. Negative = random, so neighbours don't bob in sync.")]
        [SerializeField] private float startOffset = -1f;

        private Animator animator;
        private float nextGesture;
        private float backToLoop = -1f;

        public string LoopState => loopState;

        private void Awake() => animator = GetComponent<Animator>();

        private void OnEnable()
        {
            animator.Play(loopState, 0, startOffset >= 0f ? startOffset : Random.value);
            nextGesture = Time.time + Random.Range(gestureEverySeconds.x, gestureEverySeconds.y);
            backToLoop = -1f;
        }

        private void Update()
        {
            if (backToLoop > 0f && Time.time >= backToLoop)
            {
                animator.CrossFadeInFixedTime(loopState, 0.25f);
                backToLoop = -1f;
            }

            if (gestures.Length > 0 && backToLoop < 0f && Time.time >= nextGesture)
            {
                animator.CrossFadeInFixedTime(gestures[Random.Range(0, gestures.Length)], 0.2f);
                backToLoop = Time.time + gestureSeconds;
                nextGesture = Time.time + Random.Range(gestureEverySeconds.x, gestureEverySeconds.y);
            }
        }
    }
}
