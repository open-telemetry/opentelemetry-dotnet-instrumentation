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
using System.IO;
using System.Linq;
using System.Net;
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
    private static readonly List<WebServerSpanExpectation> _expectations;
    private static int _expectedGraphQLExecuteSpanCount;

    static GraphQLTests()
    {
        _requests = new List<RequestInfo>(0);
        _expectations = new List<WebServerSpanExpectation>();
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

    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task SubmitsTraces()
    {
        SetEnvironmentVariable("OTEL_SERVICE_NAME", ServiceName);

        int aspNetCorePort = TcpPortProvider.GetOpenPort();
        using var agent = new MockZipkinCollector(Output);
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

        SubmitRequests(aspNetCorePort);

        var spans = (await agent.WaitForSpansAsync(_expectedGraphQLExecuteSpanCount, instrumentationLibrary: Library, returnAllOperations: false))
            .GroupBy(s => s.SpanId)
            .Select(grp => grp.First())
            .OrderBy(s => s.Start)
            .ToList();

        if (!process.HasExited)
        {
            process.Kill();
        }

        SpanTestHelpers.AssertExpectationsMet(_expectations, spans);
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
        _requests.Add(new RequestInfo()
        {
            Url = url,
            HttpMethod = httpMethod,
            RequestBody = graphQLRequestBody,
        });

        if (failsValidation) { return; }

        // Expect an 'execute' span
        _expectations.Add(new GraphQLSpanExpectation(ServiceName, ServiceVersion, operationName)
        {
            OriginalUri = url,
            GraphQLRequestBody = graphQLRequestBody,
            GraphQLOperationType = graphQLOperationType,
            GraphQLOperationName = graphQLOperationName,
            GraphQLDocument = graphQLDocument,
            IsGraphQLError = failsExecution
        });
        _expectedGraphQLExecuteSpanCount++;

        if (failsExecution) { return; }
    }

    private void SubmitRequests(int aspNetCorePort)
    {
        foreach (RequestInfo requestInfo in _requests)
        {
            SubmitRequest(aspNetCorePort, requestInfo);
        }
    }

    private HttpStatusCode SubmitRequest(int aspNetCorePort, RequestInfo requestInfo, bool printResponseText = true)
    {
        try
        {
#pragma warning disable SYSLIB0014 // suppress error SYSLIB0014: 'WebRequest.Create(string)' is obsolete: 'WebRequest, HttpWebRequest, ServicePoint, and WebClient are obsolete. Use HttpClient instead
            var request = WebRequest.Create($"http://localhost:{aspNetCorePort}{requestInfo.Url}");
#pragma warning restore SYSLIB0014

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
