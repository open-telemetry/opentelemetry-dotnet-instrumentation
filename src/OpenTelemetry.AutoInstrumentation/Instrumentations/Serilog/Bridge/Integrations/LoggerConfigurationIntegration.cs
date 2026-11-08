// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using System.Reflection;
using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Serilog.Bridge.Integrations;

/// <summary>
/// Serilog LoggerConfiguration integration.
/// </summary>
[InstrumentMethod(
    assemblyName: "Serilog",
    typeName: "Serilog.LoggerConfiguration",
    methodName: "CreateLogger",
    returnTypeName: "Serilog.Core.Logger",
    parameterTypeNames: [],
    minimumVersion: "2.0.0",
    maximumVersion: "4.*.*",
    integrationName: "Serilog",
    type: InstrumentationType.Log)]
public static class LoggerConfigurationIntegration
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();

    internal static CallTargetState OnMethodBegin<TTarget>(TTarget instance)
    {
        if (!Instrumentation.LogSettings.Value.EnableSerilogBridge || instance is null)
        {
            return CallTargetState.GetDefault();
        }

        try
        {
            ILoggerProviderCollection? loggerProviders = null;
            if (ExportsToOpenTelemetry(instance, depth: 0, ref loggerProviders))
            {
                Logger.Debug("Serilog bridge not added: the logger already writes to Serilog.Sinks.OpenTelemetry.");
                return CallTargetState.GetDefault();
            }

            var sinks = instance.DuckCast<ILoggerConfiguration>().LogEventSinks;
            var sink = new OpenTelemetrySerilogSink { LoggerProviders = loggerProviders };
            sinks.Add(sink.DuckImplement(sinks.GetType().GetGenericArguments()[0]));
        }
        catch (Exception e)
        {
            Logger.Debug(e, "Failed to add the Serilog bridge sink.");
        }

        return CallTargetState.GetDefault();
    }

    // Walks every field that holds a sink, so wrapper field names that differ per Serilog version do not matter.
    // A bridge sink found here was added by a sub-logger's CreateLogger call, which runs first, so it is disabled.
    private static bool ExportsToOpenTelemetry(object node, int depth, ref ILoggerProviderCollection? loggerProviders)
    {
        if (node is IDuckType { Instance: OpenTelemetrySerilogSink bridgeSink })
        {
            bridgeSink.Disabled = true;
            return false;
        }

        var type = node.GetType();
        if (type.FullName is "Serilog.Sinks.OpenTelemetry.OpenTelemetrySink")
        {
            return true;
        }

        if (type.FullName is "Serilog.Extensions.Logging.LoggerProviderCollectionSink")
        {
            loggerProviders = node.DuckCast<ILoggerProviderCollectionSink>().Providers;
            return false;
        }

        // Bounds the walk when a sink graph has a cycle.
        if (depth > 10)
        {
            return false;
        }

        var found = false;
        for (var declaringType = type; declaringType != null; declaringType = declaringType.BaseType)
        {
            foreach (var field in declaringType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                var value = field.GetValue(node);
                if (value is not null && IsSink(value.GetType()))
                {
                    found |= ExportsToOpenTelemetry(value, depth + 1, ref loggerProviders);
                }
                else if (value is IEnumerable items && IsSink(GetElementType(field.FieldType)))
                {
                    foreach (var item in items)
                    {
                        found |= item is not null && ExportsToOpenTelemetry(item, depth + 1, ref loggerProviders);
                    }
                }
            }
        }

        return found;
    }

    private static bool IsSink(Type? type)
    {
        return type is not null &&
               (type.Name is "ILogEventSink" or "IBatchedLogEventSink" ||
                type.GetInterfaces().Any(i => i.Name is "ILogEventSink" or "IBatchedLogEventSink"));
    }

    private static Type? GetElementType(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType();
        }

        var enumerable = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
            ? type
            : type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        return enumerable?.GetGenericArguments()[0];
    }
}
