using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ProgramSynthesis.AST;

namespace Refazer.Core
{
    /// <summary>
    /// Interface that represents a transformation synthesized by Refazer
    /// It should wraps the ProgramNode synthesized by Prose
    /// </summary>
    public interface Transformation
    {
        ProgramNode GetSynthesizedProgram(); 
    }
}
