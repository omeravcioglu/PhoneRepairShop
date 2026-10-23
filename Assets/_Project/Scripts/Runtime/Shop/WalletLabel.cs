using PhoneRepair.Pixel;
using UnityEngine;

namespace PhoneRepair.Shop
{
    /// <summary>The register's display: counts up to the wallet total (cha-ching).</summary>
    [DisallowMultipleComponent]
    public sealed class WalletLabel : MonoBehaviour
    {
        [SerializeField] private Wallet wallet;
        [SerializeField] private PixelText label;
        [SerializeField, Min(1f)] private float countSeconds = 0.8f;

        private float shown;
        private float speed;

        private void Update()
        {
            if (wallet == null || label == null)
            {
                return;
            }

            float target = wallet.Yen;
            if (!Mathf.Approximately(shown, target))
            {
                if (speed <= 0f)
                {
                    speed = Mathf.Max(1f, Mathf.Abs(target - shown) / countSeconds);
                }

                shown = Mathf.MoveTowards(shown, target, speed * Time.deltaTime);
            }
            else
            {
                speed = 0f;
            }

            string text = Wallet.Format(Mathf.RoundToInt(shown));
            if (label.Text != text)
            {
                label.Text = text;
            }
        }
    }
}
