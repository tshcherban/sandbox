﻿using System;
using System.Collections.Generic;
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
                        var ll = mst.Length;

                        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                        {
                            var m = new PlotModel();
                            var lineSeries = new LineSeries();
                            int x = 0;
                            lineSeries.Points.AddRange(mst.ToArray().Skip(1).Select(bt=>new DataPoint(x++, bt)));
                            m.Series.Add(lineSeries);
                            
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
}
