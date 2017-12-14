using System;
using System.Collections.Generic;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;

namespace Refazer.Core
{
    /// <summary>
    /// Refazer API learns program transformations from examples
    /// </summary>
    public interface Refazer
    {
        /// <summary>
        /// Learn transformations from examples
        /// </summary>
        /// <param name="examples">A list of tuple that contains the code before and after the transformation</param>
        /// <param name="numberOfPrograms">Max number of learned programs</param>
        /// <param name="ranking">Ranking strategic. So far: "general" or "specific"</param>
        /// <returns>List of learned transformations</returns>
        IEnumerable<Transformation> LearnTransformations(List<Tuple<string, string>> examples, int numberOfPrograms,
            string ranking);

        /// <summary>
        /// Create an input state that contains the input program to be transformed. 
        /// </summary>
        /// <param name="program">input code</param>
        /// <returns></returns>
        State CreateInputState(string program);

        /// <summary>
        /// Applies a transformation to a given input
        /// </summary>
        /// <param name="transformation"></param>
        /// <param name="program"></param>
        /// <returns>List of output programs</returns>
        IEnumerable<string> Apply(Transformation transformation, string program);
    }
}
