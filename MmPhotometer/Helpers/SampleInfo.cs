using System;
using System.IO;
using System.Linq;

namespace MmPhotometer
{
    internal class SampleInfo
    {
        private readonly string[] _sampleNames;

        public int NumberOfSamples => _sampleNames.Length;
        public string[] SampleNames => _sampleNames.Select(name => name.Trim()).ToArray();

        public SampleInfo(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                _sampleNames = LoadSampleNamesFromFile(filePath);
            }
            else
            {
                _sampleNames = new string[1];
                _sampleNames[0] = "Default Sample";
            }
        }

        public string GetSampleName(int index)
        {
            if (index < 0 || index >= _sampleNames.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index for sample names is out of range.");
            }
            return SampleNames[index];
        }

        private string[] LoadSampleNamesFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Sample names file not found.", filePath);
            }
            return File.ReadAllLines(filePath).Where(s => s.Trim() != string.Empty).ToArray();
        }

    }
}
