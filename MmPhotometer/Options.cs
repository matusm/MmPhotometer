using CommandLine;

namespace MmPhotometer
{
    internal class Options
    {
        [Option("low", Default = 400, Required = false, HelpText = "Lower bound of spectral region, in nm")]
        public double LowerBound { get; set; }
        
        [Option("high", Default = 1000, Required = false, HelpText = "Upper bound of spectral region, in nm")]
        public double UpperBound { get; set; }

        [Option("step", Default = 1, Required = false, HelpText = "Step size for wavelength range, in nm")]
        public double StepSize { get; set; }

        [Option('s', "samples", Default = 3, Required = false, HelpText = "Number of samples to calibrate.")]
        public int SampleNumber { get; set; }

        [Option('a', "average", Default = 5, Required = false, HelpText = "Number of spectra to average.")]
        public int NumberOfAverages { get; set; }

        [Option('m', "maxinttime", Default = 1, Required = false, HelpText = "Max integration time in seconds.")]
        public double MaxIntTime { get; set; }

        [Option("comment", Default = "---", Required = false, HelpText = "User supplied comment text.")]
        public string UserComment { get; set; }

        [Option("fwport", Default = "COM3", Required = false, HelpText = "Filter wheel serial port.")] // photometry lab, COM1 for development computer
        public string FwPort { get; set; }

        [Option("basepath", Default = @"C:\temp\MmPhotometer", Required = false, HelpText = "Base path for result directories.")]
        public string BasePath { get; set; }

        [Option("logfile", Default = @"MmPhotometerLog.txt", Required = false, HelpText = "File name for logging.")]
        public string LogFileName { get; set; }

        [Option("spectrometer", Default = 1, Required = false, HelpText = "Spectrometer type (see doc for usage).")]
        public int SpecType { get; set; }
        // 1: Thorlabs CCT
        // 2: Thorlabs CCS
        // 3: USB2000

        [Option("basic", Default = false, Required = false, HelpText = "Measure in basic mode only.")]
        public bool BasicOnly { get; set; }

        [Value(0, MetaName = "InputPath", Required = false, HelpText = "Standard lamp calibration filename")]
        public string InputPath { get; set; }

        [Value(1, MetaName = "OutputPath", Required = false, HelpText = "Result filename including path")]
        public string OutputPath { get; set; }

    }
}