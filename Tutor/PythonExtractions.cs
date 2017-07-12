using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ProgramSynthesis.AST;

namespace Refazer.Core

{
    public class PythonExtraction : Extraction
    {
        private readonly ProgramNode _programNode;

        public PythonExtraction(ProgramNode pythonNode)
        {
            _programNode = pythonNode;
        }

        public ProgramNode GetExtractedNode()
        {
            return _programNode;
        }
    }
}
