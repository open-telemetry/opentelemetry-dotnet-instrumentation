using System;
using System.Diagnostics;
using System.Text;
using OpenTelemetry.AutoInstrumentation.ClrProfiler.DuckTyping;
using OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Configuration;
using OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Logging;
using OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Tagging;
using OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Util;
using OpenTelemetry.Trace;

namespace OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Instrumentations.GraphQL
{
    internal class GraphQLCommon
    {
        internal const string GraphQLAssembly = "GraphQL";
        internal const string Major2 = "2";
        internal const string Major2Minor3 = "2.3";

        internal const string ServiceName = "graphql";
        internal const string ParseOperationName = "graphql.parse"; // Instrumentation not yet implemented
        internal const string ValidateOperationName = "graphql.validate";
        internal const string ExecuteOperationName = "graphql.execute";
        internal const string ResolveOperationName = "graphql.resolve"; // Instrumentation not yet implemented

        internal const string IntegrationName = nameof(Configuration.Instrumentation.GraphQL);
        internal static readonly IntegrationInfo IntegrationId = IntegrationRegistry.GetIntegrationInfo(IntegrationName);

        internal static readonly ActivitySource ActivitySource = new ActivitySource(
            "OpenTelemetry.AutoInstrumentation.GraphQL", "0.0.1");

        private static readonly ILogger Log = ConsoleLogger.Create(typeof(GraphQLCommon));

        internal static Activity CreateActivityFromValidate(IDocument document)
        {
            var settings = Instrumentation.TracerSettings;

            if (!settings.IsIntegrationEnabled(IntegrationId))
            {
                // integration disabled, don't create a scope, skip this trace
                return null;
            }

            Activity activity = null;

            try
            {
                var tags = new GraphQLTags();
                string serviceName = GetServiceName();
                activity = ActivitySource.StartActivityWithTags(ValidateOperationName, serviceName: serviceName, tags: tags);

                tags.Source = document.OriginalQuery;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating or populating scope.");
            }

            return activity;
        }

        internal static Activity CreateActivityFromExecuteAsync(IExecutionContext executionContext)
        {
            var settings = Instrumentation.TracerSettings;

            if (!settings.IsIntegrationEnabled(IntegrationId))
            {
                // integration disabled, don't create a scope, skip this trace
                return null;
            }

            Activity activity = null;

            try
            {
                string source = executionContext.Document.OriginalQuery;
                string operationName = executionContext.Operation.Name;
                string operationType = executionContext.Operation.OperationType.ToString();

                var tags = new GraphQLTags();
                string serviceName = GetServiceName();
                activity = ActivitySource.StartActivityWithTags(ExecuteOperationName, serviceName: serviceName, tags: tags);
                activity.SetResourceName($"{operationType} {operationName ?? "operation"}");

                tags.Source = source;
                tags.OperationName = operationName;
                tags.OperationType = operationType;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating or populating scope.");
            }

            return activity;
        }

        internal static void RecordExecutionErrorsIfPresent(Activity activity, string errorType, IExecutionErrors executionErrors)
        {
            var errorCount = executionErrors?.Count ?? 0;

            if (errorCount > 0)
            {
                activity.SetTag(Tags.Status, Status.Error);
                activity.SetTag(Tags.ErrorMsg, $"{errorCount} error(s)");
                activity.SetTag(Tags.ErrorType, errorType);
                activity.SetTag(Tags.ErrorStack, ConstructErrorMessage(executionErrors));
            }
        }

        internal static string GetServiceName()
        {
            return $"{Instrumentation.TracerSettings.ServiceName}-{ServiceName}";
        }

        private static string ConstructErrorMessage(IExecutionErrors executionErrors)
        {
            if (executionErrors == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();

            try
            {
                var tab = "    ";
                builder.AppendLine("errors: [");

                for (int i = 0; i < executionErrors.Count; i++)
                {
                    var executionError = executionErrors[i];

                    builder.AppendLine($"{tab}{{");

                    var message = executionError.Message;
                    if (message != null)
                    {
                        builder.AppendLine($"{tab + tab}\"message\": \"{message.Replace("\r", "\\r").Replace("\n", "\\n")}\",");
                    }

                    var path = executionError.Path;
                    if (path != null)
                    {
                        builder.AppendLine($"{tab + tab}\"path\": \"{string.Join(".", path)}\",");
                    }

                    var code = executionError.Code;
                    if (code != null)
                    {
                        builder.AppendLine($"{tab + tab}\"code\": \"{code}\",");
                    }

                    builder.AppendLine($"{tab + tab}\"locations\": [");
                    var locations = executionError.Locations;
                    if (locations != null)
                    {
                        foreach (var location in locations)
                        {
                            if (location.TryDuckCast<ErrorLocationStruct>(out var locationProxy))
                            {
                                builder.AppendLine($"{tab + tab + tab}{{");
                                builder.AppendLine($"{tab + tab + tab + tab}\"line\": {locationProxy.Line},");
                                builder.AppendLine($"{tab + tab + tab + tab}\"column\": {locationProxy.Column}");
                                builder.AppendLine($"{tab + tab + tab}}},");
                            }
                        }
                    }

                    builder.AppendLine($"{tab + tab}]");
                    builder.AppendLine($"{tab}}},");
                }

                builder.AppendLine("]");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating GraphQL error message.");
                return "errors: []";
            }

            return builder.ToString();
        }
    }
}
