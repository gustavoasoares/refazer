using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using IronPython.Compiler.Ast;
using Tutor.synthesis;

namespace Tutor.Transformation
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static class Semantics
    {

        public static IEnumerable<PythonNode> Apply(PythonNode ast, Patch patch)
        {
            var inputs = new List<PythonNode>() {ast};
            var asts = patch.Run(ast);
            return asts;
        }

        public static Patch Patch(IEnumerable<Edit> edits)
        {
            var patch = new Patch(edits.Where(e => e != null).ToList());
            return patch;
        }

        public static Patch CPatch(IEnumerable<Edit> edits, Patch patch)
        {
            patch.EditSets.Insert(0,edits.Where(e => e != null).ToList());
            return patch;
        }

        public static Edit Delete(PythonNode parent, PythonNode deleted)
        {
            var delete = new Delete(deleted, parent);
            return delete;
        }

        public static Edit Update(PythonNode before, PythonNode after)
        {
            var update = new Update(after, before);            
            return update;
        }

        public static Edit Insert(PythonNode parent, PythonNode newNode, int index)
        {
            var insert = new Insert(newNode, parent, index);
            return insert;
        }

        public static Edit Move(PythonNode parent, PythonNode movedNode, int index)
        {
            var move = new Move(movedNode, parent, index);
            return move;
        }

        public static PythonNode LeafConstNode(NodeInfo info)
        {
            var wrapped = NodeWrapper.Wrap(NodeBuilder.Create(info));
            wrapped.Reference = false;
            return wrapped;
        }

        public static PythonNode ConstNode(NodeInfo info, IEnumerable<PythonNode> children)
        {
            var wrapped = NodeBuilder.Create(info, children.ToList());
            wrapped.Reference = false;
            return wrapped;
        }

        public static PythonNode ReferenceNode(PythonNode ast, TreeTemplate template, int k)
        {
            template.Target = true;
            var referenceNode = ReferenceNodeHelper(ast, template, ref k);
            return referenceNode;
        }

        private static PythonNode ReferenceNodeHelper(PythonNode ast, TreeTemplate template, ref int k)
        {
            PythonNode match = null;
            if (FindReferenceNode(template, ast, ref match))
            {
                if (k == 0)
                    return match;
                k--;
            }
            foreach (var child in ast.Children)
            {
                var result = ReferenceNodeHelper(child, template, ref k);
                if (result != null) return result;
            }
            return null;
        }

        private static bool FindReferenceNode(TreeTemplate template, PythonNode node, ref PythonNode  match)
        {
            if (template.Match(node))
            {
                if (template.Target)
                    match = node;

                if (template.Children.Any())
                {
                    if (template.Children.Count != node.Children.Count)
                        return false;
                    for (var i = 0; i < template.Children.Count; i++)
                    {
                        var child = template.Children[i];
                        var childNode = node.Children[i];
                        var result = FindReferenceNode(child, childNode, ref match);
                        if (result == false)
                            return false;
                    }
                }
                return true;
            }
            return false;
        }

        public static IEnumerable<PythonNode> SingleChild(PythonNode node)
        {
            return (node != null) ? new List<PythonNode>() {node} : null;
        }

        public static IEnumerable<PythonNode> Children(PythonNode node, IEnumerable<PythonNode> children)
        {
            if (node == null || children == null)
                return null;

            var result = new List<PythonNode> {node};
            result.AddRange(children);
            return result;
        }

        public static bool Match(PythonNode ast, TreeTemplate template)
        {
            int templateHeight = template.FindHeightTarget(0).Item2;
            var root = ast;
            while (templateHeight > 0)
            {
                if (ast.Parent == null)
                    return false;
                root = ast.Parent;
                templateHeight--;
            }

            PythonNode match = null;
            if (FindReferenceNode(template, root, ref match) && match != null && ast.Equals(match))
            {
                return true;
            }
            return false;
        }

        public static TreeTemplate Node(NodeInfo info, IEnumerable<TreeTemplate> children)
        {
            var treeTemplate = new TreeTemplate(info.NodeType);
            if (info.NodeValue != null) treeTemplate.Value = info.NodeValue;
            treeTemplate.Children = children.ToList(); 
            return treeTemplate; 
        }
        public static TreeTemplate LeafNode(NodeInfo info)
        {
            var wrapped = NodeBuilder.Create(info);
            var treeTemplate = new TreeTemplate(wrapped.GetType().Name + "Node");
            if (info.NodeValue != null) treeTemplate.Value = info.NodeValue;
            return treeTemplate;
        }



        public static TreeTemplate Target(TreeTemplate template)
        {
            TreeTemplate result; 
            if (template is Wildcard)
            {
                result = new Wildcard(template.Type);
            }
            else
            {
                result = new TreeTemplate(template.Type);
                if (template.Value != null)
                    result.Value = template.Value;
            }
            if (template.Children != null && template.Children.Any())
                result.Children = template.Children;
            result.Target = true;
            return result;
        }

        public static TreeTemplate Skip(TreeTemplate template)
        {
            TreeTemplate result;
            if (template is Wildcard)
            {
                result = new Wildcard(template.Type);
            }
            else
            {
                result = new TreeTemplate(template.Type);
                if (template.Value != null)
                    result.Value = template.Value;
            }
            if (template.Children != null && template.Children.Any())
                result.Children = template.Children;
            result.Target = template.Target;
            return result;
        }

        public static TreeTemplate LeafWildcard(string type)
        {
            return new Wildcard(type, true);
        }

        public static TreeTemplate Wildcard(string type, IEnumerable<TreeTemplate> children)
        {
            return new Wildcard(type, children);
        }

        public static IEnumerable<TreeTemplate> TChild(TreeTemplate template)
        {
            return new List<TreeTemplate>() {template}; 
        }

        public static IEnumerable<TreeTemplate> TChildren(TreeTemplate template, IEnumerable<TreeTemplate> templateChildren)
        {
            var result = new List<TreeTemplate>() {template};
            result.AddRange(templateChildren);
            return result;
        }

        public static IEnumerable<PythonNode> InOrderSort(PythonNode ast)
        {
            var visitor = new PythonNodeVisitor();
            ast.Walk(visitor);
            return visitor.SortedNodes;
        }
    }

    public class PythonNodeVisitor : IVisitor
    {
        public List<PythonNode> SortedNodes = new List<PythonNode>();
         
        public bool Visit(PythonNode pythonNode)
        {
            SortedNodes.Add(pythonNode);
            return true;
        }
    }
}
