using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using IronPython.Compiler.Ast;

namespace Tutor
{
    /// <summary>
    /// Class that implements the Zhang and Shasha algoritm for tree edit distance
    /// Reference: http://research.cs.queensu.ca/TechReports/Reports/1995-372.pdf
    /// </summary>
    public abstract class Zss<T>
    {
        /// <summary>
        /// ASTs in some representation T of the programs before and after the change
        /// </summary>
        protected T PreviousTree, CurrentTree; 

        /// <summary>
        /// AST of the previous and the current programs wrapped in the ZssNode class
        /// </summary>
        protected List<ZssNode<T>> A, B;
        /// <summary>
        /// list of vertices of previous and current trees sorted by postorder traversal. 
        /// Each vertice is represented by an interger. 
        /// </summary>
        protected int[] T1, T2;
        /// <summary>
        /// the leftmost leaf descendant of the subtree rooted at i
        /// </summary>
        private int[] _l1,_l2;
        /// <summary>
        /// keyroots of the asts before and after the change
        /// </summary>
        private List<Int32> _k1, _k2;
        /// <summary>
        /// dynamic programming table with the edit distances as Tuple. The first item is the cost
        /// the second item is the list of edit operations 
        /// </summary>
        private Tuple<int, HashSet<Operation>>[,] _treedists;

        /// <summary>
        /// Create an object to compute the edit distance given two trees
        /// </summary>
        /// <param name="previousTree">Tree before the change</param>
        /// <param name="currentTree">Tree after the change</param>
        protected Zss(T previousTree, T currentTree)
        {
            PreviousTree = previousTree;
            CurrentTree = currentTree;
        }


        /// <summary>
        /// Abstract method that wraps the given trees into the ZssNode class and generate 
        /// the list of vertices of the trees
        /// </summary>
        /// <param name="t1">Tree before the change</param>
        /// <param name="t2">Tree after the change</param>
        protected abstract void GenerateNodes(T t1, T t2);

