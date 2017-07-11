using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Specifications;
using System;
using System.Collections.Generic;
using Tutor;

namespace Refazer.Core
{
    public class Farbindn
    {
        public Farbindn()
        {
        }

        /// <summary>
        /// Learn extractions from examples
        /// </summary>
        /// <param name="examples">A list of tuple that contains the code before and after the transformation</param>
        /// <param name="numberOfPrograms">Max number of learned programs</param>
        /// <param name="ranking">Ranking strategic. So far: "general" or "specific"</param>
        /// <returns>List of learned transformations</returns>
        IEnumerable<Extraction> LearnExtractions(List<Tuple<string, int>> examples, int numberOfPrograms,
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

        private ExampleSpec CreateExampleSpec(List<Tuple<string, int>> examples)
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

        public IEnumerable<string> Apply(Transformation transformation, string program)
        {
            var unparser = new Unparser();
            var result = transformation.GetSynthesizedProgram().Invoke(CreateInputState(program)) as IEnumerable<PythonNode>;
            return result == null ? new List<string>() : result.Select(x => unparser.Unparse(x));
        }
    }
}