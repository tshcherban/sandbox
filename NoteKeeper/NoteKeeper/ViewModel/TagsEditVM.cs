using NoteKeeper.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NoteKeeper.ViewModel
{
    public sealed class TagsEditVM : ObservableObject
    {
        private readonly Action<TagModel> tagAdded;

        public TagsEditVM(ObservableCollection<TagVM> tags, Action<TagModel> tagAdded)
        {
            Tags = tags;
            this.tagAdded = tagAdded;
            AddTagCmd = new RelayCommand(AddTagCmd_Execute);
        }

        private void AddTagCmd_Execute(object obj)
        {
            var tag = new TagModel();
            Tags.Add(new TagVM(tag));
            tagAdded(tag);
        }

        public ObservableCollection<TagVM> Tags { get; }

        public ICommand AddTagCmd { get; }
    }
}
