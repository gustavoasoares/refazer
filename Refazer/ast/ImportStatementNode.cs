using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler.Ast;

namespace Tutor.ast
{
    class ImportStatementNode : InternalNode
    {
        public IList<string> Names { set; get; }
         
        public ImportStatementNode(Node innerNode) : base(innerNode)
        {
        }

        protected override bool IsEqualToInnerNode(Node node)
        {
            throw new NotImplementedException();
        }
        public override PythonNode Clone()
        {
            var pythonNode = new ImportStatementNode(InnerNode);
            pythonNode.Names = Names;
            pythonNode.Id = Id;
            if (Value != null) pythonNode.Value = Value;
            return pythonNode;
        }
    }
}
