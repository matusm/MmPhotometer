using System.Collections.Generic;

namespace MmPhotometer
{
    public partial class Program
    {
        public static SpectralRegionPod[] SetupPods(MeasurementMode mode, int numSamples, double maxIntTime)
        {
            double commonBandWidth = 10.0; // nm

            List<SpectralRegionPod> pods = new List<SpectralRegionPod>();

            switch (mode)
            {
                case MeasurementMode.SinglePass:
                    pods.Add(new SpectralRegionPod(numSamples, FilterPosition.OpenPort, maxIntTime, 100, 2000, commonBandWidth));
                    break;
                case MeasurementMode.TwoPass:
                    pods.Add(new SpectralRegionPod(numSamples, FilterPosition.FilterA, maxIntTime, 100, 464, commonBandWidth));
                    pods.Add(new SpectralRegionPod(numSamples, FilterPosition.OpenPort, maxIntTime, 464, 2000, commonBandWidth));
                    break;
                case MeasurementMode.ThreePass:
                    pods.Add(new SpectralRegionPod(numSamples, FilterPosition.FilterA, maxIntTime, 100, 464, commonBandWidth));
                    pods.Add(new SpectralRegionPod(numSamples, FilterPosition.OpenPort, maxIntTime, 464, 875, commonBandWidth));
                    pods.Add(new SpectralRegionPod(numSamples, FilterPosition.FilterE, maxIntTime, 875, 2000, commonBandWidth));
                    break;
                case MeasurementMode.FourPass:
                    pods.Add(new SpectralRegionPod(numSamples, FilterPosition.FilterA, maxIntTime, 100, 464, commonBandWidth));
                    pods.Add(new SpectralRegionPod(numSamples, FilterPosition.FilterB, maxIntTime, 464, 545, commonBandWidth));
                    pods.Add(new SpectralRegionPod(numSamples, FilterPosition.FilterC, maxIntTime, 545, 685, commonBandWidth));
                    pods.Add(new SpectralRegionPod(numSamples, FilterPosition.FilterD, maxIntTime, 685, 2000, commonBandWidth));
                    break;
                case MeasurementMode.FivePass:
                    pods.Add(new SpectralRegionPod(numSamples, FilterPosition.FilterA, maxIntTime, 100, 464, commonBandWidth));
                    pods.Add(new SpectralRegionPod(numSamples, FilterPosition.FilterB, maxIntTime, 464, 545, commonBandWidth));
                    pods.Add(new SpectralRegionPod(numSamples, FilterPosition.FilterC, maxIntTime, 545, 685, commonBandWidth));
                    pods.Add(new SpectralRegionPod(numSamples, FilterPosition.FilterD, maxIntTime, 685, 875, commonBandWidth));
                    pods.Add(new SpectralRegionPod(numSamples, FilterPosition.FilterE, maxIntTime, 875, 2000, commonBandWidth));
                    break;
                default:
                    break;
            }

            return pods.ToArray();
        }
    }
}
