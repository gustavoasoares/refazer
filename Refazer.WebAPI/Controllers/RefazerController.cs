using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.WebSockets;
using Microsoft.ApplicationInsights.WindowsServer;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Utils;
using Microsoft.WindowsAzure.ServiceRuntime;
using Newtonsoft.Json;
using Refazer.WebAPI.Models;
using Tutor;
using Tutor.Transformation;

namespace Refazer.WebAPI.Controllers
{
    /// <summary>
    /// Refazer API for grading submissions in Refazer4Education
    /// </summary>
    [RoutePrefix("api/refazer")]
    public class RefazerController : ApiController
    {
        public static int numberOfJobs = 0; 

        /// <summary>
        /// Creates refazer
        /// </summary>
        /// <returns></returns>
        private Tutor.Refazer BuildRefazer()
        {
            var pathToGrammar = System.Web.Hosting.HostingEnvironment.MapPath(@"~/Content/");
            var pathToDslLib = System.Web.Hosting.HostingEnvironment.MapPath(@"~/bin");
            var refazer = new Refazer4Python(pathToGrammar, pathToDslLib);
            return refazer;
        }

        // POST: api/Refazer/Start
        [Route("Start"), HttpPost]
        public int Start(StartInput startInput)
        {
            var refazerDb = new RefazerDbContext();
            //First, create an experiment for this grading section
            var session = new Session();
            refazerDb.Sessions.Add(session);
            refazerDb.SaveChanges();

            //associate the submissions to this experiment and save 
            startInput.Submissions.ForEach(s => s.SessionId = session.ID);
            foreach (var submission in startInput.Submissions)
            {
                refazerDb.Submissions.Add(submission);
            }
            refazerDb.SaveChanges();
            
            return session.ID;
        }

        //POST: api/Refazer/ApplyFixFromExample
        [Route("ApplyFixFromExample"), HttpPost]
        public dynamic ApplyFixFromExample(ApplyFixFromExampleInput exampleInput)
        {
            
            Trace.TraceWarning("ApplyFixFromExample request accepted for submission: {0} on Instance: {1}. Total jobs in this instance: {2}",
                exampleInput.SubmissionId, "", ++numberOfJobs);
            var exceptions = new List<string>();
            try
            {
                var refazer = BuildRefazer();
                var example = Tuple.Create(exampleInput.CodeBefore, exampleInput.CodeAfter);
                var transformations = refazer.LearnTransformations(new List<Tuple<string, string>>() {example},
                    exampleInput.SynthesizedTransformations, exampleInput.Ranking);

                var refazerDb = new RefazerDbContext();
                var newTransformations = FilterExistingTransformations(transformations,
                    refazerDb.Transformations.Where(x => x.SessionId == exampleInput.SessionId));
                var transformationTuples = new List<Tuple<ProgramNode, Transformation>>();

                var rank = 1;
                foreach (var programNode in newTransformations)
                {
                    var transformation = new Transformation()
                    {
                        SessionId = exampleInput.SessionId,
                        Program = programNode.ToString(),
                        Rank = rank++, 
                        RankType = (exampleInput.Ranking.Equals("specific")) ? 1 : 2,
                        Examples = "[{'submission_id': " + exampleInput.SubmissionId
                    + ", 'code_before': " + exampleInput.CodeBefore
                    + ", 'fixed_code': " + exampleInput.CodeAfter + "}]"
                    };
                    refazerDb.Transformations.Add(transformation);
                    transformationTuples.Add(Tuple.Create(programNode, transformation));
                }
                refazerDb.SaveChanges();
                transformationTuples.ForEach(e => Trace.TraceWarning(string.Format("Transformation created: {0}", e.Item2.ID)));

                var submissions = refazerDb.Submissions.Where(s => s.SessionId == exampleInput.SessionId).ToList();

                var submissionTuples = new List<Tuple<Submission, State>>();
                var exList = new List<Exception>();
                foreach (var submission in submissions)
                {
                    try
                    {
                        var tuple = Tuple.Create(submission, refazer.CreateInputState(submission.Code));
                        submissionTuples.Add(tuple);
                    }
                    catch (Exception e)
                    {
                        exList.Add(e);
                    }
                }
                if (exList.Any())
                    Trace.TraceError("Total of submissions that could not be parsed: {0}", exList.Count);

                if (transformationTuples.Any())
                    Task.Run(() => TryToFixAsync(transformationTuples, exampleInput.SessionId, 
                        exampleInput.QuestionId,submissionTuples));
            }
            catch (Exception e)
            {
                Trace.TraceError("Exception was throw");
                Trace.TraceWarning(e.StackTrace);
                Trace.TraceWarning(e.Message);
                exceptions.Add(e.Message);
            }
            return Json(new {id = 0 , exceptions});
        }

