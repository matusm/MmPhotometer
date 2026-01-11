namespace MmPhotometer
{
    // FilterWheelShutter will no longer work. (no blocked port)
    public enum FilterPosition
    {
        Unknown = 0,
        FilterA = 1, // Violet
        FilterB = 2, // Blue
        FilterC = 3, // Yellow
        FilterD = 4, // Red
        FilterE = 5, // NIR
        OpenPort = 6
    }

    public enum MeasurementMode
    {
        Unknown = 0,
        SinglePass = 1,
        TwoPass = 2,
        ThreePass = 3,
        FourPass = 4,
        FivePass = 5
    }

    public static class Enums
    {
        public static string ToFriendlyString(this FilterPosition position)
        {
            switch (position)
            {
                case FilterPosition.FilterA:
                    return "Filter A (Violet)";
                case FilterPosition.FilterB:
                    return "Filter B (Blue)";
                case FilterPosition.FilterC:
                    return "Filter C (Yellow)";
                case FilterPosition.FilterD:
                    return "Filter D (Red)";
                case FilterPosition.FilterE:
                    return "Filter E (NIR)";
                case FilterPosition.OpenPort:
                    return "Open Port (no filter)";
                default:
                    return "Unknown";
            }
        }

        public static string ToFriendlyString(this MeasurementMode mode)
        {
            switch (mode)
            {
                case MeasurementMode.SinglePass:
                    return "Unfiltered (single pass)";
                case MeasurementMode.TwoPass:
                    return "Violet + Unfiltered (two pass)";
                case MeasurementMode.ThreePass:
                    return "Violet + Unfiltered + NIR (three pass)";
                case MeasurementMode.FourPass:
                    return "Violet + Blue + Yellow + Red (four pass)";
                case MeasurementMode.FivePass:
                    return "Violet + Blue + Yellow + Red + NIR (five pass)";
                default:
                    return "Unknown";
            }
        }
    }

}
