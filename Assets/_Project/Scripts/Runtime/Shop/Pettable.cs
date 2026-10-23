using PhoneRepair.Audio;
using PhoneRepair.Interaction;
using PhoneRepair.Pixel;
using UnityEngine;

namespace PhoneRepair.Shop
{
    /// <summary>Something goofy to poke: it makes its noise and hops (Denchi, the shop cat).</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Interactable))]
    public sealed class Pettable : MonoBehaviour, IClickable
    {
        [SerializeField] private SfxPlayer sfx;
        [SerializeField] private SfxId sound = SfxId.Meow;
        [SerializeField] private string hint = "Pet Denchi";
        [SerializeField, Min(0.05f)] private float hopSeconds = 0.18f;
        [SerializeField, Min(0f)] private float hopHeight = 4f;

        private Vector3 rest;
        private float hopTime = -1f;

        public int Pets { get; private set; }

        public HoverHint Hint => new(hint, InteractionTone.Ready);

        public void Click()
        {
            Pets++;
            if (sfx != null)
            {
                sfx.Play(sound, Random.Range(0.9f, 1.15f));
            }

            if (hopTime < 0f)
            {
                rest = transform.localPosition;
            }

            hopTime = 0f;
        }

        private void Update()
        {
            if (hopTime < 0f)
            {
                return;
            }

            hopTime += Time.deltaTime;
            if (hopTime >= hopSeconds)
            {
                transform.localPosition = rest;
                hopTime = -1f;
                return;
            }

            transform.localPosition = rest + new Vector3(0f, Mathf.Round(Ease.Arc(hopTime / hopSeconds) * hopHeight), 0f);
        }
    }
}
