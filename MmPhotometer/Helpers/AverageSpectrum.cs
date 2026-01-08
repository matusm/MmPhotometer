using At.Matus.OpticalSpectrumLib;
using Bev.Instruments.ArraySpectrometer.Domain;
using MmPhotometer.Helpers;
using System;

namespace MmPhotometer
{
    public partial class Program
    {
        internal static void OnCallUpdateSpectrum(MeasuredOpticalSpectrum spectrum, int numberOfAverages, string message)
        {
            Console.WriteLine($"Measurement of {message}...");
            ConsoleProgressBar consoleProgressBar = new ConsoleProgressBar();
            for (int i = 0; i < numberOfAverages; i++)
            {
                spectrum.UpdateSignal(spectro.GetNormalizedIntensityData());
                consoleProgressBar.Report(i + 1, numberOfAverages);
            }
        }

        internal static MeasuredOpticalSpectrum OnCallMeasureSpectrum(int numberOfAverages, string message)
        {
            MeasuredOpticalSpectrum resultSpectrum = new MeasuredOpticalSpectrum(spectro.Wavelengths);
            Console.WriteLine($"Measurement of {message}...");
            ConsoleProgressBar consoleProgressBar = new ConsoleProgressBar();
            for (int i = 0; i < numberOfAverages; i++)
            {
                resultSpectrum.UpdateSignal(spectro.GetNormalizedIntensityData());
                consoleProgressBar.Report(i + 1, numberOfAverages);
            }
            return resultSpectrum;
        }
    }
}