        private IEnumerable<ProgramNode> FilterExistingTransformations(IEnumerable<ProgramNode> newTransformations,
            IQueryable<Transformation> existingTransformations)
        {
            var result = new List<ProgramNode>();
            foreach (var newTransformation in newTransformations)
            {
                var exists = false;
                foreach (var existingTransformation in existingTransformations)
                {
                    if (newTransformations.ToString().Equals(existingTransformation.Program))
                    {
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                    result.Add(newTransformation);
            }
            return result;
        }


        static void TryToFixAsync(IEnumerable<Tuple<ProgramNode, Transformation>> transformationTuples, int experiementId, 
            int questionId, IEnumerable<Tuple<Submission, State>> submissionTuples)
        {
            Trace.TraceWarning(string.Format("Starting TryFix for Session: {0}, Instance: {1}", experiementId, ""));
            foreach (var transformation in transformationTuples)
            {
                Trace.TraceWarning(string.Format("Starting transformation: {0}, Session: {1}, Instance {2}", transformation.Item2.ID, transformation.Item2.SessionId, ""));
                try
                {
                    submissionTuples.AsParallel()
                        .WithDegreeOfParallelism(2)
                        .ForAll(submission => FixSubmission(transformation,
                            experiementId, questionId, submission));
                }
                catch (AggregateException ae)
                {
                    foreach (var ex in ae.InnerExceptions)
                    {
                        Trace.TraceWarning(string.Format("AggregateException"));
                        Trace.TraceWarning(ex.Message);
                    }
                }
                    Trace.TraceWarning(string.Format("Finising transformation: {0}, Session: {1}, Instance {2} ", transformation.Item2.ID, transformation.Item2.SessionId, ""));
            }
            numberOfJobs -= 1;
            Trace.TraceWarning(string.Format("Finishing TryFix for session: {0}, Instance: {1}", experiementId, ""));
        }

        private static void FixSubmission(Tuple<ProgramNode, Transformation> transformationTuple, int experiementId, int questionId,
            Tuple<Submission, State> submission)
        {
            try
            {
                if (!submission.Item1.IsFixed)
                {
                    var manager = new TestManager();
                    var mistake = new Mistake();
                    mistake.before = submission.Item1.Code;
                    var pathToGrammar = System.Web.Hosting.HostingEnvironment.MapPath(@"~/Content/");
                    var pathToDslLib = System.Web.Hosting.HostingEnvironment.MapPath(@"~/bin");
                    var fixer = new SubmissionFixer(pathToGrammar: pathToGrammar, pathToDslLib: pathToDslLib);
                    var unparser = new Unparser();
                    var fixedCode = fixer.TryFix(manager.GetTests(questionId), transformationTuple.Item1, submission.Item2, unparser);
                    if (fixedCode != null)
                    {
                        var refazerDb2 = new RefazerDbContext();
                        submission.Item1.IsFixed = true;
                        var updatedSub = refazerDb2.Submissions.SingleOrDefault(e => e.ID == submission.Item1.ID);
                        if (updatedSub != null)
                            updatedSub.IsFixed = true;
                        var trans = refazerDb2.Transformations.First(x => x.ID == transformationTuple.Item2.ID);
                        var fix = new Fix()
                        {
                            FixedCode = fixedCode,
                            SessionId = experiementId,
                            SubmissionId = submission.Item1.SubmissionId,
                            QuestionId = questionId,
                            Transformation = trans
                        };
                        refazerDb2.Fixes.Add(fix);
                        refazerDb2.SaveChanges();
                        Trace.TraceWarning(string.Format("Submission fixed: {0}, Session: {1}, Transformation: {2}, Time: {3}", 
                            submission.Item1.SubmissionId, transformationTuple.Item2.SessionId, transformationTuple.Item2.ID, DateTime.Now));
                    }
                }
            }
            catch (Exception e)
            {
                Trace.TraceWarning("Exception was thrown when applying fixes.");
                Trace.TraceWarning(e.Message);
            }
        }

        /// <summary>
        /// Get Fixes from the database
        /// </summary>
        /// <param name="SessionId">Id of the current experiment</param>
        /// <param name="FixId">starting index for the fixes</param>
        /// <returns>List of fixes</returns>
        [Route("GetFixes"), HttpGet]
        public IEnumerable<Fix> GetFixes(int SessionId, int FixId)
        {
            Trace.TraceWarning("GetFixes request accepted for session: {0} on Instance: {1}", SessionId, 
                "");
            var refazerDb2 = new RefazerDbContext();
            return refazerDb2.Fixes.Include("Transformation").Where(x => x.SessionId == SessionId && x.ID >= FixId); 
        }
    }

    public class TestManager
    {

        private string GetTestSetup()
        {
            //File.ReadAllText("../../Resources/construct_check.py");
            var testSetup = @"
from operator import add, mul

def square(x):
    return x * x

def identity(x):
    return x

def triple(x):
    return 3 * x

def increment(x):
    return x + 1
";
            return testSetup;
        }
        
        public Dictionary<string, long> GetTests(int questionId)
        {
            var testSetup = GetTestSetup();

            switch (questionId)
            {
                case 1:
                    return new Dictionary<string, long>
                    {
                        {testSetup + "assert(product(3, identity)==6)", 6},
                        {"assert(product(5, identity)==120)", 120},
                        {"assert(product(3, square)==36)", 36},
                        {"assert(product(5, square)==14400)", 14400}
                    };
                case 2:
                    return new Dictionary<string, long>
                    {
                        {testSetup + "assert(repeated(increment, 3)(5)== 8)", 8},
                        {"assert(repeated(triple, 5)(1)==243)", 243},
                        {"assert(repeated(square, 2)(5)==625)", 625},
                        {"assert(repeated(square, 3)(5)==390625)", 390625},
                        {"assert(repeated(square, 0)(5)==5)", 5}
                    };
                case 4:
                    return new Dictionary<string, long>
                    {
                        {testSetup + "assert(g(1)==1)", 15},
                        {"assert(g(2)==2)", 26},
                        {"assert(g(3)==3)", 11},
                        {"assert(g(4)==10)", 25},
                        {"assert(g(5)==22)", 72}
                    };
                case 0:
                    return new Dictionary<string, long>
                    {
                        {testSetup + "assert(accumulate(add, 0, 5, identity)==15)", 15},
                        {"assert(accumulate(add, 11, 5, identity)==26)", 26},
                        {"assert(accumulate(add, 11, 0, identity)==11)", 11},
                        {"assert(accumulate(add, 11, 3, square)==25)", 25},
                        {"assert(accumulate(mul, 2, 3, square)==72)", 72}
                    };
                case -1:
                    return new Dictionary<string, long>
                    {
                       {"assert(x==1)", 255}
                    };
            }
            return null;
        }
    }
}

