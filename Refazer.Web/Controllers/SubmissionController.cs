using Microsoft.CSharp.RuntimeBinder;
using Refazer.Core;
using Refazer.Web.Utils;
using Refazer.WebAPI.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Description;
using Tutor;

namespace Refazer.Web.Controllers
{
    [RoutePrefix("api/submissions")]
    public class SubmissionController : ApiController
    {

        private RefazerDbContext db = new RefazerDbContext();

        // POST api/submissions/fix
        [Route("Fix"), HttpPost]
        [ResponseType(typeof(List<String>))]
        public IHttpActionResult FixSubmission(Submission2 submission)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Core.Refazer refazer = refazer = BuildRefazer();
            List<String> newCodes = new List<String>();
            List<String> fixedCodes = new List<String>();
            List<Core.Transformation> transformationList = new List<Core.Transformation>();

            List<String> testCasesList = db.Assignments.Find(
                submission.EndPoint).getTestCasesAsList();

            IEnumerable<Example> exampleList = db.Examples.Where(e =>
                e.EndPoint.Equals(submission.EndPoint) &&
                e.Question.Equals(submission.Question));

            foreach (var example in exampleList)
            {
                var newTransformationList = refazer.LearnTransformations(
                    ExampleAsTupleList(example));

                foreach (var newTransformation in newTransformationList)
                {
                    if (!transformationList.Contains(newTransformation))
                    {
                        transformationList.Add(newTransformation);
                    }
                }
            }

            foreach (var transformation in transformationList)
            {
                try
                {
                    newCodes.AddRange(refazer.Apply(transformation, submission.Code));
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Exception was thrown when trying to apply transformations");
                    Trace.TraceError(ex.Message);
                }
            }

            foreach (var code in newCodes)
            {
                if (RunPythonTest.Execute(testCasesList, code))
                {
                    fixedCodes.Add(code);
                }
            }

            return Ok(fixedCodes);
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