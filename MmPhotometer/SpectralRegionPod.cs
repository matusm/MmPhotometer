using At.Matus.OpticalSpectrumLib;
using System;

namespace MmPhotometer
{
    public class SpectralRegionPod
    {
        private double _maxIntegrationTime;
        private IOpticalSpectrum[] _darkCorrectedSampleSpectra;
        private IOpticalSpectrum _darkCorrectedReferenceSpectrum;

        public double IntegrationTime { get; private set; }
        public FilterPosition FilterPosition { get; }
        public int FilterPositionAsInt => (int)FilterPosition;
        public int NumberOfAverages { get; set; } = 2;
        public int NumberOfSamples => _darkCorrectedSampleSpectra.Length;
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
            _darkCorrectedSampleSpectra = new IOpticalSpectrum[numberOfSamples];
        }

        public void SetIntegrationTime(double integrationTime)
        {
            IntegrationTime = Math.Min(integrationTime, _maxIntegrationTime);
        }

        public void SetDarkCorrectedReferenceSpectrum(IOpticalSpectrum spectrum)
        {
            _darkCorrectedReferenceSpectrum = spectrum;
        }

        public void SetDarkCorrectedSampleSpectrum(int sampleNumber, IOpticalSpectrum spectrum)
        {
            _darkCorrectedSampleSpectra[sampleNumber] = spectrum;
        }

        public IOpticalSpectrum GetMaskedTransmissionSpectrum(int sampleNumber)
        {
            return EvaluateMaskedTransmissionSpectrum(sampleNumber);
        }

        private IOpticalSpectrum EvaluateMaskedTransmissionSpectrum(int sampleNumber)
        {
            var transmissionSpectrum = (SpecMath.Ratio(_darkCorrectedSampleSpectra[sampleNumber], _darkCorrectedReferenceSpectrum)).Scale(100.0);
            var maskedTransmissionSpectrum = transmissionSpectrum.ApplyBandpassMask(CutoffLow, CutoffHigh, Bandwidth, Bandwidth);
            return maskedTransmissionSpectrum;
        }
    }
}
