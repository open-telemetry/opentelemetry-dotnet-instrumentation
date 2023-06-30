// <copyright file="GraphQLTests.cs" company="OpenTelemetry Authors">
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

#if NET6_0_OR_GREATER

using System.Net;
using System.Text;
using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Trace.V1;
using Xunit.Abstractions;

namespace IntegrationTests;

public class GraphQLTests : TestHelper
{
    public GraphQLTests(ITestOutputHelper output)
    : base("GraphQL", output)
    {
    }

    public static IEnumerable<object[]> GetData()
        => from packageVersionArray in LibraryVersion.GraphQL
           from setDocument in new[] { true, false }
           select new[] { packageVersionArray[0], setDocument };

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
        Request(requests, id: 5, body: @"{ ""query"":""subscription HumanAddedSub{humanAdded{name}}""}");
        Expect(collector, id: 5, spanName: "subscription HumanAddedSub", setDocument: setDocument);

        // TODO: re-enable if exceptions are supported again.
        // FAILURE: query fails 'execute' step
        // Request(requests, id: 6, body: @"{""query"":""subscription NotImplementedSub{throwNotImplementedException{name}}""}");
        // Expect(collector, id: 6, spanName: "subscription NotImplementedSub", setDocument: setDocument, verifyFailure: VerifyNotImplementedException);

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_GRAPHQL_SET_DOCUMENT", setDocument.ToString());
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_INSTRUMENTATION_ENABLED", "false");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_GRAPHQL_INSTRUMENTATION_ENABLED", "true");
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
                process.WaitForExit();
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
        traceId[traceId.Length - 1] = id;

        return traceId;
    }

    private static string GetTraceIdHex(byte id)
    {
        return BitConverter.ToString(GetTraceIdBytes(id)).Replace("-", string.Empty);
    }

    private static bool VerifyNotImplementedException(Span span)
    {
        var exceptionEvent = span.Events.SingleOrDefault();

        if (exceptionEvent == null)
        {
            return false;
        }

        return
            exceptionEvent.Attributes.Any(x => x.Key == "exception.type" && x.Value?.StringValue == "System.NotImplementedException") &&
            exceptionEvent.Attributes.Any(x => x.Key == "exception.message") &&
            exceptionEvent.Attributes.Any(x => x.Key == "exception.stacktrace");
    }

    private void Expect(
        MockSpansCollector collector,
        string spanName,
        byte id,
        bool setDocument,
        Predicate<Span>? verifyFailure = null)
    {
        var traceIdBytes = GetTraceIdBytes(id);

        bool Predicate(Span span)
        {
            var traceId = span.TraceId.ToByteArray();
            if (!traceId.SequenceEqual(traceIdBytes))
            {
                return false;
            }

            if (verifyFailure != null)
            {
                return verifyFailure(span);
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
        var client = new HttpClient();
        foreach (var requestInfo in requests)
        {
            await SubmitRequestAsync(client, aspNetCorePort, requestInfo);
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

                response = await client.SendAsync(requestMessage);
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

                response = await client.SendAsync(requestMessage);
            }
            else
            {
                // If additional logic is needed, implement it here.
                throw new NotImplementedException($"{method} is not supported.");
            }

            if (printResponseText)
            {
                var content = await response.Content.ReadAsStringAsync();

                Output.WriteLine($"[http] {response.StatusCode} {content}");
            }
        }
        catch (HttpRequestException ex)
        {
            Output.WriteLine($"[http] exception: {ex}");
        }
    }

    private class RequestInfo
    {
        public string? Url { get; set; }

        public string? HttpMethod { get; set; }

        public string? RequestBody { get; set; }

        public string? Id { get; set; }
    }
}

#endif
