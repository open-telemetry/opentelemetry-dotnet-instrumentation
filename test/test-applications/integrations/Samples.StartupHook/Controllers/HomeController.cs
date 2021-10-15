using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Samples.StartupHook.Models;
using System.Diagnostics;
using System.Net.Http;

namespace Samples.StartupHook.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            var client = new HttpClient();
            client.GetStringAsync("http://httpstat.us/200").Wait();

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
