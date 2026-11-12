// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.Tracing;
using System.Globalization;
using NSubstitute;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.Tests.Util;

namespace OpenTelemetry.AutoInstrumentation.Tests.Logging;

[Collection("Non-Parallel Collection")]
public sealed class SelfDiagnosticsLoggerTests : IDisposable
{
    private const int ErrorEventId = 2;
    private const int WarningEventId = 3;
    private const int InformationEventId = 4;
    private const int VerboseEventId = 5;

    private readonly EnvironmentScope _environmentScope = new(new Dictionary<string, string?>
    {
        { "OTEL_LOG_LEVEL", null },
        { "OTEL_DOTNET_AUTO_LOGGER", "none" },
    });

    public SelfDiagnosticsLoggerTests()
    {
        OtelLogging.Reset();
    }

    public void Dispose()
    {
        _environmentScope.Dispose();
        OtelLogging.Reset();
    }

    [Fact]
    public void WhenLoggerIsCreatedAfterSdkNotification_Then_LiveMessagesReachTheListener()
    {
        using var listener = new CapturingListener();
        OtelLogging.NotifySdkAvailable();

        var logger = OtelLogging.GetLogger("OpAmp");
        logger.Information("late", true);

        Assert.Equal((InformationEventId, "[OpAmp] late"), Assert.Single(listener.Events));
    }

    [Fact]
    public void WhenLoggerIsCreatedAfterFallbackNotification_Then_LiveMessagesAreMirrored()
    {
        OtelLogging.UseFallbackIfStillBuffering();
        using var listener = new CapturingListener();

        var logger = OtelLogging.GetLogger("OpAmp");
        logger.Information("late", true);

        Assert.Equal((InformationEventId, "late"), Assert.Single(listener.Events));
    }

