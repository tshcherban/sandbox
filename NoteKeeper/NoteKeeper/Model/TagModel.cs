using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoteKeeper.Model
{
    public sealed class TagModel
    {
        public Guid Id { get; set; }

        public string Header { get; set; }
    }

    public sealed class RootModel
    {
        public List<TagModel> Tags { get; set; } = new List<TagModel>();

        public List<NoteModel> Notes { get; set; } = new List<NoteModel>();
    }
}
