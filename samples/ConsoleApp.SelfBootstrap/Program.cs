using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace ConsoleApp.SelfBootstrap
{
    internal class Program
    {
        private static readonly ActivitySource MyActivitySource = new ActivitySource("OpenTelemetry.AutoInstrumentation.ConsoleApp");
        private static TracerProvider _tracerProvider;

        private static async Task<int> Main()
        {
            var builder = Sdk
                .CreateTracerProviderBuilder()
                .AddHttpClientInstrumentation()
                .AddSqlClientInstrumentation()
                .SetSampler(new AlwaysOnSampler())
                .AddSource("OpenTelemetry.AutoInstrumentation.*")
                .AddConsoleExporter()
                .AddZipkinExporter(options =>
                {
                    options.Endpoint = new Uri("http://localhost:9411/api/v2/spans");
                    options.ExportProcessorType = ExportProcessorType.Simple;
                });

            _tracerProvider = builder.Build();
            
            using (var activity = MyActivitySource.StartActivity("Main"))
            {
                activity?.SetTag("foo", "bar");

                var baseAddress = new Uri("https://www.example.com/");
                var regularHttpClient = new HttpClient { BaseAddress = baseAddress };

                Console.WriteLine("Calling regularHttpClient.GetAsync");
                await regularHttpClient.GetAsync("default-handler");
                Console.WriteLine("Called regularHttpClient.GetAsync");

                Console.WriteLine("Calling client.GetAsync");
                await regularHttpClient.GetAsync("http://127.0.0.1:8080");
                Console.WriteLine("Called client.GetAsync");

                return 0;
            }
        }
    }
}
