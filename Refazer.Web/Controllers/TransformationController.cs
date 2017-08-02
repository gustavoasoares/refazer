using Refazer.WebAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Refazer.Web.Controllers
{
    [RoutePrefix("api/transformations")]
    public class TransformationController : ApiController
    {

        private RefazerDbContext db = new RefazerDbContext();

        // GET api/<controller>
        [Route("")]
        public IQueryable<Transformation2> GetTransformations()
        {
            return db.Transformations2;
        }
    }
}