// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.Tracing;

namespace OpenTelemetry.AutoInstrumentation.Logging;

/// <summary>
/// Buffers startup diagnostics, then routes entries to SDK diagnostics or a fallback sink.
/// </summary>
internal sealed class SelfDiagnosticsLogger : InternalLogger
{
    internal const int MaxBufferedEntries = 256;

    private readonly object _lock = new();
    private readonly LogLevel? _initialLevel;
    private readonly Func<ISink> _fallbackSinkFactory;
    private readonly string _sdkMessagePrefix;

    // Written only under _lock, but read without it by IsEnabled and the SDK fast path in Emit.
    private volatile RoutingMode _routingMode;

    // The remaining state is only accessed under _lock.
    private List<(DateTime TimestampUtc, LogLevel Level, string RawMessage)>? _buffer;
    private int _dropped;
    private ISink? _fallbackSink;

    /// <summary>
    /// Initializes a new instance of the <see cref="SelfDiagnosticsLogger"/> class.
    /// </summary>
    /// <param name="level">The initial log level, or null to disable logging until SDK diagnostics are enabled.</param>
    /// <param name="fallbackSinkFactory">Creates the fallback sink, at most once and only when needed.</param>
    /// <param name="sdkMessagePrefix">
    /// Prepended to entries routed to SDK self-diagnostics. Fallback output is already
    /// per-logger and is not prefixed.
    /// </param>
    /// <param name="initialRoutingMode">
    /// The routing mode to start in. Loggers created after the process-wide routing mode has left
    /// <see cref="RoutingMode.Buffering"/> start in that mode instead of buffering.
    /// </param>
    internal SelfDiagnosticsLogger(LogLevel? level, Func<ISink> fallbackSinkFactory, string sdkMessagePrefix, RoutingMode initialRoutingMode = RoutingMode.Buffering)
        : base(NoopSink.Instance, level.GetValueOrDefault())
    {
        _initialLevel = level;
        _fallbackSinkFactory = fallbackSinkFactory ?? throw new ArgumentNullException(nameof(fallbackSinkFactory));
        _sdkMessagePrefix = sdkMessagePrefix ?? throw new ArgumentNullException(nameof(sdkMessagePrefix));
        _routingMode = initialRoutingMode;
        _buffer = initialRoutingMode == RoutingMode.Buffering ? [] : null;

        if (initialRoutingMode == RoutingMode.Fallback)
        {
            // No lock needed: the instance is not yet visible to other threads.
            _fallbackSink = CreateFallbackSink();
        }
    }

    internal enum RoutingMode
    {
        Buffering,
        Sdk,
        Fallback,
        Closed,
    }

    internal RoutingMode CurrentRoutingMode => _routingMode;

    public override bool IsEnabled(LogLevel level)
    {
        // Once routed to the SDK, the attached listener's level decides; until then, the configured level applies.
        return _routingMode switch
        {
            RoutingMode.Closed => false,
            RoutingMode.Sdk => AutoInstrumentationEventSource.Log.IsEnabled(
                level switch
                {
                    LogLevel.Error => EventLevel.Error,
                    LogLevel.Warning => EventLevel.Warning,
                    LogLevel.Information => EventLevel.Informational,
                    LogLevel.Debug => EventLevel.Verbose,
                    _ => EventLevel.LogAlways,
                },
                EventKeywords.None),
            _ => _initialLevel.HasValue && base.IsEnabled(level),
        };
    }

    public override void Close()
    {
        ISink? sinkToDispose;

        // Detach the sink under the lock so that no locked write can still be using it once it is disposed.
        lock (_lock)
        {
            _routingMode = RoutingMode.Closed;
            _buffer = null;
            sinkToDispose = _fallbackSink;
            _fallbackSink = null;
        }

        DisposeSink(sinkToDispose);
    }

