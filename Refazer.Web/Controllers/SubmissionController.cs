using Microsoft.ProgramSynthesis.AST;
using Refazer.Core;
using Refazer.WebAPI.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Refazer.Web.Controllers
{
    [RoutePrefix("api/submissions")]
    public class SubmissionController : ApiController
    {

        private RefazerDbContext db = new RefazerDbContext();

        // GET api/<controller>
        [Route("Fix"), HttpPost]
        public IEnumerable<string> FixSubmission(Submission2 submission)
        {
            Core.Refazer refazer = BuildRefazer();
            List<string> result = new List<string>();
            List<Core.Transformation> transformationList = new List<Core.Transformation>();

            IEnumerable<Example> exampleList = db.Examples.Where(e =>
                e.EndPoint.Equals(submission.EndPoint) &&
                e.Question.Equals(submission.Question));

            foreach (var example in exampleList)
            {
                var newTransformation = refazer.LearnTransformations(ExampleAsTupleList(example));
                transformationList.AddRange(newTransformation);
            }

            foreach (var transformation in transformationList)
            {
                try
                {
                    var output = refazer.Apply(transformation, submission.IncorrectCode);
                    foreach (var newCode in output)
                    {
                        result.Add(newCode);
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError(string.Format("Exception"));
                    Trace.TraceError(ex.Message);
                }
            }

            return result;
        }

        private List<Tuple<string, string>> ExampleAsTupleList(Example exampleInput)
        {
            String incorrectCode = exampleInput.IncorrectCode;
            String correctCode = exampleInput.CorrectCode;
            var exampleTuple = Tuple.Create(incorrectCode, correctCode);

            return new List<Tuple<string, string>>() { exampleTuple };
        }

        private Core.Refazer BuildRefazer()
        {
            var pathToGrammar = System.Web.Hosting.HostingEnvironment.MapPath(@"~/Content/");
            var pathToDslLib = System.Web.Hosting.HostingEnvironment.MapPath(@"~/bin");
            var refazer = new Refazer4Python(pathToGrammar, pathToDslLib);
            return refazer;
        }
    }
}