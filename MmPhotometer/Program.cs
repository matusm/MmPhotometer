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
        private static SpectralRegionPod[] spectralRegionPodsA; // for AB.. of ABBA
        private static SpectralRegionPod[] spectralRegionPodsB; // for ..BA of ABBA
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

        //========================================================================================================

        private static void Run(Options options)
        {
            _options = options; // to make options available outside of Run()
            eventLogger = new EventLogger(options.BasePath, options.LogFileName);
            filterWheel = new ManualFilterWheel();
            //filterWheel = new NullFilterWheel();
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

            SetWavelengthRange();

            temperature.Update(); // every now and then update temperature reading

            // get sample description from input file
            sampleInfo = new SampleInfo(options.InputPath);
            int numSamples = sampleInfo.NumberOfSamples;
            int numSamplesWithControls = options.ControlMeasurements ? numSamples + 2 : numSamples;
            int indexForControlBlocked = numSamples;
            int indexForControlBlank = numSamples + 1;

            dataFolderName = eventLogger.LogDirectory;
            rawDataFolderName = Path.Combine(dataFolderName, "RawSpectra");
            Directory.CreateDirectory(rawDataFolderName);
            eventLogger.LogEvent($"Program: {GetAppNameAndVersion()}");
            eventLogger.LogEvent($"User comment: {options.UserComment}");
            eventLogger.LogEvent($"Measurement mode: {options.Mode.ToFriendlyString()}");
            LogSetupInfo();

            #region Setup spectral region pods as arrays

            spectralRegionPodsA = SetupPods(options.Mode, numSamplesWithControls, options.MaxIntTime); // for AB.. of ABBA
            spectralRegionPodsB = SetupPods(options.Mode, numSamplesWithControls, options.MaxIntTime); // for ..BA of ABBA
            for (int i = 0; i < spectralRegionPodsA.Length; i++)
            {
                spectralRegionPodsA[i].SetIntegrationTime(spectro.MinimumIntegrationTime);
                spectralRegionPodsA[i].NumberOfAverages = options.NumberOfAverages;
                spectralRegionPodsA[i].Name = $"A";
            }
            for (int i = 0; i < spectralRegionPodsB.Length; i++)
            {
                spectralRegionPodsB[i].SetIntegrationTime(spectro.MinimumIntegrationTime);
                spectralRegionPodsB[i].NumberOfAverages = options.NumberOfAverages;
                spectralRegionPodsB[i].Name = $"B";
            }

            // logic to minimize measurements if spectral regions are outside desired wavelength range
            for (int i = 0; i < spectralRegionPodsA.Length; i++)
            {
                var pod = spectralRegionPodsA[i];
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
            for (int i = 0; i < spectralRegionPodsB.Length; i++)
            {
                var pod = spectralRegionPodsB[i];
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

            for (int i = 0; i < spectralRegionPodsA.Length; i++)
            {
                eventLogger.LogEvent($"Spectral region pod {i + 1}: {spectralRegionPodsA[i]}");
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
            for (int i = 0; i < spectralRegionPodsA.Length; i++)
            {
                ObtainOptimalExposureTimeAndReferenceSpectrum(spectralRegionPodsA[i]);
                spectralRegionPodsB[i].SetIntegrationTime(spectralRegionPodsA[i].IntegrationTime);
            }

            eventLogger.LogEvent($"Measuring dark spectra ...");
            for (int i = 0; i < spectralRegionPodsA.Length; i++)
            {
                ObtainDarkSpectrum(spectralRegionPodsA[i]);
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
                for (int i = 0; i < spectralRegionPodsA.Length; i++)
                {
                    ObtainSampleSpectrum(sampleIndex, spectralRegionPodsA[i]);
                }
            }
            #endregion

            #region Make control measurements with blocked and open port
            // TODO: handle ABBA
            if (options.ControlMeasurements)
            {
                temperature.Update(); // every now and then update temperature reading
                UIHelper.WriteMessageAndWait(
                    "\n===========================================================================\n" +
                    "Block beam path of photometer. (0 %)\n" +
                    "Afterwards press any key to continue with control measurements.\n" +
                    "===========================================================================\n");
                for (int i = 0; i < spectralRegionPodsA.Length; i++)
                {
                    ObtainSampleSpectrum(numSamples, spectralRegionPodsA[i]);
                }
                UIHelper.WriteMessageAndWait(
                    "\n===========================================================================\n" +
                    "Remove any samples from photometer. (100 %)\n" +
                    "Afterwards press any key to continue with control measurements.\n" +
                    "===========================================================================\n");
                for (int i = 0; i < spectralRegionPodsA.Length; i++)
                {
                    ObtainSampleSpectrum(numSamples + 1, spectralRegionPodsA[i]);
                }

                if(options.Abba)
                {
                    temperature.Update();
                    UIHelper.WriteMessageAndWait(
                        "\n===========================================================================\n" +
                        "Remove any samples from photometer. (100 %)\n" +
                        "Afterwards press any key to continue with control measurements.\n" +
                        "===========================================================================\n");
                    for (int i = 0; i < spectralRegionPodsB.Length; i++)
                    {
                        ObtainSampleSpectrum(numSamples + 1, spectralRegionPodsB[i]);
                    }
                    UIHelper.WriteMessageAndWait(
                        "\n===========================================================================\n" +
                        "Block beam path of photometer. (0 %)\n" +
                        "Afterwards press any key to continue with control measurements.\n" +
                        "===========================================================================\n");
                    for (int i = 0; i < spectralRegionPodsB.Length; i++)
                    {
                        ObtainSampleSpectrum(numSamples, spectralRegionPodsB[i]);
                    }
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
                    for (int i = 0; i < spectralRegionPodsA.Length; i++)
                    {
                        ObtainSampleSpectrum(sampleIndex, spectralRegionPodsB[i]);
                    }
                }
            }
            #endregion

            #region Take reference and dark spectra ..BA
            if (options.Abba)
            {
                UIHelper.WriteMessageAndWait(
                        "\n===========================================================================\n" +
                        "Remove any samples from photometer.\n" +
                        "Afterwards press any key to continue.\n" +
                        "===========================================================================\n");
                eventLogger.LogEvent($"Measuring dark spectra ...");
                for (int i = 0; i < spectralRegionPodsB.Length; i++)
                {
                    ObtainDarkSpectrum(spectralRegionPodsB[i]);
                }
                eventLogger.LogEvent($"Measuring reference spectra ...");
                for (int i = 0; i < spectralRegionPodsB.Length; i++)
                {
                    ObtainReferenceSpectrum(spectralRegionPodsB[i]);
                }
            }
            #endregion

            OpticalSpectrum[] samplesA = new OpticalSpectrum[numSamplesWithControls];
            OpticalSpectrum[] samplesB = new OpticalSpectrum[numSamplesWithControls];

            for (int sampleIndex = 0; sampleIndex < numSamplesWithControls; sampleIndex++)
            {
                var specA = CombineSpectralRegionTransmissionsA(sampleIndex);
                specA.SaveAsSimpleCsvFile(Path.Combine(rawDataFolderName, $"3_Sample{sampleIndex + 1}_A_{sampleInfo.GetSampleName(sampleIndex)}.csv"));
                samplesA[sampleIndex] = specA;
                if (options.Abba)
                {
                    var specB = CombineSpectralRegionTransmissionsB(sampleIndex);
                    specB.SaveAsSimpleCsvFile(Path.Combine(rawDataFolderName, $"3_Sample{sampleIndex + 1}_B_{sampleInfo.GetSampleName(sampleIndex)}.csv"));
                    samplesB[sampleIndex] = specB;
                }
            }

            // get final results and save them
            List<OpticalSpectrum> resultCollectionForPlot = new List<OpticalSpectrum>();
            List<OpticalSpectrum> controlCollectionForPlot = new List<OpticalSpectrum>();
            for (int sampleIndex = 0; sampleIndex < numSamplesWithControls; sampleIndex++)
            {
                OpticalSpectrum result = samplesA[sampleIndex];
                result.DeleteMetaDataRecords();
                result.AddMetaDataRecord(SampleMetaDataRecords(sampleIndex));
                result.AddMetaDataRecord("MeasurementPass", "singlePass");
                if (options.Abba)
                {
                    result = SpecMath.Average(samplesA[sampleIndex], samplesA[sampleIndex]);
                    result.DeleteMetaDataRecords();
                    result.AddMetaDataRecord(SampleMetaDataRecords(sampleIndex));
                    result.AddMetaDataRecord("MeasurementPass", "ABBA");
                }
                if(sampleIndex >= numSamples)
                { 
                    string design = string.Empty;
                    if (sampleIndex == indexForControlBlocked)
                    {
                        design = "blocked";
                        result.AddMetaDataRecord(SampleMetaDataRecords("Blocked", "Control sample with 0 % transmission"));
                    }
                    if (sampleIndex == indexForControlBlank) 
                    {
                        design = "blank";
                        result.AddMetaDataRecord(SampleMetaDataRecords("Open", "Control sample with 100 % transmission"));
                    }
                    result.SaveAsResultFile(Path.Combine(dataFolderName, $"Control_{design}.csv"));
                    controlCollectionForPlot.Add(result);
                }
                if (sampleIndex < numSamples)
                {
                    result.SaveAsResultFile(Path.Combine(dataFolderName, $"Sample{sampleIndex + 1}_{sampleInfo.GetSampleName(sampleIndex)}.csv"));
                    resultCollectionForPlot.Add(result);
                }
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

            if (options.ControlMeasurements)
            {
                Plotter controlPlotterBlocked = new Plotter(controlCollectionForPlot.ToArray(), _lowerWavelength, _upperWavelength, -2.0, 2.0, 0.5);
                controlPlotterBlocked.SaveTransmissionChart("Control Measurement - Blocked Port", Path.Combine(dataFolderName, "ControlBlockedChart.png"));
                Plotter controlPlotterOpen = new Plotter(controlCollectionForPlot.ToArray(), _lowerWavelength, _upperWavelength, 98.0, 102.0, 0.5);
                controlPlotterOpen.SaveTransmissionChart("Control Measurement - Open Port", Path.Combine(dataFolderName, "ControlBlankChart.png"));
            }

            Console.WriteLine();
            eventLogger.LogEvent("Measurement sequence completed.");
            eventLogger.Close();

            //Plot the sample transmission spectra
            Plotter plotter = new Plotter(resultCollectionForPlot.ToArray(), _lowerWavelength, _upperWavelength, 0, 100);
            string filePath = Path.Combine(dataFolderName, "SampleTransmissionChart.png");
            plotter.SaveTransmissionChart("Sample Transmission", filePath);
            plotter.ShowTransmissionChart("Sample Transmission");

            Console.WriteLine("done.");
        }

        //========================================================================================================

        private static void SetWavelengthRange()
        {
            _lowerWavelength = _options.LowerBound;
            _upperWavelength = _options.UpperBound;
            _wavelengthStep = _options.StepSize;
            ClampWavelengthRange();
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

        public static OpticalSpectrum CombineSpectralRegionTransmissionsA(int sampleIndex)
        {
            List<IOpticalSpectrum> regionSpectra = new List<IOpticalSpectrum>();
            for (int i = 0; i < spectralRegionPodsA.Length; i++)
            {
                if (spectralRegionPodsA[i].ShouldMeasure)
                {
                    regionSpectra.Add(spectralRegionPodsA[i].GetMaskedTransmissionSpectrum(sampleIndex));
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

        public static OpticalSpectrum CombineSpectralRegionTransmissionsB(int sampleIndex)
        {
            List<IOpticalSpectrum> regionSpectra = new List<IOpticalSpectrum>();
            for (int i = 0; i < spectralRegionPodsB.Length; i++)
            {
                if (spectralRegionPodsB[i].ShouldMeasure)
                {
                    regionSpectra.Add(spectralRegionPodsB[i].GetMaskedTransmissionSpectrum(sampleIndex));
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