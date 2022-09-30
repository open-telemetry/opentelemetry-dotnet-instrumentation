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
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IntegrationTests.Helpers;
using IntegrationTests.Helpers.Models;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests;

public class GraphQLTests : TestHelper
{
    private const string ServiceName = "TestApplication.GraphQL";
    private const string ServiceVersion = OpenTelemetry.AutoInstrumentation.Constants.Tracer.Version;
    private const string Library = "OpenTelemetry.AutoInstrumentation.GraphQL";

    private static readonly List<RequestInfo> _requests;
    private static readonly List<SpanExpectation> _expectationsAll; // Full expectations
    private static readonly List<SpanExpectation> _expectationsSafe; // PII protected expectations
    private static int _expectedGraphQLExecuteSpanCount;

    static GraphQLTests()
    {
        _requests = new List<RequestInfo>(0);
        _expectationsAll = new List<SpanExpectation>();
        _expectationsSafe = new List<SpanExpectation>();
        _expectedGraphQLExecuteSpanCount = 0;

        // SUCCESS: query using GET
        CreateGraphQLRequestsAndExpectations(url: "/graphql?query=" + WebUtility.UrlEncode("query{hero{name appearsIn}}"), httpMethod: "GET", operationName: "query", graphQLRequestBody: null, graphQLOperationType: "query", graphQLOperationName: null, graphQLDocument: "query{hero{name appearsIn} }");

        // SUCCESS: query using POST (default)
        CreateGraphQLRequestsAndExpectations(url: "/graphql", httpMethod: "POST", operationName: "query HeroQuery", graphQLRequestBody: @"{""query"":""query HeroQuery{hero {name appearsIn}}"",""operationName"": ""HeroQuery""}", graphQLOperationType: "query", graphQLOperationName: "HeroQuery", graphQLDocument: "query HeroQuery{hero{name appearsIn}}");

        // SUCCESS: mutation
        CreateGraphQLRequestsAndExpectations(url: "/graphql", httpMethod: "POST", operationName: "mutation AddBobaFett", graphQLRequestBody: @"{""query"":""mutation AddBobaFett($human:HumanInput!){createHuman(human: $human){id name}}"",""variables"":{""human"":{""name"": ""Boba Fett""}}}", graphQLOperationType: "mutation", graphQLOperationName: "AddBobaFett", graphQLDocument: "mutation AddBobaFett($human:HumanInput!){createHuman(human: $human){id name}}");

        // SUCCESS: subscription
        CreateGraphQLRequestsAndExpectations(url: "/graphql", httpMethod: "POST", operationName: "subscription HumanAddedSub", graphQLRequestBody: @"{ ""query"":""subscription HumanAddedSub{humanAdded{name}}""}", graphQLOperationType: "subscription", graphQLOperationName: "HumanAddedSub", graphQLDocument: "subscription HumanAddedSub{humanAdded{name}}");

        // FAILURE: query fails 'validate' step
        CreateGraphQLRequestsAndExpectations(url: "/graphql", httpMethod: "POST", operationName: "query HumanError", graphQLRequestBody: @"{""query"":""query HumanError{human(id:1){name apearsIn}}""}", graphQLOperationType: "query", graphQLOperationName: null, failsValidation: true, graphQLDocument: "query HumanError{human(id:1){name apearsIn}}");

        // FAILURE: query fails 'execute' step
        CreateGraphQLRequestsAndExpectations(url: "/graphql", httpMethod: "POST", operationName: "subscription NotImplementedSub", graphQLRequestBody: @"{""query"":""subscription NotImplementedSub{throwNotImplementedException{name}}""}", graphQLOperationType: "subscription", graphQLOperationName: "NotImplementedSub", graphQLDocument: "subscription NotImplementedSub{throwNotImplementedException{name}}", failsExecution: true);
    }

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
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_CONSOLE_EXPORTER_ENABLED", "true");
        SetEnvironmentVariable("OTEL_SERVICE_NAME", ServiceName);
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_GRAPHQL_SET_DOCUMENT", setDocument.ToString());

        int aspNetCorePort = TcpPortProvider.GetOpenPort();
        using var agent = await MockZipkinCollector.Start(Output);
        using var process = StartTestApplication(agent.Port, aspNetCorePort: aspNetCorePort);
        if (process.HasExited)
        {
            throw new InvalidOperationException($"Test application has exited with code: {process.ExitCode}");
        }

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
        var client = new HttpClient();

        // wait for server to be ready to receive requests
        while (intervals-- > 0)
        {
            var aliveCheckRequest = new RequestInfo { HttpMethod = "GET", Url = "/alive-check" };
            try
            {
                var responseCode = await SubmitRequestAsync(client, aspNetCorePort, aliveCheckRequest, false);

                serverReady = responseCode == HttpStatusCode.OK;
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

        await SubmitRequestsAsync(client, aspNetCorePort);

        var spans = await agent.WaitForSpansAsync(_expectedGraphQLExecuteSpanCount, instrumentationLibrary: Library);

        if (!process.HasExited)
        {
            process.Kill();
        }

        if (setDocument)
        {
            SpanTestHelpers.AssertExpectationsMet(_expectationsAll, spans);
        }
        else
        {
            SpanTestHelpers.AssertExpectationsMet(_expectationsSafe, spans);
        }
    }

    private static void CreateGraphQLRequestsAndExpectations(
        string url,
        string httpMethod,
        string operationName,
        string graphQLRequestBody,
        string graphQLOperationType,
        string graphQLOperationName,
        string graphQLDocument,
        bool failsValidation = false,
        bool failsExecution = false)
    {
        _requests.Add(new RequestInfo
        {
            Url = url,
            HttpMethod = httpMethod,
            RequestBody = graphQLRequestBody,
        });

        if (failsValidation) { return; }

        _expectationsAll.Add(new GraphQLSpanExpectation(ServiceName, ServiceVersion, operationName)
        {
            GraphQLRequestBody = graphQLRequestBody,
            GraphQLOperationType = graphQLOperationType,
            GraphQLOperationName = graphQLOperationName,
            GraphQLDocument = graphQLDocument,
            IsGraphQLError = failsExecution
        });

        _expectationsSafe.Add(new GraphQLSpanExpectation(ServiceName, ServiceVersion, operationName)
        {
            GraphQLRequestBody = graphQLRequestBody,
            GraphQLOperationType = graphQLOperationType,
            GraphQLOperationName = graphQLOperationName,
            GraphQLDocument = null,
            IsGraphQLError = failsExecution
        });

        _expectedGraphQLExecuteSpanCount++;
    }

    private async Task SubmitRequestsAsync(HttpClient client, int aspNetCorePort)
    {
        foreach (RequestInfo requestInfo in _requests)
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
