using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutor.ast
{
    public interface InsertStrategy
    {
        List<PythonNode> Insert(PythonNode context, PythonNode inserted, int index); 
    }
}
