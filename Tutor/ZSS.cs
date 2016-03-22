using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using IronPython.Compiler.Ast;
using IronPython.Modules;
using Microsoft.Scripting.Utils;
using Microsoft.SqlServer.Server;

namespace Tutor
{
    public class Zss
    {
        public Zss(IronPython.Compiler.Ast.Node t1, IronPython.Compiler.Ast.Node t2)
        {
            var walker = new PostOrderWalker();
            walker.Nodes = new List<Node>();
            t1.Walk(walker);
            A = walker.Nodes;
            T1 = Enumerable.Range(1, A.Count).ToArray();
            walker.Nodes = new List<Node>();
            t2.Walk(walker);
            B = walker.Nodes;
            T2 = Enumerable.Range(1, B.Count).ToArray();
            l1 = ComputeL(T1, A);
            l2 = ComputeL(T2, B);
            K1 = ComputeK(T1, l1);
            K2 = ComputeK(T2, l2);
        }

        private List<Int32> ComputeK(int[] tree,  
            int[] l)
        {
            var result = new List<Int32>();
            for (int i = 0; i < tree.Length; i++)
            {
                var isKeyRoot = true;
                if (i < tree.Length - 1)
                {
                    for (int j = i + 1; j < tree.Length; j++)
                    {
                        if (l[tree[i]] == l[tree[j]])
                            isKeyRoot = false;
                    }
                }
                if (isKeyRoot)
                    result.Add(tree[i]);
            }
            return result;
        }

        private int[] ComputeL(int[] t1, List<Node>  tree)
        {
            var result = new int[t1.Length+1];
            result[0] = 0;
            var walker = new PostOrderWalker();
            foreach (var node in t1)
            {
                walker.Nodes = new List<Node>();
                tree[node-1].PythonNode.Walk(walker);
                if (walker.Nodes.Count == 0)
                    throw new Exception("list should not be empty");
                result[node] = tree.IndexOf(walker.Nodes.First()) + 1;
            }
            return result;
        }

        List<Node> A;
        List<Node> B;
        private int[] T1;
        private int[] T2;
        private int[] l1;
        private int[] l2;
        private List<Int32> K1;
        private List<Int32> K2;
        private Tuple<int, List<String>>[,] treedists;

        public int Compute()
        {
            treedists = new Tuple<int, List<String>>[T1.Length + 1, T2.Length + 1];
            treedists[0, 0] = Tuple.Create(0,new List<string>());
                 
            foreach (var x in K1)
            {
                foreach (var y in K2)
                {
                    Treedists(x, y);
                }
            }
            var edits = treedists[T1.Length, T2.Length].Item2;
            foreach (var edit in edits)
            {
                Console.Out.WriteLine(edit);
            }
            return treedists[T1.Length,T2.Length].Item1;
        }

