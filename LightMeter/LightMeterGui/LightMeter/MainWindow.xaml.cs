using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using DSPLib;
using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Series;

namespace LightMeter
{
    public partial class MainWindow
    {
        private const string PortName = "COM3";
        private const int BaudRate = 2000000;
        private readonly PlotModel _signalPlotModel;
        private readonly PlotModel _fourierPlotModel;
        private readonly LineSeries _signalSeries;
        private readonly LineSeries _filteredSeries;
        private byte[] _signal;

        public MainWindow()
        {
            InitializeComponent();

            _signalPlotModel = new PlotModel();
            _signalSeries = new LineSeries
            {
                StrokeThickness = 0.75,
                Color = OxyColor.FromRgb(Colors.Green.R, Colors.Green.G, Colors.Green.B)
            };
            _filteredSeries = new LineSeries
            {
                Color = OxyColor.FromRgb(Colors.Red.R, Colors.Red.G, Colors.Red.B),
            };
            _signalPlotModel.Series.Add(_signalSeries);
            _signalPlotModel.Series.Add(_filteredSeries);

            SignalPlotView.Model = _signalPlotModel;

            _fourierPlotModel = new PlotModel();

            FourierPlotView.Model = _fourierPlotModel;
        }

        private void FilterSignal()
        {
            _filteredSeries.Points.Clear();
            var x = 0;
            var filter = new Kalman();
            foreach (var p in _signal.Skip(1))
            {
                var dout = filter.GetValue(p);

                _filteredSeries.Points.Add(new DataPoint(x++, dout));
            }
        }

        private void DrawSignal(bool updatePlot = false)
        {
            _signalSeries.Points.Clear();
            var x = 0;
            _signalSeries.Points.AddRange(_signal.Skip(1).Select(bt => new DataPoint(x++, bt)));

            if (updatePlot)
            {
                _signalPlotModel.ResetAllAxes();
                _signalPlotModel.InvalidatePlot(true);
            }
        }

        private static async Task<byte[]> GetSignalFromArduino()
        {
            return await Task.Run(async () =>
            {
                using (var port = new SerialPort(PortName, BaudRate))
                {
                    port.Open();
                    port.Write(new byte[] {24}, 0, 1);
                    var samplesNumber = 16000;
                    const int readSize = 2048;
                    var buffer = new byte[readSize];
                    using (var memoryStream = new MemoryStream())
                    {
                        while (samplesNumber > 0)
                        {
                            var bytesRead = await port.BaseStream.ReadAsync(buffer, 0, readSize);

                            if (bytesRead > 0)
                            {
                                samplesNumber -= bytesRead;
                                await memoryStream.WriteAsync(buffer, 0, bytesRead);
                            }
                        }
                        memoryStream.Seek(0, SeekOrigin.Begin);

                        var array = memoryStream.ToArray();
                        port.Close();

                        return array;
                    }
                }
            });
        }

        private async void GetFromArduinoBtn_OnClick(object sender, RoutedEventArgs e)
        {
            _signal = await GetSignalFromArduino();

            DrawSignal(true);
        }

        private void FilterKalmanBtn_OnClick(object sender, RoutedEventArgs e)
        {
            FilterSignal();

            _signalPlotModel.InvalidatePlot(true);
        }

