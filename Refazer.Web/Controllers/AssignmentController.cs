using Refazer.Web.Models;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Description;

namespace Refazer.Web.Controllers
{
    [RoutePrefix("api/assignments")]
    public class AssignmentController : ApiController
    {

        private RefazerDbContext db = new RefazerDbContext();

        // GET: api/Assignments
        [Route("")]
        public IQueryable<Assignment> GetAssignments()
        {
            return db.Assignments;
        }

        // GET: api/Assignments/5
        [Route("{id:int}")]
        [ResponseType(typeof(Assignment))]
        public IHttpActionResult GetAssignment(int id)
        {
            Assignment assignment = FindById(id);
            if (assignment == null)
            {
                return NotFound();
            }

            return Ok(assignment);
        }

        // PUT: api/Assignments/5
        [Route("{id:int}"), HttpPut]
        [ResponseType(typeof(void))]
        public IHttpActionResult PutAssignment(int id, Assignment assignment)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != assignment.Id)
            {
                return BadRequest();
            }

            db.Entry(assignment).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AssignmentExists(id))
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

        // POST: api/Assignments
        [Route(""), HttpPost]
        [ResponseType(typeof(Assignment))]
        public IHttpActionResult PostAssignment(Assignment assignment)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.Assignments.Add(assignment);
            db.SaveChanges();

            return CreatedAtRoute("", new { id = assignment.Id }, assignment);
        }

        // DELETE: api/Assignments/5
        [Route("{id:int}"), HttpDelete]
        [ResponseType(typeof(Assignment))]
        public IHttpActionResult DeleteAssignment(int id)
        {
            Assignment assignment = FindById(id);
            if (assignment == null)
            {
                return NotFound();
            }

            db.Assignments.Remove(assignment);
            db.SaveChanges();

            return Ok(assignment);
        }

        // DELETE: api/Assignment/clear
        [Route("clear"), HttpDelete]
        public IHttpActionResult DeleteAllAssignments()
        {
            db.Assignments.RemoveRange(db.Assignments);
            db.SaveChanges();

            return Ok("All assignments were deleted!");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool AssignmentExists(int id)
        {
            return db.Assignments.Count(e => e.Id == id) > 0;
        }

        private Assignment FindById(int id)
        {
            return db.Assignments.Where(a => a.Id == id)
                .FirstOrDefault();
        }
    }
}