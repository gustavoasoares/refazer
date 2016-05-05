using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsQuery.ExtensionMethods;
using IronPython.Compiler.Ast;

namespace Tutor
{
    public class PythonNode
    {
        public int EditId { get; set; }

        public string Value { get; set; }

        public Node InnerNode { get;}
        public bool IsAbstract { get; }
        public List<PythonNode> Children { get; }
        public PythonNode Parent { get; set; }
        public bool Reference { get; set; }

        public PythonNode(Node innerNode, bool isAbstract)
        {
            InnerNode = innerNode;
            IsAbstract = isAbstract;
            Children = new List<PythonNode>();
            Value = "";
            Reference = false;
        }

        public PythonNode(Node innerNode, bool isAbstract, int editId) : this(innerNode, isAbstract)
        {
            EditId = editId;
        }

        public void AddChild(PythonNode node)
        {
            Children.Add(node);
        }

        public Tuple<bool, Node> Match(Node node)
        {
            Node matchResult = null;
            if (IsAbstract)
            {
                if (!InnerNode.NodeName.Equals(node.NodeName))
                    return Tuple.Create<bool, Node>(false, null);
            }
            else
            {
                if (!IsEqualToInnerNode(node))
                    return Tuple.Create<bool, Node>(false, null);
            }

            if (EditId != 0)
            {
                matchResult = node;
            }

            if (Children.Count == 0)
            {
                return Tuple.Create<bool, Node>(true, matchResult); 
            }

            if (node is BinaryExpression)
            {
                var convertedNode = (BinaryExpression) node;
                var resultLeft = Children[0].Match(convertedNode.Left);
                var resultRight = Children[1].Match(convertedNode.Right);
                if (resultRight.Item1 && resultLeft.Item1)
                {
                    matchResult = AddMatchResult(matchResult, resultLeft.Item2);
                    matchResult = AddMatchResult(matchResult, resultRight.Item2);
                    return Tuple.Create<bool, Node>(true, matchResult);
                }
                return Tuple.Create<bool, Node>(false, null);
            }

            if (node is AugmentedAssignStatement)
            {
                var convertedNode = (AugmentedAssignStatement)node;
                var resultLeft = Children[0].Match(convertedNode.Left);
                var resultRight = Children[1].Match(convertedNode.Right);
                if (resultRight.Item1 && resultLeft.Item1)
                {
                    matchResult = AddMatchResult(matchResult, resultLeft.Item2);
                    matchResult = AddMatchResult(matchResult, resultRight.Item2);
                    return Tuple.Create<bool, Node>(true, matchResult);
                }
                return Tuple.Create<bool, Node>(false, null);
            }

            if (node is AssignmentStatement)
            {
                var convertedNode = node as AssignmentStatement;
                //if the number of expressions in the left side are different from 
                //the number of children - 1 (the last one is for the right side),
                //the tree is different 
                if (convertedNode.Left.Count != Children.Count - 1)
                    return Tuple.Create<bool, Node>(false, null);

                for (var i = 0; i < Children.Count -1; i++)
                {
                    var result = Children[i].Match(convertedNode.Left[i]);
                    if (!result.Item1)
                        return Tuple.Create<bool, Node>(false, null);
                    matchResult = AddMatchResult(matchResult, result.Item2);
                }
                var resultRight = Children.Last().Match(convertedNode.Right);
                if (resultRight.Item1)
                {
                    matchResult = AddMatchResult(matchResult, resultRight.Item2);
                    return Tuple.Create<bool, Node>(true, matchResult);
                }
                else
                {
                    return Tuple.Create<bool, Node>(false, null);
                }
            }
            if (node is TupleExpression)
            {
                var convertedNode = node as TupleExpression;
                if (convertedNode.Items.Count != Children.Count)
                    return Tuple.Create<bool, Node>(false, null);

                for (var i = 0; i < Children.Count; i++)
                {
                    var result = Children[i].Match(convertedNode.Items[i]);
                    if (!result.Item1)
                        return Tuple.Create<bool, Node>(false, null);
                    matchResult = AddMatchResult(matchResult, result.Item2);
                }
                return Tuple.Create<bool, Node>(true, matchResult);
            }
            if (node is PythonAst)
            {
                var convertedNode = (PythonAst) node;
                var result = Children[0].Match(convertedNode.Body);
                if (!result.Item1)
                    return Tuple.Create<bool, Node>(false, null);
                matchResult = AddMatchResult(matchResult, result.Item2);
            }
            if (node is SuiteStatement)
            {
                var convertedNode = node as SuiteStatement;
                if (convertedNode.Statements.Count != Children.Count)
                    return Tuple.Create<bool, Node>(false, null);
                for (var i = 0; i < Children.Count; i++)
                {
                    var result = Children[i].Match(convertedNode.Statements[i]);
                    if (!result.Item1)
                        return Tuple.Create<bool, Node>(false, null);
                    matchResult = AddMatchResult(matchResult, result.Item2);
                }
                return Tuple.Create<bool, Node>(true, matchResult);
            }
            if (node is ExpressionStatement)
            {
                var convertedNode = (ExpressionStatement) node;
                if (Children.Count != 1)
                    return Tuple.Create<bool, Node>(false, null);

                var result = Children[0].Match(convertedNode.Expression);
                if (!result.Item1)
                    return Tuple.Create<bool, Node>(false, null);
                matchResult = AddMatchResult(matchResult, result.Item2);
                return Tuple.Create<bool, Node>(true, matchResult);
            }
            if (node is ReturnStatement)
            {
                var convertedNode = (ReturnStatement)node;
                if (Children.Count != 1)
                    return Tuple.Create<bool, Node>(false, null);

                var result = Children[0].Match(convertedNode.Expression);
                if (!result.Item1)
                    return Tuple.Create<bool, Node>(false, null);
                matchResult = AddMatchResult(matchResult, result.Item2);
                return Tuple.Create<bool, Node>(true, matchResult);
            }
            if (node is ParenthesisExpression)
            {
                var convertedNode = (ParenthesisExpression)node;
                if (Children.Count != 1)
                    return Tuple.Create<bool, Node>(false, null);

                var result = Children[0].Match(convertedNode.Expression);
                if (!result.Item1)
                    return Tuple.Create<bool, Node>(false, null);
                matchResult = AddMatchResult(matchResult, result.Item2);
                return Tuple.Create<bool, Node>(true, matchResult);
            }
            if (node is Arg)
            {
                var convertedNode = (Arg)node;
                if (Children.Count != 1)
                    return Tuple.Create<bool, Node>(false, null);

                var result = Children[0].Match(convertedNode.Expression);
                if (!result.Item1)
                    return Tuple.Create<bool, Node>(false, null);
                matchResult = AddMatchResult(matchResult, result.Item2);
                return Tuple.Create<bool, Node>(true, matchResult);
            }

            if (node is LambdaExpression)
            {
                var convertedNode = (LambdaExpression)node;
                if (Children.Count != 1)
                    return Tuple.Create<bool, Node>(false, null);

                var result = Children[0].Match(convertedNode.Function);
                if (!result.Item1)
                    return Tuple.Create<bool, Node>(false, null);
                matchResult = AddMatchResult(matchResult, result.Item2);
                return Tuple.Create(true, matchResult);
            }

            if (node is MemberExpression)
            {
                var convertedNode = (MemberExpression)node;
                if (Children.Count != 1)
                    return Tuple.Create<bool, Node>(false, null);

                var result = Children[0].Match(convertedNode.Target);
                if (!result.Item1)
                    return Tuple.Create<bool, Node>(false, null);
                matchResult = AddMatchResult(matchResult, result.Item2);
                return Tuple.Create(true, matchResult);
            }

            if (node is Parameter)
            {
                var convertedNode = (Parameter)node;
                if (Children.Count != 1)
                    return Tuple.Create<bool, Node>(false, null);

                var result = Children[0].Match(convertedNode.DefaultValue);
                if (!result.Item1)
                    return Tuple.Create<bool, Node>(false, null);
                matchResult = AddMatchResult(matchResult, result.Item2);
                return Tuple.Create<bool, Node>(true, matchResult);
            }

            if (node is CallExpression)
            {
                var convertedNode = (CallExpression) node;
                if (Children.Count != convertedNode.Args.Count + 1)
                    return Tuple.Create<bool, Node>(false, null);

                var result = Children[0].Match(convertedNode.Target);
                if (!result.Item1)
                    return Tuple.Create<bool, Node>(false, null);
                matchResult = AddMatchResult(matchResult, result.Item2);
                for (var i = 1; i < Children.Count; i++)
                {
                    result = Children[i].Match(convertedNode.Args[i-1]);
                    if (!result.Item1)
                        return Tuple.Create<bool, Node>(false, null);
                    matchResult = AddMatchResult(matchResult, result.Item2);
                }
                return Tuple.Create<bool, Node>(true, matchResult);
            }

            if (node is WhileStatement)
            {
                var convertedNode = (WhileStatement) node;
                var totalChildren = (convertedNode.ElseStatement == null) ? 2 : 3;
                if (totalChildren != Children.Count)
                    return Tuple.Create<bool, Node>(false, null);
                var result = Children[0].Match(convertedNode.Test);
                if (!result.Item1)
                    return Tuple.Create<bool, Node>(false, null);
                matchResult = AddMatchResult(matchResult, result.Item2);

                result = Children[1].Match(convertedNode.Body);
                if (!result.Item1)
                    return Tuple.Create<bool, Node>(false, null);
                matchResult = AddMatchResult(matchResult, result.Item2);

                if (convertedNode.ElseStatement != null)
                {
                    result = Children[2].Match(convertedNode.ElseStatement);
                    if (!result.Item1)
                        return Tuple.Create<bool, Node>(false, null);
                    matchResult = AddMatchResult(matchResult, result.Item2);
                }
                return Tuple.Create<bool, Node>(true, matchResult);
            }
            if (node is IfStatementTest)
            {
                var convertedNode = (IfStatementTest)node;
                var result = Children[0].Match(convertedNode.Test);
                if (!result.Item1)
                    return Tuple.Create<bool, Node>(false, null);
                matchResult = AddMatchResult(matchResult, result.Item2);

                result = Children[1].Match(convertedNode.Body);
                if (!result.Item1)
                    return Tuple.Create<bool, Node>(false, null);
                matchResult = AddMatchResult(matchResult, result.Item2);
                return Tuple.Create<bool, Node>(true, matchResult);
            }

            if (node is IfStatement)
            {
                var convertedNode = (IfStatement)node;
                if (convertedNode.ElseStatement != null)
                {
                    if (Children.Count != convertedNode.Tests.Count + 1)
                        return Tuple.Create<bool, Node>(false, null);
                
                    for (var i = 0; i < Children.Count - 1; i++)
                    {
                        var result = Children[i].Match(convertedNode.Tests[i]);
                        if (!result.Item1)
                            return Tuple.Create<bool, Node>(false, null);
                        matchResult = AddMatchResult(matchResult, result.Item2);
                    }
                    var result2 = Children.Last().Match(convertedNode.ElseStatement);
                    if (!result2.Item1)
                        return Tuple.Create<bool, Node>(false, null);
                    matchResult = AddMatchResult(matchResult, result2.Item2);
                    return Tuple.Create<bool, Node>(true, matchResult);

                }
                else
                {
                    if (Children.Count !=  convertedNode.Tests.Count)
                        return Tuple.Create<bool, Node>(false, null);
                    for (var i = 1; i < Children.Count; i++)
                    {
                        var result = Children[i].Match(convertedNode.Tests[i]);
                        if (!result.Item1)
                            return Tuple.Create<bool, Node>(false, null);
                        matchResult = AddMatchResult(matchResult, result.Item2);
                    }
                    return Tuple.Create<bool, Node>(true, matchResult);
                }
            }

            if (node is FunctionDefinition)
            {
                var convertedNode = (FunctionDefinition)node;
                var currentChild = 0;
                if (convertedNode.Decorators != null && convertedNode.Decorators.Any())
                {
                    foreach (var decoretor in convertedNode.Decorators)
                    {
                        var result = Children[currentChild].Match(decoretor);
                        if (!result.Item1)
                            return Tuple.Create<bool, Node>(false, null);
                        matchResult = AddMatchResult(matchResult, result.Item2);
                        currentChild++;
                    }
                }
                if (convertedNode.Parameters != null && convertedNode.Parameters.Any())
                {
                    foreach (var parameter in convertedNode.Parameters)
                    {
                        var result = Children[currentChild].Match(parameter);
                        if (!result.Item1)
                            return Tuple.Create<bool, Node>(false, null);
                        matchResult = AddMatchResult(matchResult, result.Item2);
                        currentChild++;
                    }
                }
                var result2 = Children[currentChild].Match(convertedNode.Body);
                if (!result2.Item1)
                    return Tuple.Create<bool, Node>(false, null);
                matchResult = AddMatchResult(matchResult, result2.Item2);
                return Tuple.Create<bool, Node>(true, matchResult);
            }
            if (node is ForStatement)
            {
                var convertedNode = (ForStatement)node;
                var totalChildren = (convertedNode.Else == null) ? 3 : 4;
                if (totalChildren != Children.Count)
                    return Tuple.Create<bool, Node>(false, null);
                var result = Children[0].Match(convertedNode.Left);
                if (!result.Item1)
                    return Tuple.Create<bool, Node>(false, null);
                matchResult = AddMatchResult(matchResult, result.Item2);

                result = Children[1].Match(convertedNode.List);
                if (!result.Item1)
                    return Tuple.Create<bool, Node>(false, null);
                matchResult = AddMatchResult(matchResult, result.Item2);

                result = Children[2].Match(convertedNode.Body);
                if (!result.Item1)
                    return Tuple.Create<bool, Node>(false, null);
                matchResult = AddMatchResult(matchResult, result.Item2);

                if (convertedNode.Else != null)
                {
                    result = Children[3].Match(convertedNode.Else);
                    if (!result.Item1)
                        return Tuple.Create<bool, Node>(false, null);
                    matchResult = AddMatchResult(matchResult, result.Item2);
                }
                return Tuple.Create<bool, Node>(true, matchResult);
            }

            throw new NotImplementedException();
        }

