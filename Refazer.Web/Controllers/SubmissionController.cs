using Refazer.Core;
using Refazer.WebAPI.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Refazer.Web.Controllers
{
    [RoutePrefix("api/submissions")]
    public class SubmissionController : ApiController
    {

        private RefazerDbContext db = new RefazerDbContext();

        // GET api/<controller>
        //[Route("")]
        //public IQueryable<Example> GetTransformations()
        //{
        //   return db.Examples;
        //}

        //private void LearnTransformationFromExample(Example example)
        //{
        //    Core.Refazer refazer = BuildRefazer();
        //Tuple<String, String> exampleAsTuple = Tuple.Create(example.IncorrectCode, example.CorrectCode);

        //var transformations = refazer.LearnTransformations(new List<Tuple<string,
        //string>>() { exampleAsTuple });

        //Fixing
        //var output = refazer.Apply(transformations.First(), example.IncorrectCode);
        //  foreach (var newCode in output)
        // {
        //Debug.WriteLine("\n Código Corrigido \n");
        //Debug.WriteLine(newCode);
        //}

        //private Core.Refazer BuildRefazer()
        //{
        //    var pathToGrammar = System.Web.Hosting.HostingEnvironment.MapPath(@"~/Content/");
        //   var pathToDslLib = System.Web.Hosting.HostingEnvironment.MapPath(@"~/bin");
        //  var refazer = new Refazer4Python(pathToGrammar, pathToDslLib);
        // return refazer;
        //}
    }
}