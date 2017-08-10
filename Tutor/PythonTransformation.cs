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

        public override bool Equals(Object obj)
        {
            // Check for null values and compare run-time types.
            if (obj == null || GetType() != obj.GetType())
                return false;

            PythonTransformation pt = (PythonTransformation)obj;
            return _programNode.Equals(pt._programNode);
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = (hash * 23) + _programNode.GetHashCode();
            return hash;
        }
    }
}
