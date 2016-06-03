using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Extraction.Text.Semantics;

namespace Tutor.Transformation
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static class RankingScore
    {
        public const double VariableScore = 0;

        [FeatureCalculator("Apply")]
        public static double Score_Apply(double x, double patch) => Math.Log(patch);

        [FeatureCalculator("InOrderSort")]
        public static double Score_InOrderSort(double document) => document;

        [FeatureCalculator("SingleChild")]
        public static double Score_SingleChild(double n) => n/10;

        [FeatureCalculator("Children", Method = CalculationMethod.FromChildrenFeatureValues)]
        public static double Score_Children(double n, double children) => n + children;

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

        [FeatureCalculator("Tree")]
        public static double Score_Tree(double info, double templateChildren) => 3 + templateChildren;

        [FeatureCalculator("Node")]
        public static double Score_Node(double info) => 2;

        [FeatureCalculator("Variable")]
        public static double Score_Variable(double type) => 1;

        [FeatureCalculator("TemplateChild")]
        public static double Score_TemplateChild(double template) => template;


        [FeatureCalculator("TemplateChildren")]
        public static double Score_TemplateChildren(double template, double templateChildren) => template + templateChildren;

        [FeatureCalculator("Selected")]
        public static double Score_Selected(double match, double nodes) => match + nodes;

        [FeatureCalculator("EditMap", Method = CalculationMethod.FromChildrenFeatureValues)]
        public static double Score_EditMap(double edit, double selectednodes) => edit + selectednodes;

        [FeatureCalculator("Patch")]
        public static double Score_Patch(double editset) => editset;

        [FeatureCalculator("ConcatPatch", Method = CalculationMethod.FromChildrenFeatureValues)]
        public static double Score_ConcatPatch(double editSet, double patch) => editSet + patch;

        [FeatureCalculator("ReferenceNode")]
        public static double Score_ReferenceNode(double node, double template) => node - template;

        [FeatureCalculator("LeafConstNode")]
        public static double Score_LeafConstNode(double info)
        {
            return info * 10; 
        }

        [FeatureCalculator("ConstNode")]
        public static double Score_ConstNode(double info, double children)
        {
            return info * 10 + children;
        }

        [FeatureCalculator(Method = CalculationMethod.FromLiteral)]
        public static double KScore(int k) => 1;

        [FeatureCalculator(Method = CalculationMethod.FromLiteral)]
        public static double TypeScore(string type) => 0;

        [FeatureCalculator(Method = CalculationMethod.FromLiteral)]
        public static double InfoScore(NodeInfo info) => 1;

    }
}