        private Node AddMatchResult(Node current, Node matchresult)
        {
            return (matchresult != null && current == null) ? matchresult : current;
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
            if (InnerNode is ConstantExpression)
            {
                var inner = InnerNode as ConstantExpression;
                var comparedNode = node as ConstantExpression;
                if (comparedNode == null) return false;
                return inner.Value.Equals(comparedNode.Value);
            }
            if (InnerNode is AssignmentStatement)
            {
                var comparedNode = node as AssignmentStatement;
                if (comparedNode == null) return false;
                return true;
            }

            if (InnerNode is PythonAst)
            {
                var comparedNode = node as PythonAst;
                if (comparedNode == null) return false;
                return true;
            }

            if (InnerNode is SuiteStatement)
            {
                var comparedNode = node as SuiteStatement;
                if (comparedNode == null) return false;
                return true;
            }
            if (InnerNode is TupleExpression)
            {
                var comparedNode = node as TupleExpression;
                if (comparedNode == null) return false;
                return true;
            }

            if (InnerNode is ExpressionStatement)
            {
                var comparedNode = node as ExpressionStatement;
                if (comparedNode == null) return false;
                return true;
            }
            if (InnerNode is WhileStatement)
            {
                var comparedNode = node as WhileStatement;
                if (comparedNode == null) return false;
                return true;
            }
            if (InnerNode is CallExpression)
            {
                var comparedNode = node as CallExpression;
                if (comparedNode == null) return false;
                return true;
            }
            if (InnerNode is ReturnStatement)
            {
                var comparedNode = node as ReturnStatement;
                if (comparedNode == null) return false;
                return true;
            }
            if (InnerNode is Arg)
            {
                var comparedNode = node as Arg;
                if (comparedNode == null) return false;
                var inner = (Arg) InnerNode;

                if (inner.Name == null && comparedNode.Name == null)
                    return true;
                if (inner.Name == null && comparedNode.Name != null)
                    return false;
                if (inner.Name != null && comparedNode.Name == null)
                    return false;
                return comparedNode.Name.Equals(inner.Name);
            }

            if (InnerNode is Parameter)
            {
                var comparedNode = node as Parameter;
                if (comparedNode == null) return false;
                var inner = (Parameter)InnerNode;

                if (inner.Name == null && comparedNode.Name == null)
                    return true;
                if (inner.Name == null && comparedNode.Name != null)
                    return false;
                if (inner.Name != null && comparedNode.Name == null)
                    return false;
                return comparedNode.Name.Equals(inner.Name);
            }

            if (InnerNode is BinaryExpression)
            {
                var comparedNode = node as BinaryExpression;
                if (comparedNode == null) return false;
                var inner = (BinaryExpression)InnerNode;
                return inner.Operator.Equals(comparedNode.Operator);
            }

            if (InnerNode is AugmentedAssignStatement)
            {
                var comparedNode = node as AugmentedAssignStatement;
                if (comparedNode == null) return false;
                var inner = (AugmentedAssignStatement)InnerNode;
                return inner.Operator.Equals(comparedNode.Operator);
            }

            if (InnerNode is ParenthesisExpression)
            {
                var comparedNode = node as ParenthesisExpression;
                if (comparedNode == null) return false;
                return true;
            }
            if (InnerNode is ForStatement)
            {
                var comparedNode = node as ForStatement;
                if (comparedNode == null) return false;
                return true;
            }
            if (InnerNode is IfStatementTest)
            {
                var comparedNode = node as IfStatementTest;
                if (comparedNode == null) return false;
                return true;
            }

            if (InnerNode is IfStatement)
            {
                var comparedNode = node as IfStatement;
                if (comparedNode == null) return false;
                return true;
            }

            if (InnerNode is FunctionDefinition)
            {
                var comparedNode = node as FunctionDefinition;
                if (comparedNode == null) return false;
                var inner = (FunctionDefinition)InnerNode;
                if (inner.Name == null && comparedNode.Name == null)
                    return true;
                if (inner.Name == null && comparedNode.Name != null)
                    return false;
                if (inner.Name != null && comparedNode.Name == null)
                    return false;
                if (!comparedNode.Name.Equals(inner.Name))
                    return false;
                if (inner.IsGenerator != comparedNode.IsGenerator)
                    return false;
                return inner.IsLambda == comparedNode.IsLambda;
            }
            if (InnerNode is LambdaExpression)
            {
                var comparedNode = node as LambdaExpression;
                if (comparedNode == null) return false;
                return true;
            }
            if (InnerNode is MemberExpression)
            {
                var comparedNode = node as MemberExpression;
                if (comparedNode == null) return false;
                var inner = (MemberExpression)InnerNode;
                return inner.Name.Equals(comparedNode.Name);
            }
            throw new NotImplementedException();
        }

