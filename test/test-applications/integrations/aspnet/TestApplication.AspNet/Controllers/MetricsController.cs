using System.Diagnostics.Metrics;
using System.Net.Http;
using System.Web.Mvc;

namespace TestApplication.AspNet.Controllers
{
    public class MetricsController : Controller
    {
        private static readonly Meter MyMeter = new("MyCompany.MyProduct.MyLibrary", "1.0");
        private static readonly Counter<long> MyFruitCounter = MyMeter.CreateCounter<long>("MyFruitCounter");

        // GET: Metrics
        public ActionResult Index()
        {
            MyFruitCounter.Add(1, new("name", "apple"), new("color", "red"));
            MyFruitCounter.Add(2, new("name", "lemon"), new("color", "yellow"));
            MyFruitCounter.Add(1, new("name", "lemon"), new("color", "yellow"));
            MyFruitCounter.Add(2, new("name", "apple"), new("color", "green"));
            MyFruitCounter.Add(5, new("name", "apple"), new("color", "red"));
            MyFruitCounter.Add(4, new("name", "lemon"), new("color", "yellow"));

            return Content("Added Metrics Successfully!!!");
        }

        // GET: PrometheusMetrics
        public ActionResult GetPrometheusMetrics()
        {
            using var client = new HttpClient();
            var responseMessage = client.GetAsync("http://localhost:9464/metrics").Result;
            return Content(responseMessage.Content.ReadAsStringAsync().Result);
        }
    }
}