        private async void SaveSignalBtn_OnClick(object sender, RoutedEventArgs e)
        {
            if (_signal == null)
            {
                MessageBox.Show("No signal", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dlg = new SaveFileDialog
            {
                InitialDirectory = GetType().Assembly.CodeBase,
                FileName = "signal.bin",
            };
            if (dlg.ShowDialog(this) == true)
            {
                try
                {
                    var formatter = new BinaryFormatter();
                    using (var serializationStream = File.Create(dlg.FileName))
                    {
                        formatter.Serialize(serializationStream, _signal);
                        await serializationStream.FlushAsync();
                        serializationStream.Close();
                    }
                }
                catch (Exception exception)
                {
                    MessageBox.Show($"Error saving signal to file:\r\n{exception}", "Error", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Console.WriteLine(exception);
                }
            }
        }

        private void LoadSignalBtn_OnClick(object sender, RoutedEventArgs e)
        {
            if (_signal != null)
            {
                if (MessageBox.Show("Existing signal will be lost. Continue?", "Warning", MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;
            }

            var dlg = new OpenFileDialog
            {
                InitialDirectory = GetType().Assembly.CodeBase,
                FileName = "signal.bin",
            };
            if (dlg.ShowDialog(this) == true)
            {
                try
                {
                    var formatter = new BinaryFormatter();
                    using (var serializationStream = File.OpenRead(dlg.FileName))
                    {
                        _signal = (byte[]) formatter.Deserialize(serializationStream);
                        serializationStream.Close();
                        DrawSignal(true);
                    }
                }
                catch (Exception exception)
                {
                    MessageBox.Show($"Error reading signal from file:\r\n{exception}", "Error", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Console.WriteLine(exception);
                }
            }
        }

        private void FftBtn_OnClick(object sender, RoutedEventArgs e)
        {
            var data = _signal.Skip(1).Select(i => (double) i).ToArray();

            var x = 0;
            var filter = new Kalman();
            for (var index = 0; index < data.Length; index++)
            {
                data[index] = filter.GetValue(data[index]);
            }

            var dft = new DFT();
            dft.Initialize((uint) data.Length, 0, true);
            var dftResComplex = dft.Execute(data);
            var dftRes = DSP.ConvertComplex.ToMagnitude(dftResComplex);
            var freqSpan = dft.FrequencySpan(15625);

            var fourierSeries = new LineSeries();

            const int thresholdFreq = 10;
            const double thresholdMagnitude = 1;

            FreqListBox.Items.Clear();

            var freqWithMagnitudes = freqSpan
                .Select((i, idx) => new {freq = i, magnitude = dftRes[idx]})
                .Where(i => i.freq > thresholdFreq)
                .ToList();

            var max = freqWithMagnitudes.Max(i => i.magnitude);
            var perc = 100 / max;

            /*freqWithMagnitudes = freqWithMagnitudes
                .Where(i => i.magnitude > thresholdMagnitudeAbs)
                .ToList();*/

            /*foreach (var d in freqWithMagnitudes.OrderByDescending(i => i.magnitude))
            {
                FreqListBox.Items.Add($"{d.freq:F2} Hz ({perc * d.magnitude:F2} %)");
            }*/

            var peaks = FindPeaks(freqWithMagnitudes, i => i.magnitude, 10);

            foreach (var d in peaks.OrderByDescending(i => i.magnitude))
            {
                FreqListBox.Items.Add($"{d.freq:F2} Hz ({perc * d.magnitude:F2} %)");
            }

            var fourierApproximatedSeries = new LineSeries
            {
                Color = OxyColor.FromRgb(Colors.Blue.R, Colors.Blue.G, Colors.Blue.B),
            };

            var peakSeries = new ScatterSeries();

            foreach (var d in peaks)
                peakSeries.Points.Add(new ScatterPoint(d.freq, d.magnitude, 3));

            foreach (var d in freqWithMagnitudes)
                fourierSeries.Points.Add(new DataPoint(d.freq, d.magnitude));

            var allPeaks = FindPeaks(freqWithMagnitudes, i => i.magnitude, 0);

            foreach (var d in allPeaks)
            {
                fourierApproximatedSeries.Points.Add(new DataPoint(d.freq, d.magnitude));
            }

            _fourierPlotModel.Series.Clear();
            _fourierPlotModel.Series.Add(fourierSeries);
            _fourierPlotModel.Series.Add(peakSeries);
            _fourierPlotModel.Series.Add(fourierApproximatedSeries);

            _fourierPlotModel.InvalidatePlot(true);
        }

        private static IList<T> FindPeaks<T>(IList<T> values, Func<T, double> selector, int thresholdPercentage)
            where T : class
        {
            var max = values.Max(selector);
            var threshold = max / 100 * thresholdPercentage;

            var peaks = new List<T>();
            
            for (var i = 0; i < values.Count; i++)
            {
                var current = values[i];
                var prev = i <= 0 ? null : values[i - 1];
                var next = i == values.Count - 1 ? null : values[i + 1];
                /*if (((dynamic) current).freq > 98)
                {
                    var a = DateTime.Now;
                }*/
                if (prev != null)
                {
                    if (next == null)
                    {
                        if (selector(current) - selector(prev) > threshold)
                            peaks.Add(current);
                    }
                    else
                    {
                        if (selector(current) - selector(prev) > threshold && selector(current) > selector(next) ||
                            selector(current) > selector(prev) && selector(current) - selector(next) > threshold)
                            peaks.Add(current);
                    }
                }
                else
                {
                    if (next != null)
                    {
                        if (selector(current) - selector(next) > threshold)
                            peaks.Add(current);
                    }
                }
            }
            return peaks;
        }
    }

    public class Kalman
    {
        // kalman variables
        private double varVolt = 1e-08;

        private double varProcess = 0.8e-8;
        private double Pc = 0.0;
        private double G = 0.0;
        private double P = 1.0;
        private double Xp = 0.0;
        private double Zp = 0.0;
        private double Xe = 0.0;

        public double GetValue(double value)
        {
            // kalman process
            Pc = P + varProcess;
            G = Pc / (Pc + varVolt); // kalman gain
            P = (1 - G) * Pc;
            Xp = Xe;
            Zp = Xp;
            Xe = G * (value - Zp) + Xp; // the kalman estimate of the sensor voltage

            return Xe;
        }
    }
}