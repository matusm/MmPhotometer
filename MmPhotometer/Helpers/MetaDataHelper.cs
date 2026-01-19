using System;
using System.Collections.Generic;

namespace MmPhotometer
{
    public partial class Program
    {
        internal static Dictionary<string, string> SampleMetaDataRecords(int sampleIndex)
        {
            Dictionary<string, string> metaData = new Dictionary<string, string>();
            metaData.Add($"SampleName", $"{sampleInfo.GetSampleName(sampleIndex)}");
            metaData.Add($"SampleDescription", $"{sampleInfo.GetSampleDescription(sampleIndex)}");
            // Add mode
            // Add comment
            metaData.Add("Application", $"{GetAppNameAndVersion()}");
            metaData.Add("Date", $"{DateTime.Now:yyyy-MM-dd}");
            metaData.Add("Time", $"{DateTime.Now:HH:mm:ss}");
            metaData.Add("SpectrometerManufacturer", $"{spectro.InstrumentManufacturer}");
            metaData.Add("SpectrometerType", $"{spectro.InstrumentType}");
            metaData.Add("SpectrometerSerialNumber", $"{spectro.InstrumentSerialNumber}");
            metaData.Add("SpectrometerMinWavelength", $"{spectro.MinimumWavelength:F2} nm");
            metaData.Add("SpectrometerMaxWavelength", $"{spectro.MaximumWavelength:F2} nm");
            metaData.Add("SpectrometerPixelNumber", $"{spectro.Wavelengths.Length}");
            metaData.Add("ShutterName", $"{shutter.Name}");
            metaData.Add("FilterWheelName", $"{filterWheel.Name}");
            metaData.Add("FilterWheelPositions", $"{filterWheel.FilterCount}");
            return metaData;
        }

    }
}
