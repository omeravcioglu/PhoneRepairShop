namespace PhoneRepair.Pixel
{
    /// <summary>
    /// Sorting orders, back to front. Everything uses the default sorting layer. The shop view and the
    /// bench view never share the screen (except during the slide), so their ranges may overlap.
    /// </summary>
    public static class SortOrder
    {
        // shop view
        public const int ShopBackground = 0;
        public const int ShopDoor = 10;
        public const int CustomerAtDoor = 20;
        public const int ShopWallProps = 30;
        public const int Customer = 100;
        public const int Counter = 200;
        public const int CounterProps = 210;
        public const int SpeechBubble = 400;

        // bench view
        public const int BenchBackground = 0;
        public const int BenchProps = 50;
        public const int Phone = 300;          // + each part's installed offset (0..60)
        public const int PartsOnMat = 380;     // + spot offset
        public const int PartsFlying = 600;

        // both
        public const int Ui = 800;
        public const int UiText = 820;
        public const int Panels = 900;
        public const int PanelText = 920;
        public const int Cursor = 2000;
    }
}
