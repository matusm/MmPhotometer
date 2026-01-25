using At.Matus.OpticalSpectrumLib;
using At.Matus.OpticalSpectrumLib.Domain;
using Bev.Instruments.ArraySpectrometer.Abstractions;
using Bev.Instruments.OceanOptics.Usb2000;
using Bev.Instruments.OpticalShutterLib.Abstractions;
using Bev.Instruments.OpticalShutterLib.Domain;
using Bev.Instruments.Thorlabs.Ccs;
using Bev.Instruments.Thorlabs.Ctt;
using Bev.Instruments.Thorlabs.FW;
using MmPhotometer.Abstractions;
using MmPhotometer.Domain;
using System;
using System.Collections.Generic;
using System.IO;

namespace MmPhotometer
{
    public partial class Program
    {
        #region Global datafields
        // The following datafields are defined here for access from static methods
        private static SpectralRegionPod[] spectralRegionPods1; // for AB.. of ABBA
        private static SpectralRegionPod[] spectralRegionPods2; // for ..BA of ABBA
        private static IArraySpectrometer spectro;
        private static IShutter shutter; // FilterWheelShutter will no longer work. (no blocked port)
        private static IFilterWheel filterWheel;
        private static IInstrumentTemperature temperature;
        private static EventLogger eventLogger;
        private static SampleInfo sampleInfo;
        private static string rawDataFolderName;
        private static string dataFolderName;
        private static double _lowerWavelength;
        private static double _upperWavelength;
        private static double _wavelengthStep;
        private static Options _options; // for use outside of Run()
        #endregion

