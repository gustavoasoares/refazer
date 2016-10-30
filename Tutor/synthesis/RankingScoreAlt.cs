using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Extraction.Text.Semantics;
using Tutor.synthesis;

namespace Tutor.Transformation
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static class RankingScoreAlt
    {
        public const double VariableScore = 0;

        [FeatureCalculator("Apply")]
        public static double Score_Apply(double x, double patch) => Math.Log(patch);

        [FeatureCalculator("InOrderSort")]
        public static double Score_InOrderSort(double document) => document;

        [FeatureCalculator("SingleChild")]
        public static double Score_SingleChild(double n) => -1 + n;

        [FeatureCalculator("Children", Method = CalculationMethod.FromChildrenFeatureValues)]
        public static double Score_Children(double n, double children) => -1 + n + children;

        [FeatureCalculator("Update")]
        public static double Score_Update(double n, double node) => n + node;

        [FeatureCalculator("Insert")]
        public static double Score_Insert(double n, double node, double k) => n +  node + k;

        [FeatureCalculator("Move")]
        public static double Score_Move(double n, double node, double k) => n + node + k;

        [FeatureCalculator("Delete")]
        public static double Score_Delete(double n, double r) => n +  r;

        [FeatureCalculator("Match")]
        public static double Score_Match(double x, double template) => x + template;

        [FeatureCalculator("Node")]
        public static double Score_Tree(double info, double templateChildren) => -1 + templateChildren;

        [FeatureCalculator("LeafNode")]
        public static double Score_Node(double info) => -2;

        [FeatureCalculator("LeafWildcard")]
        public static double Score_Wildcard(double type) => (100 * type) / 10000;

        [FeatureCalculator("Wildcard")]
        public static double Score_Wildcard(double type, double children ) => (50 * type)/1000000 + children;

        [FeatureCalculator("Target")]
        public static double Score_Target(double template) => template;

        [FeatureCalculator("StartsWithParent")]
        public static double Score_StartsWithParent(double template) => template ;

        [FeatureCalculator("StartsWithTarget")]
        public static double Score_StartsWithTarget(double template) => template;

        [FeatureCalculator("TChild")]
        public static double Score_TemplateChild(double template) => -1 + template;

        [FeatureCalculator("TChildren")]
        public static double Score_TemplateChildren(double template, double templateChildren) => -1 + template + templateChildren;

        [FeatureCalculator("Selected")]
        public static double Score_Selected(double match, double nodes) => match + nodes;

        [FeatureCalculator("EditMap", Method = CalculationMethod.FromChildrenFeatureValues)]
        public static double Score_EditMap(double edit, double selectednodes) => edit + selectednodes;

        [FeatureCalculator("Patch")]
        public static double Score_Patch(double editset) => editset;

        [FeatureCalculator("CPatch", Method = CalculationMethod.FromChildrenFeatureValues)]
        public static double Score_ConcatPatch(double editSet, double patch) => editSet + patch;

        [FeatureCalculator("ReferenceNode")]
        public static double Score_ReferenceNode(double node, double template, double k) => template;

        [FeatureCalculator("LeafConstNode")]
        public static double Score_LeafConstNode(double info)
        {
            return -5; 
        }

        [FeatureCalculator("ConstNode")]
        public static double Score_ConstNode(double info, double children)
        {
            return -10 + children;
        }

        [FeatureCalculator(Method = CalculationMethod.FromLiteral)]
        public static double KScore(int k) => k < 0 ? 1 : 2;

        [FeatureCalculator(Method = CalculationMethod.FromLiteral)]
        public static double MagicKScore(MagicK k) => 1;

        [FeatureCalculator(Method = CalculationMethod.FromLiteral)]
        public static double TypeScore(string type) => (type.Equals("any")) ? 1 : 2;

        [FeatureCalculator(Method = CalculationMethod.FromLiteral)]
        public static double ValueScore(dynamic value) => 0;

        [FeatureCalculator(Method = CalculationMethod.FromLiteral)]
        public static double InfoScore(NodeInfo info) => 1;
    }
}
