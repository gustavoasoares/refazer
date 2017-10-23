using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Description;
using Refazer.Web.Models;
using System.Collections.Generic;

namespace Refazer.Web.Controllers
{
    [RoutePrefix("api/clusters")]
    public class ClusterController : ApiController
    {
        private RefazerDbContext db = new RefazerDbContext();

        // GET: api/Clusters
        [Route("")]
        public List<Cluster> GetClusters()
        {
            List<Cluster> clustersList = db.Clusters.ToList();
            clustersList.Sort();

            return clustersList;
        }

        // GET: api/Clusters/5
        [Route("{id:int}")]
        [ResponseType(typeof(Cluster))]
        public IHttpActionResult GetCluster(int id)
        {
            Cluster cluster = db.Clusters.Find(id);
            if (cluster == null)
            {
                return NotFound();
            }

            return Ok(cluster);
        }

        // PUT: api/Clusters/5
        [Route("{id:int}"), HttpPut]
        [ResponseType(typeof(void))]
        public IHttpActionResult PutCluster(int id, Cluster cluster)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != cluster.Id)
            {
                return BadRequest();
            }

            db.Entry(cluster).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ClusterExists(id))
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

        // POST: api/Clusters
        [Route(""), HttpPost]
        [ResponseType(typeof(Cluster))]
        public IHttpActionResult PostCluster(Cluster cluster)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.Clusters.Add(cluster);
            db.SaveChanges();

            return CreatedAtRoute("", new { id = cluster.Id }, cluster);
        }

        // DELETE: api/Clusters/5
        [Route("{id:int}"), HttpDelete]
        [ResponseType(typeof(Cluster))]
        public IHttpActionResult DeleteCluster(int id)
        {
            Cluster cluster = db.Clusters.Find(id);
            if (cluster == null)
            {
                return NotFound();
            }

            db.Clusters.Remove(cluster);
            db.SaveChanges();

            return Ok(cluster);
        }

        // DELETE: api/clusters/clear
        [Route("clear"), HttpDelete]
        public IHttpActionResult DeleteAllClusters()
        {
            db.Clusters.RemoveRange(db.Clusters);
            db.SaveChanges();

            return Ok("All clusters were deleted!");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool ClusterExists(int id)
        {
            return db.Clusters.Count(e => e.Id == id) > 0;
        }
    }
}