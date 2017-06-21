using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Extraction.Text.Semantics;
using Tutor.synthesis;

namespace Tutor
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class RankingScore : Feature<double>
    {
        public RankingScore(Grammar grammar) : base(grammar, "Score", isComplete: true) { }

        public static double ScoreForContext = 0;

        public const double VariableScore = 0;

        protected override double GetFeatureValueForVariable(VariableNode variable) => 0;

        [FeatureCalculator("Apply")]
        public static double Score_Apply(double x, double patch) => patch;

        [FeatureCalculator("InOrderSort")]
        public static double Score_InOrderSort(double document) => document;

        [FeatureCalculator("SingleChild")]
        public static double Score_SingleChild(double n) => n;

        [FeatureCalculator("Children", Method = CalculationMethod.FromChildrenFeatureValues)]
        public static double Score_Children(double n, double children) => n + children;

        [FeatureCalculator("Update")]
        public static double Score_Update(double n, double node) => (n + node);

        [FeatureCalculator("Insert")]
        public static double Score_Insert(double n, double node, double k) => (n + node + k);

        [FeatureCalculator("Move")]
        public static double Score_Move(double n, double node, double k) => (n + node + k);

        [FeatureCalculator("Delete")]
        public static double Score_Delete(double n, double r) => (n + r);

        [FeatureCalculator("Match")]
        public static double Score_Match(double x, double template) => template;

        [FeatureCalculator("Pattern")]
        public static double Score_Pattern(double treetemplate, double templateChildren) => treetemplate + templateChildren;

        [FeatureCalculator("LeafPattern")]
        public static double Score_LeafPattern(double treetemplate) => treetemplate;

        [FeatureCalculator("Node")]
        public static double Score_Node(double info) => 1;

        [FeatureCalculator("Type")]
        public static double Score_Type(double type) => 2;

        [FeatureCalculator("Relative")]
        public static double Score_Relative(double token, double k) => 7 * token;

        [FeatureCalculator("Path")]
        public static double Score_Path(double k) => 1 + k;

        [FeatureCalculator("Context")]
        public static double Score_Context(double k, double template, double path) => k == 0 ? (template + path) : 2 * template + path;


        [FeatureCalculator("TChild")]
        public static double Score_TemplateChild(double template) => template;

        [FeatureCalculator("TChildren")]
        public static double Score_TemplateChildren(double template, double templateChildren) => template + templateChildren;

        [FeatureCalculator("Selected")]
        public static double Score_Selected(double match, double nodes) => match + nodes;

        [FeatureCalculator("EditMap", Method = CalculationMethod.FromChildrenFeatureValues)]
        public static double Score_EditMap(double edit, double selectednodes) => edit + selectednodes;

        [FeatureCalculator("Patch")]
        public static double Score_Patch(double editset) => editset;

        [FeatureCalculator("CPatch", Method = CalculationMethod.FromChildrenFeatureValues)]
        public static double Score_ConcatPatch(double editSet, double patch) => editSet + patch;

        [FeatureCalculator("ReferenceNode")]
        public static double Score_ReferenceNode(double node, double template, double magick) => template;

        [FeatureCalculator("LeafConstNode")]
        public static double Score_LeafConstNode(double info)
        {
            return 0.5;
        }

        [FeatureCalculator("ConstNode")]
        public static double Score_ConstNode(double info, double children)
        {
            return children;
        }

        [FeatureCalculator("k", Method = CalculationMethod.FromLiteral)]
        public static double KScore(int k) => k;

        [FeatureCalculator("magicK", Method = CalculationMethod.FromLiteral)]
        public static double MagicKScore(MagicK k) => 1;

        [FeatureCalculator("type", Method = CalculationMethod.FromLiteral)]
        public static double TypeScore(string type) => (type.Equals("any")) ? 1 : 2;

        [FeatureCalculator("value", Method = CalculationMethod.FromLiteral)]
        public static double ValueScore(dynamic value) => 0;

        [FeatureCalculator("info", Method = CalculationMethod.FromLiteral)]
        public static double InfoScore(NodeInfo info) => 1;

    }
}
