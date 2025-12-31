using At.Matus.OpticalSpectrumLib;
using System.IO;

namespace MmPhotometer.Helpers
{
    internal static class Extensions
    {
        public static void SaveSpectrumAsCsv(this IOpticalSpectrum spectrum, string directory, string fileName)
        {
            StreamWriter csvFile = new StreamWriter(Path.Combine(directory, fileName));
            // Write CSV header
            csvFile.WriteLine(spectrum.DataPoints[0].GetCsvHeader());
            foreach (ISpectralPoint item in spectrum.DataPoints)
            {
                csvFile.WriteLine(item.ToCsvLine());
            }
            csvFile.Close();
        }

    }
}
