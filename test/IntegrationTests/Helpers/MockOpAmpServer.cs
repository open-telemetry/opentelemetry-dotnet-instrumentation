// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET
using System.Buffers;
using Microsoft.AspNetCore.Http;
#endif

using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Globalization;
using System.Net;
using System.Text;
using Google.Protobuf;
using OpAmp.Proto.V1;
using Xunit.Abstractions;

namespace IntegrationTests.Helpers;

internal sealed class MockOpAmpServer : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly TestHttpServer _listener;

    private readonly List<Expectation> _expectations = new();
    private readonly BlockingCollection<AgentToServer> _frames = new(10); // bounded to avoid memory leak
    private readonly List<NameValueCollection> _receivedHeaders = [];

    public MockOpAmpServer(ITestOutputHelper output, string host = "localhost")
    {
        _output = output;
#if NETFRAMEWORK
        _listener = new TestHttpServer(output, HandleHttpRequests, host, "/v1/opamp/");
#else
        _listener = new TestHttpServer(output, nameof(MockOpAmpServer), new PathHandler(HandleHttpRequests, "/v1/opamp"));
#endif
    }

    /// <summary>
    /// Gets the TCP port that this collector is listening on.
    /// </summary>
    public int Port { get => _listener.Port; }

    public void Expect(Func<AgentToServer, bool>? predicate = null, string? description = null)
    {
        predicate ??= x => true;

        _expectations.Add(new Expectation(predicate, description));
    }

    public void AssertExpectations(TimeSpan? timeout = null)
    {
        if (_expectations.Count == 0)
        {
            throw new InvalidOperationException("Expectations were not set");
        }

        var missingExpectations = new List<Expectation>(_expectations);
        var expectationsMet = new List<AgentToServer>();
        var additionalEntries = new List<AgentToServer>();

        timeout ??= TestTimeout.Expectation;
        using var cts = new CancellationTokenSource();

        try
        {
            cts.CancelAfter(timeout.Value);
            foreach (var frame in _frames.GetConsumingEnumerable(cts.Token))
            {
                var found = false;
                for (var i = missingExpectations.Count - 1; i >= 0; i--)
                {
                    var missingExpectation = missingExpectations[i];

                    if (!missingExpectation.Predicate(frame))
                    {
                        continue;
                    }

                    expectationsMet.Add(frame);
                    missingExpectations.RemoveAt(i);
                    found = true;
                    break;
                }

                if (!found)
                {
                    additionalEntries.Add(frame);
                    continue;
                }

                if (missingExpectations.Count == 0)
                {
                    return;
                }
            }
        }
        catch (ArgumentOutOfRangeException)
        {
            // CancelAfter called with non-positive value
            FailExpectations(missingExpectations, expectationsMet, additionalEntries);
        }
        catch (OperationCanceledException)
        {
            // timeout
            FailExpectations(missingExpectations, expectationsMet, additionalEntries);
        }
    }

    public void AssertEmpty(TimeSpan? timeout = null)
    {
        timeout ??= TestTimeout.NoExpectation;
        if (_frames.TryTake(out var resourceSpan, timeout.Value))
        {
            Assert.Fail($"Expected nothing, but got: {resourceSpan}");
        }
    }

    public void Dispose()
    {
        WriteOutput("Shutting down.");
        _listener?.Dispose();
        _frames.Dispose();
    }

    private static void FailExpectations(
        List<Expectation> missingExpectations,
        List<AgentToServer> expectationsMet,
        List<AgentToServer> additionalEntries)
    {
        var message = new StringBuilder();
        message.AppendLine();

        message.AppendLine("Missing expectations:");
        foreach (var logline in missingExpectations)
        {
            message.AppendLine(CultureInfo.InvariantCulture, $"  - \"{logline.Description}\"");
        }

        message.AppendLine("Entries meeting expectations:");
        foreach (var logline in expectationsMet)
        {
            message.AppendLine(CultureInfo.InvariantCulture, $"    \"{logline}\"");
        }

        message.AppendLine("Additional entries:");
        foreach (var logline in additionalEntries)
        {
            message.AppendLine(CultureInfo.InvariantCulture, $"  + \"{logline}\"");
        }

        Assert.Fail(message.ToString());
    }

    private static byte[] GenerateResponse(AgentToServer frame)
    {
        var content = "This is a mock server frame for testing purposes.";
        var responseFrame = new ServerToAgent
        {
            InstanceUid = frame.InstanceUid,
            CustomMessage = new CustomMessage()
            {
                Data = ByteString.CopyFromUtf8(content),
                Type = "Utf8String",
            },
        };

        return responseFrame.ToByteArray();
    }

#if NETFRAMEWORK
    private void HandleHttpRequests(HttpListenerContext ctx)
    {
        var frame = AgentToServer.Parser.ParseFrom(ctx.Request.InputStream);
        _frames.Add(frame);

        var headersCopy = new NameValueCollection();
        foreach (var key in ctx.Request.Headers.AllKeys)
        {
            headersCopy.Add(key, ctx.Request.Headers[key]);
        }

        _receivedHeaders.Add(headersCopy);

        var response = GenerateResponse(frame);

        ctx.Response.StatusCode = (int)HttpStatusCode.OK;
        ctx.Response.ContentType = "application/x-protobuf";

        ctx.Response.OutputStream.Write(response, 0, response.Length);
        ctx.Response.OutputStream.Close();
    }
#else
    private static async Task<AgentToServer?> ProcessReceiveAsync(HttpRequest request)
    {
        var reader = request.BodyReader;
        var messageBuffer = new ArrayBufferWriter<byte>();

        while (true)
        {
            var result = await reader.ReadAsync().ConfigureAwait(false);
            var buffer = result.Buffer;

            if (result.IsCanceled)
            {
                reader.AdvanceTo(buffer.End);
                return null;
            }

            foreach (var segment in buffer)
            {
                messageBuffer.Write(segment.Span);
            }

            reader.AdvanceTo(buffer.End);

            if (result.IsCompleted)
            {
                break;
            }
        }

        var frame = AgentToServer.Parser.ParseFrom(messageBuffer.WrittenSpan);

        return frame;
    }

    private async Task HandleHttpRequests(HttpContext ctx)
    {
        var frame = await ProcessReceiveAsync(ctx.Request).ConfigureAwait(false);
        if (frame == null)
        {
            // No suitable frame found.
            return;
        }

        _frames.Add(frame);

        var headersCopy = new NameValueCollection();
        foreach (var key in ctx.Request.Headers.Keys)
        {
            headersCopy.Add(key, ctx.Request.Headers[key]);
        }

        _receivedHeaders.Add(headersCopy);

        var response = GenerateResponse(frame);

        ctx.Response.StatusCode = (int)HttpStatusCode.OK;
        ctx.Response.ContentType = "application/x-protobuf";

        await ctx.Response.Body.WriteAsync(response).ConfigureAwait(false);
        await ctx.Response.CompleteAsync().ConfigureAwait(false);
    }
#endif

    private void WriteOutput(string msg)
    {
        const string name = nameof(MockOpAmpServer);
        _output.WriteLine($"[{name}]: {msg}");
    }

    private sealed class Expectation
    {
        public Expectation(Func<AgentToServer, bool> predicate, string? description)
        {
            Predicate = predicate;
            Description = description;
        }

        public Func<AgentToServer, bool> Predicate { get; }

        public string? Description { get; }
    }
}
