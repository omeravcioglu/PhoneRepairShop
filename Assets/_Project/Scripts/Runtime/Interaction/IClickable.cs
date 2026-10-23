namespace PhoneRepair.Interaction
{
    /// <summary>
    /// Something that reacts to a single click (buttons, the parts box, the shop cat, a tool on the rack).
    /// Lives next to an <see cref="Interactable"/>. Phone parts are not clickables: they use press-and-hold.
    /// </summary>
    public interface IClickable
    {
        /// <summary>What a click would do, for the hover outline colour and the status line.</summary>
        HoverHint Hint { get; }

        void Click();
    }

    /// <summary>Marks Interactables that draw their own hover look (buttons tint), so no outline is added.</summary>
    public interface ICustomHover
    {
    }
}
