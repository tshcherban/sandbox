using NoteKeeper.Model;
using System;

namespace NoteKeeper
{
    public class TagVM : ObservableObject
    {
        private readonly TagModel _model;

        public TagVM(TagModel model)
        {
            _model = model;
        }

        public Guid Id => _model.Id;

        public string Header
        {
            get => _model.Header;
            set
            {
                _model.Header = value;
                OnPropertyChanged(nameof(Header));
            }
        }
    }
}