        private static void Run(Options options)
        {
            _options = options;
            eventLogger = new EventLogger(options.BasePath, options.LogFileName);
            //filterWheel = new NullFilterWheel();
            filterWheel = new ManualFilterWheel();
            //filterWheel = new MotorFilterWheel(options.FwPort);
            switch (options.SpecType)
            {
                case 1: // CCT
                    spectro = new ThorlabsCct();
                    shutter = new CctShutter((ThorlabsCct)spectro);
                    temperature = new CctInstrumentTemperature((ThorlabsCct)spectro);
                    break;
                case 2: // CCS
                    spectro = new ThorlabsCcs(ProductID.CCS100, "M00928408");
                    shutter = new ManualShutter();
                    temperature = new NullInstrumentTemperature();
                    break;
                case 3: // Ocean USB2000
                    spectro = new OceanOpticsUsb2000();
                    shutter = new ManualShutter();
                    temperature = new NullInstrumentTemperature();
                    break;
                default:
                    break;
            }

            _lowerWavelength = options.LowerBound;
            _upperWavelength = options.UpperBound;
            _wavelengthStep = options.StepSize;
            ClampWavelengthRange();

            temperature.Update(); // every now and then update temperature reading

            // get sample names from input file
            sampleInfo = new SampleInfo(options.InputPath);
            int numSamples = sampleInfo.NumberOfSamples;
            int numSamplesWithControls = options.ControlMeasurements ? numSamples + 2 : numSamples;

            dataFolderName = eventLogger.LogDirectory;
            rawDataFolderName = Path.Combine(dataFolderName, "RawSpectra");
            Directory.CreateDirectory(rawDataFolderName);
            eventLogger.LogEvent($"Program: {GetAppNameAndVersion()}");
            eventLogger.LogEvent($"User comment: {options.UserComment}");
            eventLogger.LogEvent($"Measurement mode: {options.Mode.ToFriendlyString()}");
            LogSetupInfo();

            #region Setup spectral region pods as an array

            spectralRegionPods1 = SetupPods(options.Mode, numSamplesWithControls, options.MaxIntTime); // for AB.. of ABBA
            spectralRegionPods2 = SetupPods(options.Mode, numSamplesWithControls, options.MaxIntTime); // for ..BA of ABBA
            for (int i = 0; i < spectralRegionPods1.Length; i++)
            {
                spectralRegionPods1[i].SetIntegrationTime(spectro.MinimumIntegrationTime);
                spectralRegionPods1[i].NumberOfAverages = options.NumberOfAverages;
            }
            for (int i = 0; i < spectralRegionPods2.Length; i++)
            {
                spectralRegionPods2[i].SetIntegrationTime(spectro.MinimumIntegrationTime);
                spectralRegionPods2[i].NumberOfAverages = options.NumberOfAverages;
            }

            // logic to minimize measurements if spectral regions are outside desired wavelength range
            for (int i = 0; i < spectralRegionPods1.Length; i++)
            {
                var pod = spectralRegionPods1[i];
                pod.ShouldMeasure = true;
                if (_lowerWavelength > pod.CutoffHigh + pod.Bandwidth)
                {
                    pod.ShouldMeasure = false;
                }
                if (_upperWavelength < pod.CutoffLow - pod.Bandwidth)
                {
                    pod.ShouldMeasure = false;
                }
            }
            for (int i = 0; i < spectralRegionPods2.Length; i++)
            {
                var pod = spectralRegionPods2[i];
                pod.ShouldMeasure = true;
                if (_lowerWavelength > pod.CutoffHigh + pod.Bandwidth)
                {
                    pod.ShouldMeasure = false;
                }
                if (_upperWavelength < pod.CutoffLow - pod.Bandwidth)
                {
                    pod.ShouldMeasure = false;
                }
            }

            for (int i = 0; i < spectralRegionPods1.Length; i++)
            {
                eventLogger.LogEvent($"Spectral region pod(AB..) {i + 1}: {spectralRegionPods1[i]}");
            }
            for (int i = 0; i < spectralRegionPods2.Length; i++)
            {
                eventLogger.LogEvent($"Spectral region pod(..BA) {i + 1}: {spectralRegionPods2[i]}");
            }
            #endregion

            #region Get optimal integration times for each spectral region and take reference and dark spectra AB..
            temperature.Update(); // every now and then update temperature reading
            UIHelper.WriteMessageAndWait(
                "\n=================================================================\n" +
                "Switch on lamp and remove any samples from photometer.\n" +
                "After warmup press any key to continue.\n" +
                "=================================================================\n");

            eventLogger.LogEvent("Determining optimal exposure time ...");
            for (int i = 0; i < spectralRegionPods1.Length; i++)
            {
                ObtainOptimalExposureTimeAndReferenceSpectrum(spectralRegionPods1[i]);
                spectralRegionPods2[i].SetIntegrationTime(spectralRegionPods1[i].IntegrationTime);
            }

            eventLogger.LogEvent($"Measuring dark spectra ...");
            for (int i = 0; i < spectralRegionPods1.Length; i++)
            {
                ObtainDarkSpectrum(spectralRegionPods1[i]);
            }
            #endregion

            #region Measure samples AB..
            for (int sampleIndex = 0; sampleIndex < numSamples; sampleIndex++)
            {
                UIHelper.WriteMessageAndWait(
                    $"\n===========================================================================\n" +
                    $"Insert sample #{sampleIndex + 1} ({sampleInfo.GetSampleName(sampleIndex)}).\n" +
                    $"Afterwards press any key to continue with measurements.\n" +
                    $"===========================================================================\n");
                for (int i = 0; i < spectralRegionPods1.Length; i++)
                {
                    ObtainSampleSpectrum(sampleIndex, spectralRegionPods1[i]);
                }
            }
            #endregion

            #region Make control measurements with blocked and open port
            if (options.ControlMeasurements)
            {
                temperature.Update(); // every now and then update temperature reading
                UIHelper.WriteMessageAndWait(
                    "\n===========================================================================\n" +
                    "Block beam path of photometer. (0 %)\n" +
                    "Afterwards press any key to continue with control measurements.\n" +
                    "===========================================================================\n");
                for (int i = 0; i < spectralRegionPods1.Length; i++)
                {
                    ObtainSampleSpectrum(numSamples, spectralRegionPods1[i]);
                }

                UIHelper.WriteMessageAndWait(
                    "\n===========================================================================\n" +
                    "Remove any samples from photometer. (100 %)\n" +
                    "Afterwards press any key to continue with control measurements.\n" +
                    "===========================================================================\n");
                for (int i = 0; i < spectralRegionPods1.Length; i++)
                {
                    ObtainSampleSpectrum(numSamples + 1, spectralRegionPods1[i]);
                }
            }
            #endregion

            #region Measure samples ..BA
            if (options.Abba)
            {
                for (int sampleIndex = numSamples - 1; sampleIndex >= 0; sampleIndex--)
                {
                    UIHelper.WriteMessageAndWait(
                        $"\n===========================================================================\n" +
                        $"Insert sample #{sampleIndex + 1} ({sampleInfo.GetSampleName(sampleIndex)}).\n" +
                        $"Afterwards press any key to continue with measurements.\n" +
                        $"===========================================================================\n");
                    for (int i = 0; i < spectralRegionPods1.Length; i++)
                    {
                        ObtainSampleSpectrum(sampleIndex, spectralRegionPods2[i]);
                    }
                }
            }
            #endregion

            #region Take refenece and dark spectra ..BA
            eventLogger.LogEvent($"Measuring dark spectra ...");
            for (int i = 0; i < spectralRegionPods2.Length; i++)
            {
                ObtainDarkSpectrum(spectralRegionPods2[i]);
            }
            eventLogger.LogEvent($"Measuring reference spectra ...");
            for (int i = 0; i < spectralRegionPods2.Length; i++)
            {
               //TODO ObtainReferenceSpectrum(spectralRegionPods2[i]);
            }
            #endregion


            List<OpticalSpectrum> sampleTransmissions = new List<OpticalSpectrum>();

            for (int sampleIndex = 0; sampleIndex < numSamples; sampleIndex++)
            {
                var multiPassSpec = CombineSpectralRegionTransmissions(sampleIndex);
                if (multiPassSpec != null)
                {
                    multiPassSpec.DeleteMetaDataRecords();
                    multiPassSpec.AddMetaDataRecord(SampleMetaDataRecords(sampleIndex));
                    sampleTransmissions.Add(multiPassSpec);
                    multiPassSpec.SaveAsResultFile(Path.Combine(dataFolderName, $"Sample{sampleIndex + 1}_{sampleInfo.GetSampleName(sampleIndex)}.csv"));
                }
            }

            if (options.ControlMeasurements)
            {
                // process control measurements
                List<OpticalSpectrum> controlTransmissions = new List<OpticalSpectrum>();
                int blockedIndex = numSamples;
                int openIndex = numSamples + 1;

                var controlBlocked = CombineSpectralRegionTransmissions(blockedIndex);
                if (controlBlocked != null)
                {
                    controlBlocked.DeleteMetaDataRecords();
                    controlBlocked.AddMetaDataRecord(SampleMetaDataRecords("Blocked", "Control sample with 0 % transmission"));
                    controlTransmissions.Add(controlBlocked);
                    controlBlocked.SaveAsResultFile(Path.Combine(dataFolderName, $"SampleControl_Blocked.csv"));
                }

                var controlOpen = CombineSpectralRegionTransmissions(openIndex);
                if (controlOpen != null)
                {
                    controlOpen.DeleteMetaDataRecords();
                    controlOpen.AddMetaDataRecord(SampleMetaDataRecords("Open", "Control sample with 100 % transmission"));
                    controlTransmissions.Add(controlOpen);
                    controlOpen.SaveAsResultFile(Path.Combine(dataFolderName, $"SampleControl_Open.csv"));
                }

                Plotter controlPlotterBlocked = new Plotter(controlTransmissions.ToArray(), _lowerWavelength, _upperWavelength, -2.0, 2.0, 0.5);
                controlPlotterBlocked.SaveTransmissionChart("Control Measurement - Blocked Port", Path.Combine(dataFolderName, "ControlBlockedChart.png"));
                Plotter controlPlotterOpen = new Plotter(controlTransmissions.ToArray(), _lowerWavelength, _upperWavelength, 98.0, 102.0, 0.5);
                controlPlotterOpen.SaveTransmissionChart("Control Measurement - Open Port", Path.Combine(dataFolderName, "ControlOpenChart.png"));
            }

            Console.WriteLine();
            //temperature.Update(); // the very last update !!! DOES NOT RETURN !!!
            if (temperature.HasTemperatureData())
            {
                eventLogger.LogEvent($"Instrument temperature statistics:\n" +
                    $"   First value: {temperature.GetFirstTemperature():F2} °C.\n" +
                    $"   Average:     {temperature.GetTemperatureAverage():F2} °C.\n" +
                    $"   Final value: {temperature.GetLatestTemperature():F2} °C.");
            }

            Console.WriteLine();
            eventLogger.LogEvent("Measurement sequence completed.");
            eventLogger.Close();

            //Plot the sample transmission spectra
            Plotter plotter = new Plotter(sampleTransmissions.ToArray(), _lowerWavelength, _upperWavelength, 0, 100);
            string filePath = Path.Combine(dataFolderName, "SampleTransmissionChart.png");
            plotter.SaveTransmissionChart("Sample Transmission", filePath);
            plotter.ShowTransmissionChart("Sample Transmission");

            Console.WriteLine("done.");
        }

