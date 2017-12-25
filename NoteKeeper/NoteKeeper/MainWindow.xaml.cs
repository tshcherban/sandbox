using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using JetBrains.Annotations;

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
    }

    public abstract class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        [System.Diagnostics.DebuggerStepThrough]
        public bool CanExecute(object parameter)
        {
            return _canExecute?.Invoke(parameter) ?? true;
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }
    }

    public sealed class MainVM : ObservableObject
    {
        private string _basicFilterText;
        private FuzzySearchHelper _searchHelper;
        private NoteVM _editingNote;

        public MainVM()
        {
            Notes = new ObservableCollection<NoteVM>();
            NotesView = new ListCollectionView(Notes);
            NotesView.CurrentChanged += NotesViewOnCurrentChanged;

            AddNoteCommand = new RelayCommand(AddNoteCommand_Execute);
        }

        private void NotesViewOnCurrentChanged(object sender, EventArgs eventArgs)
        {
            if (_editingNote != null)
            {
                _editingNote.IsEditing = false;
                _editingNote = null;
            }
        }

        private void AddNoteCommand_Execute(object obj)
        {
            var noteVM = new NoteVM
            {
                Header = DateTime.Now.ToString("G"),
                IsEditing = true,
            };
            Notes.Add(noteVM);
            NotesView.MoveCurrentTo(noteVM);
            _editingNote = noteVM;
        }

        public ObservableCollection<NoteVM> Notes { get; }

        public ListCollectionView NotesView { get; }

        private string[] _terms;

        public string[] Terms
        {
            get => _terms;
            set
            {
                if (_terms == value)
                    return;
                _terms = value;
                OnPropertyChanged(nameof(Terms));
            }
        }

        public string BasicFilterText
        {
            get => _basicFilterText;
            set
            {
                if (value == _basicFilterText)
                    return;

                _basicFilterText = value;
                OnPropertyChanged(nameof(BasicFilterText));

                if (string.IsNullOrEmpty(_basicFilterText))
                {
                    NotesView.Filter = null;
                    _searchHelper = null;
                    Terms = null;
                }
                else
                {
                    Terms = _basicFilterText.Select(term => term.ToString()).ToArray();
                    _searchHelper = new FuzzySearchHelper(Terms);

                    NotesView.Filter = o =>
                    {
                        var noteVm = (NoteVM) o;
                        return _searchHelper.PassesFilter(noteVm.Header);
                    };
                }
            }
        }

        public ICommand AddNoteCommand { get; }
    }

    public sealed class FuzzySearchHelper
    {
        private readonly string[] _terms;

        public FuzzySearchHelper(string[] terms)
        {
            _terms = terms;
        }

        public bool PassesFilter(string value)
        {
            var indexes = new List<int>(_terms.Length);
            var idx = 0;

            foreach (var t in _terms)
            {
                var indexOf = idx > value.Length
                    ? value.Length - idx
                    : value.IndexOf(t, idx, StringComparison.OrdinalIgnoreCase);

                indexes.Add(indexOf);
                idx = indexOf + t.Length;

                if (indexOf < 0)
                    break;
            }

            return indexes.All(i => i >= 0);
        }
    }

    public class NoteVM : ObservableObject
    {
        private bool _isEditing;

        public NoteVM()
        {
            Tags = new ObservableCollection<TagVM>();
        }

        public NoteVM Parent { get; }

        public string Header { get; set; }

        public string Content { get; set; }

        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                if (value == _isEditing)
                    return;

                _isEditing = value;
                OnPropertyChanged(nameof(IsEditing));
            }
        }

        public ObservableCollection<TagVM> Tags { get; }
    }

    public class TagVM : ObservableObject
    {
        public string Header { get; set; }
    }

    public sealed class HighlightTextControl : ContentControl
    {
        public static readonly DependencyProperty HighlightPartsProperty =
            DependencyProperty.Register(
                "HighlightParts",
                typeof(IEnumerable<string>),
                typeof(HighlightTextControl),
                new PropertyMetadata(default(IEnumerable<string>), HighlightPartsPropertyChangedCallback));

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                "Text", typeof(string),
                typeof(HighlightTextControl),
                new PropertyMetadata(default(string), TextPropertyChangedCallback));

        private readonly TextBlock _textBlock;
        private readonly SolidColorBrush _highlightedTextBackgroundBrush;
        private readonly SolidColorBrush _highlightedTextForegroundBrush;

        private static void HighlightPartsPropertyChangedCallback(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var _this = (HighlightTextControl) d;
            if (e.OldValue is INotifyCollectionChanged incc)
                incc.CollectionChanged -= _this.HighlightParts_OnCollectionChanged;

            incc = e.NewValue as INotifyCollectionChanged;
            if (incc != null)
                incc.CollectionChanged += _this.HighlightParts_OnCollectionChanged;

            _this.TextChanged();
        }

        private void HighlightParts_OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            TextChanged();
        }

        private static void TextPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((HighlightTextControl) d).TextChanged();
        }

        public IEnumerable<string> HighlightParts
        {
            get => (IEnumerable<string>) GetValue(HighlightPartsProperty);
            set => SetValue(HighlightPartsProperty, value);
        }

        public string Text
        {
            get => (string) GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public HighlightTextControl()
        {
            Content = _textBlock = new TextBlock();

            _highlightedTextBackgroundBrush = new SolidColorBrush(Color.FromArgb(255, 255, 229, 107));
            _highlightedTextBackgroundBrush.Freeze();

            _highlightedTextForegroundBrush = new SolidColorBrush(SystemColors.ActiveCaptionTextColor);
            _highlightedTextForegroundBrush.Freeze();
        }

        private void TextChanged()
        {
            _textBlock.Inlines.Clear();
            var coloredText = GetColoredText().ToArray();
            _textBlock.Inlines.AddRange(coloredText);
        }

        private Dictionary<int, string> GetIndexes()
        {
            var indexes = new Dictionary<int, string>();
            var idx = 0;
            foreach (var h in HighlightParts.Where(s => !string.IsNullOrEmpty(s)))
            {
                var indexOf = idx > Text.Length
                    ? -1
                    : Text.IndexOf(h, idx, StringComparison.OrdinalIgnoreCase);

                indexes.Add(indexOf, h);
                idx = indexOf + h.Length;

                if (indexOf < 0)
                    break;
            }

            return indexes;
        }

        private IEnumerable<Inline> GetColoredText()
        {
            if (string.IsNullOrEmpty(Text) || HighlightParts == null || !HighlightParts.Any() ||
                HighlightParts.All(string.IsNullOrEmpty))
            {
                yield return new Run(Text);
                yield break;
            }

            var indexes = GetIndexes();

            var normalRun = new Run();
            var highlightRun = new Run
            {
                Background = _highlightedTextBackgroundBrush,
                Foreground = _highlightedTextForegroundBrush
            };

            for (var i = 0; i < Text.Length; ++i)
            {
                if (indexes.ContainsKey(i))
                {
                    highlightRun.Text += Text.Substring(i, indexes[i].Length);
                    i += indexes[i].Length - 1;

                    if (!string.IsNullOrEmpty(normalRun.Text))
                    {
                        yield return normalRun;
                        normalRun = new Run();
                    }
                }
                else
                {
                    normalRun.Text += Text[i];

                    if (!string.IsNullOrEmpty(highlightRun.Text))
                    {
                        yield return highlightRun;
                        highlightRun = new Run
                        {
                            Background = _highlightedTextBackgroundBrush,
                            Foreground = _highlightedTextForegroundBrush
                        };
                    }
                }
            }

            if (!string.IsNullOrEmpty(highlightRun.Text))
                yield return highlightRun;

            if (!string.IsNullOrEmpty(normalRun.Text))
                yield return normalRun;
        }
    }
}