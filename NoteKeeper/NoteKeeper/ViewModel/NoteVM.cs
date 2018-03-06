using NoteKeeper.Model;
using System.Collections.ObjectModel;

namespace NoteKeeper
{
    public class NoteVM : ObservableObject
    {
        private readonly NoteModel _model;

        public NoteVM(NoteModel model)
        {
            Tags = new ObservableCollection<TagVM>();
            _model = model;
        }

        public NoteVM Parent { get; }

        public string Header
        {
            get => _model.Header;
            set
            {
                _model.Header = value;
                OnPropertyChanged(nameof(Header));
            }
        }

        public string Content
        {
            get => _model.Content;
            set
            {
                _model.Content = value;
                OnPropertyChanged(nameof(Content));
            }
        }

        public ObservableCollection<TagVM> Tags { get; }

        public NoteModel Model => _model;
    }
}