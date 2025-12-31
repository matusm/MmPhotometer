using At.Matus.OpticalSpectrumLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace MmPhotometer.Helpers
{
    public static class SpectralReader
    {
        public static OpticalSpectrum ReadSpectrumFromCsv(string filePath)
        {
            var triples = ReadTriplesFromCsv(filePath);
            // Convert triples to OpticalSpectrum
            List<SpectralPoint> points = new List<SpectralPoint>();
            foreach (var (a, b, c) in triples)
            {
                points.Add(new SpectralPoint(a, b, c));
            }
            // TODO: Sort points by wavelength if necessary
            var spectrum = new OpticalSpectrum(points.ToArray());
            spectrum.AddMetaDataRecord("Source", Path.GetFileName(filePath));
            return spectrum;
        }

        private static List<(double a, double b, double c)> ReadTriplesFromCsv(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path must be provided.", nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException("CSV file not found.", filePath);

            var triples = new List<(double a, double b, double c)>();
            var numberFormat = CultureInfo.InvariantCulture;

            foreach (var rawLine in File.ReadLines(filePath))
            {
                var line = rawLine?.Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                // Skip common header indicators
                if (line.StartsWith("#") || line.StartsWith("//", StringComparison.Ordinal))
                    continue;

                // Split by comma; also tolerate semicolon/tab as fallbacks
                string[] parts = line.Split(new[] { ',', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3)
                    continue;

                if (!double.TryParse(parts[0], NumberStyles.Float | NumberStyles.AllowThousands, numberFormat, out double a))
                    continue;
                if (!double.TryParse(parts[1], NumberStyles.Float | NumberStyles.AllowThousands, numberFormat, out double b))
                    continue;
                if (!double.TryParse(parts[2], NumberStyles.Float | NumberStyles.AllowThousands, numberFormat, out double c))
                    continue;

                triples.Add((a, b, c));
            }

            return triples;
        }


    }
}
