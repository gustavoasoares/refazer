using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ProgramSynthesis.AST;
using Tutor;

namespace Refazer.Core
{
    /// <summary>
    /// Interface that represents an extraction by Refazer
    /// It should wraps the ProgramNode returned by Prose
    /// </summary>
    public interface Extraction
    {
        PythonNode GetExtractedNode(); 
    }
}
