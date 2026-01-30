using System;
using System.Collections.Generic;
using System.Globalization;

namespace MmPhotometer
{
    public partial class Program
    {
        internal static Dictionary<string, string> SampleMetaDataRecords(string sampleName, string sampleDescription)
        {
            Dictionary<string, string> metaData = new Dictionary<string, string>();
            metaData.Add("SampleName", $"{sampleName}");
            metaData.Add("SampleDescription", $"{sampleDescription}");
            metaData.Add("MeasurementMode", $"{_options.Mode.ToFriendlyString()}");
            metaData.Add("UserComment", $"{_options.UserComment}");
            metaData.Add("Application", $"{GetAppNameAndVersion()}");
            metaData.Add("DateTime", $"{DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)}");
            if(temperature.HasTemperatureData())
                metaData.Add("InstrumentTemperature", $"[{temperature.GetFirstTemperature():F2} ; {temperature.GetTemperatureAverage():F2} ; {temperature.GetLatestTemperature():F2}] °C");
            metaData.Add("SpectrometerManufacturer", $"{spectro.InstrumentManufacturer}");
            metaData.Add("SpectrometerType", $"{spectro.InstrumentType}");
            metaData.Add("SpectrometerSerialNumber", $"{spectro.InstrumentSerialNumber}");
            metaData.Add("SpectrometerMinWavelength", $"{spectro.MinimumWavelength:F2} nm");
            metaData.Add("SpectrometerMaxWavelength", $"{spectro.MaximumWavelength:F2} nm");
            metaData.Add("SpectrometerPixelNumber", $"{spectro.Wavelengths.Length}");
            metaData.Add("Shutter", $"{shutter.Name}");
            metaData.Add("FilterWheel", $"{filterWheel.Name}");
            metaData.Add("FilterWheelPositions", $"{filterWheel.FilterCount}");
            return metaData;
        }

        internal static Dictionary<string, string> SampleMetaDataRecords(int sampleIndex)
        {
            return SampleMetaDataRecords(sampleInfo.GetSampleName(sampleIndex), sampleInfo.GetSampleDescription(sampleIndex));
        }
    }
}