        public void PostWalk(SortedTreeVisitor visitor)
        {
            foreach (var child in Children)
            {
                child.PostWalk(visitor);
            }
            visitor.Nodes.Add(this);
        }
        

        protected bool Equals(PythonNode other)
        {
            return Equals(InnerNode, other.InnerNode) && this.IsAbstract == other.IsAbstract && 
                EditId == other.EditId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((PythonNode)obj);
        }

        public PythonNode GetLeftMostDescendant()
        {
            var walker = new SortedTreeVisitor();
            PostWalk(walker);
            if (walker.Nodes.Count == 0)
                throw new Exception("list should not be empty");
            return walker.Nodes.First();
        }

        public string AbstractType()
        {
            return InnerNode.NodeName;
        }

        public override string ToString()
        {
            var str = new StringBuilder();
            str.Append(InnerNode.NodeName);
            str.Append("(");

            if (IsAbstract)
            {
                str.Append("Abstract"); 
            }
            else
            {
                str.Append(Value);
            }
            if (EditId != 0)
            {
                str.Append(", *"); 
            }
            str.Append(")");

            if (Children.Count > 0)
            {
                str.Append("{ ");
                foreach (var child in Children)
                {
                    str.Append(child);
                    str.Append(" ");
                }
                str.Append("}");
            }
            return str.ToString();
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Value != null ? Value.GetHashCode() : 0) * 397) ^ (InnerNode!= null ? InnerNode.GetHashCode() : 0);
            }
        }

        public bool Similar(PythonNode node1)
        {
            var node =  InnerNode.NodeName + ": " + Value;
            var compared = node1.InnerNode.NodeName + ": " + node1.Value;
            return node.Equals(compared);
        }


        public PythonNode GetCopy()
        {
            var result = new PythonNode(InnerNode, false, EditId);
            if (Parent != null) result.Parent = Parent;
            result.Value = Value;
            foreach (var child in Children)
            {
                result.AddChild(child.GetCopy());
            }
            return result;
        }

        public PythonNode GetAbstractCopy()
        {
            var result = new PythonNode(InnerNode,true, EditId);
            if (Parent != null) result.Parent = Parent;
            result.Value = Value;
            foreach (var child in Children)
            {
                result.AddChild(child.GetAbstractCopy());
            }
            return result;
        }

        public void Walk(IVisitor visitor)
        {
            if (visitor.Visit(this))
                Children.ForEach(child => child.Walk(visitor));
        }

        public bool Contains(PythonNode node)
        {
            if (Match(node.InnerNode).Item1)
                return true;

            foreach (var child in Children)
            {
                var contains = child.Contains(node);
                if (contains)
                    return true;
            }
            return false;
        }

        public Tuple<bool, int> FindHeightTarget(int height)
        {
            if (EditId == 1)
                return Tuple.Create(true, height);
            foreach (var child in Children)
            {
                var result = child.FindHeightTarget(height + 1);
                if (result.Item1)
                    return result;
            }
            return Tuple.Create(false, height);
        }

        public int GetHeight()
        {
            var maxChildHeight = 0; 
            foreach (var child in Children)
            {
                var childHeight = child.GetHeight();
                maxChildHeight = childHeight > maxChildHeight ? childHeight : maxChildHeight;
            }
            return 1 + maxChildHeight;
        }

        public PythonNode GetCorrespondingNode(PythonNode node)
        {
            if (Match(node.InnerNode).Item1)
                return this;

            foreach (var child in Children)
            {
                var childResult = child.GetCorrespondingNode(node);
                if (childResult != null)
                    return childResult;
            }
            return null;
        }

        public int CountAbstract()
        {
            var value = IsAbstract ? 1 : 0;
            var numberOfAbsChildren = Children.Sum(e => e.CountAbstract());
            return value + numberOfAbsChildren; 
        }
    }

    public interface IVisitor
    {
        bool Visit(PythonNode pythonNode);
    }

    public class SubSequentNodesVisitor : IVisitor
    {
        private readonly IEnumerable<Edit> _operations;
        public List<Edit> SubOperations {get; }

        public SubSequentNodesVisitor(IEnumerable<Edit> operations)
        {
            SubOperations = new List<Edit>();
            _operations = operations;
        }

        public bool Visit(PythonNode pythonNode)
        {
            foreach (var operation in _operations)
            {
                if (pythonNode.Equals(operation.ModifiedNode))
                {
                    SubOperations.Add(operation);
                    return true;
                }
            }
            return false; 
        }
    }


}
