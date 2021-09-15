using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace Samples.AspNetCoreMvc.Controllers
{
    [Route("api/redis")]
    public class RedisController : ControllerBase
    {
        public static ConnectionMultiplexer Connection = ConnectionMultiplexer.Connect("localhost");

        [HttpGet]
        [Route("")]
        public IActionResult Index()
        {
            var db = Connection.GetDatabase();
            bool success = db.StringSet("mykey", new RedisValue("myval"));
            return Ok(success.ToString());
        }
    }
}
