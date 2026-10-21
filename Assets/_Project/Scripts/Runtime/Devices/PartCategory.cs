namespace PhoneRepair.Devices
{
    /// <summary>
    /// Broad family of a component. Drives default behaviour (e.g. collider shape, which tools
    /// apply). Explicit values so new categories can be inserted without breaking saved assets.
    /// </summary>
    public enum PartCategory
    {
        Chassis = 0,
        Display = 10,
        Cover = 20,
        Fastener = 30,
        Battery = 40,
        Connector = 50,
        LogicBoard = 60,
        Port = 70,
        Camera = 80,
        Audio = 90,
        Other = 1000,
    }

    public static class PartCategoryExtensions
    {
        public static string ToLabel(this PartCategory category) => category switch
        {
            PartCategory.LogicBoard => "Logic Board",
            _ => category.ToString(),
        };
    }
}
