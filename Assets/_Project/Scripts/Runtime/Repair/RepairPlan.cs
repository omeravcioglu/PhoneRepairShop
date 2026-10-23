using PhoneRepair.Devices;
using PhoneRepair.Interaction;

namespace PhoneRepair.Repair
{
    public enum RepairDirection
    {
        None = 0,
        Detach = 1,
        Attach = 2,
    }

    public enum RepairBeginResult
    {
        /// <summary>Nothing to do here with this tool (the press is just a look).</summary>
        NoAction = 0,

        /// <summary>The right tool, but the rules said no; feedback was shown.</summary>
        Refused = 1,

        /// <summary>An action is running; keep calling Hold while the button is down.</summary>
        Started = 2,
    }

    /// <summary>What pressing on a part with the current tool would do.</summary>
    public readonly struct RepairPlan
    {
        public RepairPlan(PhonePart part, RepairDirection direction, bool allowed, bool wrongTool, string prompt)
        {
            Part = part;
            Direction = direction;
            Allowed = allowed;
            WrongTool = wrongTool;
            Prompt = prompt ?? string.Empty;
        }

        public PhonePart Part { get; }
        public RepairDirection Direction { get; }
        public bool Allowed { get; }
        public bool WrongTool { get; }
        public string Prompt { get; }

        public InteractionTone Tone => Direction == RepairDirection.None || WrongTool
            ? InteractionTone.Neutral
            : Allowed ? InteractionTone.Ready : InteractionTone.Blocked;
    }
}
