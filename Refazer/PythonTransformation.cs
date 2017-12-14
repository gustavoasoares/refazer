using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ProgramSynthesis.AST;

namespace Refazer.Core

{
    public class PythonTransformation : Transformation
    {
        private readonly ProgramNode _programNode;

        public PythonTransformation(ProgramNode programNode)
        {
            _programNode = programNode;
        }

        public ProgramNode GetSynthesizedProgram()
        {
            return _programNode;
        }
    }
}
