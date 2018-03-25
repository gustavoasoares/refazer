using Refazer.Web.Models;
using Refazer.Web.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web.Http;
using System.Web.Http.Description;

namespace Refazer.Web.Controllers
{
    [RoutePrefix("api/submissions")]
    public class SubmissionController : ApiController
    {
        private RefazerDbContext db = new RefazerDbContext();

        // POST api/submissions/fix
        [Route(""), HttpPost]
        [ResponseType(typeof(Attempt))]
        public IHttpActionResult FixSubmission(Submission2 submission)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Attempt attempt = new Attempt();
            attempt.EndPoint = submission.EndPoint;
            attempt.SubmittedCode = submission.Code;

            List<String> testCasesList = db.Assignments.Find(
                submission.EndPoint).getTestCasesAsList();

            Tuple<bool, List<String>> testResult = RunPythonTest.
                Execute(testCasesList, submission.Code);

            attempt.LogsList = testResult.Item2;
            attempt.PassedTests = testResult.Item1;

            if (attempt.PassedTests)
            {
                return Ok(attempt);
            }

            RefazerOnline refazerOnline = RefazerOnline.Instance;

            Fix2 refazerFixes = new Fix2();

            if (!refazerOnline.IsAvailableFor(submission.KeyPoint()))
            {

                refazerFixes = WakeUpRefazerOnDemand(
                    refazerOnline, submission, testCasesList);

                attempt.Cluster = refazerFixes.Cluster;
                attempt.FixedCodeList = refazerFixes.FixedCodeList;

                return Ok(attempt);
            }

            refazerFixes = refazerOnline.TryToFixSubmission(
                submission, testCasesList);

            attempt.Cluster = refazerFixes.Cluster;
            attempt.FixedCodeList = refazerFixes.FixedCodeList;

            return Ok(attempt);
        }

        private Fix2 WakeUpRefazerOnDemand(RefazerOnline refazerOnline, Submission2 submission,
            List<String> testCasesList)
        {

            Fix2 refazerFixes = new Fix2();

            String keyPoint = submission.KeyPoint();

            List<Cluster> clustersList = db.Clusters.Where(
                c => c.KeyPoint.Equals(keyPoint)).ToList();

            clustersList.Sort();

            for (int i = 0; i < clustersList.Count; i++)
            {
                var cluster = clustersList[i];

                List<int> examplesIds = cluster.GetExamplesReferenceList();

                List<Example> examplesByCluster = db.Examples.Where(
                    e => examplesIds.Contains(e.Id)).ToList();

                TransformationSet transformationSet = refazerOnline.
                    LearnTransformationsFromExample(cluster, examplesByCluster);

                refazerFixes = refazerOnline.TryToFixSubmission(
                    submission, testCasesList, transformationSet);

                if (!refazerFixes.FixedCodeList.IsEmpty())
                {
                    int index = i + 1;
                    int count = clustersList.Count - index;
                    var restClustersList = clustersList.GetRange(index, count);

                    Thread thread = new Thread(() => KeepLearningTransformations(
                        refazerOnline, restClustersList));

                    thread.Start();

                    return refazerFixes;
                }
            }
            return refazerFixes;
        }

        private void KeepLearningTransformations(RefazerOnline refazerOnline, List<Cluster> clustersList)
        {
            foreach (var cluster in clustersList)
            {
                List<int> examplesIds = cluster.GetExamplesReferenceList();

                List<Example> examplesByCluster = db.Examples.Where(
                    e => examplesIds.Contains(e.Id)).ToList();

                refazerOnline.LearnTransformationsFromExample(cluster, examplesByCluster);
            }
        }
    }
}