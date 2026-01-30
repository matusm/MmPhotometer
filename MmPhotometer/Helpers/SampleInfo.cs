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
        public SampleName[] Samples => _samples;

        public SampleInfo(string filePath)
        {
            _samples = ParseSampleNames(LoadLinesFromFile(filePath));
        }

        public string GetSampleName(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), "Index for sample names is out of range.");
            if (index >= _samples.Length)
                return string.Empty;
            return _samples[index].Name;
        }

        public string GetSampleDescription(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), "Index for sample descriptions is out of range.");
            if (index >= _samples.Length)
                return string.Empty;
            return _samples[index].Description;
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
                if(string.IsNullOrEmpty(left))
                {
                    continue; // skip empty sample names
                }
                var right = parts.Length > 1 ? parts[1].Trim() : string.Empty;
                samples.Add(new SampleName(left, right));
            }
            if (samples.Count > 0)
            {
                return samples.ToArray();
            }
            return Array.Empty<SampleName>();
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
                description = "No description given.";
            }
            Description = description;
        }

        public override string ToString() => $"{Name}; {Description}";
    }
}
