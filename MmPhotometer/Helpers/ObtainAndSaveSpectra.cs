using Bev.Instruments.ArraySpectrometer.Domain;
using MmPhotometer.Helpers;

namespace MmPhotometer
{
    public partial class Program
    {
        internal static void ObtainOptimalExposureTimeAndReferenceSpectrum(SpectralRegionPod spectralRegionPod)
        {
            temperature.Update(); // every now and then update temperature reading
            if (spectralRegionPod.ShouldMeasure == false) return;
            bool debug = false;
            filterWheel.GoToPosition(spectralRegionPod.FilterPositionAsInt);
            shutter.Open();
            double intTime = spectro.GetOptimalExposureTime(debug);
            spectralRegionPod.SetIntegrationTime(intTime);
            eventLogger.LogEvent($"Optimal integration time - {spectralRegionPod.FilterPosition.ToFriendlyString()}: {intTime} s.");
            spectro.SetIntegrationTime(spectralRegionPod.IntegrationTime);
            var refSpectrum = OnCallMeasureSpectrum(spectralRegionPod.NumberOfAverages, "reference spectrum");
            spectralRegionPod.SetRawReferenceSpectrum(refSpectrum);
            refSpectrum.SaveSpectrumAsCsv(rawDataFolderName, $"0_RawReference{spectralRegionPod.FilterPosition}.csv");
            temperature.Update(); // every now and then update temperature reading
        }

        internal static void ObtainDarkSpectrum(SpectralRegionPod spectralRegionPod)
        {
            temperature.Update(); // every now and then update temperature reading
            if (spectralRegionPod.ShouldMeasure == false) return;
            spectro.SetIntegrationTime(spectralRegionPod.IntegrationTime);
            shutter.Close();
            var darkSpectrum = OnCallMeasureSpectrum(spectralRegionPod.NumberOfAverages, $"dark spectrum - {spectralRegionPod.FilterPosition.ToFriendlyString()}");
            spectralRegionPod.SetDarkSpectrum(darkSpectrum);
            darkSpectrum.SaveSpectrumAsCsv(rawDataFolderName, $"1_RawBackground{spectralRegionPod.FilterPosition}.csv");
            temperature.Update(); // every now and then update temperature reading
        }

        internal static void ObtainSampleSpectrum(int sampleNumber, SpectralRegionPod spectralRegionPod)
        {
            temperature.Update(); // every now and then update temperature reading
            if (spectralRegionPod.ShouldMeasure == false) return;
            eventLogger.LogEvent($"Measuring sample spectrum - {spectralRegionPod.FilterPosition.ToFriendlyString()} ...");
            filterWheel.GoToPosition(spectralRegionPod.FilterPositionAsInt);
            spectro.SetIntegrationTime(spectralRegionPod.IntegrationTime);
            shutter.Open();
            var sampleSpectrum = OnCallMeasureSpectrum(spectralRegionPod.NumberOfAverages, "sample spectrum");
            spectralRegionPod.SetRawSampleSpectrum(sampleNumber, sampleSpectrum);
            sampleSpectrum.SaveSpectrumAsCsv(rawDataFolderName, $"2_RawSample{spectralRegionPod.FilterPosition}.csv");
            temperature.Update(); // every now and then update temperature reading
        }
    }
}
