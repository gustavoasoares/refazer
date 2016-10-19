using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Compiler;
using Microsoft.ProgramSynthesis.Diagnostics;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Learning.Logging;
using Microsoft.ProgramSynthesis.Specifications;
using Tutor.Transformation;

namespace Tutor
{
    /// <summary>
    /// Python Instantiation of refazer
    /// </summary>
    public class Refazer4Python : Refazer
    {
        /// <summary>
        /// path to _prose grammar
        /// </summary>
        private string _pathToGrammar;

        /// <summary>
        /// path to Prose dependences 
        /// </summary>
        private string _pathToDslLib;

        private SynthesisEngine _prose;

        /// <summary>
        /// Prose grammar 
        /// </summary>  
        public Result<Grammar> Grammar { get; }

        public Refazer4Python(string pathToGrammar = @"..\..\..\Tutor\synthesis\", string pathToDslLib = @"..\..\Tutor\bin\debug")
        {
            _pathToGrammar = pathToGrammar;
            _pathToDslLib = pathToDslLib;
            Grammar = DSLCompiler.LoadGrammarFromFile(pathToGrammar + @"Transformation.grammar",
                    libraryPaths: new[] { pathToDslLib });
            _prose = new SynthesisEngine(Grammar.Value, new SynthesisEngine.Config { LogListener = new LogListener() });

        }

        public IEnumerable<ProgramNode> LearnTransformations(List<Tuple<string, string>> examples,
            int numberOfPrograms = 1, string ranking = "specific")
        {
            var spec = CreateExampleSpec(examples);
            RankingScore.ScoreForContext = ranking.Equals("specific") ? 100 : -100;
            var learned = _prose.LearnGrammarTopK(spec, "Score", numberOfPrograms);
            return learned;
        }

        private ExampleSpec CreateExampleSpec(List<Tuple<string, string>> examples)
        {
            var proseExamples = new Dictionary<State, object>();
            foreach (var example in examples)
            {
                var input = CreateInputState(example.Item1);
                var astAfter = NodeWrapper.Wrap(ASTHelper.ParseContent(example.Item2));
                proseExamples.Add(input, astAfter);
            }
            var spec = new ExampleSpec(proseExamples);
            return spec;
        }

        public State CreateInputState(string program)
        {
            var astBefore = NodeWrapper.Wrap(ASTHelper.ParseContent(program));
            var input = State.Create(Grammar.Value.InputSymbol, astBefore);
            return input;
        }

        public IEnumerable<string> Apply(ProgramNode transformation, string program)
        {
            var unparser = new Unparser();
            var result = transformation.Invoke(CreateInputState(program)) as IEnumerable<PythonNode>;
            return result == null ?  new List<string>() : result.Select(x => unparser.Unparse(x)); 
        }
    }
}
