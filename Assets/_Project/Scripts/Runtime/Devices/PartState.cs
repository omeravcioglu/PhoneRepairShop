namespace PhoneRepair.Devices
{
    /// <summary>Where one part of one phone is in the repair (runtime state).</summary>
    public enum PartState
    {
        /// <summary>In place and fastened: screw tight, connector plugged in, cover snapped on.</summary>
        Secured = 0,

        /// <summary>In place but not fastened: a half-unscrewed screw, an unplugged connector.</summary>
        Loose = 1,

        /// <summary>Off the phone: in the screw tray or on the mat.</summary>
        Removed = 2,
    }

    /// <summary>How a kind of part comes off. Lives on <see cref="PartDefinition"/>.</summary>
    public enum DetachStyle
    {
        /// <summary>Never comes off (the frame everything attaches to).</summary>
        Fixed = 0,

        /// <summary>Comes off the phone and goes to the screw tray or the mat.</summary>
        Remove = 10,

        /// <summary>Unplugs but stays attached to its owner (flex connectors).</summary>
        ReleaseInPlace = 20,
    }

    /// <summary>What the player physically does to work a part.</summary>
    public enum PartInteraction
    {
        /// <summary>Hold the button for a moment: pry, lift, unplug, put back.</summary>
        Hold = 0,

        /// <summary>Screwdriver minigame: hold, and turn the mouse around the screw to go faster.</summary>
        Screw = 10,
    }

    /// <summary>
    /// The kind of tool a part needs. Concrete tools (basic or upgraded screwdriver...) are
    /// ToolDefinition assets with one of these kinds.
    /// </summary>
    public enum ToolKind
    {
        Hands = 0,
        Screwdriver = 10,
        Spudger = 20,
        SuctionCup = 30,
    }
}
