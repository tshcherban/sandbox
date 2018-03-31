using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace NoteKeeper.ViewModel
{
    public sealed class NoteEditViewModel : ObservableObject
    {
        public NoteVM NoteViewModel { get; }

        public ListCollectionView TagsView { get; }

        public NoteEditViewModel(NoteVM noteViewModel, ObservableCollection<TagVM> tags)
        {
            NoteViewModel = noteViewModel;
            TagsView = new ListCollectionView(tags);
        }

        private string _basicFilterText;
        private FuzzySearchHelper _searchHelper;

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
                    TagsView.Filter = null;
                    _searchHelper = null;
                    Terms = null;
                }
                else
                {
                    Terms = _basicFilterText.Select(term => term.ToString()).ToArray();
                    _searchHelper = new FuzzySearchHelper(Terms);

                    TagsView.Filter = o =>
                    {
                        var noteVm = (TagVM)o;
                        return _searchHelper.PassesFilter(noteVm.Header);
                    };
                }
            }
        }
    }
}
