using UnityEngine;

namespace PhoneRepair.PhoneFixer
{
    /// <summary>
    /// A quick squash-and-stretch wobble around the object's origin (keep the origin at its base).
    /// Used for petting the shop cat; handy for any prop that should react to a click.
    /// </summary>
    public sealed class SquashBounce : MonoBehaviour
    {
        [SerializeField, Range(0f, 0.5f)] private float amount = 0.2f;
        [SerializeField, Min(0.05f)] private float duration = 0.5f;

        private Vector3 rest;
        private float elapsed = -1f;

        private void Awake() => rest = transform.localScale;

        private void OnDisable()
        {
            if (elapsed >= 0f)
            {
                transform.localScale = rest;
                elapsed = -1f;
            }
        }

        public void Play() => elapsed = 0f;

        private void Update()
        {
            if (elapsed < 0f)
            {
                return;
            }

            elapsed += Time.deltaTime;
            float k = elapsed / duration;
            if (k >= 1f)
            {
                transform.localScale = rest;
                elapsed = -1f;
                return;
            }

            float s = Mathf.Sin(k * Mathf.PI * 3f) * (1f - k) * amount;
            transform.localScale = new Vector3(rest.x * (1f + s * 0.5f), rest.y * (1f - s), rest.z * (1f + s * 0.5f));
        }
    }
}