    /// <summary>
    /// Replays buffered entries and routes subsequent entries to an enabled event-source listener.
    /// </summary>
    internal void OnSdkAvailable()
    {
        // Without an enabled listener, entries sent to the SDK would be lost, so keep the current routing mode.
        if (!AutoInstrumentationEventSource.Log.IsEnabled())
        {
            return;
        }

        ISink? sinkToDispose;

        lock (_lock)
        {
            if (_routingMode is RoutingMode.Sdk or RoutingMode.Closed)
            {
                return;
            }

            // From Fallback the buffer is already gone, so this only moves later entries off the fallback sink.
            ReplayBuffer(RoutingMode.Sdk);

            sinkToDispose = _fallbackSink;
            _fallbackSink = null;

            // Publish the Sdk routing mode only after the replay: SDK writes take no lock, so they must not overtake it.
            _routingMode = RoutingMode.Sdk;
        }

        DisposeSink(sinkToDispose);
    }

    /// <summary>
    /// Flushes buffered entries to the fallback sink. Other routing modes are unaffected.
    /// </summary>
    internal void UseFallbackIfStillBuffering()
    {
        lock (_lock)
        {
            if (_routingMode != RoutingMode.Buffering)
            {
                return;
            }

            _fallbackSink = CreateFallbackSink();

            ReplayBuffer(RoutingMode.Fallback);

            _routingMode = RoutingMode.Fallback;
        }
    }

    /// <inheritdoc/>
    protected override void Emit(LogLevel level, string rawMessage, DateTime timestampUtc, bool writeToEventLog)
    {
        // The Sdk routing mode only ever changes to Closed, so it needs no lock. A write racing with Close may still
        // reach the event source, which is harmless. Every entry goes to the SDK, so writeToEventLog is ignored.
        if (_routingMode == RoutingMode.Sdk)
        {
            WriteEventSourceLog(level, _sdkMessagePrefix + rawMessage);
            return;
        }

        // Serialize with routing changes so entries cannot overtake the replayed ones.
        lock (_lock)
        {
            switch (_routingMode)
            {
                case RoutingMode.Buffering:
                    if (_buffer!.Count < MaxBufferedEntries)
                    {
                        _buffer.Add((timestampUtc, level, rawMessage));
                    }
                    else
                    {
                        _dropped++;
                    }

                    break;

                case RoutingMode.Sdk:
                    // The route switched to the SDK while this call was waiting for the lock.
                    WriteEventSourceLog(level, _sdkMessagePrefix + rawMessage);
                    break;

                case RoutingMode.Fallback:
                    // Same output as the base logger, so the fallback behaves as logging did before self-diagnostics.
                    _fallbackSink!.Write(FormatLine(level, rawMessage, timestampUtc));
                    if (writeToEventLog)
                    {
                        WriteEventSourceLog(level, rawMessage);
                    }

                    break;

                case RoutingMode.Closed:
                    break;
            }
        }
    }

    /// <summary>
    /// Disposes a fallback sink that is no longer in use, ignoring any failure.
    /// </summary>
    /// <param name="sink">The sink to dispose, or null if there is nothing to dispose.</param>
    /// <remarks>
    /// Call without holding <c>_lock</c>, after the sink has been detached from the logger,
    /// so that a slow or blocking dispose cannot stall concurrent log writes.
    /// </remarks>
    private static void DisposeSink(ISink? sink)
    {
        try
        {
            (sink as IDisposable)?.Dispose();
        }
#pragma warning disable CA1031 // Do not catch general exception
        catch (Exception)
#pragma warning restore CA1031 // Do not catch general exception
        {
            // Logging must never fail the host application.
        }
    }