        /// <summary>
        /// Generate keyroots of tree T , K(T) = { k E T | !e k' > k with l(k') = l(k) }
        /// </summary>
        /// <param name="tree">list of vertices of the tree sorted in post order</param>
        /// <param name="l">the leftmost leaf descendant of the subtree rooted at i</param>
        /// <returns></returns>
        private List<Int32> ComputeK(int[] tree,  
            int[] l)
        {
            var result = new List<Int32>();
            //for each vertice, checks if the keyroot condition is valid. If so, add it
            //to the keyroot list
            for (var i = 0; i < tree.Length; i++)
            {
                var isKeyRoot = true;
                if (i < tree.Length - 1)
                {
                    for (var j = i + 1; j < tree.Length; j++)
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

        /// <summary>
        /// Compute the list l, where l(i) is the leftmost leaf descendant of the subtree rooted at i
        /// </summary>
        /// <param name="t1">tree sorted in post order</param>
        /// <param name="tree">actual tree to get the left most descendant for each i</param>
        /// <returns></returns>
        private int[] ComputeL(int[] t1, List<ZssNode<T>>  tree)
        {
            var result = new int[t1.Length+1];
            result[0] = 0;
            foreach (var node in t1)
            {
                var currentNode = tree[node-1];
                result[node] = tree.IndexOf(currentNode.GetLeftMostDescendant()) + 1;
            }
            return result;
        }

       /// <summary>
       /// Compute the tree edit distance
       /// </summary>
       /// <returns>Returns a tuple. The first item is the cost. The second item is the 
       /// sequence of edit operations</returns>
        public Tuple<int, HashSet<Operation>>  Compute()
        {
            GenerateNodes(PreviousTree, CurrentTree);
            _l1 = ComputeL(T1, A);
            _l2 = ComputeL(T2, B);
            _k1 = ComputeK(T1, _l1);
            _k2 = ComputeK(T2, _l2);

            _treedists = new Tuple<int, HashSet<Operation>>[T1.Length + 1, T2.Length + 1];

            _treedists[0, 0] = Tuple.Create(0,new HashSet<Operation>());
                 
            foreach (var x in _k1)
            {
                foreach (var y in _k2)
                {
                    Treedists(x, y);
                }
            }
            
            return _treedists[T1.Length,T2.Length];
        }

        private void Treedists(int i, int j)
        {
            var m = i - _l1[i] + 2;
            var n = j - _l2[j] + 2;

            var fd = new Tuple<int, HashSet<Operation>>[m, n];
            fd[0, 0] = Tuple.Create(0, new HashSet<Operation>());
            var ioff = _l1[i] - 1;
            var joff = _l2[j] - 1;

            for (int x = 1; x < m; x++)
            {
                //cost to delete a ZssNode is 1
                var edits = new HashSet<Operation>(fd[x - 1, 0].Item2);
                edits.Add(new Delete(A[x +ioff -1].InternalNode as Node, null));
                fd[x, 0] = Tuple.Create(fd[x - 1, 0].Item1 + 1, edits); 
            }
            for (int y = 1; y < n; y++)
            {
                var node = B[y - 1 + joff];
                var edits = new HashSet<Operation>(fd[0, y - 1].Item2);
                edits.Add(new Insert(node.InternalNode as Node, null));
                //cost do add a ZssNode is 1
                fd[0, y] = Tuple.Create(fd[0, y - 1].Item1 + 1, edits);
            }

            for (int x = 1; x < m; x++)
            {
                for (int y = 1; y < n; y++)
                {
                    if (_l1[i] == _l1[x + ioff] && _l2[j] == _l2[y + joff])
                    {
                        var value = Math.Min(Math.Min(fd[x - 1, y].Item1 + 1, //cost to remove is 1
                            fd[x, y - 1].Item1 + 1), //cost to insert is 1
                            fd[x-1,y-1].Item1 + CostUpdate(A[x+ioff-1], B[y+joff-1])); //cost to Operation depends

                        HashSet<Operation> edits;
                        if (value == fd[x - 1, y].Item1 + 1)
                        {
                            var node = A[x - 1];
                            edits = new HashSet<Operation>(fd[x - 1, y].Item2) {new Delete(node.InternalNode as Node, null)};
                        } else if (value == fd[x, y - 1].Item1 + 1)
                        {
                            var node = B[y - 1 + joff];
                            edits = new HashSet<Operation>(fd[x, y - 1].Item2) { new Insert(node.InternalNode as Node, null) };
                        }
                        else
                        {
                            edits = new HashSet<Operation>(fd[x - 1, y - 1].Item2); 
                            if (CostUpdate(A[x + ioff - 1], B[y + joff - 1]) > 0)
                            {
                                var oldNode = A[x + ioff - 1].InternalNode as Node;
                                var newNode = B[y + joff - 1].InternalNode as Node;
                                edits.Add(new Update(newNode, oldNode));
                            }
                        }

                        fd[x, y] = Tuple.Create(value, edits);
                        _treedists[x + ioff, y + joff] = fd[x, y];
                    }
                    else
                    {
                        var p = _l1[x + ioff] - 1 - ioff;
                        var q = _l2[y + joff] - 1 - joff;

                        var value = Math.Min(fd[p, q].Item1 + _treedists[x + ioff, y + joff].Item1, 
                                Math.Min(fd[x - 1, y].Item1 + 1, fd[x, y - 1].Item1 + 1));

                        HashSet<Operation> edits;
                        if (value == fd[p, q].Item1 + _treedists[x + ioff, y + joff].Item1)
                        {
                            edits = new HashSet<Operation>(fd[p, q].Item2);
                            edits.UnionWith(_treedists[x + ioff, y + joff].Item2);
                        }
                        else if (value == fd[x - 1, y].Item1 + 1)
                        {
                            edits = new HashSet<Operation>(fd[x - 1, y].Item2);
                            edits.Add(new Delete(A[x - 1].InternalNode as Node, null));
                        }
                        else
                        {
                            edits = new HashSet<Operation>(fd[x, y - 1].Item2);
                            edits.Add(new Insert(B[y - 1].InternalNode as Node, null));
                        }
                        fd[x, y] = Tuple.Create(value, edits);
                    }
                }
            }
        }

        private int CostUpdate(ZssNode<T> zssNode, ZssNode<T> node1)
        {
            return (zssNode.Similar(node1)) ? 0 : 1;
        }
    }

    public class PythonZss : Zss<Node>
    {
        public PythonZss(Node previousTree, Node currentTree)
            : base(previousTree,currentTree)
        {
        }

        protected override void GenerateNodes(Node t1, Node t2)
        {
            var walker = new PostOrderWalker();
            walker.Nodes = new List<ZssNode<Node>>();
            t1.Walk(walker);
            A = walker.Nodes;
            T1 = Enumerable.Range(1, A.Count).ToArray();
            walker.Nodes = new List<ZssNode<Node>>();
            t2.Walk(walker);
            B = walker.Nodes;
            T2 = Enumerable.Range(1, B.Count).ToArray();
        }
    }


    public abstract class ZssNode<T>
    {
        public string Label { set; get; }

        public T InternalNode { set; get; }

        public abstract bool Similar(ZssNode<T> other);

        public abstract ZssNode<T> GetLeftMostDescendant();

        public abstract string AbstractType();
    }


    class PythonZssNode : ZssNode<Node>
    {
        protected bool Equals(PythonZssNode other)
        {
            return string.Equals(Label, other.Label) && Equals(InternalNode, other.InternalNode);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((PythonZssNode)obj);
        }

        public override ZssNode<Node> GetLeftMostDescendant()
        {
            var walker = new PostOrderWalker();
            walker.Nodes = new List<ZssNode<Node>>();
            InternalNode.Walk(walker);
            if (walker.Nodes.Count == 0)
                throw new Exception("list should not be empty");
            return walker.Nodes.First();
        }

        public override string AbstractType()
        {
            return InternalNode.NodeName;
        }

        public override string ToString()
        {
            return InternalNode.NodeName + "-" + Label;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Label != null ? Label.GetHashCode() : 0) * 397) ^ (InternalNode != null ? InternalNode.GetHashCode() : 0);
            }
        }

        public override bool Similar(ZssNode<Node> node1)
        {
            return ToString().Equals(node1.ToString());
        }
    }

    class PostOrderWalker : PythonWalker
    {
        public List<ZssNode<Node>> Nodes {set; get;}


        /// <summary>
        /// Wraps an IronPython ZssNode to a ZssNode with a label related to specific properties of each ZssNode
        /// </summary>
        /// <param name="label"></param>
        /// <param name="node"></param>
        private void AddNode(string label, Node node)
        {
            Nodes.Add(new PythonZssNode()
            {
                Label = label,
                InternalNode = node
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