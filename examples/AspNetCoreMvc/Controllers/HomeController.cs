// <copyright file="HomeController.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Examples.AspNetCoreMvc.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Examples.AspNetCoreMvc.Controllers;

public class HomeController : Controller
{
    private const string CorrelationIdentifierHeaderName = "sample.correlation.identifier";
    private static readonly Meter MyMeter = new("MyCompany.MyProduct.MyLibrary", "1.0");
    private static readonly Counter<long> MyFruitCounter = MyMeter.CreateCounter<long>("MyFruitCounter");

    public IActionResult Index()
    {
        MyFruitCounter.Add(1, new("name", "apple"), new("color", "red"));
        MyFruitCounter.Add(2, new("name", "lemon"), new("color", "yellow"));
        MyFruitCounter.Add(1, new("name", "lemon"), new("color", "yellow"));
        MyFruitCounter.Add(2, new("name", "apple"), new("color", "green"));
        MyFruitCounter.Add(5, new("name", "apple"), new("color", "red"));
        MyFruitCounter.Add(4, new("name", "lemon"), new("color", "yellow"));

        var instrumentationType = Type.GetType("OpenTelemetry.AutoInstrumentation.Instrumentation, OpenTelemetry.AutoInstrumentation");
        ViewBag.ProfilerAttached = instrumentationType?.GetProperty("ProfilerAttached", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) ?? false;
        ViewBag.TracerAssemblyLocation = Type.GetType("OpenTelemetry.Trace.Tracer, OpenTelemetry.Api")?.Assembly.Location;
        ViewBag.ClrProfilerAssemblyLocation = instrumentationType?.Assembly.Location;
        ViewBag.StackTrace = StackTraceHelper.GetUsefulStack();

        var prefixes = new[] { "COR_", "CORECLR_", "DOTNET_", "OTEL_" };

        var envVars = from envVar in Environment.GetEnvironmentVariables().Cast<DictionaryEntry>()
            from prefix in prefixes
            let key = (envVar.Key as string)?.ToUpperInvariant()
            let value = envVar.Value as string
            where key.StartsWith(prefix)
            orderby key
            select new KeyValuePair<string, string>(key, value);

        AddCorrelationIdentifierToResponse();
        return View(envVars.ToList());
    }

    [Route("delay/{seconds}")]
    public IActionResult Delay(int seconds)
    {
        ViewBag.StackTrace = StackTraceHelper.GetUsefulStack();
        Thread.Sleep(TimeSpan.FromSeconds(seconds));
        AddCorrelationIdentifierToResponse();
        return View(seconds);
    }

    [Route("delay-async/{seconds}")]
    public async Task<IActionResult> DelayAsync(int seconds)
    {
        ViewBag.StackTrace = StackTraceHelper.GetUsefulStack();
        await Task.Delay(TimeSpan.FromSeconds(seconds));
        AddCorrelationIdentifierToResponse();
        return View("Delay", seconds);
    }

    [Route("bad-request")]
    public IActionResult ThrowException()
    {
        AddCorrelationIdentifierToResponse();
        throw new Exception("This was a bad request.");
    }

    [Route("status-code/{statusCode}")]
    public string StatusCodeTest(int statusCode)
    {
        AddCorrelationIdentifierToResponse();
        HttpContext.Response.StatusCode = statusCode;
        return $"Status code has been set to {statusCode}";
    }

    [Route("alive-check")]
    public string IsAlive()
    {
        AddCorrelationIdentifierToResponse();
        return "Yes";
    }

    private void AddCorrelationIdentifierToResponse()
    {
        if (Request.Headers.ContainsKey(CorrelationIdentifierHeaderName))
        {
            Response.Headers.Add(CorrelationIdentifierHeaderName, Request.Headers[CorrelationIdentifierHeaderName]);
        }
    }
}
