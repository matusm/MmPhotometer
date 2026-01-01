using At.Matus.OpticalSpectrumLib;
using System;

namespace MmPhotometer
{
    public class SpectralRegionPod
    {
        private double _maxIntegrationTime;
        private IOpticalSpectrum[] _rawSampleSpectra;
        private IOpticalSpectrum _rawReferenceSpectrum;
        private IOpticalSpectrum _rawDarkSpectrum;

        public bool IsConfigured => _rawReferenceSpectrum != null && _rawDarkSpectrum != null && ShouldMeasure;
        public bool ShouldMeasure { get; set; } = false;
        public double IntegrationTime { get; private set; }
        public FilterPosition FilterPosition { get; }
        public int FilterPositionAsInt => (int)FilterPosition;
        public int NumberOfAverages { get; set; } = 2;
        public int NumberOfSamples => _rawSampleSpectra.Length;
        public double CutoffLow { get; }
        public double CutoffHigh { get; }
        public double Bandwidth { get; }

        public SpectralRegionPod(int numberOfSamples, FilterPosition filterPosition, double maxIntegrationTime, double cutoffLow, double cutoffHigh, double bw)
        {
            _maxIntegrationTime = maxIntegrationTime;
            FilterPosition = filterPosition;
            CutoffLow = cutoffLow;
            CutoffHigh = cutoffHigh;
            Bandwidth = bw;
            _rawSampleSpectra = new IOpticalSpectrum[numberOfSamples];
        }

        public void SetIntegrationTime(double integrationTime)
        {
            IntegrationTime = Math.Min(integrationTime, _maxIntegrationTime);
        }

        public void SetRawReferenceSpectrum(IOpticalSpectrum spectrum)
        {
            _rawReferenceSpectrum = spectrum;
        }
        
        public void SetRawSampleSpectrum(int sampleNumber, IOpticalSpectrum spectrum)
        {
            _rawSampleSpectra[sampleNumber] = spectrum;
        }

        public void SetDarkSpectrum(IOpticalSpectrum spectrum)
        {
            _rawDarkSpectrum = spectrum;
        }

        public IOpticalSpectrum GetMaskedTransmissionSpectrum(int sampleNumber)
        {
            return EvaluateMaskedTransmissionSpectrum(sampleNumber);
        }

        private IOpticalSpectrum EvaluateMaskedTransmissionSpectrum(int sampleNumber)
        {
            var transmissionSpectrum = (SpecMath.ComputeBiasCorrectedRatio(_rawSampleSpectra[sampleNumber], _rawReferenceSpectrum, _rawDarkSpectrum)).Scale(100.0);
            var maskedTransmissionSpectrum = transmissionSpectrum.ApplyBandpassMask(CutoffLow, CutoffHigh, Bandwidth, Bandwidth, TransitionType.Cubic);
            return maskedTransmissionSpectrum;
        }
    }
}
