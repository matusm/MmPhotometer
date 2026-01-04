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
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

            #region Setup spectral region pods as an array

            double fromWl = options.LowerBound;
            double toWl = options.UpperBound;
            double step = options.StepSize;
            spectralRegionPods = new SpectralRegionPod[]
            {
                new SpectralRegionPod(numSamples, FilterPosition.FilterA, options.MaxIntTime, 100, 464, 10),
                new SpectralRegionPod(numSamples, FilterPosition.FilterB, options.MaxIntTime, 464, 545, 10),
                new SpectralRegionPod(numSamples, FilterPosition.FilterC, options.MaxIntTime, 545, 658, 10),
                new SpectralRegionPod(numSamples, FilterPosition.FilterD, options.MaxIntTime, 658, 2000, 10),
                new SpectralRegionPod(numSamples, FilterPosition.Blank, options.MaxIntTime, 100, 2000, 0)
            };
            for (int i = 0; i < spectralRegionPods.Length; i++)
            {
                spectralRegionPods[i].SetIntegrationTime(spectro.MinimumIntegrationTime);
                spectralRegionPods[i].NumberOfAverages = options.NumberOfAverages;
            }

            // logic to minimize measurements if not all spectral regions are needed
            for (int i = 0; i < spectralRegionPods.Length; i++) 
            {
                var pod = spectralRegionPods[i];
                pod.ShouldMeasure = true;
                if (fromWl > pod.CutoffHigh + pod.Bandwidth)
                {
                    pod.ShouldMeasure = false;
                }
                if(toWl < pod.CutoffLow - pod.Bandwidth)
                {
                    pod.ShouldMeasure = false;
                }
            }
            if(options.BasicOnly)
            {
                spectralRegionPods[0].ShouldMeasure = false;
                spectralRegionPods[1].ShouldMeasure = false;
                spectralRegionPods[2].ShouldMeasure = false;
                spectralRegionPods[3].ShouldMeasure = false;
                spectralRegionPods[4].ShouldMeasure = true;
            }
            for (int i = 0; i < spectralRegionPods.Length; i++)
            {
                var pod = spectralRegionPods[i];
                eventLogger.LogEvent($"Spectral region pod {i + 1}: {pod}");
            }
            #endregion

            #region Get optimal integration times for each spectral region and take reference and dark spectra
            UIHelper.WriteMessageAndWait(
                "=================================================================\n" +
                "Switch on lamp and remove any samples from photometer.\n" +
                "After warmup press any key to continue.\n" +
                "=================================================================\n");
            
            eventLogger.LogEvent("Determining optimal exposure time ...");
            for (int i = 0; i < spectralRegionPods.Length; i++)
            {
                if(spectralRegionPods[i].ShouldMeasure)
                    ObtainOptimalExposureTimeAndReferenceSpectrum(spectralRegionPods[i]);
            }
            eventLogger.LogEvent($"Measuring dark spectra ...");
            for (int i = 0; i < spectralRegionPods.Length; i++)
            {
                if (spectralRegionPods[i].ShouldMeasure)
                    ObtainDarkSpectrum(spectralRegionPods[i]);
            }
            #endregion

            #region Measure samples
            for (int sampleIndex = 0; sampleIndex < numSamples; sampleIndex++)
            {
                UIHelper.WriteMessageAndWait(
                    $"\n=============================================\n" +
                    $"Insert sample {sampleIndex + 1} and press any key to continue.\n" +
                    $"=============================================\n");
                for (int i = 0; i < spectralRegionPods.Length; i++)
                {
                    if (spectralRegionPods[i].ShouldMeasure)
                        ObtainSampleSpectrum(sampleIndex, spectralRegionPods[i]);
                }
            }
            #endregion

            List<OpticalSpectrum> straylightCorrectedTransmissions = new List<OpticalSpectrum>();
            List<OpticalSpectrum> basicTransmissions = new List<OpticalSpectrum>();

            for (int sampleIndex = 0; sampleIndex < numSamples; sampleIndex++)
            {
                // first process the unfiltered measurement
                if(spectralRegionPods[4].ShouldMeasure)
                {
                    IOpticalSpectrum spec0 = spectralRegionPods[4].GetMaskedTransmissionSpectrum(sampleIndex);
                    spec0.SaveSpectrumAsCsv(eventLogger.LogDirectory, $"Sample{sampleIndex + 1}_basic_raw.csv");
                    OpticalSpectrum spec1 = spec0.ResampleSpectrum(fromWl, toWl, step);
                    spec1.SaveSpectrumAsCsv(eventLogger.LogDirectory, $"Sample{sampleIndex + 1}_basic_resampled.csv");
                    basicTransmissions.Add(spec1);
                }
                // combine masked ratios from each spectral region pod
                List<IOpticalSpectrum> regionSpectra = new List<IOpticalSpectrum>();
                for (int i = 0; i < 4; i++)
                {
                    if (spectralRegionPods[i].ShouldMeasure)
                    {
                        regionSpectra.Add(spectralRegionPods[i].GetMaskedTransmissionSpectrum(sampleIndex));
                    }
                }
                if (regionSpectra.Count != 0)
                {
                    var combinedRegionSpectrum = SpecMath.Add(regionSpectra.ToArray());
                    combinedRegionSpectrum.SaveSpectrumAsCsv(eventLogger.LogDirectory, $"Sample{sampleIndex + 1}_slc_raw.csv");
                    var resampledRegionSpectrum = combinedRegionSpectrum.ResampleSpectrum(fromWl, toWl, step);
                    resampledRegionSpectrum.SaveSpectrumAsCsv(eventLogger.LogDirectory, $"Sample{sampleIndex + 1}_slc_resampled.csv");
                    straylightCorrectedTransmissions.Add(resampledRegionSpectrum);
                }
            }

            Console.WriteLine();
            eventLogger.Close();

            //Plot the spectra
            OpticalSpectrum[] transmissions = straylightCorrectedTransmissions.Concat(basicTransmissions).ToArray();            
            Console.WriteLine("number of spectra: " + transmissions.Length);
            Plotter plotter = new Plotter(transmissions, fromWl, toWl, -10, 110);
            string filePath = Path.Combine(eventLogger.LogDirectory, "TransmissionChart.png");
            plotter.SaveTransmissionChart("Sample Transmission", filePath);
            plotter.ShowTransmissionChart("Sample Transmission");

            Console.WriteLine("done.");
        }

    }

}
