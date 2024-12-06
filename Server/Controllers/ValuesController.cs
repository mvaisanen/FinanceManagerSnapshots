using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class ValuesController : Controller
    {
        // GET api/values
        [HttpGet]
        //public IEnumerable<string> Get()
        public IActionResult Get()
        {
            var t = HttpContext.User;
            Console.WriteLine("Kutsuva kayttaja: " + User.Identity.Name);
            Task.Delay(1000).Wait();
            //return BadRequest("Testivirhe");
            //return new string[] { "value1", "value2" };
            return Ok(new string[] { "value1", "value2" });
            //return Ok("testi,");
        }

        /*[HttpGet]
        [Route("getuser")]
        public IActionResult GetUser()
        {
            var user = new Auth.Entities.User() { FirstName = "pelle", LastName = "miljoona", Id = 1, Password = "passu", Token = "tokentext", Username = "pmiljoona" };
            return Ok(user);
        }*/

        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
