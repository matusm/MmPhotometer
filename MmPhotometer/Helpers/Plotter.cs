using At.Matus.OpticalSpectrumLib;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace MmPhotometer
{
    public class Plotter
    {
        private static readonly int _chartWidth = 1000;
        private static readonly int _chartHeight = 600;
        private readonly IOpticalSpectrum[] _spectra;
        private readonly double _startWl;
        private readonly double _stopWl;
        private readonly double _startTr;
        private readonly double _stopTr;

        public Plotter(IOpticalSpectrum[] spectra, double startWl, double stopWl, double startTr = 0, double stopTr = 100)
        {
            _spectra = spectra;
            _startWl = startWl;
            _stopWl = stopWl;
            _startTr = startTr;
            _stopTr = stopTr;
        }

        private Form CreateTransmissionChartForm(string titleText)
        {
            int formWidth = _chartWidth + 30;
            int formHeight = _chartHeight + 30;
            Form form = new Form();
            Chart chart = new Chart();
            ChartArea chartArea = new ChartArea();
            Series[] series = new Series[_spectra.Length];
            Title title = new Title();
            Label label = new Label();
            form.Controls.Add(chart);
            form.Text = "Filter Transmission Plot";
            form.Size = new Size(formWidth, formHeight);
            title.Text = titleText;
            title.Font = new Font("Arial", 14, FontStyle.Bold);
            chart.ChartAreas.Add(chartArea);
            chart.Titles.Add(title);
            chart.Dock = DockStyle.Fill;
            for (int index = 0; index < series.Length; index++)
            {
                series[index] = new Series();
                chart.Series.Add(series[index]);
                series[index].Points.DataBindXY(_spectra[index].Wavelengths, _spectra[index].Signals);
                series[index].Name = $"Sample {index + 1}";
                series[index].Color = Color.FromArgb((index * 70) % 256, (index * 130) % 256, (index * 200) % 256);
            }
            foreach (var s in chart.Series)
            {
                s.ChartType = SeriesChartType.Line;
                s.MarkerSize = 5;
                s.BorderWidth = 3;
                s.XValueType = ChartValueType.Int32;
                s.IsVisibleInLegend = true;
                s.LegendText = s.Name;
            }
            // x-Axis settings
            chartArea.Axes[0].Title = "Wavelength / nm";
            chartArea.Axes[0].TitleFont = new Font("Arial", 12, FontStyle.Regular);
            chartArea.Axes[0].Minimum = _startWl;
            chartArea.Axes[0].Maximum = _stopWl;
            chartArea.Axes[0].Interval = 50;
            chartArea.Axes[0].MajorGrid.Interval = 50;
            chartArea.Axes[0].MajorTickMark.Interval = 50;
            // y-Axis settings
            chartArea.Axes[1].Title = "Transmission / %";
            chartArea.Axes[1].TitleFont = new Font("Arial", 12, FontStyle.Regular);
            chartArea.Axes[1].Minimum = _startTr;
            chartArea.Axes[1].Maximum = _stopTr;
            chartArea.Axes[1].Interval = 10;
            chartArea.Axes[1].MajorGrid.Interval = 10;
            chartArea.Axes[1].MajorTickMark.Interval = 10;
            return form;
        }

        public void ShowTransmissionChart(string titleText)
        {
            Form form = CreateTransmissionChartForm(titleText);
            form.ShowDialog();
        }

        public void SaveTransmissionChart(string titleText, string filePath)
        {
            Form form = CreateTransmissionChartForm(titleText);
            form.FormBorderStyle = FormBorderStyle.None;
            form.Show();
            form.Update();
            form.PerformLayout();
            Thread.Sleep(100); // Allow time for the form to render
            Bitmap bitmap = new Bitmap(form.ClientSize.Width, form.ClientSize.Height);
            form.DrawToBitmap(bitmap, new Rectangle(0, 0, form.ClientSize.Width, form.ClientSize.Height));
            form.Close();
            // Save the bitmap to a file as PNG
            bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
        }

    }
}
