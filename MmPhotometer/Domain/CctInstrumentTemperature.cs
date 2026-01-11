using At.Matus.DataSeriesPod;
using Bev.Instruments.Thorlabs.Ctt;
using MmPhotometer.Abstractions;

namespace MmPhotometer.Domain
{
    public class CctInstrumentTemperature : IInstrumentTemperature
    {
        private readonly ThorlabsCct _spectro;
        private readonly DataSeriesPod _dsp;

        public CctInstrumentTemperature(ThorlabsCct spectro)
        {
            _spectro = spectro;
            _dsp = new DataSeriesPod();
        }

        public void Update() 
        { 
            _dsp.Update(_spectro.GetTemperature());
        }

        public void Reset() { _dsp.Restart(); }

        public bool HasTemperatureData() => _dsp.SampleSize > 0;
        public double GetTemperatureAverage() => _dsp.AverageValue;
        public double GetTemperatureRange() => _dsp.Range;
        public double GetTemperatureMax() => _dsp.MaximumValue;
        public double GetTemperatureMin() => _dsp.MinimumValue;
        public double GetFirstTemperature() => _dsp.FirstValue;
        public double GetLatestTemperature() => _dsp.MostRecentValue;
    }
}
