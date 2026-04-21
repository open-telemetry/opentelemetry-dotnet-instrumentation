// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using Examples.AspNetCoreMvc.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Examples.AspNetCoreMvc.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        var instrumentationType = Type.GetType("OpenTelemetry.AutoInstrumentation.Instrumentation, OpenTelemetry.AutoInstrumentation");
        ViewBag.TracerAssemblyLocation = Type.GetType("OpenTelemetry.Trace.Tracer, OpenTelemetry.Api")?.Assembly.Location;
        ViewBag.ClrProfilerAssemblyLocation = instrumentationType?.Assembly.Location;
        ViewBag.StackTrace = StackTraceHelper.GetUsefulStack();

        var prefixes = new[] { "COR_", "CORECLR_", "DOTNET_", "OTEL_" };

        var envVars = from envVar in Environment.GetEnvironmentVariables().Cast<DictionaryEntry>()
                      from prefix in prefixes
                      let key = (envVar.Key as string)?.ToUpperInvariant()
                      let value = envVar.Value as string
                      where key.StartsWith(prefix, StringComparison.Ordinal)
                      orderby key
                      select new KeyValuePair<string, string>(key, value);

        return View(envVars.ToList());
    }

    [Route("delay/{seconds}")]
    public IActionResult Delay(int seconds)
    {
        ViewBag.StackTrace = StackTraceHelper.GetUsefulStack();
        Thread.Sleep(TimeSpan.FromSeconds(seconds));
        return View(seconds);
    }

    [Route("delay-async/{seconds}")]
    public async Task<IActionResult> DelayAsync(int seconds)
    {
        ViewBag.StackTrace = StackTraceHelper.GetUsefulStack();
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