        //========================================================================================================

        private static void ClampWavelengthRange()
        {
            if (_lowerWavelength < spectro.MinimumWavelength)
            {
                _lowerWavelength = (int)spectro.MinimumWavelength + 1;
            }
            if (_upperWavelength > spectro.MaximumWavelength)
            {
                _upperWavelength = (int)spectro.MaximumWavelength - 1;
            }
        }

        //========================================================================================================

        public static OpticalSpectrum CombineSpectralRegionTransmissions(int sampleIndex)
        {
            List<IOpticalSpectrum> regionSpectra = new List<IOpticalSpectrum>();
            for (int i = 0; i < spectralRegionPods1.Length; i++)
            {
                if (spectralRegionPods1[i].ShouldMeasure)
                {
                    regionSpectra.Add(spectralRegionPods1[i].GetMaskedTransmissionSpectrum(sampleIndex));
                }
            }
            if (regionSpectra.Count != 0)
            {
                var combinedRegionSpectrum = SpecMath.Add(regionSpectra.ToArray());
                var resampledRegionSpectrum = combinedRegionSpectrum.ResampleSpectrum(_lowerWavelength, _upperWavelength, _wavelengthStep);
                return resampledRegionSpectrum;
            }
            return null;
        }

        //========================================================================================================

    }
}