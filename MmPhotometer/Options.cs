using CommandLine;

namespace MmPhotometer
{
    internal class Options
    {

        [Option("mode", Default = 1, Required = false, HelpText = "Bandpass filter mode (see doc for usage).")]
        public int ModeNumber { get; set; }
        // 1: single pass
        // 2: two pass (violet + open)
        // 3: three pass (violet + open + NIR)
        // 4: four pass (violet + blue + yellow + red)
        // 5: five pass (all filters)

        public MeasurementMode Mode
        {
            get
            {
                if (ModeNumber < 1 || ModeNumber > 5)
                {
                    return MeasurementMode.Unknown;
                }
                return (MeasurementMode)ModeNumber;
            }
        }

        [Option("low", Default = 400, Required = false, HelpText = "Lower bound of spectral region, in nm")]
        public double LowerBound { get; set; }
        
        [Option("high", Default = 1000, Required = false, HelpText = "Upper bound of spectral region, in nm")]
        public double UpperBound { get; set; }

        [Option("step", Default = 1, Required = false, HelpText = "Wavelength step size, in nm")]
        public double StepSize { get; set; }

        [Option('a', "average", Default = 10, Required = false, HelpText = "Number of spectra to average.")]
        public int NumberOfAverages { get; set; }

        [Option('m', "maxinttime", Default = 1, Required = false, HelpText = "Upper bound for integration time, in seconds.")]
        public double MaxIntTime { get; set; }

        [Option("control", Default = false, Required = false, HelpText = "Perform control measurements.")]
        public bool ControlMeasurements { get; set; }

        [Option("ABBA", Default = false, Required = false, HelpText = "Perform ABBA measurement sequence.")]
        public bool Abba { get; set; }

        [Option("comment", Default = "---", Required = false, HelpText = "User supplied comment text.")]
        public string UserComment { get; set; }

        [Option("fwport", Default = "COM3", Required = false, HelpText = "Serial port for motorized filter wheel.")] // photometry lab, COM1 for development computer
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


        [Value(0, MetaName = "InputPath", Required = true, HelpText = "Text file containing sample names to be processed.")]
        public string InputPath { get; set; }

        [Value(1, MetaName = "OutputPath", Required = false, HelpText = "Some output file")]
        public string OutputPath { get; set; }

    }
}