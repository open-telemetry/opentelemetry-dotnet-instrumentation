using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using Samples.AspNet.Helpers;

namespace Samples.AspNet.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            var prefixes = new[] { "COR_", "CORECLR_", "OTEL_" };

            var envVars = from envVar in Environment.GetEnvironmentVariables().Cast<DictionaryEntry>()
                          from prefix in prefixes
                          let key = (envVar.Key as string)?.ToUpperInvariant()
                          let value = envVar.Value as string
                          where key.StartsWith(prefix)
                          orderby key
                          select new KeyValuePair<string, string>(key, value);

            var instrumentationType = Type.GetType("OpenTelemetry.ClrProfiler.Managed.Instrumentation, OpenTelemetry.ClrProfiler.Managed");

            ViewBag.EnvVars = envVars;
            ViewBag.HasEnvVars = envVars.Any();
            ViewBag.ProfilerAttached = instrumentationType?.GetProperty("ProfilerAttached", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) ?? false;
            ViewBag.TracerAssemblyLocation = instrumentationType.Assembly.Location;
            ViewBag.TracerAssemblies = AssembliesHelper.GetLoadedTracesAssemblies();
            ViewBag.AllAssemblies = AssembliesHelper.GetLoadedAssemblies();

            return View();
        }
    }
}
