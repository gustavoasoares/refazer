using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutor
{
    public class Patch
    {
        public List<IEnumerable<Edit>> EditSets { get; }

        public Patch()
        {
            EditSets = new List<IEnumerable<Edit>>();
        }

        public Patch(IEnumerable<Edit> editSet)
        {
            EditSets = new List<IEnumerable<Edit>> {editSet};
        }
    }
}
