using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.WebSockets;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Utils;
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
        //Entity framework context for accessing DB
        private static RefazerDbContext refazerDb = new RefazerDbContext();

        private static Tutor.Refazer refazer = BuildRefazer();
        private static object syncRoot = new Object();

        /// <summary>
        /// Creates a single refazer object for all requests
        /// </summary>
        /// <returns></returns>
        private static Tutor.Refazer BuildRefazer()
        {
            if (refazer == null)
            {
                    var pathToGrammar = System.Web.Hosting.HostingEnvironment.MapPath(@"~/Content/");
                    var pathToDslLib = System.Web.Hosting.HostingEnvironment.MapPath(@"~/bin");
                    refazer = new Refazer4Python(pathToGrammar,pathToDslLib);
            }
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
            var exceptions = new List<string>();
            int transformationId = 0;
            try
            {
                var example = Tuple.Create(exampleInput.CodeBefore, exampleInput.CodeAfter);
                var transformations = refazer.LearnTransformations(new List<Tuple<string, string>>() {example},
                    exampleInput.SynthesizedTransformations, exampleInput.Ranking);

                var refazerDb = new RefazerDbContext();
                var transformationTuples = new List<Tuple<ProgramNode, Transformation>>();
                foreach (var programNode in transformations)
                {
                    var transformation = new Transformation()
                    {
                        Program = programNode.ToString(),
                        Examples = "[{'submission_id': " + exampleInput.SubmissionId
                    + ", 'code_before': " + exampleInput.CodeBefore
                    + ", 'fixed_code': " + exampleInput.CodeAfter + "}]"
                    };
                    refazerDb.Transformations.Add(transformation);
                    transformationTuples.Add(Tuple.Create(programNode, transformation));
                }
                refazerDb.SaveChanges();

                var submissions = refazerDb.Submissions.Where(s => s.SessionId == exampleInput.SessionId).ToList();
                var submissionTuples = submissions.Select(x => Tuple.Create(x, refazer.CreateInputState(x.Code)));
                Task.Run(() => TryToFixAsync(transformationTuples, exampleInput.SessionId, 
                    exampleInput.QuestionId,submissionTuples));
            }
            catch (Exception e)
            {
                exceptions.Add(e.Message);
            }
            return Json(new {transformationId , exceptions});
        }

        

        static void TryToFixAsync(IEnumerable<Tuple<ProgramNode, Transformation>> transformationTuples, int experiementId, 
            int questionId, IEnumerable<Tuple<Submission, State>> submissionTuples)
        {
            foreach (var transformation in transformationTuples)
            {
                submissionTuples.AsParallel().WithDegreeOfParallelism(2).ForAll(submission => FixSubmission(transformation,
                    experiementId, questionId, submission));
            }
        }

        private static void FixSubmission(Tuple<ProgramNode, Transformation> transformationTuple, int experiementId, int questionId,
            Tuple<Submission, State> submission)
        {
            try
            {
                var manager = new TestManager();
                var mistake = new Mistake();
                mistake.before = submission.Item1.Code;
                var fixer = new SubmissionFixer();
                var unparser = new Unparser();
                var fixedCode = fixer.TryFix(manager.GetTests(questionId), transformationTuple.Item1, submission.Item2, unparser);
                if (fixedCode != null)
                {
                    var fix = new Fix()
                    {
                        FixedCode = fixedCode,
                        SessionId = experiementId,
                        SubmissionId = submission.Item1.SubmissionId,
                        QuestionId = questionId,
                        Transformation = transformationTuple.Item2
                    };
                    var refazerDb2 = new RefazerDbContext();
                    refazerDb2.Fixes.Add(fix);
                    refazerDb2.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Console.Out.WriteLine(e);
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
            Console.Out.WriteLine("Got here!!!!!!!!!!!!!!!!!!!!!!!!!!");
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
                case 4:
                    return new Dictionary<string, long>
                    {
                        {testSetup + "assert(repeated(increment, 3)(5)== 8)", 8},
                        {"assert(repeated(triple, 5)(1)==243)", 243},
                        {"assert(repeated(square, 2)(5)==625)", 625},
                        {"assert(repeated(square, 3)(5)==390625)", 390625},
                        {"assert(repeated(square, 0)(5)==5)", 5}
                    };
                case 2:
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

