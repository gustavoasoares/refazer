using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json;
using Refazer.WebAPI.Models;
using Tutor;

namespace Refazer.WebAPI.Controllers
{
    public class RefazerController : ApiController
    {
        public RefazerController()
        {
        }

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

        private Dictionary<string, long> GetTests()
        {
            var testSetup = GetTestSetup();

            return new Dictionary<string, long>
                    {
                        {testSetup + "assert(accumulate(add, 0, 5, identity)==15)", 15},
                        {"assert(accumulate(add, 11, 5, identity)==26)", 26},
                        {"assert(accumulate(add, 11, 0, identity)==11)", 11},
                        {"assert(accumulate(add, 11, 3, square)==25)", 25},
                        {"assert(accumulate(mul, 2, 3, square)==72)", 72}
                    };
        }

        // GET: api/Refazer
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET: api/Refazer/5
        public string Get(int id)
        {
            return "value";
        }

        // POST: api/Refazer
        public dynamic Post([FromBody]RefazerInput  input)
        {
            if (input.Examples.Count() == 0)
                throw new ArgumentException("Examples cannot be empty");

            var fixer = new SubmissionFixer(System.Web.Hosting.HostingEnvironment.MapPath(@"~/Content/"), System.Web.Hosting.HostingEnvironment.MapPath(@"~/bin"));
            var transformation = fixer.CreateTransformation(input.Examples.First());
            fixer._classification = transformation;
            foreach (var submission in input.submissions)
            {
                var isFixed = (bool)submission["isFixed"];
                if (!isFixed)
                {
                    var mistake = new Mistake();
                    mistake.before = submission["before"] as string;
                    mistake.after = submission["after"] as string;
                    var tests = new Dictionary<string, long>();
                    isFixed = fixer.Fix(mistake, GetTests(), false);
                    submission["isFixed"] = isFixed;
                }
            }
            //Parallel.ForEach(input.submissions, (submission) =>
            //{
            //    var fixer = new SubmissionFixer(transformation);
            //    var isFixed = (bool)submission["is_fixed"];
            //    if (!isFixed)
            //    {
            //        var mistake = new Mistake();
            //        mistake.before = submission["before"] as string;
            //        mistake.after = submission["after"] as string;
            //        var tests = new Dictionary<string, long>();
            //        isFixed = fixer.Fix(mistake, GetTests(), false);
            //        submission["is_fixed"] = isFixed;
            //    }
            //});
            //try to fix the incorrect submissions
            //return new submissions
            return input;
        }

        // PUT: api/Refazer/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE: api/Refazer/5
        public void Delete(int id)
        {
        }
    }
}
