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

        // GET: api/examples
        [Route("")]
        public IQueryable<Example> GetExamples()
        {
            return db.Examples;
        }

        // GET: api/examples/5
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

        // PUT: api/examples/5
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

        // POST: api/examples
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

        // DELETE: api/examples/5
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

        // DELETE: api/Example/Clear
        [Route("Clear"), HttpDelete]
        public IHttpActionResult DeleteAllExample()
        {
            db.Examples.RemoveRange(db.Examples);
            db.SaveChanges();

            return Ok("All examples were deleted!");
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