        private void Treedists(int i, int j)
        {
            var m = i - l1[i] + 2;
            var n = j - l2[j] + 2;

            var fd = new Tuple<int,List<string>>[m, n];
            fd[0, 0] = Tuple.Create(0, new List<string>());
            var ioff = l1[i] - 1;
            var joff = l2[j] - 1;

            for (int x = 1; x < m; x++)
            {
                //cost to delete a node is 1
                var edits = new List<string>(fd[x - 1, 0].Item2);
                edits.Add("delete node: " + x);
                fd[x, 0] = Tuple.Create(fd[x - 1, 0].Item1 + 1, edits); 
            }
            for (int y = 1; y < n; y++)
            {
                var node = B[y - 1 + joff];
                var parent = node.PythonNode.Parent;
                var parentName = (parent == null) ? "" : parent.NodeName;

                var edits = new List<string>(fd[0, y - 1].Item2);
                edits.Add("insert node: " + node);
                //cost do add a node is 1
                fd[0, y] = Tuple.Create(fd[0, y - 1].Item1 + 1, edits);
            }

            for (int x = 1; x < m; x++)
            {
                for (int y = 1; y < n; y++)
                {
                    if (l1[i] == l1[x + ioff] && l2[j] == l2[y + joff])
                    {
                        var value = Math.Min(Math.Min(fd[x - 1, y].Item1 + 1, //cost to remove is 1
                            fd[x, y - 1].Item1 + 1), //cost to insert is 1
                            fd[x-1,y-1].Item1 + CostUpdate(A[x+ioff-1], B[y+joff-1])); //cost to update depends

                        List<string> edits;
                        if (value == fd[x - 1, y].Item1 + 1)
                        {
                            var node = A[x - 1];
                            var parent = node.PythonNode.Parent;
                            var parentName = (parent == null) ? "" : parent.ToString();
                            edits = new List<string>(fd[x - 1, y].Item2) {"remove node: " + node};
                        } else if (value == fd[x, y - 1].Item1 + 1)
                        {
                            var node = B[y - 1 + joff];
                            var parent = node.PythonNode.Parent;
                            var parentName = (parent == null) ? "" : parent.NodeName;
                            edits = new List<string>(fd[x, y - 1].Item2) {"insert node: " + node};
                        }
                        else
                        {
                            edits = new List<string>(fd[x - 1, y - 1].Item2); 
                            if (CostUpdate(A[x + ioff - 1], B[y + joff - 1]) > 0)
                            {
                                edits.Add("update node: " + A[x + ioff - 1] + " to: " + B[y + joff - 1]);
                            }
                        }

                        fd[x, y] = Tuple.Create(value, edits);
                        treedists[x + ioff, y + joff] = fd[x, y];
                    }
                    else
                    {
                        var p = l1[x + ioff] - 1 - ioff;
                        var q = l2[y + joff] - 1 - joff;

                        var value = Math.Min(fd[p, q].Item1 + treedists[x + ioff, y + joff].Item1, 
                                Math.Min(fd[x - 1, y].Item1 + 1, fd[x, y - 1].Item1 + 1));

                        List<string> edits;
                        if (value == fd[p, q].Item1 + treedists[x + ioff, y + joff].Item1)
                        {
                            edits = new List<string>(fd[p, q].Item2);
                            edits.AddRange(treedists[x + ioff, y + joff].Item2);
                        }
                        else if (value == fd[x - 1, y].Item1 + 1)
                        {
                            edits = new List<string>(fd[x - 1, y].Item2);
                            edits.Add("delete node: " + A[x - 1]);
                        }
                        else
                        {
                            var node = B[y - 1 + joff];
                            var parent = node.PythonNode.Parent;
                            var parentName = (parent == null) ? "" : parent.NodeName;
                            edits = new List<string>(fd[x, y - 1].Item2);
                            edits.Add("insert node: " + B[y - 1]);
                        }
                        fd[x, y] = Tuple.Create(value, edits);
                    }
                }
            }
        }

        private int CostUpdate(Node node, Node node1)
        {
            return (node.Similar(node1)) ? 0 : 1;
        }
    }

    public class Node
    {
        public string Label { set; get; }

        public IronPython.Compiler.Ast.Node PythonNode { set; get; }

