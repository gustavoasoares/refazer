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
    }
}
