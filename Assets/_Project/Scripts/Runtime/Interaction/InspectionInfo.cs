namespace PhoneRepair.Interaction
{
    /// <summary>What the UI shows about something the player points at or selects.</summary>
    public readonly struct InspectionInfo
    {
        public string Title { get; }
        public string Subtitle { get; }
        public string Body { get; }

        /// <summary>Short live state, e.g. "Tight", "Unplugged", "On the mat". Empty when not relevant.</summary>
        public string Status { get; }

        public InspectionInfo(string title, string subtitle = "", string body = "", string status = "")
        {
            Title = title ?? string.Empty;
            Subtitle = subtitle ?? string.Empty;
            Body = body ?? string.Empty;
            Status = status ?? string.Empty;
        }
    }

    /// <summary>
    /// Implemented by domain components (e.g. PhonePart) that live next to an Interactable and
    /// know what it actually is. Keeps the interaction layer free of game-specific types.
    /// </summary>
    public interface IInspectionInfoProvider
    {
        InspectionInfo GetInspectionInfo();
    }

    /// <summary>How an action on the hovered thing would go: drives highlight colour and prompt colour.</summary>
    public enum InteractionTone
    {
        /// <summary>Nothing to do here with the current tool (inspect only).</summary>
        Neutral = 0,

        /// <summary>Pressing will do something.</summary>
        Ready = 1,

        /// <summary>The right tool, but the rules say "not yet".</summary>
        Blocked = 2,
    }

    /// <summary>One-line prompt for the hovered thing, e.g. "Hold · Unscrew" or "Needs: Screwdriver [2]".</summary>
    public readonly struct HoverHint
    {
        public HoverHint(string text, InteractionTone tone)
        {
            Text = text ?? string.Empty;
            Tone = tone;
        }

        public string Text { get; }
        public InteractionTone Tone { get; }

        public static HoverHint None => new(string.Empty, InteractionTone.Neutral);
    }
}
