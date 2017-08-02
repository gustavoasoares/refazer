using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Compiler;
using Microsoft.ProgramSynthesis.Compound.Extraction.Field;
using Microsoft.ProgramSynthesis.Diagnostics;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Learning.Logging;
using Microsoft.ProgramSynthesis.Learning.Strategies;
using Microsoft.ProgramSynthesis.Specifications;
using Tutor;
using Tutor.Transformation;

namespace Refazer.Core
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

        public Refazer4Python(string pathToGrammar = @"..\..\..\Tutor\synthesis\", string pathToDslLib = @"..\..\..\Tutor\bin\debug")
        {

            _pathToGrammar = pathToGrammar;
            _pathToDslLib = pathToDslLib;
            Grammar = DSLCompiler.ParseGrammarFromFile(pathToGrammar + @"Transformation.grammar",
                    libraryPaths: new[] { pathToDslLib });

        }

        public IEnumerable<Transformation> LearnTransformations(List<Tuple<string, string>> examples,
            int numberOfPrograms = 1, string ranking = "specific")
        {
            var spec = CreateExampleSpec(examples);
            //TODO: this is not thread safe. If multiple instances of Refazer are changing 
            //the value of the ranking scores, we can have a problem.
            RankingScore.ScoreForContext = ranking.Equals("specific") ? 100 : -100;
            var scoreFeature = new RankingScore(Grammar.Value);
            DomainLearningLogic learningLogic = new WitnessFunctions(Grammar.Value);

            _prose = new SynthesisEngine(Grammar.Value,
               new SynthesisEngine.Config
               {
                   LogListener = new LogListener(),
                   Strategies = new[] { new DeductiveSynthesis(learningLogic) },
                   UseThreads = false,
                   CacheSize = int.MaxValue
               });

            var learned = _prose.LearnGrammarTopK(spec, scoreFeature, 1);

            var uniqueTransformations = new List<ProgramNode>();
            //filter repetitive transformations 
            foreach (var programNode in learned)
            {
                var exists = false;
                foreach (var uniqueTransformation in uniqueTransformations)
                {
                    if (programNode.ToString().Equals(uniqueTransformation.ToString()))
                    {
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                    uniqueTransformations.Add(programNode);
            }
            uniqueTransformations = uniqueTransformations.Count > numberOfPrograms
                ? uniqueTransformations.GetRange(0, numberOfPrograms)
                : uniqueTransformations;
            return uniqueTransformations.Select(e => new PythonTransformation(e));
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
            var input = State.CreateForExecution(Grammar.Value.InputSymbol, astBefore);
            return input;
        }

        public IEnumerable<string> Apply(Transformation transformation, string program)
        {
            var unparser = new Unparser();
            var result = transformation.GetSynthesizedProgram().Invoke(CreateInputState(program)) as IEnumerable<PythonNode>;
            return result == null ? new List<string>() : result.Select(x => unparser.Unparse(x));
        }
    }
}
