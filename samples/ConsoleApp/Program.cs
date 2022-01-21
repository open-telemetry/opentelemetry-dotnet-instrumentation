using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using OpenTracing.Propagation;
using OpenTracing.Util;

namespace ConsoleApp
{
    internal class Program
    {
        private static readonly ActivitySource MyActivitySource = new ActivitySource("OpenTelemetry.ClrProfiler.ConsoleApp");

        private static async Task<int> Main()
        {
            Console.WriteLine("Ensure right version of DiagnosticSource is installed");
            Console.WriteLine(typeof(ActivitySource).Assembly.FullName);

            using (var activity = MyActivitySource.StartActivity("Main"))
            {
                await OpenTracingLibrary.Wrapper.WithOpenTracingSpanAsync("client", RunAsync);
            }

            return 0;
        }

        private static async Task RunAsync()
        {
            using var client = new HttpClient();
            using (var activity = MyActivitySource.StartActivity("RunAsync"))
            {
                activity?.SetTag("foo", "bar");

                await HttpGet("https://www.example.com/default-handler");
                await HttpGet("http://127.0.0.1:8080/api/mongo");
                await HttpGet("http://127.0.0.1:8080/api/redis");
            }

            const string requestUrl = "https://www.example.com/";
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(requestUrl),
                Method = HttpMethod.Post,
                Content = new StringContent(string.Empty, Encoding.UTF8),
            };

            var tracer = GlobalTracer.Instance;
            using (var scope = tracer.BuildSpan("Client POST " + request.RequestUri)
                                     .WithTag("span.kind", "client")
                                     .StartActive())
            {
                var contextPropagationHeaders = new Dictionary<string, string>();
                var contextCarrier = new TextMapInjectAdapter(contextPropagationHeaders);
                tracer.Inject(scope.Span.Context, BuiltinFormats.TextMap, contextCarrier);
                foreach (var kvp in contextPropagationHeaders)
                {
                    request.Headers.Add(kvp.Key, kvp.Value);
                }

                int? statusCode;
                int? responseLength = null;
                try
                {
                    using var response = await client.SendAsync(request);

                    statusCode = (int) response.StatusCode;
                    var responseContent = await response.Content.ReadAsStringAsync();
                    responseLength = responseContent.Length;
                }
                catch (HttpRequestException e)
                {
                    LogHttpRequestException(requestUrl, e);
#if NET5_0_OR_GREATER
                    statusCode = (int?) e.StatusCode;
#else
                    statusCode = null;
#endif
                }

                if (statusCode.HasValue)
                {
                    scope.Span.SetTag("http.status_code", statusCode.Value);
                }

                if (responseLength.HasValue)
                {
                    scope.Span.SetTag("response.length", responseLength.Value);
                }
            }
        }

        private static void LogHttpRequestException(string requestUrl, HttpRequestException httpRequestException)
        {
            Console.WriteLine($"HttpRequestException occurred while calling {requestUrl}, {httpRequestException}");
        }

        private static async Task HttpGet(string url)
        {
            try
            {
                using var client = new HttpClient();
                Console.WriteLine($"Calling {url}");
                await client.GetAsync(url);
                Console.WriteLine($"Called {url}");
            }
            catch (HttpRequestException e)
            {
                LogHttpRequestException(url, e);
            }
        }
    }
}
