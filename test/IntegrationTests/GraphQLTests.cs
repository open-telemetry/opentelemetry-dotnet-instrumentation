// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET

using System.Globalization;
using System.Net;
using System.Text;
using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Trace.V1;
using Xunit.Abstractions;

namespace IntegrationTests;

public class GraphQLTests : TestHelper
{
    private static readonly HttpClient HttpClient = new();

    public GraphQLTests(ITestOutputHelper output)
    : base("GraphQL", output)
    {
    }

    public static TheoryData<string, bool> GetData()
    {
        var theoryData = new TheoryData<string, bool>();

        foreach (var version in LibraryVersion.GraphQL)
        {
            theoryData.Add(version, true);
            theoryData.Add(version, false);
        }

        return theoryData;
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [MemberData(nameof(GetData))]
    public async Task SubmitsTraces(string packageVersion, bool setDocument)
    {
        var requests = new List<RequestInfo>();
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        // SUCCESS: query using GET
        Request(requests, id: 1, method: "GET", url: "/graphql?query=" + WebUtility.UrlEncode("query{hero{name appearsIn}}"));
        Expect(collector, id: 1, spanName: "query", setDocument: setDocument);

        // SUCCESS: query using POST (default)
        Request(requests, id: 2, body: @"{""query"":""query HeroQuery{hero{name appearsIn}}"",""operationName"": ""HeroQuery""}");
        Expect(collector, id: 2, spanName: "query HeroQuery", setDocument: setDocument);

        // SUCCESS: mutation
        Request(requests, id: 3, body: @"{""query"":""mutation AddBobaFett($human:HumanInput!){createHuman(human: $human){id name}}"",""variables"":{""human"":{""name"": ""Boba Fett""}}}");
        Expect(collector, id: 3, spanName: "mutation AddBobaFett", setDocument: setDocument);

        // SUCCESS: subscription
        Request(requests, id: 4, body: @"{ ""query"":""subscription HumanAddedSub{humanAdded{name}}""}");
        Expect(collector, id: 4, spanName: "subscription HumanAddedSub", setDocument: setDocument);

        // TODO: re-enable if exceptions are supported again.
        // FAILURE: query fails 'execute' step
        // Request(requests, id: 5, body: @"{""query"":""subscription NotImplementedSub{throwNotImplementedException{name}}""}");
        // Expect(collector, id: 5, spanName: "subscription NotImplementedSub", setDocument: setDocument, verifyFailure: VerifyNotImplementedException);

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_GRAPHQL_SET_DOCUMENT", setDocument.ToString());
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_INSTRUMENTATION_ENABLED", "false");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_METRICS_INSTRUMENTATION_ENABLED", "false"); // disable metrics to disable side effect of AspNetCore - working propagation on .NET
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_GRAPHQL_INSTRUMENTATION_ENABLED", "true");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ASPNETCORE_INSTRUMENTATION_ENABLED", "true"); // AspNetCore Instrumentation enables propagation used in this test
        SetEnvironmentVariable("OTEL_TRACES_SAMPLER", "always_on");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_NETFX_REDIRECT_ENABLED", "false");

        int aspNetCorePort = TcpPortProvider.GetOpenPort();
        SetEnvironmentVariable("ASPNETCORE_URLS", $"http://127.0.0.1:{aspNetCorePort}/");
        using var process = StartTestApplication(new TestSettings { PackageVersion = packageVersion });
        using var helper = new ProcessHelper(process);
        try
        {
            await HealthzHelper.TestAsync($"http://localhost:{aspNetCorePort}/alive-check", Output);
            await SubmitRequestsAsync(aspNetCorePort, requests);

            collector.AssertExpectations();
        }
        finally
        {
            if (process != null && !process.HasExited)
            {
                process.Kill();
                await process.WaitForExitAsync();
                Output.WriteLine("Exit Code: " + process.ExitCode);
            }

            Output.WriteResult(helper);
        }
    }

    private static void Request(List<RequestInfo> requests, byte id, string method = "POST", string url = "/graphql", string? body = null)
    {
        requests.Add(new RequestInfo
        {
            Id = GetTraceIdHex(id),
            Url = url,
            HttpMethod = method,
            RequestBody = body
        });
    }

    private static byte[] GetTraceIdBytes(byte id)
    {
        var traceId = new byte[16];
        traceId[^1] = id;

        return traceId;
    }

    private static string GetTraceIdHex(byte id)
    {
        return id.ToString("x32", CultureInfo.InvariantCulture);
    }

    private void Expect(
        MockSpansCollector collector,
        string spanName,
        byte id,
        bool setDocument)
    {
        var traceIdBytes = GetTraceIdBytes(id);

        bool Predicate(Span span)
        {
            var traceId = span.TraceId.ToByteArray();
            if (!traceId.SequenceEqual(traceIdBytes))
            {
                return false;
            }

            if (setDocument && !span.Attributes.Any(attr => attr.Key == "graphql.document" && !string.IsNullOrWhiteSpace(attr.Value?.StringValue)))
            {
                return false;
            }

            if (!setDocument && span.Attributes.Any(attr => attr.Key == "graphql.document"))
            {
                return false;
            }

            return true;
        }

        collector.Expect("GraphQL", Predicate, spanName);
    }

    private async Task SubmitRequestsAsync(int aspNetCorePort, IEnumerable<RequestInfo> requests)
    {
        foreach (var requestInfo in requests)
        {
            await SubmitRequestAsync(HttpClient, aspNetCorePort, requestInfo).ConfigureAwait(false);
        }
    }

    private async Task SubmitRequestAsync(HttpClient client, int aspNetCorePort, RequestInfo requestInfo, bool printResponseText = true)
    {
        try
        {
            var url = $"http://localhost:{aspNetCorePort}{requestInfo.Url}";
            var method = requestInfo.HttpMethod;
            var w3c = $"00-{requestInfo.Id}-0000000000000001-01";

            HttpResponseMessage response;

            if (method == "GET")
            {
                using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                requestMessage.Headers.Add("traceparent", w3c);

                response = await client.SendAsync(requestMessage).ConfigureAwait(false);
            }
            else if (method == "POST")
            {
                if (requestInfo.RequestBody == null)
                {
                    throw new NotSupportedException("RequestBody cannot be null when you are using POST method");
                }

                using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
                requestMessage.Content = new StringContent(requestInfo.RequestBody, Encoding.UTF8, "application/json");
                requestMessage.Headers.Add("traceparent", w3c);

                response = await client.SendAsync(requestMessage).ConfigureAwait(false);
            }
            else
            {
                // If additional logic is needed, implement it here.
                throw new NotImplementedException($"{method} is not supported.");
            }

            if (printResponseText)
            {
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                Output.WriteLine($"[http] {response.StatusCode} {content}");
            }
        }
        catch (HttpRequestException ex)
        {
            Output.WriteLine($"[http] exception: {ex}");
        }
    }

    private sealed class RequestInfo
    {
        public string? Url { get; set; }

        public string? HttpMethod { get; set; }

        public string? RequestBody { get; set; }

        public string? Id { get; set; }
    }
}

#endif
