using At.Matus.OpticalSpectrumLib;
using Bev.Instruments.ArraySpectrometer.Abstractions;
using Bev.Instruments.OceanOptics.Usb2000;
using Bev.Instruments.OpticalShutterLib.Abstractions;
using Bev.Instruments.OpticalShutterLib.Domain;
using Bev.Instruments.Thorlabs.Ccs;
using Bev.Instruments.Thorlabs.Ctt;
using Bev.Instruments.Thorlabs.FW;
using MmPhotometer.Helpers;
using System;

using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace MmPhotometer
{
    public enum FilterPosition
    {
        FilterA = 1,
        FilterB = 2,
        FilterC = 3,
        FilterD = 4,
        Blank = 5,
        Closed = 6
    }

    public partial class Program
    {
        #region Global datafields
        // The following datafields are defined here for access from static methods
        private static SpectralRegionPod[] spectralRegionPods;
        private static IArraySpectrometer spectro;
        private static IShutter shutter;
        private static IFilterWheel filterWheel;
        private static EventLogger eventLogger;
        private static bool debug = false;
        #endregion

        private static void Run(Options options)
        {
            // instantiate instruments and logger
            eventLogger = new EventLogger(options.BasePath, options.LogFileName);
            filterWheel = new NullFilterWheel();
            //filterWheel = new ManualFilterWheel();
            //filterWheel = new MotorFilterWheel(options.FwPort);
            switch (options.SpecType)
            {
                case 1: // CCT
                    spectro = new ThorlabsCct();
                    shutter = new CctShutter((ThorlabsCct)spectro);
                    break;
                case 2: // CCS
                    spectro = new ThorlabsCcs(ProductID.CCS100, "M00928408");
                    shutter = new FilterWheelShutter(filterWheel, (int)FilterPosition.Closed);
                    shutter = new ManualShutter();
                    break;
                case 3: // Ocean USB2000
                    spectro = new OceanOpticsUsb2000();
                    shutter = new ManualShutter();
                    break;
                default:
                    break;
            }

            eventLogger.LogEvent($"Program: {GetAppNameAndVersion()}");
            eventLogger.LogEvent($"User comment: {options.UserComment}");
            LogSetupInfo();

            int numSamples = options.SampleNumber;

            // setup spectral region pods as an array
            spectralRegionPods = new SpectralRegionPod[]
            {
                new SpectralRegionPod(numSamples, FilterPosition.FilterA, options.MaxIntTime, 180, 464, 10),
                new SpectralRegionPod(numSamples, FilterPosition.FilterB, options.MaxIntTime, 464, 545, 10),
                new SpectralRegionPod(numSamples, FilterPosition.FilterC, options.MaxIntTime, 545, 658, 10),
                new SpectralRegionPod(numSamples, FilterPosition.FilterD, options.MaxIntTime, 658, 2000, 10),
                new SpectralRegionPod(numSamples, FilterPosition.Blank, options.MaxIntTime, 180, 2000, 10)
            };
            for (int i = 0; i < spectralRegionPods.Length; i++)
            {
                spectralRegionPods[i].SetIntegrationTime(spectro.MinimumIntegrationTime);
                spectralRegionPods[i].NumberOfAverages = options.NumberOfAverages;
            }

            #region Get optimal integration times for each spectral region and take reference and dark spectra
            UIHelper.WriteMessageAndWait(
                "=================================================================\n" +
                "Switch on lamp and remove any samples from photometer.\n" +
                "After warmup press any key to continue.\n" +
                "=================================================================\n");

            for (int i = 0; i < spectralRegionPods.Length; i++)
            {
                ObtainOptimalExposureTimeAndReferenceSpectrum(spectralRegionPods[i]);
            }

            for (int i = 0; i < spectralRegionPods.Length; i++)
            {
                ObtainDarkSpectrum(spectralRegionPods[i]);
            }
            #endregion

            #region Measure samples
            for (int sampleIndex = 0; sampleIndex < numSamples; sampleIndex++)
            {
                UIHelper.WriteMessageAndWait($"\n=============================================\n" +
                    $"Insert sample {sampleIndex + 1} and press any key to continue.\n" +
                    $"=============================================\n");
                for (int i = 0; i < spectralRegionPods.Length; i++)
                {
                    ObtainSampleSpectrum(sampleIndex, spectralRegionPods[i]);
                }
            }
            #endregion

            double fromWl = options.LowerBound;
            double toWl = options.UpperBound;
            double step = options.StepSize;

            OpticalSpectrum[] transmissionsDouble = new OpticalSpectrum[numSamples];
            OpticalSpectrum[] transmissionsSimple = new OpticalSpectrum[numSamples];

            for (int sampleIndex = 0; sampleIndex < numSamples; sampleIndex++)
            { 
                // combine masked ratios from each spectral region pod
                var maskedRatioA = spectralRegionPods[0].GetMaskedTransmissionSpectrum(sampleIndex);
                var maskedRatioB = spectralRegionPods[1].GetMaskedTransmissionSpectrum(sampleIndex);
                var maskedRatioC = spectralRegionPods[2].GetMaskedTransmissionSpectrum(sampleIndex);
                var maskedRatioD = spectralRegionPods[3].GetMaskedTransmissionSpectrum(sampleIndex);
                var combinedRatio = SpecMath.Add(maskedRatioA, maskedRatioB, maskedRatioC, maskedRatioD);
                combinedRatio.SaveSpectrumAsCsv(eventLogger.LogDirectory, $"3_combinedRatio_sample{sampleIndex+1}.csv");
                var finalSpectrum = combinedRatio.ResampleSpectrum(fromWl, toWl, step);
                finalSpectrum.SaveSpectrumAsCsv(eventLogger.LogDirectory, $"4_SampleTransmission_sample{sampleIndex + 1}.csv");
                transmissionsDouble[sampleIndex] = finalSpectrum;
                var finalSpectrumSimple = spectralRegionPods[4].GetMaskedTransmissionSpectrum(0).ResampleSpectrum(fromWl, toWl, step);
                finalSpectrumSimple.SaveSpectrumAsCsv(eventLogger.LogDirectory, $"4_SampleTransmissionSimple_sample{sampleIndex + 1}.csv");
                transmissionsSimple[sampleIndex] = finalSpectrumSimple;
            }

            Console.WriteLine();
            eventLogger.Close();

            //Plot the spectra
            OpticalSpectrum[] transmissions = transmissionsDouble.Concat(transmissionsSimple).ToArray();
            Console.WriteLine("number of spectra: " + transmissions.Length);
            Program program = new Program();
            program.ShowTransmissionChart(transmissions, fromWl, toWl, -10, 110);

        }

    }

}
