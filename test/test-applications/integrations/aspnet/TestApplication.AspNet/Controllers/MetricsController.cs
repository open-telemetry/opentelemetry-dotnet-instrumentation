using System.Net.Http;
using System.Web.Mvc;

namespace TestApplication.AspNet.Controllers
{
    public class MetricsController : Controller
    {
        // GET: Metrics
        // Reads metrics from prometheus scrape endpoint.
        public ActionResult Index()
        {
            using var client = new HttpClient();
            var responseMessage = client.GetAsync("http://localhost:9464/metrics").Result;
            return Content(responseMessage.Content.ReadAsStringAsync().Result);
        }
    }
}