    [Fact]
    public void WhenCreationAndWritesRaceWithSdkNotification_Then_EachEntryIsDeliveredOnce()
    {
        using var listener = new CapturingListener();

        Parallel.Invoke(
            () => Parallel.For(0, 8, i => OtelLogging.GetLogger("OpAmp").Information($"entry {i}", true)),
            OtelLogging.NotifySdkAvailable);

        var events = listener.Events;
        Assert.Equal(8, events.Count);
        for (var i = 0; i < 8; i++)
        {
            Assert.Single(events, e => e.Message.EndsWith($"entry {i}", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void WhenBufferingAndListenerAttached_Then_OnSdkAvailableReplaysInOrderWithTimestamps()
    {
        var factory = new CountingSinkFactory();
        var logger = new SelfDiagnosticsLogger(LogLevel.Debug, factory.Create, string.Empty);

        logger.Information("first", true);
        logger.Warning("second", true);
        logger.Error("third", true);
        logger.Debug("fourth", true);

        using var listener = new CapturingListener();
        logger.OnSdkAvailable();

        Assert.Equal(SelfDiagnosticsLogger.RoutingMode.Sdk, logger.CurrentRoutingMode);
        Assert.Equal(0, factory.Invocations);
        Assert.Collection(
            listener.Events,
            e => AssertReplayed(e, InformationEventId, "first"),
            e => AssertReplayed(e, WarningEventId, "second"),
            e => AssertReplayed(e, ErrorEventId, "third"),
            e => AssertReplayed(e, VerboseEventId, "fourth"));
    }

    [Fact]
    public void WhenBufferingAndNoListener_Then_OnSdkAvailableChangesNothingAndLaterFallbackDeliversAll()
    {
        var sink = new TestSink();
        var factory = new CountingSinkFactory(sink);
        var logger = new SelfDiagnosticsLogger(LogLevel.Information, factory.Create, string.Empty);

        logger.Information("one", true);
        logger.Information("two", true);

        logger.OnSdkAvailable();

        Assert.Equal(SelfDiagnosticsLogger.RoutingMode.Buffering, logger.CurrentRoutingMode);
        Assert.Equal(0, factory.Invocations);
        Assert.Empty(sink.Messages);

        logger.UseFallbackIfStillBuffering();

        Assert.Equal(SelfDiagnosticsLogger.RoutingMode.Fallback, logger.CurrentRoutingMode);
        Assert.Equal(1, factory.Invocations);
        Assert.Collection(
            sink.Messages,
            line => Assert.Contains("[Information] one", line, StringComparison.Ordinal),
            line => Assert.Contains("[Information] two", line, StringComparison.Ordinal));
    }

    [Fact]
    public void WhenBuffering_Then_UseFallbackIfStillBufferingWritesFormattedLinesToFallbackSinkAndKeepsMirroring()
    {
        var sink = new TestSink();
        var factory = new CountingSinkFactory(sink);
        var logger = new SelfDiagnosticsLogger(LogLevel.Information, factory.Create, string.Empty);

        logger.Information("buffered info", true);
        logger.Warning("buffered warning", true);

        logger.UseFallbackIfStillBuffering();

        Assert.Equal(1, factory.Invocations);
        Assert.Collection(
            sink.Messages,
            line => Assert.Contains("[Information] buffered info", line, StringComparison.Ordinal),
            line => Assert.Contains("[Warning] buffered warning", line, StringComparison.Ordinal));

        using var listener = new CapturingListener();

        logger.Information("mirrored", true);
        logger.Information("not mirrored", false);

        Assert.Equal(4, sink.Messages.Count);
        Assert.Contains("[Information] mirrored", sink.Messages[2], StringComparison.Ordinal);
        Assert.Contains("[Information] not mirrored", sink.Messages[3], StringComparison.Ordinal);
        var mirrored = Assert.Single(listener.Events);
        Assert.Equal((InformationEventId, "mirrored"), mirrored);
    }

    [Fact]
    public void WhenFallbackAndListenerAttached_Then_OnSdkAvailableSwitchesWithoutReplay()
    {
        using var sink = new DisposableTestSink();
        var logger = new SelfDiagnosticsLogger(LogLevel.Information, new CountingSinkFactory(sink).Create, string.Empty);

        logger.Information("before", true);
        logger.UseFallbackIfStillBuffering();
        Assert.Single(sink.Messages);

        using var listener = new CapturingListener();
        logger.OnSdkAvailable();

        Assert.Equal(SelfDiagnosticsLogger.RoutingMode.Sdk, logger.CurrentRoutingMode);
        Assert.Empty(listener.Events);

        logger.Information("after", true);

        var evt = Assert.Single(listener.Events);
        Assert.Equal((InformationEventId, "after"), evt);
        Assert.Single(sink.Messages);
        Assert.Equal(1, sink.DisposeCount);
        logger.Close();
        Assert.Equal(1, sink.DisposeCount);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WhenMoreThanTheBoundIsBuffered_Then_NewestAreDroppedAndOneSummaryIsWritten(bool sdkAvailable)
    {
        var sink = new TestSink();
        var logger = new SelfDiagnosticsLogger(LogLevel.Information, new CountingSinkFactory(sink).Create, string.Empty);

        for (var i = 0; i < 300; i++)
        {
            logger.Information(i.ToString(CultureInfo.InvariantCulture), true);
        }

        if (sdkAvailable)
        {
            using var listener = new CapturingListener();
            logger.OnSdkAvailable();

            var events = listener.Events;
            Assert.Equal(SelfDiagnosticsLogger.MaxBufferedEntries + 1, events.Count);
            for (var i = 0; i < SelfDiagnosticsLogger.MaxBufferedEntries; i++)
            {
                Assert.Equal(InformationEventId, events[i].EventId);
                Assert.EndsWith($"] {i}", events[i].Message, StringComparison.Ordinal);
            }

            var summary = events[SelfDiagnosticsLogger.MaxBufferedEntries];
            Assert.Equal(WarningEventId, summary.EventId);
            Assert.Contains("44 early log entries were dropped", summary.Message, StringComparison.Ordinal);
        }
        else
        {
            logger.UseFallbackIfStillBuffering();

            Assert.Equal(SelfDiagnosticsLogger.MaxBufferedEntries + 1, sink.Messages.Count);
            for (var i = 0; i < SelfDiagnosticsLogger.MaxBufferedEntries; i++)
            {
                Assert.Contains($"[Information] {i} ", sink.Messages[i], StringComparison.Ordinal);
            }

            Assert.Contains("[Warning] 44 early log entries were dropped", sink.Messages[SelfDiagnosticsLogger.MaxBufferedEntries], StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task WhenFallbackDisposalIsBlocked_Then_SdkLoggingAndCloseDoNotWaitOrDisposeAgain()
    {
        using var releaseDispose = new ManualResetEventSlim();
        var disposeEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sink = Substitute.For<ISink, IDisposable>();
        ((IDisposable)sink).When(s => s.Dispose()).Do(_ =>
        {
            disposeEntered.TrySetResult(true);
            releaseDispose.Wait();
        });
        var logger = new SelfDiagnosticsLogger(LogLevel.Information, () => sink, string.Empty, SelfDiagnosticsLogger.RoutingMode.Fallback);
        using var listener = new CapturingListener();

        var transitionTask = Task.Run(logger.OnSdkAvailable);
        Task? loggingTask = null;
        try
        {
            await TaskAssertions.CompletesAsync(disposeEntered.Task);
            loggingTask = Task.Run(() =>
            {
                Assert.Equal(SelfDiagnosticsLogger.RoutingMode.Sdk, logger.CurrentRoutingMode);
                logger.Information("during disposal", false);
                logger.OnSdkAvailable();
                logger.Close();
                logger.Information("after close", false);
            });
            await TaskAssertions.CompletesAsync(loggingTask);
        }
        finally
        {
            releaseDispose.Set();
            await TaskAssertions.CompletesAsync(Task.WhenAll(transitionTask, loggingTask ?? Task.CompletedTask));
            logger.Close();
        }

        Assert.Equal((InformationEventId, "during disposal"), Assert.Single(listener.Events));
        ((IDisposable)sink).Received(1).Dispose();
        sink.DidNotReceive().Write(Arg.Any<string>());
    }

    [Fact]
    public void WhenPrefixIsGiven_Then_ReplayedAndLiveMessagesCarryIt()
    {
        var logger = new SelfDiagnosticsLogger(LogLevel.Information, new CountingSinkFactory().Create, "[OpAmp] ");

        logger.Information("buffered", true);

        using var listener = new CapturingListener();
        logger.OnSdkAvailable();
        logger.Information("live", true);

        Assert.Collection(
            listener.Events,
            e =>
            {
                Assert.StartsWith("[OpAmp] [", e.Message, StringComparison.Ordinal);
                AssertReplayed((e.EventId, e.Message.Substring("[OpAmp] ".Length)), InformationEventId, "buffered");
            },
            e => Assert.Equal((InformationEventId, "[OpAmp] live"), e));
    }

    [Fact]
    public void WhenInSdkMode_Then_WriteToEventLogFalseIsIgnored()
    {
        var logger = new SelfDiagnosticsLogger(LogLevel.Information, new CountingSinkFactory().Create, string.Empty);

        using var listener = new CapturingListener();
        logger.OnSdkAvailable();
        logger.Warning("still delivered", false);

        var evt = Assert.Single(listener.Events);
        Assert.Equal((WarningEventId, "still delivered"), evt);
    }

    [Fact]
    public void WhenClosed_Then_NothingIsDeliveredAndFallbackSinkIsDisposed()
    {
        using var sink = new DisposableTestSink();
        var logger = new SelfDiagnosticsLogger(LogLevel.Information, new CountingSinkFactory(sink).Create, string.Empty);

        logger.Information("before", true);
        logger.UseFallbackIfStillBuffering();
        Assert.Single(sink.Messages);

        logger.Close();
        logger.Close();

        using var listener = new CapturingListener();
        logger.Information("after close", true);

        Assert.Equal(SelfDiagnosticsLogger.RoutingMode.Closed, logger.CurrentRoutingMode);
        Assert.Equal(1, sink.DisposeCount);
        Assert.Single(sink.Messages);
        Assert.Empty(listener.Events);
    }

    [Fact]
    public async Task WhenFallbackWriteIsBlocked_Then_LevelChecksAndDisabledLoggingDoNotWait()
    {
        using var releaseWrite = new ManualResetEventSlim();
        var writeEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sink = Substitute.For<ISink>();
        sink.When(s => s.Write(Arg.Any<string>())).Do(_ =>
        {
            writeEntered.TrySetResult(true);
            releaseWrite.Wait();
        });
        var logger = new SelfDiagnosticsLogger(LogLevel.Information, () => sink, string.Empty, SelfDiagnosticsLogger.RoutingMode.Fallback);

        var writeTask = Task.Run(() => logger.Information("blocked", false));
        Task? checkTask = null;
        try
        {
            await TaskAssertions.CompletesAsync(writeEntered.Task);
            checkTask = Task.Run(() =>
            {
                Assert.Equal(SelfDiagnosticsLogger.RoutingMode.Fallback, logger.CurrentRoutingMode);
                Assert.True(logger.IsEnabled(LogLevel.Information), "Information logging should remain enabled during a blocked fallback write.");
                Assert.False(logger.IsEnabled(LogLevel.Debug), "Debug logging should remain disabled during a blocked fallback write.");
                logger.Debug("disabled", false);
            });
            await TaskAssertions.CompletesAsync(checkTask);
        }
        finally
        {
            releaseWrite.Set();
            await TaskAssertions.CompletesAsync(Task.WhenAll(writeTask, checkTask ?? Task.CompletedTask));
            logger.Close();
        }

        sink.Received(1).Write(Arg.Any<string>());
    }

    [Fact]
    public void WhenLevelIsBelowThreshold_Then_EntryIsNeitherBufferedNorEmitted()
    {
        var sink = new TestSink();
        var logger = new SelfDiagnosticsLogger(LogLevel.Information, new CountingSinkFactory(sink).Create, string.Empty);

        logger.Debug("too verbose", true);

        using var listener = new CapturingListener(EventLevel.Informational);
        logger.OnSdkAvailable();
        logger.Debug("still too verbose", true);

        Assert.Empty(listener.Events);
        Assert.Empty(sink.Messages);
    }

    [Fact]
    public void WhenSdkLevelChanges_Then_LiveEntriesUseTheCurrentListenerLevel()
    {
        var logger = new SelfDiagnosticsLogger(LogLevel.Warning, new CountingSinkFactory().Create, string.Empty);
        using var listener = new CapturingListener(EventLevel.Informational);
        logger.OnSdkAvailable();

        logger.Information("enabled by SDK", true);
        logger.Debug("filtered by SDK", true);
        listener.SetLevel(EventLevel.Verbose);
        logger.Debug("enabled after level change", true);
        listener.SetLevel(EventLevel.Error);
        logger.Warning("filtered after level change", true);
        logger.Error("still enabled", true);

        Assert.Collection(
            listener.Events,
            e => Assert.Equal((InformationEventId, "enabled by SDK"), e),
            e => Assert.Equal((VerboseEventId, "enabled after level change"), e),
            e => Assert.Equal((ErrorEventId, "still enabled"), e));
    }

    [Fact]
    public void WhenFallbackReplayWriteThrows_Then_RemainingEntriesAndDropSummaryAreDelivered()
    {
        var messages = new List<string>();
        var sink = Substitute.For<ISink>();
        sink.When(s => s.Write(Arg.Any<string>())).Do(call =>
        {
            var message = call.Arg<string>();
            if (message.EndsWith("[Information] 0 " + Environment.NewLine, StringComparison.Ordinal))
            {
                throw new IOException("Transient sink failure.");
            }

            messages.Add(message);
        });
        var logger = new SelfDiagnosticsLogger(LogLevel.Information, () => sink, string.Empty);
        for (var i = 0; i <= SelfDiagnosticsLogger.MaxBufferedEntries; i++)
        {
            logger.Information(i.ToString(CultureInfo.InvariantCulture), false);
        }

        var originalWriter = Console.Error;
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        try
        {
            Console.SetError(writer);
            logger.UseFallbackIfStillBuffering();
        }
        finally
        {
            Console.SetError(originalWriter);
            logger.Close();
        }

        Assert.Contains("[Information] 0 ", writer.ToString(), StringComparison.Ordinal);
        Assert.Equal(SelfDiagnosticsLogger.MaxBufferedEntries, messages.Count);
        for (var i = 1; i < SelfDiagnosticsLogger.MaxBufferedEntries; i++)
        {
            Assert.Contains($"[Information] {i} ", messages[i - 1], StringComparison.Ordinal);
        }

        Assert.Contains("1 early log entries were dropped", messages[messages.Count - 1], StringComparison.Ordinal);
    }

    [Fact]
    public void WhenFallbackSinkLogsDuringReplay_Then_TheEntryIsReplayedAfterTheEarlierOnes()
    {
        SelfDiagnosticsLogger? logger = null;

        var messages = new List<string>();
        var echoed = false;
        var sink = Substitute.For<ISink>();

        sink.When(s => s.Write(Arg.Any<string>())).Do(call =>
        {
            messages.Add(call.Arg<string>());
            if (!echoed)
            {
                echoed = true;
                logger!.Information("echo", false);
            }
        });

        logger = new SelfDiagnosticsLogger(LogLevel.Information, () => sink, string.Empty);
        logger.Information("first", false);
        logger.Information("second", false);

        var originalWriter = Console.Error;
        using var writer = new StringWriter(CultureInfo.InvariantCulture);

        try
        {
            Console.SetError(writer);
            logger.UseFallbackIfStillBuffering();
        }
        finally
        {
            Console.SetError(originalWriter);
            logger.Close();
        }

        Assert.Empty(writer.ToString());
        Assert.Collection(
            messages,
            line => Assert.Contains("[Information] first", line, StringComparison.Ordinal),
            line => Assert.Contains("[Information] second", line, StringComparison.Ordinal),
            line => Assert.Contains("[Information] echo", line, StringComparison.Ordinal));
    }

    [Fact]
    public void WhenFallbackSinkLogsOnEveryReplayedWrite_Then_ReplayStopsAtTheBufferBound()
    {
        SelfDiagnosticsLogger? logger = null;

        var messages = new List<string>();
        var sink = Substitute.For<ISink>();

        sink.When(s => s.Write(Arg.Any<string>())).Do(call =>
        {
            messages.Add(call.Arg<string>());
            logger!.Information("echo", false);
        });

        logger = new SelfDiagnosticsLogger(LogLevel.Information, () => sink, string.Empty);
        logger.Information("seed", false);

        logger.UseFallbackIfStillBuffering();
        logger.Close();

        Assert.Equal(SelfDiagnosticsLogger.MaxBufferedEntries + 1, messages.Count);
        Assert.Contains("[Information] seed", messages[0], StringComparison.Ordinal);
        Assert.Contains("[Warning] 1 early log entries were dropped", messages[messages.Count - 1], StringComparison.Ordinal);
    }

    [Fact]
    public void WhenListenerLogsDuringSdkReplay_Then_TheEntryIsReplayedAfterTheEarlierOnes()
    {
        var logger = new SelfDiagnosticsLogger(LogLevel.Information, new CountingSinkFactory().Create, string.Empty);
        logger.Information("first", false);
        logger.Information("second", false);

        var echoed = false;
        using var listener = new CapturingListener();
        listener.OnMessage = _ =>
        {
            if (!echoed)
            {
                echoed = true;
                logger.Information("echo", false);
            }
        };

        logger.OnSdkAvailable();

        Assert.Equal(SelfDiagnosticsLogger.RoutingMode.Sdk, logger.CurrentRoutingMode);
        Assert.Collection(
            listener.Events,
            e => AssertReplayed(e, InformationEventId, "first"),
            e => AssertReplayed(e, InformationEventId, "second"),
            e => AssertReplayed(e, InformationEventId, "echo"));
    }

    [Fact]
    public void WhenFallbackWriteThrows_Then_TheMessageReachesStderrWithoutEscaping()
    {
        var logger = new SelfDiagnosticsLogger(LogLevel.Information, () => new ThrowingSink(), string.Empty);
        logger.UseFallbackIfStillBuffering();
        var originalWriter = Console.Error;
        using var writer = new StringWriter(CultureInfo.InvariantCulture);

        try
        {
            Console.SetError(writer);

            var exception = Record.Exception(() => logger.Warning("fallback message", true));

            Assert.Null(exception);
            Assert.Contains("fallback message", writer.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Console.SetError(originalWriter);
            logger.Close();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WhenFallbackCreationThrows_Then_LoggingDoesNotThrowOrRetryTheFactory(bool startInFallback)
    {
        var invocations = 0;
        var logger = new SelfDiagnosticsLogger(
            LogLevel.Information,
            () =>
            {
                invocations++;
                throw new IOException("Sink creation failed.");
            },
            string.Empty,
            startInFallback ? SelfDiagnosticsLogger.RoutingMode.Fallback : SelfDiagnosticsLogger.RoutingMode.Buffering);

        var exception = Record.Exception(() =>
        {
            logger.Warning("before fallback", true);
            logger.UseFallbackIfStillBuffering();
            logger.UseFallbackIfStillBuffering();
            logger.Warning("after fallback", true);
            logger.Close();
        });

        Assert.Null(exception);
        Assert.Equal(1, invocations);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WhenFallbackDisposalThrows_Then_RoutingStillCompletesAndDisposalIsNotRetried(bool switchToSdk)
    {
        using var sink = new DisposableTestSink { ThrowOnDispose = true };
        var logger = new SelfDiagnosticsLogger(LogLevel.Information, () => sink, string.Empty);
        logger.UseFallbackIfStillBuffering();
        using var listener = new CapturingListener();

        var exception = Record.Exception(() =>
        {
            if (switchToSdk)
            {
                logger.OnSdkAvailable();
            }
            else
            {
                logger.Close();
            }
        });

        Assert.Null(exception);
        Assert.Equal(switchToSdk ? SelfDiagnosticsLogger.RoutingMode.Sdk : SelfDiagnosticsLogger.RoutingMode.Closed, logger.CurrentRoutingMode);
        logger.Information("after disposal", true);
        Assert.Equal(switchToSdk ? 1 : 0, listener.Events.Count);
        logger.Close();
        Assert.Equal(1, sink.DisposeCount);
    }

    private static void AssertReplayed((int EventId, string Message) evt, int expectedEventId, string expectedRawMessage)
    {
        Assert.Equal(expectedEventId, evt.EventId);

        // "[<ISO-8601 UTC timestamp>] <raw message>"
        Assert.StartsWith("[", evt.Message, StringComparison.Ordinal);
        var end = evt.Message.IndexOf("] ", StringComparison.Ordinal);
        Assert.True(end > 1, $"Missing timestamp or closing delimiter in '{evt.Message}'.");
        var timestamp = evt.Message.Substring(1, end - 1);
        Assert.True(
            DateTime.TryParseExact(timestamp, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed),
            $"'{timestamp}' is not a round-trip timestamp.");
        Assert.Equal(DateTimeKind.Utc, parsed.Kind);
        Assert.Equal(expectedRawMessage, evt.Message.Substring(end + 2));
    }

    private sealed class CountingSinkFactory
    {
        private readonly ISink _sink;

        public CountingSinkFactory(ISink? sink = null)
        {
            _sink = sink ?? new TestSink();
        }

        public int Invocations { get; private set; }

        public ISink Create()
        {
            Invocations++;
            return _sink;
        }
    }

    private sealed class DisposableTestSink : ISink, IDisposable
    {
        public List<string> Messages { get; } = [];

        public int DisposeCount { get; private set; }

        public bool ThrowOnDispose { get; set; }

        public void Write(string message)
        {
            Messages.Add(message);
        }

        public void Dispose()
        {
            DisposeCount++;
            if (ThrowOnDispose)
            {
                ThrowOnDispose = false;
                throw new IOException("Sink disposal failed.");
            }
        }
    }

    private sealed class ThrowingSink : ISink
    {
        public void Write(string message)
        {
            throw new IOException("Sink failure for the test.");
        }
    }

    private sealed class CapturingListener : EventListener
    {
        private readonly List<(int EventId, string Message)> _events = [];
        private EventSource? _source;
        private EventLevel _level = EventLevel.Verbose;

        public CapturingListener(EventLevel level = EventLevel.Verbose)
        {
            SetLevel(level);
        }

        public IReadOnlyList<(int EventId, string Message)> Events
        {
            get
            {
                lock (_events)
                {
                    return [.. _events];
                }
            }
        }

        // Invoked synchronously on the writing thread after each captured event.
        public Action<string>? OnMessage { get; set; }

        public void SetLevel(EventLevel level)
        {
            _level = level;
            if (_source != null)
            {
                EnableEvents(_source, level);
            }
        }

        public override void Dispose()
        {
            // On .NET Framework, disposing a listener does not clear the source's enabled state until
            // later, which would leak "SDK available" into the next test. Disable explicitly first.
            if (_source != null)
            {
                DisableEvents(_source);
            }

            base.Dispose();
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == "OpenTelemetry-AutoInstrumentation")
            {
                _source = eventSource;
                EnableEvents(eventSource, _level);
            }

            base.OnEventSourceCreated(eventSource);
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData.EventSource.Name != "OpenTelemetry-AutoInstrumentation")
            {
                return;
            }

            var message = eventData.Payload![0] as string ?? string.Empty;
            lock (_events)
            {
                _events.Add((eventData.EventId, message));
            }

            OnMessage?.Invoke(message);
        }
    }
}
