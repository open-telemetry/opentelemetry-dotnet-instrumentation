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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Trace.V1;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests;

public class GraphQLTests : TestHelper
{
    public GraphQLTests(ITestOutputHelper output)
        : base("GraphQL", output)
    {
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Trait("Category", "EndToEnd")]
    public async Task SubmitsTraces(bool setDocument)
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

        // FAILURE: query fails 'execute' step
        Request(requests, body: @"{""query"":""subscription NotImplementedSub{throwNotImplementedException{name}}""}");
        Expect(collector, spanName: "subscription NotImplementedSub", graphQLOperationType: "subscription", graphQLOperationName: "NotImplementedSub", graphQLDocument: "subscription NotImplementedSub{throwNotImplementedException{name}}", setDocument: setDocument, verifyFailure: VerifyNotImplementedException);

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_GRAPHQL_SET_DOCUMENT", setDocument.ToString());
        int aspNetCorePort = TcpPortProvider.GetOpenPort();
        SetEnvironmentVariable("ASPNETCORE_URLS", $"http://127.0.0.1:{aspNetCorePort}/");
        EnableBytecodeInstrumentation();
        using var process = StartTestApplication();
        using var helper = new ProcessHelper(process);
        try
        {
            await HealthzHelper.TestAsync($"http://localhost:{aspNetCorePort}/alive-check", Output);
            await SubmitRequestsAsync(aspNetCorePort, requests);

            collector.AssertExpectations();
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill();
                process.WaitForExit();
            }

            Output.WriteLine("Exit Code: " + process.ExitCode);
            Output.WriteResult(helper);
        }
    }

    private static void Request(List<RequestInfo> requests, string method = "POST", string url = "/graphql", string body = null)
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

    private static void Expect(
        MockSpansCollector collector,
        string spanName,
        string graphQLOperationType,
        string graphQLOperationName,
        string graphQLDocument,
        bool setDocument,
        Predicate<Span> verifyFailure = null)
    {
        bool Predicate(Span span)
        {
            if (span.Kind != Span.Types.SpanKind.Server)
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

            return true;
        }

        collector.Expect("OpenTelemetry.AutoInstrumentation.GraphQL", Predicate, spanName);
    }

    private async Task SubmitRequestsAsync(int aspNetCorePort, IEnumerable<RequestInfo> requests)
    {
        var client = new HttpClient();
        foreach (RequestInfo requestInfo in requests)
        {
            await SubmitRequestAsync(client, aspNetCorePort, requestInfo);
        }
    }

    private async Task<HttpStatusCode> SubmitRequestAsync(HttpClient client, int aspNetCorePort, RequestInfo requestInfo, bool printResponseText = true)
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
                response = await client.PostAsync(url, new StringContent(requestInfo.RequestBody, Encoding.UTF8, "application/json"));
            }
            else
            {
                // If additional logic is needed, implement it here.
                throw new NotImplementedException($"{method} is not supported.");
            }

            if (printResponseText)
            {
                string content = await response.Content.ReadAsStringAsync();

                Output.WriteLine($"[http] {response.StatusCode} {content}");
            }

            return response.StatusCode;
        }
        catch (HttpRequestException ex)
        {
            Output.WriteLine($"[http] exception: {ex}");

#if NET6_0_OR_GREATER
            return ex.StatusCode.Value;
#else
            return HttpStatusCode.BadRequest;
#endif
        }
    }

    private class RequestInfo
    {
        public string Url { get; set; }

        public string HttpMethod { get; set; }

        public string RequestBody { get; set; }
    }
}
