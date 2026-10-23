using System.Linq;
using PhoneRepair.Devices;
using PhoneRepair.Repair;

namespace PhoneRepair.Shop
{
    public enum JobResult
    {
        Open = 0,
        Fixed = 1,
        StillBroken = 2,
        Declined = 3,
    }

    /// <summary>One repair in progress: who, which phone (the instance on the bench), what's broken, the price.</summary>
    public sealed class JobTicket
    {
        public JobTicket(CustomerDefinition customer, Phone2D phone)
        {
            Customer = customer;
            Phone = phone;
        }

        public CustomerDefinition Customer { get; }
        public Phone2D Phone { get; }
        public JobResult Result { get; set; } = JobResult.Open;

        public PhoneDevice Device => Phone != null ? Phone.Device : null;

        /// <summary>The power test: everything back in and fastened, and nothing broken left inside.</summary>
        public bool PowerTest()
        {
            PhoneDevice device = Device;
            return device != null && device.IsFullyAssembled() && device.Parts.All(p => p.Condition == PartCondition.Good);
        }
    }
}
