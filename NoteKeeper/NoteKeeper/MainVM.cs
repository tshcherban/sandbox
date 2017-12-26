using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;

namespace NoteKeeper
{
    public sealed class MainVM : ObservableObject
    {
        private string _basicFilterText;
        private FuzzySearchHelper _searchHelper;
        private NoteVM _editingNote;

        public ObservableCollection<TagVM> Tags { get; }

        public ObservableCollection<string> TagsStr { get; }

        public MainVM()
        {
            Notes = new ObservableCollection<NoteVM>();
            NotesView = new ListCollectionView(Notes);
            NotesView.CurrentChanged += NotesViewOnCurrentChanged;

            Tags = new ObservableCollection<TagVM>
            {
                new TagVM {Header = "Tag 1"},
                new TagVM {Header = "Tag 2"},
                new TagVM {Header = "Tag 3"},
            };
            TagsStr = new ObservableCollection<string>(Tags.Select(i=>i.Header));

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
}