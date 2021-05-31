using System;
using System.Net.Http;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ConsoleApp
{
    internal class Program
    {
        private static readonly ActivitySource MyActivitySource = new ActivitySource("OpenTelemetry.AutoInstrumentation.ConsoleApp");

        private static async Task<int> Main()
        {
            using (var activity = MyActivitySource.StartActivity("Main"))
            {
#if openTracingWrapper
                await OpenTracingLibrary.Wrapper.WithOpenTracingSpanAsync("client", RunAsync);
#else
                await RunAsync();
#endif
            }

            return 0;
        }

        private static async Task RunAsync()
        {
            using (var activity = MyActivitySource.StartActivity("RunAsync"))
            {
                activity?.SetTag("foo", "bar");

                var baseAddress = new Uri("https://www.example.com/");
                var regularHttpClient = new HttpClient { BaseAddress = baseAddress };

                Console.WriteLine("Calling regularHttpClient.GetAsync");
                await regularHttpClient.GetAsync("default-handler");
                Console.WriteLine("Called regularHttpClient.GetAsync");

                var client = new HttpClient();
                Console.WriteLine("Calling client.GetAsync");
                await client.GetAsync("http://127.0.0.1:8080");
                Console.WriteLine("Called client.GetAsync");
            }
        }
    }
}
