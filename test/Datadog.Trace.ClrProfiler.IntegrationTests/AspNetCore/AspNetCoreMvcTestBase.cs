using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Datadog.Core.Tools;
using Datadog.Trace.Configuration;
using Datadog.Trace.TestHelpers;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests.AspNetCore
{
    public abstract class AspNetCoreMvcTestBase : TestHelper
    {
        protected const string TopLevelOperationName = "aspnet_core.request";

        protected const string HeaderName1 = "datadog-header-name";
        protected const string HeaderName1Upper = "DATADOG-HEADER-NAME";
        protected const string HeaderValue1 = "asp-net-core";
        protected const string HeaderTagName1 = "datadog-header-tag";

        protected AspNetCoreMvcTestBase(string sampleAppName, ITestOutputHelper output, string serviceVersion)
            : base(sampleAppName, output)
        {
            ServiceVersion = serviceVersion;
            HttpClient = new HttpClient();
            HttpClient.DefaultRequestHeaders.Add(HeaderName1, HeaderValue1);
            SetEnvironmentVariable(ConfigurationKeys.HeaderTags, $"{HeaderName1Upper}:{HeaderTagName1}");
            SetEnvironmentVariable(ConfigurationKeys.HttpServerErrorStatusCodes, "400-403, 500-501-234, s342, 500");

            SetServiceVersion(ServiceVersion);

            CreateTopLevelExpectation(url: "/", httpMethod: "GET", httpStatus: "200", resourceUrl: "Home/Index", serviceVersion: ServiceVersion);
            CreateTopLevelExpectation(url: "/delay/0", httpMethod: "GET", httpStatus: "200", resourceUrl: "delay/{seconds}", serviceVersion: ServiceVersion);
            CreateTopLevelExpectation(url: "/api/delay/0", httpMethod: "GET", httpStatus: "200", resourceUrl: "api/delay/{seconds}", serviceVersion: ServiceVersion);
            CreateTopLevelExpectation(url: "/not-found", httpMethod: "GET", httpStatus: "404", resourceUrl: "/not-found", serviceVersion: ServiceVersion);
            CreateTopLevelExpectation(url: "/status-code/203", httpMethod: "GET", httpStatus: "203", resourceUrl: "status-code/{statusCode}", serviceVersion: ServiceVersion);

            CreateTopLevelExpectation(
                url: "/status-code/500",
                httpMethod: "GET",
                httpStatus: "500",
                resourceUrl: "status-code/{statusCode}",
                serviceVersion: ServiceVersion,
                additionalCheck: span =>
                                 {
                                     var failures = new List<string>();

                                     if (span.Error == 0)
                                     {
                                         failures.Add($"Expected Error flag set within {span.Resource}");
                                     }

                                     if (SpanExpectation.GetTag(span, Tags.ErrorType) != null)
                                     {
                                         failures.Add($"Did not expect exception type within {span.Resource}");
                                     }

                                     var errorMessage = SpanExpectation.GetTag(span, Tags.ErrorMsg);

                                     if (errorMessage != "The HTTP response has status code 500.")
                                     {
                                         failures.Add($"Expected specific error message within {span.Resource}. Found \"{errorMessage}\"");
                                     }

                                     return failures;
                                 });

            CreateTopLevelExpectation(
                url: "/bad-request",
                httpMethod: "GET",
                httpStatus: "500",
                resourceUrl: "bad-request",
                serviceVersion: ServiceVersion,
                additionalCheck: span =>
                {
                    var failures = new List<string>();

                    if (span.Error == 0)
                    {
                        failures.Add($"Expected Error flag set within {span.Resource}");
                    }

                    if (SpanExpectation.GetTag(span, Tags.ErrorType) != "System.Exception")
                    {
                        failures.Add($"Expected specific exception within {span.Resource}");
                    }

                    var errorMessage = SpanExpectation.GetTag(span, Tags.ErrorMsg);

                    if (errorMessage != "This was a bad request.")
                    {
                        failures.Add($"Expected specific error message within {span.Resource}. Found \"{errorMessage}\"");
                    }

                    return failures;
                });

            CreateTopLevelExpectation(
                url: "/status-code/402",
                httpMethod: "GET",
                httpStatus: "402",
                resourceUrl: "status-code/{statusCode}",
                serviceVersion: ServiceVersion,
                additionalCheck: span =>
                {
                    var failures = new List<string>();

                    if (span.Error == 0)
                    {
                        failures.Add($"Expected Error flag set within {span.Resource}");
                    }

                    var errorMessage = SpanExpectation.GetTag(span, Tags.ErrorMsg);

                    if (errorMessage != "The HTTP response has status code 402.")
                    {
                        failures.Add($"Expected specific error message within {span.Resource}. Found \"{errorMessage}\"");
                    }

                    return failures;
                });
        }

        public string ServiceVersion { get; }

        protected HttpClient HttpClient { get; }

        protected List<AspNetCoreMvcSpanExpectation> Expectations { get; set; } = new List<AspNetCoreMvcSpanExpectation>();

        public async Task RunTraceTestOnSelfHosted(string packageVersion)
        {
            var agentPort = TcpPortProvider.GetOpenPort();
            var aspNetCorePort = TcpPortProvider.GetOpenPort();

            using (var agent = new MockTracerAgent(agentPort))
            using (var process = StartSample(agent.Port, arguments: null, packageVersion: packageVersion, aspNetCorePort: aspNetCorePort))
            {
                agent.SpanFilters.Add(IsNotServerLifeCheck);

                var wh = new EventWaitHandle(false, EventResetMode.AutoReset);

                process.OutputDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        if (args.Data.Contains("Now listening on:") || args.Data.Contains("Unable to start Kestrel"))
                        {
                            wh.Set();
                        }

                        Output.WriteLine($"[webserver][stdout] {args.Data}");
                    }
                };
                process.BeginOutputReadLine();

                process.ErrorDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        Output.WriteLine($"[webserver][stderr] {args.Data}");
                    }
                };

                process.BeginErrorReadLine();

                wh.WaitOne(5000);

                var maxMillisecondsToWait = 15_000;
                var intervalMilliseconds = 500;
                var intervals = maxMillisecondsToWait / intervalMilliseconds;
                var serverReady = false;

                // wait for server to be ready to receive requests
                while (intervals-- > 0)
                {
                    try
                    {
                        serverReady = await SubmitRequest(aspNetCorePort, "/alive-check") == HttpStatusCode.OK;
                    }
                    catch
                    {
                        // ignore
                    }

                    if (serverReady)
                    {
                        break;
                    }

                    Thread.Sleep(intervalMilliseconds);
                }

                if (!serverReady)
                {
                    throw new Exception("Couldn't verify the application is ready to receive requests.");
                }

                var testStart = DateTime.Now;

                var paths = Expectations.Select(e => e.OriginalUri).ToArray();
                await SubmitRequests(aspNetCorePort, paths);

                var spans =
                    agent.WaitForSpans(
                              Expectations.Count,
                              operationName: TopLevelOperationName,
                              minDateTime: testStart)
                         .OrderBy(s => s.Start)
                         .ToList();

                if (!process.HasExited)
                {
                    process.Kill();
                }

                SpanTestHelpers.AssertExpectationsMet(Expectations, spans);
            }
        }

        protected void CreateTopLevelExpectation(
            string url,
            string httpMethod,
            string httpStatus,
            string resourceUrl,
            string serviceVersion,
            Func<MockTracerAgent.Span, List<string>> additionalCheck = null)
        {
            var resourceName = $"{httpMethod.ToUpper()} {resourceUrl}";

            var expectation = new AspNetCoreMvcSpanExpectation(
                                  EnvironmentHelper.FullSampleName,
                                  serviceVersion,
                                  TopLevelOperationName,
                                  resourceName,
                                  httpStatus,
                                  httpMethod)
                              {
                                  OriginalUri = url,
                              };

            expectation.RegisterDelegateExpectation(additionalCheck);
            expectation.RegisterTagExpectation(HeaderTagName1, HeaderValue1);

            Expectations.Add(expectation);
        }

        protected async Task SubmitRequests(int aspNetCorePort, string[] paths)
        {
            foreach (var path in paths)
            {
                await SubmitRequest(aspNetCorePort, path);
            }
        }

        protected async Task<HttpStatusCode> SubmitRequest(int aspNetCorePort, string path)
        {
            HttpResponseMessage response = await HttpClient.GetAsync($"http://localhost:{aspNetCorePort}{path}");
            string responseText = await response.Content.ReadAsStringAsync();
            Output.WriteLine($"[http] {response.StatusCode} {responseText}");
            return response.StatusCode;
        }

        private bool IsNotServerLifeCheck(MockTracerAgent.Span span)
        {
            var url = SpanExpectation.GetTag(span, Tags.HttpUrl);
            if (url == null)
            {
                return true;
            }

            return !url.Contains("alive-check");
        }
    }
}
