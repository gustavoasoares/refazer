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
        private readonly PythonNode _programNode;

        public PythonExtraction(PythonNode pythonNode)
        {
            _pythonNode = pythonNode;
        }

        public PythonNode GetExtractedNode()
        {
            return _pythonNode;
        }
    }
}
