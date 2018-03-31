using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace NoteKeeper.Model
{
    public sealed class NoteModel
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Header { get; set; }

        public string Content { get; set; }

        public List<Guid> TagIds { get; private set; } = new List<Guid>();

        [JsonIgnore]
        public bool Modified { get; set; }
    }
}