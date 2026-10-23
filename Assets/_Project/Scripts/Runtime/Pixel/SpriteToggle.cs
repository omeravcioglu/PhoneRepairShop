using UnityEngine;

namespace PhoneRepair.Pixel
{
    /// <summary>A prop with two looks: the shop door (closed/open), the sign (OPEN/CLOSED), a lamp...</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SpriteToggle : MonoBehaviour
    {
        [SerializeField] private Sprite offSprite;
        [SerializeField] private Sprite onSprite;
        [SerializeField] private bool isOn;

        public bool IsOn => isOn;

        public void Set(bool on)
        {
            isOn = on;
            GetComponent<SpriteRenderer>().sprite = on ? onSprite : offSprite;
        }

        private void OnEnable() => Set(isOn);
    }
}
