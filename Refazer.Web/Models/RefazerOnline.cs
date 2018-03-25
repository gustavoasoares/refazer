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

        private Dictionary<String, List<TransformationSet>> transformationStorage;

        private RefazerOnline()
        {
            refazer = BuildRefazer();
            transformationStorage = new Dictionary<String, List<TransformationSet>>();
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

            TransformationSet transformationSet = new TransformationSet();
            transformationSet.TransformationList = newTransformationsList.ToList();

            SaveTransformation(example.KeyPoint(), transformationSet);
        }

        public TransformationSet LearnTransformationsFromExample(Cluster cluster, List<Example> examplesList)
        {
            var examplesAsTuple = ExampleAsTupleList(examplesList);

            var newTransformationsList = refazer.LearnTransformations(examplesAsTuple);

            string keypoint = examplesList[0].KeyPoint();

            TransformationSet transformationSet = new TransformationSet();
            transformationSet.Cluster = cluster;
            transformationSet.TransformationList = newTransformationsList.ToList();

            SaveTransformation(keypoint, transformationSet);

            return transformationSet;
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

                /**
                if (transformationList != null)
                {
                    SaveTransformation(keyPoint, transformationList);
                }
                */
            }

            return clustersList;
        }

        public Fix2 TryToFixSubmission(Submission2 submission, List<String> testCasesList)
        {
            return TryToFixSubmission(submission, testCasesList, SearchTransformation(submission));
        }

        public Fix2 TryToFixSubmission(Submission2 submission, List<String> testCasesList,
            TransformationSet transformationSet)
        {
            List<TransformationSet> transformationSetList = new List<TransformationSet>();

            transformationSetList.Add(transformationSet);

            return TryToFixSubmission(submission, testCasesList, transformationSetList);
        }

        public Fix2 TryToFixSubmission(Submission2 submission, List<String> testCasesList,
             List<TransformationSet> transformationSetList)
        {
            Cluster cluster = new Cluster();

            List<String> result = new List<String>();

            foreach (var transformationSet in transformationSetList)
            {
                cluster = transformationSet.Cluster;

                result = ApplyCodeTransformation(submission, testCasesList,
                                transformationSet.TransformationList);

                if (!result.IsEmpty())
                {
                    break;
                }
            }

            return new Fix2(cluster, result);
        }

        public List<String> ApplyCodeTransformation(Submission2 submission, List<String> testCasesList,
            List<Core.Transformation> transformationsList)
        {
            List<String> fixedCodeList = new List<String>();

            for (int i = 0; i < transformationsList.Count; i++)
            {
                try
                {
                    var transformation = transformationsList[i];
                    var generatedCodesList = refazer.Apply(transformation, submission.Code).ToList();

                    if (generatedCodesList.Count > 15)
                    {
                        generatedCodesList = generatedCodesList.GetRange(0, 20);
                    }

                    foreach (var code in generatedCodesList)
                    {
                        Tuple<bool, List<String>> testResult = RunPythonTest.
                            Execute(testCasesList, code);

                        if (testResult.Item1)
                        {
                            fixedCodeList.Add(code);
                            break;
                        }
                    }

                    if (!fixedCodeList.IsEmpty())
                    {
                        break;
                    }
                }
                catch (Exception e)
                {
                    Trace.TraceError("Could not apply transformations because " + e.Message);
                }
            }

            return fixedCodeList;
        }

        private void SaveTransformation(String keyPoint, TransformationSet newTransformationSet)
        {
            if (!transformationStorage.ContainsKey(keyPoint))
            {
                List<TransformationSet> transformationSetList = new List<TransformationSet>();

                transformationSetList.Add(newTransformationSet);

                transformationStorage.Add(keyPoint, transformationSetList);
            }
            else
            {
                List<TransformationSet> transformationSetList = transformationStorage[keyPoint];

                transformationSetList.Add(newTransformationSet);

                transformationStorage[keyPoint] = transformationSetList;
            }
        }

        private List<TransformationSet> SearchTransformation(Submission2 submission)
        {
            if (!transformationStorage.ContainsKey(submission.KeyPoint()))
            {
                return new List<TransformationSet>();
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