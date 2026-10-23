using System.Collections;
using PhoneRepair.Devices;
using UnityEngine;

namespace PhoneRepair.Repair
{
    /// <summary>
    /// The phone on the repair mat, drawn from above: a front picture (screen) and a back picture
    /// (cover, screws, insides). One side shows at a time; Space flips it with a quick squash.
    /// Parts lying on the mat are not under either side, so they stay visible whatever faces up.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PhoneDevice))]
    public sealed class Phone2D : MonoBehaviour
    {
        public enum Side
        {
            Front = 0,
            Back = 1,
        }

        [SerializeField] private Transform front;
        [SerializeField] private Transform back;
        [SerializeField] private Side startSide = Side.Front;
        [SerializeField, Min(0.02f)] private float flipSeconds = 0.18f;

        private PhoneDevice device;
        private bool initialised;

        public PhoneDevice Device => device != null ? device : device = GetComponent<PhoneDevice>();
        public Side Showing { get; private set; }
        public bool IsFlipping { get; private set; }
        public Transform Front => front;
        public Transform Back => back;

        /// <summary>Which side a part belongs to (the side its installed parent hangs under).</summary>
        public Side SideOf(PhonePart part)
        {
            Transform t = part.InstalledParent;
            while (t != null && t != transform)
            {
                if (t == front)
                {
                    return Side.Front;
                }

                if (t == back)
                {
                    return Side.Back;
                }

                t = t.parent;
            }

            return Side.Back;
        }

        public bool IsOnVisibleSide(PhonePart part) => SideOf(part) == Showing;

        public void Show(Side side)
        {
            initialised = true;
            Showing = side;
            if (front != null)
            {
                front.gameObject.SetActive(side == Side.Front);
            }

            if (back != null)
            {
                back.gameObject.SetActive(side == Side.Back);
            }
        }

        public void Flip()
        {
            if (!IsFlipping && isActiveAndEnabled)
            {
                StartCoroutine(FlipRoutine());
            }
        }

        private void Awake()
        {
            if (!initialised)
            {
                Show(startSide);
            }
        }

        private IEnumerator FlipRoutine()
        {
            IsFlipping = true;
            Vector3 scale = transform.localScale;
            float half = flipSeconds * 0.5f;
            for (float t = 0f; t < half; t += Time.deltaTime)
            {
                transform.localScale = new Vector3(scale.x * Mathf.Max(0.05f, 1f - t / half), scale.y, scale.z); // never 0: colliders hate it
                yield return null;
            }

            Show(Showing == Side.Front ? Side.Back : Side.Front);
            for (float t = 0f; t < half; t += Time.deltaTime)
            {
                transform.localScale = new Vector3(scale.x * Mathf.Max(0.05f, t / half), scale.y, scale.z);
                yield return null;
            }

            transform.localScale = scale;
            IsFlipping = false;
        }
    }
}
