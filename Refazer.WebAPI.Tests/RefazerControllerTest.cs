using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Refazer.WebAPI.Controllers;
using Refazer.WebAPI.Models;

namespace Refazer.WebAPI.Tests
{
    [TestClass]
    public class RefazerControllerTest
    {
        private static TraceSource Source = new TraceSource("tests");

        [TestMethod]
        public void TestPost()
        {
            var input = JsonConvert.DeserializeObject<RefazerInput>(File.ReadAllText("../../data/testData.json"));
            var controller = new RefazerController();

            var countExamples = 0;
            try
            {
                for (int submissionCount = 0; submissionCount < input.submissions.Count(); submissionCount++)
                {
                    var submission = input.submissions[submissionCount];
                    var isFixed = (bool)submission["is_fixed"];
                    if (!isFixed)
                    {
                        countExamples++;
                        var before = submission["before"] as string;
                        var after = submission["after"] as string;
                        var example = new Dictionary<string,string>();
                        example.Add("before", before);
                        example.Add("after", after);
                        input.Examples = new[] { example };
                        input = controller.Post(input);
                        submission = input.submissions[submissionCount];
                        if (!(bool) submission["is_fixed"])
                        {
                            Source.TraceEvent(TraceEventType.Error, 0, "program was not fixed. Index: " + submissionCount);
                        }
                    }
                }
            }
            catch (NotImplementedException e)
            {
                Source.TraceEvent(TraceEventType.Error, 0, "Exception: " + e.Message);
            }
            Assert.IsTrue(countExamples < 71);
        }

        [TestMethod]
        public void TestSart()
        {
            var startInput = CreateStartInput(); 
            var controller = new RefazerController();
            var id = controller.Start(startInput);

            var db = new SubmissionDBContext();
            var submissions = db.Submissions.Where(s => s.SessionId == id);
            Assert.AreEqual(2, submissions.Count());
        }

        [TestMethod]
        public void TestLearnFix()
        {
            var controller = new RefazerController();

            //Start section
            var startInput = CreateStartInput();
            var id = controller.Start(startInput);

            //create an example input 
            var exampleInput = CreateExampleInput(id);

            //call the controller 
            controller.ApplyFixFromExample(exampleInput);

            var fixes = controller.GetFixes(id, 0);
            Assert.AreEqual(1, fixes.Count());
        }

        private ApplyFixFromExampleInput CreateExampleInput(int experiemntId)
        {
            return new ApplyFixFromExampleInput()
            {
                CodeBefore = "x = 0",
                CodeAfter = "x = 1",
                SessionId = experiemntId,
                QuestionId = 0
            };
        }

        private StartInput CreateStartInput()
        {
            var sub1 = new Submission() {Code = "x = 0", ID = 1, QuestionId = 0};
            var sub2 = new Submission() { Code = "y = 'oi'", ID = 1, QuestionId = 0};
            return new StartInput() {QuestionId = 0, Submissions = new List<Submission>() {sub1,sub2} };
        }

    }
}
