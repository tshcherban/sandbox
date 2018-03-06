using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace NoteKeeper.Model
{
    public sealed class NoteModel
    {
        public Guid Id { get; set; }

        public string Header { get; set; }

        public string Content { get; set; }

        [JsonProperty(ItemIsReference = true)]
        public List<TagModel> Tags { get; } = new List<TagModel>();
    }
}
