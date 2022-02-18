using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Samples.StartupHook.AspNetCoreMvc31.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Samples.StartupHook.AspNetCoreMvc31.Controllers;

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

        try
        {
            client.GetStringAsync("http://httpstat.us/500").Wait();
        }
        catch 
        {
        }
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