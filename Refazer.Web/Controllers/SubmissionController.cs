using Refazer.Web.Models;
using Refazer.Web.Utils;
using Refazer.WebAPI.Models;
using System;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Description;

namespace Refazer.Web.Controllers
{
    [RoutePrefix("api/submissions")]
    public class SubmissionController : ApiController
    {
        private RefazerDbContext db = new RefazerDbContext();

        // POST api/submissions/fix
        [Route("fix"), HttpPost]
        [ResponseType(typeof(List<String>))]
        public IHttpActionResult FixSubmission(Submission2 submission)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            RefazerOnline refazerOnline = RefazerOnline.Instance;

            List<String> generatedCodeList = refazerOnline.
                ApplyTransformationsForSubmission(submission);

            List<String> testCasesList = db.Assignments.Find(
                submission.EndPoint).getTestCasesAsList();

            List<String> fixedCodeList = new List<String>();

            foreach (var code in generatedCodeList)
            {
                if (RunPythonTest.Execute(testCasesList, code))
                {
                    fixedCodeList.Add(code);
                    break;
                }
            }

            return Ok(fixedCodeList);
        }
    }
}