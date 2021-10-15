using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using IntegrationTests.Helpers;
using IntegrationTests.Helpers.Mocks;
using IntegrationTests.Helpers.Models;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests.StartupHook
{
    public class StartupHookTests : TestHelper
    {
        private List<WebServerSpanExpectation> _expectations = new List<WebServerSpanExpectation>();

        public StartupHookTests(ITestOutputHelper output)
            : base("StartupHook", output)
        {
            SetServiceVersion("1.0.0");
            _expectations.Add(new WebServerSpanExpectation("Samples.StartupHook", null, "/alive-check", "/alive-check", null, "GET"));
            _expectations.Add(new WebServerSpanExpectation("Samples.StartupHook", null, "/home/index", "/home/index", null, "GET"));
            _expectations.Add(new WebServerSpanExpectation("Samples.StartupHook", null, "HTTP GET", "HTTP GET", null, "GET"));
        }

        [Theory]
        [Trait("Category", "EndToEnd")]
        [InlineData(false)]
        [InlineData(true)]
        public void SubmitsTraces(bool enableCallTarget)
        {
            SetCallTargetSettings(enableCallTarget);

            int agentPort = TcpPortProvider.GetOpenPort();
            int aspNetCorePort = TcpPortProvider.GetOpenPort();

            using (var agent = new MockZipkinCollector(Output, agentPort))
            using (Process process = StartSample(agent.Port, arguments: null, packageVersion: string.Empty, aspNetCorePort: aspNetCorePort, startupHook: true))
            {
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
                    var aliveCheckRequest = new RequestInfo() { HttpMethod = "GET", Url = "/alive-check" };
                    try
                    {
                        serverReady = SubmitRequest(aspNetCorePort, aliveCheckRequest, false) == HttpStatusCode.OK;
                    }
                    catch
                    {
                        // ignore
                    }

                    if (serverReady)
                    {
                        Output.WriteLine("The server is ready.");
                        break;
                    }

                    Thread.Sleep(intervalMilliseconds);
                }

                if (!serverReady)
                {
                    throw new Exception("Couldn't verify the application is ready to receive requests.");
                }

                var testStart = DateTime.Now;

                var requestUrl = new RequestInfo() { HttpMethod = "GET", Url = "/home/index" };
                SubmitRequest(aspNetCorePort, requestUrl, false);

                var span = agent.WaitForSpans(3);

                if (!process.HasExited)
                {
                    process.Kill();
                }

                Assert.True(span.Count() == 3, $"Expecting 3 spans, received {span.Count()}");
                Assert.Single(span.Select(s => s.Service).Distinct());

                var spanList = span.ToList();
                this.AssertExpectationsMet(_expectations, spanList);
            }
        }

        private void AssertExpectationsMet(List<WebServerSpanExpectation> expectations, List<IMockSpan> spans)
        {
            List<IMockSpan> remainingSpans = spans.Select(s => s).ToList();
            List<string> failures = new List<string>();

            foreach (SpanExpectation expectation in expectations)
            {
                List<IMockSpan> possibleSpans =
                    remainingSpans
                       .Where(s => expectation.Matches(s))
                       .ToList();

                if (possibleSpans.Count == 0)
                {
                    failures.Add($"No spans for: {expectation}");
                    continue;
                }
            }

            string finalMessage = Environment.NewLine + string.Join(Environment.NewLine, failures.Select(f => " - " + f));

            Assert.True(!failures.Any(), finalMessage);
        }

        private HttpStatusCode SubmitRequest(int aspNetCorePort, RequestInfo requestInfo, bool printResponseText = true)
        {
            try
            {
                var request = WebRequest.Create($"http://localhost:{aspNetCorePort}{requestInfo.Url}");
                request.Method = requestInfo.HttpMethod;

                if (requestInfo.RequestBody != null)
                {
                    byte[] requestBytes = System.Text.Encoding.UTF8.GetBytes(requestInfo.RequestBody);

                    request.ContentType = "application/json";
                    request.ContentLength = requestBytes.Length;

                    using (var dataStream = request.GetRequestStream())
                    {
                        dataStream.Write(requestBytes, 0, requestBytes.Length);
                    }
                }

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var stream = response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    string responseText;
                    try
                    {
                        responseText = reader.ReadToEnd();
                    }
                    catch (Exception ex)
                    {
                        responseText = "ENCOUNTERED AN ERROR WHEN READING RESPONSE.";
                        Output.WriteLine(ex.ToString());
                    }

                    if (printResponseText)
                    {
                        Output.WriteLine($"[http] {response.StatusCode} {responseText}");
                    }

                    return response.StatusCode;
                }
            }
            catch (WebException wex)
            {
                Output.WriteLine($"[http] exception: {wex}");
                if (wex.Response is HttpWebResponse response)
                {
                    using (var stream = response.GetResponseStream())
                    using (var reader = new StreamReader(stream))
                    {
                        Output.WriteLine($"[http] {response.StatusCode} {reader.ReadToEnd()}");
                    }

                    return response.StatusCode;
                }
            }

            return HttpStatusCode.BadRequest;
        }

        private class RequestInfo
        {
            public string Url { get; set; }

            public string HttpMethod { get; set; }

            public string RequestBody { get; set; }
        }
    }
}
