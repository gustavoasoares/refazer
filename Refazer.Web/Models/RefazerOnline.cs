using System.Collections.Generic;
using Refazer.Core;
using System;
using System.Diagnostics;
using System.Linq;
using Refazer.Web.Utils;

namespace Refazer.Web.Models
{
    public sealed class RefazerOnline
    {
        private static Core.Refazer refazer;

        private static object syncRoot = new Object();

        private static volatile RefazerOnline instance;

        private Dictionary<String, List<Core.Transformation>> transformationStorage;

        private RefazerOnline()
        {
            refazer = BuildRefazer();
            transformationStorage = new Dictionary<String, List<Core.Transformation>>();
        }

        public static RefazerOnline Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new RefazerOnline();
                        }
                    }
                }

                return instance;
            }
        }

        private static Core.Refazer BuildRefazer()
        {
            var pathToGrammar = System.Web.Hosting.HostingEnvironment.MapPath(@"~/Content/");
            var pathToDslLib = System.Web.Hosting.HostingEnvironment.MapPath(@"~/bin");
            var refazer = new Refazer4Python(pathToGrammar, pathToDslLib);
            return refazer;
        }

        public void LearnTransformationsFromExample(Example example)
        {
            var exampleAsTuple = Tuple.Create(example.IncorrectCode,
                example.CorrectCode);

            var newTransformationsList = refazer.LearnTransformations(
                new List<Tuple<string, string>>() { exampleAsTuple });

            SaveTransformation(example.KeyPoint(), newTransformationsList.ToList());
        }

        public IEnumerable<Core.Transformation> LearnTransformationsFromExample(List<Example> examplesList)
        {
            var examplesAsTuple = ExampleAsTupleList(examplesList);

            var newTransformationsList = refazer.LearnTransformations(examplesAsTuple);

            string keypoint = examplesList[0].KeyPoint();

            SaveTransformation(keypoint, newTransformationsList.ToList());

            return newTransformationsList;
        }

        public List<Cluster> LearnClusteredTransformations(String keyPoint, List<Example> exampleList)
        {
            List<Cluster> clustersList = new List<Cluster>();

            while (!exampleList.IsEmpty())
            {
                List<Core.Transformation> transformationList = null;

                List<Example> combinedExamples = new List<Example>();

                foreach (var example in exampleList)
                {
                    combinedExamples.Add(example);
                    var examplesAsTuples = ExampleAsTupleList(combinedExamples);
                    IEnumerable<Core.Transformation> newTransformations = null;

                    try
                    {
                        newTransformations = refazer.LearnTransformations(examplesAsTuples);
                    }
                    catch (Exception e)
                    {
                        newTransformations = new List<Core.Transformation>();
                        Trace.TraceError("Could not learn transformations from the example "
                            + example.Id + " because " + e.Message);
                    }

                    if (newTransformations.IsEmpty())
                    {
                        combinedExamples.Remove(example);
                    }
                    else
                    {
                        transformationList = newTransformations.ToList();
                    }
                }

                if (combinedExamples.IsEmpty())
                {
                    break;
                }

                Cluster newCluster = new Cluster();
                newCluster.KeyPoint = keyPoint;

                foreach (var example in combinedExamples)
                {
                    exampleList.Remove(example);
                    newCluster.AddExampleReference(example.Id);
                }

                clustersList.Add(newCluster);

                if (transformationList != null)
                {
                    SaveTransformation(keyPoint, transformationList);
                }
            }

            return clustersList;
        }

        public List<String> TryToFixSubmission(Submission2 submission, List<String> testCasesList)
        {
            return TryToFixSubmission(submission, testCasesList, SearchTransformation(submission));
        }

        public List<String> TryToFixSubmission(Submission2 submission, List<String> testCasesList,
             List<Core.Transformation> transformationsList)
        {
            List<String> fixedCodesList = new List<String>();

            for (int i = 0; i < transformationsList.Count; i++)
            {
                try
                {
                    var transformation = transformationsList[i];
                    var generatedCodesList = refazer.Apply(transformation, submission.Code);

                    foreach (var code in generatedCodesList)
                    {
                        if (RunPythonTest.Execute(testCasesList, code))
                        {
                            fixedCodesList.Add(code);
                            break;
                        }
                    }

                    if (!fixedCodesList.IsEmpty())
                    {
                        break;
                    }
                }
                catch (Exception e)
                {
                    Trace.TraceError("Could not apply transformations because " + e.Message);
                }
            }

            return fixedCodesList;
        }

        private void SaveTransformation(String keyPoint, List<Core.Transformation> newTransformations)
        {

            if (!transformationStorage.ContainsKey(keyPoint))
            {
                transformationStorage.Add(keyPoint, newTransformations.ToList());
            }
            else
            {
                List<Core.Transformation> existingTransformation = transformationStorage[keyPoint];

                foreach (var transformation in newTransformations)
                {
                    if (!existingTransformation.Contains(transformation))
                    {
                        existingTransformation.Add(transformation);
                    }
                }

                transformationStorage[keyPoint] = existingTransformation;
            }
        }

        private List<Core.Transformation> SearchTransformation(Submission2 submission)
        {
            if (!transformationStorage.ContainsKey(submission.KeyPoint()))
            {
                return new List<Core.Transformation>();
            }

            return transformationStorage[submission.KeyPoint()];
        }

        private List<Tuple<String, String>> ExampleAsTupleList(List<Example> exampleList)
        {
            List<Tuple<String, String>> result = new List<Tuple<String, String>>();

            foreach (var example in exampleList)
            {
                result.Add(Tuple.Create(example.IncorrectCode, example.CorrectCode));
            }

            return result;
        }

        public bool IsAvailable()
        {
            return !transformationStorage.IsEmpty();
        }

        public bool IsAvailableFor(String keyPoint)
        {
            return transformationStorage.ContainsKey(keyPoint);
        }
    }
}