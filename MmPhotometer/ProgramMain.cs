using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace MmPhotometer
{
    public partial class Program
    {
        static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            Parser parser = new Parser(with => with.HelpWriter = null);
            ParserResult<Options> parserResult = parser.ParseArguments<Options>(args);
            parserResult
                .WithParsed<Options>(options => Run(options))
                .WithNotParsed(errs => DisplayHelp(parserResult, errs));
        }

        private static void DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errs)
        {
            string appName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            HelpText helpText = HelpText.AutoBuild(result, h =>
            {
                h.AutoVersion = false;
                h.AdditionalNewLineAfterOption = false;
                h.AddPreOptionsLine("\nProgram to control the self built filter photometer");
                h.AddPreOptionsLine("");
                h.AddPreOptionsLine($"Usage: {appName} InputPath [OutPath] [options]");
                return HelpText.DefaultParsingErrorsHandler(result, h);
            }, e => e);
            Console.WriteLine(helpText);
        }

        private static string GetAppNameAndVersion()
        {
            string appName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            string appVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            return $"{appName} v{appVersion}";
        }

        private static void LogSetupInfo()
        {
            eventLogger.WriteLine("=== Instrument Information ===");
            eventLogger.Write(UIHelper.FormatSpectrometerInfo(spectro));
            eventLogger.WriteLine(UIHelper.FormatShutterInfo(shutter));
            eventLogger.Write(UIHelper.FormatFilterWheelInfo(filterWheel));
            eventLogger.WriteLine("===== Sample Information =====");
            for (int i = 0; i < sampleInfo.SampleNames.Length; i++)
            {
                eventLogger.WriteLine($"Sample {i + 1}: {sampleInfo.SampleNames[i]}");
            }
            eventLogger.WriteLine("==============================");
        }

    }
}
