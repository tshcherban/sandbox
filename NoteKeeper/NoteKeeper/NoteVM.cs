using System.Collections.ObjectModel;

namespace NoteKeeper
{
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
}