using PhoneRepair.Audio;
using PhoneRepair.Controls;
using PhoneRepair.Devices;
using PhoneRepair.Interaction;
using PhoneRepair.Repair;
using UnityEngine;

namespace PhoneRepair.Shop
{
    /// <summary>
    /// The bench while a job is on it: puts the customer's phone on the mat (with its broken part),
    /// tool keys 1-4, Space to turn the phone over, Enter or the bell to hand it back. An unfinished
    /// phone is not handed back.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BenchController : MonoBehaviour
    {
        [SerializeField] private GameInput input;
        [SerializeField] private ToolBelt toolBelt;
        [SerializeField] private BenchRepair repair;
        [SerializeField] private BenchFeedback feedback;
        [SerializeField] private PointerRouter router;
        [SerializeField] private CameraRig2D rig;
        [SerializeField] private PartFlights2D flights;
        [SerializeField] private SparesBox spares;
        [SerializeField] private TicketView ticket;
        [SerializeField] private CustomerPortrait portrait;
        [SerializeField] private PixelButton flipButton;
        [SerializeField] private PixelButton doneButton;
        [Tooltip("The phone's centre on the mat.")]
        [SerializeField] private Transform phoneAnchor;

        public JobTicket Job { get; private set; }
        public bool HandedBack { get; private set; }
        public Phone2D Phone => Job?.Phone;

        /// <summary>Lays the customer's phone on the mat, face up, with their fault in it.</summary>
        public JobTicket BeginJob(CustomerDefinition customer)
        {
            EndJob();
            GameObject prefab = customer.Phone != null ? customer.Phone.Prefab : null;
            if (prefab == null)
            {
                Debug.LogError($"[PhoneRepair] {customer.name}: the phone model has no 2D prefab. Run Tools > Phone Repair > Run Setup.", customer);
                return null;
            }

            GameObject instance = Instantiate(prefab, phoneAnchor);
            instance.name = prefab.name;
            instance.transform.localPosition = Vector3.zero;
            var phone = instance.GetComponent<Phone2D>();
            phone.Show(Phone2D.Side.Front);
            if (phone.Device.TryGetPart(customer.BrokenSlot, out PhonePart broken))
            {
                broken.SetCondition(PartCondition.Broken);
            }
            else
            {
                Debug.LogWarning($"[PhoneRepair] {customer.name}: no part '{customer.BrokenSlot}' to break.", customer);
            }

            Job = new JobTicket(customer, phone);
            HandedBack = false;
            repair.BeginJob(phone);
            if (toolBelt != null)
            {
                toolBelt.Equip(0);
            }

            if (ticket != null)
            {
                ticket.Show(Job);
            }

            if (portrait != null)
            {
                portrait.Show(customer);
            }

            return Job;
        }

        /// <summary>Clears the bench (the phone goes back to its owner).</summary>
        public void EndJob()
        {
            if (repair != null)
            {
                repair.EndJob();
            }

            if (Job != null && Job.Phone != null)
            {
                foreach (PhonePart part in Job.Device.Parts)
                {
                    if (part != null && !part.transform.IsChildOf(Job.Phone.transform))
                    {
                        Destroy(part.gameObject); // still lying on the mat
                    }
                }

                Destroy(Job.Phone.gameObject);
            }

            Job = null;
            HandedBack = false;
            if (ticket != null)
            {
                ticket.Show(null);
            }

            if (portrait != null)
            {
                portrait.Show(null);
            }
        }

        public void TryHandBack()
        {
            if (Job == null || HandedBack || Busy)
            {
                return;
            }

            if (!Job.Device.IsFullyAssembled())
            {
                feedback.Refuse(doneButton != null ? doneButton.GetComponent<Interactable>() : null,
                    "It's still in pieces! Put it all back together first.");
                return;
            }

            HandedBack = true;
            feedback.Play(SfxId.Bell);
        }

        public void Flip()
        {
            if (Job == null || Busy)
            {
                return;
            }

            Job.Phone.Flip();
            feedback.Play(SfxId.Paper, 1.2f, 0.6f);
        }

        private bool Busy => (repair != null && repair.IsBusy) || (router != null && router.IsHolding) ||
                             (spares != null && spares.IsSwapping) || (flights != null && flights.AnyFlying) ||
                             (Job != null && Job.Phone != null && Job.Phone.IsFlipping);

        private void Awake()
        {
            if (flipButton != null)
            {
                flipButton.Clicked += Flip;
            }

            if (doneButton != null)
            {
                doneButton.Clicked += TryHandBack;
            }
        }

        private void Update()
        {
            if (Job == null || input == null || rig == null || rig.Current != CameraRig2D.View.Bench || rig.IsSliding)
            {
                return;
            }

            int key = input.ToolSlotPressedThisFrame;
            if (key >= 0 && toolBelt != null && key < toolBelt.Tools.Count && !(router != null && router.IsHolding))
            {
                toolBelt.Equip(key);
            }

            if (input.FlipPressedThisFrame)
            {
                Flip();
            }

            if (input.ConfirmPressedThisFrame)
            {
                TryHandBack();
            }
        }
    }
}
