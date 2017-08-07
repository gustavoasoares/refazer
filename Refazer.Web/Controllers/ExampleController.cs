using Refazer.Core;
using Refazer.Web.Utils;
using Refazer.WebAPI.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Description;

namespace Refazer.WebAPI.Controllers
{
    [RoutePrefix("api/examples")]
    public class ExampleController : ApiController
    {
        private RefazerDbContext db = new RefazerDbContext();

        // GET: api/Example
        [Route("")]
        public IQueryable<Example> GetExamples()
        {
            return db.Examples;
        }

        // GET: api/Example/5
        [Route("{id:int}")]
        [ResponseType(typeof(Example))]
        public IHttpActionResult GetExample(int id)
        {
            Example example = db.Examples.Find(id);
            if (example == null)
            {
                return NotFound();
            }

            return Ok(example);
        }

        // PUT: api/Example/5
        [Route("{id:int}"), HttpPut]
        [ResponseType(typeof(void))]
        public IHttpActionResult PutExample(int id, Example example)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != example.Id)
            {
                return BadRequest();
            }

            db.Entry(example).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ExampleExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: api/Example
        [Route(""), HttpPost]
        [ResponseType(typeof(Example))]
        public IHttpActionResult PostExample(Example example)
        {
            example.CorrectCode = FunctionExtractor.ExtractPythonFunction(
                example.CorrectCode, example.Question);

            example.IncorrectCode = FunctionExtractor.ExtractPythonFunction(
                example.IncorrectCode, example.Question);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.Examples.Add(example);
            db.SaveChanges();

            return CreatedAtRoute("", new { id = example.Id }, example);
        }

        // DELETE: api/Example/5
        [Route("{id:int}"), HttpDelete]
        [ResponseType(typeof(Example))]
        public IHttpActionResult DeleteExample(int id)
        {
            Example example = db.Examples.Find(id);
            if (example == null)
            {
                return NotFound();
            }

            db.Examples.Remove(example);
            db.SaveChanges();

            return Ok(example);
        }

        // DELETE: api/Example/5
        [Route("clear"), HttpDelete]
        public IHttpActionResult DeleteAllExample()
        {
            db.Examples.RemoveRange(db.Examples);
            db.SaveChanges();

            return Ok();
        }

        private void LearnTransformationFromExample(Example example)
        {
            Core.Refazer refazer = BuildRefazer();
            Tuple<String, String> exampleAsTuple = Tuple.Create(example.IncorrectCode, example.CorrectCode);

            var transformations = refazer.LearnTransformations(new List<Tuple<string,
                string>>() { exampleAsTuple });

            //Fixing
            var output = refazer.Apply(transformations.First(), example.IncorrectCode);
            foreach (var newCode in output)
            {
                Debug.WriteLine("\n Código Corrigido \n");
                Debug.WriteLine(newCode);
            }
        }

        private Core.Refazer BuildRefazer()
        {
            var pathToGrammar = System.Web.Hosting.HostingEnvironment.MapPath(@"~/Content/");
            var pathToDslLib = System.Web.Hosting.HostingEnvironment.MapPath(@"~/bin");
            var refazer = new Refazer4Python(pathToGrammar, pathToDslLib);
            return refazer;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool ExampleExists(int id)
        {
            return db.Examples.Count(e => e.Id == id) > 0;
        }
    }
}