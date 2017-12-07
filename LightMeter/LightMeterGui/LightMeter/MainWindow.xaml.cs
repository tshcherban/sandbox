using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using OxyPlot;
using OxyPlot.Series;

namespace LightMeter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SerialPort p;

        public MainWindow()
        {
            InitializeComponent();

            Task.Run(() =>
            {
                p = new SerialPort("COM3", 2000000);
                p.Open();
                while (true)
                {
                    var toRead = 16000;
                    const int readSize = 2048;
                    var bf = new byte[readSize];
                    using (var mst = new MemoryStream())
                    {
                        while (toRead > 0)
                        {
                            var read = p.BaseStream.Read(bf, 0, readSize);
                            toRead -= read;

                            if (read > 0)
                            {
                                mst.Write(bf, 0, read);
                            }
                        }
                        mst.Seek(0, SeekOrigin.Begin);

                        var array = mst.ToArray();

                        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                        {
                            var m = new PlotModel();
                            var lineSeries = new LineSeries();
                            var x = 0;
                            lineSeries.Points.AddRange(array.Skip(1).Select(bt=>new DataPoint(x++, bt)));
                            m.Series.Add(lineSeries);

                            var series1 = new LineSeries()
                            {
                                Color = OxyColor.FromRgb(Colors.Red.R, Colors.Red.G, Colors.Red.B)
                            };

                            x = 0;
                            var klm = new Kalman();
                            foreach (var p in array.Skip(1))
                            {
                                var dout = klm.GetValue(p);

                                series1.Points.Add(new DataPoint(x++, dout));
                            }
                            m.Series.Add(series1);
                            PlotView1.Model = m;
                        }));
                    }

                    System.Threading.Thread.Sleep(10);
                }
            });
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            p.Write(new byte[]{24}, 0, 1);
        }
    }

    public class Kalman
    {
        // kalman variables
        double varVolt = 1e-06;
        double varProcess = 0.8e-8;
        double Pc = 0.0;
        double G = 0.0;
        double P = 1.0;
        double Xp = 0.0;
        double Zp = 0.0;
        double Xe = 0.0;

        public double GetValue(double value)
        {
            // kalman process
            Pc = P + varProcess;
            G = Pc / (Pc + varVolt);    // kalman gain
            P = (1 - G) * Pc;
            Xp = Xe;
            Zp = Xp;
            Xe = G * (value - Zp) + Xp;   // the kalman estimate of the sensor voltage

            return Xe;
        }
    }
}
