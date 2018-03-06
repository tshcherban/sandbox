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
            SaveCommand = new RelayCommand(SaveCmd_Execute);
            LoadCommand = new RelayCommand(LoadCmd_Execute);
            TagListCommand = new RelayCommand(TagListCmd_Execute);
        }

        private void TagListCmd_Execute(object obj)
        {
            var vm = new TagsEditVM(Tags, t => model.Tags.Add(t));
            var view = new TagsEditView { DataContext = vm };
            view.ShowDialog();
        }

        const string FilePath = "data.json";

        RootModel model;
        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            PreserveReferencesHandling = PreserveReferencesHandling.All
        };

        private void LoadCmd_Execute(object obj)
        {
            if (File.Exists(FilePath))
                model = JsonConvert.DeserializeObject<RootModel>(File.ReadAllText(FilePath), settings);
            else
                model = new RootModel();

            foreach (var tag in model.Tags)
                Tags.Add(new TagVM(tag));

            foreach (var note in model.Notes)
                Notes.Add(new NoteVM(note));

        }

        private void SaveCmd_Execute(object obj)
        {
            var s = JsonConvert.SerializeObject(model, settings);
            File.WriteAllText(FilePath, s);
        }

        private void NotesViewOnCurrentChanged(object sender, EventArgs eventArgs)
        {
        }

        private void AddNoteCommand_Execute(object obj)
        {
            var noteVM = new NoteVM(new NoteModel { Id = Guid.NewGuid() })
            {
                Header = DateTime.Now.ToString("G"),
            };
            var vm = new NoteEditViewModel(noteVM);
            var view = new NoteEditView
            {
                DataContext = vm
            };
            if (view.ShowDialog() ?? false)
            {
                Notes.Add(noteVM);
                model.Notes.Add(noteVM.Model);
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

        public ICommand AddNoteCommand { get; }

        public ICommand SaveCommand { get; }

        public ICommand LoadCommand { get; }

        public ICommand TagListCommand { get; }
    }
}