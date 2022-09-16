using System;
using System.Collections.Generic;

namespace g3
{
    public class Tokens
    {
        /// <summary>
        /// The ID and token name map
        /// </summary>
        private readonly Dictionary<int, string> _names;

        public string this[int id] => _names[id];

        public int Count => _names.Count;

        public int Counter { get; set; }
        public int ActiveID { get; private set; }

        public Tokens(int invalidID)
        {
            _names = new Dictionary<int, string>();

            Counter = 0;
            ActiveID = invalidID;
        }

        public void Append(string line, string[] tokens)
        {
            var name = tokens.Length == 2 ? tokens[1] : line.Substring(line.IndexOf(tokens[1], StringComparison.Ordinal));

            ActiveID = Counter;
            _names[ActiveID] = name;
            ++Counter;
        }

        public IEnumerable<int> ListID() => _names.Keys;

        public IEnumerable<string> ListName() => _names.Values;
    }
}