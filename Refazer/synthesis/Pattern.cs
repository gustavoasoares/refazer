using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Scripting.Metadata;

namespace Tutor.synthesis
{
    public class Pattern
    {
        protected bool Equals(Pattern other)
        {
            return Equals(TreeTemplate, other.TreeTemplate) && Equals(_path, other._path);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Pattern)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ (TreeTemplate != null ? TreeTemplate.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (_path != null ? _path.GetHashCode() : 0);
                return hashCode;
            }
        }

        public TreeTemplate TreeTemplate { set; get; }
        private readonly Path _path;
        private readonly int _d;

        public Pattern(TreeTemplate treeTemplate, int d, Path path)
        {
            TreeTemplate = treeTemplate;
            _path = path;
            _d = d;
        }

        public IList<PythonNode> Matches(PythonNode inp)
        {
            var visitor = new MatchVisitor(this);
            inp.Walk(visitor);
            return visitor.Matches;
        }

        public bool Match(PythonNode node)
        {

            if (_d == 0)
                return FindReferenceNode(TreeTemplate, node);
            if (node.Parent == null)
                return false;
            if (FindReferenceNode(TreeTemplate, node.Parent))
            {
                return _path.Match(node.Parent, node);
            }
            return false;
        }

        private bool FindReferenceNode(TreeTemplate template, PythonNode node)
        {
            if (template.Match(node))
            {
                if (template.Children.Any())
                {
                    if (template.Children.Count != node.Children.Count)
                        return false;
                    for (var i = 0; i < template.Children.Count; i++)
                    {
                        var child = template.Children[i];
                        var childNode = node.Children[i];
                        var result = FindReferenceNode(child, childNode);
                        if (result == false)
                            return false;
                    }
                }
                return true;
            }
            return false;
        }
    }
}
