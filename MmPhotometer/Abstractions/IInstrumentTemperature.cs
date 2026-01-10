namespace MmPhotometer.Abstractions
{
    public interface IInstrumentTemperature
    {
        void Update();
        void Reset();
        bool HasTemperatureData();
        double GetTemperatureAverage();
        double GetTemperatureRange();
        double GetTemperatureMax();
        double GetTemperatureMin();
        double GetFirstTemperature();
        double GetLatestTemperature();
    }
}
