// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Process.Integrations;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.Context.Propagation;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Process;

internal static class ProcessInstrumentation
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();
    private static ProcessContextPropagation? _context;

    internal static bool InjectionEnabled { get; private set; }

    internal static void Initialize(InstrumentationOptions options)
    {
        try
        {
            // SDK setup may be delegated to the application. Initialize its default
            // propagators before taking our startup snapshot, without creating a provider.
            RuntimeHelpers.RunClassConstructor(typeof(Sdk).TypeHandle);
            var context = new ProcessContextPropagation(options.ProcessContextExtractionMode, Propagators.DefaultTextMapPropagator, EnvironmentCarrier.Capture());
            context.InitializeBaggage();
            _context = context;
            InjectionEnabled = options.ProcessContextInjectionEnabled;
        }
        catch (Exception exception)
        {
            Logger.Warning(exception, "Process environment context initialization failed.");
        }
    }

    internal static bool IsDefinitionEnabled(NativeCallTargetDefinition2 definition)
    {
        // Do not rewrite activity creation across the application unless startup
        // extraction produced a usable trace context. Baggage and injection are independent.
        return _context?.HasIncomingTraceContext == true ||
               Marshal.PtrToStringUni(definition.IntegrationType) != typeof(ActivitySourceCreateActivityIntegration).FullName;
    }

    internal static void ApplyToRoot(ref ActivityContext context, string? parentId, ref IEnumerable<ActivityLink>? links)
    {
        try
        {
            _context?.ApplyToRoot(ref context, parentId, ref links);
        }
        catch (Exception exception)
        {
            Logger.Debug(exception, "Could not apply process environment context to the root activity.");
        }
    }

    internal static void InjectEnvironment(IDictionary<string, string?> environment)
    {
        _context?.InjectEnvironment(environment);
    }
}
