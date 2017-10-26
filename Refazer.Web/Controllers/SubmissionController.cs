using Refazer.Web.Models;
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
        [Route("fix"), HttpPost]
        [ResponseType(typeof(List<String>))]
        public IHttpActionResult FixSubmission(Submission2 submission)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            List<String> testCasesList = db.Assignments.Find(
                submission.EndPoint).getTestCasesAsList();

            RefazerOnline refazerOnline = RefazerOnline.Instance;

            if (!refazerOnline.IsAvailableFor(submission.KeyPoint()))
            {
                return Ok(WakeUpRefazerOnDemand(refazerOnline, submission, testCasesList));
            }

            List<String> fixedCodesList = refazerOnline.
                TryToFixSubmission(submission, testCasesList);

            return Ok(fixedCodesList);
        }

        private List<String> WakeUpRefazerOnDemand(RefazerOnline refazerOnline, Submission2 submission,
            List<String> testCasesList)
        {
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

                var transformationsList = refazerOnline.
                    LearnTransformationsFromExample(examplesByCluster);

                List<String> fixedCodesList = refazerOnline.TryToFixSubmission(
                    submission, testCasesList, transformationsList.ToList());

                if (!fixedCodesList.IsEmpty())
                {
                    int index = i + 1;
                    int count = clustersList.Count - index;
                    var restClustersList = clustersList.GetRange(index, count);

                    Thread thread = new Thread(() => KeepLearningTransformations(
                        refazerOnline, restClustersList));

                    thread.Start();

                    return fixedCodesList;
                }
            }
            return new List<String>();
        }

        private void KeepLearningTransformations(RefazerOnline refazerOnline, List<Cluster> clustersList)
        {
            foreach (var cluster in clustersList)
            {
                List<int> examplesIds = cluster.GetExamplesReferenceList();

                List<Example> examplesByCluster = db.Examples.Where(
                    e => examplesIds.Contains(e.Id)).ToList();

                refazerOnline.LearnTransformationsFromExample(examplesByCluster);
            }
        }
    }
}