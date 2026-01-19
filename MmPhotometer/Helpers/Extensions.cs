using At.Matus.OpticalSpectrumLib;
using System.IO;

namespace MmPhotometer.Helpers
{
    internal static class Extensions
    {
        public static void SaveAsSimpleCsvFile(this IOpticalSpectrum spectrum, string filePath, bool writeHeader = true)
        {
            StreamWriter csvFile = new StreamWriter(filePath);
            // Write CSV header
            if (writeHeader)
                csvFile.WriteLine(spectrum.DataPoints[0].GetCsvHeader());
            foreach (ISpectralPoint item in spectrum.DataPoints)
            {
                csvFile.WriteLine(item.ToCsvLine());
            }
            csvFile.Close();
        }

    }
}
