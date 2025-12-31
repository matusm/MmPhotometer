using At.Matus.OpticalSpectrumLib;
using Bev.Instruments.ArraySpectrometer.Domain;
using MmPhotometer.Helpers;
using System;

namespace MmPhotometer
{
    public partial class Program
    {
        // this method sets the filter wheel to the desired filter position,
        // sets the integration time, and performs an ABBA measurement sequence
        internal static IOpticalSpectrum PerformABBAMeasurement(int filterIdx, double integrationTime, int numberOfAverages)
        {
            Console.WriteLine();
            spectro.SetIntegrationTime(integrationTime);
            if(filterWheel.GetPosition() != filterIdx)
                filterWheel.GoToPosition(filterIdx);
            eventLogger.LogEvent($"ABBA sequence, filter index {filterIdx}, integration time {spectro.GetIntegrationTime()} s, {numberOfAverages} samples.");
            MeasuredOpticalSpectrum signal = new MeasuredOpticalSpectrum(spectro.Wavelengths);
            MeasuredOpticalSpectrum dark = new MeasuredOpticalSpectrum(spectro.Wavelengths);

            // first A of ABBA Measurement Sequence
            shutter.Open();
            OnCallUpdateSpectrum(signal, numberOfAverages, "first A of ABBA");
            // first B of ABBA Measurement Sequence
            shutter.Close();
            OnCallUpdateSpectrum(dark, numberOfAverages, "first B of ABBA");
            // second B of ABBA Measurement Sequence
            shutter.Close();
            OnCallUpdateSpectrum(dark, numberOfAverages, "second B of ABBA");
            // second A of ABBA Measurement Sequence
            shutter.Open();
            OnCallUpdateSpectrum(signal, numberOfAverages, "second A of ABBA");

            OpticalSpectrum correctedSignal = SpecMath.Subtract(signal, dark);
            // TODO: update metadata of correctedSignal to indicate ABBA correction
            Console.WriteLine();
            return correctedSignal;
        }

        // this method sets the integration time and performs an ABBA measurement sequence.
        // filter wheel position is not changed here.
        internal static IOpticalSpectrum PerformABBAControlMeasurement(double integrationTime, int numberOfAverages)
        {
            Console.WriteLine();
            spectro.SetIntegrationTime(integrationTime);
            eventLogger.LogEvent($"ABBA sequence, control with closed shutter, integration time {spectro.GetIntegrationTime()} s, {numberOfAverages} samples.");
            MeasuredOpticalSpectrum signal = new MeasuredOpticalSpectrum(spectro.Wavelengths);
            MeasuredOpticalSpectrum dark = new MeasuredOpticalSpectrum(spectro.Wavelengths);

            shutter.Close();
            // first A of ABBA Measurement Sequence
            OnCallUpdateSpectrum(signal, numberOfAverages, "first A of ABBA");
            // first B of ABBA Measurement Sequence
            OnCallUpdateSpectrum(dark, numberOfAverages, "first B of ABBA");
            // second B of ABBA Measurement Sequence
            OnCallUpdateSpectrum(dark, numberOfAverages, "second B of ABBA");
            // second A of ABBA Measurement Sequence
            OnCallUpdateSpectrum(signal, numberOfAverages, "second A of ABBA");
            shutter.Open();
            OpticalSpectrum correctedSignal = SpecMath.Subtract(signal, dark);
            // TODO: update metadata of correctedSignal to indicate ABBA correction
            return correctedSignal;
        }




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
