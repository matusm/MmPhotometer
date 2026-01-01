using Bev.Instruments.ArraySpectrometer.Domain;
using MmPhotometer.Helpers;
using System.Diagnostics;

namespace MmPhotometer
{
    public partial class Program
    {

        internal static void ObtainOptimalExposureTimeAndReferenceSpectrum(SpectralRegionPod spectralRegionPod)
        {
            SetDebug();
            eventLogger.LogEvent("Determining optimal exposure time ...");
            filterWheel.GoToPosition(spectralRegionPod.FilterPositionAsInt);
            shutter.Open();
            double intTime = spectro.GetOptimalExposureTime(debug);
            spectralRegionPod.SetIntegrationTime(intTime);
            eventLogger.LogEvent($"Optimal integration time for filter {spectralRegionPod.FilterPosition}: {intTime} s.");
            //var refSpectrum = PerformABBAMeasurement(spectralRegionPod.FilterPositionAsInt, spectralRegionPod.IntegrationTime, spectralRegionPod.NumberOfAverages);
            spectro.SetIntegrationTime(spectralRegionPod.IntegrationTime);
            var refSpectrum = OnCallMeasureSpectrum(spectralRegionPod.NumberOfAverages, "reference spectrum");
            spectralRegionPod.SetRawReferenceSpectrum(refSpectrum);
            refSpectrum.SaveSpectrumAsCsv(eventLogger.LogDirectory, $"1_RawSpecRef{spectralRegionPod.FilterPosition}.csv");
        }

        internal static void ObtainDarkSpectrum(SpectralRegionPod spectralRegionPod)
        {
            eventLogger.LogEvent($"Measuring dark spectra ...");
            spectro.SetIntegrationTime(spectralRegionPod.IntegrationTime);
            shutter.Close();
            var darkSpectrum = OnCallMeasureSpectrum(spectralRegionPod.NumberOfAverages, "dark spectrum");
            spectralRegionPod.SetDarkSpectrum(darkSpectrum);
            darkSpectrum.SaveSpectrumAsCsv(eventLogger.LogDirectory, $"0_RawSpecDark{spectralRegionPod.FilterPosition}.csv");
        }

        internal static void ObtainSampleSpectrum(int sampleNumber, SpectralRegionPod spectralRegionPod)
        {
            eventLogger.LogEvent($"Measuring sample spectrum for filter {spectralRegionPod.FilterPosition} ...");
            filterWheel.GoToPosition(spectralRegionPod.FilterPositionAsInt);
            spectro.SetIntegrationTime(spectralRegionPod.IntegrationTime);
            shutter.Open();
            var sampleSpectrum = OnCallMeasureSpectrum(spectralRegionPod.NumberOfAverages, "sample spectrum");
            spectralRegionPod.SetRawSampleSpectrum(sampleNumber, sampleSpectrum);
            sampleSpectrum.SaveSpectrumAsCsv(eventLogger.LogDirectory, $"2_RawSpecSample{spectralRegionPod.FilterPosition}.csv");
        }

        [Conditional("DEBUG")]
        private static void SetDebug()
        {
            debug = true;
        }

    }
}
