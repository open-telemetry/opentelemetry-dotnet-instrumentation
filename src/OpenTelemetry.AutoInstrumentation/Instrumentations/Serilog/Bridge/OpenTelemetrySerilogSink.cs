// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.AutoInstrumentation.Instrumentations.LogBridge;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.Logs;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Serilog.Bridge;

/// <summary>
/// Serilog sink that bridges Serilog log events into OpenTelemetry LogRecords.
/// </summary>
internal class OpenTelemetrySerilogSink
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();
    private static readonly Lazy<Func<string?, object?>?> GetLoggerFactory = new(CreateGetLoggerDelegate, true);
    private static readonly ConcurrentDictionary<string, object> Loggers = new(StringComparer.Ordinal);
    private static readonly string[] LevelNames = ["Verbose", "Debug", "Information", "Warning", "Error", "Fatal"];

    public bool Disabled { get; set; }

    public List<ILoggerProviderCollection> LoggerProviders { get; set; } = [];

    [DuckReverseMethod(ParameterTypeNames = ["Serilog.Events.LogEvent, Serilog"])]
    public void Emit(ILogEvent logEvent)
    {
        var logEmitter = OpenTelemetryLogHelpers.LogEmitterWithContext;
        if (Disabled || Sdk.SuppressInstrumentation || logEmitter is null)
        {
            return;
        }

        foreach (var loggerProviders in LoggerProviders)
        {
            foreach (var provider in loggerProviders.Providers)
            {
                if (provider is OpenTelemetryLoggerProvider)
                {
                    return;
                }
            }
        }

        string? sourceContext = null;
        var properties = new List<KeyValuePair<string, object?>>();
        foreach (var item in logEvent.Properties)
        {
            var property = item.DuckCast<LogEventProperty>();
            var value = ConvertValue(property.Value);
            if (property.Key == "SourceContext")
            {
                sourceContext = value as string;
                continue;
            }

            properties.Add(new KeyValuePair<string, object?>(property.Key, value));
        }

        if (Instrumentation.LogSettings.Value.IncludeFormattedMessage)
        {
            properties.Add(new KeyValuePair<string, object?>("serilog.rendered_message", logEvent.RenderMessage(null)));
        }

        var logger = GetLogger(sourceContext);
        if (logger is null)
        {
            return;
        }

        var activity = Activity.Current;
        var context = activity?.Context ?? default;

        // Serilog stores no trace flags, so the flags of Activity.Current are kept only when the stored IDs match it.
        if (((IDuckType)logEvent).Instance.TryDuckCast<LogEventTraceContext>(out var stored) &&
            (stored.TraceId != activity?.TraceId || stored.SpanId != activity?.SpanId))
        {
            context = stored is { TraceId: { } traceId, SpanId: { } spanId } ? new ActivityContext(traceId, spanId, ActivityTraceFlags.None) : default;
        }

        var level = logEvent.Level;
        logEmitter(
            logger,
            logEvent.MessageTemplate.Text,
            logEvent.Timestamp.UtcDateTime,
            level >= 0 && level < LevelNames.Length ? LevelNames[level] : null,
            MapLogLevel(level),
            logEvent.Exception,
            properties,
            context,
            null,
            null);
    }

    internal static int MapLogLevel(int levelValue)
    {
        // Serilog LogEventLevel Verbose (0) to Fatal (5) -> OpenTelemetry Trace, Debug, Info, Warn, Error, Fatal.
        return levelValue switch
        {
            <= 0 => 1,
            1 => 5,
            2 => 9,
            3 => 13,
            4 => 17,
            _ => 21
        };
    }

    internal static object? ConvertValue(object? value)
    {
        switch (value?.GetType().FullName)
        {
            case "Serilog.Events.ScalarValue":
                value = value.DuckCast<IScalarValue>().Value;
                if (value is null or string || value.GetType().IsPrimitive)
                {
                    return value;
                }

                break;
            case "Serilog.Events.StructureValue":
                var members = new List<KeyValuePair<string, object?>>();
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var item in value.DuckCast<IStructureValue>().Properties)
                {
                    var member = item.DuckCast<IStructureProperty>();
                    var name = member.Name;
                    if (names.Add(name))
                    {
                        members.Add(new KeyValuePair<string, object?>(name, ConvertValue(member.Value)));
                    }
                }

                return new SerilogKeyValueList(members, (IFormattable)value);
            case "Serilog.Events.DictionaryValue":
                var entries = new List<KeyValuePair<string, object?>>();
                var keys = new HashSet<string>(StringComparer.Ordinal);
                foreach (var item in value.DuckCast<IElementsValue>().Elements)
                {
                    var element = item.DuckCast<DictionaryElement>();
                    var keyValue = element.Key.DuckCast<IScalarValue>().Value;
                    var key = (keyValue is IFormattable formattableKey ? formattableKey.ToString(null, CultureInfo.InvariantCulture) : keyValue?.ToString()) ?? "null";
                    if (keys.Add(key))
                    {
                        entries.Add(new KeyValuePair<string, object?>(key, ConvertValue(element.Value)));
                    }
                }

                return new SerilogKeyValueList(entries, (IFormattable)value);
            case "Serilog.Events.SequenceValue":
                var elements = new List<object?>();
                foreach (var element in value.DuckCast<IElementsValue>().Elements)
                {
                    // The SDK writes an array nested in an array as text, so a nested sequence becomes Serilog's text here.
                    elements.Add(element?.GetType().FullName == "Serilog.Events.SequenceValue"
                        ? ((IFormattable)element).ToString(null, CultureInfo.InvariantCulture)
                        : ConvertValue(element));
                }

                return elements.ToArray();
        }

        return value is IFormattable formattable ? formattable.ToString(null, CultureInfo.InvariantCulture) : value?.ToString();
    }

    private static Func<string?, object?>? CreateGetLoggerDelegate()
    {
        try
        {
            var methodInfo = typeof(LoggerProvider)
                .GetMethod("GetLogger", BindingFlags.NonPublic | BindingFlags.Instance, null, [typeof(string)], null)!;
#if NET
            return methodInfo.CreateDelegate<Func<string?, object?>>(Instrumentation.LoggerProvider!);
#else
            return (Func<string?, object?>)methodInfo.CreateDelegate(typeof(Func<string?, object?>), Instrumentation.LoggerProvider!);
#endif
        }
        catch (Exception e)
        {
            Logger.Error(e, "Failed to create logger factory delegate.");
            return null;
        }
    }

    private static object? GetLogger(string? loggerName)
    {
        var getLoggerFactory = GetLoggerFactory.Value;
        if (getLoggerFactory is null)
        {
            return null;
        }

        var name = loggerName ?? string.Empty;
        if (Loggers.TryGetValue(name, out var logger))
        {
            return logger;
        }

        if (Loggers.Count < 100)
        {
            return Loggers.GetOrAdd(name, getLoggerFactory!);
        }

        return getLoggerFactory(name);
    }
}
