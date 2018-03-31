using Newtonsoft.Json;
using NoteKeeper.Model;
using NoteKeeper.View;
using NoteKeeper.ViewModel;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;

namespace NoteKeeper
{
    public sealed class MainVM : ObservableObject
    {
        private string _basicFilterText;
        private FuzzySearchHelper _searchHelper;

        public ObservableCollection<TagVM> Tags { get; }

        public ObservableCollection<string> TagsStr { get; }

        public MainVM()
        {
            Notes = new ObservableCollection<NoteVM>();
            NotesView = new ListCollectionView(Notes);
            NotesView.CurrentChanged += NotesViewOnCurrentChanged;

            Tags = new ObservableCollection<TagVM>();
            TagsStr = new ObservableCollection<string>(Tags.Select(i => i.Header));

            AddNoteCommand = new RelayCommand(AddNoteCommand_Execute);
            EditNoteCommand = new RelayCommand(EditNoteCmd_Execute, EditNoteCmd_CanExecute);
            SaveCommand = new RelayCommand(SaveCmd_Execute);
            LoadCommand = new RelayCommand(LoadCmd_Execute);
            TagListCommand = new RelayCommand(TagListCmd_Execute);

            _modelSaver = new FileSystemJsonModelSaver(FilePath);
        }

        private bool EditNoteCmd_CanExecute(object obj)
        {
            return NotesView?.CurrentItem != null;
        }

        private void EditNoteCmd_Execute(object obj)
        {
            var noteVm = NotesView?.CurrentItem as NoteVM;
            if (noteVm == null)
                return;

            var vm = new NoteEditViewModel(noteVm, Tags);
            var view = new NoteEditView { DataContext = vm };
            view.ShowDialog();
        }

        private void TagListCmd_Execute(object obj)
        {
            var vm = new TagsEditVM(Tags, t => _model.Tags.Add(t));
            var view = new TagsEditView { DataContext = vm };
            view.ShowDialog();
        }

        const string FilePath = "data.json";

        RootModel _model;
        IModelSaver _modelSaver;

        private void LoadCmd_Execute(object obj)
        {
            Notes.Clear();
            Tags.Clear();

            _model = _modelSaver.LoadOrNew();

            foreach (var tag in _model.Tags)
                Tags.Add(new TagVM(tag));

            foreach (var note in _model.Notes)
                Notes.Add(new NoteVM(note, Tags.Where(t => note.TagIds.Contains(t.Id))));
        }

        private void SaveCmd_Execute(object obj)
        {
            _modelSaver.Save(_model);
        }

        private void NotesViewOnCurrentChanged(object sender, EventArgs eventArgs)
        {
        }

        private void AddNoteCommand_Execute(object obj)
        {
            var noteVM = new NoteVM(new NoteModel(), Enumerable.Empty<TagVM>())
            {
                Header = DateTime.Now.ToString("G"),
            };
            var vm = new NoteEditViewModel(noteVM, Tags);
            var view = new NoteEditView
            {
                DataContext = vm
            };
            if (view.ShowDialog() ?? false)
            {
                Notes.Add(noteVM);
                _model.Notes.Add(noteVM.Model);
            }
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
                        var noteVm = (NoteVM)o;
                        return _searchHelper.PassesFilter(noteVm.Header) || noteVm.Tags.Any(i => _searchHelper.PassesFilter(i.Header));
                    };
                }
            }
        }



        public ICommand EditNoteCommand { get; }

        public ICommand AddNoteCommand { get; }

        public ICommand SaveCommand { get; }

        public ICommand LoadCommand { get; }

        public ICommand TagListCommand { get; }
    }
}