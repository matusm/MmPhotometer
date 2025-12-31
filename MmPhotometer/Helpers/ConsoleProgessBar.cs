using System;

namespace MmPhotometer.Helpers
{
    public class ConsoleProgressBar
    {
        private readonly int _width;
        private readonly string _left = "[";
        private readonly string _right = "]";

        public ConsoleProgressBar(int width = 68)
        {
            _width = Math.Max(10, width);
        }

        public void Report(int current, int total)
        {
            if (total <= 0)
            {
                Report(0.0);
                return;
            }
            double percent = ((double)current / (double)total) * 100.0;
            Report(percent);
        }

        public void Report(double percent) // percent: 0.0..100.0
        {
            percent = Math_Clamp(percent, 0.0, 100.0);
            int filled = (int)Math.Round((percent / 100.0) * _width);
            string hashes = new string('#', filled);
            string spaces = new string(' ', _width - filled);
            string pctText = $"{percent,6:0.0} %"; // formats like " 100.0%"

            // Build bar similar to: [####################..........]  42.3%
            string bar = $"{_left}{hashes}{spaces}{_right} {pctText}";

            Console.Write("\r" + bar);
            if (percent >= 100.0)
            {
                Console.WriteLine();
            }
        }

        private static double Math_Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
