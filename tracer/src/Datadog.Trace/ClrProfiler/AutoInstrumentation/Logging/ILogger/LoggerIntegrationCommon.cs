using System;
using Datadog.Trace.Configuration;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Logging.ILogger
{
    internal static class LoggerIntegrationCommon
    {
        public const string IntegrationName = nameof(IntegrationIds.ILogger);
        private static readonly IntegrationInfo IntegrationId = IntegrationRegistry.GetIntegrationInfo(IntegrationName);

        private static readonly DatadogLoggingScope DatadogScope = new();

        public static void AddScope<TAction, TState>(Tracer tracer, TAction callback, TState state)
        {
            if (tracer.Settings.LogsInjectionEnabled
             && tracer.Settings.IsIntegrationEnabled(IntegrationId)
             && callback is Action<object, TState> foreachCallback)
            {
                foreachCallback.Invoke(DatadogScope, state);
            }
        }
    }
}
