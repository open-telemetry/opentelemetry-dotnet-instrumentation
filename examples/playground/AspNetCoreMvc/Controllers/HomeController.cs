// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.AspNetCore.Mvc;

namespace Examples.AspNetCoreMvc.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        var instrumentationType = Type.GetType("OpenTelemetry.AutoInstrumentation.Instrumentation, OpenTelemetry.AutoInstrumentation");
        ViewBag.TracerAssemblyLoaded = Type.GetType("OpenTelemetry.Trace.Tracer, OpenTelemetry.Api") is not null;
        ViewBag.ClrProfilerAssemblyLoaded = instrumentationType is not null;

        return View();
    }

    [Route("delay/{seconds}")]
    public IActionResult Delay(int seconds)
    {
        Thread.Sleep(TimeSpan.FromSeconds(seconds));
        return View(seconds);
    }

    [Route("delay-async/{seconds}")]
    public async Task<IActionResult> DelayAsync(int seconds)
    {
        await Task.Delay(TimeSpan.FromSeconds(seconds)).ConfigureAwait(false);
        return View("Delay", seconds);
    }

    [Route("bad-request")]
    public IActionResult ThrowException()
    {
        throw new InvalidOperationException("This was a bad request.");
    }

    [Route("status-code/{statusCode}")]
    public string StatusCodeTest(int statusCode)
    {
        HttpContext.Response.StatusCode = statusCode;
        return $"Status code has been set to {statusCode}";
    }

    [Route("alive-check")]
    public string IsAlive()
    {
        return "Yes";
    }
}
