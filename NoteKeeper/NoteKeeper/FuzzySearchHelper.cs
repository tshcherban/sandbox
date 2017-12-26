using System;
using System.Collections.Generic;
using System.Linq;

namespace NoteKeeper
{
    public sealed class FuzzySearchHelper
    {
        private readonly string[] _terms;

        public FuzzySearchHelper(string[] terms)
        {
            _terms = terms;
        }

        public bool PassesFilter(string value)
        {
            var indexes = new List<int>(_terms.Length);
            var idx = 0;

            foreach (var t in _terms)
            {
                var indexOf = idx > value.Length
                    ? value.Length - idx
                    : value.IndexOf(t, idx, StringComparison.OrdinalIgnoreCase);

                indexes.Add(indexOf);
                idx = indexOf + t.Length;

                if (indexOf < 0)
                    break;
            }

            return indexes.All(i => i >= 0);
        }
    }
}