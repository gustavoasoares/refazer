using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler.Ast;

namespace Tutor.ast
{
    class ComprehensionIteratorNode : InternalNode
    {
        public ComprehensionIteratorNode(Node innerNode, bool isAbstract) : base(innerNode, isAbstract)
        {
        }

        public ComprehensionIteratorNode(Node innerNode, bool isAbstract, int editId) : base(innerNode, isAbstract, editId)
        {
        }

        protected override bool IsEqualToInnerNode(Node node)
        {
            throw new NotImplementedException();
        }

        public override PythonNode Clone()
        {
            throw new NotImplementedException();
        }
    }
}
