using PhoneRepair.Pixel;
using UnityEngine;

namespace PhoneRepair.Shop
{
    /// <summary>The paper job ticket pinned at the top-left of the bench: who, which phone, what's wrong, the pay.</summary>
    [DisallowMultipleComponent]
    public sealed class TicketView : MonoBehaviour
    {
        [SerializeField] private PixelText title;
        [SerializeField] private PixelText body;

        public string Text => (title != null ? title.Text : string.Empty) + "\n" + (body != null ? body.Text : string.Empty);

        public void Show(JobTicket job)
        {
            gameObject.SetActive(job != null);
            if (job == null)
            {
                return;
            }

            CustomerDefinition c = job.Customer;
            string model = c.Phone != null ? c.Phone.ModelName : "Phone";
            if (title != null)
            {
                title.Text = $"{c.DisplayName.ToUpperInvariant()}  -  {model}";
            }

            if (body != null)
            {
                body.Text = $"{c.ProblemLabel}\nPays {Wallet.Format(c.Payment)}";
            }
        }
    }
}
