using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Diagnostics;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Learning.Logging;
using Microsoft.ProgramSynthesis.Learning.Strategies;
using Microsoft.ProgramSynthesis.Specifications;
using Tutor;
using Tutor.Transformation;

//from Refazer4Python.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ProgramSynthesis.Compiler;
using Microsoft.ProgramSynthesis.Compound.Extraction.Field;
using Tutor;
using Tutor.Transformation;

namespace Refazer.Core
{
    public class Farbindn
    {
        public Farbindn(string pathToGrammar = @"..\..\..\Tutor\synthesis\", string pathToDslLib = @"..\..\..\Tutor\bin\debug")
        {
            
            _pathToGrammar = pathToGrammar;
            _pathToDslLib = pathToDslLib;
            Grammar = DSLCompiler.ParseGrammarFromFile(pathToGrammar + @"Transformation.grammar",
                    libraryPaths: new[] { pathToDslLib});
        }

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

        /// <summary>
        /// Learn extractions from examples
        /// </summary>
        /// <param name="examples">A list of tuple that contains the code before and after the transformation</param>
        /// <param name="numberOfPrograms">Max number of learned programs</param>
        /// <param name="ranking">Ranking strategic. So far: "general" or "specific"</param>
        /// <returns>List of learned transformations</returns>
        public List<PythonExtraction> LearnExtractions(List<Tuple<PythonNode, PythonNode>> examples, int numberOfPrograms,
            string ranking)
        {
            var spec = CreateExampleSpec(examples);
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

            var learned = _prose.LearnGrammarTopK(spec, scoreFeature, numberOfPrograms);

            var extractions = new List<ProgramNode>();

            foreach (var programNode in learned)
            {
                extractions.Add(programNode);
            }
            extractions = extractions.Count > numberOfPrograms
                ? extractions.GetRange(0, numberOfPrograms)
                : extractions;
            return extractions.Select(e => new PythonExtraction(e)).ToList(); //what is this?
        }

        private ExampleSpec CreateExampleSpec(List<Tuple<PythonNode, PythonNode>> examples)
        {
            var proseExamples = new Dictionary<State, object>();
            foreach (var example in examples)
            {
                var input = CreateInputState(example.Item1);
                //var astAfter = NodeWrapper.Wrap(ASTHelper.ParseContent(example.Item2));
                proseExamples.Add(input, example.Item2);
            }
            var spec = new ExampleSpec(proseExamples);
            return spec;
        }

        public State CreateInputState(PythonNode program)
        {
            //var astBefore = NodeWrapper.Wrap(ASTHelper.ParseContent(program));
            var input = State.Create(Grammar.Value.InputSymbol, program);
            return input;
        }

        public IEnumerable<PythonNode> Apply(Extraction extraction, PythonNode program)
        {
            var unparser = new Unparser();
            var result = extraction.GetSynthesizedProgram().Invoke(CreateInputState(program)) as IEnumerable<PythonNode>;
            return result; //== null ? new List<string>() : result.Select(x => unparser.Unparse(x));
        }
    }
}