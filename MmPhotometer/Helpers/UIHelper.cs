using Bev.Instruments.ArraySpectrometer.Abstractions;
using Bev.Instruments.OpticalShutterLib.Abstractions;
using Bev.Instruments.Thorlabs.FW;
using System;
using System.Text;

namespace MmPhotometer
{
    public static class UIHelper
    {
        public static void WriteMessageAndWait(string message)
        {
            while (Console.KeyAvailable) // Check if any key is pressed
            {
                Console.ReadKey(true); // Read and ignore the key
            }
            Console.WriteLine(message);
            Console.ReadKey(true); // true = do not display the key pressed
        }

        public static bool SkipAction(string message)
        {
            while (Console.KeyAvailable) // Check if any key is pressed
            {
                Console.ReadKey(true); // Read and ignore the key
            }
            Console.WriteLine($"Press any key to {message} - 's' to skip.");
            ConsoleKeyInfo keyInfo = Console.ReadKey(true);
            return keyInfo.Key == ConsoleKey.S;
        }

        public static void DisplaySpectrometerInfo(IArraySpectrometer spectro)
        {
            Console.Write(FormatSpectrometerInfo(spectro));
        }

        public static string FormatSpectrometerInfo(IArraySpectrometer spectro)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Instrument Manufacturer:  {spectro.InstrumentManufacturer}");
            sb.AppendLine($"Instrument Type:          {spectro.InstrumentType}");
            sb.AppendLine($"Instrument Serial Number: {spectro.InstrumentSerialNumber}");
            sb.AppendLine($"Firmware Revision:        {spectro.InstrumentFirmwareVersion}");
            sb.AppendLine($"Min Wavelength:           {spectro.MinimumWavelength:F2} nm");
            sb.AppendLine($"Max Wavelength:           {spectro.MaximumWavelength:F2} nm");
            sb.AppendLine($"Min Integration Time:     {spectro.MinimumIntegrationTime} s");
            sb.AppendLine($"Max Integration Time:     {spectro.MaximumIntegrationTime} s");
            sb.AppendLine($"Pixel Number:             {spectro.Wavelengths.Length}");
            sb.AppendLine($"Set Integration Time:     {spectro.GetIntegrationTime()} s");
            return sb.ToString();
        }

        public static void DisplayShutterInfo(IShutter shutter)
        {
            Console.WriteLine(FormatShutterInfo(shutter));
        }

        public static string FormatShutterInfo(IShutter shutter)
        {
            return $"Shutter Name:             {shutter.Name}";
        }

        public static void DisplayFilterWheelInfo(IFilterWheel filterWheel)
        {
            Console.Write(FormatFilterWheelInfo(filterWheel));
        }

        public static string FormatFilterWheelInfo(IFilterWheel filterWheel)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Filter Wheel Name:        {filterWheel.Name}");
            sb.AppendLine($"Number of Positions:      {filterWheel.FilterCount}");
            return sb.ToString();
        }
    }
}
