using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace NoteKeeper.Model
{
    public sealed class RootModel
    {
        public List<TagModel> Tags { get; set; } = new List<TagModel>();

        public List<NoteModel> Notes { get; set; } = new List<NoteModel>();

        [JsonIgnore]
        public bool Modified
        {
            get { return Tags.Any(i => i.Modified); }
        }
    }
}