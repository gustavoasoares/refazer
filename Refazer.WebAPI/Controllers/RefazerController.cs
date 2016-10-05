using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Utils;
using Newtonsoft.Json;
using Refazer.WebAPI.Models;
using Tutor;

namespace Refazer.WebAPI.Controllers
{
    /// <summary>
    /// Refazer API for grading submissions in Refazer4Education
    /// </summary>
    public class RefazerController : ApiController
    {
        //Entity framework context for accessing DB
        private SubmissionDBContext subDb = new SubmissionDBContext();
        private SessionDbContext sessionDb = new SessionDbContext();
        private FixDbContext fixDb = new FixDbContext();
        private TransformationDBContext transformationDb = new TransformationDBContext();

        // POST: api/Refazer
        public dynamic Post([FromBody]RefazerInput  input)
        {
            var exceptions = new List<string>();

            //try
            //{
            //    if (input.Examples.Count() == 0)
            //        throw new ArgumentException("Examples cannot be empty");

            //    var fixer = new SubmissionFixer(System.Web.Hosting.HostingEnvironment.MapPath(@"~/Content/"), System.Web.Hosting.HostingEnvironment.MapPath(@"~/bin"));
            //    var transformation = fixer.CreateTransformation(input.Examples.First());
            //    fixer._classification = transformation;
            //    foreach (var submission in input.submissions)
            //    {
            //        try
            //        {
            //            var mistake = new Mistake();
            //            mistake.before = submission["before"] as string;
            //            var isFixed = fixer.Fix(mistake, GetTests(), false);
            //            submission.Add("fixes_worked", isFixed);
            //            if (isFixed)
            //                submission.Add("fixed_code", mistake.SynthesizedAfter);
            //        }
            //        catch (Exception e)
            //        {
            //            submission.Add("fixes_worked", false);
            //            submission.Add("exception", e.Message);
            //            exceptions.Add(e.Message);
            //        }
            //    }
            //}
            //catch (Exception e)
            //{
            //    exceptions.Add(e.Message);
            //}
            return Json(new {input.submissions, exceptions});
        }

        // POST: api/Refazer/Start
        [System.Web.Http.Route("Start"), System.Web.Http.HttpPost]
        public int Start(StartInput startInput)
        {
            //First, create an experiment for this grading section
            var session = new Session();
            sessionDb.Sessions.Add(session);
            sessionDb.SaveChanges();

            //associate the submissions to this experiment and save 
            startInput.Submissions.ForEach(s => s.SessionId = session.ID);
            foreach (var submission in startInput.Submissions)
            {
                subDb.Submissions.Add(submission);
            }
            subDb.SaveChanges();
            
            return session.ID;
        }

        //POST: api/Refazer/ApplyFixFromExample
        [Route("ApplyFixFromExample"), System.Web.Http.HttpPost]
        public dynamic ApplyFixFromExample(ApplyFixFromExampleInput exampleInput)
        {
            var exceptions = new List<string>();
            int transformationId = 0;
            try
            {
                var fixer = new SubmissionFixer(System.Web.Hosting.HostingEnvironment.MapPath(@"~/Content/"),
                System.Web.Hosting.HostingEnvironment.MapPath(@"~/bin"));
                var t = fixer.CreateTransformation(exampleInput.CodeBefore, exampleInput.CodeAfter);
                fixer._classification = t;

                var transformation = new Transformation()
                {
                    Program = t.First().Item2.ToString(),
                    Examples = "[{'submission_id': " + exampleInput.QuestionId 
                    +", 'code_before': "+ exampleInput.CodeBefore 
                    + ", 'fixed_code': " + exampleInput.CodeAfter + "}]"
                };
                transformationDb.Transformations.Add(transformation);
                transformationDb.SaveChanges();
                transformationId = transformation.ID;
                TryToFixAsync(fixer, exampleInput.SessionId, exampleInput.QuestionId, transformation);
            }
            catch(Exception e)
            {
                exceptions.Add(e.Message);
            }
            return Json(new {transformationId , exceptions});
        }

        async Task<int> TryToFixAsync(SubmissionFixer fixer, int experiementId, int questionId, Transformation transformation)
        {
            var submissions = subDb.Submissions.Where(s => s.SessionId == experiementId);
            var manager = new TestManager();
            foreach (var submission in submissions)
            {
                try
                {
                    var mistake = new Mistake();
                    mistake.before = submission.Code;
                    var isFixed = fixer.Fix(mistake, manager.GetTests(questionId), false);
                    if (isFixed)
                    {
                        var fix = new Fix()
                        {
                            FixedCode = mistake.SynthesizedAfter,
                            SessionId = experiementId,
                            SubmissionId = submission.ID,
                            Transformation = transformation
                        };
                        fixDb.Fixes.Add(fix);
                        fixDb.SaveChanges();
                    }
                }
                catch (Exception e)
                {
                    //TODO: Log this exceptions somewhere
                }
            }
            return 0;
        }

        /// <summary>
        /// Get Fixes from the database
        /// </summary>
        /// <param name="experiement_id">Id of the current experiment</param>
        /// <param name="index">starting index for the fixes</param>
        /// <returns>List of fixes</returns>
        [System.Web.Http.Route("GetFixes"), System.Web.Http.HttpGet]
        public IEnumerable<Fix> GetFixes(int experiement_id, int index)
        {
            return fixDb.Fixes.Where(x => x.SessionId == experiement_id && x.ID >= index); 
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
                case 3:
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
                case 1:
                    return new Dictionary<string, long>
                    {
                        {testSetup + "assert(accumulate(add, 0, 5, identity)==15)", 15},
                        {"assert(accumulate(add, 11, 5, identity)==26)", 26},
                        {"assert(accumulate(add, 11, 0, identity)==11)", 11},
                        {"assert(accumulate(add, 11, 3, square)==25)", 25},
                        {"assert(accumulate(mul, 2, 3, square)==72)", 72}
                    };
                case 0:
                    return new Dictionary<string, long>
                    {
                       {"assert(x==1)", 255}
                    };
            }
            return null;
        }
    }
}

