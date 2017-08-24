using System.Collections.Generic;
using Refazer.Core;
using System;
using System.Diagnostics;
using System.Linq;

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

        public void LearnClusteredTransformations(String keyPoint, List<Example> exampleList)
        {
            while (!exampleList.IsEmpty())
            {
                List<Core.Transformation> transformationList = null;

                List<Example> combinedExamples = new List<Example>();

                foreach (var example in exampleList)
                {
                    combinedExamples.Add(example);
                    var examplesAsTuples = ExampleAsTupleList(combinedExamples);
                    var newTransformations = refazer.LearnTransformations(examplesAsTuples);

                    if (newTransformations.IsEmpty())
                    {
                        combinedExamples.Remove(example);
                    }
                    else
                    {
                        transformationList = newTransformations.ToList();
                    }
                }

                foreach (var example in combinedExamples)
                {
                    exampleList.Remove(example);
                }

                if (transformationList != null)
                {
                    SaveTransformation(keyPoint, transformationList);
                }
            }
        }

        public List<String> ApplyTransformationsForSubmission(Submission2 submission)
        {
            List<String> generatedCodeList = new List<String>();

            foreach (var transformation in SearchTransformation(submission))
            {
                try
                {
                    generatedCodeList.AddRange(refazer.Apply(
                        transformation, submission.Code));
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Exception when trying to apply transformations");
                    Trace.TraceError(ex.Message);
                }
            }

            return generatedCodeList;
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
    }
}