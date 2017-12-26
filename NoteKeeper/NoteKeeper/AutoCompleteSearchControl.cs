using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Enumerable = System.Linq.Enumerable;

namespace NoteKeeper
{
    public sealed class AutoCompleteSearchControl : Control, INotifyPropertyChanged
    {
        public class SearchItem : ObservableObject
        {
            public SearchItem(string fullPath)
            {
                FullPath = fullPath;
                Path = ExtractPath(fullPath);
                Name = ExtractName(fullPath);
            }

            private string ExtractName(string fullPath)
            {
                if (fullPath.Contains("\\"))
                    return FullPath.Substring(FullPath.LastIndexOf('\\') + 1);
                return fullPath;
            }

            private string ExtractPath(string fullPath)
            {
                if (fullPath.Contains("\\"))
                    return FullPath.Substring(0, FullPath.LastIndexOf('\\'));
                return string.Empty;
            }

            public string Name { get; set; }
            public string Path { get; set; }
            public string FullPath { get; set; }

            public override string ToString()
            {
                return Name;
            }
        }

        #region Fields

        private ListView _filteredList;
        private string _filteringString;
        private ObservableCollection<SearchItem> _filteredStrings;
        private SearchItem _selectedFilteredItem;
        private TextBox _textBox;
        private ICommand _buttonCommand;
        private string _foundResultMessage;
        private List<SearchItem> _searchItemsSource;
        private double _verticalOffset;
        private double _horizontalOffset;

        #endregion

