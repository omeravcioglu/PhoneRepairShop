namespace PhoneRepair.Devices
{
    /// <summary>
    /// Whether a part works. A job starts with one part broken (cracked screen, swollen battery);
    /// the fix is to take it out, swap it at the parts box and put the new one in.
    /// Later milestones add finer faults (dirty port, loose connector) on top of this.
    /// </summary>
    public enum PartCondition
    {
        Good = 0,
        Broken = 10,
    }
}
