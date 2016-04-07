using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython;
using IronPython.Compiler;
using IronPython.Compiler.Ast;
using IronPython.Hosting;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Compiler;
using Microsoft.ProgramSynthesis.Extraction.Text.Semantics;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting.Hosting.Providers;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tutor
{
    class Program
    {

        static void Main(string[] args)
        {

            var grammar = DSLCompiler.LoadGrammarFromFile(@"ProseSample.Substrings.grammar");
            //
            // Parse a program in this grammar. PROSE supports two serialization formats:
            // "human-readable" expression format, used in this tutorial, and machine-readable XML.
            var ast = grammar.Value.ParseAST("SubStr(v, PosPair(AbsPos(v, 1), AbsPos(v, 5)))",
                                       ASTSerializationFormat.HumanReadable);
            // Create an input state to the program. It contains one binding: a variable 'inp' (DSL input)
            // is bound to the string "PROSE Rocks".
            var input = State.Create(grammar.Value.InputSymbol, StringRegion.Create("PROSE Rocks"));
            // Execute the program on the input state.
            var output = (StringRegion) ast.Invoke(input);
            Debug.Assert(output.Value == "PROS");

            //            var generateReport = false;

            //            if (generateReport)
            //            {
            //                var report = new Report();
            //                report.GenerateReport();
            //            }

            //            var product = new Tuple<TestBasedCluster.Question, string>(TestBasedCluster.Question.Product,
            //              "C:/Users/Gustavo/Box Sync/pesquisa/tutor/hw02-sp16/" + "mistake_pairs_product_complete.json");
            //            var questionLogs = new[] {product};
            //            var cluster = new TestBasedCluster();
            //            cluster.GenerateCluster(questionLogs);
            //            var clusters = cluster.Clusters[TestBasedCluster.Question.Product];
            //            var values = from pair in clusters
            //                orderby pair.Value.Count descending 
            //                select pair.Value;
            //            var biggest = values.First();

            //            var count = 0;

            //            var children = new List<string>() { "TupleExpression", "NameExpression", "NameExpression", "TupleExpression", "literal", "literal" };
            //            var Match = new Match("AssignmentStatement", children, new BindingInfo() { BindingIndex = 5, BindingValue = 0 });

            //            var newNode = new IronPython.Compiler.Ast.ConstantExpression(1);
            //            var Operation = new Update() { NewNode = newNode };
            //            var fix1 = new Patch(Match, Operation);

            //             children = new List<string>() { "NameExpression", "literal" };
            //             Match = new Match("AssignmentStatement", children, new BindingInfo() { BindingIndex = 2, BindingValue = 0});

            //            var fix2 = new Patch(Match, Operation);

            //            var fixer = new SubmissionFixer();

            //            var testSetup = @"def square(x):
            //    return x * x

            //def identity(x):
            //    return x
            //";
            //            var tests = new Dictionary<String, int>
            //            {
            //                {testSetup + "product(3, identity)", 6},
            //                {testSetup + "product(5, identity)", 120},
            //                {testSetup + "product(3, square)", 36},
            //                {testSetup + "product(5, square)", 14400}
            //            };

            //            foreach (var mistake in biggest)
            //            {
            //                Console.Out.WriteLine("Diff =====================");
            //                Console.Out.WriteLine(mistake.diff);
            //                Console.Out.WriteLine("Before ===================================");
            //                Console.Out.WriteLine(mistake.before);
            //                var isFixed = fixer.Fix(mistake.before, new List<Patch>() {fix1, fix2}, tests);
            //                if (isFixed)
            //                {
            //                    count++;
            //                    Console.Out.WriteLine("Fixed!" + count);
            //                }
            //                else
            //                {
            //                    Console.Out.WriteLine("HELPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPP!" );
            //                }
            //            }
            //            Console.Out.WriteLine("Total tested: " + biggest.Count);
            //            Console.Out.WriteLine("Fixed: " + count);
            //            Console.Out.WriteLine("Not Fixed: " + (biggest.Count -count));
            //            Console.ReadKey();
        }

    }
}
