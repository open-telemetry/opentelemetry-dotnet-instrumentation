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
using System.Linq;
using System.Web.Mvc;
using TestApplication.AspNet.NetFramework.Helpers;
using TestApplication.Shared;

namespace TestApplication.AspNet.NetFramework.Controllers;

public class HomeController : Controller
{
    public ActionResult Index()
    {
        var envVars = ProfilerHelper.GetEnvironmentConfiguration();

        ViewBag.EnvVars = envVars;
        ViewBag.HasEnvVars = envVars.Any();
        ViewBag.TracerAssemblies = AssembliesHelper.GetLoadedTracesAssemblies();
        ViewBag.AllAssemblies = AssembliesHelper.GetLoadedAssemblies();

        try
        {
            var headers = HttpContext.Response.Headers;

            headers.Add("Custom-Response-Test-Header1", "Test-Value4");
            headers.Add("Custom-Response-Test-Header2", "Test-Value5");
            headers.Add("Custom-Response-Test-Header3", "Test-Value6");
        }
        catch (PlatformNotSupportedException)
        {
            // do nothing, it can be raised on classic mode
        }

        return View();
    }
}
