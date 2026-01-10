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
}
