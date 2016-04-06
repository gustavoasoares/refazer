using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler.Ast;

namespace Tutor
{
    public class PythonNode
    {
        public int EditId { get; }

        public Node InnerNode { get;}
        public bool IsAbstract { get; }

        public List<PythonNode> Children { get; } 

        public PythonNode(Node innerNode, bool isAbstract)
        {
            InnerNode = innerNode;
            IsAbstract = isAbstract;
            Children = new List<PythonNode>();
        }

        public PythonNode(Node innerNode, bool isAbstract, int editId) : this(innerNode, isAbstract)
        {
            EditId = editId;
        }

        public void AddChild(PythonNode node)
        {
            Children.Add(node);
        }

        public Tuple<bool, Dictionary<int, Node>> Match(Node node)
        {
            var matchResult = new Dictionary<int, Node>();

            if (IsAbstract)
            {
                if (!InnerNode.NodeName.Equals(node.NodeName))
                    return Tuple.Create<bool, Dictionary<int, Node>>(false, new Dictionary<int, Node>());
            }
            else
            {
                if (!IsEqualToInnerNode(node))
                    return Tuple.Create<bool, Dictionary<int, Node>>(false, new Dictionary<int, Node>());
            }

            if (EditId != 0)
            {
                matchResult.Add(EditId,node);
            }

            if (Children.Count == 0)
            {
                return Tuple.Create<bool, Dictionary<int, Node>>(true, matchResult); 
            }

            if (node is BinaryExpression)
            {
                var convertedNode = node as BinaryExpression;
                var resultLeft = Children[0].Match(convertedNode.Left);
                var resultRight = Children[1].Match(convertedNode.Right);
                if (resultRight.Item1 && resultLeft.Item1)
                {
                    AddMatchResult(matchResult, resultLeft.Item2);
                    AddMatchResult(matchResult, resultRight.Item2);
                    return Tuple.Create<bool, Dictionary<int, Node>>(true, matchResult);
                }
                else
                {
                    return Tuple.Create<bool, Dictionary<int, Node>>(false, new Dictionary<int, Node>());
                }
            } else if (node is AssignmentStatement)
            {
                var convertedNode = node as AssignmentStatement;
                //if the number of expressions in the left side are different from 
                //the number of children - 1 (the last one is for the right side),
                //the tree is different 
                if (convertedNode.Left.Count != Children.Count - 1)
                    return Tuple.Create<bool, Dictionary<int, Node>>(false, new Dictionary<int, Node>());

                for (var i = 0; i < Children.Count -1; i++)
                {
                    var result = Children[i].Match(convertedNode.Left[i]);
                    if (!result.Item1)
                        return Tuple.Create<bool, Dictionary<int, Node>>(false, new Dictionary<int, Node>());
                    AddMatchResult(matchResult, result.Item2);
                }
                var resultRight = Children.Last().Match(convertedNode.Right);
                if (resultRight.Item1)
                {
                    AddMatchResult(matchResult, resultRight.Item2);
                    return Tuple.Create<bool, Dictionary<int, Node>>(true, matchResult);
                }
                else
                {
                    return Tuple.Create<bool, Dictionary<int, Node>>(false, new Dictionary<int, Node>());
                }
            } else if (node is TupleExpression)
            {
                var convertedNode = node as TupleExpression;
                if (convertedNode.Items.Count != Children.Count)
                    return Tuple.Create<bool, Dictionary<int, Node>>(false, new Dictionary<int, Node>());

                for (var i = 0; i < Children.Count; i++)
                {
                    var result = Children[i].Match(convertedNode.Items[i]);
                    if (!result.Item1)
                        return Tuple.Create<bool, Dictionary<int, Node>>(false, new Dictionary<int, Node>());
                    AddMatchResult(matchResult, result.Item2);
                }
                return Tuple.Create<bool, Dictionary<int, Node>>(true, matchResult);
            }

            throw new NotImplementedException();
        }

        private void AddMatchResult(Dictionary<int, Node> current, Dictionary<int, Node> matchresult)
        {
            foreach (var keyValuePair in matchresult)
            {
                current.Add(keyValuePair.Key, keyValuePair.Value);
            }
        }

        private bool IsEqualToInnerNode(Node node)
        {
            if (InnerNode is NameExpression)
            {
                var inner = InnerNode as NameExpression;
                var comparedNode = node as NameExpression;
                if (comparedNode == null) return false;
                return inner.Name.Equals(comparedNode.Name);
            }
            else if (InnerNode is ConstantExpression)
            {
                var inner = InnerNode as ConstantExpression;
                var comparedNode = node as ConstantExpression;
                if (comparedNode == null) return false;
                return inner.Value.Equals(comparedNode.Value);
            }
            throw new NotImplementedException();
        }
    }
}
