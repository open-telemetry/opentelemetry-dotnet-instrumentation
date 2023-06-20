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

using System.Net;
using System.Text;
using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Trace.V1;
using Xunit.Abstractions;

namespace IntegrationTests;

public abstract class GraphQLTests : TestHelper
{
    public GraphQLTests(string testApplicationName, ITestOutputHelper output)
    : base(testApplicationName, output)
    {
    }

    public abstract bool SupportsExceptions { get; }

    public abstract string InstrumentationScope { get; }

    public static IEnumerable<object[]> GetData(bool isNative)
        => from packageVersionArray in (isNative ? LibraryVersion.GraphQLNativeSupport : LibraryVersion.GraphQL)
           from setDocument in new[] { true, false }
           select new[] { packageVersionArray[0], setDocument };

    public async Task SubmitsTracesBase(string packageVersion, bool setDocument)
    {
        var requests = new List<RequestInfo>();
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        // SUCCESS: query using GET
        Request(requests, method: "GET", url: "/graphql?query=" + WebUtility.UrlEncode("query{hero{name appearsIn}}"));
        Expect(collector, spanName: "query", graphQLOperationType: "query", graphQLOperationName: null, graphQLDocument: "query{hero{name appearsIn}}", setDocument: setDocument);

        // SUCCESS: query using POST (default)
        Request(requests, body: @"{""query"":""query HeroQuery{hero{name appearsIn}}"",""operationName"": ""HeroQuery""}");
        Expect(collector, spanName: "query HeroQuery", graphQLOperationType: "query", graphQLOperationName: "HeroQuery", graphQLDocument: "query HeroQuery{hero{name appearsIn}}", setDocument: setDocument);

        // SUCCESS: mutation
        Request(requests, body: @"{""query"":""mutation AddBobaFett($human:HumanInput!){createHuman(human: $human){id name}}"",""variables"":{""human"":{""name"": ""Boba Fett""}}}");
        Expect(collector, spanName: "mutation AddBobaFett", graphQLOperationType: "mutation", graphQLOperationName: "AddBobaFett", graphQLDocument: "mutation AddBobaFett($human:HumanInput!){createHuman(human: $human){id name}}", setDocument: setDocument);

        // SUCCESS: subscription
        Request(requests, body: @"{ ""query"":""subscription HumanAddedSub{humanAdded{name}}""}");
        Expect(collector, spanName: "subscription HumanAddedSub", graphQLOperationType: "subscription", graphQLOperationName: "HumanAddedSub", graphQLDocument: "subscription HumanAddedSub{humanAdded{name}}", setDocument: setDocument);

        if (SupportsExceptions)
        {
            // FAILURE: query fails 'execute' step
            Request(requests, body: @"{""query"":""subscription NotImplementedSub{throwNotImplementedException{name}}""}");
            Expect(collector, spanName: "subscription NotImplementedSub", graphQLOperationType: "subscription", graphQLOperationName: "NotImplementedSub", graphQLDocument: "subscription NotImplementedSub{throwNotImplementedException{name}}", setDocument: setDocument, verifyFailure: VerifyNotImplementedException);
        }

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_GRAPHQL_SET_DOCUMENT", setDocument.ToString());
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_INSTRUMENTATION_ENABLED", "false");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_GRAPHQL_INSTRUMENTATION_ENABLED", "true");
        SetEnvironmentVariable("OTEL_TRACES_SAMPLER", "always_on");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_NETFX_REDIRECT_ENABLED", "false");

        int aspNetCorePort = TcpPortProvider.GetOpenPort();
        SetEnvironmentVariable("ASPNETCORE_URLS", $"http://127.0.0.1:{aspNetCorePort}/");
        EnableBytecodeInstrumentation();
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

    private static void Request(List<RequestInfo> requests, string method = "POST", string url = "/graphql", string? body = null)
    {
        requests.Add(new RequestInfo
        {
            Url = url,
            HttpMethod = method,
            RequestBody = body
        });
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
        string graphQLOperationType,
        string? graphQLOperationName,
        string graphQLDocument,
        bool setDocument,
        Predicate<Span>? verifyFailure = null)
    {
        bool Predicate(Span span)
        {
#if NETFRAMEWORK
            // There is no parent Span. There is no parent Activity on .NET Fx
            if (span.Kind != Span.Types.SpanKind.Server)
#else
            // AspNetCore instrumentation always creates parent Activity. The activity is not recorded if instrumentation is disabled.
            if (span.Kind != Span.Types.SpanKind.Internal)
#endif
            {
                return false;
            }

            if (span.Name != spanName)
            {
                return false;
            }

            if (verifyFailure != null)
            {
                return verifyFailure(span);
            }

            if (!span.Attributes.Any(attr => attr.Key == "graphql.operation.type" && attr.Value?.StringValue == graphQLOperationType))
            {
                return false;
            }

            if (graphQLOperationName != null && !span.Attributes.Any(attr => attr.Key == "graphql.operation.name" && attr.Value?.StringValue == graphQLOperationName))
            {
                return false;
            }

            if (setDocument && !span.Attributes.Any(attr => attr.Key == "graphql.document" && attr.Value?.StringValue == graphQLDocument))
            {
                return false;
            }

            if (!setDocument && span.Attributes.Any(attr => attr.Key == "graphql.document"))
            {
                return false;
            }

            return true;
        }

        collector.Expect(InstrumentationScope, Predicate, spanName);
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

            HttpResponseMessage response;

            if (method == "GET")
            {
                response = await client.GetAsync(url);
            }
            else if (method == "POST")
            {
                if (requestInfo.RequestBody == null)
                {
                    throw new NotSupportedException("RequestBody cannot be null when you are using POST method");
                }

                response = await client.PostAsync(url, new StringContent(requestInfo.RequestBody, Encoding.UTF8, "application/json"));
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
    }
}
