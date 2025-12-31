using Bev.Instruments.ArraySpectrometer.Domain;
using MmPhotometer.Helpers;
using System.Diagnostics;

namespace MmPhotometer
{
    public partial class Program
    {
        // This high level method determines optimal exposure time for a given filter position,
        // performs an ABBA measurement and saves the reference spectrum as CSV file.
        // and takes care of logging and filter wheel positioning, and console output.
        //
        // This method is quite worse in terms of SRP (Single Responsibility Principle)!
        internal static void ObtainReferenceSpectrum(SpectralRegionPod spectralRegionPod)
        {
            SetDebug();
            eventLogger.LogEvent("Determining optimal exposure time ...");
            filterWheel.GoToPosition(spectralRegionPod.FilterPositionAsInt);
            double intTime = spectro.GetOptimalExposureTime(debug);
            spectralRegionPod.SetIntegrationTime(intTime);
            eventLogger.LogEvent($"Optimal integration time for filter {spectralRegionPod.FilterPosition}: {intTime} s.");
            var refSpectrum = PerformABBAMeasurement(spectralRegionPod.FilterPositionAsInt, spectralRegionPod.IntegrationTime, spectralRegionPod.NumberOfAverages);
            spectralRegionPod.SetDarkCorrectedReferenceSpectrum(refSpectrum);
            refSpectrum.SaveSpectrumAsCsv(eventLogger.LogDirectory, $"1_RawSpecRef{spectralRegionPod.FilterPosition}.csv");
        }

        internal static void ObtainSampleSpectrum(int sampleNumber, SpectralRegionPod spectralRegionPod)
        {
            eventLogger.LogEvent($"Measuring sample spectrum for filter {spectralRegionPod.FilterPosition} ...");
            filterWheel.GoToPosition(spectralRegionPod.FilterPositionAsInt);
            var sampleSpectrum = PerformABBAMeasurement(spectralRegionPod.FilterPositionAsInt, spectralRegionPod.IntegrationTime, spectralRegionPod.NumberOfAverages);
            spectralRegionPod.SetDarkCorrectedSampleSpectrum(sampleNumber, sampleSpectrum);
            sampleSpectrum.SaveSpectrumAsCsv(eventLogger.LogDirectory, $"2_RawSpecSample{spectralRegionPod.FilterPosition}.csv");
        }

        [Conditional("DEBUG")]
        private static void SetDebug()
        {
            debug = true;
        }

    }
}
