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
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using TestApplication.AspNet.Helpers;

namespace TestApplication.AspNet.Controllers;

public class HomeController : Controller
{
    public ActionResult Index()
    {
        var prefixes = new[] { "COR_", "CORECLR_", "DOTNET_", "OTEL_" };

        var envVars = from envVar in Environment.GetEnvironmentVariables().Cast<DictionaryEntry>()
                      from prefix in prefixes
                      let key = (envVar.Key as string)?.ToUpperInvariant()
                      let value = envVar.Value as string
                      where key.StartsWith(prefix)
                      orderby key
                      select new KeyValuePair<string, string>(key, value);

        var instrumentationType =
            Type.GetType("OpenTelemetry.AutoInstrumentation.Instrumentation, OpenTelemetry.AutoInstrumentation");

        ViewBag.EnvVars = envVars;
        ViewBag.HasEnvVars = envVars.Any();
        ViewBag.ProfilerAttached =
            instrumentationType?.GetProperty("ProfilerAttached", BindingFlags.Public | BindingFlags.Static)
                ?.GetValue(null) ?? false;
        ViewBag.TracerAssemblyLocation = instrumentationType?.Assembly.Location;
        ViewBag.TracerAssemblies = AssembliesHelper.GetLoadedTracesAssemblies();
        ViewBag.AllAssemblies = AssembliesHelper.GetLoadedAssemblies();

        return View();
    }
}
