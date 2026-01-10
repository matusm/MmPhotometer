using MmPhotometer.Abstractions;

namespace MmPhotometer.Domain
{
    public class NullInstrumentTemperature : IInstrumentTemperature
    {
        public void Update() { }
        public void Reset() { }
        public bool HasTemperatureData() => false;
        public double GetTemperatureAverage() => double.NaN;
        public double GetTemperatureRange() => double.NaN;
        public double GetTemperatureMax() => double.NaN;
        public double GetTemperatureMin() => double.NaN;
        public double GetFirstTemperature() => double.NaN;
        public double GetLatestTemperature() => double.NaN;
    }
}