    /// <summary>
    /// Writes all buffered startup entries to the given route, followed by a warning if any entries were dropped,
    /// and ends buffering.
    /// </summary>
    /// <param name="target">The route to replay to: <see cref="RoutingMode.Sdk"/> or <see cref="RoutingMode.Fallback"/>.</param>
    /// <remarks>
    /// The caller must hold <c>_lock</c> and must not publish the new routing mode until this method returns,
    /// so that new entries cannot be written ahead of the replayed ones.
    /// <para>
    /// The lock is re-entrant, so an event listener or fallback sink that logs through this logger on the same
    /// thread reaches <see cref="Emit"/> mid-replay, still in <see cref="RoutingMode.Buffering"/>. Such entries
    /// are buffered and replayed in order like any other.
    /// </para>
    /// </remarks>
    private void ReplayBuffer(RoutingMode target)
    {
        if (_buffer is not { } buffer)
        {
            return;
        }

        // Index loop, not foreach: entries logged re-entrantly during replay are appended to the buffer and must
        // not invalidate the iteration. MaxBufferedEntries bounds how many can be added.
        for (var i = 0; i < buffer.Count; i++)
        {
            var entry = buffer[i];
            ReplayLog(target, entry.Level, entry.RawMessage, entry.TimestampUtc);
        }

        if (_dropped > 0)
        {
            // The buffer stays in place until after this warning. Entries are only dropped once the buffer is full,
            // so anything logged re-entrantly here is dropped too, rather than appended to a buffer nobody replays.
            ReplayLog(target, LogLevel.Warning, $"{_dropped} early log entries were dropped before SDK self-diagnostics started.");
        }

        _buffer = null;
    }

    /// <summary>
    /// Writes a single replayed entry to the given route, falling back to standard error if that write fails.
    /// </summary>
    /// <param name="target">The route to write to: <see cref="RoutingMode.Sdk"/> or <see cref="RoutingMode.Fallback"/>.</param>
    /// <param name="level">The level of the entry.</param>
    /// <param name="rawMessage">The unformatted message of the entry.</param>
    /// <param name="timestampUtc">When the entry was originally logged, or null to use the current time.</param>
    /// <remarks>
    /// The caller must hold <c>_lock</c>. When <paramref name="target"/> is <see cref="RoutingMode.Fallback"/>,
    /// the fallback sink must already have been created.
    /// </remarks>
    private void ReplayLog(RoutingMode target, LogLevel level, string rawMessage, DateTime? timestampUtc = null)
    {
        try
        {
            if (target == RoutingMode.Sdk)
            {
                // The replay time becomes the SDK timestamp; the original time is kept in the text.
                var message = timestampUtc is { } timestamp
                    ? $"{_sdkMessagePrefix}[{timestamp:O}] {rawMessage}"
                    : _sdkMessagePrefix + rawMessage;

                WriteEventSourceLog(level, message);
            }
            else
            {
                _fallbackSink!.Write(FormatLine(level, rawMessage, timestampUtc ?? DateTime.UtcNow));
            }
        }
#pragma warning disable CA1031 // Do not catch general exception
        catch (Exception)
#pragma warning restore CA1031 // Do not catch general exception
        {
            try
            {
                Console.Error.WriteLine(FormatLine(level, rawMessage, timestampUtc ?? DateTime.UtcNow));
            }
#pragma warning disable CA1031 // Do not catch general exception
            catch (Exception)
#pragma warning restore CA1031 // Do not catch general exception
            {
                // A failed entry must not prevent replay of the remaining entries.
            }
        }
    }

    /// <summary>
    /// Creates the fallback sink, substituting a no-op sink if creation fails.
    /// </summary>
    /// <returns>The fallback sink; never null.</returns>
    /// <remarks>
    /// Outside the constructor, the caller must hold <c>_lock</c> so that only one fallback sink is ever created.
    /// </remarks>
    private ISink CreateFallbackSink()
    {
        try
        {
            return _fallbackSinkFactory() ?? NoopSink.Instance;
        }
#pragma warning disable CA1031 // Do not catch general exception
        catch (Exception)
#pragma warning restore CA1031 // Do not catch general exception
        {
            return NoopSink.Instance;
        }
    }
}
