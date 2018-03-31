using System;
using Newtonsoft.Json;

namespace NoteKeeper.Model
{
    public sealed class TagModel
    {
        public TagModel()
        {
            Id = Guid.NewGuid();
        }

        public Guid Id { get; set; }

        public string Header { get; set; }

        [JsonIgnore]
        public bool Modified { get; set; }
    }
}