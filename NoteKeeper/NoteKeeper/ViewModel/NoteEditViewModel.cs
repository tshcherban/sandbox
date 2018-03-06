using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoteKeeper.ViewModel
{
    public sealed class NoteEditViewModel : ObservableObject
    {
        public NoteVM NoteViewModel { get; }

        public NoteEditViewModel(NoteVM noteViewModel)
        {
            NoteViewModel = noteViewModel;
        }
    }
}