        #region DependencyProperties

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IEnumerable<string>), typeof(AutoCompleteSearchControl),
                new PropertyMetadata(null));


        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command", typeof(ICommand), typeof(AutoCompleteSearchControl),
                new PropertyMetadata(null));


        public static readonly DependencyProperty HintStringProperty =
            DependencyProperty.Register("HintString", typeof(string), typeof(AutoCompleteSearchControl),
                new PropertyMetadata("Enter name to search:"));

        public static readonly DependencyProperty ItemToSearchProperty =
            DependencyProperty.Register("ItemToSearch", typeof(string), typeof(AutoCompleteSearchControl),
                new PropertyMetadata("item"));

        #endregion

        #region Popup Properties and DP

        public string ItemToSearch
        {
            get => (string) GetValue(ItemToSearchProperty);
            set => SetValue(ItemToSearchProperty, value);
        }

        public UIElement PlacementTarget
        {
            get => (UIElement) GetValue(PlacementTargetProperty);
            set => SetValue(PlacementTargetProperty, value);
        }

        public bool IsOpen
        {
            get => (bool) GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        public PopupAnimation PopupAnimation
        {
            get => (PopupAnimation) GetValue(PopupAnimationProperty);
            set => SetValue(PopupAnimationProperty, value);
        }

        public double VerticalOffset
        {
            get => _verticalOffset;
            set
            {
                _verticalOffset = value;
                OnPropertyChanged(nameof(VerticalOffset));
            }
        }

        public double HorizontalOffset
        {
            get => _horizontalOffset;
            set
            {
                _horizontalOffset = value;
                OnPropertyChanged(nameof(HorizontalOffset));
            }
        }


        public static readonly DependencyProperty PopupAnimationProperty =
            DependencyProperty.Register("PopupAnimation", typeof(PopupAnimation), typeof(AutoCompleteSearchControl),
                new PropertyMetadata(PopupAnimation.Slide));


        public static readonly DependencyProperty PlacementTargetProperty =
            DependencyProperty.Register("PlacementTarget", typeof(UIElement), typeof(AutoCompleteSearchControl),
                new PropertyMetadata(null));


        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register("IsOpen", typeof(bool), typeof(AutoCompleteSearchControl),
                new PropertyMetadata(false, OnIsOpenChanged));

        public AutoCompleteSearchControl()
        {
            _filteredStrings = new ObservableCollection<SearchItem>();
        }

        #endregion

        #region Properties

        public IEnumerable<string> ItemsSource
        {
            get => (IEnumerable<string>) GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public string HintString
        {
            get => (string) GetValue(HintStringProperty);
            set => SetValue(HintStringProperty, value);
        }

        public ICommand Command
        {
            get => (ICommand) GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public string FoundResultMessage
        {
            get => _foundResultMessage;
            set
            {
                _foundResultMessage = value;
                OnPropertyChanged(nameof(FoundResultMessage));
            }
        }


        public string FilteringString
        {
            get => _filteringString;
            set
            {
                _filteringString = value;
                OnFilteringStringChanged();
                OnPropertyChanged(nameof(FilteringString));
            }
        }

        public SearchItem SelectedFilteredItem
        {
            get => _selectedFilteredItem;
            set
            {
                _selectedFilteredItem = value;
                OnPropertyChanged(nameof(SelectedFilteredItem));
            }
        }

        public ObservableCollection<SearchItem> FilteredStrings
        {
            get => _filteredStrings;
            set
            {
                _filteredStrings = value;
                OnPropertyChanged(nameof(FilteredStrings));
            }
        }

        public ICommand ButtonCommand => _buttonCommand ??
                                         (_buttonCommand = new RelayCommand(ExecuteControlCommand,
                                             CanExecuteButtonCommand));

        #endregion

        #region Methods

        private void ExecuteControlCommand(object parameter)
        {
            if (Command != null && Command.CanExecute(parameter))
            {
                Command.Execute(parameter.ToString());
                IsOpen = false;
            }
        }

        private bool CanExecuteButtonCommand(object parameter)
        {
            return Command != null && Command.CanExecute(parameter);
        }

        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as AutoCompleteSearchControl;
            if (control != null && control.IsOpen)
            {
                control.Initialize();
            }
        }

        private void Initialize()
        {
            SetPosition();
            InitSearchItems();
            OnFilteringStringChanged();
            if (FilteredStrings.Count > 0)
            {
                SelectedFilteredItem = FilteredStrings[0];
                FocusSelectedFilteredItem();
            }
        }

        private void SetPosition()
        {
            FrameworkElement target = (FrameworkElement) PlacementTarget ?? Application.Current.MainWindow;
            if (target != null)
            {
                var ownWidth = 280;
                var ownHeight = 60;
                HorizontalOffset = (target.ActualWidth / 2 - ownWidth / 2.0);
                VerticalOffset = (target.ActualHeight / 2 - ownHeight / 2.0);
            }
        }

        private void InitSearchItems()
        {
            var temporarySearchItemsList =
                Enumerable.ToList(Enumerable.Select(ItemsSource, path => new SearchItem(path)));
            _searchItemsSource = new List<SearchItem>(Enumerable.OrderBy(temporarySearchItemsList, s => s.Name));
        }

        private void AutoCompleteControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (AreFilteredStringsVisible)
                {
                    ExecuteControlCommand(SelectedFilteredItem);
                }

                IsOpen = false;
            }

            if (e.Key == Key.Escape)
                IsOpen = false;
            if (FilteredStrings.Count == 0)
                return;

            if (e.Key == Key.Down)
            {
                if (SelectedFilteredItem != FilteredStrings[FilteredStrings.Count - 1])
                    SelectedFilteredItem = FilteredStrings[FilteredStrings.IndexOf(SelectedFilteredItem) + 1];
                FocusSelectedFilteredItem();
            }

            if (e.Key == Key.Up)
            {
                if (SelectedFilteredItem != FilteredStrings[0])
                    SelectedFilteredItem = FilteredStrings[FilteredStrings.IndexOf(SelectedFilteredItem) - 1];
                FocusSelectedFilteredItem();
            }

            if (ListScrollViewer == null) return;
            if (e.Key == Key.Home)
            {
                ListScrollViewer.ScrollToLeftEnd();
            }

            if (e.Key == Key.End)
            {
                ListScrollViewer.ScrollToRightEnd();
            }

            if (_filteredList.SelectedItem == null) return;
            if (e.Key == Key.PageUp)
            {
                ExecutePageUp();
            }

            if (e.Key == Key.PageDown)
            {
                ExecutePageDown();
            }
        }

        private void ExecutePageDown()
        {
            var indexToSelect = _filteredList.SelectedIndex + (int) Math.Floor(ListScrollViewer.ViewportHeight) - 1;
            _filteredList.SelectedIndex = _filteredList.Items.Count <= indexToSelect
                ? _filteredList.Items.Count - 1
                : indexToSelect;
            _filteredList.ScrollIntoView(_filteredList.SelectedItem);
        }

        private void ExecutePageUp()
        {
            var indexToSelect = _filteredList.SelectedIndex - (int) Math.Floor(ListScrollViewer.ViewportHeight) + 1;
            _filteredList.SelectedIndex = indexToSelect < 0 ? 0 : indexToSelect;
            _filteredList.ScrollIntoView(_filteredList.SelectedItem);
        }

        private void FocusSelectedFilteredItem()
        {
            if (_filteredList == null) return;
            _filteredList.UpdateLayout();
            if (_filteredList.SelectedItem !=
                null)
                _filteredList.ScrollIntoView(
                    _filteredList.SelectedItem);
        }

        public bool AreFilteredStringsVisible => FilteredStrings.Count > 0;

        private ScrollViewer ListScrollViewer => GetVisualChild<ScrollViewer>(_filteredList);

        public override void OnApplyTemplate()
        {
            _filteredList = GetTemplateChild("FilteredList") as ListView;

            if (_filteredList != null)
            {
                _filteredList.PreviewMouseUp += FilteredListMouseUp;
            }

            _textBox = GetTemplateChild("FilteringTextBox") as TextBox;
            if (_textBox != null)
                _textBox.Loaded += TextBoxLoaded;
            PreviewKeyDown += AutoCompleteControl_KeyDown;
        }

        private void TextBoxLoaded(object sender, RoutedEventArgs e)
        {
            var tB = sender as TextBox;
            if (tB == null) return;
            Keyboard.Focus(tB);
            tB.SelectAll();
        }

        private void FilteredListMouseUp(object sender, MouseButtonEventArgs e)
        {
            var objectUnderMouse = Mouse.DirectlyOver;
            if (objectUnderMouse is Thumb || objectUnderMouse is RepeatButton || objectUnderMouse is ScrollBar) return;
            ExecuteControlCommand(SelectedFilteredItem);
        }


        private void OnFilteringStringChanged()
        {
            var previousSelectedFilteredItem = SelectedFilteredItem;
            FilteredStrings.Clear();
            if (string.IsNullOrWhiteSpace(FilteringString) || ItemsSource == null)
            {
                SelectedFilteredItem = null;
                FoundResultMessage = string.Empty;
            }
            else
            {
                foreach (var item in _searchItemsSource)
                {
                    if (item.Name.IndexOf(FilteringString, StringComparison.OrdinalIgnoreCase) >= 0 &&
                        !FilteredStrings.Contains(item))
                        FilteredStrings.Add(item);
                }

                SelectedFilteredItem = previousSelectedFilteredItem == null ||
                                       !FilteredStrings.Contains(previousSelectedFilteredItem)
                    ? FilteredStrings.Count > 0 ? FilteredStrings[0] : null
                    : previousSelectedFilteredItem;
                FoundResultMessage = FilteredStrings.Count == 0
                    ? $"No {ItemToSearch}s found"
                    : $"{FilteredStrings.Count} {ItemToSearch}s found";
            }

            CommandManager.InvalidateRequerySuggested();
            OnPropertyChanged(nameof(AreFilteredStringsVisible));
        }

        #endregion

        [field: NonSerialized] public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static T GetVisualChild<T>(Visual parent) where T : Visual
        {
            T child = default(T);
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual) VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                {
                    child = GetVisualChild<T>(v);
                }

                if (child != null)
                {
                    break;
                }
            }

            return child;
        }
    }
}