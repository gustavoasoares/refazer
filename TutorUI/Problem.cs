using System;
using System.Collections;
using System.Collections.Generic;
using Tutor;

namespace TutorUI
{
    [Serializable]
    public class Problem
    {
        public string Id { get; }
        public IEnumerable<Mistake> Mistakes { get; set; }

        public IDictionary<int, List<Mistake>> AttemptsPerStudent { get; set; }
        public Dictionary<string, long> Tests { get; set; }

        public Tuple<string, List<string>> StaticTests { get; set; }

        public Problem(string id, IEnumerable<Mistake> mistakes)
        {
            Id = id;
            Mistakes = mistakes;
        }

        public Problem(string id)
        {
            Id = id;
        }
    }
}