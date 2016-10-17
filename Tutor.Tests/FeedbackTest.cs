using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Tutor.feedback;

namespace Tutor.Tests
{
    /// <summary>
    /// Summary description for FeedbackTest
    /// </summary>
    [TestClass]
    public class FeedbackTest
    {
        public FeedbackTest()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void TestBottomOutHintUpdate()
        {
            var before = "x = 0";
            var after = @"x = 1";

            var feedbackGen = new BottomOutHintGen();
            var feedbackList = feedbackGen.Generate(before, after);
            Assert.AreEqual(1, feedbackList.Count());
            Assert.AreEqual("Update 0 to 1", feedbackList.First());
        }

        [TestMethod]
        public void TestBottomOutHintInsert()
        {
            var before = "x = 0";
            var after = "x = 0\r\nx = 1";

            var feedbackGen = new BottomOutHintGen();
            var feedbackList = feedbackGen.Generate(before, after);
            Assert.AreEqual(1, feedbackList.Count());
            Assert.AreEqual("Insert x = 1", feedbackList.First());
        }

        [TestMethod]
        public void AddTestsToJSon()
        {
            var feedbackGen = new BottomOutHintGen();

            var dataPath = @"C:\Users\Gustavo\git\HerokuUI\data\accumulate_all_attempts.json";
            var input = JsonConvert.DeserializeObject<IEnumerable<Dictionary<string,dynamic>>>(File.ReadAllText(dataPath));
            
            foreach (var submission in input)
            {
                var before = submission["before"];
                var after = submission["SynthesizedAfter"];
                try
                {
                    submission.Add("fix_hints", feedbackGen.Generate(before, after));
                }
                catch (Exception e)
                {
                    Console.Out.WriteLine(e.StackTrace);
                }
            }
            var submissionsToJson = JsonConvert.SerializeObject(input);
            File.WriteAllText(dataPath, submissionsToJson);
        }
    }
}
