using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MmPhotometer
{
    internal class SampleInfo
    {
        private readonly SampleName[] _samples;

        public int NumberOfSamples => _samples.Length;
        public string[] SampleNames => _samples.Select(sample => sample.Name).ToArray();
        public string[] SampleDescriptions => _samples.Select(sample => sample.Description).ToArray();

        public SampleInfo(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                _samples = ParseSampleNames(LoadLinesFromFile(filePath));
            }
            else
            {
                _samples = new SampleName[1];
                _samples[0] = new SampleName("DefaultSample", "No description available.");
            }
        }

        public string GetSampleName(int index)
        {
            if (index < 0 || index >= _samples.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index for sample names is out of range.");
            }
            return SampleNames[index];
        }

        public string GetSampleDescription(int index)
        {
            if (index < 0 || index >= _samples.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index for sample names is out of range.");
            }
            return SampleDescriptions[index];
        }

        private string[] LoadLinesFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Sample names file not found.", filePath);
            }
            return File.ReadAllLines(filePath).Where(s => s.Trim() != string.Empty).ToArray();
        }

        private SampleName[] ParseSampleNames(string[] textLines)
        {
            if (textLines == null || textLines.Length == 0)
            {
                throw new ArgumentException("Sample names cannot be null or empty.");
            }
            List<SampleName> samples = new List<SampleName>();
            foreach (var line in textLines)
            {
                if (line.StartsWith("#"))
                {
                    // this is a comment line, skip it
                    continue;
                }
                var parts = line.Split(new[] { ';' }, 2);
                var left = parts[0].Trim();
                var right = parts.Length > 1 ? parts[1].Trim() : string.Empty;
                samples.Add(new SampleName(left, right));
            }
            return samples.ToArray();
        }
    }

    internal class SampleName
    {
        public string Name { get; }
        public string Description { get; }
        
        public SampleName(string name, string description)
        {
            // Substitute blanks or tabs in name with underscore
            var normalizedName = name?.Replace(' ', '_').Replace('\t', '_');
            Name = normalizedName;

            if (string.IsNullOrEmpty(description))
            {
                description = "No description available.";
            }

            Description = description;
        }
    }
}
