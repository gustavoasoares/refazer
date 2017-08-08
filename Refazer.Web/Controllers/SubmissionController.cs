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

            IEnumerable<Example> exampleList = db.Examples.Where(e =>
                e.EndPoint.Equals(submission.EndPoint) &&
                e.Question.Equals(submission.Question));

            List<Tuple<string, string>> examplesAsTuples = ExamplesAsTupleList(exampleList);

            foreach (var transformation in refazer.LearnTransformations(examplesAsTuples))
            {
                var output = refazer.Apply(transformation, submission.IncorrectCode);

                foreach (var newCode in output)
                {
                    result.Add(newCode);
                }
            }

            return result;
        }

        private List<Tuple<string, string>> ExamplesAsTupleList(IEnumerable<Example> exampleList)
        {
            List<Tuple<string, string>> tuplesList = new List<Tuple<string, string>>();

            foreach (var example in exampleList)
            {
                tuplesList.Add(Tuple.Create(example.IncorrectCode, example.CorrectCode));
            }

            return tuplesList;
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