        protected bool Equals(Node other)
        {
            return string.Equals(Label, other.Label) && Equals(PythonNode, other.PythonNode);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Node) obj);
        }


        public override string ToString()
        {
            return PythonNode.NodeName + "-" +  Label;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Label != null ? Label.GetHashCode() : 0)*397) ^ (PythonNode != null ? PythonNode.GetHashCode() : 0);
            }
        }

        public bool Similar(Node node1)
        {
            return this.ToString().Equals(node1.ToString());
        }
    }

    class PostOrderWalker : PythonWalker
    {
        public List<Node> Nodes {set; get;}


        /// <summary>
        /// Wraps an IronPython node to a Node with a label related to specific properties of each node
        /// </summary>
        /// <param name="label"></param>
        /// <param name="node"></param>
        private void AddNode(string label, IronPython.Compiler.Ast.Node node)
        {
            Nodes.Add(new Node()
            {
                Label = label,
                PythonNode = node
            });
        }

        public override void PostWalk(BinaryExpression node)
        {
            AddNode(node.Operator.ToString(), node);
            base.PostWalk(node);
        }

        public override void PostWalk(ConstantExpression node)
        {
            var label = node.Type.FullName + ": " + node.Value;
            AddNode(label, node);
            base.PostWalk(node);
        }

        public override void PostWalk(NameExpression node)
        {
            var label = node.Name;
            AddNode(label, node);
            base.PostWalk(node);
        }

        public override void PostWalk(ExpressionStatement node)
        {
            var label = node.Expression.Type.FullName;
            AddNode(label, node);
        }

        public override void PostWalk(AndExpression node)
        {
            var label = node.NodeName;
            AddNode(label, node);
        }

        public override void PostWalk(BackQuoteExpression node)
        {
            var label = node.NodeName;
            AddNode(label, node);
        }


        public override void PostWalk(CallExpression node)
        {
            var label = node.Target.NodeName;
            AddNode(label, node);
        }

        public override void PostWalk(ConditionalExpression node)
        {
            var label = node.NodeName;
            AddNode(label, node);
        }


        public override void PostWalk(IndexExpression node)
        {
            var label = node.Index.NodeName;
            AddNode(label, node);
        }

        public override void PostWalk(LambdaExpression node)
        {
            var label = node.Function.Name;
            AddNode(label, node);
        }

        public override void PostWalk(ListComprehension node)
        {
            var label = node.NodeName;
            AddNode(label, node);
        }

        public override void PostWalk(ListExpression node)
        {
            var label = node.NodeName;
            AddNode(label, node);
        }

        public override void PostWalk(MemberExpression node)
        {
            var label = node.Target.Type.FullName + ": " + node.Name;
            AddNode(label, node);
        }
        
        public override void PostWalk(OrExpression node)
        {
            var label = node.NodeName;
            AddNode(label, node);
        }

        public override void PostWalk(ParenthesisExpression node)
        {
            var label = node.NodeName;
            AddNode(label, node);
        }

        public override void PostWalk(SetComprehension node)
        {
            var label = node.NodeName;
            AddNode(label, node);
        }

        public override void PostWalk(SetExpression node)
        {
            var label = node.NodeName;
            AddNode(label, node);
        }

        public override void PostWalk(SliceExpression node)
        {
            var label = node.NodeName;
            AddNode(label, node);
        }

        public override void PostWalk(TupleExpression node)
        {
            var label = node.NodeName;
            AddNode(label, node);
        }

        public override void PostWalk(UnaryExpression node)
        {
            var label = node.NodeName + "" +  node.Op.ToString();
            AddNode(label, node);
        }

        public override void PostWalk(YieldExpression node)
        {
            var label = node.NodeName; 
            AddNode(label, node);
        }

        public override void PostWalk(AssertStatement node)
        {
            var label = node.NodeName;
            AddNode(label, node);
        }

        public override void PostWalk(AssignmentStatement node)
        {
            var label = node.NodeName;
            AddNode(label, node);
        }

        public override void PostWalk(Arg node)
        {
            var label = node.NodeName;
            AddNode(label, node);
        }

        public override void PostWalk(AugmentedAssignStatement node)
        {
            var label = node.NodeName + ": " +  node.Operator.ToString();
            AddNode(label, node);
        }

        public override void PostWalk(BreakStatement node)
        {
            var label = node.NodeName;
            AddNode(label, node);
        }

        public override void PostWalk(PythonAst node)
        {
            var label = node.NodeName;
            AddNode(label, node);
        }

        public override void PostWalk(PrintStatement node)
        {
            var label = node.NodeName;
            AddNode(label, node);
        }

        public override void PostWalk(ClassDefinition node)
        {

            var label = node.Name;
            AddNode(label, node);
        }

        public override void PostWalk(WhileStatement node)
        {
            var label = node.NodeName;
            AddNode(label, node);
            base.PostWalk(node);
        }

        public override void PostWalk(ComprehensionFor node)
        {
            var label = node.NodeName;
            AddNode(label, node);
            base.PostWalk(node);
        }

        public override void PostWalk(Parameter node)
        {
            var label = node.Name;
            AddNode(label, node);
            base.PostWalk(node);
        }

        public override void PostWalk(IfStatementTest node)
        {
            var label = node.NodeName;
            AddNode(label, node);
            base.PostWalk(node);
        }

        public override void PostWalk(ComprehensionIf node)
        {
            var label = node.NodeName;
            AddNode(label, node);
            base.PostWalk(node);
        }

        public override void PostWalk(ContinueStatement node)
        {
            var label = node.NodeName;
            AddNode(label, node);
            base.PostWalk(node);
        }

        public override void PostWalk(EmptyStatement node)
        {
            var label = node.NodeName;
            AddNode(label, node);
            base.PostWalk(node);
        }

        public override void PostWalk(ForStatement node)
        {
            var label = node.NodeName;
            AddNode(label, node);
            base.PostWalk(node);
        }

        public override void PostWalk(IfStatement node)
        {
            var label = node.NodeName;
            AddNode(label, node);
            base.PostWalk(node);
        }

        public override void PostWalk(ReturnStatement node)
        {
            var label = node.NodeName;
            AddNode(label, node);
            base.PostWalk(node);
        }

        public override void PostWalk(FunctionDefinition node)
        {
            var label = node.Name;
            AddNode(label, node);
            base.PostWalk(node);
        }
    }

}