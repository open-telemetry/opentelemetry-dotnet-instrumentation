using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace OldReference
{
    public static class InstrumentedHttpCall
    {
        private static readonly ActivitySource MyActivitySource = new("OpenTelemetry.AutoInstrumentation.BindingRedirect");

        public static async Task GetAsync(string url)
        {
            Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>> System.Diagnostics.DiagnosticSource assemblies loaded:");
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var loaded = assemblies
                .Where(assembly => assembly.FullName.Contains("System.Diagnostics.DiagnosticSource"))
                .Select(assembly => $">>>>>>>>>>>>>>>>>>>>>>> {assembly.FullName}");
            
            Console.WriteLine(string.Join("\n", loaded));

            using (var activity = MyActivitySource.StartActivity("RunAsync"))
            {
                activity?.SetTag("foo", "bar");

                using var client = new HttpClient();
                Console.WriteLine($"Calling {url}");
                await client.GetAsync(url);
                Console.WriteLine($"Called {url}");
            }
        }
    }
}
