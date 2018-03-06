using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace NoteKeeper
{
    public partial class MainWindow
    {
        private readonly MainVM _mainVm;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = _mainVm = new MainVM();
        }

        private void FrameworkElement_OnLoaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => { ((TextBox) sender).Focus(); }));
        }
    }
}