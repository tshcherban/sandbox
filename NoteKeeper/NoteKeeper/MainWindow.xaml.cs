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
            _mainVm.Notes.Add(new NoteVM
            {
                Header = "Note 1",
                Content = "Note 1 contents",
                Tags =
                {
                    new TagVM {Header = "Tag 1"},
                }
            });
            _mainVm.Notes.Add(new NoteVM
            {
                Header = "Note 2",
                Content = "Note 2 contents",
                Tags =
                {
                    new TagVM {Header = "Tag 1"},
                    new TagVM {Header = "Tag 2"},
                }
            });
        }

        private void FrameworkElement_OnLoaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => { ((TextBox) sender).Focus(); }));
        }
    }
}